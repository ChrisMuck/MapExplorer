# Simulation Runner Concept

## Purpose

The Simulation Runner is an internal desktop developer tool for testing the actual game simulation
quickly and transparently. It is not a second game client, a map editor or a replacement for Unity.

It uses the same JSON content, WorldGenerator, commands and game-rule services as the game. Its
purpose is to make hidden objective world developments visible to the team while preserving the
same hidden information for players in Unity.

The runner is especially intended to answer questions such as:

- What happens after the expedition takes or ignores an option?
- Which trigger, evidence, observation and reaction caused a later event?
- Did a faction have a plausible way to learn about a change?
- Which consequences spread over time and which cells or locations did they affect?
- Did the player receive a fair warning and an actionable response opportunity?
- Does a generated world contain the required soft connections without revealing them to the
  expedition automatically?

## Non-Negotiable Architecture

```text
JSON authoring data
        |
Game.Core + Game.App
  World State, Knowledge State, commands, resolver, event queue, ViewModels
        |
   +----+-------------------+
   |                        |
Game.Unity            Game.Simulation.Wpf
player presentation   developer inspection and scenario control
```

- `Game.Core` remains pure C# and owns deterministic simulation rules.
- `Game.App` owns content loading, command handling, turn/world-phase orchestration and
  presentation-neutral ViewModels.
- `Game.Unity` sends commands and renders the ordinary player-facing game. It does not own rules.
- `Game.Simulation.Wpf` is a WPF wrapper around the same shared C# services. It must not reference
  `UnityEngine`, MonoBehaviours, prefabs, scenes or Unity UI classes.
- No consequence, faction reaction, scout result or option availability may be implemented a
  second time for the runner.

If relevant gameplay logic currently lives in a Unity script, it must first be extracted into
`Game.Core` or `Game.App`. Unity and WPF then call that one shared implementation.

## Two Views of One Session

The runner always distinguishes the following views. Switching views must not mutate the session.

| View | Shows | Primary use |
|---|---|---|
| Player view | only `KnowledgeState`, reports, known options and Player Notes | check fairness and information leaks |
| World-truth view | `WorldState`, generated contexts, scheduled branches and faction observations | investigate simulation logic |
| Causality view | links from an action to all derived triggers, processes, reactions and reports | explain why something happened |

The Player view applies the same knowledge gate and interaction-option builder as Unity. It is not
an imitation of Unity's visual layout.

## Runner Capabilities

### 1. Scenario Setup

The runner can load a fixed development scenario or generate a world from a seed. It can configure:

- world-generation preset and deterministic seed;
- expedition members, roles and star ratings;
- starting day, position, known evidence and archived knowledge;
- generated factions, territories, locations and active world situations;
- optional scheduled commands for reproducible multi-day tests.

The runner may create deliberate edge cases that ordinary world generation would rarely produce.
This is necessary for testing, but scenario overrides must be explicit and displayed as such.

### 2. Player Command and Option Inspection

For the current expedition context, the runner displays the same interaction data supplied to Unity:

- visible action label and description;
- availability or disabled state;
- known missing requirements and reasons;
- known risks and uncertainty labels;
- scout availability and mission scope;
- commitment type: immediate, day operation or project.

Selecting an option sends the same command as the Unity client. The runner may additionally offer
an explicit development-only command script to reproduce a test path.

### 3. Time and World-Phase Control

The runner can advance one phase, one day, a specified number of days or until a selected event.
It runs the normal expedition, faction and world phases in their ordinary order. Fast-forwarding
does not skip logic; it only removes real-time presentation delays.

### 4. Causality and Event Timeline

Every authoritative state-changing operation receives a stable `traceId` and, where applicable,
one or more `causedByTraceId` references. The runner can render a human-readable chain such as:

```text
Trace 104: Expedition repairs a crossing (day 3)
  -> Trace 105: trigger "access changed"
  -> Trace 106: local patrol observes changed access (day 4)
  -> Trace 107: faction reaction "investigate" scheduled (day 4)
  -> Trace 108: messenger situation becomes available (day 6)
  -> Trace 109: promise expires without reply (day 10)
```

The event queue remains the source of truth. The timeline is an inspection projection of queued,
resolved and expired events; it must not become a parallel rule engine.

### 5. Spatial Process Inspection Without a Presentation Map

The runner does not need the Unity map, terrain art or prefabs. It can inspect logical coordinates
and spatial relationships through:

- lists of affected hex coordinates, regions, routes and locations;
- process summaries grouped by day and stage;
- source, frontier and affected-area counts for propagation;
- directional and distance summaries;
- optional minimal debug grid or table for a selected region.

