# Item transport refactoring

## Implemented stages

1. Added `Item.Type` to the root of the actual Salmon, Churub_Package_Defalt,
   and Churub_Box prefabs. Existing tags and prefab GUIDs are unchanged.
2. Added `CarrierInventory` with a private `Stack<Item>`. It accepts one type
   at a time, checks capacity, rejects null and duplicate entries, and exposes
   Count, IsEmpty, CanAccept, TryAdd, TryPeek, and TryPop.
   Item and inventory live in the auto-referenced `Churub.Items` assembly;
   the Item script GUID was preserved when moving it from Work to Items.

3. Player and Employee now own one CarrierInventory each. Their old three stacks
   and public stack setters are removed. Cart state, employee target selection,
   packaging eligibility, guide checks, and trash disposal use Inventory.
   Storage pickup no longer takes an isChuru boolean: the actual Item supplies
   its type. Employee role/reservation guards and player-only truck loading remain.

4. WorkPoint now references one WorkAction and dispatches Enter/Stay/Exit.
   ItemTransfer, PackagingInteraction, StoreInteraction, and UpgradeInteraction
   implement the respective interactions. WorkPointType and station-specific
   WorkPoint fields are removed. Player/Employee station-specific TakeObject and
   GiveObject methods are also removed.

   IngredientMaker, ConveyorBelt, BoxStorage, BoxPackaging, and Truck implement
   IItemTransferEndpoint. The station selects transfer direction and accepted
   type; ItemTransfer does not branch on ItemType or concrete station classes.
   Employee.CanCollectFrom retains role/reservation checks for IStackable sources.

5. Stations now own private ItemBuffers instead of exposed GameObject stacks.
   CarrierInventory reuses the same rules with no fixed type; station buffers
   specify their accepted type. ItemBuffer.TryMoveTo validates and commits both
   sides synchronously. Item records its runtime buffer owner so the same physical
   object cannot be added to two buffers.

   LegacyCarrierTransfer was replaced by ItemTransferUtility, which delegates data
   changes to buffers and handles presentation after success. Utility.ObjectDrop,
   its CheckType mode enum, and the conveyor's transform-to-stack reconstruction
   were removed. The conveyor now only receives items through its private buffer.

Save keys and formats remain unchanged. Truck uses a restoredBoxCount plus actual
Box items instead of inserting null placeholders. LoadedCount drives capacity,
UI, sale rewards, and saves. Completed packaging Boxes remain owned by a temporary
output buffer until the existing output tween completes; collision collection must
not cancel the callback that resets packaging state. Pool return rejects stored
Items and cancels their tweens after ownership is released.

## Inventory contract

- An empty inventory accepts any ItemType; a non-empty one uses the top item.
- Rejected additions leave contents unchanged. CanAccept only queries state;
  TryAdd checks again and returns whether the addition succeeded.
- Capacity is a non-negative integer. Lowering it preserves carried items and
  blocks additions until Count is below the new capacity.
- The caller must remove items before pooling or destroying them. Inventory
  does not move, parent, destroy, or return objects to the pool.
- Item.Owner is runtime-only and internal to the item assembly. TryAdd accepts
  unowned items; TryMoveTo transfers ownership; TryPop releases it for processing
  or pool return. Failed transfers leave both buffers and ownership unchanged.
- Fixed-type station buffers reject the wrong type even when empty. Carrier
  buffers accept any type when empty and retain the one-type-at-a-time rule.
- Player capacity uses PlayerMaxStackCount + buffMaxObjStackCount; Employee uses
  EmployeeMaxStackCount. Each Inventory access refreshes capacity with
  Max(0, CeilToInt(limit)), preserving `count < limit` for finite gameplay limits.
  Buff expiration reduces the limit but does not delete carried items.

## Deferred Unity verification

Stage 2 static compilation passed for Item, CarrierInventory, and the test source
using the installed Unity 6000.3.23f1 DLLs and NUnit (0 errors; one expected CS0649
warning for the Inspector-assigned Item.type field). This standalone .NET check
does not validate Unity import, assembly loading, or native execution. The three
prefab references were also checked against the preserved Item script GUID.

