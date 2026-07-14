# Cross-System Foundation Implementation Plan

Status: **Active**
Branch: `codex/cross-system-foundation`
Scope: shared simulation foundation for the approved core gameplay concepts.
Related concepts:

- `docs/gameconcept/cross_system_integration_concept.md`
- `docs/gameconcept/cross_system_json_authoring_schema.md`
- `docs/gameconcept/simulation_runner_concept.md`

## 1. Objective

Build one authoritative, deterministic game simulation that is shared by Unity, automated tests and
the future Simulation Runner. The implementation must turn the approved JSON contracts into typed
content, generated World State and player-facing knowledge without exposing hidden truth through
the Unity UI.

This plan deliberately does **not** build the WPF interface first. Its foundation is a shared
headless session API. WPF becomes a thin inspection client only after that API is proven by tests.

## 2. Verified Starting Point

The repository already has useful foundations:

- `Game.Core` and `Game.App` target .NET 8 and do not depend on Unity.
- `GameApplication` is already a command facade used by Unity and tests.
- World generation is bridged through `WorldGenBridge` in `Game.App`.
- Generic location actions, local scouting, basic world triggers, delayed evidence and faction
  reactions already have proof tests.
- `Game.Tests` is an executable regression suite with focused end-to-end checks.

The following gaps must be resolved rather than worked around:

- location and cross-system content are loaded through separate directory scans instead of one
  explicit catalog;
- the Unity bootstrap currently loads World and Faction data but omits the Scouting folder, so it
  can diverge from the application/test loading path;
- the current consequence scheduler stores individual delayed stages, but not a fixed resolved
  branch, typed world effects, situations or a causal trace;
- some location presentation text and content decisions remain hardcoded in Unity UI scripts;
- legacy material-resource terminology still exists and must be isolated from the retained
  supplies/medicine expedition-logistics model;
- no shared scenario session, replay trace or headless runner exists yet.

## 3. Delivery Rules

- Every block is independently buildable and covered by focused tests before the next starts.
- When an audit finds an older concept that contradicts a locked current decision, correct or
  explicitly supersede that passage in the same implementation block. A stale specialist document
  must never remain an alternative source of truth for implemented behaviour.
- Static JSON never names a generated faction instance, coordinate, territory, claim or resolved
  consequence branch.
- Runtime IDs, random choices and trace records are deterministic for the same content version,
  seed and command sequence.
- Unity sends commands and renders ViewModels. It does not gain simulation exceptions or a second
  rule path.
- The runner may inspect World Truth, but must not write player knowledge merely by viewing it.
- We keep existing authored JSON compatible while its replacement fields are introduced; no broad
  content migration happens without validation and regression coverage.

## 4. Work Blocks

### Block 0 — Baseline Audit and Compatibility Contract — Complete

**Purpose:** establish an exact boundary before moving code or content.

**Work**

1. Map every gameplay mutation in `Game.Core`, `Game.App`, `UnityHexMapView` and `WorldGen`.
2. Classify each Unity method as input/presentation, application orchestration or incorrectly
   embedded authoritative logic.
3. Record the current JSON document types, their loaders, fallback paths and consumers.
4. Record all material-resource and expedition-logistics uses. Classify each as obsolete material
   economy, retained supplies/medicine logistics, presentation wording or test fixture.
5. Define a compatibility matrix for old content documents and target schema documents.

**Primary files**

- `src/Game.App/GameApplication.cs`
- `src/Game.App/LocationDataLoader.cs`
- `src/Game.App/CrossSystemDataLoader.cs`
- `src/Game.App/WorldPhaseService.cs`
- `UnityHexMapView/Assets/Scripts/UnityHexMapView.cs`
- `UnityHexMapView/Assets/UI/ExpeditionScreenController.cs`

**Exit criteria**

- A short audit record identifies the single authoritative path for each current player command.
- No code changes alter normal gameplay in this block.
- The existing test suite passes before and after the audit.

