# Cross-System Integration Concept

Source of truth for how Expeditions, Locations, Scout Missions, Factions and delayed World Consequences connect.

This document does not replace the specialist concepts. It defines their shared contract so that each system can stay data-driven without embedding generated-world facts in static content definitions.

---

## 1. Core Decision

The game is built around interpretation, not around isolated interaction widgets.

> A location supplies a concrete question.  
> Scouts supply contextual evidence.  
> The generated world gives an action its political and historical meaning.  
> Factions and delayed consequences make that action persist.

Every meaningful action at a location can therefore affect three separate things:

1. **Local state**: what physically changed at the location or route.
2. **Player knowledge**: what the expedition learned, suspects or documented.
3. **World state**: a neutral world trigger and, if applicable, a scheduled consequence.

Factions react only after the world has evaluated the action against the concrete generated location instance. A generic location definition or outcome table must never name a particular faction.

---

## 2. Non-Negotiable Separation

### 2.1 Content Definitions

Static JSON content defines reusable possibilities:

- location archetypes, variants, modifiers and actions
- risks, outcome tables and generic effects
- scout order types, focuses, reports and mission outcome tables
- faction values, territorial rules, signatures and reaction rules
- evidence, consequences and player-facing event templates

Content definitions do **not** decide which concrete faction controls, watches, values or claims a concrete location.

### 2.2 Generated World State

The WorldGenerator produces the concrete relationships after terrain, territory, roads and locations exist. It owns facts such as:

- which territory contains an anchor
- whether a location is claimed, watched, sacred, guarded or neutral-in-origin
- which enclosing faction, if any, is connected to that state
- why the location's current condition exists
- what signs, patrols, routes or witnesses are present around it
- which long-term consequences are currently scheduled
- what a faction has noticed in a specific region

This state is persisted in the savegame. It is not authored as a static location-to-faction lookup table.

### 2.3 Player Knowledge

The player sees only evidence gained through expedition actions, scouts, contacts, events and archive analysis. The UI must not expose generated World Truth merely because it exists in the same runtime object.

For example, the World State may know that a bridge was deliberately dismantled by a faction to contain a danger. Player Knowledge may initially contain only: "several structural supports were removed deliberately."

---

## 3. The Integration Loop

```text
Discover location
  -> Inspect location with expedition
  -> Send scout to investigate surroundings (optional, requires a free scout)
  -> Collect and interpret evidence
  -> Intervene, defer or leave
  -> Resolve local outcome
  -> Raise neutral World Trigger / schedule consequence
  -> World evaluates generated territorial and faction context
  -> Factions and the world react over time
  -> New reports, evidence, risks and opportunities appear
```

This loop is deliberately circular. A faction reaction can create a new scout report, marker, patrol, warning, contact opportunity, hazard or changed route; it must not be a final score adjustment only.

---

## 4. Location Interaction Contract

### 4.1 The Two Fundamental Location Options

Every discoverable special location exposes these two conceptual paths whenever applicable:

| Path | Performed by | Primary result |
|---|---|---|
| **Inspect location** | Main expedition | Direct facts about the structure, material, condition, contents or immediate danger. Relevant specialists improve interpretation, options, recovery or risk estimate. |
| **Scout surroundings** | A currently free scout | Indirect spatial and social evidence: signs, paths, patrols, usage, nearby camps, witnesses, hazards and connections to other places. |

"Scout surroundings" is presented in the location context, but is a Scout Mission rather than an ordinary immediate location action. It is unavailable while no scout is free, and normally returns a report after its mission time has passed.

Other actions are specific interventions: repair, open, cross, preserve, collect, offer, bypass, document, negotiate or leave.

### 4.2 Specialist Contribution

Specialists do not replace discovery with an automatic answer. They may:

- reveal a deeper local fact during inspection
- improve the confidence of an estimate
- unlock a safer or more respectful intervention
- reduce risk, cost or project duration
- improve recovery after a failed action

A specialist normally improves the answer to "what is this and what can we do here?" A scout improves the answer to "what surrounds this place and who may care about it?"

### 4.3 Action Output

Every resolved action may emit these independent outputs:

```text
Local effects       ChangeLocationState, route change, project, item or injury
Knowledge effects   AddEvidence, AddFinding, AddReport, confirm/contradict knowledge
World effects       RaiseWorldTrigger, ScheduleConsequence, create dynamic situation
```

An action needs none, one or many effects in each category. This keeps harmless observation, a bridge reconstruction and a dangerous crypt opening inside one common model.

---

## 5. Neutral World Triggers

A **World Trigger** is a runtime message describing that something meaningful happened. It is not automatically player-visible and it does not carry a hardcoded faction ID.

Examples:

```text
location-infrastructure-repaired
location-seal-broken
location-grave-disturbed
location-resource-harvested
location-warning-respected
scout-observed-in-region
hazard-spread
```

