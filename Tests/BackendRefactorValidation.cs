using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BackEnd;
using Churub.Core;
using LitJson;

// Standalone runner against the compiled game/SDK assemblies. No Unity native calls or network.
internal static class BackendRefactorValidation
{
    private static int passed;
    private static void Check(bool value, string name)
    { if (!value) throw new Exception(name); Console.WriteLine("PASS " + name); passed++; }
    private static OperationResult Ok => new OperationResult();
    private static OperationResult Fail => OperationResult.Error(FailureKind.Network, "Injected network failure");
    public static async Task<int> Main()
    {
        BackendResponse.LogSink = _ => { };
        try
        {
            await Pipelines();
            await Gates();
            await Repositories();
            TransportReadiness();
            await Saves();
            Codec();
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            foreach (var method in type.GetMethods().Where(m => m.GetCustomAttributes().Any(a => a.GetType().Name == "TestAttribute")))
            {
                var instance = Activator.CreateInstance(type);
                method.Invoke(instance, null);
                Check(true, "Existing Core: " + method.Name);
            }
            Console.WriteLine($"TOTAL {passed} passed");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }

    private sealed class Steps : IStartupSteps
    {
        public int Calls, FailAt;
        public TaskCompletionSource<OperationResult> Hold;
        private Task<OperationResult> Step()
        { Calls++; return Hold != null && Calls == 1 ? Hold.Task : Task.FromResult(Calls == FailAt ? Fail : Ok); }
        public Task<OperationResult> Initialize() => Step();
        public Task<OperationResult> Login() => Step();
        public Task<OperationResult> LoadData() => Step();
        public Task<OperationResult> ApplyData() => Step();
        public Task<OperationResult> PrepareScene() => Step();
        public Task<OperationResult> ActivateScene() => Step();
    }
    private static async Task Pipelines()
    {
        for (int failure = 1; failure <= 6; failure++)
        {
            var steps = new Steps { FailAt = failure };
            var pipeline = new StartupPipeline();
            var result = await pipeline.Run(steps, (_, __) => { });
            Check(!result.Succeeded && steps.Calls == failure && !pipeline.Completed, "Startup blocks after failed step " + failure);
            steps.FailAt = 0;
            Check((await pipeline.Run(steps, (_, __) => { })).Succeeded, "Startup retry after failure " + failure);
        }
        var held = new Steps { Hold = new TaskCompletionSource<OperationResult>() };
        var p = new StartupPipeline();
        var progress = new List<float>();
        var first = p.Run(held, (value, _) => progress.Add(value));
        Check(progress.SequenceEqual(new[] { 0f }), "Progress waits for real initialization");
        Check((await p.Run(held, (_, __) => { })).Kind == FailureKind.Busy, "Duplicate startup blocked");
        held.Hold.SetResult(Ok);
        Check((await first).Succeeded && p.Completed && held.Calls == 6, "Successful startup executes all six steps");
        Check(progress.SequenceEqual(new[] { 0f, .2f, .45f, .75f, .9f }), "Stage progress never reports completion before scene readiness");
        Check((await p.Run(held, (_, __) => { })).Kind == FailureKind.Busy, "Completed startup cannot restart");
    }
    private static async Task Gates()
    {
        var gate = new RequestGate();
        Action<OperationResult> callback = null;
        var request = gate.Run(done => callback = done, 20);
        Check((await gate.Run(_ => throw new Exception())).Kind == FailureKind.Busy, "Concurrent SDK request rejected");
        Check((await request).Kind == FailureKind.Timeout && gate.Pending, "Timeout preserves physical request lock");
        callback(Ok);
        Check(!gate.Pending && (await request).Kind == FailureKind.Timeout, "Late callback cannot replace expired result");
        Check((await gate.Run(done => done(Ok))).Succeeded, "Retry allowed after transport settles");
        Check((await gate.Run(_ => throw new InvalidOperationException())).Kind == FailureKind.Unknown && !gate.Pending, "Synchronous request exception releases gate");
        Action<OperationResult> current = null;
        var next = gate.Run(done => current = done);
        callback(Ok);
        Check(gate.Pending, "Duplicate old callback cannot unlock a newer request");
        current(Ok); await next;
    }

    private sealed class Journal : ICreationJournal { public bool Pending { get; set; } }
    private sealed class Transport : IGameDataTransport
    {
        public string GuestId => "test-guest";
        public JsonData Rows = JsonMapper.ToObject("[]");
        public OperationResult GetResult = Ok, WriteResult = Ok;
        public int Inserts, Updates;
        public bool HoldInsert;
        public Action<DataResponse> InsertCallback;
        public void Get(Action<DataResponse> done) => done(new DataResponse { Result = GetResult, Rows = Rows });
        public void Insert(Param data, Action<DataResponse> done)
        {
            Inserts++;
            if (HoldInsert) { InsertCallback = done; return; }
            if (WriteResult.Succeeded) Rows = Row();
            done(new DataResponse { Result = WriteResult });
        }
        public void Update(string row, Param data, Action<DataResponse> done)
        { Updates++; done(new DataResponse { Result = WriteResult }); }
        public void Delete(string row, Action<DataResponse> done) => done(new DataResponse { Result = WriteResult });
    }
    private static JsonData Row()
    {
        var fields = GameDataCodec.Fields(new BaseCost { guestID = "test-guest" }, true);
        fields["inDate"] = "row-1";
        return JsonMapper.ToObject("[" + JsonMapper.ToJson(fields) + "]");
    }
    private static async Task Repositories()
    {
        var transport = new Transport { Rows = Row() };
        var journal = new Journal();
        var repo = new GameDataRepository("owner", transport, journal, 20);
        Check((await repo.Get()).State == DataLookup.Found && transport.Inserts == 0, "Existing user loads without insert");
        transport.WriteResult = Fail;
        Check(!(await repo.Save(new BaseCost())).Succeeded && transport.Updates == 1, "Save failure returned to caller");
        transport.GetResult = Fail;
        Check((await repo.Get()).State == DataLookup.Failed, "Failed lookup is distinct from missing user");
        await repo.Create();
        Check(transport.Inserts == 0, "Failed lookup never inserts");
        transport = new Transport(); journal = new Journal(); repo = new GameDataRepository("owner", transport, journal, 20);
        Check((await repo.Get()).State == DataLookup.NotFound, "Successful empty lookup is NotFound");
        Check((await repo.Create()).State == DataLookup.Found && transport.Inserts == 1 && !journal.Pending, "New user created and reread");
        await repo.Create();
        Check(transport.Inserts == 1, "Repeated create without fresh empty lookup blocked");
        transport = new Transport { HoldInsert = true }; journal = new Journal(); repo = new GameDataRepository("owner", transport, journal, 20);
        await repo.Get();
        Check((await repo.Create()).State == DataLookup.Failed && journal.Pending, "Lost insert response retains durable ambiguity marker");
        await repo.Create();
        Check(transport.Inserts == 1, "Insert timeout cannot blindly insert again");
        transport.Rows = Row();
        transport.InsertCallback(new DataResponse { Result = Ok });
        Check((await repo.Get()).State == DataLookup.Found && !journal.Pending && transport.Inserts == 1, "Requery recovers committed insert after late callback");
        transport = new Transport(); journal = new Journal { Pending = true }; repo = new GameDataRepository("owner", transport, journal, 20);
        await repo.Get(); await repo.Create();
        Check(transport.Inserts == 0 && journal.Pending, "Unresolved creation survives app restart");
        transport.Rows = JsonMapper.ToObject("[{},{}]");
        Check((await repo.Get()).State == DataLookup.Failed, "Duplicate rows fail instead of selecting arbitrary data");
        transport.Rows = JsonMapper.ToObject("[{}]");
        Check((await repo.Get()).State == DataLookup.Failed, "Malformed data blocks game entry");
        transport.Rows = JsonMapper.ToObject("{}");
        Check((await repo.Get()).State == DataLookup.Failed, "Malformed empty object is not a new user");
        var noRow = new GameDataRepository("other-owner", new Transport(), new Journal());
        Check(!(await noRow.Save(new BaseCost())).Succeeded, "Saving without confirmed row is blocked");
        transport = new Transport { WriteResult = OperationResult.Error(FailureKind.Rejected, "Forbidden") };
        journal = new Journal(); repo = new GameDataRepository("owner", transport, journal);
        await repo.Get();
        Check((await repo.Create()).State == DataLookup.Failed && !journal.Pending, "Definitive insert rejection is returned without retaining ambiguity");
    }

    private static void TransportReadiness()
    {
        DataResponse response = null;
        var transport = new BackendGameDataTransport("owner");
        transport.Update("row", new Param(), result => response = result);
        Check(response != null && !response.Result.Succeeded,
            "Transport blocks SDK calls before BACKND initialization and login");
    }

    private static async Task Saves()
    {
        var queue = new SnapshotSaveQueue();
        var firstStarted = new TaskCompletionSource<bool>();
        var completion = new TaskCompletionSource<OperationResult>();
        var sent = new List<string>();
        Func<string, Task<OperationResult>> send = async json =>
        { sent.Add(json); firstStarted.TrySetResult(true); return sent.Count == 1 ? await completion.Task : Ok; };
        var first = queue.Enqueue("one", send);
        await firstStarted.Task;
        var second = queue.Enqueue("two", send);
        var third = queue.Enqueue("three", send);
        Check(ReferenceEquals(first, second) && ReferenceEquals(second, third) && sent.Count == 1, "Concurrent saves share one worker");
        completion.SetResult(Ok);
        Check((await third).Succeeded && sent.SequenceEqual(new[] { "one", "three" }), "Save coalesces additional changes into latest snapshot");
        Check(!(await queue.Enqueue("four", _ => Task.FromResult(Fail))).Succeeded, "Queue surfaces server failure");
        Check((await queue.Enqueue("four", _ => Task.FromResult(Ok))).Succeeded, "Failed save can be retried");

        var suspended = new SnapshotSaveQueue();
        int dispatches = 0;
        suspended.Suspend();
        var cancelled = suspended.Enqueue("local-only", _ =>
        {
            dispatches++;
            return Task.FromResult(Ok);
        });
        Check(!(await cancelled).Succeeded && dispatches == 0, "Suspended queue keeps snapshot local");
        suspended.Resume();
        Check((await suspended.Enqueue("resume", _ =>
        {
            dispatches++;
            return Task.FromResult(Ok);
        })).Succeeded && dispatches == 1, "Resuming allows a fresh server save");

        var stopped = new SnapshotSaveQueue();
        dispatches = 0;
        stopped.Stop();
        var stoppedTask = stopped.Enqueue("quit", _ =>
        {
            dispatches++;
            return Task.FromResult(Ok);
        });
        Check(!(await stoppedTask).Succeeded && dispatches == 0, "Stopped queue prevents quit-time transport calls");
        Check((await stopped.Enqueue("late", _ => Task.FromResult(Ok))).Kind == FailureKind.Rejected,
            "Stopped queue rejects late save requests");

        var inFlight = new SnapshotSaveQueue();
        var transportStarted = new TaskCompletionSource<bool>();
        var transportFinished = new TaskCompletionSource<OperationResult>();
        var calls = new List<string>();
        Func<string, Task<OperationResult>> heldSend = async json =>
        {
            calls.Add(json);
            transportStarted.TrySetResult(true);
            return await transportFinished.Task;
        };
        var heldSave = inFlight.Enqueue("active", heldSend);
        await transportStarted.Task;
        _ = inFlight.Enqueue("newer", heldSend);
        inFlight.Stop();
        transportFinished.SetResult(Ok);
        await heldSave;
        Check(calls.SequenceEqual(new[] { "active" }), "Stopping an in-flight save prevents a follow-up dispatch");
    }
    private static void Codec()
    {
        var data = new BaseCost { guestID = "test", guideStep = 7, newGame = false };
        data.PlayerGold = 1234.5f; data.employeeList.Add("Employee");
        var oldCulture = CultureInfo.CurrentCulture;
        try
        {
            foreach (string culture in new[] { "en-US", "ko-KR", "fr-FR" })
            {
                CultureInfo.CurrentCulture = new CultureInfo(culture);
                var restored = GameDataCodec.Deserialize(GameDataCodec.Serialize(data));
                Check(restored.PlayerGold == data.PlayerGold && restored.employeeList.SequenceEqual(data.employeeList) && restored.guideStep == 7, "Schema roundtrip under " + culture);
            }
        }
        finally { CultureInfo.CurrentCulture = oldCulture; }
    }
}
