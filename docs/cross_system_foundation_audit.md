# Cross-System Foundation Audit

Status: **Completed — Block 0**

This audit records the verified baseline before the shared data catalog and runtime contracts are
changed. It describes current code behaviour, not the target design.

## 1. Authoritative Runtime Paths

### 1.1 Campaign Creation

```text
Unity generation controls
  -> UnityHexMapView.RequestStartGeneratedCampaignFromUi
  -> GameApplication.CreateGeneratedGame
  -> WorldGenBridge.Generate
  -> TutorialGameFactory.CreateGenerated
  -> GameState
```

- `WorldGen` stays pure and produces a `GeneratedWorld`.
- `WorldGenBridge` maps generator cells, anchors, paths, factions, location relations, contexts and
  evidence seeds into `Game.Core` state.
- Generated faction identities and claims are runtime state. Static JSON is not the source of a
  concrete faction-to-location assignment.
- Signature profile assignment is deterministic from the generated seed and faction salt.

### 1.2 Player Commands

```text
Unity input/UI
  -> UnityHexMapView request method
  -> GameApplication facade
  -> Game.App command/service
  -> Game.Core GameState mutation
  -> Unity reads refreshed state/result
```

The relevant command paths are already outside Unity:

| Player intent | Current application path |
|---|---|
| movement and territorial entry | `MoveExpeditionCommand` |
| inspect a reached location | `InspectLocationCommand` |
| inspect available location actions | `GetLocationInteractionCommand` / `LocationInteractionService` |
| resolve an action or project | `ResolveLocationActionCommand` / `AdvanceLocationProjectCommand` |
| local surroundings scouting | `ScoutLocationSurroundingsCommand` |
| directional scouting | `SendScoutMissionCommand` |
| end expedition day | `EndDayCommand` -> `WorldPhaseService` -> scout resolution |
| base time | `AdvanceBaseTimeCommand` -> `WorldPhaseService` |
| faction contact/offers | faction interaction commands |

Search of `UnityHexMapView/Assets` found no direct calls that mutate `coreGameState` through
`Add`, `Set`, `Advance`, `Consume`, `Spend`, `Schedule` or equivalent methods. Unity does own
presentation state such as selected hex, open panel and auto-open bookkeeping. That boundary is
correct and should be preserved.

### 1.3 Current World-Phase Order

`EndDayCommand` currently performs:

1. supplies consumption outside the base;
2. expedition-day and world-day advancement;
3. `WorldPhaseService.Resolve`;
4. expedition failure when supplies reach zero;
5. due scout mission resolution and player events.

`WorldPhaseService` currently:

1. resolves pending world triggers;
2. schedules each configured consequence stage independently;
3. evaluates faction reaction and territorial policy resolvers;
4. applies due stages as player evidence plus an event-queue entry.

This is a functional presentation foundation, but it does not yet implement fixed consequence
branches, typed persistent effects, spatial processes, direct situations or causal trace records.

## 2. Content Loading Baseline

### 2.1 Existing Documents

The GameData tree currently contains 19 schema-version-1 documents:

| Area | Document types |
|---|---|
| Locations | archetypes, variants, modifiers, actions, outcome tables, content profiles |
| World | evidence, triggers, consequences, generation presets, generation-option presets |
| Factions | profiles, signatures, reaction rules, territory-entry rules |
| Scouting | focuses, mission types, outcome rules, report templates |

`LocationDataLoader` supports only the six current location document types and requires
`schemaVersion == 1`. `CrossSystemDataLoader` separately supports the current World, Faction and
Scouting document types and also requires schema version 1. `WorldGenerationPresetLoader` is a
third independent loader.

### 2.2 Bootstrap Divergence

`GameApplication` default discovery loads:

```text
Locations
World
Factions
Scouting
```

The Unity bootstrap in `UnityHexMapView.CreateGameApplication` currently loads:

```text
Locations
World
Factions
```

It omits `Scouting`. It also catches any load failure and falls back to in-code content. This means
Unity can silently run a different content set from tests and direct application callers. Block 1
must replace both paths with one catalog and explicit diagnostics.

### 2.3 Target Schema Compatibility

