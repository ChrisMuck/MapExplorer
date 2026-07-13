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

## 13. Next Concept Work: Deepening the Integration

**Status: open design work. This section records the next conceptual goals; it does not authorise implementation until each item has been discussed and accepted.**

The shared technical pipeline is only the foundation. The following work packages define how its parts become an exploration and interpretation game rather than isolated reports, score changes or location widgets.

### 13.1 Evidence, Hypotheses and Confirmed Knowledge

Evidence must be able to form player-facing threads: recurring signs, competing explanations, regional patterns and eventually well-supported hypotheses. The system must distinguish reported, inferred, confirmed, old, doubtful and contradicted knowledge without turning an inference into hidden World Truth. Archive analysis, specialists, contacts and repeated observations may improve confidence. This work package defines the model by which several reports become meaningful preparation for a later expedition.

**Current decision — player-owned hypotheses.** A hypothesis is a Player Note: the player creates,
names, links and revises it. It is never silently promoted into expedition knowledge merely because
the game has sufficient hidden World State.

**Optional interpretation assistance.** A campaign or accessibility/difficulty setting may enable
or disable assistance without changing simulation outcomes:

- **Off:** the player receives the individual reports, symbols and observations only and makes all
  connections through notes.
- **On:** the archive automatically shows neutral connections between compatible evidence: related
  reports, repeated symbols, a shared region, time window or location context. It may group these
  into an evidence thread, but it may not name the thread as an explanation, claim a motive, owner,
  faction or other hidden truth, or create a player hypothesis on the player's behalf.

Confirmed knowledge remains separate. It can arise only from an explicit confirming source such as
a direct observation, specialist interpretation, reliable contact, archive analysis or a later
event; the relevant source must be visible to the player.

### 13.2 Generated Context Changes an Intervention's Meaning

Generated context such as `deliberate-destruction`, `quarantine`, `collapsed-by-weather`, `sacred-use` or `smuggling-route` must be able to influence why an action is risky, useful or politically sensitive. The same reusable action may retain its mechanical identity while selecting different evidence, outcome weighting, consequence candidates or faction interpretation from the generated context. Static content still never assigns a faction to a location.

**Decision — layered, initially hidden context.** World generation gives a location one dominant
context and zero or more additional context tags. Several explanations may therefore be true at
once: a bridge can be structurally unsound and deliberately made impassable; a place can be both
ritually important and part of a smuggling route. The objective context is not initially known to
the expedition. Inspection, local scouting, specialists, encounters and later consequences reveal
only discoverable parts of it. An apparently deliberate destruction is not an explanation by
itself; the expedition still has to learn why it happened.

**Decision — actions are generic, availability is contextual.** Every archetype offers its
understandable basic actions. Generated context, current location state, expedition condition,
available equipment, assigned specialists and acquired knowledge can then block those actions or
unlock additional, situation-appropriate actions. Examples are deliberately generic rather than
bridge rules:

- poor morale can prevent a dangerous improvised approach;
- a missing relevant specialist can prevent a proper repair, analysis or safe intervention;
- an observed structural detail can unlock a cautious alternative;
- newly understood quarantine, ritual or territorial context can unlock restraint, warning,
  containment, communication or other context-appropriate responses.

The reason for a restriction is shown only when the expedition can reasonably know it. The system
must never explain a hidden context merely by displaying an objective failure reason.

**Decision — fair requirements and unknown risk.** A basic action whose clearly known requirement
is missing cannot be selected; the UI states the understandable reason, such as insufficient
morale, missing equipment or no relevant specialist. A plausible contextual alternative may also
be shown in this disabled form when its prerequisite is something the player could reasonably
anticipate — for example *"install a temporary safety line — requires a pioneer"*. This lets the
player mark the place and deliberately return better prepared. By contrast, an action that only
conflicts with still-hidden context remains attemptable. Its danger, failure or consequence is
discovered through play rather than revealed by an omniscient lock.

**Decision — meaningful interventions persist.** An action can permanently change a location and
the World State: a broken seal remains broken, an installed safety line remains present, a repaired
structure remains repaired, and a removed or damaged object does not silently reset. Content may
author later world reactions, deterioration or further changes, but there is no general automatic
restoration. One-time interventions and repeatable actions must be declared by content and use the
same persistent state model for every archetype.

