# IMPLEMENTATION_PLAN.md

Implementation plan for the Expedition Exploration Game.

Engine: **Unity 6.5 + C#**

This document translates the design and technical concept into concrete implementation tasks for AI coding agents and human review.

Related documents:

- `docs/exploration_game_concept.md`
- `docs/technical_concept.md`
- `AGENTS.md`
- `CLAUDE.md`
- `UI_UX_CONCEPT.md`
- `HEX_MAP_VISUAL_CONCEPT.md`
- `MAP_PRESENTATION_SIMPLIFIED_CONCEPT.md`

---

## 1. Purpose

The goal is not to build the full game immediately.

The goal is to reach a playable Vertical Slice that proves the core loop:

> move, scout, read, mark, decide, return, archive, prepare, go farther.

The first implementation must stay small.

Do not start with final art, full procedural generation, deep factions or complex events.

The first milestone is a stable playable prototype with:

- hex map
- expedition movement
- fog of war / knowledge
- expedition day loop
- supplies
- scout missions
- scout reports
- map markers and notes
- return to base
- archived knowledge
- second expedition using inherited knowledge

---

## 1A. Revised Map Implementation Strategy

The MVP should not implement a 4X-style map.

The map implementation is split into blocks:

### Block 1: Logical Map Foundation

- hex coordinates
- map data
- terrain types
- knowledge state
- movement costs
- selected hex
- debug grid
- simple placeholder rendering

### Block 2: Basic Isometric Map Presentation

- flat isometric 2D projection
- no permanent visible hex outlines
- simple terrain base layer
- object placement layer
- forest cluster placeholders
- mountain/rock placeholders
- road and river placeholder segments
- expedition marker

### Block 3: UI Overlay

- top status bar
- left context panel
- right scout/report/expedition panel
- bottom action bar

### Block 4: Movement and Knowledge Loop

- movement command
- movement range feedback
- fog/knowledge overlay
- supplies consumption
- day loop
- markers and notes

### Block 5: Scouts and Reports

- scout missions
- report popup with portrait
- report hints
- create markers from hints
- archive reports

### Block 6: Special Locations and Events

- marked grave
- ravine/broken bridge
- abandoned camp
- event popup with image
- discovery cards

### Block 7: Base Phase and Second Expedition

- return to base
- archive knowledge
- time passes
- world reaction
- second expedition

Do not implement polished terrain art before Block 4 is playable.

---

## 1B. Unity 6.5 Implementation Rules

The project now uses Unity 6.5, not Godot.

Core rules:

- Use Unity for presentation, input, UI, scenes and prefabs.
- Keep `Game.Core` free of UnityEngine dependencies.
- Do not put game rules into MonoBehaviours.
- UI and scene scripts send commands to `Game.App`.
- `GameState` remains the authoritative source of truth.
- Unity GameObjects and prefabs are views, not simulation state.
- The MVP map is a hidden logical hex grid with a flat isometric 2D presentation layer.
- Do not build a 4X-style terrain map.
- Do not show permanent visible hex outlines in normal play.

---

## 2. General Agent Workflow

Every task should follow this workflow:

1. Read `AGENTS.md`.
2. Read the relevant sections of `technical_concept.md`.
3. Make the smallest useful change.
4. Add or update tests for `Game.Core` changes.
5. Run tests.
6. Summarize:
   - files changed
   - tests run
   - known risks
   - follow-up work

Do not combine unrelated systems in one task.

Do not implement future systems early because they seem interesting.

---

## 3. Repository Setup Milestone

Goal:

> Create the project skeleton and make sure C#, Unity and tests can coexist cleanly.

### Task 001: Create Repository Folder Structure

Create the folder layout:

```text
docs/
UnityProject/
src/
content/
saves/dev/
```

Move or copy the following documents into `docs/`:

- `exploration_game_concept.md`
- `technical_concept.md`

Keep these files at repository root:

- `AGENTS.md`
- `CLAUDE.md`
- `UI_UX_CONCEPT.md`
- `HEX_MAP_VISUAL_CONCEPT.md`
- `MAP_PRESENTATION_SIMPLIFIED_CONCEPT.md`
- `IMPLEMENTATION_PLAN.md`

Acceptance criteria:

- folder structure exists
- docs are in `docs/`
- root agent files exist
- no gameplay code yet

Suggested model:

- fast/default coding model

---

### Task 002: Initialize Unity 6.5 Project

Create a Unity 6.5 project inside `UnityProject/`.

