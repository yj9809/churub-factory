# BACKND startup and persistence refactor

## Scope and compatibility

Existing user changes were inspected before editing. No repository-local or parent `AGENTS.md` was found; the supplied global instructions were followed. BalanceTable, GameDataState, GameDataSchema and their existing balance changes were not edited. Existing script GUIDs, serialized bridge fields, table name, JSON field names and BaseCost compatibility type are retained. New scripts have Unity metadata. Legacy-encoded scripts were rewritten with ASCII-only text compatible with their original encoding; UTF-8 files remain UTF-8 without BOM.

The Title scene already references BackendManager, LoadingManager and InAppUpdate, and Game is enabled in build settings. BackendManager creates its StartupCoordinator at runtime, so no scene migration is required. Its old public login events remain usable. LoadingManager's old StartCoroutine event can no longer bypass startup validation.

## Responsibilities and changed files

| File | Responsibility/change |
| --- | --- |
| `Assets/1. Scripts/Core/ServerOperation.cs` | Explicit results, request timeout/settlement gate, sequential startup pipeline, latest-snapshot save queue; independent of Unity |
| `Assets/1. Scripts/System/BackendService.cs` | SDK initialization, guest login, GPGS2 login, safe response classification/logging; no scenes or UI |
| `Assets/1. Scripts/System/GameDataRepository.cs` | Found/NotFound/Failed lookup, SDK readiness and account guards, guarded creation, row-specific update/delete, explicit schema codec; fake transport and creation journal seams |
| `Assets/1. Scripts/System/StartupCoordinator.cs` | Update gate, ordered startup, retry state/button, data readiness, scene preparation and activation |
| `Assets/1. Scripts/System/BackendManager.cs` | Existing serialized/UnityEvent bridge only |
| `Assets/1. Scripts/System/LoadingManager.cs` | Step targets, status text, unscaled interpolation bounded by target; no scene loading |
| `Assets/1. Scripts/System/InAppUpdate.cs` | Update check result, duplicate check prevention, timeout, failed/cancelled update blocks startup |
| `Assets/1. Scripts/System/DataManager.cs` | Runtime data application, local pending snapshot, queued saves, periodic/resume synchronization, synchronous pause/quit local capture and result/event |
| `Assets/1. Scripts/System/GameManager.cs` | Removes duplicate quit save; DataManager owns lifecycle persistence |
| `Assets/1. Scripts/System/UIManager.cs` | Saves after committed upgrades; existing free employee claim save retained |
| `Assets/1. Scripts/Guide/UnlockManager.cs` | Saves after committed facility purchases |
| `Tests/BackendRefactorValidation.cs`, `Tests/ValidateBackend.ps1` | Reproducible managed compilation and standalone fake-server/Core tests |

## SDK 5.14.1 evidence

The installed `Assets/TheBackend/Plugins/Backend.dll` reports assembly version **5.14.1.0**. Both managed compilation paths reference this DLL; SDK/plugin files were not updated.

Official documentation used:

