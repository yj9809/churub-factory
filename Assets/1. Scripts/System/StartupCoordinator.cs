using System;
using System.Threading.Tasks;
using Churub.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartupCoordinator : MonoBehaviour, IStartupSteps
{
    private readonly StartupPipeline pipeline = new StartupPipeline();
    private IBackendService service;
    private GameDataRepository repository;
    private DataLoadResult loaded;
    private LoadingManager view;
    private Image errorImage;
    private InAppUpdate updater;
    private AsyncOperation scene;
    private bool google, running, destroyed;
    public OperationResult LastResult { get; private set; }
    public void Configure(LoadingManager loading, Image error, InAppUpdate update)
    { view = loading; errorImage = error; updater = update; service = new BackendService(); }

    public async void Begin(bool useGoogle)
    {
        if (running || pipeline.Completed) return;
        running = true;
        google = useGoogle;
        try
        {
            if (view == null) view = FindObjectOfType<LoadingManager>();
            if (view == null) throw new InvalidOperationException("Missing loading view.");
            if (service == null) service = new BackendService();
            if (errorImage != null) errorImage.gameObject.SetActive(false);
            LastResult = null;
            Show(0, "Checking app update");
            LastResult = updater == null ? OperationResult.Error(FailureKind.Rejected, "Missing update checker.") : await updater.CheckAsync();
            if (destroyed || !LastResult.Succeeded) return;
            LastResult = await pipeline.Run(this, Show);
        }
        catch (Exception e) { LastResult = OperationResult.Error(FailureKind.Unknown, e.Message); }
        finally
        {
            running = false;
            if (!destroyed && LastResult != null && !LastResult.Succeeded)
            {
                BackendResponse.Log("Startup", LastResult);
                if (errorImage != null) errorImage.gameObject.SetActive(true);
                if (updater != null) updater.LogMessage("Startup failed: " + LastResult.Kind + ". Retry when ready.");
            }
        }
    }
    public void Retry() => Begin(google);
    // A player who declines or cannot complete Google sign-in previously had no way into the
    // game at all: Retry() always reused the failed google flag. Offer guest as an explicit
    // fallback after a Google attempt fails.
    public void RetryAsGuest() => Begin(false);
    private void Show(float amount, string message)
    {
        if (destroyed) throw new OperationCanceledException();
        view.Show(amount, message);
        if (updater != null) updater.LogMessage(message);
    }
    public Task<OperationResult> Initialize() => service.Initialize();
    public Task<OperationResult> Login() => service.Login(google);
    public async Task<OperationResult> LoadData()
    {
        if (destroyed) return OperationResult.Error(FailureKind.Rejected, "Startup cancelled.");
        if (repository == null) repository = new GameDataRepository(service.UserId);
        loaded = await repository.Get();
        if (destroyed) return OperationResult.Error(FailureKind.Rejected, "Startup cancelled.");
        if (loaded.State == DataLookup.NotFound) loaded = await repository.Create();
        return loaded.State == DataLookup.Found ? loaded.Result :
            loaded.State == DataLookup.Failed ? loaded.Result : OperationResult.Error(FailureKind.Network, "Created row is not visible yet. Retry lookup.");
    }
    public async Task<OperationResult> ApplyData()
    {
        if (destroyed) return OperationResult.Error(FailureKind.Rejected, "Startup cancelled.");
        var data = DataManager.Instance;
        var result = data.Apply(repository, loaded.Data);
        if (!result.Succeeded) return result;
        // Replay a failed save before entering gameplay, after verifying this account's server row.
        return data.HasPendingLocalSave ? await data.SaveAsync() : result;
    }

    public async Task<OperationResult> PrepareScene()
    {
        if (destroyed || DataManager.Instance.baseCost == null) return OperationResult.Error(FailureKind.InvalidData, "Player data not ready.");
        GameManager.Instance.sceneName = "Game";
        if (scene == null)
        {
            scene = SceneManager.LoadSceneAsync("Game");
            if (scene == null) return OperationResult.Error(FailureKind.InvalidData, "Game scene missing from build.");
            scene.allowSceneActivation = false;
        }
        float deadline = Time.realtimeSinceStartup + 60;
        while (scene.progress < .9f)
        {
            if (destroyed || Time.realtimeSinceStartup >= deadline)
                return OperationResult.Error(FailureKind.Timeout, "Scene preparation timed out.");
            await Task.Yield();
        }
        return new OperationResult();
    }
    public async Task<OperationResult> ActivateScene()
    {
        if (destroyed || scene == null || scene.progress < .9f || DataManager.Instance.baseCost == null)
            return OperationResult.Error(FailureKind.InvalidData, "Game is not prepared.");
        Show(1, "Ready");
        float deadline = Time.realtimeSinceStartup + 10;
        while (!view.IsComplete)
        {
            if (destroyed) return OperationResult.Error(FailureKind.Rejected, "Startup cancelled.");
            if (Time.realtimeSinceStartup >= deadline)
                return OperationResult.Error(FailureKind.Timeout, "Loading view did not finish displaying progress.");
            await Task.Yield();
        }
        scene.allowSceneActivation = true;
        return new OperationResult();
    }
    private void OnGUI()
    {
        if (!running && !pipeline.Completed && LastResult != null && !LastResult.Succeeded)
        {
            GUI.Box(new Rect(20, 20, 600, 130), "Startup stopped: " + LastResult.Kind + "\n" + LastResult.Message);
            if (GUI.Button(new Rect(40, 105, 180, 35), "Retry")) Retry();
            if (google && GUI.Button(new Rect(240, 105, 180, 35), "Guest Login")) RetryAsGuest();
        }
    }
    private void OnDestroy() { destroyed = true; }
}