Acceptance criteria:

- `UnityProject/project.godot` exists
- project opens in Unity 6.5
- an empty `Main.unity` scene can enter Play Mode
- no game logic implemented yet

Suggested model:

- strong coding model
- human may need to open Unity 6.5 and confirm project loads

---

### Task 002U: Initialize Unity 6.5 Project

Create a Unity 6.5 project inside `UnityProject/`.

Recommended initial structure:

```text
UnityProject/
  Assets/
    ExpeditionGame/
      Scenes/
      Scripts/
      Prefabs/
      Art/
      Data/
      Settings/
  Packages/
  ProjectSettings/
```

Acceptance criteria:

- Unity project opens in Unity 6.5
- `Assets/ExpeditionGame/Scenes/Main.unity` exists
- empty Main scene can enter Play Mode
- no gameplay implemented yet
- no game rules placed in MonoBehaviours

Suggested model:

- strong coding model

Human review:

- open Unity 6.5 and confirm the project loads

---

### Task 003: Create C# Solution and Projects

Create C# projects:

```text
src/Game.Core/
src/Game.App/
src/Game.Tests/
```

Suggested project types:

- `Game.Core`: class library
- `Game.App`: class library
- `Game.Tests`: test project

Acceptance criteria:

- solution builds
- `Game.Tests` references `Game.Core`
- `Game.App` references `Game.Core`
- test runner works with one placeholder test

Suggested model:

- strong coding model

---

### Task 004: Add .gitignore and Basic Tooling Files

Add `.gitignore` suitable for:

- Unity
- C#
- build outputs
- local saves
- IDE files

Acceptance criteria:

- build outputs ignored
- `.UnityProject/` cache ignored
- local dev saves ignored
- source, docs and content are not ignored

Suggested model:

- fast/default coding model

---

## 4. Core Hex System Milestone

Goal:

> Build a tested hex coordinate foundation before any game visuals depend on it.

### Task 005: Implement HexCoord

Implement axial hex coordinates:

```csharp
public readonly record struct HexCoord(int Q, int R);
```

Add methods or helper service for:

- neighbor lookup
- all six directions
- distance between two hexes
- equality / dictionary use

Acceptance criteria:

- `HexCoord` exists in `Game.Core/Hex`
- tests cover all six neighbors
- tests cover distance calculation
- tests cover dictionary key usage if needed

Suggested model:

- default strong coding model

---

### Task 006: Implement HexDirection

Create enum:

```csharp
public enum HexDirection
{
    North,
    NorthEast,
    SouthEast,
    South,
    SouthWest,
    NorthWest
}
```

Acceptance criteria:

- direction enum exists
- neighbor lookup uses it
- tests verify direction offsets

Suggested model:

- fast/default coding model

---

### Task 007: Implement HexMapState and HexTileState

Implement initial map state models:

- `HexMapState`
- `HexTileState`

Fields should match the technical concept as closely as practical.

Acceptance criteria:

- map stores width and height
- map stores tiles by `HexCoord`
- tile has biome, terrain, movement cost, hazard level
- tests can create a small map and retrieve tiles

Suggested model:

- default strong coding model

---

### Task 008: Implement Movement Cost Service

Create a simple movement cost service.

Initial rules:

- plains = 1
- forest = 2
- hills = 2
- swamp/dense forest = 3
- mountain = 3 or blocked if marked blocked
- road reduces cost, minimum 1

Acceptance criteria:

- service exists in `Game.Core`
- tests cover basic terrain costs
- tests cover road reduction
- tests cover blocked tile behavior

Suggested model:

- default strong coding model

---

## 5. Initial Game State Milestone

Goal:

> Create the minimum persistent game state needed for movement, fog and expedition day flow.

### Task 009: Implement GameState Root

Implement:

- `GameState`
- `GameMode`
- `WorldState`
- `KnowledgeState`
- `EventQueueState`

Keep unused fields simple but present where appropriate.

Acceptance criteria:

- `GameState` can be instantiated
- `CurrentWorldDay` exists
- `CurrentExpeditionDay` exists
- `WorldState` and `KnowledgeState` are separate
- tests verify initial mode and days

Suggested model:

- strong coding model

---

### Task 010: Implement Knowledge States

Implement:

- `HexKnowledgeState`
- `KnowledgeLevel`
- `ReliabilityLevel`
- initial knowledge update methods

Acceptance criteria:

- unknown hexes can exist
- reported hexes can be created
- confirmed hexes can be created
- last confirmed world day can be stored
- tests prove `WorldState` and `KnowledgeState` are separate

Suggested model:

- strong coding model

---

### Task 011: Implement PlayerMapNoteState

Implement map notes and marker enum.

Acceptance criteria:

- player can create note linked to one hex
- player can create free text note
- player can create marker-only note
- note stores creation world day
- tests cover marker creation

Suggested model:

- default strong coding model

---

## 6. Expedition Core Milestone

Goal:

> The expedition exists, has people, has supplies, spends movement points and advances days.

### Task 012: Implement ExpeditionMemberState

Implement:

- `ExpeditionMemberState`
- `ExpeditionRole`
- `MemberCondition`
- `InjuryLevel`

Acceptance criteria:

- named members can be created
- roles exist
- star level is stored
- condition and injury are stored
- tests create the tutorial expedition roster

Suggested model:

- default strong coding model

---

### Task 013: Implement ExpeditionState and Resources

Implement:

- `ExpeditionState`
- `ExpeditionResources`
- `ExpeditionStatus`

Acceptance criteria:

- expedition has current position
- expedition has members
- expedition has movement points
- expedition has supplies, medicine, morale, capacity
- tests verify initial expedition state

Suggested model:

- default strong coding model

---

### Task 014: Implement Tutorial Expedition Factory

Create a factory for the first predefined tutorial expedition:

- 2 scouts
- 2 soldiers
- 2 carriers
- 1 medic
- 1 scholar/mediator
- 0 engineers

Acceptance criteria:

- factory creates 8 named members
- role counts are correct
- max expedition size is respected
- engineer is absent by design
- tests verify roster

Suggested model:

- default strong coding model

Human design review:

- confirm names are acceptable or replace later

---

### Task 015: Implement Expedition Movement

Implement movement command / service:

- validate target neighbor
- validate movement points
- apply movement cost
- move expedition
- reduce remaining movement points
- update confirmed knowledge around expedition

Acceptance criteria:

- expedition can move to adjacent reachable tile
- movement points decrease
- expedition cannot move too far in one command
- expedition cannot enter blocked tile
- confirmed knowledge updates
- tests cover all cases

Suggested model:

- strong coding model

---

### Task 016: Implement End Day

Implement day advancement:

- reset movement points
- increment expedition day
- increment world day
- consume supplies
- apply basic morale warning if supplies low

Acceptance criteria:

- expedition day increases
- world day increases
- supplies decrease by party size
- movement points reset
- tests cover day advancement

Suggested model:

- strong coding model

---

## 7. Unity Map Prototype Milestone

Goal:

> Show the core map visually in Unity without final art.

### Task 017: Create Main Scene and Runtime Bootstrap

Create:

- `Main.unity`
- a Unity bootstrap script
- minimal connection to application layer or placeholder game state

Acceptance criteria:

- project runs
- main scene loads
- basic empty UI visible
- no simulation rules in scene script

Suggested model:

- strong coding model
- human should run Unity and confirm

---

### Task 018: Render Placeholder Hex Map

Render a small test map, such as 10 x 8 hexes.

Use simple placeholder shapes or basic GameObjects.

Acceptance criteria:

- hexes are visible
- map layout visually resembles hex grid
- no final art required
- clicking or hovering can identify a hex coordinate
- rendering reads from map state where possible

Suggested model:

- strong coding model

Human review:

- confirm layout orientation is understandable

---

### Task 018A: Add Road, River and Edge Feature Data

Use `HEX_MAP_VISUAL_CONCEPT.md`.

Add data structures for connected landscape features.

Implement:

- `RoadConnectionState`
- `RiverConnectionState`
- `EdgeBarrierState`

Acceptance criteria:

- roads can connect two neighboring hexes
- rivers can connect two neighboring hexes
- ridges/cliffs/ravines can be represented as edge barriers
- these features can be marked as known/unknown to player
- tests verify road and river connections between neighboring hexes
- tile-level `HasRoad` / `HasRiver` may remain as helper flags, but connected features are the source of truth for visuals

Suggested model:

- strong coding model

---

### Task 018B: Render Connected Roads and Rivers

Replace dot-based road/river visualization with connected segments.

Acceptance criteria:

- roads render as connected brown path segments
- rivers render as connected blue path segments
- segments visually connect neighboring hexes
- unknown roads/rivers are not shown
- reported roads/rivers can be shown as dashed or faint if implemented
- confirmed roads/rivers are shown clearly
- no simulation logic is placed in rendering code