Triggers contain enough neutral context for world systems to evaluate them:

```text
trigger ID
source location / region / mission ID
action tags
affected map anchor or region
world day
visibility to player (normally hidden)
```

The World Reaction resolver uses the generated state to decide whether anything follows:

1. Is the source inside a territory?
2. Is the location claimed, watched, sacred, guarded or otherwise socially relevant?
3. Has a faction actually observed the action or its result?
4. Which faction values, territorial rules, memories and awareness apply?
5. Does the reaction become an immediate event, a delayed consequence, a state change or no reaction at all?

This rule preserves variation. Rebuilding the same bridge can be welcome, ignored, suspicious or hostile in different generated worlds.

---

## 6. Evidence Is the Shared Player-Facing Currency

Evidence connects locations, scouts, factions, reports and archive analysis. It is not a claim of objective truth.

An evidence item should support:

```text
stable evidence ID
category (structural, environmental, faction-signature, route, witness, hazard, historical)
source (inspection, scout report, contact, event, archive)
subject location / region / symbol thread
knowledge status (reported, confirmed, old, doubtful, contradicted)
confidence
player-facing wording
optional stable symbol ID
```

The generator selects which evidence is present in a concrete instance. Content supplies the reusable evidence definitions and report language.

Stable symbol IDs are especially important: the player may see the same sign at a bridge, on a patrol marker and in an old archive sketch before correctly connecting them to a known faction.

---

## 7. Faction Reactions Without Static Ownership

Faction definitions describe general values and rules, such as valuing restraint, secrecy, trade, ritual, roads or territorial control. They may subscribe to neutral trigger categories, but never to static location IDs.

Example of a generic rule:

```text
When: an observed infrastructure-repair changes a claimed or guarded passage
Then: evaluate the faction's current territorial rule and attitude
Possible result: appreciation, suspicion, warning, patrol, demand for contact or hostility
```

Whether the rule applies is determined entirely by the generated location and regional state. A location inside territory is not automatically claimed, and a neutral-origin location may be watched or sacred without being faction-owned.

Faction awareness from detected scout missions follows the same approach. It is regional World State, not a visible global meter and not a property authored into a scout report.

---

## 8. Delayed Consequences

Delayed consequences make the world persistent without turning every action into an immediate visible punishment.

An outcome table may schedule a consequence by ID. At action resolution time, its specific branch and effect bundle are resolved once and stored on the relevant world/location instance. Time later determines only when that fixed result is applied; it must not reroll history.

A consequence definition supports staged effects, for example:

```text
Stage 0: location state changes immediately
Stage 1: after 3 world days, create a local hazard
Stage 2: after 7 world days, expand the hazard or create a dynamic situation
Stage 3: when observed or when a threshold is reached, queue a player-facing event
```

Stages may emit World Triggers. The faction system can react to those triggers only if the generated state makes them relevant. A consequence therefore never needs to know a faction in advance.

### Fairness Rule

An action with major irreversible consequences must have discoverable warning evidence before commitment. Inspection, a relevant specialist, previous reports or scouting can improve the warning, but they do not need to reveal the precise future outcome.

---

## 9. Required Data Files

The existing six location files remain the core location authoring set. They should be expanded with neutral tags and reusable effect references rather than duplicated per faction.

### 9.1 Existing Location Files to Retain

```text
locations/archetypes.json
locations/variants.json
locations/actions.json
locations/modifiers.json
locations/content-profiles.json
locations/outcome-tables.json
```

Required additions to their schemas:

- `actions.json`: semantic `actionTags` such as `repair`, `open`, `disturb`, `respect`, `harvest`, `cross`
- `outcome-tables.json`: neutral `RaiseWorldTrigger`, `ScheduleConsequence` and evidence effects; no fixed `factionId`
- `variants.json`: compatible evidence, scout mission types and consequence IDs; no faction assignment
- `content-profiles.json`: only presentation and authored wording, never hidden truth

### 9.2 New Shared Definition Files

```text
world/evidence-definitions.json
world/world-trigger-definitions.json
world/consequences.json
world/events.json
```

| File | Responsibility |
|---|---|
| `evidence-definitions.json` | Reusable clues, symbol IDs, categories, knowledge presentation and compatibility rules. |
| `world-trigger-definitions.json` | Stable trigger IDs, payload requirements and authoring metadata. Runtime trigger instances remain in WorldState. |
| `consequences.json` | Neutral staged consequence plans and their effect bundles. |
| `events.json` | Player-facing event cards, choices and presentation text after the player has legitimately learned of a world change. |

### 9.3 Scout Definition Files

```text
scouting/mission-types.json
scouting/focuses.json
scouting/outcome-tables.json
scouting/report-templates.json
```

