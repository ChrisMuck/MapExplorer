# Cross-System Phase 2: Playable Location Chains and Runner Paths

Status: **In progress — Blocks 2.0–2.8 complete; Block 2.9 automated gates complete, human review pending**
Branch: `codex/phase-2-archetype-flows`

Prerequisite: `docs/cross_system_foundation_implementation_plan.md` establishes the shared
catalog, deterministic simulation, world processes, scouting, Unity adapter and WPF runner.
Phase 2 turns that foundation into complete, testable expedition decision chains.

Related concepts:

- `docs/gameconcept/cross_system_integration_concept.md`
- `docs/gameconcept/expedition_and_members_concept.md`
- `docs/gameconcept/scout_mission_cycle_concept.md`
- `docs/gameconcept/world_locations_and_events_concept.md`
- `docs/gameconcept/simulation_runner_concept.md`
- `docs/ui_ux_concept.md`

## 1. Goal

An interaction must not stop at a result text when it changes the world. Resolving an action can
change a location state, produce evidence or findings, create an immediate follow-up option and
schedule a later world reaction. The player can then decide whether to continue, secure, avoid,
investigate, seek help or return later.

The sealed entrance is the reference example, not a special implementation case:

```text
sealed -> inspect seal -> open -> investigate / scout inside / enter / secure / leave
```

The same data-driven mechanism must serve bridges, graves, warnings, camps, contact sites and all
future archetypes.

## 2. Non-Negotiable Rules

- `Game.Core` and `Game.App` remain the only rule implementation. Unity and WPF only render shared
  queries and send shared commands.
- No action, action chain, faction, coordinate, claim, hidden consequence or WPF scenario behavior
  is hardcoded by location ID, variant ID, `LocationKind`, Unity UI branch or WPF UI branch.
- Reusable authored content lives in JSON with stable IDs. The WorldGenerator alone assigns runtime
  territory, claim, signature, local context and resolved consequence branches.
- `WorldState`, `KnowledgeState` and `PlayerNotes` remain separate. A new option may be visible
  because the expedition has earned an appropriate fact; the UI must not expose the hidden reason.
- Every path must still offer a meaningful leave, mark, defer or return-later decision where that
  is logically possible. Specialist gates create choices, not arbitrary dead ends.
- A delayed severe consequence needs an earned warning and at least one feasible response path.
- Development scenarios and the WPF runner may inspect objective World Truth, but never create
  player knowledge merely by displaying it.

### 2.1 Confirmed Archetype-Flow Boundary

The common interaction pipeline must preserve distinct archetype questions rather than flattening
every location into one generic menu. `Game.App` therefore owns one registered interaction-flow
policy for each supported **archetype**. The selected policy is identified by the authored
`ScenarioProfile.ArchetypeId`, never by `LocationKind`, a variant ID, a location ID or a Unity/WPF
branch.

An archetype flow owns the order and meaning of its decision phases: Route Obstacle owns
access/bypass/infrastructure choices; Investigation Site owns interpretation/respect/recovery;
Containment Site owns protection/temptation/release. The shared Core/App pipeline still owns
requirements, costs, state persistence, evidence, findings, triggers, processes and option
presentation. JSON supplies the action IDs, state/context gates, text and effects used by that
archetype flow. A variant only composes a particular scenario and presentation; an individual
generated location supplies runtime state and context. Neither may add a separate code path.

## 3. Phase-2 Data Contract

Phase 2 extends existing schema fields compatibly; it does not replace the established content
catalog or create a second content format.

### 3.1 Required Generic Inputs

`actions.json`, State Profiles and Scenario Profiles must be able to express:

- required or allowed interaction, operational and presence states;
- known context/evidence prerequisites and player-visible missing-requirement wording;
- optional specialist, tool, morale, movement-point, supplies or medicine requirements;
- action visibility: always visible, visible-but-disabled, discovered by known context, or hidden
  until a supported discovery;
- action outputs: state transition, context/evidence/finding, project, trigger, consequence,
  situation, route/access change and follow-up availability;
- repeat policy and persistent local effects.

Scenario Profiles define which generic action IDs belong to an archetype/variant and under which
state/context conditions additional action IDs become available. The registered archetype flow
interprets that authored action surface; it does not infer a flow from a concrete variant. Content
Profiles remain strictly presentation-only: titles, descriptions, state wording and visual IDs,
never hidden world truth.

### 3.2 Follow-Up Model

There is no `OpenGateSpecialCase` or `RepairBridgeSpecialCase`. A follow-up is an ordinary action
whose normal requirements are now true after the preceding transition.