**Decision — context does not require a faction.** Natural, historical and supernatural context
can affect risk, benefit, follow-up situations and delayed consequences even for an unclaimed
location. If a generated location is claimed, the claim and its depth of meaning are also part of
the hidden context: a faction may claim a place simply because it lies in its territory, or for a
more specific practical, cultural, protective or political reason. Content profiles define
possible contexts and effects; world generation assigns them to a particular world and may later
connect a claim without embedding a concrete faction ID in static location content.

### 13.3 Faction Reactions Create New Situations

Faction trust, anger, fear and attention are inputs to future play, not final score changes. A reaction must be capable of creating a warning, patrol, contact opportunity, request, blocked route, rumour, changed location state or other follow-up situation. The resulting situation again uses the same evidence, location, scout and world-trigger contracts.

**Decision — reactions do not automatically identify their source.** A reaction from an unknown
faction is initially presented as an unknown group, sign, patrol, warning, alteration or report.
It identifies the faction only when the situation provides a credible player-facing identification:
for example a direct encounter, an explicit warning, a message of thanks or an unmistakable act of
contact. Matching a known sign remains evidence only; it never lets the UI draw the conclusion for
the player.

**Decision — timing follows context and distance.** A faction response may occur immediately when
the action is witnessed or directly affects present people, at the end of the current day through
the World Phase, or after days, base time or a later expedition. The chosen timing is authored and
generated from the relevant context, distance, awareness and propagation. A released contagion,
for example, first needs time to spread before it can create evidence, affect a faction or cause a
response. Delayed reactions use the same deferred-consequence contract as all other world changes.

**Decision — factions take visible, persistent world turns.** During World Phase, factions may
create positive or negative situations appropriate to what they know, value and can reach: contact,
gratitude, a request, information, tolerated access, a safe route, warning, patrol, territorial
marking, protection, blockade, removal of an improvised structure, securing of a site or another
world change. These are persistent visible changes, events or evidence in the world rather than
only hidden relationship-number adjustments. The initial authored catalogue below is deliberately
broad enough to create new situations rather than a fixed reward/punishment table; individual
faction content and further reaction variants remain an ongoing authoring task.

#### 13.3.1 Faction Reaction Catalogue

The following generic reaction modes form the initial authoring catalogue. A reaction rule selects
from them according to generated context, what the faction knows, distance, capability, values,
territorial rules, specific memories and current Trust, Anger and Fear; no static location content
names the faction that will use a mode.

| Reaction mode | Player-facing form |
|---|---|
| Observe | patrol, trace, scout report or a period without direct approach |
| Signal | warning sign, renewed marker or indirect message |
| Contact | messenger, invitation, inquiry, demand, thanks or offer |
| Support | information, guide, supplies, medical aid, access or practical assistance |
| Intervene | secure, repair, seal, remove, protect or otherwise alter a place |
| Restrict | block a route, refuse trade, close access or impose conditions |
| Escalate | pursue, intercept, detain or create open hostility through the normal event/risk systems |
| Withdraw or Conceal | remove traces, hide a settlement, break contact or avoid the expedition |

Fear particularly favors withdrawal, concealment or short-term concessions; it must never be
treated as friendship. A direct escalation is fair only in clearly warned territory, an acute
threatening situation, or under a known exceptionally strict faction rule. Factions may apply
Intervene, Restrict or Support modes during the World Phase even while the expedition is elsewhere.
Reaction chains are valid: observation can become a signal, then contact, then support,
restriction or escalation depending on later player choices and world developments.

### 13.4 One Shared Lead System for Both Scout Paths

Directional reconnaissance and local surroundings scouting remain separate orders with separate availability and risk. Their reports should nevertheless create compatible leads: an unconfirmed location sighting, route hint, recurring sign, hazard indication, witness trace or possible link between places. This work defines how reports become actionable without automatically revealing objective map information.

**Decision — one evidence language, two search scopes.** Both scout paths create evidence and
neutral links that can be compared in the archive. They must not be merged into a single generic
order:

- **Directional reconnaissance** searches a selected compass sector beyond the expedition. It may
  report a possible location or landmark, faction sign, route, crossing, blockade, danger,
  environmental change, trace of people or creatures, or a possible connection to an earlier
  report.
