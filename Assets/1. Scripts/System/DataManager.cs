using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Churub.Core;

public interface IObjectDataSave { void ObjectDataSave(); }
// Preserve the existing serialized/runtime compatibility type.
public class BaseCost : GameDataState { }

public class DataManager : Singleton<DataManager>
{
    public BaseCost baseCost;
    public string fileName = "SaveFile";
    private readonly List<IObjectDataSave> objectDataList = new List<IObjectDataSave>();
    private GameDataRepository repository;
    private bool deleting, ready, paused, shuttingDown;
    private readonly SnapshotSaveQueue saves = new SnapshotSaveQueue();
    private bool saving => saves.Running;
    private float nextSave;
    private string pendingJson;
    public OperationResult LastSaveResult { get; private set; }
    public event Action<OperationResult> SaveCompleted;
    public bool HasPendingLocalSave => repository != null && (File.Exists(LocalPath) || File.Exists(LocalPath + ".tmp"));
    private string LocalPath => Path.Combine(Application.persistentDataPath, "backnd-" + Uri.EscapeDataString(repository.Owner) + ".pending.json");

    public void AddObjStackCountList(IObjectDataSave item)
    { if (!objectDataList.Contains(item)) objectDataList.Add(item); }

    public OperationResult Apply(GameDataRepository source, BaseCost loaded)
    {
        if (loaded == null) return OperationResult.Error(FailureKind.InvalidData, "Missing player data.");
        if (saving) return OperationResult.Error(FailureKind.Busy, "Previous save is still settling.");
        ready = false;
        repository = source;
        try
        {
            // An outstanding local write belongs exclusively to this authenticated account.
            if (File.Exists(LocalPath))
            {
                pendingJson = File.ReadAllText(LocalPath);
                loaded = GameDataCodec.Deserialize(pendingJson);
            }
            else if (File.Exists(LocalPath + ".tmp"))
            {
                pendingJson = File.ReadAllText(LocalPath + ".tmp");
                loaded = GameDataCodec.Deserialize(pendingJson);
            }
            BalanceTable.Synchronize(loaded);
            baseCost = loaded;
            ready = true;
            nextSave = Time.unscaledTime + 30;
            return new OperationResult();
        }
        catch (Exception e) { return OperationResult.Error(FailureKind.InvalidData, "Local recovery failed: " + e.GetType().Name); }
    }

    private void Capture()
    {
        for (int i = objectDataList.Count - 1; i >= 0; i--)
        {
            var item = objectDataList[i];
            if (item is UnityEngine.Object obj && obj == null) { objectDataList.RemoveAt(i); continue; }
            item.ObjectDataSave();
        }
        BalanceTable.Synchronize(baseCost);
        pendingJson = GameDataCodec.Serialize(baseCost);
        // Same-directory atomic replace: keep the old valid snapshot if the write is interrupted.
        string temporary = LocalPath + ".tmp";
        File.WriteAllText(temporary, pendingJson);
        if (File.Exists(LocalPath)) File.Replace(temporary, LocalPath, null);
        else File.Move(temporary, LocalPath);
    }

    private OperationResult SaveLocalSnapshot()
    {
        if (!ready || deleting || repository == null)
            return OperationResult.Error(FailureKind.Rejected, "Player data is not ready for local saving.");
        try
        {
            Capture();
            return new OperationResult();
        }
        catch (Exception e)
        {
            LastSaveResult = OperationResult.Error(FailureKind.Unknown, "Local snapshot failed: " + e.GetType().Name);
            BackendResponse.Log("LocalSave", LastSaveResult);
            return LastSaveResult;
        }
    }

    public void GameDataUpdate()
    {
        if (!ready || deleting) return;
        if (paused || shuttingDown) { SaveLocalSnapshot(); return; }
        _ = SaveAsync();
    }

    public async Task<OperationResult> SaveAsync()
    {
        if (!ready || deleting) return OperationResult.Error(FailureKind.Rejected, "Player data is not ready for saving.");
        OperationResult local = SaveLocalSnapshot();
        if (!local.Succeeded) return local;
        if (paused || shuttingDown)
            return OperationResult.Error(FailureKind.Rejected, "Remote save skipped while the application is inactive.");

        return await saves.Enqueue(pendingJson, async sent =>
        {
            if (paused || shuttingDown)
                return OperationResult.Error(FailureKind.Rejected, "Remote save cancelled before dispatch.");
            try
            {
                LastSaveResult = await repository.Save(GameDataCodec.Deserialize(sent));
                if (paused || shuttingDown) return LastSaveResult;
                if (LastSaveResult.Succeeded)
                {
                    // Capture changes made while the network request was in flight.
                    Capture();
                    if (pendingJson != sent) _ = saves.Enqueue(pendingJson, _ => Task.FromResult(new OperationResult()));
                    else if (File.Exists(LocalPath)) File.Delete(LocalPath);
                }
            }
            catch (Exception e)
            {
                LastSaveResult = OperationResult.Error(FailureKind.Unknown, "Save failed: " + e.GetType().Name);
                BackendResponse.Log("Save", LastSaveResult);
            }
            if (paused || shuttingDown) return LastSaveResult;
            nextSave = Time.unscaledTime + 30;
            try { SaveCompleted?.Invoke(LastSaveResult); }
            catch (Exception e) { Debug.LogException(e); }
            return LastSaveResult;
        });
    }
    private void Update()
    {
        if (ready && !paused && !shuttingDown && !saving && !deleting && Time.unscaledTime >= nextSave)
            GameDataUpdate();
    }

    private void OnApplicationPause(bool isPaused)
    {
        paused = isPaused;
        if (paused)
        {
            saves.Suspend();
            SaveLocalSnapshot();
            return;
        }

        saves.Resume();
        if (ready && !deleting && HasPendingLocalSave) GameDataUpdate();
    }

    private void OnApplicationQuit()
    {
        shuttingDown = true;
        saves.Stop();
        SaveLocalSnapshot();
    }

    public async void DeleteData()
    {
        if (!ready || saving || deleting) return;
        deleting = true;
        try
        {
            var result = await repository.Delete();
            if (!result.Succeeded) return;
            ready = false;
            if (File.Exists(LocalPath)) File.Delete(LocalPath);
            if (File.Exists(LocalPath + ".tmp")) File.Delete(LocalPath + ".tmp");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
        catch (Exception e) { BackendResponse.Log("Delete", OperationResult.Error(FailureKind.Unknown, e.GetType().Name)); }
        finally { deleting = false; }
    }
}