Example authored relationship:

```text
action-open-seal
  -> operational state: opened
  -> trigger: location-seal-broken

scenario-sealed-gate
  -> when operational state is opened: add action-investigate-interior
  -> when opened + known danger context: add action-secure-entrance
```

The resolver recalculates the same option contract after every command. It invokes the registered
archetype flow, but contains no special-variant continuation table.

### 3.3 Development Scenario Contract

Each WPF proof path is authored as JSON development data, separate from normal game content and
saves. A path references scenario profile IDs, action IDs, selected team IDs and expected
knowledge/world/trace assertions. The WPF UI contains generic controls for selecting and executing
these actions; it does not contain one button or workflow per location.

## 4. Implementation Blocks

### Block 2.0 — Plan and Content Audit — Complete

1. Mark the completed foundation blocks accurately and remove stale sequencing from the Phase-1
   plan.
2. Inventory every existing action, state transition, Scenario Profile and current WPF command.
3. Identify where a current result ends in text despite creating a state that should enable a
   follow-up.
4. Define a small initial action vocabulary for continue, investigate, secure, enter, scout,
   document, defer and leave. The vocabulary is reusable IDs/tags, not a mandatory action list for
   every location.

**Audit record:** [`cross_system_phase_2_audit.md`](cross_system_phase_2_audit.md)

**Exit criteria:** the audit maps every initial Slice location to a proposed non-hardcoded path and
identifies the JSON field needed for every missing transition or condition.

### Block 2.1 — State-Driven Follow-Up Option Resolver — Complete

1. Introduce the registered archetype-flow boundary in `Game.App`; prove that it is selected by
   `ScenarioProfile.ArchetypeId` and cannot fall back to a variant or `LocationKind` branch.
2. Extend the typed JSON schema and validator with state-gated additional-action rules.
3. Make the shared interaction resolver evaluate operational, interaction and presence state along
   with existing known context and role requirements through the selected archetype flow.
4. Preserve information honesty: only known prerequisite failures can be shown as disabled reasons;
   hidden context only changes uncertainty or later discovery.
5. Re-query options after inspect, scout report, action resolution, project progress and world-stage
   location changes.
6. Add Core/App tests proving that different generated instances of the same profile expose
   different legal options without variant-specific code, while different archetypes select their
   own flow policies.

**Exit criteria:** changing a location to `opened`, `repaired`, `searched` or comparable authored
state can expose a generic follow-up action through JSON alone.

**Implemented:** `Game.App` now selects a registered interaction-flow policy from the authored
archetype ID, including all seven current archetypes. Scenario Profiles support
`stateActionRules` over interaction, operational and presence states; the catalog validates state
and action references. The shared option query runs the selected flow after every normal command
query. Core/App tests prove state-only follow-up availability, compound channel gates, archetype
selection and rejection of invalid state/flow authoring without a variant or `LocationKind` branch.

### Block 2.2 — Complete Initial Slice Location Chains

**Status:** Complete for the seven currently defined archetype profiles; runner and Unity path-library coverage remains in Blocks 2.5 and 2.7.

Author and test at least one complete decision chain for every current Scenario Profile.

| Scenario profile | Minimum path to prove |
|---|---|
| Route Obstacle | inspect -> assess -> bypass / temporary passage / repair -> changed route or later reaction |
| Investigation Site | inspect -> document / respectful examination / disturbance -> evidence or finding plus possible consequence |
| Territorial Marker | observe -> interpret -> respect / cross / communicate -> territorial response only if observed and relevant |
| Containment Site | inspect seal -> open -> investigate interior / scout inside / secure / leave -> delayed release or contained result |
| Contact Site | observe -> approach / communicate / withdraw -> contact knowledge, offer, warning or social memory |
| Hazard Site | observe -> assess -> avoid / traverse / find safe route / contain -> persistent risk or route knowledge |
| Natural Phenomenon | observe -> approach -> survey / map -> persistent landmark knowledge |

Each chain must contain a safe or deferrable choice, a contextually risky choice and at least one
specialist-sensitive branch where it makes sense. A location may be atmospheric and yield only
knowledge; rewards are never mandatory material loot.

**Exit criteria:** all seven current archetype profiles have a playable, data-authored path that
begins with the appropriate first investigation action (including inspection where applicable),
continues after its first intervention, and reaches a persistent local or world result.

### Block 2.3 — Findings, Knowledge and Persistent Local Results

**Status:** Complete.