- **Local surroundings scouting** searches only the immediate area of the current location. It
  clarifies local signs, use, traces, hazards and nearby context; it does not independently reveal
  routes, crossings, blockades or broad environmental changes.

All spatial evidence is deliberately approximate and player-facing language must never expose hex
coordinates or an exact target cell. A directional report may say *"east of here"*, *"beyond the
next ridge"* or *"somewhere to the north"*. A local report may, exceptionally, create a restricted
outward lead such as *"tracks lead east"* or *"there are indications of a crypt somewhere in the
north"*; this is a direction of further investigation, not a revealed location.

**Decision — evidence visibility.** A report begins as an archive item. A useful, still uncertain
lead may also create an approximate map or compass marker. A location, route or other world fact
becomes confirmed only through arrival, local investigation, repeated corroboration or another
explicitly reliable source. The optional interpretation assistance may point out neutral
relationships between reports, but must not identify their common owner or meaning — including
after the player has encountered a faction that visibly uses a matching sign.

### 13.5 Specialists Improve Interpretation Rather Than Only Access

Specialists should improve what the expedition can reasonably infer: an architect can identify deliberate structural work, a historian a ritual pattern, a medic contamination, and a linguist a social intent. This package defines evidence-quality changes, alternative interpretations and safe or respectful options. Specialists must not simply reveal the hidden answer.

**Decision — specialists improve evidence quality.** A relevant specialist may influence both
scout paths, during planning, report interpretation or later archive analysis. Their contribution
may improve clarity or confidence, add an additional compatible reading, identify a meaningful
detail, or make a cautious response available. It must never reveal the hidden explanation,
identity, faction ownership or exact destination. A linguist can, for example, distinguish a
formal warning from an ordinary mark; the player still decides who made it and what it implies.

### 13.6 Active Delayed Consequences

The separate deferred block in the implementation plan defines how delayed stages can later alter World State, create situations and offer responses. It must remain generic: a broken seal can lead to a threat, contamination, a discovery, political attention or no further escalation depending on authored and generated context; it is never a hardcoded crypt rule.

**Decision — delayed consequences are staged world processes.** A delayed consequence advances in
authored stages during the end-of-day World Phase: trigger, early signs, escalation or spread,
world impact and aftermath. It may pause, branch, worsen, be contained or resolve differently in
response to player actions and other world actors. Every potentially severe delayed consequence
must offer at least one perceptible warning and a genuine opportunity to respond before its major
impact; uncertainty may remain, but unavoidable hidden punishment is not acceptable.

**Decision — warnings create choices, not merely notifications.** Early signs may reach the player
through direct observation, scout reports, faction contacts, rumours, messages or later base
reports. They can be misinterpreted initially and may open a new inspection, containment,
observation, warning, avoidance, request-for-help or other context-appropriate option in the
relevant interaction. The player may act, prepare and return with a needed specialist, seek help,
or deliberately ignore the situation. Ignoring it remains a meaningful choice: the consequence may
continue and factions may later react to both the original act and the failure to respond.

**Decision — delayed outcomes can be positive, negative or mixed.** A consequence can create
danger, contamination, political attention, a changed route or loss; it can also reveal knowledge,
open access, create a contact, free a potential ally, provide a guide or lead to another discovery.
Mixed outcomes are desirable when they create a difficult interpretation or responsibility rather
than a simple reward. The initial consequence-family catalogue below is a common authoring
vocabulary; detailed stage templates and content combinations remain a later authoring block. The
shared process must support all archetypes and triggers without special-case crypt logic.

#### 13.6.1 Initial Consequence Families

Each scheduled consequence selects one primary family and may add at most one secondary family or
later transition. This keeps a situation readable while allowing the world to connect its systems.

