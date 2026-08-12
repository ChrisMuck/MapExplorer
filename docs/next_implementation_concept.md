# Next Implementation Concept: Curated Vertical Slice and Content Authoring

Status: **Proposed — owner review required before implementation**

Related documents:

- `gameconcept/exploration_game_concept.md`
- `tutorial_vertical_slice_route_spec.md`
- `gameconcept/mvp_vertical_slice_concept.md`
- `gameconcept/cross_system_integration_concept.md`
- `gameconcept/situational_scene_description_concept.md`
- `technical_concept.md`
- `ui_ux_concept.md`
- `map_presentation_simplified_concept.md`

## 1. Decision and Priority

The next phase should not add another broad simulation system. The core expedition, knowledge,
location, faction, delayed-consequence, base and presentation contracts already exist. The central
question is now whether they form one understandable, desirable player journey.

Priority order:

1. **Curated, playable tutorial journey.** This is the next deliverable.
2. **Content workflow and a small polished starter library.** It makes the journey readable,
   varied and maintainable.
3. **Editor discovery and decision.** Investigate authoring pain points before committing to a
   large tool.
4. **Map-presentation replacement/polish.** This remains the final substantial MVP implementation
   area, after playtests establish what visual communication the journey needs.

The intended proof remains:

```text
first expedition: incomplete but useful knowledge
  -> voluntary return and base analysis/preparation
  -> second expedition: a concrete, better-informed action
```

This phase does not implement a complete campaign-goal system, full save/load, final art, a large
procedural content library, deep diplomacy or combat. They require separate owner decisions.

## 2. Why This Should Come Next

The project can already resolve the relevant parts individually. The risk has moved from technical
feasibility to player understanding: do reports, notes, warnings, an obstacle, the return decision
and base preparation feel causally connected?

A deterministic authored tutorial is therefore a design integration test. It gives one stable
reference world for balancing supplies and movement, judging report clarity, testing faction
warnings honestly, evaluating specialist gates, and identifying missing text, visuals and UI cues.
Generated worlds, WPF scenarios and batch tests remain regression tools; they cannot replace this
authored player path.

## 3. Deliverable A — Curated Tutorial Vertical Slice

### 3.1 Player promise

In 30–45 minutes, a player should experience:

> The first expedition cannot safely solve everything, but it returns with knowledge that makes a
> concrete second expedition better prepared and meaningfully different.

This must remain exploration and interpretation, not a chain of quest arrows or a debug map.

### 3.2 Required authored journey

The exact coordinates, pacing and text need owner review. The following beats are required and use
existing shared commands/data contracts rather than Unity-only scripts.

| Beat | First expedition | Persistent result |
| --- | --- | --- |
| Departure | Fixed eight-person team learns movement, fog, supplies and daily capacity. | A legible risk baseline. |
| First scout report | A directional report gives an approximate useful clue and invites a marker/note. No exact unseen target. | Report and annotation persist. |
| Faction sign/contact | A warning or sign has a cautious non-hostile response; one helpful contact demonstrates incomplete local knowledge. | Earned signature/contact knowledge and memory affect later interpretation. |
| Ravine / broken route | A safe durable route needs the absent engineer. Risky, scouting-led or deferrable choices remain; no dead end. | Known obstacle, evidence and route need persist. |
| Legacy site | An abandoned camp, marked grave or equivalent gives partial earlier-expedition evidence. | An archive/evaluation input or durable lead remains unresolved. |
| Return pressure | Low supplies, injury, overdue scout risk or warning makes return a considered decision, never arbitrary loss. | Field knowledge is secured only through return. |
| Base phase | Return scene, archive, analysis and preparation expose the value of Knowledge Points/time. | Engineer or equivalent relevant preparation becomes available. |
| Second expedition | The player revisits the known issue using inherited knowledge and preparation. | A different action/outcome proves continuity. |

All three MVP factions should be territorial actors. Their tutorial functions must remain distinct:
Coastal People provide partial help, Border Wardens teach warned territorial conduct, and Hidden
Ones demonstrate serious danger after clear warning.

### 3.3 Authoring rules

- The tutorial is a deterministic versioned fixture selected through the existing Tutorial campaign
  start path, not authoritative Unity-scene state.
- Terrain, locations, factions, evidence, initial states, scenes, visual IDs and consequences use
  normal catalog schemas wherever possible.
- A fixture may set exact placement and outcome tiers, but it may not add tutorial-only command
  rules or bypass catalog validation.
