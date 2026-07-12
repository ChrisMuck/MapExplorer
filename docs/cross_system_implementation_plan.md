# Cross-System Implementation Plan

Status: **Active implementation plan**  
Related concept: `docs/gameconcept/cross_system_integration_concept.md`

This plan implements the shared loop between generated world state, locations, scouts, factions and delayed consequences. It is deliberately staged so that the procedural generator remains pure and the player never receives unearned map knowledge.

---

## Locked Scope Decisions

- `WorldGen` is the source of initial world truth; it must not depend on `Game.Core`, `Game.App` or Unity presentation.
- `Game.App` owns the bridge from `GeneratedWorld` into `GameState`.
- Static content definitions never assign a concrete faction to a concrete location. The generator does that after terrain and territory generation.
- A player creates a campaign without seeing or rerolling its generated map. Reroll, seed controls and full map previews belong to a developer/editor mode only.
- The player UI shows broad world preferences only: world size, faction count, faction mood, and optional high-level terrain/settlement presets.
- World triggers, evidence and consequences are generic. Faction reactions are derived from generated runtime context.
- Existing Location JSON remains the source for reusable location content. Generated location instances are runtime/save data.

---

## Phase 0 — Preserve the Existing Baseline

### Work

1. Keep the current Location JSON loader and location interaction framework working while new contracts are added alongside it.
2. Keep `TutorialGameFactory` available for focused tests until generated campaigns replace it.
3. Add regression tests before replacing direct faction effects in current outcome tables.

### Exit Criteria

- Existing location interaction tests continue to pass.
- Existing tutorial behaviour remains usable behind the same `GameApplication` facade.

---

## Phase 1 — Integrate the Pure WorldGen Core

### Work

1. Add the pure `WorldGen` source as one project/assembly in this repository. Do not add `WorldGen.Unity` or `UnityEngine` references to `Game.Core`.
2. Add a .NET project reference for tests and `Game.App`; retain a matching Unity asmdef for the Unity project.
3. Create `WorldGenerationRequest` in `Game.App` as the application-facing subset of generator parameters.
4. Retain `GenerationParams` as the generator-side structure. A later JSON preset layer maps request choices to it.

### Key Rule

The generator returns data only. It never creates `GameState`, UI elements, events or faction relationship scores.

### Exit Criteria

- A deterministic world can be generated from `Game.App` in a unit test.
- No `UnityEngine` dependency is introduced in `Game.Core` or the pure WorldGen assembly.

---

## Phase 2 — Establish the WorldGen Bridge and Coordinate Contract

### Work

1. Implement `WorldGenBridge` in `Game.App`.
2. Translate generator axial coordinates (`AX`, `AZ`) to `Game.Core.HexCoord`; do not use generator offset coordinates (`Col`, `Row`) as axial coordinates.
3. Define the map origin/bounds convention once and test it for every generated cell.
4. Map terrain, elevation, impassability, ownership, roads, rivers, base position and location anchors into Core state.
5. Snapshot generated data into Core state. Core must not retain mutable `WorldGen.HexCell` references.

### Exit Criteria

- Adjacent generator cells remain adjacent after bridging.
- Every generated edge anchor is valid in `Game.Core`.
- Roads, rivers, base and location anchors all refer to existing Core tiles.

---

## Phase 3 — Stable IDs and Generated Location Context

### Work

1. Give generated factions a stable content definition ID in addition to any generator-local numeric index.
2. Replace generator display-name variants with stable location archetype and variant IDs.
3. Extend the generator output / bridge mapping with generated location context:
   - territorial faction, if any
   - claimed, watched, sacred and guarded state
   - neutral/ancient origin
   - context tags such as `deliberate-destruction` or `quarantine`
   - evidence seed IDs such as `warning-posts` or `structural-cuts`
4. Add `LocationFactionRelationState` in Core. This is generated runtime data, not static JSON.

### Exit Criteria

- The same variant can be neutral, watched or claimed in different generated worlds.
- A location inside territory is not automatically faction-owned.

---

## Phase 4 — Campaign Creation UI and Editor Mode

### Player Campaign Creation

1. Create a world-generation screen independent of the expedition UI.
2. Show preset-level options only: world size, faction count, faction mood, coastline, terrain, activity and settlement density.
3. Create `world-generation-presets.json` to map UI choices to `GenerationParams` values.
4. On **Begin Expedition**, generate once without showing the resulting map, then bridge the accepted world into `GameState` and start the expedition scene.