- [5.14.1 GPGS2 flow](https://docs.backnd.com/sdk-docs/backend/5.14.1/base/user/federation/example-using-gpgs2/): Play Games authentication → server auth code → GetGPGS2AccessToken → AuthorizeFederation with **FederationType.GPGS2**. No Toolkit login or credential logging remains.
- [5.14.1 update history/API changes](https://docs.backnd.com/sdk-docs/backend/5.14.1/base/update-history/): InitializeAsync callback API and removal of AsyncPoll. SDK APIs are invoked on Unity's main thread; Task wrappers await callbacks, without Task.Run or blocking Wait/Result.
- [5.14.1 UpdateV2](https://docs.backnd.com/sdk-docs/backend/5.14.1/base/game-information/update/using-indate): save uses the confirmed row inDate and authenticated owner inDate, with callback result checking.

**Account compatibility risk:** GPGS2 identity is incompatible with prior Toolkit Sign in with Google/GPGS1 identity according to the official documentation. Existing Toolkit-created users may appear as different accounts under GPGS2. No identity migration or automatic linking is attempted. Existing-player account migration must be resolved before releasing this login change to them.

## Ordered startup and progress

Update check → initialize → login → successful lookup → create only on valid empty rows → decode and BalanceTable.Synchronize → replay this account's pending local save → prepare Game at activation-disabled state → display completion → activate.

The targets are 0, 20, 45, 75 and 90 percent after the corresponding preceding work succeeds. They are stage indicators, not network byte progress. The final 100 percent target is set only after data application and scene preparation reach completion. Display uses unscaled time, remains at or below its target, and activation waits for the display to reach 100 percent.

SDK/Google request timeout: 30 seconds. App update check timeout: 120 seconds. Scene preparation: 60 seconds. Final display: 10 seconds. Duplicate Begin/Retry calls are ignored while startup is active or complete. The existing status panel displays progress; an immediate-mode Retry button provides a fallback without requiring scene rewiring. Optional LoadingManager.statusText can be bound to a dedicated label later.

## Request failure policy

| Request | Failure handling |
| --- | --- |
| InitializeAsync | Checks IsSuccess; failed initialization cannot reach login |
| GuestLogin / GPGS authorization | Each step must succeed; no query or scene entry on failure; explicit Google retry uses ManuallyAuthenticate |
| GetGPGS2AccessToken | Checks response and nonempty access_token; no federation request on failed/malformed result |
| GetMyData | Only a valid successful empty array is NotFound; transport errors, malformed rows and multiple rows are Failed |
| Insert | Persists account-specific ambiguity marker before dispatch; rereads authoritative data afterward; timeout/lost response never causes blind repeat |
| UpdateV2 | Returns explicit result; no broad Where fallback; local snapshot survives any unsuccessful result |
| DeleteV2 | Checks result and only exits on confirmed success; shares repository gate with updates |

Error records contain operation, status, code and message; raw response/auth code/token are not logged. 401 is authentication failure; missing/408/429/5xx status is treated as network failure; other server rejections are not automatically retried. Timeout and Busy are separately reported. Authentication recovery during gameplay requires a new login session; failed snapshots remain local.

Timeout expires the caller **without pretending the SDK request was cancelled**. Until its callback settles, another physical request through that gate is blocked. A late callback only settles the transport lock, not the expired result or current startup stage. A duplicate old callback cannot unlock a newer request. If the SDK never calls back, restarting the application is required.

An unresolved insert marker survives process restart. Retry first rereads: Found clears the marker; still-empty data keeps creation blocked for manual reconciliation. Do not remove the marker without checking the server. This favors avoiding duplicate rows over automatic recovery when creation is uncertain. Cross-device simultaneous creation cannot be made exactly-once by this client; server uniqueness/idempotency is outside the unchanged schema scope.

## Save and recovery policy

DataManager captures registered runtime objects and writes an account-specific `.pending.json` file under Application.persistentDataPath before queuing the server save. Same-directory temporary write and replacement preserve the prior snapshot during an interrupted write. A leftover temporary file is recovered when no primary snapshot exists. Malformed local data blocks startup rather than silently discarding it.

Concurrent save calls share one worker and coalesce to the latest snapshot. After a successful response, current runtime state is captured again; changes during the request are queued for another update. The pending file is removed only when the latest state has a confirmed successful server write. SaveAsync returns results; LastSaveResult and SaveCompleted expose status to callers/UI.

Server saves run after committed upgrades/facility purchases, existing explicit saves, every 30 seconds, and after returning from the background with a pending snapshot. Pause and quit synchronously capture the latest state to the local pending file and suspend or stop queued transport dispatches. They do not start a new BACKND request while the application is inactive. An already in-flight request may settle, but it cannot trigger a follow-up request or delete the newer local snapshot while paused or shutting down. On resume or the next authenticated startup, pending data for that account is restored, synchronized and retried.

Remaining limits: local files are not tamper-proof or encrypted; OS termination before capture and disk failures cannot guarantee durability. The small local write still uses the main thread and needs device profiling. There is no server revision/conflict field; this implementation favors a pending local snapshot and can overwrite changes from another device. Multi-device conflict resolution requires a separate design. Data deletion ambiguity also needs server reconciliation if a delete response is lost.

## Validation

Run from PowerShell with the installed Unity-generated csproj files and .NET 10 SDK:

```powershell
./Tests/ValidateBackend.ps1
dotnet build Assembly-CSharp.csproj --no-restore -v:q /p:CustomAfterMicrosoftCommonTargets=C:/Workspace/backend-refactor-artifacts/validation.targets /p:ValidateAndroid=true
```

The temporary targets include new files and the already-present user BalanceTable source missing from stale generated project files; the repository csproj files are not rewritten. Logs go to `C:/Workspace/backend-refactor-artifacts` by default.

Managed validation passed **73 checks: 52 scenario assertions and 21 existing NUnit Core test methods**. Coverage includes existing-user lookup, new-user creation/reread, failed lookup not inserting, malformed/duplicate data, failed saves, unresolved insert after restart, late callbacks, concurrent SDK calls, SDK readiness guards, duplicate startup, each startup step failing, retry, stage progress, save coalescing, pause/quit dispatch blocking, staged facility unlock progression, and schema roundtrip under en-US/ko-KR/fr-FR. This is not a Unity Test Runner execution. Final Editor compilation and Android-conditional compilation both completed with 0 errors; pre-existing obsolete-API and generated-project duplicate-include warnings remain.

Unity 6000.3.23f1 EditMode execution was attempted and stopped before tests: the batch process could not obtain a valid Editor license (exit 198). Managed Editor and Android-conditional compilation are available as fallback, but do not validate Unity native behavior, Android packaging, IL2CPP stripping or live BACKND authentication.

## Actual Android/device checklist — not executed

- Internal Play track install: update unavailable, available, cancelled, offline and restart after installation.
- Guest and GPGS2 login with configured web client ID, app signing certificates and BACKND console settings; compare existing Toolkit identity before release.
- Existing row and genuinely new account: exactly one row, stable owner/inDate, correct balance and serialized values.
- Offline/invalid credentials at every startup stage: no Game activation; progress remains bounded; Retry works after transport settlement.
- Disconnect immediately after Insert dispatch; restore network/restart; inspect server rows and retained ambiguity marker.
- Slow UpdateV2 plus repeated upgrades: single active transport and final server state equals latest local state.
- Home button, screen lock, process kill after purchase, airplane mode, resume and next launch: local file persists and replay succeeds for the same account only.
- Verify File.Replace behavior and storage errors on target Android/IL2CPP runtime; check no visible disk-write stalls.
- Loading at timeScale zero, actual status text/bar, Game Awake data access, scene load timeout/retry, and touch usability of the Retry button.
- Two devices editing the same account: document the current local-wins conflict limitation; no cross-device correctness is claimed.

Growth review: the reusable verification script captures the SDK-version and callback-settlement checks from this work. No global preferences/skills were modified; the license failure is recorded as this execution environment's observation, not a general claim about the user's license.