This is sufficient to inspect a flood, storm, disease, route closure, patrol observation range or
other spatial process. A full visual map is explicitly out of scope for the first runner version.

### 6. Faction and Consequence Inspection

The World-truth view exposes, for development purposes only:

- which evidence or world observation a faction possesses;
- whether the observation is local, communicated or inferred;
- distance and route conditions that limited observation;
- Trust, Anger and Fear changes with their source trace;
- faction memories, active situations and planned actions;
- scheduled consequence branches, current stage, next stage and end state.

This view is never part of the normal Unity player UI.

### 7. Batch and Regression Simulation

The runner supports non-interactive runs over many seeds and scenarios. It records only aggregated
development metrics, such as:

- generated locations and soft connections per world;
- occurrence, resolution and expiry of situations;
- concurrent active world processes;
- unavailable or dead-end player options;
- warnings and response paths before severe consequences;
- faction observation and reaction frequency;
- reproducible failing seed and command trace.

Batch runs complement unit tests. They do not replace targeted rule tests or human playtests.

## Scenario Definition Data

Development scenarios are JSON, but separate from ordinary authored game content and from save
games. They may reference static content IDs and use explicit generated-world overrides for tests.

```json
{
  "documentType": "development-scenario",
  "schemaVersion": 1,
  "contentVersion": 1,
  "items": [
    {
      "id": "scenario-crossing-claimed-but-unidentified",
      "seed": 41027,
      "worldPresetId": "slice-default",
      "initialExpedition": {
        "memberProfileIds": ["leader-1", "scout-2", "architect-1"],
        "knownEvidenceIds": []
      },
      "worldOverrides": {
        "requiredScenarioProfileIds": ["scenario-broken-crossing"],
        "requiredContextIds": ["context-deliberate-closure"],
        "factionContactStates": ["unknown"]
      },
      "scriptedCommands": [
        { "day": 1, "command": "InspectLocationCommand" },
        { "day": 1, "command": "StartLocalScoutMissionCommand" },
        { "day": 2, "command": "ExecuteLocationActionCommand", "actionId": "action-rebuild-bridge" }
      ],
      "assertions": [
        "player-does-not-know-claim-before-evidence",
        "faction-reaction-requires-observation",
        "player-has-warning-before-severe-restriction"
      ]
    }
  ]
}
```

Scenario IDs, static content IDs and assertions are stable. Generated faction instance IDs,
coordinates, resolved branches and runtime traces belong to the generated session or its saved
result, not to reusable normal location content.

## First Runner Scope

The first usable version deliberately stays small:

1. load the shared JSON content and one seed or scenario;
2. create a `SimulationSession` through the shared application services;
3. show player options plus the outcome of sending commands;
4. advance days through the normal event queue and world phase;
5. display an event timeline, causality chain and selected World-truth inspector;
6. save a reproducible run record containing seed, content version and command trace.

WPF visual polish, graph layouts, a custom map, large-scale batch dashboards and content editing
are later enhancements. The runner's first job is trustworthy, reproducible inspection.

## Relationship to Tests and Unity

- Unit tests prove focused rules and edge cases automatically.
- Scenario assertions prove an end-to-end chain using the same command path as the game.
- Batch simulation detects unwanted distributions and rare generated failures.
- The runner explains a particular seed or test failure.
- Unity verifies the actual presentation, input and player-facing flow.
- Human playtests determine whether uncertainty, warning and decisions feel good.

No one layer replaces the others.

## Implementation Order

1. Audit current Unity scripts and extract any authoritative game-rule code into `Game.Core` or
   `Game.App`.
2. Establish the shared content catalog, manifest loader and cross-reference validator described in
   `cross_system_json_authoring_schema.md`.
3. Add a shared `SimulationSession` / scenario execution facade in `Game.App`; Unity and the runner
   use it through the same commands and ViewModels.
4. Add deterministic trace records, scenario JSON loading and end-to-end scenario assertions.
5. Build a minimal headless runner and use it in automated tests or local command-line runs.
6. Add the WPF shell over that proven runner API.
7. Add batch reporting and optional spatial debug views after the first world processes exist.

The WPF shell comes after the shared API by design. This prevents a useful testing tool from
accidentally becoming a second simulation implementation.

## Acceptance Criteria

- A fixed seed, content version and command sequence always reproduce the same result.
- Unity and the runner return the same player-facing interaction options for the same session.
- Every state-changing consequence can be traced back to a command, generator decision or external
  world trigger.
- The runner can reveal World Truth without changing `KnowledgeState`.
- At least one complete location-to-faction consequence chain and one delayed world process run
  end-to-end before batch features are added.