### Developer / Editor Mode

1. Keep the existing `WorldGenRunner` / `WorldGenSettings` workflow as a technical tool.
2. Allow full preview, seed editing, stats/warnings and independent faction/location rerolls only here.
3. Never expose this preview or reroll behaviour in the normal campaign flow.

### Exit Criteria

- Player mode cannot inspect or reroll a generated campaign map.
- Editor mode can generate and inspect deterministic worlds without starting an expedition.

---

## Phase 5 — Shared Runtime Contracts and JSON Definitions

### New Core Runtime State

```text
EvidenceState
WorldTriggerState
ScheduledConsequenceState
RegionFactionAwarenessState
LocationFactionRelationState
```

### New JSON Definition Sets

```text
world/evidence-definitions.json
world/world-trigger-definitions.json
world/consequences.json
world/events.json

scouting/mission-types.json
scouting/focuses.json
scouting/outcome-tables.json
scouting/report-templates.json

factions/factions.json
factions/territorial-rules.json
factions/signatures.json
factions/reaction-rules.json
```

### Existing Location Content Changes

- `actions.json` gains semantic action tags (`repair`, `open`, `disturb`, `respect`, and so on).
- `outcome-tables.json` gains generic evidence, trigger and scheduled-consequence effects.
- Existing direct `factionId` effects are migrated only after the reaction resolver is available.

### Exit Criteria

- Definition validation detects unresolved IDs.
- Content can express a neutral trigger without naming a generated faction.

---

## Phase 6 — Location Outcomes, World Phase and Consequence Scheduler

### Work

1. Extend location outcome effects with `AddEvidence`, `RaiseWorldTrigger` and `ScheduleConsequence`.
2. Replace direct faction changes from generic location outcomes with world triggers.
3. Add one shared World Phase called after expedition days and base-time advancement:
   1. advance world time
   2. apply due consequence stages
   3. resolve pending world triggers
   4. resolve faction observation and reactions
   5. resolve scout progress
   6. enqueue player-visible events
4. Resolve a consequence's branch when it is triggered; store the fixed result and never reroll it when the delay expires.

### Exit Criteria

- A sealed-location consequence can become visible after several expedition or base days.
- A world trigger affects no faction when its generated context is neutral.

---

## Phase 7 — Scout Surroundings Reconnaissance

### Work

1. Add a location-targeted scout mission type: **Scout surroundings**.
2. Require one or more currently available scout members.
3. Return report evidence selected from generated location and regional evidence seeds.
4. Track regional faction awareness when a scout is detected.
5. Preserve the distinction between a scout report, confirmed expedition knowledge and player notes.

### Exit Criteria

- A scout can return evidence connecting a location to a recurring faction signature without revealing ownership truth.
- An unavailable scout prevents the action.

---

## Phase 8 — Data-Driven Faction Reactions

### Work

1. Move faction values, territorial rules, signatures and reaction rules out of the current hardcoded application paths.
2. Resolve reactions from:
   - neutral world trigger
   - generated location relation
   - faction observation/awareness
   - current trust, anger, fear and memory
3. Surface only player-earned reactions through the event queue.

### Exit Criteria

- Rebuilding an identical bridge can be welcome, ignored or hostile in three differently generated contexts.
- Faction reactions do not require a faction ID in a generic location outcome table.

---

## Phase 9 — Proof Cases and Test Coverage

### Broken Bridge

```text
inspect -> structural evidence
scout surroundings -> regional/faction-sign evidence
rebuild -> neutral world trigger
world phase -> context-specific faction reaction or no reaction
```

### Sealed Crypt

```text
inspect -> warning evidence
open -> local state + scheduled consequence
world time passes -> hidden consequence stages
evidence/event -> danger becomes player-visible
```

### Required Tests

- generator determinism and bridge coordinate correctness
- valid location anchors and paths after bridging
- player campaign generation does not expose map truth
- no direct faction ID in generic location outcomes
- scout availability, evidence and awareness behaviour
- delayed consequence timing through expedition and base time
- generated neutral / watched / claimed reaction differences
- existing location interaction regression suite

---

## Human Review Gates

Human approval is required before merging changes to:

- player-visible campaign-creation flow
- generator parameter presets and procedural generation rules
- save compatibility for generated-world metadata or scheduled consequences
- faction reaction semantics
- final presentation of unexplored map information

