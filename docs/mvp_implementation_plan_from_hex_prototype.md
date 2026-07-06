# MVP Implementation Plan From Hex Prototype

Status: Draft based on the current Unity hex map prototype and the concept documents in `docs/`.

## Goal

Turn the existing Unity hex map prototype into a playable expedition-game vertical slice.

The MVP should not become a 4X game. The map is the stage for exploration, uncertainty, scouts, notes, faction clues, special locations and returning to base with useful knowledge.

Core MVP feeling:

> The first expedition cannot solve everything, but it makes the second expedition better.

## Current Starting Point

Already available:

- Unity hex map with a 40 x 30 board.
- Strategic/isometric camera.
- Terrain colors and placeholder terrain features.
- Forests, mountains, settlements and landmarks as prefab-capable visual objects.
- Roads, rivers and debug/selection overlays.
- URP configured.
- Direct3D11 configured for Standalone.

Still missing:

- Core game state.
- Movement rules.
- Fog/knowledge model.
- Expedition resources.
- Scout missions and scout reports.
- Player markers and notes.
- Special location logic.
- Event queue.
- Base phase.
- Second expedition using inherited knowledge.

## Architecture Rule

Keep game rules out of Unity scenes and MonoBehaviours.

Use three layers:

- `Game.Core`: pure C# state and rules, no `UnityEngine`.
- `Game.App`: commands/use cases that mutate `GameState`.
- `Game.Unity`: scene, input, map rendering, UI and prefab presentation.

Important state separation:

- `WorldState`: objective truth.
- `KnowledgeState`: what the expedition/archive knows.
- `PlayerNotes`: what the player manually marks or assumes.

The Unity hex map should become a view of this state, not the state itself.

## Vertical Slice Target

Use a dense 25 x 20 playable region first, even though the board can be 40 x 30.

The slice should include:

- Starting base on a safe coast.
- First unknown forest/riverland.
- Warning signs and faction hints.
- Ravine or broken bridge that requires an engineer later.
- Abandoned camp from an earlier expedition.
- Marked grave or taboo location.
- Coastal/river faction with partial information.
- Border Warden faction mostly represented through signs and warnings.
- Hinted deeper mystery such as a sealed ruin, gate or wall.

## Implementation Steps

### 1. Create Core Solution Structure

Add:

- `Game.Core`
- `Game.App`
- `Game.Tests`
- Unity-side bridge under `Game.Unity` or `UnityHexMapView/Assets/Scripts`

Acceptance:

- Core tests run without Unity.
- Unity presentation can reference the application layer, but Core does not reference Unity.

### 2. Extract Hex Logic Into Core

Implement:

- `HexCoord`
- `HexDirection`
- neighbor lookup
- hex distance
- rectangular map coordinate conversion if needed

Acceptance:

- Tests cover all six neighbors.
- Tests cover distance.
- Tests cover dictionary/key usage.

### 3. Add Map State

Implement:

- `HexMapState`
- `HexTileState`
- terrain/biome enum
- movement flags
- road/river/location references

Acceptance:

- A 40 x 30 map can be generated or loaded.
- Existing Unity map can render from `HexMapState`.
- Terrain is no longer authored only inside `UnityHexMapView`.

### 4. Add Movement Cost Service

Initial rules:

- Plains/grass = 1
- Forest = 2
- Hills = 2
- Swamp/dense forest = 3
- Mountain = 3 or blocked
- Road reduces cost, minimum 1

Acceptance:

- Tests cover terrain costs.
- Tests cover roads.
- Tests cover blocked tiles.

### 5. Add Game State Root

Implement:

- `GameState`
- `WorldState`
- `KnowledgeState`
- `PlayerNotesState`
- `ExpeditionState`
- `BaseState`

Acceptance:

- World truth, knowledge and notes are separate.
- A new game can initialize a tutorial world.

### 6. Connect Unity Map To Core State

Refactor the current map view so it:

- receives `HexMapState`
- renders terrain and feature prefabs from state
- renders fog/knowledge from `KnowledgeState`
- sends selections/commands outward
- keeps debug grid available

Acceptance:

- Existing map still displays.
- Default hex outlines stay hidden.
- Debug grid can be toggled.
- Selected/reachable overlays still work.

### 7. Implement Expedition State

Tutorial expedition:

- 2 scouts
- 2 soldiers/guards
- 2 carriers
- 1 medic
- 1 scholar/mediator
- 0 engineers

Track:

- expedition day
- world day
- movement points
- supplies
- medicine
- morale
- capacity
- member availability/status

Acceptance:

- Expedition initializes with named members.
- UI can display current status.

### 8. Implement Movement Command

Add:

- `MoveExpeditionCommand`
- validation
- movement cost spending
- expedition marker update
- knowledge reveal on movement

Acceptance:

- Selecting adjacent reachable hex allows movement.
- Invalid movement is rejected.
- Movement updates state and map.

### 9. Implement End Day

Add:

- `EndDayCommand`
- MP reset
- supply consumption
- day advancement
- basic world phase hook

Acceptance:

- Day counter advances.
- Supplies decrease.
- UI refreshes from `GameState`.

### 10. Add Fog And Knowledge Visuals

Knowledge states:

- Unknown
- Reported
- Confirmed
- Old/Doubtful later

Acceptance:

- Unknown tiles are visually distinct.
- Movement confirms nearby tiles.
- Scout reports can mark tiles as reported without revealing perfect truth.

### 11. Add Minimal UI Layout

Implement:

- top status bar
- left selected-hex/context panel
- right expedition/scout/report panel
- bottom action bar

Acceptance:

- Player sees day, supplies, morale, MP.
- Selected known hex shows knowledge and notes.
- UI sends commands, it does not mutate state directly.

### 12. Add Player Markers And Notes

Implement:

- `PlayerMapMarkerState`
- `PlayerMapNoteState`
- add/edit note command
- add marker command

Acceptance:

- Player can mark a hex.
- Player can write a note.
- Notes persist after returning to base.

### 13. Implement Scout Missions

Add:

- `ScoutMissionState`
- direction
- duration
- focus
- behavior
- status
- assigned member IDs

Acceptance:

- Scouts become unavailable while assigned.
- Mission has expected return day.
- Invalid scout assignments are rejected.

### 14. Resolve Scouts And Generate Reports

Add:

- deterministic scout risk resolver
- returned / injured / overdue / missing statuses
- `ScoutReportState`
- report text
- reliability
- related hexes
- structured hints

Acceptance:

- Scout can return with imperfect information.
- Report is stored.
- Report does not expose objective truth automatically.

### 15. Add Scout Report UI

Implement:

- report list
- report detail view
- hint list
- create marker from hint

Acceptance:

- Reports are readable.
- Player can turn a hint into a map marker.

### 16. Add Special Locations

Minimum locations:

- marked grave
- ravine / broken bridge
- abandoned camp

Implement:

- `SpecialLocationState`
- discovery state
- inspect actions
- location-specific notes/archive entries

Acceptance:

- Locations appear when discovered.
- Ravine cannot be safely solved without engineer.
- Abandoned camp can produce old unreliable knowledge.

### 17. Add Event Queue

Implement:

- `EventQueueState`
- `EventState`
- event options
- simple effects

Initial events:

- scout overdue
- warning sign
- abandoned camp discovery
- found sealed box or object

Acceptance:

- Systems enqueue events.
- UI displays event popup.
- Option resolution mutates `GameState` through application logic.

### 18. Add Minimal Faction Presence

Implement three active factions for the slice:

- Coastal People / River Village
- Border Wardens
- Hidden Ones

Track:

- contact status
- trust
- anger
- fear
- territory / influence area
- representative roles such as watcher, guard, scout, messenger, trader and leader
- leader access requirement
- authored leverage objects or faction-request items
- leverage item source links from map generation or tutorial map setup
- 5 to 10 authored faction/location offers
- baseline Knowledge for Supplies trade where trading is available
- warning zones
- memories

Acceptance:

- Coastal faction can provide a partial warning.
- Border Warden territory is inferred through signs, not clean map borders.
- Hidden Ones exist far from the base and create a dangerous late-slice pressure.
- Entering any known or hidden faction territory runs a faction reaction check.
- Territory entry can create observation, warning, contact, memory or event outcomes.
- First contact usually happens through guards, scouts, watchers or messengers, not leaders.
- Leader contact is gated by faction attitude, location progress or valuable offers.
- At least one faction can request a concrete object, proof or favor before deeper conversation opens.
- Leverage objects can be found in graves, camps, special locations, scout reports or another faction's possession.
- The tutorial map may hand-place leverage sources, but the data model must support assigning them during future map generation.
- A leverage object definition should record item id, source location/system, interested faction, unlocked offer/dialogue and persistence behavior.
- Trade/contact UI can show curated offers, including Knowledge for Supplies.
- Offers can reveal routes, safe camps, springs, warning interpretations or map fragments.
- Factions remember major territory violations across expeditions.
- Debug ownership overlays can show whether generated faction territories make sense.

### 19. Implement Return To Base

Add:

- `ReturnToBaseCommand`
- expedition status changes to returned
- archive update
- reports and notes persist
- world day continues

Acceptance:

- Returning feels useful, not like failure.
- Knowledge is preserved.

### 20. Add Base Phase

Implement:

- simple base screen
- heal member
- archive reports
- recruit/request engineer
- time passing
- simple world reactions

Acceptance:

- Base actions advance world day.
- Engineer can become available for the second expedition.
- World reaction creates journal/archive/world change entry.

### 21. Start Second Expedition

Add:

- `StartNewExpeditionCommand`
- expedition number increments
- old knowledge and notes remain
- new team can include engineer
- expedition starts from base

Acceptance:

- Player can return to the ravine with better preparation.
- Second expedition feels like continuation, not restart.

## Review Gates

### Gate A: Core State

- Tests pass.
- WorldState / KnowledgeState / PlayerNotes are separate.
- Movement, day advancement and supplies work.

### Gate B: Playable Movement

- Unity map displays from state.
- Expedition moves.
- Fog/knowledge updates.
- Status UI updates.

### Gate C: Scouts

- Scout mission works.
- Scout can return or become overdue.
- Scout report appears.
- Report can create marker.

### Gate D: Legacy

- Return to base works.
- Archive persists.
- World day advances.
- Second expedition starts with old knowledge.

### Gate E: Fun Test

Tester should say:

- I want to know what is beyond the ravine.
- My notes helped.
- Scout reports matter.
- Returning to base felt useful.
- The second expedition felt better prepared.

## Immediate Next Implementation Recommendation

Start with:

1. `Game.Core` project.
2. `HexCoord`.
3. `HexMapState`.
4. `MovementCostService`.
5. `GameState` with `WorldState`, `KnowledgeState` and `PlayerNotes`.
6. Connect current Unity map rendering to this state.

Do not continue polishing map visuals until movement, fog and the first scout-report loop are playable.
