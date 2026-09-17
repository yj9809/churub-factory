using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using BackEnd;
using Churub.Core;
using LitJson;
using UnityEngine;

public enum DataLookup { Found, NotFound, Failed }
public sealed class DataLoadResult
{
    public DataLookup State;
    public BaseCost Data;
    public OperationResult Result;
}

public sealed class DataResponse
{
    public OperationResult Result;
    public JsonData Rows;
}
public interface IGameDataTransport
{
    string GuestId { get; }
    void Get(Action<DataResponse> done);
    void Insert(Param data, Action<DataResponse> done);
    void Update(string row, Param data, Action<DataResponse> done);
    void Delete(string row, Action<DataResponse> done);
}
public interface ICreationJournal { bool Pending { get; set; } }
public sealed class PlayerPrefsCreationJournal : ICreationJournal
{
    private readonly string key;
    public PlayerPrefsCreationJournal(string owner) { key = "BACKND.InsertPending." + owner; }
    public bool Pending
    {
        get => PlayerPrefs.HasKey(key);
        set { if (value) PlayerPrefs.SetInt(key, 1); else PlayerPrefs.DeleteKey(key); PlayerPrefs.Save(); }
    }
}
public sealed class BackendGameDataTransport : IGameDataTransport
{
    private readonly string owner;
    public BackendGameDataTransport(string owner) { this.owner = owner; }
    public string GuestId => Backend.BMember.GetGuestID();
    public void Get(Action<DataResponse> done)
    {
        if (!CanRequest(done)) return;
        Backend.GameData.GetMyData(GameDataSchema.TableName, new Where(), bro => Return(bro, done, true));
    }
    public void Insert(Param data, Action<DataResponse> done)
    {
        if (!CanRequest(done)) return;
        Backend.GameData.Insert(GameDataSchema.TableName, data, bro => Return(bro, done));
    }
    public void Update(string row, Param data, Action<DataResponse> done)
    {
        if (!CanRequest(done)) return;
        Backend.GameData.UpdateV2(GameDataSchema.TableName, row, owner, data, bro => Return(bro, done));
    }
    public void Delete(string row, Action<DataResponse> done)
    {
        if (!CanRequest(done)) return;
        Backend.GameData.DeleteV2(GameDataSchema.TableName, row, owner, bro => Return(bro, done));
    }
    private bool CanRequest(Action<DataResponse> done)
    {
        OperationResult failure = null;
        try
        {
            if (!Backend.IsInitialized)
                failure = OperationResult.Error(FailureKind.Rejected, "BACKND SDK is not initialized.");
            else if (!Backend.IsLogin || string.IsNullOrEmpty(Backend.UserInDate))
                failure = OperationResult.Error(FailureKind.Authentication, "BACKND session is not authenticated.");
            else if (Backend.UserInDate != owner)
                failure = OperationResult.Error(FailureKind.Authentication, "BACKND session owner changed.");
        }
        catch (Exception e)
        {
            failure = OperationResult.Error(FailureKind.Unknown, "BACKND readiness check failed: " + e.GetType().Name);
        }

        if (failure == null) return true;
        done(new DataResponse { Result = failure });
        return false;
    }
    private static void Return(BackendReturnObject bro, Action<DataResponse> done, bool rows = false)
    {
        var result = BackendResponse.Convert(bro);
        JsonData data = null;
        if (result.Succeeded && rows)
        {
            try { data = bro.FlattenRows(); }
            catch { result = OperationResult.Error(FailureKind.InvalidData, "Invalid server rows."); }
        }
        done(new DataResponse { Result = result, Rows = data });
    }
}

public sealed class GameDataRepository
{
    private readonly RequestGate gate = new RequestGate();
    private readonly string owner;
    private readonly IGameDataTransport transport;
    private readonly ICreationJournal journal;
    private readonly int timeoutMs;
    private string row;
    private bool confirmedMissing;
    public string Owner => owner;

    public GameDataRepository(string owner, IGameDataTransport transport = null, ICreationJournal journal = null, int timeoutMs = 30000)
    {
        if (string.IsNullOrEmpty(owner)) throw new ArgumentException("Authenticated owner is required.");
        this.owner = owner;
        this.timeoutMs = timeoutMs;
        this.transport = transport ?? new BackendGameDataTransport(owner);
        this.journal = journal ?? new PlayerPrefsCreationJournal(owner);
    }