| Family | Core change | Typical early signs | Typical responses |
|---|---|---|---|
| Propagation | A danger, contamination, disease, fire or other influence expands. | sick animals, altered plants, smoke, rumours, local absence | investigate, contain, warn, avoid, seek help |
| Release | Something formerly confined becomes active; the term is deliberately neutral. | movement, sounds, missing guards, renewed signs | secure, follow, contact, observe, re-contain |
| Route or Place Change | Access, safety or use of a route or site changes. | damage, new traffic, fresh markers, changed water | reroute, repair, negotiate, document, return prepared |
| Social Aftereffect | An action or its result changes relationships and creates communication. | a messenger, warning, gratitude, demand or rumour | respond, help, decline, ask for time, negotiate |
| Knowledge Trail | A development reveals a new pattern, record, connection or lead. | repeated signs, fragments, new testimony, visible alignment | document, analyse, follow the lead, consult a specialist |
| Rescue or Assistance | A person or group needs help, is uncovered or can offer limited support. | calls, an empty camp, survivors, a plea delivered by others | rescue, supply, escort, leave, seek assistance |
| External World Crisis | A natural or world-scale event changes conditions independently of expedition action. | ash, tremors, rising water, storm signs, displaced people | protect, investigate, aid, evacuate, adapt routes, coordinate |

Families can transition through player-facing stages: a Propagation consequence may create a Social
Aftereffect when a faction requests help; a Release may become a Knowledge Trail; an External World
Crisis may create route changes and rescue situations. The player receives the individual evidence
and situations, not a pre-solved statement of the family chain.

**Decision — external crises may be seeded or dynamic.** World generation may create persistent
susceptibilities where geography makes them meaningful, such as a volcano, floodplain, unstable
slope or exposed coast. The World Phase may also initiate dynamic crises not encoded as a fixed
map feature, such as storms or earthquakes. A sudden onset is allowed, but its continuing or major
effects must create observable signs, coherent context and a genuine chance to respond; it must not
become an unexplained unavoidable long-term punishment.

An External World Crisis can alter any compatible persistent location state. A storm or flood may
damage or destroy an intact bridge, including one repaired by an earlier expedition; a landslide may
close a known route; an eruption may expose or bury a site. The archive retains the earlier
confirmed state as history, while the changed state remains unknown until the expedition receives
new evidence or investigates again.

#### 13.6.2 Irreversibility, Capacity and Success

**Decision — escalation can cross a permanent threshold.** A world process may reach a stage from
which its original harm cannot be fully undone: an outbreak may take lives, an eruption may destroy
a place, and a released threat may escape. The expedition must still have meaningful later choices
to contain, mitigate, rescue, document, negotiate around or live with the changed world. A severe
outcome is therefore not erased merely because the player acts late, but it is never a hidden,
unanswerable punishment: its visible signs and a real opportunity to affect its course precede the
major threshold.

**Decision — success depends on the nature of the process.** A contained disease may have a fully
positive resolution. A volcanic eruption, earthquake or comparable external crisis normally cannot
be "won" in that sense; success may instead mean protecting people, preserving a route or record,
limiting secondary harm, earning trust or adapting well to a permanently changed landscape. Each
process defines context-appropriate success, partial success, failure and aftermath rather than
requiring every event to offer a universal best outcome.

**Decision — no harm displacement in the initial scope.** An intervention may have costs,
uncertain effectiveness or a persistent local trade-off, but it does not initially resolve a
process by secretly moving equivalent harm to another place. Deliberately redirecting a flood,
displacing danger or similar cross-place dilemmas are a later extension once the core process model
is proven readable.

**Decision — concurrent world activity, bounded acute surface.** Several processes may run in the
World Phase at once. At most one or two require an immediate player decision at the same time;
others remain as early signs, non-urgent reports or background developments until they become
actionable. This is a presentation and pacing limit, not a promise that inactive processes pause.

### 13.7 Persistent History and Later Expeditions

The world must retain enough history for later expeditions to encounter repaired routes, damaged sites, changed faction attitudes, old warnings, unresolved situations and lost or secured knowledge. The player should inherit useful but imperfect preparation, while the world retains objective consequences that the player may not fully understand yet.

**Decision — archive delivery determines secured knowledge.** Only knowledge, reports and finds
that reach the base become secured archive knowledge for later expeditions. An expedition that
fails before returning leaves its unsecured field knowledge and finds behind; they are not silently
added to the archive and may later create recovery objectives. Player-created map markers persist
as Player Notes, including after a loss, but they are never proof of a current world state.

**Decision — map memory is a last-known record.** A later expedition retains the known existence
and approximate position of a previously found bridge, grave, ruin, route or former boundary.
Meanwhile the World Phase continues during base time and between expeditions: factions, natural
processes and earlier consequences can alter these places. The map does not receive omniscient
updates. Changed conditions are learned only through a new scout report, contact, evidence,
arrival or local investigation.

