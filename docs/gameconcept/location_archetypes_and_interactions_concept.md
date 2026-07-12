# Location Archetypes and Reusable Interaction Concept

Working Title: **Untitled Expedition Game**

Status: **Proposed specialist concept**

Purpose: This document defines the reusable gameplay model for locations, map objects, obstacles, hazards, contact points and temporary situations. It exists to prevent every concrete map object from requiring its own bespoke gameplay code.

If accepted, this document should become the source of truth for:

- location archetypes
- reusable location actions
- location states
- location modifiers
- requirements, costs, risks and outcomes
- multi-day location projects
- persistent location changes
- authored and procedural location variants
- the shared risk and outcome resolution pipeline used by locations, scouts and social interactions alike (Section 9)
- variant/modifier conflict resolution (Section 2.7)

The existing `world_locations_and_events_concept.md` remains the source of truth for world structure, tone, mysteries, large-scale consequences, event categories, and passive Base Phase transitions (Section 9.16).  
The existing `expedition_and_members_concept.md` remains the source of truth for expedition resources, specialists, daily capacity and conflict.  
The existing `knowledge_base_and_analysis_concept.md` remains the source of truth for findings, unsecured knowledge, analysis and Knowledge Points.  
The existing `factions_and_trade_concept.md` remains the source of truth for Trust/Anger/Fear values consumed as risk inputs in Section 9.4B.  
The existing `map_and_exploration_concept.md` remains the source of truth for scout order structure and Fog of War, cross-checked against Sections 9.10A and 4.5 respectively.

---

## 1. Core Design Decision

Concrete map objects must not own bespoke gameplay logic unless they introduce a genuinely new system.

A marked grave, underground vault and ruined shrine may look different and contain different clues, but they can use the same investigation pattern.

A cold fire, abandoned expedition camp and improvised shelter may contain different evidence and rewards, but they can use the same trace-site pattern.

A broken bridge, flooded ford and collapsed tunnel may block movement differently, but they can use the same route-obstacle pattern.

The core model is:

> **Primary Archetype + Variant + Modifiers + Content Profile + Runtime State**

### Primary Archetype

Defines the dominant interaction pattern.

Examples:

- investigate evidence left behind
- study a physical place
- overcome a blocked route
- interpret a territorial warning
- approach a faction contact point

### Variant

Defines what the location appears to be.

Examples:

- marked grave
- ruined shrine
- broken bridge
- cold campfire
- fishing village

Variants provide presentation, authored text, default actions and compatible content. They do not require unique code.

### Modifiers

Add or alter reusable behavior.

Examples:

- Sacred
- Faction-Owned
- Sealed
- Unstable
- Contaminated
- Occupied
- Watched
- Repairable
- Campable

Modifiers may:

- add or remove actions
- add requirements
- change risk
- alter costs
- add faction consequences
- change available outcomes
- change presentation

### Content Profile

Defines the authored content for a specific location instance or variant.

Examples:

- visible description
- hidden purpose
- clues
- possible findings
- knowledge rewards
- faction links
- journal text
- delayed consequences
- false interpretations
- visual references

### Runtime State

Stores what has happened to this location in the current world.

Examples:

- discovered but not inspected
- searched and exhausted
- bridge still blocked
- seal damaged
- resource depleted
- faction post abandoned
- hazard spreading
- situation expired

---

## 2. Extensibility Principles

### 2.1 Composition Over Inheritance

The system should not create classes such as:

```text
MarkedGraveLocation
AncientMarkedGraveLocation
BorderWardenMarkedGraveLocation
ContaminatedBorderWardenMarkedGraveLocation
```

Instead:

```text
Archetype: Investigation Site
Variant: Marked Grave
Modifiers:
- Sacred
- Faction-Owned
- Contaminated
```

The concrete place is assembled from reusable definitions.

### 2.2 Stable IDs, Not a Closed Enum

The initial ten archetypes are the MVP library, not a permanently closed list.

Archetypes, variants, modifiers, actions, effects and content profiles must use stable registry-backed IDs.

Examples:

```text
trace-site
investigation-site
route-obstacle
modifier-sacred
action-investigate
effect-add-finding
```

Adding a new archetype later should not require changing every existing location or save file.

### 2.3 Definition Data Is Separate From Runtime State

A location definition describes what a location can do.

A location instance stores what has happened in this generated world.

Definitions should be authored outside gameplay code, using the repository's chosen data format.

Runtime saves should primarily store:

- stable definition IDs
- placement
- current state channels
- active projects
- consumed content
- generated outcomes
- history references

They should not duplicate the full authored definition.

### 2.4 Generic Systems Before New Archetypes

A new concrete object should normally be implemented by:

1. selecting an existing archetype
2. selecting or creating a variant
3. adding modifiers
4. assigning content
5. configuring actions and outcomes

A new archetype is justified only when the dominant player interaction cannot be expressed cleanly through the existing ten patterns.

### 2.5 No Hidden Variant Switches

Gameplay code must not contain logic such as:

```csharp
if (location.VariantId == "marked-grave") { ... }
```

Concrete variant IDs may be used for presentation, content selection and authored references, but generic gameplay resolution must use archetypes, actions, requirements, modifiers, states and effects.

### 2.6 Graceful Expansion

New actions, requirements, modifiers and effects should be registered through small handler interfaces.

Adding one new reusable effect may require code once.

Adding twenty new locations that use that effect should require data only.

### 2.7 Conflict Resolution

Variants and Modifiers may both add or remove actions (Section 6, pipeline steps 3-5). This section states what happens when they disagree, since neither the pipeline order alone nor an unordered set of active modifiers resolves every case on its own.

**Governing principle — Conservative Resolution:** when two layers or two simultaneously active modifiers disagree about the same thing, the resolution that makes the location harder to access, more restricted, or more clearly gated wins over the one that makes it easier or less gated. Every rule below is an instance of this one idea, not an independent decision.

**Rule A — Sequential Layer Override.** For a specific action, whichever layer touches it *last* in the pipeline order (Archetype, then Variant, then Modifier) determines its final add/remove state, in either direction. A Modifier can re-add something a Variant removed, and a Variant can remove something the Archetype defaulted to including — later layers are more specific to the concrete situation than earlier ones.

```text
Archetype default:  action-X is included
Variant:             removes action-X
Modifier:             adds action-X back
Result:               action-X is available (Modifier is last, so it wins)
```

**Rule B — Removal Precedence Within the Modifier Layer.** A location instance can have several Modifiers active at once, and there is no natural order among them — requiring authors to carefully order a `modifierIds` list to get correct behavior would be a fragile authoring hazard. Instead: among all currently active modifiers, if *any* removes a given action, it is removed, even if another active modifier would add it. This makes the modifier layer's result independent of list order.

```text
Active modifiers: [Repairable, Sealed]
Repairable:  adds action-rebuild-bridge
Sealed:       removes action-rebuild-bridge (can't rebuild what's still sealed shut)
Result:       action-rebuild-bridge is not available, regardless of list order
```

**Rule C — Requirements and Costs Stack; Waivers Lose to Impositions.** Hard Requirements from different sources are already a logical AND by definition (Section 7.3) — every active Hard Requirement from every source must be satisfied simultaneously, so two modifiers each adding a requirement simply both apply. The narrower conflict case is one modifier *imposing* a requirement while another *waives* the identical requirement: the requirement stays imposed. Costs and risk-related Situational Modifiers are unaffected by this rule — they already stack additively by design and need no separate conflict handling.

```text
Guarded:     imposes "requires faction permission" on action-approach
Invitation:  waives "requires faction permission" on action-approach
Result:      the requirement stays imposed (imposed beats waived)
```

**Rule D — Impossible Combinations Are an Authoring Failure, With a Runtime Failsafe.** Two active modifiers can jointly make an action's requirements unsatisfiable (e.g. one requiring `Operational State = Sealed`, another requiring `Operational State = Open`, when a location holds exactly one Operational State at a time). Modifier Compatibility declarations (Section 12.2) and Authoring Validation (Section 19) are meant to catch this before it ships. As a runtime failsafe if a bug lets an impossible combination through anyway: an action whose requirements can never jointly be satisfied resolves as **removed**, not as a broken or unreachable-but-visible action — Rule B's Removal Precedence extended one level, treating the contradiction itself as equivalent to an active removal.

**Dormant Modifiers.** A modifier can remain listed on a location instance's `modifierIds` (for history and journal purposes) while contributing nothing right now, if its own declared applicability condition isn't currently met — see Section 5.5.

---

## 3. Core Runtime Model

A map location is composed of the following layers.

```text
Location Definition
├── Archetype
├── Variant
├── Default Modifiers
├── Action Templates
├── Content Profile Rules
├── Compatible Anchors
└── Generation Rules

Location Instance
├── Stable Instance ID
├── Definition IDs
├── Map Anchor
├── Runtime Modifiers
├── State Channels
├── Consumed Content
├── Active Project
├── Scheduled Consequences
└── Interaction History
```

### 3.1 Definition Types

The system should support these reusable definition types:

- `LocationArchetypeDefinition`
- `LocationVariantDefinition`
- `LocationModifierDefinition`
- `LocationActionDefinition`
- `RequirementDefinition`
- `CostDefinition`
- `RiskProfileDefinition`
- `OutcomeTableDefinition`
- `EffectDefinition`
- `ContentProfileDefinition`
- `ClueDefinition`
- `LocationStateDefinition`
- `LocationProjectDefinition`

These names describe responsibilities, not mandatory C# class names.

### 3.2 Definition Layer, Instance Layer and Player-Knowledge Layer

The game must keep three things separate:

#### World Truth

What the location objectively is.

Examples:

- the grave contains a containment seal
- the bridge was deliberately destroyed
- the abandoned camp was staged
- the warning stones belong to the Border Wardens

#### Runtime World State

What has physically happened.

Examples:

- the grave was opened
- the bridge was repaired
- the camp was searched
- the warning stones were removed

#### Player Knowledge

What the expedition or archive currently believes.

Examples:

- possible grave
- scout-reported bridge
- confirmed abandoned camp
- suspected faction border
- doubtful interpretation

The UI must never read hidden world truth directly.

---

## 4. Map Anchors

Locations are not always single-hex objects.

The location system must support several placement forms.

### 4.1 Point Anchor

A single hex.

Used for:

- grave
- campfire
- watchtower
- spring
- village entrance

### 4.2 Edge Anchor

A connection between two adjacent hexes.

Used for:

- broken bridge
- closed gate
- blocked ford
- collapsed pass connection
- rope crossing

An edge location may alter movement between the two connected hexes without blocking either hex itself.

### 4.3 Area Anchor

A set of hexes or a generated region.

Used for:

- toxic swamp
- predator range
- ruined settlement
- sacred grove
- unstable ground

### 4.4 Path Anchor

An ordered sequence of connected hexes or edges.

Used later for:

- damaged road
- patrol corridor
- ritual procession path
- long wall section
- spreading contamination route

Point, edge and area anchors are required for the MVP. Path anchors may be added later, but the model must not prevent them.

### 4.5 Interaction Range

Every action defined so far in this document implicitly assumes the expedition is at or adjacent to the location's anchor before the action is offered — Section 7.3 even lists "adjacent position" as an example Hard Requirement. This default holds for most archetypes, but breaks on purpose for `Landmark Site` (Section 11.9): a signal tower's entire point is being visible and partially usable from several hexes away, before the expedition physically reaches it.

Each action may therefore declare an **interaction range**, defaulting to on-anchor/adjacent (the behavior every other archetype in this document already assumes):

```text
action-observe-from-distance:
  interactionRange: visibleAtDistance
  # requires the modifier-visible-at-distance or modifier-multi-region-visible
  # to be active on the location; otherwise falls back to adjacent
```

Only Landmark Site variants are expected to override this in the MVP. "Can be seen" (fog of war, visibility) and "can be acted on" (interaction range) are related but distinct questions — this should be cross-checked against `map_and_exploration_concept.md`'s Fog of War rules before Landmark Site content is authored.

---

## 5. State Model

A single global location-state enum would become unmanageable.

Locations therefore use several independent state channels.

### 5.1 Knowledge State

Shared with the map-knowledge system.

```text
Unknown
Reported
Discovered
Confirmed
Old
Doubtful
Lost
```

This state describes knowledge reliability, not physical condition.

### 5.2 Interaction Progress

Common progression channel.

```text
Untouched
Observed
Inspected
Investigated
Exhausted
```

Not every location must use every value.

### 5.3 Operational State

Archetype-specific physical or functional state.

Examples:

For a route obstacle:

```text
Blocked
RiskyPassage
TemporarilyOpen
Open
Repaired
Destroyed
```

For a containment site:

```text
Intact
Weakened
PartiallyOpened
Opened
Breached
Reinforced
Resealed
```

For a resource site:

```text
Available
Low
Depleted
Renewing
Contaminated
```

### 5.4 Presence State

Used when people, creatures or active groups may occupy the location.

```text
Empty
Occupied
Watched
Contested
Abandoned
Unknown
```

### 5.5 Temporary Flags and Modifiers

Conditions such as `Burning`, `Flooded`, `RecentlyVisited` or `UnderObservation` should normally be modifiers with duration or expiry, not permanent additions to a giant state enum.

A modifier can also be **dormant**: present on a location instance's `modifierIds` list (for history and journal purposes) while contributing nothing right now, because its own declared applicability condition isn't currently met.

```text
modifier-unstable:
  appliesWhenState:
    operational: [Blocked, RiskyPassage]
  # once Operational State reaches Repaired, Unstable is still listed
  # on the instance but is dormant - it contributes no risk, no
  # requirement, no action change, until/unless the state regresses.
```

This is the same underlying idea as state-gated Risk Inputs (Section 9.5A) extended from risk values to modifier activity in general — a location's current state decides what actually applies right now.

### 5.6 State Transitions

Actions and world events change state through declared transitions.

Example:

```text
RouteObstacle.OperationalState:
Blocked
  -> RiskyPassage        after "Create Rope Crossing"
  -> Repaired            after "Rebuild Bridge"
  -> Destroyed           after major collapse
```

Invalid transitions must be rejected by validation.

---

## 6. Common Interaction Pipeline

Every location interaction follows the same high-level flow.

1. Determine what the expedition currently knows.
2. Present only observable information.
3. Gather action templates from the archetype.
4. Apply variant additions and removals.
5. Apply modifier additions and removals.
6. Evaluate visibility conditions for each action.
7. Evaluate hard requirements and soft advantages.
8. Calculate costs, duration and qualitative risk.
9. Present available actions and known consequences.
10. Resolve the chosen action.
11. Apply immediate effects.
12. Change state channels.
13. Create clues, findings, knowledge and records.
14. Register delayed consequences.
15. Update journal, map and archive references.
16. Re-evaluate the location for future interaction.

Concrete locations do not bypass this pipeline.

---

## 7. Action Model

An action is a reusable operation offered by one or more archetypes.

### 7.1 Initial Action Library

The initial action library may include:

```text
Observe
Inspect
Search
Investigate
Document
Interpret
Collect
Take
LeaveOffering
Respect
Disturb
Cross
FindBypass
Repair
Construct
Open
ForceOpen
Seal
Reinforce
Activate
Deactivate
Communicate
Trade
RequestPassage
Assist
Treat
Wait
Camp
Mark
Leave
```

Not all of these need full MVP implementation on day one. They define the intended vocabulary.

### 7.2 Action Properties

Each action definition may specify:

- stable action ID
- player-facing label
- description
- action family
- visibility conditions
- hard requirements
- soft requirements
- costs
- duration
- base risk profile
- specialist contributions
- outcome table
- state transitions
- repeat policy
- interruption rules
- UI presentation hints
- journal text templates

### 7.3 Hard Requirements

Hard requirements make an action unavailable.

Examples:

- a specific operational state
- enough Capacity
- a required tool
- minimum Supplies
- adjacent position
- permission from a faction
- a previously discovered clue
- a valid edge connection

Hard requirements should be used sparingly.

### 7.4 Soft Requirements

Soft requirements do not block an action. They improve it.

Examples:

- Engineer reduces repair time
- Scholar improves interpretation
- Scout reveals ambush risk
- Medic reduces contamination danger
- Soldiers reduce forced-entry danger
- Carriers allow recovery of heavy objects

This supports the rule that specialists create options and safer approaches rather than functioning only as keys.

### 7.5 Costs

Actions may consume:

- movement points
- remaining day
- full days
- multiple project days
- Supplies
- Medicine
- Morale
- Capacity
- equipment durability
- trade goods
- faction goodwill
- personal risk to selected members

Knowledge Points are not a normal field-action cost.

### 7.6 Repeat Policies

Every rewarding action must declare a repeat policy.

Examples:

```text
OncePerLocation
OncePerState
OncePerExpedition
RepeatableWithCost
RepeatableUntilDepleted
RepeatableAfterWorldRefresh
```

This prevents knowledge and resource farming.

---

## 8. Requirements and Specialist Contributions

Requirements must be declarative.

Initial reusable requirement kinds:

- role present
- minimum role count
- member condition
- item or equipment tag
- minimum resource
- capacity available
- location state
- modifier present or absent
- clue known
- archive entry known
- faction access level
- faction permission
- map position
- world time
- project not already active

Specialists can contribute in five reusable ways:

### Unlock

Reveals an action that would otherwise be unavailable.

### Reduce Cost

Reduces days, Supplies, Medicine or equipment use.

### Reduce Risk

Improves the action's outcome distribution.

### Improve Interpretation

Adds clues, confidence or additional knowledge.

### Improve Recovery

Increases the chance of preserving findings, records or injured members if the action fails.

A specialist contribution should be configured on the action or modifier. Concrete location code should not directly check for a named variant.

---

## 9. Risk and Outcome Resolution

Location actions, scout missions, and any other risky choice in the game resolve through **one shared pipeline** — not bespoke per-feature dice rolls. This section defines that pipeline end to end: how risk inputs become a score, how a score becomes a qualitative estimate the player can see, how a committed action resolves to an outcome tier, and how that tier becomes concrete effects. Cross-references below (e.g. "Section 4.2A") refer to this section's own numbering under the 9.x prefix (i.e. Section 9.4.2A) unless another document is named.

### 9.1 Core Design Decision

Every risky action in the game — a location action, a scout mission, a route crossing, a spontaneous event choice — resolves through **one shared pipeline**, not bespoke per-feature dice rolls.

> A location action, a scout order and a world event are different *sources* of risk. They are not different *resolution systems*.

The pipeline consumes a declarative **Risk Profile** and an **Outcome Table**, both authored as data, and produces:

1. a **displayed risk estimate** (shown to the player before commitment)
2. a **resolved outcome tier** (determined at commitment)
3. an **effect bundle** (applied through the generic effect registry, Section 9.8B)

Concrete features do not implement their own probability logic. They provide inputs and reference an Outcome Table.

---

### 9.2 Relationship to the Existing Risk Score Model

`world_locations_and_events_concept.md` (23A.1) already lists the inputs that can influence a Risk Score (terrain, faction territory, morale, specialists, supplies, previous decisions, and so on) and states that the player only ever sees a qualitative label (Low / Moderate / High / Extreme / Unknown).

This document keeps that list as the canonical **input catalogue** and adds the missing middle layer: how those inputs become a score, how that score becomes a band, how a band becomes an outcome, and how specialists and clues change any of these steps without the player ever seeing a raw number.

---

### 9.3 The Resolution Pipeline

Every risky action resolves through the following stages. This is the same pipeline for a location action (Section 6, steps 7-10), a scout mission, or a spontaneous event choice.

```text
1. Collect Risk Inputs
2. Compute Raw Risk Score        (hidden)
3. Compute Risk Band              (hidden, but displayable)
4. Compute Estimate Confidence    (hidden)
5. Present Risk Estimate to Player (qualitative, confidence-shaped)
6. Player commits to the action
7. Resolve Outcome Tier           (Outcome Table + Risk Band)
8. Apply Specialist Recovery Adjustment (only if tier is Failure or worse)
9. Resolve Effect Bundle          (generic effect registry)
10. Record Resolution             (history, journal, save)
```

Steps 1–5 happen when the player is *considering* the action. Steps 6–10 happen when the player *commits*. This split matters: it is what allows "Low / Moderate / High / Extreme / Unknown" to be shown honestly before the player decides, without ever leaking the resolved outcome.

---

### 9.4 Risk Inputs and the Raw Risk Score

#### 9.4.1 Input Categories

Risk Inputs are grouped into three categories so authors and systems know how each one is allowed to act:

**Base Risk** — fixed per action/archetype/variant combination, and may itself vary by the location's current Operational State (see Section 4.2A). Defined at authoring time. Example: `Disturb` on an `investigation-site` has a higher Base Risk than `Observe`.

**Situational Modifiers** — additive or multiplicative factors from world state: biome danger, faction territory, active location modifiers (`Unstable`, `Contaminated`, `Guarded`), morale, injuries, supplies, distance from base, prior consequence events, time spent at the location.

**Mitigating Factors** — factors that only ever reduce risk or improve the estimate: a present specialist with a `ReduceRisk` contribution, useful equipment, a previously discovered clue about this specific location, an accurate faction relationship.

This grouping exists so that a designer authoring a new action cannot accidentally create a runaway score — Base Risk sets the ceiling, Situational Modifiers move within a bounded range around it, and Mitigating Factors only ever pull the score down, never up.

#### 9.4.2 Raw Risk Score

The Raw Risk Score is computed as:

```text
RawRiskScore = clamp(
    BaseRisk
    + sum(SituationalModifiers)
    - sum(MitigatingFactors),
    MinScore,
    MaxScore
)
```

The exact numeric range (e.g. 0–100) is an implementation detail and not fixed by this document. What matters at the design level is that the formula is **additive and bounded**, not compounding multipliers that can produce unpredictable spikes.

The Raw Risk Score is never shown to the player. It exists only to be translated into a Risk Band.

#### 9.4.2A State-Gated Risk Inputs

Some actions are meaningfully more or less dangerous depending on the location's current state — not only its Operational State, but sometimes an instance-level flag set by an earlier action at this same location. Two examples:

- Attempting to cross a broken bridge is far more dangerous while it is `Blocked` than after a temporary passage has been built (`RiskyPassage`) — this is a Base Risk that depends on Operational State.
- `Sacred` should make `Investigate`/`Disturb` at a grave riskier by default, but *less* so if the expedition already performed `LeaveOffering` here — this is a Situational Modifier whose magnitude depends on something the player already did at this specific instance, not on a formal state channel at all.

Both cases are the same underlying need: **a Risk Input's value should be allowed to depend on the current state of the specific location instance**, whether that state is a declared state channel (Operational, Knowledge, Interaction Progress) or a simple instance-level flag written by an earlier action's effect (e.g. `hasBeenRespected: true`).

`RiskProfileDefinition` therefore supports optional **state-gated overrides on any Risk Input**, not only Base Risk:

```text
action-attempt-crossing:
  baseRiskByState:
    Blocked:       45
    RiskyPassage:  20
  # any state not listed falls through to a default baseRisk value

action-investigate (on investigation-site, variant marked-grave):
  baseRisk: 20
  situationalModifiers:
    - id: modifier-sacred
      value: 15
      reducedValueIfFlag:
        flag: "hasBeenRespected"
        value: 5
```

This is not a separate mechanism from Section 4.2 — it only changes where a given input's value comes from before the same additive formula runs. Situational Modifiers, Mitigating Factors and the resulting Band computation (Section 5) work exactly as already described once the gated value is resolved.

