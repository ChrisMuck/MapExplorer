# CLAUDE.md

This project uses `AGENTS.md` as the main repository instruction file.

Before making changes, read:

- `AGENTS.md`
- `docs/exploration_game_concept.md`
- `docs/technical_concept.md`
- `docs/map_presentation_simplified_concept.md`
- `docs/ui_ux_concept.md` before UI work
- `docs/visual_asset_tech_addendum.md` before visual asset work

Follow the architecture and MVP scope defined there.

---

## Claude Code Model Guidance

Prefer model aliases over pinned model names.

Suggested usage:

- `sonnet`: daily coding tasks and normal feature implementation
- `opus`: complex reasoning, architecture, difficult debugging and major reviews
- `opusplan`: complex tasks where planning should use a stronger model before execution
- `haiku`: simple mechanical edits only
- `fable` or `best`: hardest, longest-running tasks when available and justified

Do not use a fast/simple model for:

- save/load
- core data model changes
- `WorldState` / `KnowledgeState` / `PlayerNotes` separation
- event/risk resolution
- procedural generation design
- major refactors
- Vertical Slice core loop changes
- Unity scene/prefab architecture that affects multiple systems

---

## Most Important Project Rules

- The project uses **Unity 6.5 + C#**.
- Do not place game rules inside Unity scenes, prefabs, GameObjects, MonoBehaviours or UI scripts.
- Preserve the separation between `WorldState`, `KnowledgeState` and `PlayerNotes`.
- Keep the project focused on the Vertical Slice.
- Use commands for player actions.
- Add or update tests when changing `Game.Core`.
- Follow the simplified map direction: hidden logical hex grid, flat isometric 2D presentation, no permanent visible hex lines.
- Use portraits/images/placeholders for important communication and discoveries.
- Ask for human review before major scope, architecture, save-format, scene/prefab or UI workflow changes.