    public async Task<DataLoadResult> Get()
    {
        confirmedMissing = false;
        DataResponse response = null;
        var result = await Request("GetMyData", transport.Get, bro => response = bro);
        if (!result.Succeeded) return new DataLoadResult { State = DataLookup.Failed, Result = result };
        try
        {
            var rows = response.Rows;
            if (rows == null || !rows.IsArray) throw new FormatException("Expected a rows array.");
            if (rows.Count == 0)
            {
                confirmedMissing = true;
                return new DataLoadResult { State = DataLookup.NotFound, Result = result };
            }
            if (rows.Count != 1) throw new InvalidOperationException("Multiple player rows require reconciliation.");
            var data = GameDataCodec.Read(rows[0]);
            row = rows[0]["inDate"].ToString();
            if (string.IsNullOrEmpty(row)) throw new InvalidOperationException("Missing row ID.");
            journal.Pending = false;
            return new DataLoadResult { State = DataLookup.Found, Data = data, Result = result };
        }
        catch (Exception e)
        {
            result = OperationResult.Error(FailureKind.InvalidData, e.Message);
            BackendResponse.Log("Deserialize", result);
            return new DataLoadResult { State = DataLookup.Failed, Result = result };
        }
    }

    public async Task<DataLoadResult> Create()
    {
        if (!confirmedMissing || gate.Pending)
            return Failed("Creation requires a successful empty lookup and settled transport.");
        // Persist BEFORE dispatch. A timeout, crash or lost response must never trigger blind insert.
        if (journal.Pending)
        {
            // An earlier session's insert never confirmed. Re-verify emptiness right now rather
            // than refusing forever; the fresh lookup is the authoritative signal, not the stale
            // local marker. Only proceed if the row is still confirmed missing.
            var recheck = await Get();
            if (recheck.State != DataLookup.NotFound)
                return recheck.State == DataLookup.Found
                    ? recheck
                    : Failed("Retry lookup failed while reconciling an unresolved creation.");
        }
        confirmedMissing = false;
        var initial = new BaseCost { guestID = transport.GuestId };
        var param = GameDataCodec.Param(initial, true);
        journal.Pending = true;
        var result = await Request("Insert", cb => transport.Insert(param, cb));
        if (!result.Succeeded && result.Kind != FailureKind.Network && result.Kind != FailureKind.Timeout && result.Kind != FailureKind.Unknown)
        {
            journal.Pending = false;
            return new DataLoadResult { State = DataLookup.Failed, Result = result };
        }
        // Always read the authoritative row; Get refuses overlap while timed-out insert is pending.
        return await Get();
    }

    public Task<OperationResult> Save(BaseCost snapshot)
    {
        if (string.IsNullOrEmpty(row)) return Task.FromResult(OperationResult.Error(FailureKind.Rejected, "No confirmed player row."));
        var param = GameDataCodec.Param(snapshot, false);
        return Request("UpdateV2", cb => transport.Update(row, param, cb));
    }

    public Task<OperationResult> Delete()
    {
        if (string.IsNullOrEmpty(row)) return Task.FromResult(OperationResult.Error(FailureKind.Rejected, "No confirmed player row."));
        return Request("DeleteV2", cb => transport.Delete(row, cb));
    }

    private static DataLoadResult Failed(string message) => new DataLoadResult
    { State = DataLookup.Failed, Result = OperationResult.Error(FailureKind.Rejected, message) };

    private async Task<OperationResult> Request(string name, Action<Action<DataResponse>> start, Action<DataResponse> accept = null)
    {
        DataResponse response = null;
        bool received = false;
        var result = await gate.Run(done => start(bro =>
        {
            if (received) return;
            received = true;
            response = bro;
            done(bro == null ? OperationResult.Error(FailureKind.Network, "No response.") : bro.Result);
        }), timeoutMs);
        if (result.Succeeded) accept?.Invoke(response);
        else BackendResponse.Log(name, result);
        return result;
    }
}