Suggested model:

- strong coding model

Human review:

- confirm roads and rivers can be visually followed across the map

---

### Task 018C: Render Edge Barriers

Render ridges, cliffs, ravines or walls as edge features.

Acceptance criteria:

- edge barrier can appear between two neighboring hexes
- barrier visually communicates movement restriction
- known barriers are visible
- unknown barriers are hidden
- selected hex panel can mention known barrier if relevant

Suggested model:

- strong coding model

---

### Task 018D: Improve Forest and Mountain Readability

Improve placeholder terrain visuals.

Acceptance criteria:

- forest hexes read as forest regions, not just flat green
- mountain/ridge hexes read as barriers or high terrain
- hills are visually distinct from plains
- dense forest/swamp feels visually heavier
- hex grid remains readable

Suggested model:

- default strong coding model

Human review:

- confirm map is still readable and not visually noisy

---

### Task 018R: Reset Map Visual Target to Simple Isometric 2D

Use `MAP_PRESENTATION_SIMPLIFIED_CONCEPT.md`.

Refactor or guide the map presentation toward a simple flat isometric 2D view.

Acceptance criteria:

- normal gameplay does not show permanent hex outlines
- debug grid can still be toggled
- hexes remain the logical map structure
- terrain cells are flat
- no height or elevation rendering is introduced
- forests and mountains are represented as placed placeholder objects
- roads and rivers are represented as simple connected segments, not dots
- map remains readable before any final art exists

Suggested model:

- strong coding model

Human review:

- confirm the map no longer feels like a 4X board
- confirm the hidden hex structure still supports movement planning

---

### Task 019: Display Expedition Marker

Show expedition position on the map.

Acceptance criteria:

- expedition marker appears on current hex
- marker moves when state changes
- marker is visual only
- no game rule lives inside marker node

Suggested model:

- default strong coding model

---

### Task 020: Basic Camera and Selection

Implement simple map camera and selection:

- pan
- zoom if practical
- click/select hex
- show selected hex info panel

Acceptance criteria:

- user can inspect hex coordinates
- selected hex is visually highlighted
- info panel shows biome, movement cost and knowledge state
- no final UI polish required

Suggested model:

- default strong coding model

---

### Task 020B: Add Placeholder Visual Asset System

Use `UI_UX_CONCEPT.md` and `VISUAL_ASSET_TECH_ADDENDUM.md`.

Add a simple visual asset system for portraits, location images, object images and symbols.

Acceptance criteria:

- `VisualAssetDefinition` exists
- visual assets are referenced by stable ID
- missing visual assets fall back to generic placeholders
- placeholder asset folders exist
- at least 10 dummy placeholder assets exist or are referenced
- no simulation logic depends on visual assets

Suggested model:

- default strong coding model

Human review:

- confirm placeholder style is acceptable for MVP

---

### Task 020C: Add Communication Popup With Portrait/Image

Create a reusable communication popup.

It should support:

- title
- portrait or scene image
- speaker name
- speaker role/faction
- main text
- option buttons
- unavailable option reason

Acceptance criteria:

- scout reports can use the popup or a similar visual report panel
- faction interactions can use the popup later
- event popups can show an image even without a speaker
- options are sent as commands/effects, not handled as simulation inside UI
- no hidden `WorldState` truth is revealed by images

Suggested model:

- strong coding model

Human review:

- confirm popup feels personal and readable

---

### Task 020D: Add Discovery Card With Image

Create a reusable discovery card for special locations and important objects.

Acceptance criteria:

- discovery card shows image/placeholder
- card shows visible name or description
- card shows known clues
- card can create marker or archive entry if implemented
- card does not reveal hidden purpose

Suggested model:

- strong coding model

---

### Task 033A: Add Archive Thumbnails

Extend archive cards with thumbnails.

Acceptance criteria:

- archive entries can reference `ThumbnailAssetId`
- scout report archive entries can show scout portrait
- location archive entries can show location image
- object archive entries can show object image
- missing thumbnails use fallback placeholder

Suggested model:

- default strong coding model

---

## 8. Movement Playable Prototype Milestone

Goal:

> The player can move the expedition around a fogged hex map and advance days.

### Task 021: Connect Move Command to UI

Allow player to select an adjacent hex and move there.

Acceptance criteria:

- selecting adjacent reachable hex shows move option
- move command is sent to application layer
- movement updates GameState
- UI refreshes after state change
- invalid moves are rejected with message

Suggested model:

- strong coding model

---

### Task 022: Implement Fog / Knowledge Visuals

Display:

- unknown hexes
- reported hexes
- confirmed hexes

Initial visuals can be simple colors or opacity.

Acceptance criteria:

- unknown hexes are visually distinct
- confirmed hexes are visible
- movement updates confirmed area
- knowledge visuals read from `KnowledgeState`

Suggested model:

- strong coding model

---

### Task 023: Display Expedition Status UI

Show:

- expedition day
- world day
- remaining movement points
- supplies
- medicine
- morale
- party size

Acceptance criteria:

- UI updates after movement
- UI updates after end day
- values come from GameState

Suggested model:

- default strong coding model

---

### Task 024: Implement End Day Button

Add UI button to end day.

Acceptance criteria:

- clicking button sends `EndDayCommand`
- movement points reset
- supplies decrease
- expedition day and world day increase
- UI refreshes

Suggested model:

- default strong coding model

Human playtest:

- confirm that moving and ending days is understandable

---

## 9. Scout System Milestone

Goal:

> Scouts can be sent away, later return or become overdue, and produce imperfect reports.

### Task 025: Implement ScoutMissionState

Implement scout mission model and enums:

- direction
- focus
- behavior
- status
- expected return day
- scout member IDs

Acceptance criteria:

- scout mission can be created
- assigned members become unavailable/on mission
- expected return world day is calculated
- tests cover mission creation

Suggested model:

- strong coding model

---

### Task 026: Implement SendScoutCommand

Implement command:

- choose scout(s)
- choose direction
- choose duration
- choose focus
- choose behavior

Validation:

- selected member must be scout
- scout must be available
- duration must be allowed
- one-person and two-person teams allowed

Acceptance criteria:

- valid scout mission starts
- invalid scout mission is rejected
- scout member state updates
- tests cover validation

Suggested model:

- strong coding model

---

### Task 027: Implement Basic Scout Resolution

At expected return day, resolve scout mission.

Initial possible outcomes:

- returned
- overdue
- returned injured
- missing

Use simple risk logic first.

Acceptance criteria:

- scout mission progresses during end day/world phase
- scout can return
- scout can become overdue
- overdue is not treated as dead
- tests cover all statuses with deterministic random seed

Suggested model:

- high-reasoning or strong coding model

---

### Task 028: Implement ScoutReportState

Generate a simple scout report when a scout returns.

Report should include:

- title
- body text
- reliability
- related hexes
- structured hints

Acceptance criteria:

- returned scout creates report
- report is stored in KnowledgeState
- hints can refer to hexes
- tests verify report creation

Suggested model:

- strong coding model

---

### Task 029: Scout Report UI

Create UI panel to show scout reports.

Acceptance criteria:

- pending/new scout reports can be opened
- report body text visible
- hints visible
- related hexes listed
- no automatic perfect map reveal

Suggested model:

- default strong coding model

Human review:

- confirm reports feel readable and not too technical

---

## 10. Markers, Notes and Archive Milestone

Goal:

> The player can record uncertain information and preserve it across expedition phases.

### Task 030: Create Marker From Scout Hint

Add UI action:

- create marker from scout report hint

Acceptance criteria:

- player can select hint
- marker appears on map
- marker links back to report
- marker lives in PlayerNotes / KnowledgeState, not WorldState

Suggested model:

- strong coding model

---

### Task 031: Add Free Text Notes

Allow player to add a free text note to a selected hex.

Acceptance criteria:

- note text can be entered
- note appears in selected hex info
- note persists during session
- note is stored separately from objective world truth

Suggested model:

- default strong coding model

---

### Task 032: Basic Expedition Journal

Create chronological journal entries.

Initial entries:

- expedition started
- moved
- scout sent
- scout returned/overdue
- supplies low
- expedition returned

Acceptance criteria:

- journal records events by expedition day/world day
- journal can be opened in UI
- tests cover journal entry creation if in Core

Suggested model:

- default strong coding model

---

### Task 033: Basic Archive

Implement archive storage for:

- scout reports
- journal entries
- discovered locations
- faction notes later

Acceptance criteria:

- scout reports can be archived
- archived reports remain available after return to base
- archive is separate from active event queue

Suggested model:

- strong coding model

---

## 11. Special Location Prototype Milestone

Goal:

> Add the first meaningful decision locations.

