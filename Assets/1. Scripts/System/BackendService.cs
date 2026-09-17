using System;
using System.Threading.Tasks;
using BackEnd;
using Churub.Core;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine;

public interface IBackendService
{
    Task<OperationResult> Initialize();
    Task<OperationResult> Login(bool google);
    string UserId { get; }
}

public sealed class BackendService : IBackendService
{
    private readonly RequestGate gate = new RequestGate();
    private bool initialized;
    private bool authenticated;
    private bool googleAttempted;
    public string UserId => authenticated ? Backend.UserInDate : null;

    public async Task<OperationResult> Initialize()
    {
        if (initialized) return new OperationResult();
        var result = await Call("Initialize", cb => Backend.InitializeAsync(bro => cb(bro)));
        initialized = result.Succeeded;
        return result;
    }

    public async Task<OperationResult> Login(bool google)
    {
        if (!initialized) return OperationResult.Error(FailureKind.Rejected, "SDK is not initialized.");
        if (authenticated) return new OperationResult();
        OperationResult result;
        if (!google)
            result = await Call("GuestLogin", cb => Backend.BMember.GuestLogin("Guest login", bro => cb(bro)));
        else
        {
            PlayGamesPlatform.Activate();
            result = await gate.Run(done =>
            {
                Action<SignInStatus> completed = status => done(status == SignInStatus.Success
                    ? new OperationResult() : OperationResult.Error(FailureKind.Authentication, status.ToString()));
                if (googleAttempted) PlayGamesPlatform.Instance.ManuallyAuthenticate(completed);
                else { googleAttempted = true; PlayGamesPlatform.Instance.Authenticate(completed); }
            });
            if (!result.Succeeded) return result;
            string authCode = null;
            result = await gate.Run(done => PlayGamesPlatform.Instance.RequestServerSideAccess(false, code =>
            {
                authCode = code;
                done(string.IsNullOrEmpty(code) ? OperationResult.Error(FailureKind.Authentication, "Empty GPGS authorization code.") : new OperationResult());
            }));
            if (!result.Succeeded) return result;
            BackendReturnObject tokenResponse = null;
            result = await Call("GetGPGS2AccessToken", cb => Backend.BMember.GetGPGS2AccessToken(authCode, bro => cb(bro)), bro => tokenResponse = bro);
            if (!result.Succeeded) return result;
            string token;
            try { token = tokenResponse.GetReturnValuetoJSON()["access_token"].ToString(); }
            catch { return OperationResult.Error(FailureKind.InvalidData, "Missing GPGS access token."); }
            if (string.IsNullOrEmpty(token)) return OperationResult.Error(FailureKind.InvalidData, "Empty GPGS access token.");
            result = await Call("AuthorizeFederation", cb => Backend.BMember.AuthorizeFederation(token, FederationType.GPGS2, bro => cb(bro)));
        }
        authenticated = result.Succeeded && !string.IsNullOrEmpty(Backend.UserInDate);
        return result.Succeeded && !authenticated ? OperationResult.Error(FailureKind.InvalidData, "Missing authenticated user ID.") : result;
    }

    private async Task<OperationResult> Call(string name, Action<Action<BackendReturnObject>> start, Action<BackendReturnObject> accept = null)
    {
        BackendReturnObject response = null;
        bool received = false;
        var result = await gate.Run(done => start(bro =>
        {
            if (received) return;
            received = true;
            response = bro;
            done(BackendResponse.Convert(bro));
        }));
        if (result.Succeeded) accept?.Invoke(response);
        else BackendResponse.Log(name, result);
        return result;
    }
}

public static class BackendResponse
{
    public static Action<string> LogSink = message => Debug.LogWarning(message);
    public static OperationResult Convert(BackendReturnObject response)
    {
        if (response == null) return OperationResult.Error(FailureKind.Network, "No response.");
        if (response.IsSuccess()) return new OperationResult();
        string status = Safe(response.GetStatusCode), code = Safe(response.GetErrorCode), message = Safe(response.GetMessage);
        int.TryParse(status, out int number);
        var kind = number == 401 ? FailureKind.Authentication :
            number == 0 || number == 408 || number == 429 || number >= 500 ? FailureKind.Network : FailureKind.Rejected;
        return new OperationResult(kind, status, code, message);
    }
    private static string Safe(Func<string> read) { try { return read() ?? ""; } catch { return "unavailable"; } }
    public static void Log(string operation, OperationResult result)
    {
        // Never log raw responses, auth codes or tokens.
        LogSink($"BACKND {operation}: {result.ToString().Replace('\n', ' ').Replace('\r', ' ')}");
    }
}