**Decision — information ages rather than being overwritten.** Earlier reports and archive entries
remain historically available and acquire an *old* status when later time or evidence makes their
current reliability uncertain. A later expedition must reach and inspect a known location again to
know its current state. It may find an earlier persistent intervention, such as a safety line,
still present, removed, damaged or changed; the archive can state that it was installed, but cannot
claim that it remains usable without renewed evidence.

### 13.8 Archetypes Need Distinct Questions

The common interaction pipeline must not flatten all locations into the same choice. Each archetype needs a recognisable kind of uncertainty:

- route obstacle: access, danger, bypass and control
- investigation site: interpretation, recovery, respect and loss
- resource site: use, exploitation, dependency and restraint
- territorial marker: meaning, permission and provocation
- contact site: intent, language and trust
- hazard zone: cause, spread, avoidance and containment
- containment site: protection, temptation and release
- natural phenomenon: observation, research, interpretation and respectful intervention

New variants and content profiles should deepen these questions through data, not require bespoke simulation code.

**Decision — the archetype taxonomy is extensible.** The listed families are an initial set, not a
closed catalogue. New primary archetypes may be added through data as long as each contributes a
recognisable exploration question and works through the shared location, evidence, context,
outcome and persistence contracts. A generated location has one primary archetype; it gains
complexity through variants, states and generated context rather than by becoming several unrelated
archetypes at once.

**Current taxonomy decisions.** Camps, wrecks, abandoned settlements and the remains of earlier
expeditions are variants of an Investigation Site rather than a separate Trace Site family. A
Natural Phenomenon is a distinct primary archetype because its central purpose is research and
interpretation: springs, unusual trees, caves, peaks, geological formations and similar places may
be studied, observed or approached with restraint without being reduced to a resource or a hazard.
Inhabited settlements, outposts and other active community places remain Contact Site variants;
they do not require a separate settlement archetype. The initial set is deliberately expanded by
the Natural Phenomenon archetype; further families remain data-driven additions after the core loop
proves them necessary.

**Decision — shared floor, different opportunities.** Every location offers *inspect*, *scout the
surroundings* and *leave* as its common interaction floor. Further actions arise from its primary
archetype, state, discovered context and expedition capabilities. Leaving is always a valid player
decision, even when it leaves an opportunity, risk or delayed consequence unresolved.

**Decision — not every location needs a dramatic payoff.** An archetype may offer a persistent
change, information, risk, a follow-up lead, a resource or none of these beyond atmosphere and
observation. A camp may only point toward a crypt; a crypt may be empty. Such observations still
become unsecured field knowledge with a later archive-analysis value if the expedition returns.
The content profile decides which opportunities are present; the generic system does not require
every location to contain an intervention, reward or world-changing outcome.

### 13.9 Operational Fairness and World-Facing Feedback

The integration rules above establish what can happen. The following remaining design work defines
how the player can understand, plan around and fairly respond to an active world without exposing
hidden truth:

1. **Action duration and interruption:** distinguish immediate actions, day-consuming work and
   multi-day projects; define partial progress, abandonment and persistent incomplete work.
2. **Observation and attention:** define how factions, witnesses, patrols, signs and rumours can
   actually notice an action or its aftermath, and how attention changes or fades.
3. **Open situations and urgency:** define requests, warnings, response windows, expiry and the
   consequences of deliberate non-response.
4. **Player-known cause chains:** archive and journal entries must connect a known action, report,
   reaction and later state change without inventing an unknown motive.
5. **Multiple stakeholders:** decide the MVP scope when neutral world processes, one or more
   factions and other interested groups all relate to one location.
6. **Interaction-state communication:** consistently distinguish known requirements, unknown risk,
   newly available choices, old information and urgent opportunities in the UI.
7. **Active-world pacing:** prioritize, group and pace simultaneous reports, requests and delayed
   consequences so the world feels alive without flooding the player.

These are concept decisions, not implementation instructions. They are worked through in the order
listed, because fair timing and observation must be known before urgency, presentation and pacing
can be authored.

#### 13.9.1 Action Duration and Interruption