### Block 1 — Unified Game-Data Catalog and Validation — Complete

**Purpose:** Unity, tests and future runner load the identical declared content set.

**Work**

1. Add a `GameDataCatalog` in `Game.App` that owns root discovery, manifest loading, document
   ordering, schema-version checks and consolidated error reporting.
2. Support the target `game-data-manifest.json` while retaining the current directory-scan fallback
   for existing content during migration.
3. Make `LocationDataLoader` and `CrossSystemDataLoader` internal participants of the catalog,
   rather than separate bootstrapping paths.
4. Change Unity bootstrap and test setup to request the same catalog from the same game-data root.
5. Add validation for duplicate IDs, unknown `documentType`, missing required documents, invalid
   schema versions and unresolved cross-references.

**Acceptance tests**

- Unity, tests and a standalone catalog test resolve the same content IDs.
- A manifest that omits Scouting fails clearly instead of silently creating a divergent session.
- Old directory-based content remains loadable until the manifest migration is complete.

### Block 2 — Typed Authoring Definitions and Slice Content Migration

**Purpose:** implement the approved JSON authoring structure without introducing per-location
special code.

**Work**

1. Correct the affected master and specialist concepts before data migration. The current rules are:
   seven canonical location archetypes (no `resource-site`), Knowledge Points as the only general
   base/trade currency, supplies and medicine as expedition-only consumables, no generic material
   harvesting loop, and approximate player-facing scout reports without hex values. Record any
   deliberately deferred replacement separately rather than retaining incompatible legacy rules.
2. Add typed definitions and loaders for State Profiles, Scenario Profiles, Findings, neutral
   Context Definitions, Situation Definitions, Faction Offers and Faction Memory Definitions.
3. Expand existing Action, Evidence, Consequence, Faction Profile, Signature and Scout definitions
   with the approved optional fields and compatibility defaults.
4. Extend `CrossSystemContentValidator` with the schema rules from the JSON concept: reference
   validity, archetype/state compatibility, action cap, no static faction instance IDs, no generic
   material harvesting rewards, valid expedition-logistics effects, warning path for severe
   consequence stages and soft-connection requirements.
5. Add the five Vertical Slice scenario profiles: Route Obstacle, Investigation Site, Territorial
   Marker, Containment Site and Contact Site.
6. Keep `content-profiles.json` presentation-only and move rule-bearing data to Scenario Profiles
   or the appropriate shared definition file.

**Acceptance tests**

- Invalid definitions fail at catalog validation with an actionable file/ID/field error.
- The master concept and every affected specialist document point to the same current taxonomy,
  knowledge/trade currency, expedition-logistics and scout-information rule.
- A reusable scenario profile can produce neutral, watched or claimed generated instances without
  naming a concrete faction in JSON.
- Five Slice profiles load through the catalog and preserve the current bridge/gate proof flows.

### Block 3 — Shared Runtime State, Determinism and Causal Trace

**Purpose:** give every generated or delayed effect durable, inspectable runtime identity.

**Work**

1. Extend `WorldState` with generated context assignments, active world processes, situations and
   fixed consequence branches.
2. Replace stage-only scheduling with a consequence instance that records its definition, resolved
   branch, source, affected context, current stage and future due stages.
3. Add deterministic runtime ID allocation and a seeded random abstraction at the application
   boundary; do not use Unity random APIs in simulation decisions.
4. Add `SimulationTrace` records with `traceId` and causal-parent references for commands,
   generator decisions, triggers, observation, reaction, situation and consequence stage changes.
5. Preserve trace, resolved branch and generated context in save-ready runtime models.

**Acceptance tests**

- The same seed and command sequence creates identical branch IDs, state changes and trace order.
- A delayed consequence cannot reroll after time advances or after reloading its persisted state.
- Inspecting the World-truth trace does not alter `KnowledgeState` or `PlayerNotes`.