Stage 3: standalone MSBuild compilation of Assembly-CSharp plus the newly added
inventory/bridge sources passed (0 errors). This includes Player, Employee, Guide,
WorkPoint, and TrashCan; Unity import/runtime testing is still deferred. A source
search found no remaining actor-specific stack accesses or bool storage calls.

Stage 4: standalone game compilation passed (0 errors; existing project warnings
remain). The deferred Unity binding validator also compiled against actual Unity
DLLs with 0 errors. `Tests/ValidateWorkPoints.py` passed: 9 authored action
components expand to 15 WorkPoints in Game.unity, including a third conveyor
instance that inherits its settings without scene overrides. The script checks
component ownership, station references, inherited overrides, and player-only
permissions. These are static checks, not proof of a successful Unity YAML import.

Stage 5: game and inventory/test source compilation passed with 0 errors. Added
7 ItemBuffer test cases (16 total including carrier tests); their native execution
is still pending. The 15 WorkPoint bindings still pass the static validator. No
GameObject stacks or legacy ObjectDrop/LegacyCarrierTransfer calls remain in game
scripts. Existing prefab/scene fields and saved key strings were not migrated in
this stage; the old runtime stacks were not persisted as item instances.

Unity batch compilation previously stopped before compilation due to an Editor
license error. Native Unity tests and play checks remain pending by agreement.

After restoring Editor access:

1. Import the project and confirm no script/assembly compilation errors.
2. Run EditMode tests with filter `CarrierInventoryTests` (9 cases), for example:
   `./Tests/RunUnityTests.ps1 -Filter CarrierInventoryTests`.
   Also run `./Tests/RunUnityTests.ps1 -Filter ItemBufferTests` (7 cases).
3. Inspect the three prefabs: one Item on each root; types Ingredient, Churu,
   and Box respectively; no missing scripts or changed tags.
4. Exercise fresh generation and pooled reuse through ingredient pickup, belt
   conversion, churu storage, packaging, box storage, and truck loading.
5. Load an existing save with waiting churu, partial packaging, stored boxes,
   and partially loaded truck. Confirm counts and Item components on restored
   objects; the truck's restored numeric count must combine with new physical Boxes.
6. Run `Tools > Churub > Validate WorkPoint Bindings` in Unity. This reads three
   machinery prefabs and Game.unity without saving them. For batch use, the method
   is `-executeMethod WorkPointValidation.Validate`. It checks the bindings after
   Unity's actual prefab import, including all 15 active/inactive WorkPoints.

## Checks required when integrating the next stages

- Player and Employee use one inventory without keeping parallel live stacks.
- Preserve employee role/reservation checks and player-only pickup/load actions.
- Preserve buffs, cart visibility/speed, direct collision pickup, trash disposal,
  guide progress, and employee target selection.
- Test full/empty inventories, mixed types, capacity changes, and two actors
  approaching the same source.
- Test transfers interrupted by disable, pooling, or another transfer; ensure no
  stale tween callbacks, duplicate ownership, or item loss.
- Keep packaging completion timing and saved progress separate from ordinary
  transfer timing. Preserve existing save keys and prefab references.

### Stage 3 manual regression cases

- Fill each actor to capacity; another pickup must leave both inventories/stacks
  unchanged. Reject a different type while carrying; accept it after emptying.
- With a fractional limit such as 2.5, accept three items as the old comparison
  did. Test integer limits, zero, upgrades, and buff expiration while loaded.
- Deliver Ingredient to a belt and Churu to packaging; reject the wrong carried
  type at each destination. A full/unavailable truck must leave the carrier intact.
- Confirm employee pickup only occurs for its reserved source and transport role;
  storage Box pickup and truck loading remain player-only WorkPoint actions.
- Pick up a loose Ingredient by collision; verify capacity, stack height, and cart
  state. Dispose of items mid-movement, then reuse them from the pool.
- Verify the first-sale guide sequence, empty-handed packaging, cart speed,
  and existing 0.5-second employee pickup window.

These Stage 3 cases also apply to the subsequent buffer/ownership changes.

## Stage 4 asset migration and follow-up

