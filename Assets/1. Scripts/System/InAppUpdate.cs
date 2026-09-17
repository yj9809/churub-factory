using System;
using System.Threading.Tasks;
using UnityEngine;
using Google.Play.AppUpdate;
using TMPro;
using Churub.Core;

public class InAppUpdate : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private GameObject textPanel;
    [Header("Manager")]
    [SerializeField] private BackendManager backendManager;
    private Task<OperationResult> activeCheck;
    private bool checkedUpdate;

    private void Start()
    {
        if (textPanel != null) textPanel.SetActive(false);
        if (backendManager == null) backendManager = FindObjectOfType<BackendManager>();
        if (backendManager == null) { LogMessage("Missing startup bridge."); return; }
#if UNITY_ANDROID && !UNITY_EDITOR
        backendManager.StartGoogleLogin();
#else
        backendManager.GuestLogin();
#endif
    }

    public async Task<OperationResult> CheckAsync()
    {
        if (checkedUpdate) return new OperationResult();
        if (activeCheck != null && !activeCheck.IsCompleted)
            return OperationResult.Error(FailureKind.Busy, "Update check is still settling.");
        // Only the metadata lookup blocks startup. A flexible update downloads in the
        // background by design; waiting on the full download here caused startup to time out
        // on slow connections even though nothing was actually wrong.
        activeCheck = CheckInfoAsync();
        if (await Task.WhenAny(activeCheck, Task.Delay(30000)) != activeCheck)
            return OperationResult.Error(FailureKind.Timeout, "Update check timed out.");
        var result = await activeCheck;
        checkedUpdate = result.Succeeded;
        return result;
    }

    private async Task<OperationResult> CheckInfoAsync()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            LogMessage("Checking app update");
            var manager = new AppUpdateManager();
            var info = manager.GetAppUpdateInfo();
            while (!info.IsDone) await Task.Yield();
            if (!info.IsSuccessful) return OperationResult.Error(FailureKind.Network, "Update info: " + info.Error);
            var value = info.GetResult();
            if (value.UpdateAvailability == UpdateAvailability.UpdateAvailable)
                _ = RunFlexibleUpdate(manager, value);
            return new OperationResult();
        }
        catch (Exception e) { return OperationResult.Error(FailureKind.Unknown, "Update check: " + e.GetType().Name); }
#else
        await Task.CompletedTask;
        return new OperationResult();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    // Fire-and-forget: downloads and installs without blocking the startup pipeline.
    private async Task RunFlexibleUpdate(AppUpdateManager manager, AppUpdateInfo value)
    {
        try
        {
            var request = manager.StartUpdate(value, AppUpdateOptions.FlexibleAppUpdateOptions());
            while (!request.IsDone) await Task.Yield();
            if (request.Error != AppUpdateErrorCode.NoError) return;
            var complete = manager.CompleteUpdate();
            while (!complete.IsDone) await Task.Yield();
            if (complete.IsSuccessful) LogMessage("Update downloaded. Restart to apply.");
        }
        catch (Exception e) { Debug.LogWarning("Flexible update failed: " + e.GetType().Name); }
    }
#endif
    public void LogMessage(string message)
    {
        if (textPanel != null) textPanel.SetActive(true);
        if (logText != null) logText.text = message;
    }
}