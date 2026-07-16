# CHANGELOG

## Phase 2 closure

Cross-System Phase 2 was accepted for the current Vertical Slice on 2026-07-16 after the project
owner tested representative scenarios. Blocks 2.0–2.9 are complete. Additional wording, balance
and interaction-flow refinement remains deferred to later playtests rather than blocking the
functional phase closure.

The next player-facing priority is the shared situational scene-description system. It should give
players enough atmospheric, state-aware and knowledge-honest context to make the data-driven
archetype decisions introduced in Phase 2.

## Situational scene description elaboration

`situational_scene_description_concept.md` was elaborated from a direction document into an
implementable specification.

Main changes:

- mapped the information-honesty sources to concrete Core state (KnowledgeState, LocationConditionKnowledgeState, delivered reports and member states)
- defined the full scene-fragment authoring schema (`scene-fragments`, `scene-policies` document types under `Scenes/`)
- specified the resolver selection algorithm, determinism rules and the SceneDescriptionResult contract
- reused modifier `inspectionText` as the modifier fragment layer instead of re-authoring it
- added a curated German MVP starter library (~90 fragments, 11 policies) bound to existing state profiles, modifiers and status enums
- added validator rules for scene content and an eight-package implementation path including the `flavorByState` migration

## Modular concept revision

The uploaded single-file concept was reviewed and reorganized.

Main changes:

- replaced the old map presentation section with the hidden-hex, flat isometric 2D/2.5D direction
- confirmed three territorial MVP factions
- defined the Hidden Ones as extremely aggressive inside their land
- clarified eight approximate compass sectors for scout orders over a six-neighbor hex grid
- clarified the resource model: four expedition resources plus Knowledge Points
- added portraits and visual communication rules
- added event-based conflict resolution with no tactical battle screen
- added a provisional partial-return and knowledge-preservation matrix
- added automated travel over confirmed known routes with revalidation after uncertain failure
- made analysis time-only and Knowledge-producing
- defined purchased-hint effects on independent discovery rewards
- removed expedition splitting
- limited Outer Camps for the MVP
- added unbounded persistent character histories
- cleaned the Open Questions section
- added Locked Current Decisions at the start of the master document
- split the large design into specialist documents for agents