**Decision — three action commitments.** Location content classifies an offered action as an
immediate action, a day-consuming operation or a multi-day project. Immediate actions do not
advance the World Phase by themselves. A day-consuming operation prevents further expedition
progress that day: choosing it is deliberately a choice to spend time, expose the expedition to the
World Phase and postpone movement. A project occupies the main expedition for each committed day
until it is completed, suspended or abandoned.

**Decision — projects pause only at a day boundary and persist.** A project can be suspended or
abandoned at the end of a committed day, never by erasing the work already performed. Its partial
progress, consumed resources, incomplete structure, local risks and visible traces become
persistent World State. A later expedition may resume an unfinished project with a different team
and after an arbitrary amount of time, subject to the changed world context and then-current
requirements.

**Decision — duration is an honest estimate.** When the expedition can reasonably assess a task,
the UI presents an approximate duration such as *"about 2–3 days"*. Hidden difficulties may extend
the work, but a material extension must first create a perceptible warning or decision point; the
simulation must not silently turn a comprehensible commitment into an arbitrary loss of time.

**Decision — free scouts remain independent.** While the main expedition is committed to a
day-consuming operation or project, scouts who are otherwise ready and free may still receive
local or directional missions under their normal availability, duration and risk rules. They do not
remove the main expedition's time commitment or make its project safe.

#### 13.9.2 Observation and Attention

**Decision — factions possess limited information, not World Truth.** A faction may have more
people, local presence and observation capacity than the expedition, but it is not all-knowing. It
reacts only to direct witnesses, patrols, guards, later-discovered traces, travelling people,
rumours, messages or other authored information channels. Its information may be incomplete,
delayed or mistaken; faction reactions express what that faction reasonably believes, not an
authoritative explanation of what objectively happened.

**Decision — observation is local and spreads through the world.** A witnessed action initially
creates attention near its source. Its awareness can move over time through settlements, routes,
trade, messengers, patrols, signal sites, sacred places and other context-appropriate networks.
Distance, accessibility, local significance and the action's visible aftermath govern timing. An
event far across the island cannot immediately create informed reaction from a distant faction
without an actual connection between them.

**Decision — attention fades; meaningful memory persists.** Transient attention such as a patrol's
unconfirmed sighting can decay when no further evidence supports it. Significant acts, repeated
behaviour, direct contact and major consequences may create durable faction memories. Persistent
world changes can later renew attention when another observer encounters them.

**Decision — only earned signs reach the player.** The player receives no percentage chances,
hidden-observer indicators or debug-like awareness state. Visible guards, patrols, signs, routes,
known territorial rules, messages and rumours can support an inference that an action may be
noticed; absence of a clue never guarantees secrecy.

#### 13.9.3 Open Situations and Urgency

**Decision — direct situations permit meaningful replies.** A direct warning, request or offer
may present several context-appropriate responses: accept, decline, ask for time, offer an
alternative, seek clarification or leave without commitment. Asking for time creates a visible
social commitment rather than a free delay. Failing to return or communicate after such a promise
can create a durable faction memory of unreliability, even when the original request later becomes
impossible or irrelevant.

**Decision — urgency is clear but not numerically exposed.** The player sees qualitative urgency
such as *immediate*, *soon*, *there is still time* or *timing unclear*, never a hidden exact
countdown. The authoring model retains the actual response window and may change its visible
urgency as stages advance.

**Decision — expiry branches by context.** An unanswered, refused or expired situation resolves
through its generated context and current relationships: disappointment, lost opportunity,
increased danger, changed access, a later request, reduced trust, a resolved crisis or hostility
are all possible. Expiry is not an automatic hostility rule.

**Decision — open situations are recorded, not questified.** An accepted or otherwise relevant
situation appears in the journal/archive and may create an approximate map or compass reminder
when the expedition has earned one. It never supplies a precise quest path or objective-world
marker. If the current expedition lacks the ability to help, honest communication can
contextually earn time, assistance or an alternative response; it is not guaranteed to do so.

#### 13.9.4 Player-Known Event Trace

**Decision — important knowledge carries provenance.** Where known, every important report,
finding, request, world change or archive entry records its source, day and location or approximate
region, together with the source's normal reliability. This is the player-facing provenance of
knowledge, not access to the hidden event resolver.

