# AGENTS.md

Repository-level instructions for AI coding agents working on this project.

These instructions apply to Codex, Claude Code and other coding agents unless a tool-specific file says otherwise.

---

## Required Reading

Before changing gameplay systems, read:

- `docs/exploration_game_concept.md`
- `docs/technical_concept.md`
- `docs/map_presentation_simplified_concept.md` before changing map presentation
- `docs/ui_ux_concept.md` before changing UI, map panels, popups or interaction flow
- `docs/visual_asset_tech_addendum.md` before changing portraits, event images, archive thumbnails or visual asset references

---

## Core Project Rules

- The project uses **Unity 6.5 + C#**.
- Keep simulation logic separate from Unity presentation.
- Do not hide game rules inside Unity scenes, prefabs, GameObjects, MonoBehaviours or UI scripts.
- Do not mutate `GameState` directly from UI objects.
- Use commands for player actions.
- Preserve the separation between `WorldState`, `KnowledgeState` and `PlayerNotes`.
- Prefer data-driven content over hardcoded content.
- Keep MVP scope focused on the Vertical Slice.
- Do not add 4X, city-builder, survival-crafting or conquest mechanics.
- Do not expose objective world truth to the player UI unless the design says it is confirmed knowledge.
- Follow `docs/ui_ux_concept.md` for UI layout, information hierarchy and interaction flow.
- Important communication and discoveries should have a portrait, scene image or placeholder visual.
- Do not build a 4X-style map for the MVP.
- Normal gameplay should use a flat isometric 2D map without permanent visible hex outlines.
- Hexes are the logical data structure, not the default visual style.
- Roads and rivers should be visually traceable and not represented as dots.
- Do not add final art, audio polish or large content libraries before the core loop works.

---

## Architecture

The project uses three layers:

- `Game.Core`: pure C# simulation and data models
- `Game.App`: commands, controllers and use cases
- `Game.Unity`: Unity scenes, prefabs, MonoBehaviours, UI, input and visuals

Unity presentation code may call the application layer.

`Game.Core` must not depend on Unity.

---

## Core Technical Principles

### Truth / Knowledge / Notes Separation

This separation is mandatory:

```text
WorldState      = objective truth
KnowledgeState  = what the expedition/archive knows
PlayerNotes     = what the player personally marks or assumes
```

Do not collapse these into one map state.

### Command-Based Actions

Player actions should flow through commands such as:

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
StartNewExpeditionCommand
```

UI objects must send commands. They must not directly implement simulation rules.

### Event Queue

Systems should create events and place them in the event queue.

The UI displays events.

The UI does not own event logic.

---

## Simplified Map Presentation Rules

The MVP must not become a full 4X-style map.

Follow these rules:

- Hexes are the logical data structure, not the default visual style.
- Normal gameplay should use a flat isometric 2D map.
- Do not show permanent visible hex outlines in normal play.
- Keep hex grid display as a debug or temporary planning overlay only.
- Do not implement terrain height or sculpted 3D terrain for MVP.
- Forests, mountains, hills, roads, rivers and locations should be represented as simple placed objects, sprites or line segments.
- Roads and rivers should be visually traceable and not represented as dots.
- Build map implementation in blocks: logical map first, map presentation second, UI overlay third.
- Do not polish terrain art before the movement and knowledge loop works.

---

## UI Work Rules

When changing UI:

- keep the map as the main stage
- use top status bar, left context panel, right report/expedition panel and bottom action bar as the target layout
- do not put simulation rules in Unity UI scripts
- clearly distinguish Unknown, Reported, Confirmed, Old, Doubtful and Lost information
- make scout reports readable
- make marker/note creation fast
- use portraits/images/placeholders for important communication and discoveries
- do not reveal hidden `WorldState` data through debug-like UI unless explicitly in developer mode

---

## Unity Work Rules

Unity-specific scripts may:

- read view models
- display state
- open/close panels
- handle input
- send commands
- instantiate visual prefabs
- resolve visual asset IDs to sprites/prefabs

Unity-specific scripts must not:

- own authoritative game state
- implement core game rules
- directly change faction trust/anger/fear
- directly consume supplies
- directly resolve scout outcomes
- directly resolve event consequences
- reveal hidden `WorldState` data

Use Unity scenes and prefabs as presentation.

Use `GameState` as the source of truth.

---

## Model Selection Policy

Model names and availability change over time. Use the best currently available model in the relevant class, not an outdated hardcoded model name.

Prefer tool aliases where available.

### Recommended Model Classes by Task

| Task | Recommended model class |
|---|---|
| Architecture decisions, core data model changes, save/load design, procedural generation design, risk system design | Highest-reasoning model available |
| Long-running autonomous implementation across several files | Strong agentic coding model |
| Normal feature implementation with tests | Default strong coding model |
| Small mechanical edits, formatting, renames, simple JSON content additions | Fast/low-cost model if available |
| Code review, architecture review, bug investigation | High-reasoning or strong review-capable coding model |
| UI polish, text layout, wording cleanup | Standard coding/chat model is acceptable |
| Final decision on design scope, game feel or MVP priorities | Human owner decision required |

### Cross-Model Review

When possible, use a different model family for review than for implementation.

Use cross-model review especially for:

- save/load
- core state mutation
- procedural generation
- event effects
- risk resolution
- faction memory
- legacy persistence
- Unity scene/prefab architecture that touches core flow

### Escalation Rules

Escalate to a stronger model if:

- the task touches multiple core systems
- the task changes persistent data structures
- the task changes save format
- the task changes the meaning of player knowledge
- the task causes nondeterministic bugs
- tests pass but behavior feels wrong
- the agent is uncertain about architecture
- the task affects the Vertical Slice core loop

### Human Review Required

Human review is required before merging changes that affect:

- design scope
- MVP priorities
- game feel
- player-facing interpretation mechanics
- save compatibility
- procedural generation rules
- major UI workflow
- faction behavior model
- special location consequences
- Unity scene/prefab structure used by multiple systems

---

## Testing

When changing `Game.Core`, add or update tests.

Prioritize tests for:

- hex coordinates and movement
- expedition day flow
- supply consumption
- scout missions
- overdue scout state
- knowledge state updates
- map notes and markers
- faction state changes
- special location state changes
- event resolution
- base phase time advancement
- save/load roundtrip

Do not rely on manual Unity testing for core logic that can be unit-tested.

Unity Play Mode tests may be used for presentation integration later, but Core correctness should not depend on them.

---

## MVP Priorities

Build toward the Vertical Slice:

1. Logical hex map
2. Simple flat isometric map presentation
3. UI overlay
4. Expedition movement
5. Turn/day system
6. Supplies and return pressure
7. Scout missions and reports
8. Map markers and notes
9. Special locations
10. Factions
11. Base phase
12. Second expedition with inherited knowledge

The Vertical Slice exists to test whether the core loop is fun.

The first expedition should return with incomplete but useful knowledge, and the second expedition should feel meaningfully better prepared.

---

## Do Not Do Yet

- final art
- complex combat
- full procedural world generator
- procedural special location generator
- deep diplomacy
- large tech tree
- base building economy
- multiplayer
- WebGL export
- mobile support
- controller support
- seasons
- large content library
- non-human faction implementation
- full supernatural system
- 4X-style terrain map
- visible permanent hex-grid visual style
- terrain height system

---

## Work Log Requirement

When an agent performs a task, it should record or state:

- model/tool used
- files changed
- tests run
- known risks
- follow-up recommendations

This can be in the PR description, commit message or task summary.