**In progress:** `AddFinding` is now a generic location-outcome/project-completion effect. It
creates only an unsecured field finding, observes the authored repeat policy, transfers through
the existing return handoff and awards Knowledge Points only after normal base analysis. Initial
chains use this for investigation, containment, hazard containment and natural-phenomenon survey.
The distinct player-knowledge model for a once-observed condition becoming old or doubtful now has
its Core representation; no objective location state is exposed merely to satisfy that requirement.

**Implemented:** validated Finding Tables and the generic `RollFindingTable` effect now resolve
weighted, state/context-filtered candidates through deterministic World State and persist the
resolved entry or empty result. Direct `AddFinding` remains available for guaranteed narrative
discoveries; ordinary fixed investigation/sample/survey findings have been migrated to tables.

**Current implementation step:** `KnowledgeState` records the last directly observed interaction,
operational and presence state of a location with its observation day. Later World-State changes do
not update this record omnisciently. The record can become old against a caller/content-supplied
freshness window or explicitly doubtful through earned conflicting evidence; the gameplay
balancing threshold is intentionally not hardcoded before human review.

**Implemented:** the serialization-safe `KnowledgeRuntimeSnapshot` roundtrips last-known location
states, observation days, old/doubtful flags, known context, evidence, approximate scout reports,
claimed sources and tile knowledge without rebuilding any of them from current World Truth. It is
separate from `WorldRuntimeSnapshot` and rejects duplicate saved identities. A no-truth-leakage
roundtrip test proves that an unobserved objective location change does not refresh saved player
knowledge.

**Implemented:** every valid interaction, operational and presence state in all seven current State
Profiles has neutral player-facing wording in its presentation-only Content Profile. Catalog
validation rejects a Scenario Profile whose state vocabulary is not fully covered, preventing a
new gameplay state from silently falling back to misleading generic text.

1. Ensure every chain can use the common outputs independently: description, evidence, field
   finding, unsecured knowledge, persistent state, route/access, trigger and situation.
2. Keep the existing knowledge loop intact: findings and field knowledge only become Knowledge
   Points through return and base analysis; supplies and medicine remain expedition consumables.
3. Ensure physical changes persist across expeditions while their current condition may become old
   or uncertain until reached again.
4. Author neutral player-facing result and status wording for each state. Add text variations only
   after the first paths are functionally complete.

**Exit criteria:** a location can be empty, informative, useful, dangerous or politically relevant
without requiring a separate reward system or a special code path, and its fallible last-known
condition survives save/load without exposing the current objective condition.

### Block 2.4 — Follow-Up World Processes and Fair Responses

**Status:** Complete. The sealed-containment reference path proves the complete generic
chain from opening through delayed earned evidence and an active warning situation to an authored
containment response. The response persists and suppresses the later escalation trigger without
erasing the later evidence or historical trace. The route-obstacle reference path now likewise
turns observed post-repair traffic into a response situation; coordination preserves the immediate
authored traffic reaction while suppressing only the later unmanaged-attention escalation. The
investigation-site reference path distinguishes a respectful choice that starts no
disturbance process from a known risky intervention that produces an uncertain rumour, warning and
preparation response; preparation suppresses only the later related-awareness escalation while
preserving the physical disturbance and later evidence.

The generic situation lifecycle now expires unanswered situations exactly once at their authored
deadline and records both causal trace and player-facing notice. `promise-return` creates a
persistent `Promised` state rather than resolving the request; it remains fulfillable until the
deadline. A fulfilled promise resolves normally, while an expired promise applies only its authored
trust delta and memory ID. World runtime snapshots preserve the open promise and response tag.
The contact-site reference path establishes contact only with one concrete generated related
faction, then schedules a request through the ordinary trigger/process contract. The runtime
situation records `SourceKind=faction` and `DeliveryChannel=direct-contact`; a generated relation
without established local contact cannot deliver the request. Source and delivery survive the
World runtime snapshot. Together the containment, route, investigation and contact proofs cover
earned warnings, mitigation, coordination, respectful non-escalation, unanswered expiry and kept
or broken promises without a variant or location-specific simulation branch.

1. Connect relevant action chains to existing generic triggers, consequence branches, observation
   channels, faction reactions and situations.
2. Author response actions that can mitigate, redirect, contain, investigate, negotiate or ignore a
   developing problem when the context permits it.
3. Show player-facing warning through earned reports, direct observation, contacts, rumours or
   messages; do not reveal process branches or distant faction truth.
4. Cover both positive and negative outcomes: gratitude, information, assistance, contacts,
   warnings, route changes, hazards and social distrust.