- Guidance uses observable landmarks, reports, scene text and restrained teaching copy. It never
  reveals secret territory, exact scout targets or consequence branches.
- Major choices retain leave, defer or return options when logically possible.
- Important communications and discoveries go through the existing scene-description and visual
  reference path. MVP placeholders are acceptable; generic empty panels are not.

### 3.4 Implementation packages

#### A1 — Journey specification and review

Create an owner-reviewed route sheet before coding. For each beat it records player choices,
required knowledge before/after, expected day range, resource envelope, optional failure and
recovery paths, texts, scene fragments and visual placeholders.

**Exit criterion:** a reviewer can trace first and second expeditions without source code; every
beat has a meaningful decision and a testable state outcome.

#### A2 — Data-authored tutorial world

Author the tutorial fixture and content using the current catalog. Reuse existing archetype flows;
add only minimal data fields that generated worlds could also use later.

**Exit criterion:** Tutorial mode always creates the same world, team, initial knowledge and
opportunities; catalog validation passes.

#### A3 — Guided two-expedition proof

Connect the beats through ordinary movement, scout, location, faction, return, base and new
expedition commands. Add dismissible contextual teaching only where players would otherwise miss
an existing interaction.

**Exit criterion:** a fresh player can prove the intended two-expedition loop without developer
tools or manual state changes.

#### A4 — Playtest and balance loop

Observe the reference path and deliberate detours. Record report misunderstandings, missing notes,
return-pressure failures, unclear specialist locks and unclear base value. Tune content, scene text
and UI emphasis first; change generic rules only with evidence of a systemic defect.

**Exit criterion:** testers can explain what they learned, why they returned and what will change
for expedition two; they want to resolve an unanswered route or mystery.

### 3.5 Required automated coverage

Add deterministic tutorial smoke coverage for:

- stable bootstrap and catalog validation;
- normal approximate scout-report delivery;
- no premature faction/territory disclosure;
- a specialist-gated obstacle with a leave/defer option;
- field-knowledge transfer through first return;
- base preparation changing normal second-expedition readiness/options;
- a second-expedition different outcome through ordinary commands; and
- absence of tutorial-only Unity state mutation.

Tests should assert player-visible contracts and state, not fragile full prose or camera layout.

## 4. Deliverable B — Content Workflow and Starter Library

### 4.1 Goal

The project already has typed JSON, catalog validation, localized scene fragments, visual asset IDs
and development scenarios. The next task is to turn them into a coherent production workflow,
without prematurely building a large content library.

The starter library should cover the tutorial and early generated replay:

- multiple readable fragments for relevant location states;
- distinct faction contact, warning and reaction fragments;
- normal, overdue and injured scout-return/report variations;
- base return, archive and evaluation entries;
- placeholder visual assignments for each important tutorial communication/discovery; and
- observable, uncertainty-honest action/result wording.

### 4.2 Production contract

```text
edit JSON + localized text + visual ID
  -> existing schema/catalog validation
  -> deterministic tutorial/scenario smoke path
  -> Unity player-facing review
  -> human wording and game-feel approval
```

Content defines data; Core/App resolves rules; Unity and WPF render the shared result. Content must
not acquire C# callbacks, UnityEvents or prefab-specific rule branches.

### 4.3 Conventions to establish now

- Stable IDs are not casually renamed. Once external saves exist, renames need an explicit
  migration policy.
- Rule-bearing fields and player-facing prose remain separate. Text may not claim hidden truth.
- Each important scene/action has reviewable German default text; other languages add locale files
  without duplicating rules.
- Visuals use asset IDs with allowed placeholder fallback. Agree whether an important missing
  visual is a catalog warning or error.
- Development scenarios are tests, not normal content. Tutorial placement lives in tutorial data.

### 4.4 Immediate improvements without a general editor

1. Add authoring templates and a concise field reference for tutorial-used schemas: fixture,
   scenario profile, action, state/content profile, scene fragment/policy, locale, visual asset and
   faction content.
2. Improve catalog diagnostics to name document path, stable ID and invalid reference.
3. Add a non-mutating content report: counts, unused IDs, missing localizations/visuals,
   unreachable actions and unresolved fixture references.
4. Add a curated Unity or WPF preview path for actual player-visible scenes and action states from
   legitimate tutorial/development states.

**Exit criterion:** a designer can change a tutorial chain, find mistakes through validation, and
review actual player presentation without hunting through C# or prefabs.

## 5. Editor: Discovery Before Commitment

### 5.1 Decision

