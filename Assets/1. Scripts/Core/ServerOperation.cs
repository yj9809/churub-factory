using System;
using System.Threading.Tasks;

namespace Churub.Core
{
    public enum FailureKind { None, Network, Authentication, Rejected, InvalidData, Timeout, Busy, Unknown }

    public sealed class OperationResult
    {
        public bool Succeeded => Kind == FailureKind.None;
        public FailureKind Kind { get; }
        public string Status { get; }
        public string Code { get; }
        public string Message { get; }
        public bool Retryable => Kind == FailureKind.Network || Kind == FailureKind.Timeout || Kind == FailureKind.Busy;
        public OperationResult(FailureKind kind = FailureKind.None, string status = "", string code = "", string message = "")
        { Kind = kind; Status = status; Code = code; Message = message; }
        public override string ToString() => $"{Kind}: status={Status}, code={Code}, message={Message}";
        public static OperationResult Error(FailureKind kind, string message) => new OperationResult(kind, message: message);
    }

    // A timeout expires the caller, not the SDK request. Never overlap an unresolved request.
    // Start and callbacks run on Unity's main thread; no SDK call is dispatched via Task.Run.
    public sealed class RequestGate
    {
        // If the SDK callback never arrives at all, the gate must not stay Busy for the rest of
        // the process. A late callback after this deadline only closes over stale settled/expired
        // flags and is ignored; it cannot unlock a newer request either way.
        private static readonly TimeSpan GraceAfterTimeout = TimeSpan.FromSeconds(60);
        private bool pending;
        private DateTime graceDeadline;
        public bool Pending
        {
            get
            {
                if (pending && DateTime.UtcNow >= graceDeadline) pending = false;
                return pending;
            }
        }
        public async Task<OperationResult> Run(Action<Action<OperationResult>> start, int timeoutMs = 30000)
        {
            if (Pending) return OperationResult.Error(FailureKind.Busy, "Previous request is still settling.");
            pending = true;
            graceDeadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeoutMs) + GraceAfterTimeout;
            var completion = new TaskCompletionSource<OperationResult>();
            bool expired = false;
            bool settled = false;
            try
            {
                start(result =>
                {
                    if (settled) return;
                    settled = true;
                    pending = false;
                    if (!expired) completion.TrySetResult(result);
                });
                if (await Task.WhenAny(completion.Task, Task.Delay(timeoutMs)) != completion.Task)
                {
                    expired = true;
                    return OperationResult.Error(FailureKind.Timeout, "Request timed out; awaiting transport settlement before retry.");
                }
                return await completion.Task;
            }
            catch (Exception e)
            {
                settled = true;
                pending = false;
                expired = true;
                return OperationResult.Error(FailureKind.Unknown, e.GetType().Name);
            }
        }
    }

    public sealed class SnapshotSaveQueue
    {
        private string latest;
        private Task<OperationResult> worker;
        private bool suspended;
        private bool stopped;
        public bool Running => worker != null && !worker.IsCompleted;
        public bool Suspended => suspended;
        public bool Stopped => stopped;

        public void Suspend() => suspended = true;
        public void Resume() { if (!stopped) suspended = false; }
        public void Stop()
        {
            stopped = true;
            suspended = true;
            latest = null;
        }

        public Task<OperationResult> Enqueue(string snapshot, Func<string, Task<OperationResult>> send)
        {
            if (stopped)
                return Task.FromResult(OperationResult.Error(FailureKind.Rejected, "Save queue is stopped."));
            if (suspended)
                return Task.FromResult(OperationResult.Error(FailureKind.Rejected, "Save queue is suspended."));
            latest = snapshot;
            if (!Running) worker = Drain(send);
            return worker;
        }
        private async Task<OperationResult> Drain(Func<string, Task<OperationResult>> send)
        {
            // Publish worker before invoking callbacks, even for synchronous fake transports.
            await Task.Yield();
            while (true)
            {
                if (stopped || suspended)
                    return OperationResult.Error(FailureKind.Rejected, "Save dispatch was cancelled before transport started.");
                string sent = latest;
                var result = await send(sent);
                if (stopped || suspended || !result.Succeeded || latest == sent) return result;
            }
        }
    }

    public interface IStartupSteps
    {
        Task<OperationResult> Initialize();
        Task<OperationResult> Login();
        Task<OperationResult> LoadData();
        Task<OperationResult> ApplyData();
        Task<OperationResult> PrepareScene();
        Task<OperationResult> ActivateScene();
    }

    public sealed class StartupPipeline
    {
        public bool Running { get; private set; }
        public bool Completed { get; private set; }
        public async Task<OperationResult> Run(IStartupSteps steps, Action<float, string> progress)
        {
            if (Running || Completed) return OperationResult.Error(FailureKind.Busy, "Startup already running or complete.");
            Running = true;
            try
            {
                progress(0f, "Initializing server");
                var result = await steps.Initialize(); if (!result.Succeeded) return result;
                progress(.2f, "Signing in");
                result = await steps.Login(); if (!result.Succeeded) return result;
                progress(.45f, "Loading player data");
                result = await steps.LoadData(); if (!result.Succeeded) return result;
                progress(.75f, "Applying player data");
                result = await steps.ApplyData(); if (!result.Succeeded) return result;
                progress(.9f, "Preparing game scene");
                result = await steps.PrepareScene(); if (!result.Succeeded) return result;
                result = await steps.ActivateScene(); if (!result.Succeeded) return result;
                Completed = true;
                return result;
            }
            catch (Exception e) { return OperationResult.Error(FailureKind.Unknown, e.GetType().Name); }
            finally { Running = false; }
        }
    }
}