- Modified Work Point.prefab, IngredientSpawn.prefab, ChuruConveyerBelt Obj.prefab,
  Box Packaging.prefab, and Game.unity. Existing script/prefab GUIDs and existing
  object IDs were retained. Added actions are attached to the corresponding
  nested WorkPoint GameObjects, with one configured action per point.
- Game scene coverage: 3 ingredient pickups, 3 conveyor inputs, 3 churu pickups,
  1 box pickup, 1 packaging input, 1 packaging interaction, 1 truck, 1 office,
  and 1 store. Box pickup and truck transfer are player-only.
- The source assets contained retired WorkPoint component-ID overrides alongside
  current ones. Migration preferred current overrides, recovered IngredientSpawn's
  legacy ingredient reference, and retargeted effective scene references to action
  fields. Unrelated property overrides were checked byte-for-byte for preservation.
- `Tests/MigrateWorkPoints.py` is a one-time migration utility (preview by default,
  `--apply` to write), not a runtime dependency. It fails if already migrated or if
  the expected source structure changes. Pre-migration snapshots and a binding
  report are under `Logs/WorkPointMigration` in this workspace.
- `Tests/ValidateWorkPoints.py` is repeatable and uses the migrated assets directly;
  it does not require those local snapshots.
- The bare Work Point prefab is a template with an unassigned action. When creating
  a new worktable, attach the desired WorkAction to the point and assign it. For
  ItemTransfer, assign a MonoBehaviour implementing IItemTransferEndpoint and set
  the actor restriction. Adding a new action does not require editing WorkPoint.

Deferred behavior checks:

- Enter/stay/exit each UI area; store effects still play on entry and exit. Exiting
  unrelated work areas must no longer close either UI or stop packaging animation.
- Disable an occupied WorkPoint and verify its own cleanup. Exiting one collider
  must not end the interaction while another collider on the same actor remains.
- Leave packaging midway: stop that actor's animation, while preserving the current
  in-flight packaging completion and saved count. Verify employee exit as well.
- Verify an employee cannot collect from an unreserved source, use the office/store,
  collect finished boxes, or load the truck. All transfer rates, caps, and physics
  paths still need the deferred game run.

## Stage 5 storage policy and deferred cases

| Owner | Accepted item | Limit |
| --- | --- | --- |
| Carrier | First item's type while non-empty | Existing upgrades and buffs |
| IngredientMaker | Ingredient | Intake unbounded; generation still stops at maxObj |
| Conveyor input | Ingredient | Unbounded, matching the old input stack |
| Churu storage | Churu | 40 |
| Box storage | Box | 40 |
| Packaging wait queue | Churu | Unbounded |
| Packaging output in transit | Box | 1, released after its existing tween |
| Truck physical cargo | Box | Truck capacity minus restored count |

- Ingredient collision pads forward collection to IngredientMaker.TryCollect;
  they no longer mutate its stack directly. They also no longer register as
  finished-box save writers: the old generic else branch could write a zero
  PackagingStorageCount from an ingredient pad. Each storage now writes only its
  own existing save key.
- Existing saved storage counts above 40 are restored without truncation before
  resetting intake capacity to 40. Restored packaging waiting-item placement keeps
  the original one-item-height offset. Save formats and packaging count timing
  were retained; this is not a redesign of saves during in-flight production.
- Before deleting the conveyor reconstruction path, its prefab input anchor was
  checked to have no pre-placed children; the Game scene has no input-anchor
  overrides or children parented to its referenced anchor.
- Verify wrong-type collisions do not populate storage, full storage stops intake,
  and two actors/overlapping pads cannot collect the same physical Item twice.
- Verify rejected transfers preserve both counts and transforms. Run several
  complete ingredient-to-truck cycles, including collection while tweens are active.
- Verify packaging output cannot be collected before its output tween completes;
  it must then be collected on a subsequent collision stay and permit the next box.
- Restore truck counts 0 through 4, add the remaining boxes, verify exactly one sale
  for the full loaded count, then verify the next empty truck accepts its full load.
- Save/load partial packaging and counts at/above storage capacity; ingredient pads
  must not overwrite the finished-box count. Saves during the packaging completion
  frame still require explicit runtime checking because the old timing was retained.
- Pool reuse must start with IsStored false, no stale movement tween, and the prefab's
  original ItemType. Stored items must be removed before pool return.
