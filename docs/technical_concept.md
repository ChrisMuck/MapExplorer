# Technical Concept

Working Title: **Untitled Expedition Game**  
Related Design Document: `docs/gameconcept/exploration_game_concept.md`
Engine Decision: **Unity 6.5 + C#**  
MVP Target Platform: **Desktop, primarily Windows**  
Status: **Early Technical Architecture / Living Document**

---

## 1. Purpose of This Document

This document describes the technical architecture for the expedition exploration game.

The design document explains **what** the game should be.

This technical document explains **how** the game should be structured so that AI coding agents and human contributors can build it without mixing systems, UI and game rules into unmaintainable code.

The main technical goals are:

- make the project AI-agent-friendly
- keep game rules separate from presentation
- keep systems testable
- make content data-driven
- support long-term expansion
- support expedition legacy and persistent world changes
- prevent game logic from being hidden inside Unity scenes, prefabs, MonoBehaviours or UI scripts

Core technical rule:

> Simulation and game state must be separate from presentation.

---

## 2. Technology Decision

The game will be built with:

- **Unity 6.5**
- **C#**
- **Desktop target, primarily Windows**

The MVP targets a desktop build and editor play mode.

WebGL is not a priority for the MVP.

### 2.1 Why Unity 6.5

Unity is a good choice for this project because:

- the user already has Unity experience
- C# is first-class
- the editor workflow is familiar
- 2D/isometric presentation is well-supported
- UI Toolkit or uGUI can be used for panels and overlays
- ScriptableObjects can be used for content definitions if useful
- the ecosystem is large
- AI coding agents can work well with C# projects when the architecture is explicit

### 2.2 Why C#

C# is preferred because:

- it is strongly typed
- AI coding agents generally handle it well
- it supports cleaner architecture for game systems
- it is suitable for unit testing
- it makes data models explicit
- it helps prevent large unstructured script files

### 2.3 Target Platform

The MVP targets desktop, primarily Windows.

Do not optimize for mobile, WebGL, console or controller support during the first Vertical Slice.

---

## 3. Architectural Principles

### 3.1 Game Logic Must Not Live in Unity Scenes or MonoBehaviours

Unity scenes, GameObjects, prefabs and MonoBehaviours should be responsible for:

- rendering
- input
- UI
- camera
- visual feedback
- map display
- panels and screens
- connecting Unity events to application commands

They should not contain core game rules.

Bad pattern:

> A UI button directly modifies a Hex GameObject, changes faction state and creates a scout report.

Good pattern:

> A UI button creates a command. The command is handled by the application layer. The application layer updates GameState. The UI refreshes from GameState.

### 3.2 Separate Truth, Knowledge and Notes

The game depends on imperfect information.

The technical model must separate:

```text
WorldState      = objective truth
KnowledgeState  = what the expedition/archive knows
PlayerNotes     = what the player personally marks or assumes
```

Example:

```text
WorldState:
  Hex 14/08 is inside Border Warden warning territory.

KnowledgeState:
  Scout report says carved posts were seen near Hex 14/08.

PlayerNotes:
  "Probably a faction border. Avoid with large group."
```

This separation is essential.

If the game exposes objective truth too easily, the exploration and interpretation fantasy breaks.

### 3.3 Data-Driven Content

The game should become data-driven.

Factions, events, roles, biomes, locations, goals, leverage objects, visual assets and scout report templates should not be hardcoded long term.

For the MVP, some content may be created directly in code if needed, but the architecture should support file-based definitions.

Recommended MVP format:

> JSON for content definitions.

Unity ScriptableObjects may be added later for editor-friendly authoring.

For AI coding agents and version control, JSON is usually easier to generate, diff and review.

Leverage objects should use data definitions rather than hardcoded special cases once the map generator exists. A generated map should be able to assign an object, proof, favor or clue to a special location, faction territory, scout report or trade chain and then link it to faction dialogue, offers or leader access.

---

## 4. Layered Architecture

The project should be divided into three main layers.

### 4.1 Game.Core

Pure C# game logic.

No UnityEngine dependency.

No MonoBehaviours.

No UI code.

No presentation logic.

Responsible for:

- hex coordinates
- map state
- biome data
- movement cost
- fog / knowledge states
- expedition state
- expedition members
- resources
- scout missions
- scout reports
- factions
- special locations
- events
- risk resolution
- mandates / goals
- base phase
- savegame data models

The Core layer should be unit-testable without launching Unity.

### 4.2 Game.App

Application layer / orchestration layer.

Responsible for:

- commands
- controllers
- use cases
- validating player actions
- applying changes to GameState
- coordinating systems
- running turn flow
- resolving world phase
- resolving base phase
- creating event queue items

This layer connects player intent to core systems.

### 4.3 Game.Unity

Unity presentation layer.

Responsible for:

- Unity scenes
- GameObjects
- prefabs
- MonoBehaviours
- UI Toolkit/uGUI panels
- map rendering
- camera
- input
- visual markers
- report windows
- archive screen
- base screen
- event presentation
- placeholder portraits and discovery images

This layer reads GameState and sends commands.

It should not own the simulation.

---

## 5. Recommended Project Layout

```text
expedition-game/
  docs/
    gameconcept/
      exploration_game_concept.md
      *_concept.md
    technical_concept.md
    ui_ux_concept.md
    map_presentation_simplified_concept.md
    visual_asset_tech_addendum.md
    implementation_plan.md

  UnityProject/
    Assets/
      ExpeditionGame/
        Scenes/
          Main.unity
        Scripts/
          CoreBridge/
          Map/
          UI/
          Input/
          Visuals/
          Bootstrap/
        Prefabs/
          Map/
          UI/
          Markers/
          Popups/
        Art/
          Placeholders/
            Portraits/
            Factions/
            Locations/
            Events/
            Objects/
            Symbols/
        Data/
          Biomes/
          Roles/
          Factions/
          Locations/
          Events/
          Goals/
          ScoutReports/
        Settings/
    Packages/
    ProjectSettings/

  src/
    Game.Core/
      Hex/
      World/
      Expedition/
      Scouts/
      Factions/
      Locations/
      Events/
      Goals/
      Base/
      Knowledge/
      Save/
      Common/
      Visuals/

    Game.App/
      Commands/
      Controllers/
      Services/
      UseCases/

    Game.Tests/
      Hex/
      Expedition/
      Scouts/
      Factions/
      Events/
      Save/
      Visuals/

  content/
    biomes/
    roles/
    factions/
    locations/
    events/
    goals/
    scout_reports/
    visuals/
    tutorial/

  saves/
    dev/
```

The exact Unity folder structure can evolve, but the separation must remain.

Unity presentation scripts may reference `Game.App`.

`Game.App` may reference `Game.Core`.

`Game.Core` must not reference Unity.

---

## 6. Runtime Flow

### 6.1 Game Modes

The game has two main runtime modes:

```csharp
public enum GameMode
{
    Expedition,
    BasePhase
}
```

### 6.2 Expedition Day Flow

Each expedition turn represents one expedition day.

Flow:

1. Player phase begins.
2. Player moves expedition, sends scouts, reads reports, interacts with map or locations.
3. Player ends day.
4. World phase runs.
5. Scouts progress or resolve.
6. Factions react.
7. Events are generated.
8. Supplies are consumed.
9. Morale, injuries and projects update.
10. UI refreshes from GameState.
11. New day begins.

### 6.3 Base Phase Flow

When the expedition returns, the game enters Base Phase.

Base actions may consume days.

The world continues to advance during this time.

Base phase flow:

1. Expedition returns.
2. Reports are archived.
3. Members recover or remain unavailable.
4. Base actions are chosen.
5. Time passes.
6. World reactions are resolved.
7. New expedition is prepared.
8. Expedition starts.

Core rule:

> Preparation costs time. Time changes the world.

---

## 7. Command System

Player actions should be modeled as commands.

Examples:

```text
MoveExpeditionCommand
SendScoutCommand
EndDayCommand
AddMapMarkerCommand
AddFreeTextNoteCommand
InspectLocationCommand
StartCampCommand
StartProjectCommand
ReturnToBaseCommand
StartBaseActionCommand
StartUpgradeCommand
EvaluateKnowledgeItemCommand
AdvanceBaseTimeCommand
StartNewExpeditionCommand
```

Each command should have:

- input data
- validation
- result
- possible generated events
- state changes

Example:

```csharp
public sealed class SendScoutCommand
{
    public List<string> ScoutMemberIds { get; set; }
    public ScoutDirection Direction { get; set; }
    public int DurationDays { get; set; }
    public ScoutFocus Focus { get; set; }
    public ScoutBehavior Behavior { get; set; }
}
```

Command result:

```csharp
public sealed class CommandResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public List<EventState> GeneratedEvents { get; set; }
}
```

The UI must not directly mutate game state.

The UI sends commands.

---

## 8. Event Queue

Systems should create events and place them in an event queue.

The UI displays pending events and lets the player resolve them.

Examples:

```text
ScoutOverdueEvent
FactionWarningEvent
FoundSealedBoxEvent
StrangerWithMapEvent
LocationNoiseEvent
BridgeConstructionDiscoveredEvent
```

The event queue prevents systems from directly opening UI windows or mixing simulation with presentation.

---

## 9. Core Data Model

The initial data model remains defined around:

- GameState
- WorldState
- KnowledgeState
- PlayerNotes
- ExpeditionState
- ScoutMissionState
- ScoutReportState
- FactionState
- SpecialLocationState
- EventState
- GoalState
- BaseState
- BaseRosterState
- BaseMemberState
- BaseUnitStockState
- BaseUnitState
- BaseUpgradesState
- BaseUpgradeState
- EvaluationQueueState
- EvaluationItemState
- ArchiveEntryState
- VisualAssetDefinition
- PersistentWorldChange

Most runtime models should use stable string IDs.

Hexes use axial coordinates:

```csharp
public readonly record struct HexCoord(int Q, int R);
```

### 9.1 Current Base Camp Model

The current MVP prototype treats Base Camp as a command-driven preparation phase backed by Core/App
state, not by Unity scene objects.

Current data ownership:

- `BaseState` stores base location, Knowledge Points, pending supply bonus, next-expedition timing,
  typed archive entries, lost expedition records, upgrades, evaluation queue and unit stock.
- `BaseRosterState` stores named people available to the base; UI composition selects from this
  roster and `StartNewExpeditionCommand` converts selected roster members into expedition members.
- `BaseUnitStockState` stores generic porter/soldier support units used during departure loadout.
- `BaseUpgradesState` stores persistent upgrades; `StartUpgradeCommand` spends Knowledge Points and
  marks upgrades built.
- `EvaluationQueueState` stores base analysis items; `AdvanceBaseTimeCommand` advances progress and
  `EvaluateKnowledgeItemCommand` converts ready items into archived insights and Knowledge Points.
- `ArchiveEntryState` is the typed archive source of truth; legacy string archive access is only a
  compatibility projection.

Unity presentation may open the Base Camp UI, show these state objects and send commands. It must not
own the base economy, recovery rules, upgrade rules, evaluation rules or expedition departure rules.

Current rule:

- A new tutorial expedition starts with an empty `EvaluationQueueState`.
- Evaluation items should be created by world discoveries, inspected locations, scout reports,
  faction exchanges or recovered expedition records, not seeded into the base at game start.
- Future discovery commands should add analysis items with stable source ids, source text, required
  base days, Knowledge Point reward and insight text.
- Lost expeditions should lose their unsecured analysis items unless a later recovery action restores
  them.

---

## 10. Simplified Map Presentation

The MVP must not attempt a full 4X map.

Use `map_presentation_simplified_concept.md` as the source of truth.

Core map decisions:

- hidden logical hex grid
- flat isometric 2D presentation
- no permanent visible hex outlines in normal play
- no terrain height on hex cells
- no sculpted 3D terrain
- forests, mountains, roads, rivers and locations represented by simple placed objects, sprites or line segments
- debug grid allowed as toggle
- selection/reachability overlays allowed temporarily

Core rule:

> Hexes are the data structure, not the art style.

Implementation order:

1. logical map foundation
2. simple isometric map presentation
3. UI overlay
4. movement and knowledge loop
5. scouts and reports
6. special locations and events
7. base phase and second expedition

---

## 11. Unity Presentation Rules

Unity scripts may:

- handle input
- display state
- open and close UI panels
- send commands
- refresh visuals
- instantiate visual prefabs
- load placeholder art
- render map objects from state

Unity scripts must not:

- calculate scout outcomes
- change faction trust directly
- consume supplies directly
- decide event consequences directly
- change hidden world truth directly
- reveal objective truth not present in KnowledgeState
- store authoritative game state only in scene objects

Scene objects are views.

GameState is the source of truth.

---

## 12. UI/UX Concept Reference

The project should include `ui_ux_concept.md` as the source of truth for UI direction.

UI work should follow these principles:

- map-first layout
- top global status bar
- left selected-context panel
- right expedition/report/journal panel
- bottom context action bar
- readable scout reports
- clear uncertainty states
- fast player markers and notes
- no hidden `WorldState` truth exposed through UI
- no simulation rules inside Unity UI scripts

The current prototype UI is acceptable for development, but it should not become the final layout by accident.

---

## 13. Portraits and Discovery Images

The MVP should support visual anchors for important communication and discovery.

Core rule:

> Important information should have a face, a place or an image.

The game should show:

- scout portrait in scout reports
- faction/person portrait in faction interactions
- scene/object image in event popups
- location image in discovery cards
- thumbnail image in archive cards

Use placeholders first.

Do not block implementation on final art.

Images must not reveal hidden truth.

---

## 14. VisualAssetDefinition

```csharp
public sealed class VisualAssetDefinition
{
    public string VisualAssetId { get; set; }
    public VisualAssetType Type { get; set; }

    public string DisplayName { get; set; }
    public string AssetPath { get; set; }

    public bool IsPlaceholder { get; set; }
    public string? Description { get; set; }

    public List<string> Tags { get; set; }
}

public enum VisualAssetType
{
    Portrait,
    FactionSymbol,
    LocationImage,
    EventImage,
    ObjectImage,
    SymbolImage,
    ReportImage,
    UnknownPlaceholder
}
```