### Task 034: Implement SpecialLocationState

Implement model if not already present.

Acceptance criteria:

- location has ID, type, state and coordinate
- location can be discovered
- state changes are recorded
- tests cover discovery and state change

Suggested model:

- strong coding model

---

### Task 035: Add Marked Grave

Create first special location:

- marked grave
- visible clue
- actions: leave, mark, inspect, disturb/open if implemented

Acceptance criteria:

- grave appears when discovered
- player can mark it
- player can inspect it
- action creates journal/archive entry
- disturbing it can later affect faction memory

Suggested model:

- strong coding model

Human design review:

- confirm tone and wording

---

### Task 036: Add Ravine / Broken Bridge

Create obstacle location:

- ravine or broken bridge
- cannot be safely solved without engineer
- can be marked for later

Acceptance criteria:

- location blocks or discourages passage
- without engineer, bridge project unavailable
- player can add marker/note
- later engineer can unlock project

Suggested model:

- strong coding model

---

### Task 037: Add Abandoned Camp

Create abandoned earlier expedition camp.

Acceptance criteria:

- camp can be discovered
- player can search it
- search produces old report or journal entry
- old report has limited reliability
- archive stores recovered information

Suggested model:

- strong coding model

---

## 12. Event System Milestone

Goal:

> Add event queue and simple player choices.

### Task 038: Implement EventQueueState and EventState

Implement event queue if not already complete.

Acceptance criteria:

- systems can enqueue event
- UI can display next event
- event can be resolved
- resolved event stored
- tests cover enqueue/resolve

Suggested model:

- strong coding model

---

### Task 039: Implement Event Options and Effects

Implement basic event option resolution.

Initial effects:

- change supplies
- change morale
- add note/archive entry
- change location state

Acceptance criteria:

- option can be selected
- effects apply to GameState
- delayed effects can be represented, even if not fully used
- tests cover effects

Suggested model:

- high-reasoning or strong coding model

---

### Task 040: Add Spontaneous Found Box Event

Implement one spontaneous one-off event.

Acceptance criteria:

- event can trigger in appropriate region
- player can open, leave, mark or take box if options implemented
- outcome can add note, archive entry, resource change or future flag
- event does not repeat

Suggested model:

- strong coding model

Human design review:

- confirm event feels mysterious but not random

---

## 13. Faction Prototype Milestone

Goal:

> Add the first faction presence without building full diplomacy.

### Task 041: Implement FactionState

Implement faction model if not already present.

Acceptance criteria:

- faction has ID and contact status
- trust, anger and fear exist
- territory and warning zones exist
- memories can be added
- tests cover memory and value changes

Suggested model:

- high-reasoning or strong coding model

---

### Task 042: Add Coastal People / River Villages

Implement first friendly/open faction.

Acceptance criteria:

- faction exists in world state
- some tiles have influence
- first contact event or simple interaction can happen
- faction can provide warning or rumor
- known faction note appears after contact

Suggested model:

- strong coding model

Human design review:

- confirm wording avoids colonial framing

---

### Task 043: Add Border Wardens Warning Signs

Implement second faction as indirect presence.

Acceptance criteria:

- warning zones exist
- carved posts / warning markers can be discovered
- scout reports can mention possible border
- entering warning zone can create memory or event

Suggested model:

- high-reasoning or strong coding model

---

## 14. Base Phase Milestone

Goal:

> Returning to base preserves knowledge, advances time and prepares the next expedition.

### Task 044: Implement ReturnToBaseCommand

Acceptance criteria:

- active expedition can return to base
- game mode changes to BasePhase
- expedition status becomes Returned
- reports and notes remain
- expedition history entry created

Suggested model:

- strong coding model

---

### Task 045: Implement BaseState and Base Actions

Implement simple base phase model.

Initial actions:

- archive reports
- heal member
- recruit member
- request engineer

Acceptance criteria:

- base actions have start and completion day
- world day advances during base actions
- completed action updates base or members
- tests cover base time advancement

Suggested model:

- high-reasoning or strong coding model

---

### Task 046: Implement Simple World Reaction During Base Phase

Implement one or two simple reactions:

- Border Wardens renew warnings
- Coastal People report smoke
- abandoned camp changes state

Acceptance criteria:

- reaction happens after time passes
- reaction creates journal/archive/world change entry
- reaction can be known or unknown to player
- tests cover deterministic reaction

Suggested model:

- high-reasoning or strong coding model

---