| File | Responsibility |
|---|---|
| `mission-types.json` | Local surroundings reconnaissance and regional compass-sector missions, costs and availability requirements. |
| `focuses.json` | What a scout prioritises: faction signs, routes, hazards, locations, resources or witnesses. |
| `outcome-tables.json` | Scout-leg outcome tables and escalation effects, using the shared risk/outcome structure. |
| `report-templates.json` | Evidence-driven report wording, uncertainty language and follow-up-question hooks. |

### 9.4 Faction Definition Files

```text
factions/factions.json
factions/territorial-rules.json
factions/signatures.json
factions/reaction-rules.json
```

| File | Responsibility |
|---|---|
| `factions.json` | Values, baseline attitudes, leadership/contact data and visual references. |
| `territorial-rules.json` | Discoverable rules for passage, camping, mapping, graves, hunting and similar conduct. |
| `signatures.json` | Symbols, construction styles, markers and other evidence a player may later identify. |
| `reaction-rules.json` | Generic subscriptions to neutral World Triggers, conditioned by generated territory, claim, observation and current faction state. |

No file in this list stores a static mapping between a faction and a generated location.

---

## 10. Runtime State Required From the Generator

The WorldGenerator and savegame need persistent instance data, not new static content documents:

```text
LocationInstance
  anchor, definition IDs, state channels, runtime modifiers
  territorial context and optional generated faction relation
  present evidence IDs
  active projects and resolved scheduled consequences
  interaction history

RegionFactionAwarenessState
  faction ID, region, qualitative awareness state, decay and history

ScheduledConsequenceInstance
  source instance, resolved effect bundle, trigger condition, completed stages

EvidenceInstance
  evidence definition ID, source, subject, knowledge status and confidence
```

These data belong to `WorldState` and `KnowledgeState`, depending on whether they are objective world facts or player-acquired evidence. They are created by generation and play; they are not authored as static JSON content.

---

## 11. Worked Flow: Broken Bridge

1. The WorldGenerator places a terrain-appropriate `broken-bridge` on a route edge.
2. It derives territorial context from the finished territory map and may add generated runtime modifiers such as `Watched` or `FactionOwned` when justified.
3. The expedition inspects it. An engineer identifies deliberate dismantling; the player gains structural evidence, not the motive.
4. A free scout receives a local surroundings reconnaissance order. The returned report finds repaired warning posts and a recurring symbol.
5. If the faction is still unknown, the symbol remains an unresolved evidence thread. If it is known, the archive can link it to that faction's signature.
6. The expedition rebuilds the bridge. The outcome changes its operational state and raises `location-infrastructure-repaired`.
7. The World Reaction resolver checks this concrete bridge's generated state. It may do nothing, add faction awareness, create a warning, schedule a contact event or alter a faction relationship.
8. Any delayed reaction is stored and continues during later expedition and base days.

The same authored location therefore creates different stories without a faction being hardcoded into its JSON definition.

---

## 12. MVP Boundary

The Vertical Slice needs only:

- the two fundamental location paths: inspect and local scout reconnaissance
- evidence IDs and one recurring faction-signature thread
- neutral world triggers from location actions
- one faction reaction rule evaluated from generated runtime state
- one delayed consequence with two stages
- the broken bridge and one containment site as proof cases

Deferred:

- a large catalogue of consequences
- complex multi-faction conflicts at one location
- exhaustive generic reaction rules
- full interview content for every scout report
- dynamic migration of every faction and hazard type

The slice succeeds if the player can plausibly infer that a location matters to somebody, make an imperfect decision, see a delayed reaction, and use the resulting knowledge to plan a better later expedition.

---

## 13. Authoring and Validation Rules

1. A location outcome table must not contain a concrete faction ID.
2. Every `RaiseWorldTrigger`, `ScheduleConsequence`, evidence ID and action tag must reference a valid definition.
3. Every scheduled consequence must define its trigger condition and at least one effect stage.
4. A player-facing event must be gated by player knowledge, observation or explicit contact; World Truth alone is insufficient.
5. A local scout mission must require an available scout and create a mission/report state rather than instantly returning hidden truth.
6. Any major irreversible consequence must have at least one discoverable warning-evidence route.
7. Save data stores resolved consequences and generated relationships so later content balancing never rewrites past world history.

---

## 14. Related Concepts

- `location_archetypes_and_interactions_concept.md`: location model, states, actions, effects, generation placement and outcomes
- `risk_and_outcome_resolution_concept.md`: shared risk pipeline, outcome tiers, recovery and deferred effect resolution
- `scout_mission_cycle_concept.md`: scout orders, reports, persistence, symbols and regional faction awareness
- `factions_and_trade_concept.md`: territories, values, rules, contact, memory and world-phase reactions
- `world_locations_and_events_concept.md`: world persistence, event queue and world consequences