The approved JSON schema adds manifest, state-profile, scenario-profile, finding, context,
situation, offer, memory and world-generation-rule documents. None is currently loadable, and
target examples use later schema structures. They must not be added to runtime GameData until the
catalog and typed loader support them.

## 3. Design/Implementation Mismatches to Migrate

### 3.1 Legacy Archetypes and Economy Terminology

Current `archetypes.json` contains ten IDs, including `resource-site`, `hazard-zone`,
`landmark-site` and `dynamic-situation`. The approved core taxonomy has seven IDs and explicitly
excludes `resource-site`; the first Slice uses five of those seven.

The following **expedition-logistics** concepts are intentionally present in Core, App, Unity and
tests and must be retained:

- `ExpeditionState.Supplies`, medicine, ration loadout and end-day consumption;
- `BaseState.PendingSupplyBonus` and `PrepareSuppliesWithKnowledgeCommand`;
- faction offers that exchange Knowledge Points for supplies, medicine, reports or practical help;
- base-unit and upgrade effects that raise supply capacity;
- Unity heads-up display and loadout controls;
- existing generator comments naming resource sites.

The approved economy is deliberately two-layered: findings, reports and observations are brought
back to the base and analysed there to produce Knowledge Points; Knowledge Points are the sole
general base/trade currency. Supplies and medicine are separate, limited expedition consumables:
supplies impose return pressure and an expedition fails when they run out, while medicine treats
injuries or enables help at locations. There is no generic material harvesting, processing or
stockpile loop.

This audit does not change expedition pressure or delete the current logistics system. Block 4
must remove only obsolete *material-resource* reward paths and make the finding-to-analysis-to-
Knowledge flow explicit. It must retain and validate supplies/medicine effects in expedition and
faction-offer content. Any change to expedition pressure requires a focused, reviewed change
rather than a silent rename.

### 3.2 Unity Presentation Logic that Must Become Shared Data/ViewModels

`ExpeditionScreenController` contains variant/archetype/modifier/action switches for labels,
descriptions and state wording, including explicit bridge and marked-grave branches. These do not
currently mutate state, but they would make the WPF runner and Unity show different options/text.

Block 4 must move simulation-relevant option descriptions, disabled reasons, state descriptions
and contextual presentation into shared data or application ViewModels. Unity retains only layout,
binding and visual asset selection.

### 3.3 Directional Scout Precision

`ScoutMissionResolutionService.AddDirectionalDiscoveryHints` currently writes the exact logical
location coordinate into a player-facing scout hint. This violates the approved rule that reports
must be spatially approximate and never expose hex values. Block 6 must replace it with qualitative
direction/lead wording and ensure related coordinates remain diagnostic/runtime data rather than
player-facing text.

### 3.4 Consequence Scheduler and Faction Awareness

The current scheduler queues every configured consequence stage separately and applies a stage as
evidence/event text. It has no stored branch selection, affected-coordinate collection, process
state, player response situation or trace lineage.

Faction awareness is currently a coarse region value keyed by a generated relation/location. Local
scouting can increase it. This is a useful seed, but Block 5 must add generated observation
channels, spatial/distance limits, causal provenance and bounded reactions before it is used as the
complete faction-observation model.

## 4. Current Test Baseline

The full existing executable suite was run with:

```text
dotnet run --project tests\Game.Tests\Game.Tests.csproj
```

Result:

```text
All Game.Tests checks passed.
```

The first run was prevented by sandbox access to the user's NuGet configuration; the repeated run
with the required local configuration access passed. No code behaviour was changed during this
audit.

## 5. Block 1 Inputs

Block 1 starts from these decisions:

1. Introduce one `GameDataCatalog` in `Game.App`; it is the only root discovery and loading entry
   point for Unity, tests, runner and direct application use.
2. Keep current directory scans as a documented legacy fallback only while manifest migration is
   incomplete.
3. Make missing or invalid declared content fail visibly; Unity must not silently swap to in-code
   fallback after a partial load.
4. Preserve `LocationDataLoader`, `CrossSystemDataLoader` and `WorldGenerationPresetLoader` as
   parser participants initially, then consolidate their diagnostics and validation through the
   catalog.
5. Add catalog tests before changing any target-schema content document.