### Task 047: Start Second Expedition

Allow a second expedition to start.

Acceptance criteria:

- old map knowledge remains
- old player notes remain
- old reports remain in archive
- new expedition number increments
- player can include engineer if unlocked
- expedition starts from base

Suggested model:

- strong coding model

Human playtest:

- confirm second expedition feels like continuation, not restart

---

## 15. Vertical Slice Assembly Milestone

Goal:

> Connect the systems into a short playable slice.

### Task 048: Create Vertical Slice Scenario Data

Create a handcrafted small scenario.

Suggested map size:

- 25 x 20 later
- 10 x 8 or 15 x 10 acceptable for first internal prototype

Must include:

- base
- coast/river
- forest or hills
- marked grave
- ravine/broken bridge
- abandoned camp
- friendly faction presence
- border warning signs

Acceptance criteria:

- scenario loads from code or JSON
- map is deterministic
- key locations are reachable
- first expedition cannot solve everything

Suggested model:

- strong coding model

Human design review required.

---

### Task 049: Vertical Slice UI Pass

Create basic screens/panels:

- map
- selected hex info
- expedition status
- scout report
- journal/archive
- event popup
- base screen

Acceptance criteria:

- all core interactions are accessible
- placeholder visuals acceptable
- no final art required
- no hidden simulation logic in UI

Suggested model:

- strong coding model

Human playtest required.

---

### Task 050: First Playable Internal Build

Create first internal playable build.

Acceptance criteria:

- player can start expedition
- move for multiple days
- send scout
- receive report or overdue result
- create marker/note
- discover at least one special location
- return to base
- start second expedition with inherited knowledge

Suggested model:

- strongest available coding/agentic model for integration
- cross-model review recommended

Human playtest required.

---

## 16. Review Gates

Do not proceed to larger systems before these gates pass.

### Gate A: Core State Gate

Required:

- tests pass
- GameState / WorldState / KnowledgeState separation works
- movement works
- day advancement works
- supplies work

### Gate B: Playable Movement Gate

Required:

- Unity map displays correctly
- expedition moves
- fog/knowledge updates
- UI displays status

### Gate C: Scout Gate

Required:

- scout missions work
- scout can return or become overdue
- report appears
- report can create marker

### Gate D: Legacy Gate

Required:

- return to base works
- archive persists
- world day advances
- second expedition starts with old knowledge

### Gate E: Fun Test Gate

Required human feedback:

- movement is understandable
- fog creates curiosity
- scout reports feel useful
- notes feel worthwhile
- return to base feels useful, not failure
- second expedition feels better prepared

---

## 17. First Agent Prompt Suggestion

Use this as the first implementation prompt:

```text
Read AGENTS.md and docs/technical_concept.md.

Start with Task 001 through Task 004 from IMPLEMENTATION_PLAN.md.

Create the repository folder structure, initialize the Unity 6.5 C# project placeholder if possible, create the C# solution with Game.Core, Game.App and Game.Tests, and add a suitable .gitignore.

Do not implement gameplay yet.

After completing the task, summarize files changed, tests run and any manual steps needed in Unity.
```

If the agent cannot initialize Unity directly, split the task:

```text
Create the repository structure and C# solution first. Leave a clear TODO for manual Unity project initialization.
```

---

## 18. Current Priority

Current priority:

> Build a boring but correct foundation before building interesting systems.

The first exciting moment should come only after movement, fog and scout reports work.

Do not skip ahead to factions, events or special locations before the movement/scout/knowledge loop exists.

---

## 19. Base Camp Screen — Deferred Systems (post-MVP roadmap)

The full-screen base-camp screen (`UnityHexMapView/Assets/UI/BaseCampScreen.uxml` +
`BaseCampScreenController.cs`) was built with six tabs. The **first increment** wired the tabs that
map onto existing systems — **Team** (roster + rich person sheet + composition), **base actions**
(heal / recruit / request-engineer / prepare-supplies / advance-time), **Aufbruch** (start the next
expedition), and **Fraktionen / Archiv** (read-only). The four systems below are the tabs/features
that were intentionally left as **non-functional layout placeholders**; record them here so they are
not forgotten.

Shared constraints: keep rules in `Game.Core`, actions as commands in `Game.App`, Unity only
renders + sends commands; keep everything deterministic and add `tests/Game.Tests` coverage; treat
costs/day-counts as tunable placeholders (game-feel, human-owned).

---