Authoring guidance: only gate an input by state when it represents a genuinely different physical or relational situation (a bridge that's half-repaired really is safer; a grave the expedition has already shown respect to really does carry less social risk). Don't use state-gating to fake a difficulty curve — that's what Situational Modifiers and specialist contributions are for on their own.

---

### 9.4B Social Risk: Mapping Faction Attitude Into the Shared Pipeline

Not every risky action is physically dangerous. `Trade`, `Communicate`, `RequestPassage` and similar `Contact Site` actions (and `FollowInstruction`/`CrossBoundary` on a `Territorial Marker`) are risky in a social sense: the expedition might be refused, or provoke a hostile reaction, based on how a faction currently feels about it — not based on terrain or structural danger.

**Locked decision:** these actions resolve through the exact same pipeline (Sections 3-9), not a separate "dialogue success" system. This keeps Section 1's core decision intact — different *sources* of risk, one resolution system.

#### 9.4B.1 Mapping Faction Attitude Into Risk Inputs

`factions_and_trade_concept.md`'s Trust, Anger and Fear values (Section 13P.3 there) map into the same three input categories already defined in Section 4.1:

```text
Anger  -> Situational Modifier (higher Anger raises Raw Risk Score)
Fear   -> Situational Modifier (higher Fear raises Raw Risk Score,
                                 and independently biases the Outcome
                                 Table toward avoidance/refusal rather
                                 than confrontation - see 4B.2)
Trust  -> Mitigating Factor (higher Trust lowers Raw Risk Score)
```

A simple, boundable mapping: `SituationalModifier = round(Anger / 4)`, `MitigatingFactor = round(Trust / 4)`, keeping individual contributions within the same 5-30 magnitude guideline as any other modifier (Section 5.1A), assuming Trust/Anger/Fear are themselves tracked on a comparable 0-100-ish internal scale. The exact divisor is a balancing constant, not a structural decision.

#### 9.4B.2 Reinterpreting Outcome Tiers for Social Stakes

The six generic tiers (Section 7) still apply, but their meaning shifts:

```text
Major Success      -> enthusiastic help, a favorable deal, unprompted extra information
Success             -> the request is granted
Success With Cost   -> granted, but at a cost (worse trade terms, a favor owed)
Partial Result       -> a partial or hedged answer, cautious cooperation
Failure               -> refused, no reaction beyond refusal
Severe Failure        -> hostile reaction; may trigger a Presence State
                         change (e.g. Contact Site -> `Hostile`) and a
                         Consequence Event, rather than a physical injury
```

High Fear specifically should weight `Failure` (avoidance, the faction withdraws or refuses contact) over `Severe Failure` (confrontation) relative to what high Anger alone would produce — a fearful faction is more likely to hide than fight, which the factions document already states as a design rule (Fear section: "a fearful faction may avoid contact... rather than attacking directly"). This is a weight-adjustment rule (Section 7.2), not a new mechanic.

#### 9.4B.3 What Stays a Hard Requirement

Whether an action is offered at all is still gated by Presence/Operational State as a Hard Requirement (Section 7.3), not by attitude values — `Trade` simply doesn't appear as an option while the location's state is `Closed` or `Hostile`. Risk resolution only decides how an *available* action goes; attitude values do not need to duplicate as availability gates on top of the state channel that already governs them.

---

### 9.5 Risk Bands

#### 9.5.1 Band Thresholds Are Data, Not Constants

With the Raw Risk Score fixed to a **0–100 scale** (see Section 13), each archetype/action defines its own thresholds for converting that score into a band:

```text
Low        [0   .. LowMax]
Moderate   [LowMax+1 .. ModMax]
High       [ModMax+1 .. HighMax]
Extreme    [HighMax+1 .. 100]
```

This keeps "High risk" meaningful for both a gentle Trace Site and a genuinely dangerous Containment Site, instead of forcing one global scale to describe wildly different stakes.

**Default thresholds**, used unless an action explicitly overrides them:

```text
Low        0  .. 25
Moderate   26 .. 50
High       51 .. 75
Extreme    76 .. 100
```

Authors should only override these when an archetype's stakes genuinely compress or stretch the meaningful range — for example, a `Containment Site` might set `Extreme` to start at 60 rather than 76, since even a "moderate-looking" score there represents real consequences.

#### 9.5.1A Tuning Guidelines and Worked Examples

The 25/50/75 defaults only produce sensible bands if Base Risk and modifier magnitudes are authored within a consistent range. Two guidelines keep that true:

**Base Risk guideline, by stakes rather than by archetype name:**

```text
Trivial (Observe-type actions, any archetype)    0  .. 10
Low-stakes default actions                       10 .. 20
Moderate-stakes default actions                  20 .. 40
High-stakes default actions                      40 .. 60
```

No action should set a Base Risk above roughly 65 on its own. Higher scores should only be reachable by stacking Situational Modifiers on top of an already-risky Base Risk — this makes Fairness Rule 1 (Section 11) true by construction: a location isn't Extreme just by existing, it becomes Extreme because of what's actively wrong with it (Contaminated, Unstable, deep in hostile territory, and so on).

**Modifier magnitude guideline:**

```text
Minor factor    (e.g. Watched, minor terrain penalty)      5  .. 10
Moderate factor (e.g. Sacred, FactionOwned, Flooded)       10 .. 20
Severe factor   (e.g. ActivePressure, forbidden territory) 20 .. 30
```

No single modifier should be able to move a location more than one band by itself. Band changes should come from *combinations* of factors, which also keeps the qualitative risk estimate (Section 6) legible — a player who learns "Sacred plus FactionOwned plus Contaminated together made this High" is learning to read the world, not memorizing a magic number.

**Worked example — Disturb on a Sacred, Faction-Owned, Contaminated marked grave, deep in Border Warden territory, no specialist present:**

```text
Base Risk (Disturb, high-stakes default)       45
+ Sacred (moderate)                            +15
+ FactionOwned (moderate)                      +15
+ Contaminated (moderate)                      +15
+ deep faction territory (severe)              +25
= Raw Risk Score                               115 -> clamped to 100
Band: Extreme
```

With a Scholar present (Reduce Risk contribution, roughly -15 to -20):

```text
Raw Risk Score: 100 - 18 = 82
Band: still Extreme, but closer to the boundary
```

This matches intent: disturbing a sacred, contaminated, faction-guarded grave deep in hostile territory should be Extreme regardless of preparation — the specialist changes the *outcome distribution within* Extreme (Section 7.2's weight shifting), not the fact that it's genuinely dangerous.

**Worked example — Observe on a cold campfire:**

```text
Base Risk (Observe, trivial default)     5
+ mild biome danger (minor)              +5
= Raw Risk Score                         10
Band: Low
```

These two examples anchor the scale: a routine, harmless action should sit comfortably under 25, and a maximally dangerous authored situation should comfortably clear 75 — with most real decisions landing in the Moderate-to-High range in between, where specialists and clues meaningfully matter.

#### 9.5.2 Band Is What Gets Shown

The player only ever sees the band (`Low / Moderate / High / Extreme / Unknown`), never the score. This matches Section 23A.1 of the world document — this section simply defines the mechanism that produces that label.

---

### 9.6 Estimate Confidence: Why the Same Danger Can Look Different Twice

A location's actual danger does not change depending on who is looking at it. What changes is how well the expedition can *read* it. This is the mechanic that lets "a suitable specialist or clue may improve the estimate without changing the actual risk" (location doc, 9.2) actually work.

#### 9.6.1 Confidence Levels

```text
Unknown       — no reliable read at all; shown as "Unknown risk"
Guess         — a rough read; shown band may be a range ("Moderate to High")
Assessed      — a solid read; shown band is a single label
Confirmed     — near-certain read; shown band is a single label with high trust
```

#### 9.6.2 Confidence Inputs

- a present specialist whose role improves interpretation (e.g. Scout, Scholar)
- a previously discovered clue that specifically describes this location or hazard type
- prior confirmed experience with the same variant or modifier combination
- archived knowledge from a previous expedition about this exact location

#### 9.6.3 Confidence Does Not Change the Real Risk

Confidence only changes what stage 5 (Present Risk Estimate) shows. Stage 2–3 (Raw Risk Score, Risk Band) are computed the same way regardless of confidence. A player with `Guess` confidence facing an actually `Low` risk location might see "Moderate to High" — this is the game being honestly uncertain, not the game punishing them for lacking a specialist.

This separation is what prevents two failure modes at once: the game never lies about danger existing, and it never requires a specific specialist just to see the truth — only to see it *clearly*.

---

### 9.7 Outcome Tables

#### 9.7.1 Structure

An Outcome Table is authored per action (or shared across a family of actions) and defines the relative weight of each Outcome Tier, per Risk Band:

```text
OutcomeTable: "outcome-investigate-grave"

Risk Band: Low
  Major Success        20
  Success              55
  Success With Cost     15
  Partial Result       10
  Failure               0
  Severe Failure         0

Risk Band: High
  Major Success         5
  Success               20
  Success With Cost     25
  Partial Result        20
  Failure               20
  Severe Failure        10
```

Not every action needs all six tiers (location doc, 9.3). An `Observe` action might only ever resolve to `Success` or `Partial Result` — it has no meaningful way to fail severely.

#### 9.7.1A Table Scope: Archetype-Level, Content-Flavored

Outcome Tables are authored at the **archetype + action** level (e.g. one table for `action-investigate` on `investigation-site`), not per variant. A marked grave and a ruined shrine share the same tier weights and the same set of Effect Bundle *slots* — what differs is which concrete clue, finding or journal text fills each slot, and that comes from the location's Content Profile (Section 3.1).

This keeps archetype-specific tables practical: choosing them does not mean every one of ten `investigation-site` variants needs its own authored table — it means `investigation-site` needs one table per action, and ten Content Profiles supply the flavor.

#### 9.7.2 Weight Adjustments

Mitigating Factors and specialist contributions may shift weight between tiers rather than only affecting the Raw Risk Score. This is the mechanism behind the `ReduceRisk` and `ImproveRecovery` specialist contributions (location doc, Section 8):

- **Reduce Risk** shifts weight from `Severe Failure`/`Failure` toward `Success With Cost`/`Partial Result`. It does not create outcomes that could not otherwise happen; it makes the bad ones less likely and the survivable ones more likely.
- **Improve Recovery** does not touch the initial roll. It triggers a second, smaller roll *only when the initial result lands on Failure or Severe Failure*, to determine whether findings, records or injured members are preserved anyway (see Section 9).

#### 9.7.3 Selection

Once the Risk Band and any weight adjustments are applied, the resolver picks a single Outcome Tier using a weighted random selection, seeded per action instance (see Section 10 on determinism).

---

### 9.8 From Outcome Tier to Effect Bundle

Each Outcome Tier for a given action maps to an **Effect Bundle** — a list of effects drawn from the generic effect registry (Section 9.8B: add clue, injure member, change faction Trust, schedule consequence, and so on).

```text
Action: "action-investigate" on investigation-site / marked-grave
Outcome Tier: Success With Cost
Effect Bundle:
  - add-finding: "Bruchstück einer Karte"
  - add-clue: "warden-grave-symbol"
  - change-morale: -1
  - add-faction-memory: border-wardens, "grave disturbed respectfully"
```

Effect Bundles are authored alongside the Outcome Table, not computed procedurally. This keeps every possible outcome inspectable and testable ahead of time, which matters for the Authoring Validation rules (Section 19): "outcome tables with no valid outcome" is only checkable if every tier's Effect Bundle actually exists.

#### 9.8B Generic Effects Library

Outcomes apply reusable effects, handled through a registry so new reusable effect kinds can be added later. Initial effect library:

- add unsecured knowledge
- add archive candidate
- add clue
- add finding
- add map report
- confirm or contradict information
- add or remove item
- consume Supplies
- consume Medicine
- change Morale
- reserve or release Capacity
- injure member
- kill member
- capture member
- separate member
- change faction Trust
- change faction Anger
- change faction Fear
- add faction memory
- change location state
- add or remove modifier
- change edge traversal
- move expedition across edge
- reveal route
- create location
- create dynamic situation
- create hazard
- start project
- schedule world event
- create recovery objective
- add journal entry
- add expedition-member memory
- unlock dialogue topic
- unlock analysis item
- escalate-flag (Section 9.10A.2, for scout leg and ambient area ticks)

---

### 9.8A Scheduled Consequences Resolve Once, Apply Later

Some effects in the registry — `schedule world event`, `create hazard` — don't apply immediately. A released containment threat might not reach a nearby village until several expeditions later (world doc, Section 23A.4, Consequence Events).

**Locked clarification:** the *severity and content* of a scheduled consequence are resolved once, at the moment the triggering action commits (Stage 9 of the pipeline, same as any other effect). The delay only controls *when* the already-determined effect bundle is applied to the world, not *whether it gets rolled again* when that time comes.

```text
At commit time (Stage 9):
  1. The triggering outcome tier is already known (e.g. Severe Failure on ForceOpen).
  2. The consequence's specific content is resolved now: which hazard,
     how severe, which regions/factions/routes it will eventually affect.
  3. This resolved content is stored as a fixed, ready-to-apply Effect Bundle
     with a trigger condition (a world-time delay, an expedition count,
     or a later event).

When the trigger condition is later met:
  4. The stored Effect Bundle is applied exactly as resolved. No second
     roll happens. Nothing about it depends on the player's state or
     specialists at the later moment it fires.
```

This keeps the system consistent with this document's own versioning rule (Section 20): "generated outcomes should be stored once resolved so later balance changes do not retroactively reroll world history." Without this explicit rule, "schedule consequence" could be misread as deferring the *roll* rather than only the *application* — which would let content patched long after the triggering action retroactively change what a past decision caused. This section closes that ambiguity.

---

### 9.9 Recovery Resolution (Failure Does Not Mean Erasure)

When the resolved Outcome Tier is `Failure` or `Severe Failure`, a second, narrower resolution step runs before effects are finalized:

```text
1. Was a finding, record or member at stake in this action?
2. If yes, roll a Recovery Check using ImproveRecovery contributions and any relevant modifiers.
3. Recovery Check outcomes:
   - Preserved       (finding/member kept despite the failure)
   - Partially Preserved (degraded finding, or member injured instead of lost)
   - Lost            (the base Failure/Severe Failure consequence applies in full)
```

This is the formal mechanism behind design pillar 9 ("Failure should create history") and the existing rule that a dangerous action may still produce knowledge even if it also causes harm (world doc, 23A.2). Failure is a tier of the main roll; *loss* is a separate, softer roll layered on top of it.

**Known dependency, not yet closed:** when `Lost` creates a recovery objective referencing a specific member, that objective eventually needs to surface as identity-linked content at some future location (a name in a journal, a specific body found downstream) rather than generic flavor text. This document only defines that the objective is created — carrying the actual identity forward into a future Content Profile uses the dynamic reference-slot mechanism now defined in Section 13.1.

---

### 9.10 Save Points, Crash Safety and Seeding

**Locked decision:** the game only saves at the Base Camp, between expeditions. There is no manual save-and-reload during an active expedition, and no in-game "load checkpoint" screen exists at any point.

#### 9.10.1 Why the Save Policy Already Solves Most of the Reroll Problem

A single risky action can no longer be reloaded and retried in isolation — the smallest unit a player could even attempt to "redo" is the entire expedition since the last camp visit, which costs enough time and decisions to be self-limiting rather than an exploit. Seeded RNG (10.3) is therefore not the primary defense against reroll-fishing; the save policy already provides that.

#### 9.10.2 Crash-Safe Suspend Point

**Locked decision:** the game maintains exactly one hidden suspend snapshot, silently overwritten after every fully-resolved action or completed day. This snapshot is not a save file the player can browse, name, or choose to load — it is invisible plumbing that exists purely so that closing the app, an OS interruption, or a crash never destroys progress unfairly.

This resolves "not unfair, but not self-loadable" through one property: **there is only ever one suspend state, and it is always the most recent one.**

```text
Rule: the suspend snapshot is written only after a resolution
      completes fully (Stage 9 of the pipeline in Section 3, or
      the end of a scout leg in Section 10A.2), never mid-resolution.
```

Consequences of this rule:

- If the app closes *before* the player commits an action (Stage 6), nothing has changed — relaunching returns them to the same still-open decision. Nothing was lost, and nothing was gained.
- If the app closes *during* resolution (Stages 6-9, which should take a fraction of a second and involve no player input), the write to the suspend point is atomic: either the whole resolution is recorded, or none of it is. There is no observable state where a player could see a bad roll, quit fast enough, and relaunch to a version where it didn't happen — atomicity, not detection, is what prevents this.
- If the app closes normally, by crash, or by force-quit at any other point, relaunching simply resumes at the latest suspend point. There is no "continue from an earlier point" option, because no earlier point is ever kept.

Because deliberately quitting mid-expedition produces exactly the same resume state as accidentally crashing — the latest fully-resolved moment — there is no incentive to "quit and hope," and no fairness cost to genuine crashes. The two cases don't need to be told apart, because the system treats them identically by construction.

#### 9.10.3 Seeding Recommendation

Each resolved action still stores:

- the action ID and location/scout/event instance ID
- the Raw Risk Score and Risk Band at resolution time (for later inspection/debugging, never shown to the player)
- the random seed used for the Outcome Tier roll and any Recovery Check
- the resolved tier and the applied Effect Bundle

With reroll-by-reload already closed off by 10.1-10.2, seeding earns its place for two remaining reasons:

1. **Reproducibility for debugging and balancing.** A reported "this outcome felt unfair" bug report can be replayed exactly from its stored seed and inputs.
2. **Fairness auditing.** Designers can re-run an Outcome Table against many seeds to confirm the actual tier distribution matches the intended feel before shipping it, independent of the qualitative band shown to the player.

Derive each roll's seed from the persistent world seed plus a strictly incrementing action-resolution counter, not wall-clock time, so replays stay reproducible regardless of real-world timing.


---

### 9.10A Scout Mission Resolution: A Distinct Pipeline Variant

**Locked decision:** scout missions do not resolve through the single-commit pipeline in Section 3. A scout mission spans multiple world-phase days with no player decision point in between, and its outcome space (`world_locations_and_events_concept.md`, 23A.3: On Mission, Expected Back, Overdue, Returned, Returned Injured, Returned Disturbed, Missing, Captured, Dead) is a state machine, not a single tier result.

#### 9.10A.1 Dispatch: Shared Stages, Adapted

Stages 1–5 of the shared pipeline still apply at the moment the player issues a scout order: Risk Inputs (direction, biome, faction territory, scout star level, expedition composition) produce a Raw Risk Score, a Risk Band, and an Estimate Confidence, shown before the player commits the scout. This is the same "honest but possibly blurry" estimate as any location action.

#### 9.10A.2 Per-Leg Resolution Reuses the Shared Outcome Table Structure

Once dispatched, the mission resolves as a sequence of **daily or per-leg risk ticks**, rather than one roll at the end. Each tick reuses the exact same Outcome Table machinery from Section 7 — a leg is treated as its own resolvable unit, keyed by the region's danger profile (biome, faction territory, mission behavior) instead of a location ID:

```text
For each day/leg of the mission:
  1. Recompute a leg-level Risk Score (base mission risk + that day's situational modifiers)
  2. Resolve against that region's Scout Leg Outcome Table (Section 7 structure, reused)
  3. The resulting Effect Bundle may include ordinary effects (add clue, add map knowledge)
     and/or one new effect kind: escalate-flag(<flag>, <severity>)
```

**Escalation Flags (the MVP set):**

```text
Spotted     - the scout was noticed by a faction or hostile presence
Injured     - the scout took physical harm
Lost        - the scout drifted from the intended route or timing
Exhausted   - supplies or stamina ran low over a long mission
Trailed     - something followed the scout back toward camp
```

Each flag has two severity levels, `Minor` and `Major`. A flag starts unset; a leg tick can set it to `Minor`, or escalate an existing `Minor` to `Major`. A flag cannot skip directly from unset to `Major` in one tick except through an explicitly authored high-severity event (an ambush, a direct confrontation) — ordinary bad luck accumulates, it doesn't spike, mirroring the same principle already stated for the main pipeline.

This reuse matters for two reasons: authors don't have to learn a second resolution system, and the same Estimate Confidence and specialist-contribution mechanisms (Sections 6 and 8) apply unchanged — a scout with the right star level reduces leg risk exactly the way a specialist reduces a location action's risk.

#### 9.10A.3 Final State Resolution

On the scheduled return day, a **priority-ordered decision list** reads the accumulated flags and their severities and resolves to one of the Scout States (world doc 23A.3). Priority order, evaluated top to bottom, first match wins:

```text
1. Major Injured + Major Spotted, in hostile/forbidden territory  -> Captured or Dead (Recovery Check applies, 10A.5)
2. Major Injured (alone)                                          -> Returned Injured
3. Major Spotted or Major Trailed (alone)                         -> Returned Disturbed
4. Any Minor flag, no Major flags                                 -> Returned (with a minor journal note)
5. No flags at all                                                -> Returned
6. Major Lost + Major Exhausted, no evidence of contact           -> Overdue (see 10A.4)
```

This list is authored per faction-territory / biome combination, not globally — Hidden Ones territory should weight rows 1 and 3 more heavily than Coastal People territory, without changing the list's structure.

**Fairness constraint:** row 1 (the only path to Captured/Dead) requires at least one Major flag already present from an earlier leg. A mission with an all-clear flag state can never resolve to Captured or Dead on the final day alone — severity must accumulate over the mission, never spike from a single unlucky final tick.

#### 9.10A.4 Overdue Handling

If the final resolution or an escalation flag produces `Overdue`, this is deliberately *not* an immediate answer (world doc, 23A.3: "an overdue scout should create a question on the map, not an instant answer"). The resolved state exists in World Truth immediately (the Definition/Instance/Player-Knowledge split from Section 3.2 applies here too), but Player Knowledge only updates to `Returned` / `Captured` / `Dead` once the expedition gains evidence for it — the scout arriving late, a later expedition finding traces, or a faction interaction.

#### 9.10A.5 Recovery Check Still Applies

If a leg or the final resolution would result in `Dead` or `Captured`, the Recovery Check from Section 9 still runs before that result is locked in — a scout's fate is not exempt from the same fairness mechanism a location action gets.

---

### 9.10B Ambient Risk: Generalizing the Per-Tick Pattern Beyond Scouts

Section 10A.2's per-leg tick pattern — resolve a small risk check per day, accumulate flags rather than spike to a severe result — isn't actually specific to scouts. A `Dynamic Situation` or `Hazard Zone` (a spreading swamp danger, a contaminated area) presents the same shape of problem for the **main expedition**: danger that accrues from simply being present in or traveling through an area over time, not from a single discrete action the player chooses.

**Locked decision:** the per-tick pattern is a general mechanism, not a scout-only feature. Section 10A is one application of it (a scout traveling through hostile territory); the main expedition's presence in a Hazard Zone is a second application of the same underlying pattern, reusing the identical structure:

```text
For each day the expedition is present in or travels through a
Hazard Zone / Dynamic Situation area:
  1. Recompute a day-level Risk Score for that area (Base Risk of the
     hazard + situational modifiers: exposure duration, protective
     equipment, biome)
  2. Resolve against that hazard's Outcome Table (Section 7 structure,
     reused again)
  3. The resulting Effect Bundle may set or escalate an
     exposure-style flag (e.g. `Contaminated: Minor`), rather than
     applying a full location-action-sized consequence on day one
  4. Accumulated flags feed a final or ongoing severity resolution,
     the same way accumulated scout flags feed 10A.3's decision list
```

This means a coding agent only has to implement the per-tick resolver once. Scout missions, Hazard Zones, and any future "ongoing danger without a single trigger action" archetype all configure the same mechanism with different Outcome Tables and flag vocabularies, rather than each getting a bespoke timer system.

---

### 9.11 Fairness Rules

These extend the fairness rule already stated in Section 11.4 (Containment Site): *"Major irreversible consequences should have discoverable warning evidence."*

1. No Outcome Table may assign `Severe Failure` a nonzero weight at a `Low` Risk Band unless the action is explicitly flagged `IgnoresLowRiskFloor` (reserved for a small number of authored "this was never actually safe" moments, used deliberately, not by accident).
2. Every action that can end an expedition member's story (death, permanent capture) must have a Recovery Check available, even if that check has a low success chance. There is no action in the MVP that skips Section 9 entirely.
3. Estimate Confidence may be low, but the displayed band must never be a strictly *lower* danger than the true band. If the game is uncertain, it may show a wider range or "Unknown" — it may never show "Low" when the truth is "Extreme."
4. An `Unknown` displayed risk is itself information: it should be rare enough that the player learns to be cautious around it, not a routine default that becomes meaningless noise.

---

### 9.12 Data Structures

Extending the definition types already listed in Section 3.1:

```text
RiskProfileDefinition
├── baseRisk
├── baseRiskByState (optional, overrides baseRisk per Operational State — Section 4.2A)
├── situationalModifierRefs
├── mitigatingFactorRefs
├── bandThresholds
└── ignoresLowRiskFloor (bool)

OutcomeTableDefinition
├── id
├── perBandWeights (Low / Moderate / High / Extreme)
├── weightAdjustmentRules (from specialist contributions / modifiers)
└── tierEffectBundles (per Outcome Tier)

RecoveryProfileDefinition
├── appliesToTiers (Failure, Severe Failure)
├── baseRecoveryChance
├── improveRecoveryContributionRefs
└── recoveryOutcomeEffectBundles (Preserved / Partially Preserved / Lost)
```

These integrate directly with the `RequirementDefinition`, `EffectDefinition` and `LocationActionDefinition` types already defined in Section 3.1 — a `RiskProfileDefinition` and `OutcomeTableDefinition` are referenced by ID from an action definition, the same way `ContentProfileId` is referenced today.

---

### 9.13 Decision Log

#### Locked

1. **Save policy.** The game saves only at Base Camp; there is no in-expedition save/load screen.
2. **Crash-safe suspend point.** One hidden, always-latest suspend snapshot, written atomically only after a resolution fully completes. It is never player-loadable and never allows returning to an earlier moment (Section 10.2).
3. **Numeric scale.** The Raw Risk Score uses a fixed **0-100** range, with Base Risk and modifier magnitude guidelines (Section 5.1A) chosen so the 25/50/75 default bands hold up under worked examples.
4. **Outcome Table scope.** Tables are authored per **archetype + action**, not per variant; Content Profiles supply variant flavor (Section 7.1A).
5. **Scout missions.** Resolved through the per-leg tick + priority-ordered final resolution variant (Section 10A), reusing the same Outcome Table structure as location actions rather than inventing a second system.
6. **Seeding.** Retained for reproducibility and balancing, not as the primary anti-scum mechanism (Section 10.3).
7. **State-gated Risk Inputs.** `RiskProfileDefinition` supports state-gated overrides on any Risk Input, not only Base Risk — including Situational Modifiers whose magnitude depends on an instance-level flag set by an earlier action (Section 4.2A). Found while authoring the Broken Bridge example (Base Risk by state) and generalized while simulating the Marked Grave (Situational Modifier by instance flag).
8. **Scheduled consequences resolve once.** A delayed effect's severity and content are fixed at the moment the triggering action commits; the delay only controls when the already-resolved effect bundle is applied, never whether it's rolled again (Section 8A). Found while simulating a Containment Site.
9. **Social risk uses the same pipeline.** Faction Trust/Anger/Fear map into Situational Modifiers and Mitigating Factors exactly like any other risk input, so Contact Site and Territorial Marker actions resolve through Sections 3-9 rather than a parallel dialogue-success system (Section 4B). Found while simulating a Contact Site.
10. **Ambient risk generalizes beyond scouts.** The per-tick pattern from Section 10A.2 is a reusable mechanism for any "danger accrues over presence in an area" archetype, not scout-specific; Hazard Zones and Dynamic Situations reuse it directly (Section 10B). Found while simulating a Hazard Zone.
11. **Passive time-driven transitions are out of scope here.** This document only covers action-resolution-driven change; renewal, expiry and decay are a separate Base Phase mechanism belonging to `world_locations_and_events_concept.md` (Section 16). Found while simulating Resource Site and Dynamic Situation.

#### Found During Simulation, Owned by Other Documents (not fixed here)

1. **Dynamic content reference slots for identity-linked findings** (e.g. a specific lost member's remains) — now defined in Section 13.1.
2. **Declared interaction range per action** (Landmark Sites need actions usable from a distance, not just adjacency) — belongs in that same document's Anchor model, Section 4. See the same write-up, Section 3.
3. **Moving anchors vs. the MVP Dynamic Situation requirement** — not a new gap (Path Anchors are already acknowledged as deferred), but a scope conflict worth an explicit MVP decision: restrict MVP Dynamic Situation content to stationary variants. See the same write-up, Section 4.

#### Remaining Tuning Tasks (numbers to playtest, not open design questions)

1. Whether 25/50/75 hold up once real content is authored across all ten archetypes, or need per-archetype adjustment (Section 5.1).
2. The exact Minor-to-Major escalation probabilities per biome/faction-territory combination for scout legs (Section 10A.2) — a balancing pass, not a structural decision.
3. Whether five escalation flags (Spotted, Injured, Lost, Exhausted, Trailed) cover enough narrative variety once actual scout mission content is authored, or whether a sixth is needed for a specific faction or biome.

---

### 9.14 MVP Implementation Boundary

#### Required for MVP

- Raw Risk Score computation (Section 4)
- Risk Band thresholds per action (Section 5)
- Crash-safe suspend point (Section 10.2) — this is a fairness-critical baseline, not a nice-to-have, even for the Vertical Slice
- Estimate Confidence with at least `Guess` and `Assessed` (Section 6) - `Unknown` and `Confirmed` can follow shortly after
- Outcome Tables with at minimum `Success`, `Success With Cost`, `Failure` for MVP actions (full six-tier range not required everywhere)
- Recovery Check for any action that can injure, capture or kill a named member (Section 9)
- Fairness Rule 1 and 2 from Section 11 (no unwarned severe failure at Low risk; recovery always available for member-ending outcomes)
- Scout dispatch estimate (Section 10A.1) using the same shared Stages 1-5 as location actions

#### Deferred

- Full six-tier Outcome Tables for every action
- `Confirmed` confidence tier and its authoring requirements
- Full per-leg Scout Mission resolution (Section 10A.2-10A.4) - the Vertical Slice may use a simplified single end-of-mission roll against the same input catalogue, with the per-leg tick system following once the core loop is validated
- Deterministic seeding infrastructure (Section 10.3) - acceptable to use ordinary RNG for the Vertical Slice, since the save policy and suspend point already limit reroll-scumming without it

---

### 9.15 Acceptance Criteria

1. A designer can author a new action's danger by filling in a `RiskProfileDefinition` and referencing an `OutcomeTableDefinition`, without writing code.
2. The same pipeline resolves a location action, and — via the 10A variant — a scout mission outcome.
3. Two players facing the identical true risk but different specialists/clues can see different displayed risk bands, while the actual resolution odds remain identical.
4. No player-visible path exists for the displayed risk to understate true danger (Fairness Rule 3).
5. A failed `Disturb` action on a `marked-grave` can still preserve a finding through the Recovery Check, producing a "damaged but present" result rather than simple loss.
6. Authoring validation (location doc, Section 19) can detect an Outcome Table with a tier that has no Effect Bundle.
7. Force-quitting the app at any point during an expedition and relaunching resumes at the exact latest resolved state, with no option to reach an earlier one.
8. A scout mission with no escalation flags set cannot resolve to Captured or Dead on its final day.

---

### 9.16 Scope Boundary: Passive Time-Driven Transitions

**Locked clarification:** this entire document governs transitions and effects that result from *resolving an action* (Sections 3-10B) — a player commits, a scout leg ticks, a project day passes. It does not govern location state changes that happen purely from elapsed world time, with no action involved at all.

Examples surfaced while simulating archetypes beyond the risk-heavy ones:

```text
Resource Site:      Renewing -> Available, after enough Base Phase time passes
Dynamic Situation:   TimeLimited -> Expired, after its deadline elapses
Location (general):  RecentlyUsed / WeatherDamaged fading over time
```

None of these involve a Raw Risk Score, an Outcome Table, or a player decision — they're bookkeeping that should run once per Base Phase tick across all relevant location instances. That mechanism belongs to `world_locations_and_events_concept.md`'s Base Phase system, not to this document. This section exists only to state the boundary explicitly, so a coding agent doesn't try to route passive decay/renewal through the risk pipeline just because it's the only "location changes over time" mechanism documented so far.

---

---

## 10. Multi-Day Projects

Some location actions create persistent projects rather than resolving immediately.

Examples:

- rebuild bridge
- clear tunnel
- excavate entrance
- reinforce seal
- construct raft
- stabilize ruin
- contain hazard

A project instance stores:

- source location
- project definition
- required progress
- current progress
- assigned specialist contribution
- required equipment
- daily resource costs
- exposure risk
- interruption rules
- completion effects
- cancellation effects
- persistence across expeditions

Projects may be:

- completed continuously
- paused and resumed
- abandoned
- damaged by world events
- completed by a later expedition

The expedition remains together while performing a project.

### Broken Bridge Example

The bridge is an edge-anchored `RouteObstacle`.

The `Rebuild Bridge` action creates a project.

```text
Duration: 3 workdays
Hard Requirement: bridge material or valid construction equipment
Soft Requirement: Engineer
Daily Cost: normal expedition Supply consumption
Risk: exposure, accident, faction observation
Completion:
- operational state -> Repaired
- edge traversal -> Open
- route knowledge candidate created
- persistent world history entry created
```

Without an Engineer, the same project may be unavailable or significantly slower and riskier, depending on authored rules.

---

## 11. Initial Ten Archetypes

The ten archetypes below are the initial reusable gameplay library.

They are registry entries, not a closed code enum.

---

## 11.1 Trace Site

**ID:** `trace-site`

### Dominant Question

> What happened here, and what did it leave behind?

### Typical Anchors

- point
- small area

### Example Variants

- cold campfire
- abandoned expedition camp
- improvised shelter
- discarded backpack
- wrecked cart
- abandoned observation post
- blood trail
- ruined boat landing
- former battle site
- left-behind map case

### Default Actions

- Observe
- Inspect
- Search
- Investigate
- Document
- Collect
- CompareWithArchive
- Mark
- Leave

### Typical Outputs

- clues about past movement
- old or uncertain map information
- journals and records
- Supplies or Medicine
- physical findings
- missing-person evidence
- faction traces
- false or outdated interpretations
- recovery objectives

### Typical Progress

```text
Untouched -> Observed -> Searched -> Exhausted
```

### Common Modifiers

- RecentlyUsed
- WeatherDamaged
- FactionOwned
- Watched
- Searchable
- Contaminated
- ContainsRemains
- Campable

### Variant Difference Example

A cold campfire and an old expedition camp use the same action pipeline.

The cold campfire may provide:

- footprints
- food remains
- one low-value clue

The expedition camp may provide:

- damaged journal
- old map
- missing-member evidence
- recoverable supplies
- several linked clues

No new code is required.

---

## 11.2 Investigation Site

**ID:** `investigation-site`

### Dominant Question

> What is this place, what does it mean, and should we disturb it?

### Typical Anchors

- point
- area
- composite location parent

### Example Variants

- marked grave
- underground vault
- ruined shrine
- collapsed ruin
- cave chamber
- stone circle
- old laboratory
- burial mound
- abandoned temple
- sealed storage chamber

### Default Actions

- Observe
- Inspect
- Investigate
- Document
- Interpret
- Search
- CollectSample
- Disturb
- Open
- ReturnWithSpecialist
- Mark
- Leave

### Typical Outputs

- historical or cultural clues
- findings
- symbols
- hidden access
- faction consequences
- structural danger
- new questions
- partial understanding
- major discovery

### Typical Progress

```text
Untouched -> Observed -> Inspected -> Investigated
                                      -> Disturbed
                                      -> Exhausted
```

### Common Modifiers

- Sacred
- FactionOwned
- Sealed
- Underground
- Unstable
- Trapped
- Contaminated
- Ancient
- Searchable
- ContainsHeavyObject

### Important Rule

A sealed underground vault may still be an `InvestigationSite`.

The `Sealed` modifier adds opening actions and state requirements.

A location uses the separate `ContainmentSite` archetype only when maintaining, weakening or breaking containment is the dominant interaction loop.

---

## 11.3 Route Obstacle

**ID:** `route-obstacle`

### Dominant Question

> Can the expedition pass, bypass or permanently change this connection?

### Typical Anchors

- edge
- point
- narrow area

### Example Variants

- broken bridge
- deep ravine
- collapsed tunnel
- flooded ford
- landslide
- blocked mountain pass
- fallen trees
- damaged road
- collapsed gate
- washed-out causeway

### Default Actions

- Observe
- AssessCrossing
- FindBypass
- SendScoutAround
- Cross
- ConstructTemporaryPassage
- Repair
- Clear
- Mark
- TurnBack

### Typical Outputs

- blocked route
- risky one-time passage
- temporary passage
- permanent route
- injury or loss
- time and Supply cost
- new route knowledge
- faction observation
- multi-day project

### Operational States

```text
Blocked
RiskyPassage
TemporarilyOpen
Open
Repaired
Destroyed
```

### Common Modifiers

- Repairable
- Unstable
- Flooded
- Guarded
- FactionControlled
- HiddenByTerrain
- Seasonal
- RequiresHeavyTools

---

## 11.4 Containment Site

**ID:** `containment-site`

### Dominant Question

> Why is this sealed, and what happens if the containment changes?

### Typical Anchors

- point
- edge
- area boundary

### Example Variants

- sealed gate
- quarantine entrance
- reinforced cave mouth
- containment chamber
- barred valley entrance
- sealed machine room
- enclosed forest threshold
- ritual prison entrance
- locked underground city gate

### Default Actions

- Observe
- InspectSeal
- Document
- Interpret
- SearchPerimeter
- AskFaction
- Weaken
- Open
- ForceOpen
- Reinforce
- Reseal
- Mark
- Leave

### Typical Outputs

- new access
- major knowledge
- faction panic or anger
- released hazard
- changed world region
- delayed consequence
- containment understanding
- new mandate
- permanent world event

### Operational States

```text
Intact
Examined
Weakened
PartiallyOpened
Opened
Breached
Reinforced
Resealed
```

### Common Modifiers

- Ancient
- FactionGuarded
- Sacred
- Contaminated
- ActivePressure
- Unstable
- Mechanical
- Ritual
- UnknownPurpose

### Fairness Rule

Major irreversible consequences should have discoverable warning evidence.

The player may still misunderstand it, but the warning cannot exist only in hidden data.

---

## 11.5 Territorial Marker

**ID:** `territorial-marker`

### Dominant Question

> Who placed this, what behavior does it demand, and what happens if we ignore it?

### Typical Anchors

- point
- edge
- area boundary
- path

### Example Variants

- carved border posts
- black stones
- cloth ribbons
- warning arrows
- skull markers
- ritual offerings
- marked trees
- blocked path arrangement
- graves lining a road
- painted rock symbols

### Default Actions

- Observe
- Inspect
- Document
- Interpret
- FollowInstruction
- LeaveOffering
- CrossBoundary
- Remove
- CopySymbol
- SendScoutAhead
- Mark
- Leave

### Typical Outputs

- suspected faction border
- territorial warning level
- cultural knowledge
- map marker
- Trust, Anger or Fear changes
- faction memory
- safer route behavior
- misinterpretation
- hidden observation event

### Progress States

```text
Unobserved
Observed
Interpreted
Respected
Violated
Removed
Renewed
```

### Common Modifiers

- FactionOwned
- Sacred
- Fresh
- Old
- Misleading
- Watched
- Threatening
- Invitation
- RepeatedPattern

### Reward Rule

Territorial markers primarily provide information and behavioral guidance, not loot.

---

## 11.6 Contact Site

**ID:** `contact-site`

### Dominant Question

> How does the expedition approach, communicate and establish practical relations here?

### Typical Anchors

- point
- settlement area
- entrance point

### Example Variants

- village
- border post
- trading camp
- ferry station
- watch station
- hermit's dwelling
- nomad camp
- fortified gate
- council place
- temporary faction camp

### Default Actions

- Observe
- Wait
- ApproachCautiously
- ApproachOpenly
- Communicate
- OfferGift
- Trade
- AskInformation
- RequestPassage
- OfferHelp
- RequestGuide
- Withdraw

### Typical Outputs

- faction contact progression
- trade
- rumors and information
- permission
- guide
- warning
- assistance request
- rejection
- capture or conflict
- faction memory

### Operational or Presence States

```text
Unknown
Occupied
OpenToContact
Restricted
Closed
Hostile
Abandoned
```

### Common Modifiers

- FactionOwned
- Guarded
- Welcoming
- Suspicious
- Hidden
- LanguageBarrier
- TradeAvailable
- HelpRequested
- Temporary
- UnderThreat

### Important Rule

Faction attitudes and contact stages belong to faction state.

The contact site exposes and reacts to that state; it does not duplicate it.

---

## 11.7 Resource Site

**ID:** `resource-site`

### Dominant Question

> Is this resource safe, useful, renewable and socially acceptable to use?

### Typical Anchors

- point
- area

### Example Variants

- freshwater spring
- medicinal herb patch
- fishing ground
- berry grove
- abandoned supply cache
- salvage pile
- salt deposit
- timber stand
- animal herd
- tool store

### Default Actions

- Observe
- Assess
- Collect
- HarvestCarefully
- HarvestFully
- Purify
- TakeSample
- Mark
- Leave

### Typical Outputs

- Supplies
- Medicine
- equipment or materials
- findings
- contamination
- injury
- faction reaction
- depletion
- future renewable use

### Operational States

```text
Available
Low
Depleted
Renewing
Contaminated
Destroyed
```

### Common Modifiers

- Renewable
- FactionOwned
- Sacred
- Contaminated
- Seasonal
- Hidden
- DangerousToHarvest
- HeavyYield
- Perishable

### Scope Rule

Resource sites support expeditions.

They must not become the foundation of a production-chain or city-builder economy.

---

## 11.8 Hazard Zone

**ID:** `hazard-zone`

### Dominant Question

> What is dangerous here, how can it be understood, and can the expedition pass or reduce the danger?

### Typical Anchors

- area
- point
- path

### Example Variants

- toxic swamp
- unstable ground
- predator territory
- spore forest
- rockfall zone
- disease area
- memory-loss riverbank
- flooded valley
- unnatural silence zone
- burning region

### Default Actions

- Observe
- AssessRisk
- Avoid
- TraverseCautiously
- TraverseQuickly
- UseProtection
- TakeSample
- InvestigateCause
- FindSafeRoute
- Mitigate
- Contain
- Mark

### Typical Outputs

- injury
- Medicine use
- Supply loss
- Morale loss
- delay
- finding
- safe route
- improved risk knowledge
- spreading or contained hazard
- world event

### Operational States

```text
Unknown
Active
Known
Mitigated
Contained
Cleared
Spreading
Dormant
```

### Common Modifiers

- Contaminated
- Seasonal
- Spreading
- FactionCreated
- CreaturePresence
- InvisibleUntilTriggered
- WeatherSensitive
- RouteBlocking

---

## 11.9 Landmark Site

**ID:** `landmark-site`

### Dominant Question

> What can this place reveal about the wider world?

### Typical Anchors

- point
- area
- path for very large structures

### Example Variants

- signal tower
- mountain peak
- giant wall
- crater rim
- lighthouse
- colossal statue
- ancient road junction
- visible temple
- carved mountain
- ruined harbor

### Default Actions

- ObserveFromDistance
- Approach
- Reach
- Climb
- Survey
- MapSurroundings
- Document
- CompareWithReports
- Signal
- Activate
- Mark

### Typical Outputs

- reported map regions
- better orientation
- improved scout direction
- new route candidates
- archive knowledge
- faction attention
- long-term mystery
- distant objective
- signal or mechanism effect

### Progress States

```text
Reported
Sighted
Reached
Surveyed
Understood
Activated
Damaged
```

### Common Modifiers

- VisibleAtDistance
- Climbable
- Active
- FactionControlled
- Ancient
- Damaged
- SignalCapable
- MultiRegionVisible

### Information Rule

A landmark improves orientation and evidence.

It does not automatically reveal objective truth or exact hidden locations.

---

## 11.10 Dynamic Situation

**ID:** `dynamic-situation`

### Dominant Question

> What is happening now, and will the expedition intervene before the situation changes?

### Typical Anchors

- point
- moving point
- area

### Example Variants

- injured person
- faction patrol
- refugee group
- stranded boat
- burning camp
- captured scout
- merchant caravan
- ongoing ritual
- pursued traveler
- creature attack

### Default Actions

- Observe
- Follow
- Approach
- Communicate
- Assist
- Treat
- Intervene
- Negotiate
- Trade
- Withdraw
- Ignore

### Typical Outputs

- rescue or loss
- faction reaction
- information
- recruitment
- trade
- conflict
- new trace site
- moved situation
- later consequence
- member memory

### Operational States

```text
Pending
Observed
Engaged
Resolved
Expired
Moved
Escalated
```

### Common Modifiers

- TimeLimited
- Moving
- FactionOwned
- Hostile
- Injured
- Deceptive
- Urgent
- Watched
- ContainsKnownMember

### Lifecycle Rule

A resolved or expired situation may create a persistent location.

Examples:

- burning camp -> Trace Site
- dead patrol -> Trace Site
- stranded boat -> Resource Site or Trace Site
- captured scout moved away -> new Dynamic Situation at another location

---

## 12. Modifier System

Modifiers are reusable behavior packages.

A modifier may contain:

- compatible archetypes
- incompatible modifiers
- added actions
- removed actions
- added requirements
- risk adjustments
- cost adjustments
- outcome adjustments
- state constraints
- faction effects
- UI hints
- visual tags
- expiry rules

### 12.1 Initial Modifier Families

#### Physical

- Sealed
- Unstable
- Collapsed
- Flooded
- Underground
- Damaged
- Burning

#### Social and Cultural

- FactionOwned
- Sacred
- Taboo
- Guarded
- Watched
- Occupied
- Invitation

#### Danger

- Contaminated
- Trapped
- HostilePresence
- CreaturePresence
- Spreading

#### Utility

- Searchable
- Repairable
- Campable
- Harvestable
- Climbable
- SignalCapable
- ContainsHeavyObject

#### Historical

- RecentlyUsed
- PreviouslyVisited
- ChangedByExpedition
- WeatherDamaged
- OldKnowledgeLinked

### 12.2 Modifier Compatibility

Definitions must declare compatibility.

Examples:

- `Renewable` is valid for Resource Sites.
- `Repairable` is valid for Route Obstacles, Landmarks and some Investigation Sites.
- `Sacred` may apply to Investigation Sites, Territorial Markers, Resource Sites or Contact Sites.
- `Moving` is normally valid only for Dynamic Situations.

Validation should warn when incompatible combinations are authored.

---

## 13. Content Profiles

The archetype defines the interaction pattern.

The content profile defines what the player actually learns or receives.

A content profile may contain:

- visible description stages
- clue pool
- hidden clue pool
- false interpretation options
- finding pool
- resource rewards
- knowledge values
- faction links
- hidden purpose
- archive entry templates
- journal text
- image or portrait references
- delayed consequence definitions
- related mandate evidence
- repeat rules

### Example: Trace Site Variants

#### Cold Campfire

```text
Clues:
- ash age
- fish bones
- boot print

Possible Finding:
- unfamiliar seed

Knowledge:
- low

Resources:
- none
```

#### Old Expedition Camp

```text
Clues:
- damaged tent
- blood on map case
- missing personal item
- route notes

Possible Findings:
- journal page
- damaged map
- unknown medicine

Knowledge:
- medium to high

Resources:
- possible Supplies or Medicine
```

Both use `trace-site`.

### 13.1 Dynamic Reference Slots

A Content Profile is normally authored, static data. One declared exception exists: content that must reference a specific, procedurally-created identity — a named expedition member lost in the field (Section 9.9's Recovery Resolution, `Lost` outcome), a specific prior expedition, a specific earlier consequence event — rather than a generic template.

A Content Profile may declare **dynamic reference slots**, named placeholders that get filled in at generation time by whatever effect created the reference (e.g. `create recovery objective`, Section 9.8B), not at authoring time:

```text
content-downstream-camp:
  journalText: "A weathered pack lies half-buried near the bank.
                Inside: {lostMemberName}'s water-stained journal."
  dynamicReferenceSlots:
    - lostMemberName
    - lostExpeditionId
```

This is the only place identity-specific data is allowed to enter an otherwise authored Content Profile. Everywhere else, Content Profiles remain static and interchangeable across generated worlds.

---

## 14. Composite Locations

Large or complex places should not force one archetype to perform every function.

A composite location may contain a parent location and child locations.

Example: ruined village

```text
Parent:
- Investigation Site: Ruined Village

Children:
- Trace Site: Burned House
- Resource Site: Intact Well
- Hazard Zone: Unstable Cellar
- Territorial Marker: Fresh Warning Symbol
- Dynamic Situation: Hidden Survivor
```

The parent provides shared:

- name
- map presentation
- faction context
- discovery card
- archive grouping
- hidden purpose

Children provide their normal reusable interaction patterns.

Composite locations are optional for the MVP but the data model should allow them.

---

## 15. Knowledge, Evidence and Preservation

Location results must connect to the existing knowledge-preservation model.

Every clue, report or finding should declare its preservation type:

```text
SharedMemory
WitnessBound
WrittenRecord
PhysicalFinding
PreviouslyTransmitted
```

Examples:

- the entire expedition sees the destroyed bridge: `SharedMemory`
- only the Scholar interprets a symbol: `WitnessBound`
- copied inscription: `WrittenRecord`
- recovered metal object: `PhysicalFinding`
- scout report already delivered to base: `PreviouslyTransmitted`

Location interaction should therefore create evidence records, not only add a number.

Knowledge Points are awarded later when valid unsecured knowledge is secured at the base.

---

## 16. Persistence and Legacy

Locations persist across expeditions.

The saved instance should remember:

- discovery and reliability state
- interaction progress
- operational state
- active modifiers
- consumed clues and rewards
- active or paused project
- physical changes
- faction memories caused here
- scheduled consequences
- expedition IDs that interacted with it
- member memories linked to it
- last confirmation date

Examples:

- a grave opened by Expedition 1 remains opened
- a repaired bridge remains usable
- a depleted cache remains depleted
- a hazard may spread during base time
- a faction may renew removed warning markers
- an old camp may deteriorate
- a completed bridge project may later be damaged

Location history should support readable archive and journal summaries.

---

## 17. JSON Authoring Contract

Location content is authored as JSON data and loaded into generic registries. Gameplay code must not know whether a concrete entry is a broken bridge, a grave, a shrine or a campfire. It only reads stable IDs, declarative requirements, costs, risk profiles, outcome tables, effects and presentation fields.

### 17.1 Loader Rules

The loader scans the configured game-data folders, reads every JSON document with a supported `documentType`, validates references, then merges the definitions into registries by stable ID.

Recommended MVP folder layout:

```text
UnityHexMapView/Assets/GameData/Locations/Archetypes/*.json
UnityHexMapView/Assets/GameData/Locations/Variants/*.json
UnityHexMapView/Assets/GameData/Locations/Modifiers/*.json
UnityHexMapView/Assets/GameData/Locations/Actions/*.json
UnityHexMapView/Assets/GameData/Locations/OutcomeTables/*.json
UnityHexMapView/Assets/GameData/Locations/ContentProfiles/*.json
UnityHexMapView/Assets/GameData/Locations/Instances/*.json
```

This folder layout is a convention for humans, not a gameplay rule. The code keys off `documentType` and IDs, not filenames or concrete variant names.

Each document uses this envelope:

```json
{
  "documentType": "location-actions",
  "schemaVersion": 1,
  "contentVersion": 1,
  "items": []
}
```

Supported `documentType` values:

```text
location-archetypes
location-variants
location-modifiers
location-actions
location-outcome-tables
location-content-profiles
location-instances
```

Rules:

- `schemaVersion` controls parser compatibility.
- `contentVersion` controls balancing/text/content iteration and does not by itself require save migration.
- `id` values are globally stable within their registry.
- References are by ID only.
- Runtime saves store IDs, state and resolved history, not duplicated definitions.
- Unknown IDs fail validation in editor/tests and fail gracefully at runtime with placeholder text rather than corrupting state.

### 17.2 Archetype Documents

Archetypes define the default interaction pattern and default action library.

```json
{
  "documentType": "location-archetypes",
  "schemaVersion": 1,
  "items": [
    {
      "id": "route-obstacle",
      "label": "Route Obstacle",
      "defaultActionIds": [
        "action-assess-crossing",
        "action-find-bypass",
        "action-construct-temporary-passage",
        "action-attempt-crossing",
        "action-mark",
        "action-leave"
      ],
      "allowedAnchorKinds": ["Point", "Edge", "Area"],
      "presentation": {
        "categoryLabel": "FUNDSTELLE",
        "icon": "route-obstacle"
      }
    }
  ]
}
```

### 17.3 Variant Documents

Variants define what the location appears to be. They may add or remove action IDs, but they do not create bespoke code paths.

```json
{
  "documentType": "location-variants",
  "schemaVersion": 1,
  "items": [
    {
      "id": "broken-bridge",
      "archetypeId": "route-obstacle",
      "label": "Zerstoerte Bruecke",
      "subtitle": "Streckenhindernis",
      "addedActionIds": [],
      "removedActionIds": [],
      "compatibleModifierIds": [
        "modifier-repairable",
        "modifier-unstable",
        "modifier-watched"
      ],
      "defaultContentProfileId": "content-old-trade-road-bridge",
      "presentation": {
        "icon": "broken-bridge",
        "imageId": "placeholder-bridge"
      }
    }
  ]
}
```

### 17.4 Modifier Documents

Modifiers alter reusable behavior: actions, requirements, costs, risk, effects or presentation hints. A modifier can be dormant when its `appliesWhen` condition is not met.

```json
{
  "documentType": "location-modifiers",
  "schemaVersion": 1,
  "items": [
    {
      "id": "modifier-unstable",
      "label": "Instabil",
      "addedActionIds": [],
      "removedActionIds": [],
      "appliesWhen": {
        "operationalStateAny": ["Blocked", "RiskyPassage"]
      },
      "riskAdjustments": [
        {
          "actionId": "action-attempt-crossing",
          "scoreDelta": 15
        },
        {
          "actionId": "action-rebuild-bridge",
          "scoreDelta": 15
        }
      ],
      "presentation": {
        "chipTone": "warning"
      }
    }
  ]
}
```

### 17.5 Action Documents

Actions are reusable operations. The UI must be able to render any action from this shape without special-case code.

```json
{
  "documentType": "location-actions",
  "schemaVersion": 1,
  "items": [
    {
      "id": "action-attempt-crossing",
      "label": "Bruecke ueberqueren",
      "description": "Die Truemmer oder die provisorische Querung nutzen und die andere Seite erreichen.",
      "family": "Traverse",
      "visibility": {
        "default": "visible"
      },
      "hardRequirements": [
        {
          "kind": "PositionOnOrAdjacent",
          "unmetReason": "Die Expedition muss am Ort oder angrenzend sein."
        }
      ],
      "costs": [
        {
          "kind": "MovementPoints",
          "amount": 1,
          "timing": "onCommit"
        }
      ],
      "duration": {
        "kind": "Immediate"
      },
      "riskProfile": {
        "baseRisk": 45,
        "baseRiskByOperationalState": {
          "Blocked": 45,
          "RiskyPassage": 20,
          "Repaired": 5
        },
        "confidence": "Assessed",
        "bandThresholds": "default"
      },
      "outcomeTableId": "outcome-route-obstacle-attempt-crossing",
      "repeatPolicy": "RepeatableWithCost",
      "presentation": {
        "icon": "crossing",
        "primaryButtonLabel": "Aktion durchfuehren"
      }
    }
  ]
}
```

Cost objects use this common shape:

```json
{
  "kind": "Supplies",
  "amount": 1,
  "timing": "onCommit",
  "optional": false,
  "failureBehavior": "rejectIfCannotPay"
}
```

Initial cost kinds:

```text
MovementPoints
FieldDay
ProjectDay
Supplies
Medicine
Morale
Capacity
EquipmentDurability
TradeGoods
FactionGoodwill
MemberRisk
```

### 17.6 Outcome Table Documents

Outcome tables define weighted outcomes per risk band and the effects for each tier. The resolver consumes the table generically; the content profile supplies concrete text, clues and presentation where referenced.

```json
{
  "documentType": "location-outcome-tables",
  "schemaVersion": 1,
  "items": [
    {
      "id": "outcome-route-obstacle-attempt-crossing",
      "appliesTo": {
        "archetypeId": "route-obstacle",
        "actionId": "action-attempt-crossing"
      },
      "tiers": {
        "Low": [
          { "tier": "MajorSuccess", "weight": 20 },
          { "tier": "Success", "weight": 55 },
          { "tier": "SuccessWithCost", "weight": 15 },
          { "tier": "PartialResult", "weight": 8 },
          { "tier": "Failure", "weight": 2 },
          { "tier": "SevereFailure", "weight": 0 }
        ],
        "Moderate": [
          { "tier": "MajorSuccess", "weight": 15 },
          { "tier": "Success", "weight": 45 },
          { "tier": "SuccessWithCost", "weight": 20 },
          { "tier": "PartialResult", "weight": 10 },
          { "tier": "Failure", "weight": 8 },
          { "tier": "SevereFailure", "weight": 2 }
        ],
        "High": [
          { "tier": "MajorSuccess", "weight": 5 },
          { "tier": "Success", "weight": 25 },
          { "tier": "SuccessWithCost", "weight": 25 },
          { "tier": "PartialResult", "weight": 20 },
          { "tier": "Failure", "weight": 15 },
          { "tier": "SevereFailure", "weight": 10 }
        ],
        "Extreme": [
          { "tier": "MajorSuccess", "weight": 0 },
          { "tier": "Success", "weight": 15 },
          { "tier": "SuccessWithCost", "weight": 20 },
          { "tier": "PartialResult", "weight": 20 },
          { "tier": "Failure", "weight": 25 },
          { "tier": "SevereFailure", "weight": 20 }
        ]
      },
      "effectBundles": {
        "MajorSuccess": [
          {
            "kind": "AddUnsecuredKnowledge",
            "amount": 2,
            "text": "Die Expedition bestaetigt eine nutzbare Route ueber die Schlucht."
          }
        ],
        "Success": [
          {
            "kind": "AddUnsecuredKnowledge",
            "amount": 1,
            "text": "Die Expedition erreicht die andere Seite."
          }
        ],
        "SuccessWithCost": [
          {
            "kind": "ConsumeSupplies",
            "amount": 1,
            "text": "Beim Uebergang geht Ausruestung verloren."
          },
          {
            "kind": "ChangeMorale",
            "amount": -1,
            "text": "Der Uebergang belastet die Gruppe."
          }
        ],
        "PartialResult": [
          {
            "kind": "ConsumeSupplies",
            "amount": 1,
            "text": "Die Expedition muss umkehren und verliert Material."
          }
        ],
        "Failure": [
          {
            "kind": "ChangeMorale",
            "amount": -1,
            "text": "Der Versuch scheitert und erschuettert die Gruppe."
          }
        ],
        "SevereFailure": [
          {
            "kind": "InjureMember",
            "selection": "randomActiveMember",
            "severity": "Wounded",
            "text": "Ein Expeditionsmitglied stuerzt bei der Querung."
          }
        ]
      }
    }
  ]
}
```

Outcome tier IDs are fixed vocabulary:

```text
MajorSuccess
Success
SuccessWithCost
PartialResult
Failure
SevereFailure
```

An outcome table may omit tiers that are impossible for an action, but every weighted tier must have a matching effect bundle. Zero-weight tiers may be omitted from `effectBundles`.

### 17.7 Content Profile Documents

Content profiles provide authored text, clues, findings, image references and flavor slots. They must not redefine gameplay rules.

```json
{
  "documentType": "location-content-profiles",
  "schemaVersion": 1,
  "items": [
    {
      "id": "content-old-trade-road-bridge",
      "title": "Zerstoerte Bruecke",
      "shortDescription": "Die alte Handelsbruecke ist eingestuerzt.",
      "description": "Balken haengen schraeg ueber der Schlucht; zu instabil, um sie einfach zu betreten.",
      "flavorByState": {
        "Blocked": "Die Schlucht trennt die alte Handelsroute.",
        "RiskyPassage": "Ein provisorischer Uebergang haengt ueber der Schlucht.",
        "Repaired": "Die Bruecke ist wieder passierbar."
      },
      "imageId": "placeholder-bridge",
      "journalText": {
        "discovered": "Eine zerstoerte Bruecke blockiert die Route.",
        "resolved": "Die Bruecke wurde als Routenproblem dokumentiert."
      }
    }
  ]
}
```

### 17.8 Instance Documents

Instances place authored or generated locations into a world. They reference definitions and store initial runtime state only.

```json
{
  "documentType": "location-instances",
  "schemaVersion": 1,
  "items": [
    {
      "id": "loc-old-trade-road-bridge",
      "archetypeId": "route-obstacle",
      "variantId": "broken-bridge",
      "anchor": {
        "kind": "Edge",
        "hexes": [[25, 21], [26, 20]]
      },
      "modifierIds": [
        "modifier-repairable",
        "modifier-unstable",
        "modifier-watched"
      ],
      "factionIds": ["border-wardens"],
      "contentProfileId": "content-old-trade-road-bridge",
      "initialState": {
        "knowledge": "Unknown",
        "interaction": "Untouched",
        "operational": "Blocked",
        "presence": "Unknown"
      },
      "generationTags": [
        "old-road",
        "river-crossing"
      ]
    }
  ]
}
```

Anchor shapes:

```json
[
  { "kind": "Point", "hexes": [[18, 23]] },
  { "kind": "Edge", "hexes": [[25, 21], [26, 20]] },
  { "kind": "Area", "hexes": [[10, 12], [10, 13], [11, 12]] },
  { "kind": "Path", "hexes": [[10, 12], [11, 12], [12, 13]] }
]
```

### 17.9 UI Binding Contract

The location interaction screen binds to the resolved interaction model, not raw JSON and not variant-specific code.

The model exposed to UI must contain:

- location title, subtitle, icon/image and flavor text from Content Profile + Variant presentation
- anchor text derived from the generic anchor
- state chips derived from state channels
- modifier chips derived from active or dormant modifier definitions
- action rows derived from resolved available/locked action definitions
- cost summary derived from `costs`
- risk band and confidence derived from `riskProfile`
- selected action detail from the action definition
- result view from the resolved outcome tier and effect bundle text
- project view from active project state and project-related effects

If a new JSON action is authored using existing requirement, cost, risk, outcome and effect kinds, the screen must render it without code changes.

### 17.10 MVP Minimum

For the first playable implementation, the JSON loader only needs to support:

- `location-actions`
- `location-outcome-tables`
- `location-content-profiles`
- `location-instances`
- the currently implemented archetypes, variants and modifiers
- the currently implemented requirement, cost and effect kinds

Hardcoded fallback definitions are acceptable during migration, but JSON definitions are authoritative when present. The long-term target is that adding a new concrete location requires JSON only unless it introduces a genuinely new requirement, cost or effect kind.

### 17.11 Broken Bridge Instance Example

```json
{
  "documentType": "location-instances",
  "schemaVersion": 1,
  "items": [
    {
      "id": "loc-old-trade-road-bridge",
      "archetypeId": "route-obstacle",
      "variantId": "broken-bridge",
      "anchor": {
        "kind": "Edge",
        "hexes": [[25, 21], [26, 20]]
      },
      "modifierIds": [
        "modifier-repairable",
        "modifier-unstable",
        "modifier-watched"
      ],
      "contentProfileId": "content-old-trade-road-bridge",
      "initialState": {
        "knowledge": "Unknown",
        "interaction": "Untouched",
        "operational": "Blocked",
        "presence": "Unknown"
      }
    }
  ]
}
```

### 17.12 Marked Grave Instance Example

```json
{
  "documentType": "location-instances",
  "schemaVersion": 1,
  "items": [
    {
      "id": "loc-warden-marked-grave",
      "archetypeId": "investigation-site",
      "variantId": "marked-grave",
      "anchor": {
        "kind": "Point",
        "hexes": [[18, 23]]
      },
      "modifierIds": [
        "modifier-sacred",
        "modifier-faction-owned",
        "modifier-searchable"
      ],
      "factionIds": [
        "border-wardens"
      ],
      "contentProfileId": "content-warden-grave-warning",
      "initialState": {
        "knowledge": "Unknown",
        "interaction": "Untouched",
        "operational": "Closed",
        "presence": "Empty"
      }
    }
  ]
}
```

These examples should resolve through generic systems.

Neither requires a unique location class.

---

## 18. Procedural and Semi-Procedural Generation

The MVP may use hand-authored locations.

Later generation should select compatible components.

A generated location may be composed from:

```text
Archetype
+ Variant
+ Anchor
+ 0..N Compatible Modifiers
+ Content Profile
+ Faction Link
+ Hidden Purpose
+ Clue Set
+ Outcome Profile
```

Definitions may contain generation constraints:

- allowed biomes
- prohibited biomes
- allowed anchor kinds
- distance from coast
- distance from base
- faction territory requirements
- route proximity
- required neighboring terrain
- rarity
- campaign-stage range
- incompatible modifiers
- required clue support
- unique-per-world flag

### 18.1 Placement Is a Post-Generation Stage, Driven by the Generated Map

Location placement runs **after** the world map already exists — after terrain, climate, rivers, biomes, faction archetypes, territory and roads have all been generated. It consumes that map as **read-only input**. This is why every generation constraint above references something that only exists post-generation (biome, coast distance, route proximity, faction territory): placement is not part of terrain generation, it *reads* terrain generation's result.

Concretely, this stage is the procedural world-generation concept's **Stage 9 (Special Locations & Landmarks)**, which runs after Faction Territory Growth (Stage 7) and Roads (Stage 8) and feeds Landmark Visibility (Stage 10). This document owns *what* a location is and *the logic by which it earns its spot*; the world-generation pipeline owns *when* the stage runs and hands it the finished map.

**Coherence principle (shared with the world-generation concept):** every generated location is placed for a reason **derivable from other generated data — never dropped at random.** A generated location that cannot be explained from terrain, route or faction data is a generation bug, not content. This is the placement-side counterpart to the Generation Quality Rule 4 below ("connect to a faction, route, hazard, mandate or larger mystery").

### 18.2 Placement Is Terrain-Driven; Faction Ownership Is Derived From Where It Lands

Two questions must be kept separate — conflating them is what made the first generator pass feel wrong:

1. **Where does a location go?** (placement)
2. **Does a faction own it?** (ownership)

**Placement is terrain-driven and territory-agnostic.** With the two exceptions below, a location's *position* is chosen entirely from terrain and route data (the §18.3 rules) — it does **not** require, prefer or avoid faction territory. A location may land inside a territory, in a buffer zone, or in unclaimed wilderness with equal right. This is what keeps placement robust **even when territories are large or cover the whole island**: there is always somewhere to put a terrain-appropriate location, because placement never needed empty land in the first place.

**Ownership is derived after placement, from territory containment.** The `FactionOwned` / `Guarded` / `Watched` modifiers can attach to a location **only if its anchor lies inside a faction's territory, and then only for that enclosing faction.** A location in unclaimed land is neutral by necessity — no faction modifier can apply, because no faction is present to claim it.

Inside a territory, ownership is *possible but not automatic*, and the archetype's nature decides it — this is the world-generation concept's **World History Hook** made concrete (the immutable terrain layer predates the mutable faction layer around it):

- **Ancient, terrain-old things** — ruins, containment sites, landmarks, old graves — stay **neutral in origin even inside a territory**, because they predate the faction living around them. A faction may still *watch*, *revere* or *forbid* such a place (`Watched`, `Sacred`), but it does not *own* it (`FactionOwned`).
- **Recent, human things** — a fresh warning marker, an occupied camp, a currently-used resource patch — inside a territory naturally take `FactionOwned` for the enclosing faction.

**The two inherently-faction archetypes are the exceptions to "placement is terrain-driven":** `territorial-marker` and `contact-site` are placed *because of* a faction (on its borders, at its settlements), so they are faction-owned by construction. Everything else is placed by terrain and *may or may not* end up owned, depending purely on where it landed.

**Locked resolution — a fully-covered island is defined, not a failure.** If territory leaves no wilderness, terrain-driven locations are still placed everywhere they fit; each simply becomes eligible for ownership by whichever territory encloses it, with ancient ones staying neutral-in-origin per the rule above. A map with no free land is therefore one where most neutral-origin locations happen to sit inside someone's territory — exactly what a long-settled island should look like. **"Neutral" describes a location's origin and allegiance, not a requirement that it stand on unclaimed ground.** The earlier generator pass's real mistake was not "too much territory" but treating faction-ownership as a *placement* driver instead of a *post-placement* consequence.

### 18.3 Archetype → Terrain and Anchor Placement Rules

Each archetype has a natural geographic home. This mapping is what turns "generation constraints exist as fields" into "the generator knows where to look."

| Archetype | Ownership tendency | Anchor | Placement logic (where it earns its spot) |
|---|---|---|---|
| `trace-site` | Neutral | Point / small area | Along old routes, road segments and coast landings — *where someone plausibly passed through and left something behind.* |
| `investigation-site` | Neutral (ancient) or Faction | Point / area | Ancient ruins, graves and shrines in **remote or elevated** interior; faction-owned graves/markers near a territory's edge. |
| `route-obstacle` | Neutral | Edge | Only on a **road/path edge that crosses a river, ravine or steep elevation delta** — an obstacle earns placement only where a route actually needs the crossing. |
| `containment-site` | Neutral (ancient) | Point / edge | **Remote, defensible, sealed-feeling** spots: high elevation, cave-like (steep-surrounded), far from base by path cost. |
| `territorial-marker` | Faction | Point / edge / area boundary | On a faction territory **boundary**, preferentially snapped to the natural border feature (river / ridge) the territory already formed along. |
| `contact-site` | Faction | Point / settlement | **The faction settlements themselves** (capitals and towns already produced by the settlement stage) — not a separate placement pass. |
| `resource-site` | Neutral | Point / area | **Biome-appropriate:** spring / fishing near fresh water; herbs / berries in forest; salt / wrack near coast; timber in dense forest. |
| `hazard-zone` | Neutral | Area / path | Stamped onto a matching **biome region**: swamp → disease/toxic; volcanic lands → burning/unstable; dead zone → unnatural; predator range in open wilderness. |
| `landmark-site` | Neutral | Point / area | On **visually dominant terrain** — peaks, ridgelines, coastal headlands — and it feeds the Stage 10 viewshed (visible-before-reachable). |
| `dynamic-situation` | Runtime, not world-gen | Point / moving | Spawned during play near routes / territory (deferred past the slice, §21); not placed at world-generation time. |

The *Ownership tendency* column is only realized through §18.2: a `Faction` tendency attaches a faction modifier **only** when the anchor lands inside that faction's territory (and stays neutral otherwise); a `Neutral (ancient) or Faction` archetype stays neutral-in-origin even inside a territory but may pick up `Watched` / `Sacred`. `territorial-marker` and `contact-site` are the by-construction faction cases whose *placement itself* targets a faction.

Two archetypes therefore need **no dedicated terrain placement search**: `contact-site` reuses existing settlements, and `dynamic-situation` is a runtime spawn. The other eight are what Stage 9 actively places by terrain.

### 18.4 Anchor-Kind Placement Requirements

Placement must respect the anchor model (§4), not just biome:

- **Point** — any single cell satisfying the definition's constraints.
- **Edge** — requires a *valid traversable edge* between two adjacent land hexes. `route-obstacle` additionally requires that edge to lie on a **road** or a **natural crossing** (river mouth, ravine, steep step); an edge obstacle in open country blocking nothing is not placed.
- **Area** — requires a contiguous terrain / biome region of at least a minimum size; the anchor *is* that region, not one cell.
- **Path** — deferred (§4.4); when added, an ordered edge / hex sequence along a road or river.

If a definition's anchor kind cannot be satisfied on the current map (e.g. no road-over-river edge exists for a broken bridge), the generator **skips or relaxes** that definition rather than forcing it — and must never emit an edge anchor on a non-adjacent or invalid hex pair (the runtime mirror of Authoring Validation §19's "edge anchors whose hexes are not adjacent").

### 18.5 Density Driven by Map Features, Not a Flat Baseline

The count of neutral locations should scale with the **features the map actually generated**, not a single area ratio:

- `route-obstacle` count ∝ a fraction of the map's road × (river / ravine) crossings — not every crossing gets one.
- `hazard-zone` count ∝ the number of qualifying biome regions (swamp / volcanic / dead-zone clusters).
- `landmark-site` count ∝ the number of dominant peaks and headlands, capped.
- `resource-site` count ∝ available water and forest, scaled to map size.
- `investigation-site` / `containment-site` / `trace-site` count ∝ the area of **remote interior** land — measured by path-cost distance from the base and from the nearest settlement, **not** by whether the land is claimed. This keeps the formula working when territory covers everything (a remote spot deep inside a large territory still counts as remote).

Two guardrails bound this: the **hard floor** from the world-generation concept's Stage 9 (never fewer than the MVP minimum set, even on the smallest allowed map) and a **global cap** so a large map is not oversaturated into unreadable noise. Faction-anchored counts are unchanged — they still come from each faction's rolled taboos and values.

### 18.6 MVP Scope Alignment

Consistent with §21, the Vertical Slice does not place all ten archetypes. The slice's placement pass only needs: `trace-site`, `investigation-site`, `route-obstacle`, a minimal `territorial-marker`, and a preview `containment-site`. The neutral-track archetypes deferred there (`resource-site`, `hazard-zone`, `landmark-site`) still follow the §18.3 rules when they arrive; nothing above requires a slice-time implementation of all eight.

### Generation Quality Rules

A generated significant location should:

1. have a purpose beyond generic loot
2. offer at least two meaningful responses
3. provide fair clues before major irreversible consequences
4. connect to a faction, route, hazard, mandate or larger mystery
5. support persistence
6. not always be dangerous
7. not always be beneficial
8. sometimes remain unresolved for later expeditions
9. have a valid presentation before and after investigation
10. avoid contradictory modifier combinations

---

## 19. Authoring Validation

Automated validation should report:

- duplicate or missing IDs
- unsupported `documentType` values
- missing or unsupported `schemaVersion`
- malformed JSON document envelopes
- duplicate IDs across files in the same registry
- missing archetype, variant, modifier or action references
- missing outcome table, content profile or effect bundle references
- unsupported anchor types
- invalid state values
- invalid state transitions
- unsupported requirement, cost or effect kinds
- actions with impossible requirements
- actions with costs that cannot be paid and no declared failure behavior
- outcome tables with no valid outcome
- weighted outcome tiers with no matching effect bundle
- outcome table weights that are all zero for a reachable risk band
- unknown effects
- missing content text
- rewarding actions without repeat policies
- irreversible consequences without linked warning evidence
- physical findings without Capacity or preservation rules
- faction consequences without a faction link
- edge anchors whose hexes are not adjacent
- area anchors with no cells
- projects with no completion effect
- modifier incompatibilities
- locations with no safe exit or leave action
- procedural combinations that violate generation constraints
- two modifiers declared compatible on the same archetype that also declare contradictory Hard Requirements on the same action, unless scoped to mutually exclusive states (Section 2.7, Rule D)
- an action whose combined Hard Requirements can never be simultaneously satisfied given the declared state model (Section 2.7, Rule D)
- a Variant that removes an action which no active or declarable Modifier for this archetype ever re-adds (a soft warning, not necessarily an error)
- an Outcome Table tier with no Effect Bundle (Section 9.8)

Validation should run in editor tooling and automated tests where practical.

---

## 20. Versioning and Save Compatibility

Each authored definition should have:

- stable ID
- schema version
- optional content version

Save files should reference stable IDs.

When definitions change:

- compatible text and balance changes require no migration
- renamed IDs require alias or migration tables
- changed state schemas require explicit migration
- removed content should fail gracefully and retain a placeholder record rather than corrupting the world save

Generated outcomes should be stored once resolved so later balance changes do not retroactively reroll world history.

---

## 21. MVP Implementation Boundary

The MVP should implement the framework broadly but the content narrowly.

### Required Framework

- definition and instance separation
- stable registry IDs
- primary archetype
- variants
- modifiers
- point, edge and area anchors
- multiple state channels
- generic requirements
- generic costs
- generic actions
- generic effects
- shared risk resolution
- persistent state changes
- multi-day projects
- journal and archive hooks
- authoring validation

### Initial Playable Archetypes

All ten archetypes should be representable in data.

The Vertical Slice does not need a major authored example of all ten, and — cross-checked against `mvp_vertical_slice_concept.md`'s actual named MVP locations (Marked Grave, Broken Bridge, Abandoned Camp, Sealed Gate) and `world_locations_and_events_concept.md`'s zone descriptions — needs fewer than previously listed here.

**Vertical Slice scope, at three different depths:**

```text
Full implementation (real risk resolution, real Outcome Tables):
  - Trace Site         (Abandoned Camp)
  - Investigation Site (Marked Grave)
  - Route Obstacle     (Broken Bridge - see Section 25 for a full worked example)

Minimal implementation (a handful of actions, not the full archetype library):
  - Territorial Marker (a single warning-sign instance; only Observe,
    Interpret, CrossBoundary are needed - required because
    `world_locations_and_events_concept.md`'s Zone 2 explicitly calls
    for "warning signs... first suspected border")

Preview only (visible, zero resolution mechanics):
  - Containment Site (Sealed Gate; Observe only, no Operational State
    machine or ForceOpen - the world document's own text already says
    this location "should not necessarily be solved in the first
    expedition")
```

**Deferred past the Vertical Slice** (not required by any named MVP location, zone description, or locked decision, though fully specified as reusable archetypes for the first content milestone after the slice): Contact Site, Landmark Site, Dynamic Situation, Hazard Zone, Resource Site.

This lands at five slice locations total, inside `world_locations_and_events_concept.md`'s own stated range ("the MVP island should contain 4 to 6 special locations," Section 11.3.6), and covers every feature `mvp_vertical_slice_concept.md`'s Section 24A.4 requires (hidden borders via the minimal Territorial Marker, legacy knowledge via Trace Site, specialist-gated obstacles via Route Obstacle, faction territory reactions via the Territorial Marker's social-risk `CrossBoundary`, Section 9.4B).

The broader seven-archetype target (adding Contact Site, Landmark Site, and Dynamic Situation/Hazard Zone) remains the right goal for the first content milestone *after* the slice proves the loop is fun — not for the slice itself.

### Initial Concrete Slice Locations

Recommended first authored set, matching the narrowed scope above:

- old expedition camp: Trace Site (full)
- marked grave: Investigation Site (full)
- broken bridge: Route Obstacle (full — see Section 25)
- border warning sign near Border Wardens territory: Territorial Marker (minimal: Observe, Interpret, CrossBoundary only)
- sealed gate: Containment Site (preview only: Observe, no further mechanics)

Everything below belongs to the first post-slice content milestone, not the Vertical Slice itself:

- cold fire or improvised shelter: Trace Site (additional variant)
- Coastal People contact place: Contact Site
- signal tower: Landmark Site
- Hidden Ones warning situation: Dynamic Situation
- swamp danger: Hazard Zone
- freshwater source: Resource Site

### Initial Reusable Effects

The MVP should first support:

- add clue
- add finding
- add unsecured knowledge
- change location state
- consume expedition resource
- change Morale
- injure member
- change faction Trust, Anger or Fear
- add faction memory
- open or block route
- start and complete project
- schedule consequence
- add journal entry
- add map knowledge

Additional effects can be registered later.

---

## 22. Non-Goals

This concept does not require:

- a unique minigame for each location
- complex tactical combat
- a full crafting system
- a deep harvesting economy
- freeform construction anywhere
- simulation of every physical object in a camp
- procedural generation of final narrative prose
- unrestricted combinations of every modifier
- one universal state enum
- direct exposure of hidden world truth to the UI

---

## 23. Acceptance Criteria

The concept is successfully implemented when:

1. A new cold campfire can be authored as data using `trace-site`.
2. A new abandoned expedition camp can use the same logic but different clues and rewards.
3. A marked grave, ruined shrine and underground vault can all use `investigation-site`.
4. A sealed vault can gain opening behavior through a modifier without a bespoke class.
5. A broken bridge can block one edge between two hexes.
6. Repairing the bridge can use a persistent multi-day project and permanently reopen that edge.
7. A flooded ford can reuse the route-obstacle logic with different modifiers and outcomes.
8. Actions are derived from archetype, variant, modifiers and state rather than concrete variant switches.
9. Specialists can unlock, improve or de-risk actions through declarative contributions.
10. Location effects update expedition, faction, knowledge, route and world systems through generic effect handlers.
11. Location changes survive return to base and later expeditions.
12. The UI shows observed evidence and qualitative risk without exposing hidden purpose.
13. Rewarding actions have repeat rules and cannot be farmed indefinitely.
14. New variants usually require data and content only.
15. New reusable mechanics require one generic handler rather than code in every location.
16. Existing saves can continue to reference stable location IDs after content expansion.
17. A designer can author a new action's danger by filling in a Risk Profile and referencing an Outcome Table, without writing code (Section 9.4).
18. The same risk pipeline resolves a location action, a scout mission (Section 9.10A), and a social-stakes action like Trade or CrossBoundary (Section 9.4B) — never a second, parallel resolution system.
19. Two players facing identical true risk but different specialists/clues can see different displayed risk bands, while the actual resolution odds remain identical (Section 9.6).
20. No player-visible path exists for the displayed risk to understate true danger (Section 9.11, Fairness Rule 3).
21. A failed Disturb action on a marked grave can still preserve a finding through the Recovery Check, producing a "damaged but present" result rather than simple loss (Section 9.9).
22. Force-quitting the app at any point during an expedition and relaunching resumes at the exact latest resolved state, with no option to reach an earlier one (Section 9.10.2).
23. A scout mission with no escalation flags set cannot resolve to Captured or Dead on its final day (Section 9.10A.3).
24. Given a Variant that removes an action and a Modifier that re-adds it, the action is available (Section 2.7, Rule A).
25. Given two simultaneously active Modifiers where one adds and the other removes the same action, the action is unavailable regardless of list order (Section 2.7, Rule B).
26. A Modifier whose state-applicability condition isn't currently met contributes nothing without needing to be removed from the instance's modifier list (Section 5.5).

---

## 24. Recommended Repository Integration

After review and acceptance:

1. Add this file as:

```text
location_archetypes_and_interactions_concept.md
```

This single file now includes the archetype/variant/modifier framework, the full Risk and Outcome Resolution system (Section 9), Variant/Modifier conflict resolution (Section 2.7), a complete worked example (Section 25), and a design validation log (Section 26). It supersedes the separate `risk_and_outcome_resolution_concept.md`, `variant_and_modifier_conflict_resolution_concept.md`, and archetype-simulation working documents produced while developing this concept — those do not need to be kept as separate files going forward.

2. Link it from `exploration_game_concept.md`.

3. Move or replace overlapping standard-location-model detail from `world_locations_and_events_concept.md` with a short summary and reference to this document. In particular, `world_locations_and_events_concept.md`'s Section 23A ("Event and Risk System") should point to this document's Section 9 as the resolution mechanism, keeping only its own Risk Score input catalogue and event-category tone as the source of truth there.

4. Extend `technical_design_notes_for_agents.md` with the definition/instance split, anchor types, state channels, generic action/effect registries, and the Risk/Outcome data structures (Section 9.12).

5. Add a passive time-driven transition mechanism (renewal, expiry, decay) to `world_locations_and_events_concept.md`'s Base Phase section — Section 9.16 names this as a real requirement that belongs there, not here.

6. Cross-check the per-leg tick model (Section 9.10A) against the scout order structure (direction, duration, focus, behavior) in `map_and_exploration_concept.md` before authoring the first Scout Mission Outcome Table.

7. Cross-check the Interaction Range concept (Section 4.5) against `map_and_exploration_concept.md`'s Fog of War rules before authoring Landmark Site content.

8. Add the Base-Camp-only save policy and crash-safe suspend point (Section 9.10) as explicit locked decisions in `exploration_game_concept.md`'s Section 0, since they affect more systems than just risk resolution.

9. Keep concrete Vertical Slice coordinates and authored location instances in a separate map specification.

10. Do not ask a coding agent to implement the concrete bridge, grave or camp until this generic location framework and its acceptance tests (Section 23) exist. The Broken Bridge worked example (Section 25) can be used directly as the first implementation target and test case.

---

## 25. Worked Example: Broken Bridge

This is a complete, concrete authoring of a single location — the Old Trade Road Bridge — proving that the archetype/variant/modifier framework (Sections 1-8) and the Risk and Outcome Resolution system (Section 9) actually compose into something implementable, not just describable. Every action, requirement, cost, risk number and effect bundle below is filled in.

This example should be treated as authored content, not as a new system. It is also the recommended first implementation target for a coding agent, per Section 24, point 10.

### 25.1 Identity

```text
Archetype:  route-obstacle
Variant:    broken-bridge
Anchor:     Edge (hex [18,22] <-> hex [19,22])
Modifiers:  modifier-repairable, modifier-unstable, modifier-watched
Faction:    border-wardens (via modifier-watched)
```

#### World Truth (hidden)

The bridge was not destroyed by weather or age. The Border Wardens deliberately collapsed it years ago to slow outside movement toward their inner territory. They still watch the crossing from a distance.

#### Player Knowledge (starts at)

`Unknown`. Nothing is known about the bridge until the expedition observes it directly or a scout reports it from a distance.

This split follows Section 3.2 exactly: the UI and all resolution logic below only ever read Player Knowledge and Runtime World State, never World Truth directly.

---

### 25.2 State Channels

```text
Knowledge State:        Unknown -> Reported -> Discovered -> Confirmed
Interaction Progress:   Untouched -> Observed -> Inspected
Operational State:      Blocked -> RiskyPassage -> TemporarilyOpen -> Open
                                -> Repaired
                         (Destroyed is reachable only via Severe Failure
                          on Rebuild Bridge, see Section 4.7.1)
Presence State:         not used for this variant (no occupants)
```

Only three of the five Operational States are reachable through normal play for this specific authored instance (`Blocked`, `RiskyPassage` via a temporary crossing, `Repaired` via the project). `TemporarilyOpen` and `Open` are included in the enum because other route-obstacle variants use them, but this instance's action set never targets them directly — a deliberate authoring choice, not a gap.

---

### 25.3 Action Templates

Eight actions are attached, matching the archetype's default library plus modifier-gated additions. `Clear` is deliberately **not** included — that action belongs to variants like landslides or fallen trees, not a bridge. This is the reusability principle from Section 2.4 working as intended: not every archetype action has to appear on every variant instance.

| Action ID | Available when | Gated by |
|---|---|---|
| `action-observe` | always | — |
| `action-assess-crossing` | Operational State is `Blocked` or `RiskyPassage` | — |
| `action-find-bypass` | Operational State is `Blocked` | — |
| `action-send-scout-around` | Operational State is `Blocked`, a scout available | Capacity |
| `action-construct-temporary-passage` | Operational State is `Blocked` | — |
| `action-attempt-crossing` | Operational State is `Blocked` or `RiskyPassage` | — |
| `action-rebuild-bridge` | Operational State is `Blocked` or `RiskyPassage` | `modifier-repairable` present |
| `action-mark` / `action-leave` | always | — |

---

### 25.4 Action-by-Action Specification

#### 25.4.1 Observe

```text
Hard Requirements:   none
Soft Requirements:   none
Cost:                negligible (part of normal movement)
Duration:             instant
Base Risk:           5   (Trivial tier)
Situational:         none apply here (Unstable is a structural fact,
                      not a danger while merely looking)
Raw Risk Score:      5 -> Band: Low
```

Outcome Table: single-tier, deterministic (no real chance of failure to *look at a bridge*).

**Effect Bundle (always applied):**
- reveal visible description ("a collapsed wooden bridge, one support beam clearly cracked")
- change Interaction Progress -> `Observed`
- change Knowledge State -> `Discovered`
- add clue: `clue-bridge-cracked-beam` (a fair warning hint toward `Unstable`, satisfying the Containment/Fairness pattern that dangerous facts must be discoverable, not only hidden in data)

Repeat Policy: `OncePerState` (repeating after the state changes may reveal new visible description text; repeating without a state change is a no-op).

---

#### 25.4.2 Assess Crossing

```text
Hard Requirements:   Operational State is Blocked or RiskyPassage
Soft Requirements:   Scout present  -> Improve Interpretation (raises Estimate Confidence)
                     Engineer present -> Improve Interpretation
                                         (adds a repair-feasibility clue)
Cost:                part of a day, no Supplies
Duration:            part of a day
Base Risk:           10  (Trivial/Low boundary)
Situational:         + Unstable, but only if clue-bridge-cracked-beam
                       is already known (10 -> 25 once Unstable is
                       inferable; otherwise stays 10)
Raw Risk Score:      10 (Low) or 25 (Low, at the boundary) once Unstable
                     is suspected
```

Outcome Table (single tier, `Success` only — this action cannot itself fail, it only informs):

**Effect Bundle:**
- raise Estimate Confidence for `action-attempt-crossing` and `action-rebuild-bridge` from `Guess` to `Assessed`
- if Engineer present: add clue `clue-bridge-repairable-with-timber` (informs the Rebuild Bridge hard requirement, see 4.6)
- if `clue-bridge-cracked-beam` known: reveal `modifier-unstable` to Player Knowledge explicitly (the player now *knows* it's unstable, not just suspects it)

Repeat Policy: `OncePerState`.

---

#### 25.4.3 Find Bypass

```text
Hard Requirements:   Operational State is Blocked
Soft Requirements:   Scout present -> Reduce Cost (half duration),
                                       Reduce Risk (-15)
Cost:                1 full day, 1 day of Supplies at expedition rate
Duration:            1 day
Base Risk:           15  (Low-stakes default)
Situational:         + rough terrain (minor, +5)
Raw Risk Score:      20 (Low), or 5 with a Scout (Low)
```

**Outcome Table** (`route-obstacle` + `action-find-bypass`, Low Band):

```text
Major Success    15   -> permanent alternate route found; edge traversal
                          candidate created elsewhere on the map;
                          add map knowledge; OncePerLocation reward
Success          50   -> a usable but slower bypass found; adds a
                          temporary alternate edge, consumes +1 day
                          of future travel through this area
Partial Result   25   -> no bypass found, but terrain knowledge gained
                          (a clue toward a different action)
Failure           8   -> day and Supplies spent, nothing found
Severe Failure    2   -> a member is injured scouting difficult terrain
                          (Recovery Check applies, see the shared
                          Recovery Resolution in the risk/outcome doc)
```

Repeat Policy: `OncePerExpedition` (searching again during the same expedition after a Partial Result or Failure is allowed once more; a Major Success or Success closes the action for this expedition, since the bypass is now known).

---

#### 25.4.4 Send Scout Around

This action does not resolve through the main pipeline at all — it dispatches a scout mission and resolves through the **Scout Mission Resolution variant** (Section 9.10A).

```text
Hard Requirements:   Operational State is Blocked, 1 available scout (Capacity)
Mission Focus:       "find a route around this specific obstacle"
Mission Duration:    2-4 days depending on terrain
```

At dispatch, Stages 1-5 of the shared pipeline still apply (Section 10A.1): the player sees a Risk Band and Confidence for the scout mission before committing, exactly like any other action.

**Final State outcomes, mapped back onto this location:**

```text
Returned            -> bypass found; same effect as Find Bypass "Success"
Returned Injured     -> bypass found; scout arrives with an injury
                        (member state effect, not a location effect)
Returned Disturbed   -> bypass found, but modifier-watched escalates:
                        add faction memory (border-wardens, "scouts probing our border"),
                        change Fear +1
Overdue / Missing     -> no bypass revealed yet; this becomes its own
                        open thread the expedition must decide about
                        independently of the bridge
```

This is the intended cross-system hook from Section 8 (specialist contributions) meeting the scout system: sending a scout is a genuinely different strategy from Find Bypass, not a reskin of it — it risks a named character instead of expedition days, and it can go wrong in ways the main pipeline doesn't model (Overdue).

---

#### 25.4.5 Construct Temporary Passage

```text
Hard Requirements:   Operational State is Blocked
Soft Requirements:   Engineer present -> Reduce Cost, Reduce Risk (-15 to -20)
Cost:                1 full day, rope/timber equipment tag consumed
Duration:            1 day
Base Risk:           20  (Low/Moderate boundary, moderate-stakes default)
Situational:         + Unstable (moderate, +15)
Raw Risk Score:      35 (Moderate) without Engineer,
                     ~15-20 (Low/Moderate boundary) with Engineer
```

**Outcome Table** (`route-obstacle` + `action-construct-temporary-passage`, Moderate Band):

```text
Major Success    10   -> Operational State -> TemporarilyOpen
                          (safe for the rest of this expedition)
Success          45   -> Operational State -> RiskyPassage
Success With Cost 20  -> Operational State -> RiskyPassage,
                          equipment tag consumed twice (repair materials strained)
Partial Result    15  -> stays Blocked, equipment consumed, day lost
Failure            8  -> stays Blocked, equipment consumed, day lost,
                          Morale -1
Severe Failure     2  -> stays Blocked, a member is injured
                          (Recovery Check applies)
```

Repeat Policy: `OncePerState` for a Major Success/Success (no reason to rebuild what's already standing); `RepeatableWithCost` after a Failure or Partial Result.

---

#### 25.4.6 Attempt Crossing

This is the "just go for it" gamble. Its risk depends entirely on the current Operational State, which is exactly the kind of state-dependent behavior Section 5.6's state-transition model is built for — this is one action definition, not two.

**While Blocked:**

```text
Base Risk:      45  (High-stakes default)
Situational:     + Unstable (moderate, +15)
Raw Risk Score: 60 -> Band: High
```

**While RiskyPassage (after Construct Temporary Passage):**

```text
Base Risk:      20  (moderate-stakes default)
Situational:     + Unstable (moderate, +15)
Raw Risk Score: 35 -> Band: Moderate
```

**Outcome Table, High Band (Blocked state):**

```text
Major Success     5   -> crossed cleanly; a minor finding noticed
                          mid-crossing (add clue)
Success           25   -> crossed, minor time cost
Success With Cost 25   -> crossed, but 1 Supplies lost / minor injury
Partial Result    15   -> forced to turn back partway; day lost,
                          Operational State unchanged
Failure           20   -> failed to cross, forced retreat,
                          Supplies -1, Morale -1
Severe Failure    10   -> a member falls; Recovery Check applies
                          (see Section 4.6.1)
```

**Outcome Table, Moderate Band (RiskyPassage state):** same tier structure, weights shifted toward the safer end (`Major Success` 15, `Success` 45, `Success With Cost` 20, `Partial Result` 10, `Failure` 8, `Severe Failure` 2) — reflecting that the temporary passage genuinely reduced danger without pretending it removed it.

##### 25.4.6.1 Recovery Check on Severe Failure

If a member falls, the Recovery Check (risk/outcome doc, Section 9) resolves before the consequence locks in:

```text
Preserved            -> the member catches themselves or is caught by
                         another member; Morale -1, no injury
Partially Preserved   -> the member is injured but alive; standard
                         Injured state, treatable at camp
Lost                  -> the member is lost to the ravine below;
                         this creates a recovery objective for a later
                         expedition (a body, belongings, or evidence
                         may be found downstream in a future visit)
```

`Lost` never simply deletes the member from the game's memory — it creates the kind of recoverable thread the world document's persistence model expects (world doc: "failure should create history").

Repeat Policy for `Attempt Crossing`: `RepeatableWithCost` — the expedition can keep trying, but each attempt is its own independent gamble at full risk. There is no farming concern here because every attempt genuinely risks the outcome above; repetition is a player choice with real stakes, not an exploit.

---

#### 25.4.7 Rebuild Bridge (Multi-Day Project)

```text
Hard Requirements:   modifier-repairable present, Operational State is
                     Blocked or RiskyPassage, bridge material or valid
                     construction equipment tag
Soft Requirements:   Engineer present -> Reduce Cost (duration 3 -> 2 days),
                                          Reduce Risk (-15)
Duration:            3 workdays (2 with Engineer)
Daily Cost:          normal expedition Supply consumption
```

The expedition remains at the location for the duration (Section 10). Each project day resolves through the same shared Outcome Table structure used elsewhere in this document — no separate system is invented for "a day of construction work":

```text
Base Risk (per day):  25  (moderate-stakes default)
Situational:           + Unstable (moderate, +15)
Raw Risk Score:        40 (Moderate) without Engineer, ~25 with Engineer
```

**Outcome Table** (`route-obstacle` + `action-rebuild-bridge`, per project day, Moderate Band):

```text
Success            65   -> normal daily progress
Success With Cost  20   -> normal progress, but +1 Supplies consumed
                            beyond the daily rate (a tool breaks)
Partial Result      10   -> Setback: no progress this day, and if
                            modifier-watched is active, a chance to
                            also trigger the Faction Notices effect below
Failure              4   -> Setback, and the project loses 1 day of
                            already-banked progress (a partial collapse)
Severe Failure        1   -> a member is injured during construction
                            (Recovery Check applies)
```

**Faction Notices (conditional effect, only if `modifier-watched` is active):** on any day the daily roll lands on `Partial Result` or worse, there is an additional chance that Border Wardens observation escalates from passive to active — this fires the effect bundle: add faction memory (`border-wardens`, "outsiders rebuilding the old crossing"), change Anger +1 if the expedition never attempted contact with the Border Wardens beforehand, or change Trust +1 instead if a prior Contact Site interaction had already requested permission. This is the intended hook between this document and `factions_and_trade_concept.md` — it is included here as a demonstration of how the effect registry is meant to reach across systems, not as a new mechanic of its own.

**Completion Effects (once required progress is reached):**

```text
- Operational State -> Repaired
- edge traversal -> Open (permanently, for all future expeditions)
- route knowledge candidate created, added to the map
- persistent world history entry created (world doc 23A, Consequence Events)
- if Faction Notices fired during construction: the accumulated
  Trust/Anger change from those ticks persists as-is; there is no
  additional bonus or penalty simply for finishing
```

Repeat Policy: `OncePerLocation` — once Repaired, the project cannot be started again here.

##### 25.4.7.1 Destroyed as a Structural Outcome

`Destroyed` (the sixth Operational State) is reached only if `Failure` fires on a project day while the bridge is already at `RiskyPassage` from a prior temporary crossing attempt — representing the partially-repaired, still-unstable structure finally giving way. This is authored deliberately narrow: it should be rare, and it should never be reachable from a single unlucky roll starting from `Blocked` (matching the same "severity accumulates, doesn't spike" principle used for scout missions in the risk/outcome document, Section 10A.3).

---

#### 25.4.8 Mark / Leave

Both free, no risk, no cost beyond the implicit time of the interaction itself. `Mark` adds a player-authored map note; `Leave` ends the interaction without consequence.

---

### 25.5 Content Profile

```text
id: content-old-trade-road-bridge
visibleDescription: "A collapsed wooden bridge over a deep ravine.
                      One support beam is visibly cracked."
hiddenPurpose: "Deliberately destroyed by the Border Wardens to slow
                movement toward their inner territory."
clues:
  - clue-bridge-cracked-beam (fair warning toward Unstable)
  - clue-bridge-repairable-with-timber (requires Engineer + Assess Crossing)
possibleFindings:
  - minor finding during Major Success on Attempt Crossing
    ("an old coin wedged in the wreckage")
knowledgeRewards:
  - map knowledge on repair (via Rebuild Bridge completion)
  - route knowledge (via any successful crossing method)
factionLinks:
  - border-wardens (via modifier-watched)
journalText:
  - per-tier templates for Attempt Crossing and Rebuild Bridge outcomes
falseInterpretations:
  - a Scholar without direct inspection may initially misattribute the
    collapse to flooding rather than deliberate action; this is only
    corrected by discovering clue-bridge-repairable-with-timber or by
    a later faction interaction that reveals the truth
```

---

### 25.6 Instance Definition (JSON)

This uses the JSON Authoring Contract from Section 17. The concrete action set, costs, risk profiles and outcome tables are registry definitions; the instance only places this bridge into the world and declares its initial runtime state.

```json
{
  "documentType": "location-instances",
  "schemaVersion": 1,
  "items": [
    {
      "id": "loc-old-trade-road-bridge",
      "archetypeId": "route-obstacle",
      "variantId": "broken-bridge",
      "anchor": {
        "kind": "Edge",
        "hexes": [[18, 22], [19, 22]]
      },
      "modifierIds": [
        "modifier-repairable",
        "modifier-unstable",
        "modifier-watched"
      ],
      "factionIds": ["border-wardens"],
      "contentProfileId": "content-old-trade-road-bridge",
      "initialState": {
        "knowledge": "Unknown",
        "interaction": "Untouched",
        "operational": "Blocked",
        "presence": "Unknown"
      }
    }
  ]
}
```

No field here duplicates the authored definitions. The instance stores only anchor, active modifiers, faction link, content reference and initial state, exactly as prescribed in Section 3.2 (Definition Data Is Separate From Runtime State). Actions, costs, risk profiles and outcome tables are loaded from the definition registries described in Section 17.

---

### 25.7 Validation Against This Document's Acceptance Criteria

Checking this example against the relevant criteria from Section 23:

```text
5.  A broken bridge can block one edge between two hexes.
    -> Yes (Section 1, anchor).

6.  Repairing the bridge can use a persistent multi-day project and
    permanently reopen that edge.
    -> Yes (Section 4.7).

8.  Actions are derived from archetype, variant, modifiers and state
    rather than concrete variant switches.
    -> Yes -- every gate in Section 4 references a modifier or a state
       value, never a variant-ID check like `if variantId == "broken-bridge"`.

9.  Specialists can unlock, improve or de-risk actions through
    declarative contributions.
    -> Yes -- Engineer appears as a Reduce Cost / Reduce Risk contribution
       on three different actions (4.5, 4.6, 4.7) without any bespoke code.

10. Location effects update expedition, faction, knowledge, route and
    world systems through generic effect handlers.
    -> Yes -- Section 4.7's Faction Notices effect is the clearest example,
       reaching into `factions_and_trade_concept.md`'s Trust/Anger values
       through the same generic effect registry used for everything else.

11. Location changes survive return to base and later expeditions.
    -> Yes -- Repaired state and the opened edge persist (Section 4.7,
       Completion Effects).

13. Rewarding actions have repeat rules and cannot be farmed indefinitely.
    -> Yes -- every action in Section 4 declares an explicit repeat policy.
```

No gap was found while authoring this example, with one exception that has since been closed: `action-attempt-crossing`'s Operational-State-dependent risk (Section 25.4.6) required expressing "the same action has different Base Risk depending on current state." Section 9.4.2A now supports this explicitly through the `baseRiskByState` field on `RiskProfileDefinition` (Section 9.12), added specifically in response to this example.

---

## 26. Design Validation Log

This section records what changed in this document as a result of simulating all ten archetypes end to end (Trace Site, Investigation Site, Route Obstacle, Containment Site, Territorial Marker, Contact Site, Resource Site, Hazard Zone, Landmark Site, Dynamic Situation) against the framework in Sections 1-9. Every entry below is already reflected in the section it names; this log exists so the reasoning behind those sections isn't lost.

### 26.1 Fixes Folded Into Section 9 (Risk and Outcome Resolution)

1. **State-gated Risk Inputs** (Section 9.4.2A) — found while authoring the Broken Bridge example (Base Risk needed to vary by Operational State for `Attempt Crossing`), generalized while simulating the Marked Grave (a Situational Modifier's magnitude needed to depend on an earlier action's effect, not just a state channel).
2. **Scheduled consequences resolve once, apply later** (Section 9.8A) — found while simulating a Containment Site: releasing a hazard through `Force Open` needed an explicit rule for whether severity is fixed at commit time or re-rolled when the delayed effect fires. It's fixed at commit time, consistent with Section 20's versioning rule.
3. **Social risk uses the same pipeline** (Section 9.4B) — found while simulating a Contact Site: actions like Trade and Communicate aren't physically dangerous, but without an explicit bridge from Trust/Anger/Fear into Situational Modifiers and Mitigating Factors, an implementer would likely build a second "dialogue success" system, breaking Section 9.1's core decision.
4. **Ambient risk generalizes beyond scouts** (Section 9.10B) — found while simulating a Hazard Zone: danger from simply being present in an area over time is the same shape of problem as a scout's per-leg risk (Section 9.10A.2), just applied to the main expedition instead.
5. **Passive time-driven transitions are explicitly out of scope for Section 9** (Section 9.16) — found while simulating Resource Site (`Renewing -> Available`) and Dynamic Situation (`TimeLimited -> Expired`): neither involves resolving an action, so neither belongs in the risk pipeline. This boundary is now stated explicitly rather than left ambiguous.

### 26.2 Fixes Folded Into the Framework Directly

6. **Dynamic Content Reference Slots** (Section 13.1) — found while simulating a Trace Site: a recovery objective created by a `Lost` outcome (Section 9.9) needs to eventually reference a specific member's identity in a future Content Profile, which the previously fully-static Content Profile model had no mechanism for.
7. **Interaction Range** (Section 4.5) — found while simulating a Landmark Site: every action defined elsewhere in this document implicitly assumes adjacency, which breaks for a signal tower meant to be observed and used from several hexes away.
8. **Variant/Modifier Conflict Resolution** (Section 2.7) — not found through a single archetype simulation, but through the recurring question of what happens when a Variant and a Modifier (or two active Modifiers) disagree about the same action. Resolved with one governing principle (Conservative Resolution) and four concrete rules.

### 26.3 Confirmed, Not a New Finding

- Simulating Territorial Marker confirmed the need for Section 9.4B (Social Risk) rather than surfacing an independent problem — `FollowInstruction`/`CrossBoundary` face the same social-stakes-not-physical-danger issue as Contact Site's actions.
- A moving anchor for `Dynamic Situation` variants like a faction patrol was considered and is not a new gap — Path Anchors are already acknowledged as deferred (Section 4.4). Since Dynamic Situation is deferred past the Vertical Slice entirely (Section 21), this doesn't need resolving yet.