### Block 4 — Generic Location Interaction and Knowledge Contract

**Purpose:** make all location archetypes use common action conditions, evidence and persistent
state changes.

**Work**

1. Extend interaction-option building from hardcoded variants to Scenario Profile, State Profile,
   generated context and specialist requirements.
2. Model known hard requirements, visible disabled reasons, unknown risks and contextual action
   discovery separately.
3. Limit each initial location instance to its common actions plus one to three additional actions;
   later knowledge, specialist or context discoveries may expand it.
4. Add generic finding acquisition and base-analysis handoff. Findings, inspections and reports
   generate field knowledge; analysed findings award Knowledge Points and archive entries.
5. Remove obsolete material-resource reward paths from cross-system content. Make the
   finding-to-base-analysis-to-Knowledge flow explicit, while retaining supplies and medicine as
   expedition consumables that can be obtained through defined opportunities such as faction
   offers.
6. Move simulation-relevant action labels, state wording and availability reasons out of Unity UI
   conditionals into shared data/ViewModels.

**Acceptance tests**

- A missing obvious requirement disables the appropriate option with a reason.
- A hidden risk remains an uncertainty label, not an accidental truth leak.
- A bridge, a sealed site and a non-bridge/non-crypt profile all use the same option-resolution
  pipeline.
- A finding becomes Knowledge Points only through the base-analysis path.

### Block 5 — World Processes, Observation and Faction Reactions

**Purpose:** replace presentation-only delayed outcomes with generic, fair and spatially grounded
world development.

**Work**

1. Implement typed generic world effects for location state, route/access, affected cells, evidence,
   follow-up trigger, situation, faction awareness and connection creation.
2. Add staged process execution for the approved consequence families: propagation, release,
   route-or-place change, social aftereffect, knowledge trail, rescue/assistance and external world
   crisis.
3. Resolve a branch once when its trigger is scheduled; stages apply in normal world phases.
4. Implement faction observation based on generated territory/context, observation channels,
   distance/route constraints and actual evidence. Factions are not all-knowing.
5. Resolve reactions through profile values, taboo, memory, observation and situation context.
   Deliver player-visible effects only through reports, contacts, rumours, direct observation or
   event queue entries.
6. Enforce a bounded MVP load: at most one direct open situation per faction and a cap on concurrent
   world processes.

**Acceptance tests**

- A neutral action produces no faction reaction merely because a faction exists elsewhere.
- A watched/claimed action can produce different reactions from the same generic action.
- A severe process provides at least one valid warning and response path before its serious stage.
- Spatial effects update only logical coordinates/relations; no Unity map dependency is introduced.

### Block 6 — Both Scout Paths and Generated Soft Connections

**Purpose:** complete the distinct local and directional scouting loops.

**Work**

1. Keep local surroundings scouting tied to a reached location and limited to local evidence.
2. Expand directional scouting to search a sector for approximate leads about locations, routes,
   hazards, signatures, traces, witnesses and environmental change.
3. Scale report quality by scout star rating without turning higher quality into hidden-truth
   revelation; reports remain directionally vague and evidence-based.
4. Generate and persist at least two optional soft connections in every Slice world. They may share
   a signature, make another place intelligible, change access or connect evidence threads, but
   never create a mandatory quest or automatic conclusion.
5. Let the WorldGenerator assign candidate contexts, claims, signatures and connections only after
   terrain and territories are generated.

**Acceptance tests**

- Local scouting cannot reveal a distant exact target or broad route.
- Directional scouting does not require a location interaction.
- A veteran report has better evidence quality, not a solved faction/location identity.
- Every generated Slice fixture has two optional soft connections and no static faction assignment.

### Block 7 — Shared Simulation Session and Headless Scenario Runner

**Purpose:** make the full simulation reproducible before adding a desktop inspection UI.

**Work**

1. Add a `SimulationSession` facade in `Game.App` around `GameApplication`, `GameState`, content
   version, seed, trace and command history.