**Decision — the archive presents chronology, not invented causality.** The journal can place
known facts in a factual sequence: *"day 12: bridge repaired; day 15: warning posts found; day 18:
a messenger arrived."* It must not assert that one caused another unless a visible source directly
states that relationship. An explicit message such as *"we saw what you did at the bridge"* may be
recorded as direct evidence; ordinary temporal proximity remains a player interpretation.

**Decision — hypotheses attach to evidence.** Player Notes may link to reports, findings,
interactions and chronology entries so that the player can record and revise their own explanation.
Archive assistance may surface neutral related entries, never create or validate that explanation.

**Decision — later evidence preserves history.** A newer report can link back to earlier entries
and mark their current status as old, doubtful or contradicted. It does not rewrite or delete the
earlier observation, its source or the player's historical decision context.

#### 13.9.5 Multiple Stakeholders

**Decision — bounded MVP scope.** At the start, a location has at most one primary claiming or
directly responsible faction. It may additionally host a deliberately limited number of neutral
world processes, such as natural danger, contamination, historical context or a delayed
consequence. Content and generation must keep this combination readable rather than stacking
unbounded simultaneous complications.

**Decision — contested and secondary faction interest are deferred.** A second faction developing
an interest in a location, contested locations and multi-faction disputes are later extensions of
the same framework, not initial MVP requirements. They must be designed separately before content
uses them.

**Decision — other actors remain possible, but need their own design.** Individual survivors,
independent groups, traders, freed beings and similar non-faction actors may become stakeholders,
but the initial framework must not silently treat them as factions. Their identity, agency,
persistence and relationship to the expedition are defined in Section 13.10.

**Decision — separate evidence remains separate.** When more than one known process or actor
affects a place, the player receives the respective reports, signs and observations independently.
The game does not summarize these into an explained conflict; the player can form that
interpretation through notes and chronology.

#### 13.9.6 Interaction-State Communication

**Decision — every action has a readable text state.** Available actions, known requirement locks,
known risk, newly available options, active work, completed interventions and time-critical
opportunities each carry a short explicit text status. Icons and color may reinforce the state, but
must never be the sole channel of meaning.

**Decision — unknown risk is not artificially signposted.** An action is marked as risky only when
the expedition has earned a concrete indication of danger. Without such evidence it remains
neutrally presented; the UI must not manufacture a warning from hidden World State.

**Decision — new options disclose their known basis, not their outcome.** A newly unlocked action
briefly identifies the player-known observation, report or capability that made it plausible, such
as *"new possibility: the current has been observed"*. It never exposes success chance, hidden context or the
actual result.

**Decision — relevant context is local as well as archived.** When the expedition views or reaches
a location, its interaction panel shows linked old information, current known project state and any
known open or urgent situation that concerns that location. The same items remain in journal and
archive as their chronological records; the location panel is a contextual view, not a second
source of truth.

#### 13.9.7 Active-World Pacing

**Decision — only acute decisions interrupt.** Immediate interruption is reserved for direct
communication requiring a response, acute danger to the current expedition and other decisions
that cannot fairly wait. Reports, distant developments and non-urgent discoveries do not
automatically stop play.

**Decision — minor developments are gathered into readable summaries.** Smaller reports and world
updates are grouped into an end-of-day report or another calm summary rather than opening a chain
of separate popups. The system prioritizes relevant and urgent situations, while lower-priority
entries remain available in the report list, journal and archive.

**Decision — urgency is bounded at the surface.** The world may continue to hold several active
processes, but only a limited, readable set of urgent situations is surfaced at one time. Additional
developments continue in the background and are reported when they become actionable, relevant or
part of a summary; they are not discarded.

**Decision — reading is the player's choice.** Non-urgent reports may remain unread and can be
opened later. Their underlying world processes, deadlines and faction behaviour continue while the
player explores, works or postpones reading them.

**Decision — quiet days are intentional.** The World Phase keeps running even when it has no new
player-facing report. Calm periods are necessary for movement, atmosphere and deliberate
exploration; the game should not equate a living world with constant notifications.

### 13.10 Non-Faction Actors and Temporary Companions

Non-faction actors are people or beings with individual, local or temporary relevance. They are not
automatically abstracted into a faction and never create a second player-controlled expedition.