An editor is likely valuable, but a broad game editor is not the next deliverable. Building one now
risks duplicating schemas, validators and simulation previews before real authoring has revealed
which operations are frequent and error-prone.

Run a bounded editor-discovery spike alongside or after the first tutorial content pass. Its output
is an owner decision, not an automatically approved tool project.

### 5.2 Questions the spike must answer

- What is actually slow/error-prone: placement, action links, state gates, scene text, visual
  assignment or outcome-chain inspection?
- Who will author content: owner, content designer, programmers or all of them?
- Is a Unity tool needed for placement/visual review, or do JSON plus WPF/Unity previews suffice?
- Can a tool round-trip existing JSON with stable, reviewable diffs?
- Can it reuse `GameDataCatalog` validation and read-only `SimulationSession`/scene previews rather
  than reimplement rules?
- Which dangerous actions need special handling: deleting referenced IDs, exposing hidden truth or
  changing save-affecting identifiers?

### 5.3 Recommended first tool if justified

Build only a **Tutorial Content Workbench**, not a general world editor. Initial scope:

- open and validate the existing tutorial catalog;
- select a placed location, faction sign or scene subject on a logical 2D map/list;
- inspect references and player-visible state/action/scene output for a legal state;
- edit placement and presentation references, then explicitly write valid JSON;
- show the graph `location -> scenario profile -> actions -> effects -> scene fragments -> locale
  text -> visual asset`; and
- run the existing validator and deterministic smoke scenario.

Explicit exclusions: a second simulation, direct live `GameState`/save editing, automatic complex
chain generation, an authoritative node-graph replacement for schemas and prefab-based rules.

### 5.4 Technical boundary

```text
authored JSON read/write
  -> existing loaders and GameDataCatalog validation
  -> existing read-only Game.App query / scene projection / SimulationSession preview
  -> editor diagnostics and previews
```

The editor must not get a privileged rule path. Saves are explicit, atomic and validated before
write; unrelated JSON stays untouched. Failed validation reports structured diagnostics instead of
creating a partial content tree.

### 5.5 Decision gate

After the tutorial pass, record time spent per change, validation failures, review friction and
repeated feature requests. The owner then selects one option:

- no editor yet — templates, validation and previews are adequate;
- narrow Unity workbench — placement and visual review dominate; or
- standalone content workbench — schema editing, reference graphs and batch validation dominate.

This is a human-review gate because it creates long-lived production architecture.

## 6. Deliberately Deferred: Map Presentation

Map presentation is the final substantial MVP implementation area, but must follow the functional
tutorial. Playtests will establish which landmark silhouettes, route continuity, fog states and
scene contexts actually need stronger visual communication.

When this work begins it must follow `map_presentation_simplified_concept.md`: flat isometric
2D/2.5D, no permanent grid, connected roads/rivers, objects/sprites over logical hexes, and a pure
Unity projection of Core/App state. The current map is a functional visualization/input host, not
the final visual commitment. Do not begin final-art production before the tutorial defines the
visual brief.

## 7. Execution Order and Completion

```text
owner approves route sheet
  -> A2 tutorial data
  -> A3 two-expedition proof plus smoke tests
  -> A4 observed playtests and tuning
  -> workflow/report improvements from real authoring pain
  -> editor discovery and owner decision
  -> optional narrow workbench
  -> final map-presentation implementation
```

The phase is complete when a new player can complete the deterministic two-expedition proof; the
tutorial demonstrates scouts, notes, uncertainty, factions, specialist preparation, legacy,
return pressure and persistence; important moments use data-authored scenes/placeholder visuals;
content is validated and previewable; and the project has an evidence-based editor decision.

## 8. Risks and Mitigations

| Risk | Mitigation |
| --- | --- |
| Tutorial becomes a scripted corridor. | Keep defer/leave/risk choices; guide through clues, not quest arrows. |
| Tutorial-only C# branches leak into rules. | Use catalog fixtures, shared commands and end-to-end contract tests. |
| Content volume outruns quality. | Limit the starter library to tutorial/replay needs and playtest early. |
| Editor grows into a second client. | Time-box discovery and permit only a narrow workbench using existing seams. |
| UI text leaks hidden truth. | Retain Knowledge-only projections and review teaching/scene text. |
| Map polish starts too early. | Make tutorial findings the input to the visual brief. |

## 9. Work Log Requirements

Every task under this concept reports the model/tool used, files and content IDs changed, automated
and manual checks run, known player-experience/authoring risks, and the next required human review.