Runtime objects should reference visuals by ID, not hardcoded Unity object references wherever possible.

Unity may resolve IDs to sprites, textures or prefabs through a presentation-layer registry.

---

## 15. Testing Strategy

Because core logic is separated from Unity, unit tests should cover:

- hex neighbor lookup
- hex distance
- movement cost
- expedition movement point spending
- supply consumption per day
- expedition member availability
- scout mission creation
- scout return day calculation
- overdue scout state
- knowledge state updates
- map marker creation
- faction trust / anger / fear changes
- location state changes
- event creation
- base phase time advancement
- save/load roundtrip
- visual asset fallback ID resolution in presentation-facing services

Early tests should prioritize game-state correctness over visual behavior.

Unity play mode tests may be added later for presentation and scene integration.

---

## 16. Technical Implementation Roadmap

### Phase 1: Core C# Without Unity

Build pure C# logic first.

Minimum:

- HexCoord
- Hex map
- GameState
- ExpeditionState
- members and roles
- movement points
- supply consumption
- day advancement
- knowledge state
- basic tests

### Phase 2: Unity Project and Simple Map

Create Unity 6.5 project.

Show a placeholder isometric map.

Minimum:

- bootstrap scene
- hidden logical hex grid
- optional debug grid
- flat map view
- coordinate selection
- expedition marker

### Phase 3: Basic Map Presentation

Implement:

- isometric coordinate conversion
- terrain base layer
- object placement layer
- forest/mountain placeholders
- road/river line segments
- fog/knowledge overlay

No final art.

### Phase 4: UI Overlay

Implement:

- top status bar
- left context panel
- right scout/report/expedition panel
- bottom action bar
- selected hex information
- end day button

### Phase 5: Movement and Turn Loop

Implement:

- move expedition
- spend movement points
- reveal knowledge
- end day
- consume supplies
- update expedition day

### Phase 6: Scouts

Implement:

- send scout
- choose direction, duration, focus and behavior
- world phase progresses scout mission
- scout returns, becomes overdue or is missing
- scout report with portrait is generated
- report hints can create markers

### Phase 7: Notes and Archive

Implement:

- quick markers
- free text notes
- scout report archive
- basic expedition journal
- old/current knowledge distinction
- archive thumbnails

### Phase 8: Special Locations and Events

Implement:

- marked grave
- ravine / broken bridge
- abandoned camp
- sealed gate / ruin placeholder
- event queue
- event popup with image
- discovery cards
- immediate and delayed effects

### Phase 9: Factions

Implement:

- three MVP factions
- hidden territories
- visible warning signs
- trust / anger / fear
- simple world phase reactions
- communication popup with portrait/image

### Phase 10: Base Phase and Second Expedition

Implement:

- return to base
- archive knowledge
- time passes
- world reacts
- member recovery
- recruit / request engineer
- start second expedition with inherited knowledge

This completes the Vertical Slice technical target.

---

## 17. Coding Agent Rules

All AI coding agents should follow these rules:

1. Read `docs/gameconcept/exploration_game_concept.md` before changing gameplay systems.
   Then read the relevant specialist document in `docs/gameconcept/`.
2. Read `technical_concept.md` before changing architecture.
3. Read `map_presentation_simplified_concept.md` before changing map presentation.
4. Read `ui_ux_concept.md` before changing UI.
5. Do not hide game rules inside Unity scene scripts, prefabs or MonoBehaviours.
6. Do not directly mutate game state from UI objects.
7. Use commands for player actions.
8. Keep Core logic free of Unity dependencies.
9. Preserve the separation between WorldState, KnowledgeState and PlayerNotes.
10. Prefer data-driven content over hardcoded content.
11. Add or update tests for Core logic changes.
12. Avoid implementing final art, audio or polish before the core loop works.
13. Keep MVP scope small and focused.
14. Do not add 4X, city-builder or survival-crafting mechanics unless explicitly requested.
15. When adding a system, document its purpose and integration point.
16. When adding content, prefer JSON definitions and stable IDs.
17. Do not expose objective world truth to the player UI unless the design says it is confirmed knowledge.
18. Do not build a 4X-style map for the MVP.
19. Normal gameplay should use a flat isometric 2D map without permanent visible hex outlines.
20. Important communication and discoveries should have a portrait, scene image or placeholder visual.

---

## 18. Current Technical Summary

The project will use Unity 6.5 with C#.

The architecture separates pure game simulation from Unity presentation.

The core game state is built around a strict separation of objective world truth, player knowledge and player notes.

The map uses a hidden logical hex grid with a flat isometric 2D presentation layer.

The first implementation should focus on a playable core loop:

> move, scout, read, mark, decide, return, archive, prepare, go farther.

The MVP is not a content-complete game.

The MVP is a technical and design proof that the core loop is fun.