| Role | Duration | Contribution |
|---|---|---|
| Encounter | one interaction | information, trade, warning, request or choice |
| Protected person | until a safe destination or base | responsibility plus testimony, gratitude, future contact or other authored value |
| Local companion | limited to a region, task or agreement | guide, interpretation, local access, warning or practical help |
| Recruit | persistent after return to base | becomes a named regular expedition member through an explicit base decision |

**Decision — companionship is an uncertain choice.** A protected person or temporary companion
travelling with the expedition consumes Supplies like any other person. The player sees the
person's observable condition, immediate need and the cost of taking responsibility, but not a
guaranteed mechanical benefit. A companion may later provide local knowledge, route guidance,
social legitimacy, interpretation, practical assistance, a future contact, archive insight or a
credible route toward recruitment. Their information may also duplicate what the expedition already
knows, corroborate an earlier report without resolving it, or prove less useful than hoped.
Accepting a companion is therefore not presented as an optimization card or a promise of reward.

**Decision — care can slow the expedition.** A wounded, exhausted or otherwise protected person
can impose a visible escort burden that slows the main expedition until the person reaches a safe
destination, recovers, leaves or is handed over. A healthy local guide normally consumes Supplies
without this movement penalty. Content may present an additional known practical requirement when
the person's condition makes it plausible, but must not invent a hidden logistics penalty.

**Decision — non-assistance can become history.** Leaving or refusing to help remains a valid
player choice, not a forbidden action. Depending on the observable situation, later witnesses,
local ties, faction values and what happens in the World Phase, it may lead to a lost opportunity,
injury or loss of the person, a recovery lead, a rumour, a faction memory, renewed contact or no
further consequence. The player is never punished by an omniscient morality system: only people or
processes that could plausibly know of the choice can react, and the expedition may have
context-appropriate alternatives such as first aid, supplies, a message, a marker or a promise to
return.

**Decision — assistance has several possible scales.** When the circumstances support them, an
encounter may offer four broad response shapes: take the person along and protect them; provide
limited immediate help such as medicine or supplies; organize help through a message, marker or
known local contact; or decline and continue. Content selects only the options that are plausible
for the particular situation. A limited intervention can still fail, create a promise or lead to a
later situation; it is not a guaranteed substitute for taking responsibility.

**Decision — one main expedition remains intact.** A temporary companion travels with the main
group, cannot be dispatched as an independent scout and cannot form a separately controlled party.
Their contribution is contextual rather than a universal extra specialist: a local guide may make
one region traversable or interpretable, but does not solve unrelated locations. Permanent
recruitment is considered only after the person reaches the base and agrees to join; field
companionship alone does not silently add a roster member.

### Recommended Discussion Order

1. Evidence, hypotheses and confirmation
2. Shared scout leads and specialist interpretation
3. Generated context and context-sensitive outcomes
4. Faction-created situations
5. Persistent history across expeditions
6. Active delayed consequences
7. Archetype content expansion using the agreed systems
8. Operational fairness and world-facing feedback

---

## 14. Authoring and Validation Rules

1. A location outcome table must not contain a concrete faction ID.
2. Every `RaiseWorldTrigger`, `ScheduleConsequence`, evidence ID and action tag must reference a valid definition.
3. Every scheduled consequence must define its trigger condition and at least one effect stage.
4. A player-facing event must be gated by player knowledge, observation or explicit contact; World Truth alone is insufficient.
5. A local scout mission must require an available scout and create a mission/report state rather than instantly returning hidden truth.
6. Any major irreversible consequence must have at least one discoverable warning-evidence route.
7. Save data stores resolved consequences and generated relationships so later content balancing never rewrites past world history.

---

## 15. Related Concepts

- `location_archetypes_and_interactions_concept.md`: location model, states, actions, effects, generation placement and outcomes
- `risk_and_outcome_resolution_concept.md`: shared risk pipeline, outcome tiers, recovery and deferred effect resolution
- `scout_mission_cycle_concept.md`: scout orders, reports, persistence, symbols and regional faction awareness
- `factions_and_trade_concept.md`: territories, values, rules, contact, memory and world-phase reactions
- `world_locations_and_events_concept.md`: world persistence, event queue and world consequences