Every player-facing situation must use one shared causal contract:

```text
expedition action or external event
  -> neutral world trigger / process
  -> plausible observation, communication or direct local notice
  -> player-facing situation, message, report or contact
  -> response, deferral with promise, or deliberate inaction
  -> fulfilled, expired or ignored result with persistent consequences
```

Faction contact is not automatic. A faction may only send a messenger, warning, request or offer
when its generated context, observation channel and distance/route constraints plausibly support
it. A remote or unaware faction remains silent. The same process must also work without a faction:
an expedition can notice a local change, receive a scout report or hear a neutral rumour and still
gain a fair response opportunity.

Initial response vocabulary is data-driven and may include `help`, `contain`, `investigate`,
`negotiate`, `withdraw`, `ignore` and `ask-for-time`. `ask-for-time` records a clear deadline and
must affect future trust or reliability if the expedition does not follow up. Benefits of helping
are intentionally not fully previewed; they may be information, trust, assistance, contact or a
later opportunity, and may occasionally duplicate knowledge already known.

**Exit criteria:** a broken seal, repaired route or disturbed investigation site can produce a
plausible world response over time, but the expedition has a fair and understandable opportunity
to react when one is possible. Contact, deadline and ignored-response outcomes remain explainable
through causal traces and persist in World State/faction memory rather than a transient UI message.

### Block 2.5 — WPF Path Library for Every Existing Location Type

**Status:** Complete. The development-scenario contract now carries purpose, selected team, starting
knowledge, expected world process and outstanding decision metadata, and WPF displays that context
without changing simulation state. Generic assertions cover player-visible/available/locked
options, objective and last-known state channels, situation delivery channels, evidence, triggers
and causal trace. The automatically discovered path library now exercises all seven current
archetypes, including separate respectful and disturbing investigation branches, territorial
respect, established-contact delivery, hazard routing and scholar-gated landmark survey. The
explicit event-chain matrix also proves a silent unobserved remote passability change, an earned
neutral external crisis, a fulfilled direct-contact request, a broken promise after deadline and an
ignored warning expiry. Development fixture triggers enter the ordinary trigger/process pipeline;
they do not introduce a WPF-only world-rule path.

1. Add one or more explicit JSON development scenarios for every current Scenario Profile and key
   state branch, including sealed-opened follow-up, bridge repair, respectful/disturbing
   investigation, territorial choice and contact choice.
2. Present scenario path metadata in WPF: purpose, selected team, starting knowledge, expected
   world process and outstanding decision point.
3. Keep the existing team-preview tab and use it to prove specialist gates before committing an
   action.
4. Add generic runner assertions for visible/locked options, follow-up availability, player
   knowledge, world state and causal trace.
5. Add explicit event-chain scenarios: observed expedition-caused change, unobserved remote
   change, faction request with fulfilled/expired deadline, neutral external crisis and an ignored
   warning. Each scenario must show the player-facing delivery channel separately from hidden
   World Truth.

**Exit criteria:** a developer can select any existing location path in WPF, execute its decisions
without Unity and understand why each new option, warning, contact deadline or reaction appeared.

### Block 2.6 — Directional Scout Controls in WPF

**Status:** Complete. WPF now exposes the shared directional mission contract for one or two free
scouts, all eight compass sectors, one to five days, every authored focus and every behavior. The
control sends through `DevelopmentScenarioPlayback` into the ordinary `SimulationSession` scout
command; reports appear only after normal day advancement. A dedicated status view keeps active,
overdue, returned and returned-injured missions visible beside approximate lead reports. Explicit
development scenarios prove a two-scout team, an overdue return, an injured low-confidence return
and the no-exact-coordinate knowledge contract.

The current WPF runner already supports local surroundings scouting. Add the separate directional
mission path with generic controls for:

- one or two available scouts;
- approximate compass sector;
- duration;
- focus;
- behavior.

The resulting reports must appear only after normal day advancement and show the existing
approximate lead/scope/confidence contract. They must never reveal exact remote coordinates,
objective faction identity or automatic map conclusions.

**Exit criteria:** WPF can demonstrate local and directional scouting side by side, including an
overdue, injured or lower-confidence result when authored scenario content calls for it.

### Block 2.7 — Unity Player-Facing Parity