// Explicit schema mapping avoids serializing GameDataState's convenience properties.
public static class GameDataCodec
{
    public static Dictionary<string, object> Fields(BaseCost data, bool guest)
    {
        var fields = new Dictionary<string, object>
        {
            [GameDataSchema.Fields.UpgradeCosts] = data.upgradeCosts,
            [GameDataSchema.Fields.PlayerData] = data.playerData,
            [GameDataSchema.Fields.EmployeeList] = data.employeeList,
            [GameDataSchema.Fields.EmployeeData] = data.employeeData,
            [GameDataSchema.Fields.ObjectData] = data.objectData,
            [GameDataSchema.Fields.GameProgress] = data.gameProgressBool,
            [GameDataSchema.Fields.GuideStep] = data.guideStep,
            [GameDataSchema.Fields.NewGame] = data.newGame,
            [GameDataSchema.Fields.SaveRevision] = data.saveRevision
        };
        if (guest) fields[GameDataSchema.Fields.GuestId] = data.guestID ?? "";
        return fields;
    }
    public static Param Param(BaseCost data, bool guest)
    {
        var param = new Param();
        foreach (var field in Fields(data, guest)) param.Add(field.Key, field.Value);
        return param;
    }
    public static string Serialize(BaseCost data) => JsonMapper.ToJson(Fields(data, true));
    public static BaseCost Deserialize(string json) => Read(JsonMapper.ToObject(json));
    // Missing top-level fields or map entries fall back to BaseCost's own constructor
    // defaults instead of throwing, so a save written before a field existed still loads.
    // A row missing every field present since the first schema version is not a legacy save,
    // it is empty/malformed data and must still fail rather than silently become a fresh state.
    public static BaseCost Read(JsonData json)
    {
        if (json == null || !json.IsObject ||
            (!json.Keys.Contains(GameDataSchema.Fields.GuideStep) && !json.Keys.Contains(GameDataSchema.Fields.NewGame)))
            throw new FormatException("Row is missing core save fields.");
        var data = new BaseCost();
        data.guestID = ReadString(json, GameDataSchema.Fields.GuestId, data.guestID);
        data.guideStep = ReadInt(json, GameDataSchema.Fields.GuideStep, data.guideStep);
        data.newGame = ReadBool(json, GameDataSchema.Fields.NewGame, data.newGame);
        data.saveRevision = ReadInt(json, GameDataSchema.Fields.SaveRevision, data.saveRevision);
        ReadMap(TryGet(json, GameDataSchema.Fields.UpgradeCosts), data.upgradeCosts, value => int.Parse(value, CultureInfo.InvariantCulture));
        ReadMap(TryGet(json, GameDataSchema.Fields.PlayerData), data.playerData, ReadFloat);
        ReadMap(TryGet(json, GameDataSchema.Fields.EmployeeData), data.employeeData, ReadFloat);
        ReadMap(TryGet(json, GameDataSchema.Fields.ObjectData), data.objectData, value => int.Parse(value, CultureInfo.InvariantCulture));
        ReadMap(TryGet(json, GameDataSchema.Fields.GameProgress), data.gameProgressBool, bool.Parse);
        var employeeList = TryGet(json, GameDataSchema.Fields.EmployeeList);
        if (employeeList != null && employeeList.IsArray)
            foreach (JsonData name in employeeList) data.employeeList.Add(name.ToString());
        return data;
    }
    private static float ReadFloat(string value)
    {
        float number = float.Parse(value, CultureInfo.InvariantCulture);
        if (float.IsNaN(number) || float.IsInfinity(number)) throw new FormatException("Non-finite player value.");
        return number;
    }
    private static JsonData TryGet(JsonData json, string key)
    {
        // LitJson returns a plain C# null for a JSON null value, so no separate null check
        // of the returned JsonData is needed here.
        if (json == null || !json.IsObject || !json.Keys.Contains(key)) return null;
        return json[key];
    }
    private static string ReadString(JsonData json, string key, string fallback)
    {
        var value = TryGet(json, key);
        return value == null ? fallback : value.ToString();
    }
    private static int ReadInt(JsonData json, string key, int fallback)
    {
        var value = TryGet(json, key);
        return value != null && int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int number) ? number : fallback;
    }
    private static bool ReadBool(JsonData json, string key, bool fallback)
    {
        var value = TryGet(json, key);
        return value != null && bool.TryParse(value.ToString(), out bool result) ? result : fallback;
    }
    private static void ReadMap<T>(JsonData json, Dictionary<string, T> target, Func<string, T> convert)
    {
        if (json == null || !json.IsObject) return;
        foreach (string key in json.Keys)
        {
            var value = json[key];
            // Skip entries that fail to parse (legacy/foreign data) rather than aborting the
            // whole load; the constructor default for that key is kept.
            try { target[key] = convert(value.IsDouble || value.IsInt || value.IsLong ? value.ToJson() : value.ToString()); }
            catch (FormatException) { }
        }
    }
}