### Task 051: Base Upgrade Tree ("Basis ausbauen") — ✅ Done

Spend Knowledge Points on persistent base upgrades, grouped in categories (Medizin, Werkstatt,
Kartografie, Vorräte, Unterkünfte), with built / available / locked (prerequisite) states.

Example effects:

- Feldlazarett — faster/cheaper healing between expeditions
- Trägerunterkünfte / Baracken — grow the porter / soldier stock (feeds Task 053)
- Kartentisch / Signalturm — better scout reports / +1 scout range
- Vorratskeller / Räucherei — higher supply cap / slower spoilage

Current state:

- the "Basis ausbauen" tab shows the mockup layout but does nothing.

Suggested model additions:

- `BaseUpgradeState` (id, category, cost, isBuilt, prerequisiteIds) held on `BaseState`
- `StartUpgradeCommand` — spend KP, mark built, expose effect hooks other systems read

Acceptance criteria:

- upgrades persist, cost Knowledge Points, and gate on prerequisites
- at least one effect is observable (e.g. heal cost/time or stock size)
- deterministic; tests cover purchase, prerequisite gating and effect

Suggested model: high-reasoning or strong coding model.

---

### Task 052: Knowledge Evaluation Queue ("Wissen auswerten") — ✅ Done

Unsecured field discoveries enter an evaluation queue at the base; a limited number of "Auswerter"
process items over base days into archived **Insights** plus Knowledge Points — replacing today's
lump-sum securing of all unsecured knowledge on return.

Current state:

- the "Wissen auswerten" tab shows the queue + insights layout but does nothing
- `CompleteExpeditionCommand` currently converts unsecured knowledge to KP in a single lump

Suggested model additions:

- `EvaluationQueueState` (items with source, progress, eta) + evaluator capacity on `BaseState`
- `AdvanceBaseTimeCommand` advances item progress; a completed item yields an archive Insight + KP

Acceptance criteria:

- items progress deterministically as base time passes; capacity limits parallel evaluation
- completion adds an archive entry and awards KP (dedup by stable source id)
- tests cover progress, capacity and completion rewards

Suggested model: high-reasoning or strong coding model.

---

### Task 053: Unit Stock + Resource Loadout ("Aufbruch") — ✅ Done

Träger and Soldaten become a countable base stock (grown by Task 051 upgrades), each with a
condition (frisch / erschöpft). On departure the player picks which units and how many rations /
medicine to take; readiness (Traglast, Verpflegung, Verteidigung, Marschtempo) is derived and can
gate the start.

Implemented:

- `BaseUnitState` / `BaseUnitStockState` on `BaseState` (porters + soldiers with condition);
  seeded in `TutorialGameFactory`; `GrowPorterStock` / `GrowSoldierStock` upgrades add units.
- `ExpeditionReadiness.Compute(...)` in Core derives carry capacity, load/overload, food days,
  defense and slow-march from members + selected units + rations + medicine.
- `StartNewExpeditionCommand.Execute(game, memberIds, unitIds, rations, medicine)` loadout
  overload: clamps rations/medicine to base budgets, rejects overload, and starts the expedition
  with the derived supplies/medicine/capacity. Facade `GameApplication.StartNewExpedition(...)` +
  `ComputeReadiness(...)`.
- Unity: seam `GetUnitStockForUi` / `ComputeReadinessForUi` / `RequestStartLoadoutExpeditionFromUi`;
  the "Aufbruch" tab has live unit grids, resource steppers and readiness stats.
- Tests: `BaseLoadoutCommandTests` (loadout start, overload rejection, ration clamping, readiness,
  Baracken stock growth).

Suggested model: high-reasoning or strong coding model.

---

### Task 054: Typed, Filterable Archive

Replace the flat string `BaseState.ArchiveEntries` with typed archive entries so the Archiv tab can
filter and search, and the read-only display carries meaning.

Current state:

- the Archiv tab lists raw archive strings + scout-report titles; the filters are static

Suggested model additions:

- `ArchiveEntryState` (id, kind = Bericht/Brief/Erkenntnis/Vertrag/Notiz, title, source, worldDay,
  reliability); `BaseState` stores these; populate from scout reports, discoveries, contracts, notes
- keep a compatibility path so existing plain-string archive writing still works during migration

Acceptance criteria:

- entries are typed and carry source / world day / reliability
- the tab filters by kind and searches by text
- existing archive text migrates or coexists; tests cover typing and filtering

Suggested model: high-reasoning or strong coding model.