**Status:** Complete for implementation; final human Unity play-through remains part of Block 2.9.
Unity already obtained option IDs, availability and known locked reasons from the shared
`SimulationSession` query. Its location header now also consumes a `Game.App` player-facing
presentation projection built from authored Content Profiles and the last observed
`KnowledgeState` condition. It no longer renders live operational/presence World Truth, objective
modifier IDs or variant-specific fallback prose. Action glyphs are selected from generic authored
action tags rather than special action IDs. Action, project, scout and end-day/world-process paths
all refresh the shared UI query, while report and event surfaces retain explicit portrait/scene
placeholder anchors. Tests prove that an unobserved repaired state does not replace the last-known
blocked wording and that earned context, not hidden modifiers, reaches presentation.

1. Display the same dynamic follow-up options, availability reasons and location state wording in
   the Unity location panel.
2. Refresh the panel after each action, scout report, world message or project step.
3. Present warnings, messages and important discoveries through the event/report flow with a
   placeholder portrait or scene image where appropriate.
4. Verify Unity exposes no runner-only World Truth.

**Exit criteria:** Unity and WPF return identical option IDs, availability and known requirement
text for every Phase-2 scenario state.

### Block 2.8 — WPF Directional Movement Controls (Last)

**Status:** Complete. WPF now provides a six-direction logical hex pad. Every press derives exactly
one adjacent `HexCoord` through `HexCoord.Neighbor` and delegates to the ordinary
`SimulationSession.MoveExpedition` path. The runner displays the returned cost or rejection and
then refreshes location options, player knowledge, movement points and scout controls from the same
session. It contains no map, teleport, alternative passability check or WPF movement-cost rule. A
deterministic test proves adjacency, normal terrain cost, ordinary knowledge revelation and that
interactive movement does not consume the authored JSON command path.

Only after the fixed paths and directional scout controls are trustworthy, add a convenience
movement pad to WPF. It sends the ordinary `MoveExpeditionCommand` to the adjacent logical hex in
the selected approximate direction and displays the normal movement result/cost. It does not add a
WPF-specific map, teleport, coordinate reveal or alternative movement rule.

**Exit criteria:** a developer can reach a nearby scenario location and launch a directional scout
mission with buttons alone, while movement points, terrain restrictions and knowledge gates remain
the shared simulation behavior.

### Block 2.9 — Regression, Batch and Human Review

**Status:** In progress. Deterministic regression and batch gates now cover every authored proof
scenario and all seven initial archetype profiles. The batch runner steps scenario commands through
the shared application path and flags unreachable scripts, state changes without follow-up, missing
leave/mark/defer choices, specialist-gated dead ends, unanswerable situations and severe branches
without an earlier answerable warning. The checks operate on scenario profiles, action tags,
requirements and runtime interaction queries; they contain no concrete location, variant or
`LocationKind` branches. The remaining gate is the project-owner WPF/Unity play-through recorded in
`docs/phase_2_human_review_checklist.md`.

1. Add deterministic tests for every new state transition, follow-up option, missing specialist,
   local/directional scout distinction, warning path and persistence rule.
2. Expand batch metrics to flag locations with no follow-up after a state-changing action, dead-end
   specialist gates, absent leave/defer choices, unanswerable severe situations and unreachable
   test paths.
3. Run the path library in WPF and the matching Unity paths with the project owner.
4. Only then expand text variation, add further location archetypes or introduce dynamic interior
   locations.

**Exit criteria:** all initial location chains are reproducible, explainable in causality view,
usable in Unity and judged by human playtest rather than only automated success.

## 5. Explicitly Deferred

- new archetypes beyond the existing initial Slice;
- contested multi-faction locations;
- a separate tactical interior map or dynamically generated interior sub-world;
- broad text-variation libraries, final art and audio polish;
- a second rule path for WPF or Unity;
- map rendering in WPF;
- broad legacy save-format migration beyond the explicit Phase-2 Knowledge snapshot requirement.

An opened location uses state-driven actions in the current logical location first. Dynamic
interior locations may be added later only if the state/action model cannot express the intended
gameplay cleanly.

## 6. Required Execution Order

```text
2.0 Audit
  -> 2.1 state-driven follow-up resolver
  -> 2.2 authored Slice chains
  -> 2.3 persistent results and knowledge
  -> 2.4 world responses
  -> 2.5 WPF path library
  -> 2.6 directional scouts in WPF
  -> 2.7 Unity parity
  -> 2.8 WPF movement controls
  -> 2.9 regression, batch and human review
```

No block may add per-location simulation code to Unity or WPF. If a requested chain cannot be
authored from the generic data contract, extend that contract in `Game.Core`/`Game.App`, validate
it in the catalog and prove it with a reusable scenario before adding more content.