2. Add JSON development scenarios with seed, explicit test overrides, scripted commands and
   assertions. Keep them separate from normal game content and saves.
3. Add a headless `Game.Simulation.Runner` executable for one scenario, time advancement, timeline
   output and reproducible run record export.
4. Add scenario assertions for player knowledge, World Truth, expected trace chain and fair warning
   paths.
5. Create at least three end-to-end scenarios: claimed-but-unidentified crossing, containment
   release with delayed response, and directional scout lead that later becomes useful.

**Acceptance tests**

- A failing scenario reports seed, content version, command index and trace chain.
- Replaying a run record yields the same terminal state and player-visible options.
- The headless runner has no Unity reference.

### Block 8 — Unity Adapter Migration and Regression Pass

**Purpose:** route normal gameplay through the same catalog/session/option contracts without
changing its intended player-facing presentation.

**Work**

1. Replace Unity's bespoke bootstrap with the shared `GameDataCatalog` and `SimulationSession`.
2. Replace simulation-relevant Unity switch statements and string tables with shared interaction
   ViewModels and content-driven descriptions.
3. Keep map rendering, camera, panel layout and visual asset resolution in Unity.
4. Ensure player views show only Knowledge State and earned reports, never runner trace or hidden
   World Truth.
5. Add Unity integration checks for catalog-load failure messaging and representative location
   panels; Core correctness remains covered outside Unity.

**Acceptance tests**

- Unity and the headless runner show identical option IDs, availability and known requirement text
  for the same scenario state.
- Unity does not load a content subset that differs from tests/runner.
- Existing movement, map and panel behavior remains functional.

### Block 9 — WPF Simulation Runner Shell

**Purpose:** provide fast human inspection of the proven headless simulation.

**Work**

1. Add a Windows-only `Game.Simulation.Wpf` project referencing only the shared .NET projects.
2. Implement scenario selection, command execution, phase/day fast-forward and reproducible run
   loading.
3. Show separate Player, World-truth and Causality views.
4. Show location options as shared interaction ViewModels, not copied Unity windows.
5. Add timeline, faction observation/reaction inspector and a mapless spatial process inspector
   based on coordinates, regions and affected-area summaries.

**Acceptance tests**

- The WPF application can load and replay all three headless proof scenarios.
- Viewing World Truth cannot mutate the session.
- No Unity assemblies are referenced by the WPF project.

### Block 10 — Batch Simulation, Balance Signals and Release Gate

**Purpose:** discover rare generated failures and make the first Slice suitable for human playtests.

**Work**

1. Add batch runs over deterministic seed ranges and record generation/interaction metrics.
2. Flag invalid content, absent soft connections, excessive concurrent processes, unanswerable
   situations, missing warning paths and no-op faction observation.
3. Review the three proof scenarios and a representative batch with the project owner.
4. Freeze the first Slice content profiles and start Unity playtests for clarity and game feel.

**Release criteria**

- Core test suite, scenario suite and a documented batch run pass.
- The first Slice has five data-driven profiles, two generated soft connections and three complete
  end-to-end proof scenarios.
- No player-facing path reveals hidden faction identity, claim truth, exact remote targets or
  resolved consequence branches without earned evidence.

## 5. Explicitly Deferred

- large content libraries and text-variation authoring;
- advanced contested multi-faction locations;
- non-human factions and deep diplomacy;
- a rendered WPF map, final art or final Unity UI polish;
- broad WorldGenerator parameter redesign beyond data-driven context/connection assignment;
- save-format migration for old external saves before a save system is formally introduced.

## 6. Working Sequence

We execute one block at a time. A block is only marked complete after its acceptance tests pass and
the project owner has reviewed any player-facing or generated-world semantics. Block 0 is recorded
in `docs/cross_system_foundation_audit.md`. Blocks 0 and 1 are complete; the immediate next
implementation block is **Block 2**. No WPF work begins before Block 7.
