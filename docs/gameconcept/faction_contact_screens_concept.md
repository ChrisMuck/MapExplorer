# Faction Contact Screens Concept

Source of truth for the concrete faction contact UI: the Encounter Card and the Contact Window, how they connect, how dialogue depth scales with representative access, and the full set of implementation rules and data shapes needed to build it.

This document extends `factions_and_trade_concept.md` (13F First Contact Progression, 13G Faction-Initiated Interaction, 13J Faction Memory, 13M Leader Conversations, 13N Team Composition and Faction Perception, 13P.2B Representatives, 13P.2C Leverage Objects, 13P.8 Interaction Menu, 13P.9/13P.10 Offers and Trade) and `ui_and_visual_communication_concept.md` (25A.4 Scout Report UI, 25A.5 Journal, 25A.7 Faction Notes, 25B.4 Faction Contact Presentation).

Every data shape below is written as an implementation-ready JSON schema so an AI coding agent can turn each section directly into types, tables and functions without needing additional interpretation.

**Terminology note.** This document uses **Representative Tier** (`repTier`) for how much a specific representative is willing to disclose (0–2, gates topics and dialogue depth). This is intentionally a different concept from the faction-level **Contact Status** (13P.2: Unknown → ... → Trusted), which 13D also refers to as "diplomatic access level." Contact Status describes the relationship with the faction as a whole. Representative Tier describes only the person currently standing in front of the player. A Trusted faction can still greet the expedition with a Tier 0 Watcher — see Section 3.4 for how the two interact.

---

## 1. Two-Layer Contact Model

Faction contact happens in two connected but distinct UI layers.

**Encounter Card** — a low-commitment interrupt. It asks one question: *does the expedition engage right now, or not?*

**Contact Window** — a full session with a representative, opened only after "Open contact." It asks: *how does the expedition engage?*

Design rule:

> The Card decides whether to talk. The Window decides how the conversation goes.

Every path through either screen must write something — a faction memory flag, a trust/anger/fear change, or at minimum a Faction Note entry (25A.7). There is no pure flavor option that leaves the world state untouched, because the player is always being read by the faction, even when choosing not to engage.

---

## 2. The Encounter Card

### 2.1 Purpose

The Card surfaces a faction moment without forcing the player into a dialogue tree. It is the concrete implementation of the early stages in 13F (Warning, First Contact) and of faction-initiated interactions in 13G.

### 2.2 Triggers

- expedition enters a hex owned or influenced by a faction (13P.2A)
- a world-phase faction action resolves near the expedition (patrol, renewed markers, messenger sent)
- a scout report surfaces a faction contact opportunity
- the expedition camps, lingers, or repeats an action a faction is watching
- a mandate or leverage-object condition is met (13P.2C)

### 2.3 Anatomy (matches screenshot 1)

- faction tag (e.g. "Coastal People")
- title — short, situational, in-world phrasing ("Coastal watchers"), never a system label like "Faction Event"
- body text — 1–3 sentences, written with the same interpretive uncertainty as scout reports (13I): what was seen, not what it means
- optional composition-warning line appended below the body (see 2.6)
- 2–4 action buttons, one visually primary

### 2.4 Standard Action Set

- **Open contact** (primary) → opens the Contact Window
- **Continue carefully** → the expedition moves on without stopping; a real alternate path, not a dismissal
- **Archive observation** → logs the moment without approaching; the safe default
- optional situational 4th action (e.g. "Send scout instead," "Withdraw," "Leave a gift and go") when the encounter specifically supports it

> Note: this generic action set covers ambient "may we talk" encounters only. Faction-initiated requests, demands, warnings and offers (13G) use their own Card intents and action sets — see Section 2A.

### 2.5 Consequences of Non-Engagement Choices

Both non-contact options are designed as real strategic alternatives, each with its own effect. Neither is a "skip."

**Archive observation**

- zero risk: cannot offend, cannot be misled, cannot trigger a taboo
- always writes a Faction Note and Journal entry
- contact status still advances at most one step (e.g. Observed → Warned), because the moment happened whether or not the expedition engaged
- typically flat on Trust, and does not reduce Fear/Anger the way real contact could
- correct choice when the right specialist is missing, the faction is hostile-leaning, or the player wants information without exposure

**Continue carefully**

- expedition proceeds past/through without stopping, at a small time or route cost
- effect depends on the faction's values and current state, not a fixed number:

| Faction disposition | Effect of continuing carefully |
|---|---|
| Territorial / Border Wardens type | Small Trust for restraint (not lingering, not investigating), but no relationship depth gained |
| Fearful / Traumatized | Small Fear reduction (non-threatening behavior observed), Trust unchanged |
| Curious | Small missed-opportunity cost — they wanted an approach and didn't get one; slight Curiosity/interest decay |
| Hostile / Isolationist | Often the only safe option; avoids escalation, no relationship gained |

**Open contact**

- full engagement, opens the Contact Window
- carries the real risk and reward: trust/anger/fear swings, offers, leverage-object progress, leader-access progress

### 2.6 Team Composition Warning

Before the Card is generated, the current expedition roster is checked against the faction's perception rules (13N). If the composition crosses a defined threshold for this faction (too many soldiers for a peaceful/fearful faction, no scholar present for a knowledge faction, etc.), one warning line from a small per-faction pool is appended to the Card body. This does not block "Open contact" — it only informs the decision before it's made.

```json
{
  "id": "composition-warning-coastal-many-soldiers",
  "factionId": "coastal-people",
  "condition": { "roleCount": { "soldier": { "min": 3 } } },
  "warningLine": "Your soldiers make the villagers uneasy.",
  "effectIfIgnored": { "fearOnOpenContact": 1 }
}
```

Implementation rule: the composition check runs once, at Card-generation time. It is not re-evaluated live — the roster is assumed fixed for the duration of a single encounter in the MVP.

### 2.7 Card Action Comparison

| Action | Risk | Typical effect | Best used when |
|---|---|---|---|
| Open contact | Highest | Trust/Anger/Fear can move meaningfully in either direction | Team composition fits, faction is at least Cautious, player wants progress |
| Continue carefully | Low | Small, faction-specific nudge | Faction is Territorial/Fearful/Hostile, or team isn't ready |
| Archive observation | None | Neutral log, minimal status tick | No good specialist present, faction is Hostile, or player wants to scout before committing |

---

## 2A. Faction-Initiated Situation Cards

Section 2's generic action set (Open contact / Continue carefully / Archive observation) fits ambient "may we talk" moments, but not the richer situations 13G describes: a village asking for medicine, a faction demanding an artifact back, a faction offering a guide, a faction asking why past expeditions entered its land. This section gives those situations their own Card intents, actions, and a shared consequence system, while reusing as much of the already-built machinery (Contact Window routing, leverage objects, escalation patterns from 8B, reliability from 8A) as possible rather than inventing a parallel system.

### 2A.1 Card Intents

Every Encounter Card carries an `intent`. `observation` is the existing generic type (Section 2). This section adds four more:

- **warning** — the faction proactively warns the expedition (e.g. "do not enter this forest," "a ruin we refuse to enter")
- **request** — the faction asks for help (medicine, a bridge repair, mediation)
- **demand** — the faction wants something surrendered or done (an artifact returned, a body returned, a map destroyed or hidden)
- **offer** — the faction proactively offers something (a guide, access, information)

Two 13G examples deliberately do **not** get their own intent, to keep the taxonomy small:

- "A faction tries to redirect the expedition toward a rival's territory" is a `request` or `offer` authored with `reliability: "variable"` (8A) — it is a trust question, not a new Card shape.
- "A faction returns a captured scout but keeps the scout's notes" is a resolution outcome of the existing Captured Member system (8B), not a new Card type.

### 2A.2 Resolution Rule: Card Actions vs. Contact Window

Per your direction, any action that involves a real exchange — accepting help, negotiating a demand, accepting or questioning an offer — opens the Contact Window, routed directly to a pre-selected topic rather than the general topic menu. Only a flat refusal/decline stays a card-level action, resolving immediately without a conversation, consistent with how Continue Carefully and Archive Observation already work in Section 2.

This keeps a hard line between "this needs a person to actually respond to" and "this is just a no," instead of blurring every action into the same generic template.

### 2A.3 Action Sets by Intent

**Warning**
- *Heed Warning* — resolves on the card; small faction-specific trust/fear effect (reuses the 2.5 disposition table), expedition avoids the flagged area/action
- *Ask Why* — opens Contact Window at the general topic menu
- *Ignore Warning* — resolves on the card; faction-specific anger/fear effect (2.5 table)

**Request**
- *Offer Help* — opens Contact Window, routed to a highlighted "fulfill request" topic. If the required specialist or resource isn't present, the action still appears but is shown locked with a reason ("Requires Medic"), reusing the locked-offer pattern from 3.3
- *Decline* — resolves on the card; enters the escalation ladder (2A.4)
- *Ask for Time* — resolves on the card; the request stays open as a `SituationRecord`, drift continues per 2A.4

**Demand**
- *Negotiate* — opens Contact Window, routed to a highlighted "demand" topic, structurally identical to the leverage-object hook (3.2.1) and the captive negotiation (8B.3): offer a leverage object, Knowledge, an apology, or a promise
- *Refuse* — resolves on the card; applies an immediate escalation step (2A.4)

**Offer**
- *Accept* — opens Contact Window, routed to a topic that finalizes the offer
- *Decline* — resolves on the card
- *Ask About It First* — opens Contact Window at the general topic menu

### 2A.4 The Escalation Ladder

This answers both open questions about time pressure and repeated refusal with one mechanism, rather than two separate systems: **unaddressed drift and active refusal move the same ladder.** Refusing moves it immediately and further; ignoring moves it slowly and only sometimes. This directly reuses 8B's slow, faction-dependent drift pattern (world-phase interval checks, not a daily countdown) instead of introducing a hard deadline — consistent with the project's existing "no exact numbers, no full simulation" approach, and avoids punishing a player who is simply busy elsewhere on the map with a harsh timer.

```json
{
  "situationId": "situation-demand-return-artifact",
  "factionId": "border-wardens",
  "intent": "demand",
  "escalationLadder": ["asked", "annoyed", "insistent", "hostile-over-this"],
  "currentRung": "asked",
  "escalationChancePerCheck": 0.2,
  "checkIntervalDays": 4,
  "relatedLeverageObjectId": "border-shrine-stone",
  "createdOnDay": 20,
  "resolved": false
}
```

Rules:

- At each world-phase check (same interval-based mechanism as 8B's `checkIntervalDays`), if the situation is still unresolved, roll `escalationChancePerCheck`. On success, `currentRung` advances by one step. This is the "ignoring it" path.
- An active *Refuse* advances `currentRung` by one step immediately, plus applies its own direct penalty (trust hit, memory flag) — refusing is faster than ignoring, but not worse per step. A player who wants to actively manage the relationship by saying a clear "no" is not punished harder than one who lets it drift.
- Reaching the final rung should have an authored, faction-specific severe consequence: the faction's disposition shifts specifically regarding this topic, a related offer or topic becomes permanently blocked, or a Warning-intent Card is triggered as a territorial response.
- Successfully resolving the situation through the Contact Window (Comply/Negotiate reaching a real outcome) removes the `SituationRecord` entirely and may partially reverse ladder effects — a late but genuine resolution should read as better than a permanently ignored one, not equivalent to it.
- *Ask for Time* pauses neither drift nor the clock — it resolves the card interaction but the `SituationRecord` keeps ticking at its normal pace. This makes "ask for time" an honest stalling tactic, not a free pass.

### 2A.5 Demand Cards as the Faction-Initiated Half of Leverage Objects

A `demand` Card is not a new desire system — it is the faction-initiated half of 13P.2C's existing leverage-object desires. Previously, a faction's desire only surfaced when the player happened to have the item and opened a Contact Window (3.2.1's leverage hook). A Demand Card flips this: the faction proactively tells the player what it wants, whether or not the expedition has it yet, turning `relatedLeverageObjectId` into a map objective the moment the Card appears — consistent with 13P.2C's own rule that leverage objects "should unlock dialogue options... and create map goals that are not simply 'go here.'"

### 2A.6 Data Shapes

```json
{
  "id": "card-border-wardens-demand-artifact",
  "factionId": "border-wardens",
  "intent": "demand",
  "title": "The wardens want their relic back",
  "body": "An old commander studies your packs. \"That stone belongs to the ridge shrine. Return it, or we remember this.\"",
  "situationId": "situation-demand-return-artifact",
  "actions": [
    { "id": "negotiate", "label": "Negotiate", "opensWindow": true, "routeToTopic": "demand-return-artifact-topic" },
    { "id": "refuse", "label": "Refuse", "endsCard": true, "effects": { "advancesSituationRung": 1, "trust": -3, "memoryFlag": "refused-artifact-demand" } }
  ]
}
```

---

## 3. The Contact Window

The window has three panels, matching screenshot 2.

### 3.1 Left Panel — Representative

- portrait or placeholder (25B.2/25B.4); silhouette or obscured face if identity is unknown, per 25B.8
- name, or a functional role-name when the true name is unknown ("River Messenger") — consistent with 13P.2B's masked/hidden speaker rule
- role · faction line
- **Haltung (Stance)** — a qualitative label only, never a number: Open, Curious, Cautious, Suspicious, Hostile, Fearful, etc. Derived from trust/anger/fear via threshold bands
- one italic flavor line establishing the representative as a person, not a stat block

**Live Haltung updates.** The Haltung label is not fixed for the duration of the encounter. It recomputes from current trust/anger/fear after every dialogue option that carries an `effects` payload (4.1) and re-renders immediately in the left panel — the player sees the label change inline (e.g. "Open → Cautious") within the same session, not only on the next visit. The threshold bands stay qualitative only; the only requirement is that the mapping function runs after every effect application, not just on window open.

Representative data shape:

```json
{
  "id": "rep-river-messenger",
  "factionId": "coastal-people",
  "role": "messenger",
  "archetypeTag": "hospitable",
  "repTier": 1,
  "patience": 4,
  "stanceThresholds": {
    "open": 40, "curious": 20, "cautious": 0, "suspicious": -20, "hostile": -40
  }
}
```

`patience` is the numeric budget behind the qualitative cue described in Section 5; `repTier` drives both topic gating (Section 4) and dialogue depth.

### 3.2 Center Panel — This Encounter

- header: "DIESE BEGEGNUNG · TAG X" (ties the encounter to the expedition day, linking it to the Journal, 25A.5)
- quote bubble showing the representative's current line (root line, recap line, or current branch node)
- topic list — the response options, filtered by what this representative is allowed/willing to discuss (Section 4)

**Standard recurring topics** (base set, subset shown per representative):

- ask about the situation ("Nach der Lage fragen")
- ask about a location/landmark
- ask about a missing scout
- acquire something / trade ("Etwas erwerben wollen")
- offer help (role-gated: medic, engineer, scholar)
- apologize for a violation
- show a discovered symbol or object
- request passage
- say goodbye ("Sich verabschieden")

#### 3.2.1 Special Item Tracking and Leverage Object Hooks

Leverage objects (13P.2C) need something the expedition system does not currently have: tracking for unique, named items. The existing Capacity resource (18M) only tracks numeric cargo (Supplies, Medicine, tools, trade goods) — it has no concept of "the grave token," specifically. The following is the minimal ledger needed to close that gap, deliberately kept small per 13P.2C's own rule ("a small number of authored leverage objects, not a large inventory system").

**Special Item data shape:**

```json
{
  "itemId": "grave-token-01",
  "displayName": "Grabtoken",
  "category": "LeverageObject",
  "currentLocation": "expedition-inventory",
  "sourceLocationId": "marked-grave-04",
  "interestedFactionIds": ["border-wardens"],
  "acquiredOnDay": 9,
  "lastKnownHexId": null
}
```

`currentLocation` is one of: `expedition-inventory` (carried, available for hooks below), `base-storage` (returned, available to equip on a future expedition — same pattern as choosing team composition at prep time), `lost-in-field` (see loss rule below), `held-by-faction` (a faction other than the intended recipient picked it up), `consumed` (offered and spent).

**Capacity rule.** Special items do not consume Capacity. They are tracked in a separate list on the expedition state, parallel to but independent from Supplies/Medicine/Tools. There is no reason to leave one behind voluntarily — the only way to lose one is a forced event, not a loadout decision.

**Loss and recovery rule.** A special item can be lost during a Forced Retreat, Partial Return, or a conflict resolution that involves surrendering equipment (23B). When this happens, `currentLocation` becomes `lost-in-field` and `lastKnownHexId` is set to the expedition's position at the time — the item is never deleted from game state. It re-enters play as a discoverable Finding at or near that hex, reusing the existing Finding schema (`sourceTags: ["lost-expedition-item"]`), so a later expedition or scout can recover it exactly the way 15A's Recovery list already covers "faction-held belongings" and "abandoned findings." If a faction reaches it first, `currentLocation` becomes `held-by-faction` instead — which can turn into its own negotiation topic (the item is now something a *different* faction holds), layering naturally with 8B's captured-member negotiation if the timing lines up.

**Leverage object hook.** If any special item with `currentLocation: "expedition-inventory"` appears on a faction's leverage-object list, the topic list gains a highlighted entry above the standard topics, computed dynamically rather than authored per encounter:

```json
{
  "id": "leverage-offer-grave-token",
  "factionId": "border-wardens",
  "requiresItemId": "grave-token-01",
  "minRepTier": 2,
  "label": "Das Grabtoken anbieten",
  "highlighted": true,
  "nextNode": "leverage-grave-token-response",
  "onceConsumed": true
}
```

Rule: this entry is computed at window-open time by filtering the expedition's special items for `currentLocation === "expedition-inventory"` and intersecting with `faction.leverageObjectIds`. It disappears once the item is consumed (`onceConsumed: true`, which also sets `currentLocation: "consumed"`) or if the representative's `repTier` is below the leverage object's `minRepTier` — some leverage objects only matter to an Elder, not a Watcher, and should not appear as an option the Watcher has no authority to act on.

#### 3.2.2 Repeat Contact Recap

Before the root line is chosen, the system checks whether this player has already discussed topics with this representative (or, for unnamed/rotating representatives, with this faction at this tier), using the faction memory store (13J). If prior topics exist, the root line is swapped for a recap variant referencing the most recently discussed topic instead of repeating the first-contact line:

```json
{
  "condition": "hasDiscussed(topicId: 'ask-lage', factionId: 'coastal-people')",
  "recapLine": "You already asked about the ford. Little has changed since."
}
```

Data requirement: every resolved topic writes a `discussedTopics: [topicId]` entry to the faction/representative memory record, keyed by faction id (or representative id if uniquely named). This extends the memory flag system already defined in 13J rather than introducing a parallel one.

### 3.3 Right Panel — Offers (Angebote)

- **Wissen** (Knowledge) balance shown at the top
- offer cards: name, short description, cost, buy button
- locked offers show the unlock requirement in place of the buy button ("Requires river chart" / "Gesperrt") instead of hiding the offer entirely
- pricing and access follow 13P.9/13P.10 exactly: `Final Cost = Base Cost × Relationship Factor`, gated by minimum relationship tier, trust/anger thresholds, and memory flags

This panel is a direct UI binding of the already-defined trade rules — no new mechanic needed here, just the visual home for it.

### 3.4 Contact Status vs. Representative Tier

Two independent gates apply to everything in the Contact Window, and both must pass:

1. **Contact Status** (13P.2, faction-wide): Unknown → Traces Discovered → Observed → Warned → First Contact → Audience Possible → Trusted → Hostile. Governs whether contact is possible at all, and which relationship-tier offers exist in principle (13P.9/13P.10).
2. **Representative Tier** (this document, per-representative): Tier 0–2. Governs what the specific person in front of the player is personally willing to discuss or act on, regardless of how the faction as a whole feels.

A faction can be globally Trusted while the only available representative is a Tier 0 Watcher — trusted factions still post guards. The reverse also holds: a faction still at First Contact might send an unusually forthcoming Tier 1 messenger.

**Offer gating rule.** An offer is visible and purchasable only if both gates pass:

`offer.minimumRelationshipTier <= faction.relationshipTier` (13P.9/13P.10) **and** `offer.minRepTier <= currentRepresentative.repTier` (this document)

Most offers should leave `minRepTier` unset (defaults to 0), meaning any representative able to trade at all can offer them — this covers baseline trade like "Knowledge for Supplies." Only sensitive offers (secret paths, faction taboos, leader access, information about sealed places — the "Trusted" tier examples in 13P.10) should carry `minRepTier: 2`, since those require both a trusting faction and someone senior enough to actually grant them.

```json
{
  "id": "offer-border-warden-secret-path",
  "factionId": "border-wardens",
  "minimumRelationshipTier": "Trusted",
  "minRepTier": 2,
  "baseKnowledgeCost": 20,
  "resultText": "The elder marks a hidden path behind the fallen cedar."
}
```

If the relationship tier is met but no sufficiently senior representative is present, the offer is listed as locked with a specific reason ("Requires audience with an elder") rather than hidden — reusing the locked-offer pattern from 3.3. This gives the player a concrete next step (find or earn access to a higher-tier representative) instead of an unexplained absence.

### 3.5 Representative Tier Growth

Representative Tier is not purely fixed by role. A specific, identifiable representative (one with a stable `id` in the faction's representative roster) can grow more forthcoming through repeated positive contact with the expedition. Anonymous or freshly-generated encounter instances (a Card that spawns "a border scout" without a persistent identity) do not grow — persistence has to be earned by the representative existing as a named individual in the first place, which rewards factions and authors that give representatives real identities rather than generic role stand-ins.

Each role defines a `baseTier` and a `maxTier`:

```json
[
  { "role": "watcher",   "baseTier": 0, "maxTier": 1 },
  { "role": "messenger", "baseTier": 1, "maxTier": 2 },
  { "role": "elder",     "baseTier": 2, "maxTier": 2 }
]
```

Growth tracking, stored per representative:

```json
{
  "representativeId": "rep-river-messenger",
  "currentTier": 1,
  "positiveContactCount": 2,
  "negativeContactCount": 0,
  "tierUpThreshold": 3,
  "tierDownThreshold": 2
}
```

Rules:

- After an encounter ends cleanly (`farewell` or `withdraw`) with a non-negative net trust effect, increment `positiveContactCount`.
- After an encounter ends with a net negative trust effect, a broken promise, or a taboo violation attributed to this representative's faction, increment `negativeContactCount` and halve `positiveContactCount` — a single bad encounter sets progress back, but does not erase it entirely.
- When `positiveContactCount >= tierUpThreshold` and `currentTier < maxTier`, increase `currentTier` by 1, reset `positiveContactCount` to 0, and write a Faction Note ("The messenger speaks more freely now") plus a memory flag.
- When `negativeContactCount >= tierDownThreshold` and `currentTier > baseTier`, decrease `currentTier` by 1 and reset `negativeContactCount` to 0.
- Growth never exceeds the role's `maxTier`. Reaching a true Tier 2 Elder/Leader audience where none previously existed still requires the harder 13E.1 access conditions (gift, proof of strength, solved problem) — tier growth makes an *existing* representative more open, it does not conjure a leader who wasn't part of the roster.

This is what Section 3.2.2's Repeat Contact Recap should draw on for its most rewarding variant: a recap line that specifically marks a tier-up moment ("You've spoken enough times that he no longer holds back.") rather than only restating an old topic.

### 3.6 Personal Opinion Modifier

Trust, Anger and Fear remain faction-wide values (13P.3) — the game does not track a full second set of these three values per representative. Instead, each representative carries one lightweight scalar:

```json
{ "id": "rep-river-messenger", "personalOpinion": 6 }
```

`personalOpinion` is bounded (suggested MVP range: -30 to +30) and folds into this representative's Haltung display only, on top of the faction-wide values: `effectiveStance = factionTrust - factionAnger - (factionFear * 0.5) + personalOpinion` (weights are a placeholder for playtesting, matching the approach already used for relationship price factors in 13P.10).

Split rule for dialogue effects: when a dialogue option resolves with a `trust` effect, the value is split — the majority (suggested 70%) applies to the faction-wide Trust value (shared, affects all representatives and offers), and the remainder (30%) applies only to this representative's `personalOpinion`. This produces the intended feel without duplicating the full state model: talking respectfully to one guard mostly helps standing with the whole faction, but a little of it sticks specifically to that guard — explaining why "this particular watcher remembers you fondly" even when the faction's overall Trust is only Neutral.

`personalOpinion` also feeds Section 3.5's growth thresholds: a higher personal opinion should reduce `tierUpThreshold` slightly (or increase the share of a trust effect counted toward `positiveContactCount`), so a representative who has been treated well individually grows into their `maxTier` faster than the faction average would predict.

This keeps the system at a single extra number per representative rather than a parallel Trust/Anger/Fear triple. Full per-representative Trust/Anger/Fear (needed for a representative to actively disagree with their own faction's stated position, per 13K) remains a later-expansion option, not an MVP requirement.

### 3.7 World Generation Output: Representative Rosters

13E and 13L describe leadership structures and archetypes qualitatively, but nothing in the wider concept specifies how faction generation actually produces the concrete `FactionRepresentative` objects (3.1) the Contact Window depends on. Without this, every future faction would need its roster hand-authored one representative at a time.

Rule: roster generation is a required output of faction generation, not an afterthought. For every generated faction, the generator produces:

**1. A leadership structure tag** (13E: single chief, council of elders, hidden leader, etc.) — already implied by faction generation, now made a concrete enum the roster generator reads.

**2. A small set of role slots**, derived from the leadership structure tag via a lookup table:

```json
{
  "single-chief": [
    { "role": "elder", "count": 1, "baseTier": 2, "maxTier": 2 },
    { "role": "messenger", "count": 1, "baseTier": 1, "maxTier": 2 },
    { "role": "watcher", "count": 2, "baseTier": 0, "maxTier": 1 }
  ],
  "council-of-elders": [
    { "role": "elder", "count": 3, "baseTier": 2, "maxTier": 2 },
    { "role": "trader", "count": 1, "baseTier": 1, "maxTier": 1 },
    { "role": "watcher", "count": 2, "baseTier": 0, "maxTier": 1 }
  ],
  "hidden-leader": [
    { "role": "masked-speaker", "count": 1, "baseTier": 1, "maxTier": 1 },
    { "role": "watcher", "count": 2, "baseTier": 0, "maxTier": 0 }
  ]
}
```

**3. Instantiation.** Each role slot becomes one or more `FactionRepresentative` objects with a stable `id`, a default `archetypeTag`, default `patience` and `stanceThresholds` from a small set of authored presets, and `personalOpinion: 0`.

**Archetype assignment.** Each representative's `archetypeTag` defaults to the faction's primary archetype (13L). For factions tagged Fragmented, or wherever the generator rolls internal tension, individual representatives may instead receive a secondary or conflicting archetype from the faction's combined archetype list. This is the concrete data-level hook for 13K's "one village asks for help against official orders" or "a border commander acts more aggressively than the leader intends" — it falls directly out of two representatives from the same faction having different `archetypeTag` values, with no new mechanic required.

**Guaranteed entry point rule.** Every generated faction must produce at least one role slot with `baseTier` 0 or 1 — this guarantees the "some possible contact path, even if hostile" rule from 13E is always satisfiable by the roster itself. A true Tier 2 Elder/Leader slot is optional and may be entirely absent at generation time for isolationist/hidden factions (13P.6). For those factions, leader-equivalent interaction happens outside the normal roster entirely, through the indirect means already described in 13P.6 (masked messenger, voice from concealment, warning left at camp) — not through a `FactionRepresentative` with `repTier: 2`.

**Determinism.** Roster generation is deterministic for a given world seed and faction id, matching the pattern already used for finding generation (knowledge_base_and_analysis_concept.md).

**MVP note.** The three MVP factions (Coastal People, Border Wardens, Hidden Ones) are hand-authored, not generated — but their rosters should be written directly in this output shape (role slot table plus instantiated `FactionRepresentative` objects), so they already validate the shape a future generator would need to produce. This follows the same precedent as 13P.2C's leverage objects: author the MVP content in the target data shape, build the generator later.

---

## 4. Dialogue Depth by Representative Tier

**Representative Tier 0 — Watcher / Border Guard** (low trust, first-contact-stage reps)
Topic click → one line of response → back to menu immediately. No follow-up.

**Representative Tier 1 — Messenger / Trader / Guide** (Cautious to Neutral relationship)
Topic click → line → one follow-up choice (2 options) → resolution line → back to menu. Roughly 2–3 dialogue nodes deep.

**Representative Tier 2 — Elder / Leader / trusted Audience** (13M leader conversations, Trusted tier or audience unlocked)
Topic click → line → up to two sequential follow-up choices → resolution → back to menu. This is where leverage-object hooks, apologies, and demands live.

### 4.1 Dialogue Node Data Shape

```json
{
  "id": "topic-ask-situation-coastal-messenger",
  "factionId": "coastal-people",
  "representativeRole": "messenger",
  "minRepTier": 1,
  "rootLine": "You have come far from your shore camp. We can trade a little, but we will not speak for every village.",
  "recapLine": null,
  "options": [
    {
      "id": "ask-lage",
      "label": "Nach der Lage fragen",
      "nextNode": "topic-ask-situation-followup",
      "effects": { "trust": 0, "memoryFlags": [] }
    },
    {
      "id": "erwerben",
      "label": "Etwas erwerben wollen",
      "opensPanel": "offers"
    }
  ],
  "disengageOption": {
    "id": "verabschieden",
    "label": "Sich verabschieden",
    "endsEncounter": true,
    "effects": { "trust": 1 }
  }
}
```

`nextNode` points to a follow-up node only if the representative's `repTier` supports it; Tier 0 representatives simply omit `nextNode` and resolve inline.

### 4.2 Always-Available Disengage

Regardless of representative tier or current branch depth, every dialogue node includes a disengage option that is never nested and never removed by branch logic. It is rendered in the same position (bottom of the topic list) on every node, root or branch, sourced from one global template rather than authored per node:

```json
{
  "disengageOptionTemplate": {
    "id": "withdraw",
    "label": "Zurückziehen",
    "alwaysVisible": true,
    "endsEncounter": true,
    "endReason": "withdraw",
    "effects": { "trust": 0 }
  }
}
```

Implementation rule: the dialogue renderer appends `disengageOptionTemplate` to every node's option list at render time — it must not be possible for an authored node to omit it. This guarantees the player is never more than one click from leaving, independent of how deep a Representative Tier 2 branch goes.

### 4.3 Flavor Text Separated From Mechanics

Mechanical options (which topics exist, what they cost, what they unlock) are authored once per faction/role/tier, as in 4.1. Presentation lines (the actual sentence spoken) are pulled from a separate pool keyed by the representative's `archetypeTag` (13L), so the same mechanical topic can sound different across factions without duplicating logic:

```json
{
  "topicId": "ask-lage",
  "linePool": {
    "hospitable": "Of course, ask away. We have little to hide.",
    "traumatized": "Why do you want to know? ...Fine. Ask.",
    "warrior-society": "Speak plainly. We have no patience for riddles."
  },
  "fallback": "hospitable"
}
```

Resolution rule: `topicId + representative.archetypeTag → line`, falling back to `fallback` if no archetype-specific line exists. Writers add lines to a pool; engineers never touch topic logic to add faction flavor.

---

## 5. Replacing "Feld 04/15"

The current corner indicator shows the expedition's hex position, which carries no meaning inside a conversation screen. Replacement, in order of recommendation:

**A. Contact Progress Tracker (recommended, permanent).** Replace the fraction with the 13F stage pips for this faction: `Unknown → Observed → Warned → First Contact → Limited Exchange → Audience → Trust`, current stage highlighted. Diegetic, always meaningful, reuses an existing system.

**B. Representative Patience (qualitative, contextual).** For Representative Tier 0–1 only, a short non-numeric cue near the portrait ("The messenger glances toward the trees") signals the conversation won't last forever. Driven by the `patience` field defined in 3.1.

**C. Encounter Exchange Counter (mechanism, not display).** `patience` decrements per exchange under the hood; only ever surfaced through B's qualitative text, never as a bare "4/15."

**D. Expedition Day** — already shown as "TAG 1" in the center header; do not duplicate it in the corner.

Implementation: show A permanently; render B's text whenever `patience` drops below a per-tier threshold (e.g. `patience <= 2` for Representative Tier 0–1 only; Tier 2 representatives don't display a patience cue at all).

---

## 6. Consequence & Memory Writing Summary

| Source | Writes to |
|---|---|
| Card: Open contact | opens Window; no faction-state write by itself |
| Card: Continue carefully | Trust/Fear nudge (2.5 table), Faction Note, contact status +0/+1 |
| Card: Archive observation | Faction Note only, contact status +0/+1, no Trust/Anger/Fear change |
| Window: topic resolution | Trust/Anger/Fear per node `effects`, optional memory flag, optional map marker/report link, `discussedTopics` update |
| Window: leverage object offered | Trust/Anger/Fear per offer definition, inventory removal if `onceConsumed`, memory flag |
| Window: offer purchase | Knowledge spent, Supplies/item/info gained, optional Trust/Anger/Fear per offer definition |
| Window: clean exit ("Sich verabschieden" / "Zurückziehen") | small Trust bonus, `endReason` recorded, Recap Card shown (Section 8) |
| Window: abrupt close | small Trust/Curiosity penalty, `unfinishedConversation: true` flag, Recap Card still shown |

### 6.1 Clean Exit vs. Abrupt Close

Two distinct end-states for a Contact Window session:

- **Clean exit** — player selects "Sich verabschieden" or the always-available disengage option (4.2). Writes the small Trust bonus and proceeds to the recap card (Section 8) normally.
- **Abrupt close** — player closes the window via a UI close action (X button, ESC, navigating away) without selecting an exit option. This is not free: it writes an `unfinishedConversation: true` flag and applies a small Trust/Curiosity penalty — smaller than any negative dialogue choice would cause, since this is about rudeness, not betrayal.

Implementation requirement: the UI close action must route through the same `endEncounter(reason)` function as the dialogue-driven endings, with `reason: "farewell" | "withdraw" | "abrupt"`. All three paths write a memory/journal entry and pass through Section 8's recap — there is no code path that closes the window without writing state.

---

## 7. Cross-Faction Memory & Shared Knowledge

Some memory flags written during a contact session should not be scoped to a single faction, but to a global expedition-reputation store that other factions' dialogue systems can query.

Rule: a subset of memory flags are tagged `visibility: "shared"` at authoring time (most remain `"local"`, visible only to the faction that observed them). Shared flags become queryable by any faction's dialogue-node `condition` field, with a propagation delay to keep it plausible — a rival faction hears about the expedition through trade contacts, scouts or rumor, not telepathically:

```json
{
  "flagId": "spoke-with-coastal-people",
  "visibility": "shared",
  "propagationDelayDays": 2,
  "sourceFactionId": "coastal-people"
}
```

A Border Warden dialogue node can then reference it:

```json
{
  "condition": "hasFlag('spoke-with-coastal-people')",
  "line": "The coast people already speak of you. That is not nothing, here."
}
```

MVP scope: implement with a small authored list (2–4 shared flags total, e.g. "helped a village," "opened a grave," "carries a leverage object openly") rather than a general propagation simulation — matching the project's existing pattern of starting with a small authored set before generalizing (13P.2C).

---

## 7A. Memory Flag → Narrative Text Convention

13J describes faction memory as prose ("A previous expedition opened our grave"), while every structured schema in this document references memory only as an opaque flag ID (`MemoryFlag` above, `requiredMemoryFlags` in 13P.9, `discussedTopics` in 3.2.2). Nothing connects the two — a flag ID by itself cannot become the qualitative Faction Note text 25A.7 requires, the Journal line 25A.5 requires, or a dialogue hint a representative might reference.

**General rule:** any data object that changes world state and needs to describe that change to the player carries its own authored text template(s). There is no separate "state-to-text" translator layer that guesses how to phrase a raw flag ID. This document already follows this instinctively in several places without naming it (`resultText` on 13P.9 offers, `warningLine` on CompositionWarningRule 2.6, `journalLine` on the Contact Recap 8). This section makes it explicit and closes the one place that was still missing it: `MemoryFlag` itself.

Extended `MemoryFlag` shape:

```json
{
  "flagId": "opened-marked-grave",
  "visibility": "local",
  "sourceFactionId": "border-wardens",
  "factionNoteTemplates": [
    "The Border Wardens have not forgotten the grave that was opened.",
    "Warning markers near the old grave have been renewed since the incident."
  ],
  "journalTemplate": "Day {day}: The expedition opened a marked grave near {locationName}.",
  "dialogueHintTemplate": "You are the ones who opened the grave."
}
```

- `factionNoteTemplates` is a pool, not a single string — reusing the LinePool pattern from 4.3, so a persistent memory doesn't read identically every time the player checks Faction Notes. One entry is picked whenever the note is displayed.
- `journalTemplate` supports simple placeholders (`{day}`, `{locationName}`, `{memberName}`) resolved from the event context that set the flag, written once into the Journal (25A.5) the moment the flag is set — not regenerated later.
- `dialogueHintTemplate` is a short fragment a representative can fold into a dialogue line when the flag is active and relevant — this is what powers the `condition`-based lines already sketched in 3.2.2 and Section 7, so those sections reference `dialogueHintTemplate` directly instead of hand-writing a new sentence per faction that reacts to the same flag.

This closes the loop: 13J's prose examples are not a separate authoring style from the structured flag system — they are literally the `factionNoteTemplates` pool for the corresponding flag. A flag authored without template text is incomplete, the same way an offer without `resultText` would be.

---

## 8. Contact Recap Card

When a Contact Window session ends (any `endReason` from 6.1), show a short recap card before returning to the map/exploration view — structurally similar to a Scout Report card (25A.4) so it reuses a presentation pattern the player already knows.

```json
{
  "encounterId": "enc-2026-07-08-river-messenger",
  "representativeId": "rep-river-messenger",
  "factionId": "coastal-people",
  "endReason": "farewell",
  "trustDelta": 1,
  "angerDelta": 0,
  "fearDelta": 0,
  "itemsGained": ["river-route-hint"],
  "itemsSpent": [],
  "newContactStatus": "limited-exchange",
  "journalLine": "The river messenger shared a route hint and left on good terms.",
  "unresolvedTopics": []
}
```

UI presentation: a compact card (portrait, one summary line from `journalLine`, small delta chips for trust/anger/fear shown as qualitative arrows — not raw numbers — plus any items gained). This is the moment invisible stance changes become visible to the player without breaking the "no exact numbers" rule: show ↑ Trust, not "+1".

The card writes directly into the Journal (25A.5) and Faction Notes (25A.7) using `journalLine`, so no separate authoring step is needed for those two systems.

---

## 8A. Reliability & Deception in Dialogue and Offers

Deception is computed from faction state at delivery time, not hand-authored per line as a fixed true/false. This keeps the "Deceptively Friendly" archetype (13B) and general information unreliability (13I) systemic, rather than requiring an author to decide in advance exactly which of hundreds of lines lies.

### 8A.1 Deception Profile

Each faction defines a deception profile:

```json
{
  "factionId": "hidden-ones",
  "baseDeceptionChance": 0.05,
  "fearMultiplier": 0.01,
  "angerMultiplier": 0.005,
  "archetypeModifier": { "deceiver": 0.3, "traumatized": 0.1, "hospitable": -0.05 },
  "maxDeceptionChance": 0.6
}
```

Actual chance for a given delivery: `chance = clamp(baseDeceptionChance + archetypeModifier[faction.archetypeTag] + faction.fear * fearMultiplier + faction.anger * angerMultiplier, 0, maxDeceptionChance)`.

Most factions should sit close to zero — Coastal People, for example, should use `baseDeceptionChance: 0` with no archetype modifier. Deception is a targeted tool for specific archetypes (Deceivers, Fragmented, Desperate, Traumatized-lying-out-of-fear per 13I), not a universal mechanic applied everywhere.

### 8A.2 Variable Dialogue Lines and Offers

Individual dialogue resolutions and offers opt in to this system by setting `reliability: "variable"`. Lines that must always be true (critical plot beats, safety-critical warnings) or always false (known propaganda) stay `reliability: "true"` / `"false"` and never roll.

```json
{
  "id": "topic-ask-situation-followup",
  "reliability": "variable",
  "trueLine": "Follow the eastern river until the black stones.",
  "misleadingVariant": "Follow the western ridge; it's faster.",
  "trueEffects": { "mapHint": "eastern-river-route" },
  "misleadingEffects": { "mapHint": "false-western-ridge-route" }
}
```

Roll timing: the outcome is rolled once, the first time this specific node is delivered within an encounter, and cached for the rest of that session — never re-rolled on repeated viewing within the same conversation. This keeps a single conversation internally consistent; the same claim doesn't flicker into truth if the player backs up and re-reads it.

The engine always knows the true content (`trueLine` / `trueEffects`) even when it displays the misleading variant, so later systems (consequential world events, a Scholar comparing notes back at base, a second expedition confirming or contradicting the claim) can react to the actual truth.

### 8A.3 Detection via Scholar/Mediator

The player can never see the roll result directly, but a Scholar or Mediator present in the expedition unlocks a soft tag on `variable` lines and offers, reusing the UI pattern already defined in 13P.10:

```json
{
  "detectionRule": {
    "requiresRole": "scholar",
    "revealsTag": "Reliability: Uncertain",
    "revealsTruth": false
  }
}
```

Without a Scholar/Mediator present, `variable` content displays with no tag at all — confident-sounding, exactly like reliable content. This is the point: the player has no way to flag uncertainty without the right specialist. With one present, any `variable` line or offer (regardless of whether it happened to roll true or false this time) shows "Reliability: Uncertain" — telling the player where to be careful without ever confirming the actual answer, matching the existing rule that the UI "must not reveal hidden deception directly" (13P.10). Cross-referencing the claim against the archive, another faction, or a later scout report remains the only way to actually resolve the uncertainty — keeping "diplomacy as exploration" (13O) intact even for a well-prepared team.

---

## 8B. Captured Member Negotiation

This section works out the full flow for the "Captured" expedition outcome (expedition_and_members_concept.md 15A, 23A.2, 23A.3, 23B "accept capture"), which was previously only named as an outcome without a defined resolution path. Negotiation happens inside the normal Contact Window, as a special highlighted topic — no separate screen.

### 8B.1 From Missing to Identified Captor

A captured member does not reveal their captor immediately. This follows the existing uncertainty rule for scout status (23A.3: "the player should only know 'Captured' or 'Dead' if evidence exists") one step further:

1. **Missing / Overdue** — existing scout status states, no capture confirmed yet.
2. **Captured (unidentified)** — evidence confirms capture (a risk outcome per 23A.2, or a conflict resolution per 23B), but `captorFactionId` is unknown.
3. **Captured (identified)** — further evidence names the captor. Only at this point does the negotiation topic become available in that faction's Contact Window.

```json
{
  "memberId": "member-mara",
  "status": "captured-identified",
  "captorFactionId": "hidden-ones",
  "capturedOnDay": 14,
  "severity": "held-under-pressure",
  "lastEscalationCheckDay": 19,
  "discoveryClues": ["clue-recovered-pack", "clue-warden-rumor"],
  "resolution": null
}
```

Identification sources reuse existing systems rather than inventing a new one:

- a Finding's `unlocks` field (knowledge_base_and_analysis_concept.md finding schema) can point to `{ "kind": "CaptorIdentification", "memberId": "member-mara", "factionId": "hidden-ones" }`
- a cross-faction shared memory flag (Section 7) from an unrelated faction ("The Border Wardens mention a stranger was taken near the ridge")
- a Faction Event (23A.4) such as "captured scout's notes are returned" from the captor faction itself, which implicitly identifies them
- direct evidence found by a scout at the capture site

### 8B.2 Severity and Escalation

Per your direction: escalation speed depends on the captor faction's disposition, but the state overall changes slowly regardless of faction.

Severity states: `held-safely` → `held-under-pressure` → `at-risk`. Movement between them is checked only during the world phase (not daily), which is what keeps it slow by construction — the fastest any faction can escalate a captive's situation is once per check interval, not once per day.

```json
{
  "factionId": "hidden-ones",
  "escalationChancePerCheck": 0.15,
  "deescalationChancePerCheck": 0.05,
  "checkIntervalDays": 5
}
```

Guidance for authoring these per MVP faction:

- **Coastal People**: escalation chance ~0, `held-safely` is effectively a floor — a friendly faction holding someone is really just "keeping them until this is sorted out."
- **Border Wardens**: low escalation chance, meaningful de-escalation chance — cautious, rule-bound factions are more likely to hold steady or ease off than to escalate.
- **Hidden Ones**: the only faction where `at-risk` is a real, if slow, possibility — this is what gives their territory lethal stakes without making capture instantly fatal.

Severity affects negotiation cost and tone (a `held-safely` negotiation is calmer; an `at-risk` negotiation should read as urgent) but does not itself end the encounter — only the two resolution paths below do.

### 8B.3 Resolution Path 1 — Negotiation via Contact Window

Once identified, the captor faction's Contact Window gains a highlighted topic, generated the same way as the Section 3.2.1 leverage-object hook:

```json
{
  "id": "negotiate-captive-mara",
  "factionId": "hidden-ones",
  "requiresCapturedMemberId": "member-mara",
  "minRepTier": 2,
  "label": "Um Maras Freilassung verhandeln",
  "highlighted": true,
  "nextNode": "captive-negotiation-node"
}
```

**Tier gating matters here more than for ordinary topics.** A release decision is not something a Watcher (Tier 0) makes. If the only available representative is below `minRepTier`, the topic still appears, but resolves to a relay line instead of an outcome:

> "I will carry this to the others. Do not expect an answer today."

This is a deliberate, low-cost way to make Representative Tier Growth (3.5) and Repeat Contact Recap (3.2.2) matter emotionally: returning to the same low-tier representative repeatedly, building their tier, or reaching a genuine Tier 2 audience becomes the path to an actual answer — not a random wall.

At a sufficient tier, the negotiation node offers real resolution options, structurally identical to leverage-object and offer resolution:

- offer a leverage object the faction wants (13P.2C)
- pay Knowledge as a form of ransom/favor
- apologize for a taboo violation, if one caused the capture
- promise a future concession (e.g. avoid a location) — tracked as a memory flag, breakable later with Broken Trust consequences (13F)

If the faction has no specific desire on record yet, the response should generate one on the spot, consistent with 13P.2C's leverage-object-desire pattern:

> "Bring us the stone taken from the old grave, then we will speak of your scout."

This turns a captured-member negotiation into a map objective, not just a dialogue toggle.

### 8B.4 Resolution Path 2 — Passive Escape Chance

In parallel with negotiation, a small independent chance exists for the member to escape on their own during a world-phase check, requiring no player action:

```json
{
  "memberId": "member-mara",
  "escapeChancePerCheck": 0.08
}
```

Escape and negotiation are independent — whichever resolves first ends the captivity. A successful escape returns the member using the existing "Returned Injured" / "Returned Disturbed" states (23A.3) rather than a clean return, since an unaided escape should read as rough.

An **active rescue mission** (sending armed members to physically retrieve a captive) was considered but is explicitly deferred, not MVP scope — it would need its own small risk-event rather than a tactical battle screen (23B already rules out tactical combat), and is a reasonable Tier-3/later addition once negotiation and escape are proven.

### 8B.5 Resolution and Memory

Either resolution path writes:

- member status updated to `Returned` / `Returned Injured` / `Returned Disturbed`
- a Persistent Member History entry (18N): "recovery after capture," "people rescued" if others were involved in securing release
- a Recap Card (Section 8) if resolved through the Contact Window, or a Journal entry (25A.5) if resolved through passive escape
- a faction memory flag recording how it was resolved (negotiated vs. escaped vs. — in a failure case — confirmed lost), since 13J explicitly tracks whether a previous expedition's captured member was returned or not, and this should carry forward to later expeditions

---

## 9. MVP Scope

- **Coastal People**: full Card + Window loop, Representative Tier 1 messenger/trader reps, baseline offers (13P.9) — the primary teaching faction for the whole system.
- **Border Wardens**: Card fully active (warnings, territory entry reactions); Window available once status reaches First Contact, capped at Representative Tier 0–1 until trust is earned — no Elder access in the MVP.
- **Hidden Ones**: Card only, using the alternate action set (e.g. "Withdraw," "Leave the zone") instead of "Open contact"; no Contact Window in the MVP — first contact stays indirect (13P.6).

For Section 2A: `observation` and `warning` intents are sufficient for all three MVP factions (Border Wardens' territorial warnings are the primary teaching case). `request`, `demand` and `offer` intents can be proven with one authored example each on Coastal People or Border Wardens — the Hidden Ones do not need any of the four new intents for the MVP, consistent with their Card-only, no-Window scope above.

### 9.1 Implementation Priority

**Tier 1 — required for the loop to feel complete, low complexity:**
- Always-available disengage (4.2)
- Clean exit vs. abrupt close (6.1)
- Contact Recap Card (8)
- Live Haltung updates (3.1)

**Tier 2 — adds depth, moderate complexity:**
- Leverage object hooks (3.2.1)
- Team composition warning (2.6)
- Repeat contact recap (3.2.2)

**Tier 3 — content/polish, build once the mechanics are proven:**
- Flavor text separated by archetype (4.3)
- Cross-faction shared memory (7)

---

## 10. Implementation Data Model Summary

Consolidated reference for engineering — each entry should become a concrete type/table in the game's data layer:

- `FactionRepresentative { id, factionId, role, archetypeTag, repTier, patience, personalOpinion, stanceThresholds }`
- `RoleTierDefinition { role, baseTier, maxTier }`
- `RoleSlot { role, count, baseTier, maxTier }` and `RosterTemplate { leadershipStructureTag, roleSlots[] }` — world generation output feeding `FactionRepresentative` instantiation (3.7)
- `RepresentativeGrowthState { representativeId, currentTier, positiveContactCount, negativeContactCount, tierUpThreshold, tierDownThreshold }`
- `EncounterCard { factionId, intent: "observation" | "warning" | "request" | "demand" | "offer", title, body, compositionWarning?, situationId?, actions[] }`
- `CardAction { id, label, opensWindow?, routeToTopic?, endsCard?, effects }`
- `SituationRecord { situationId, factionId, intent, escalationLadder[], currentRung, escalationChancePerCheck, checkIntervalDays, relatedLeverageObjectId?, createdOnDay, resolved }`
- `CompositionWarningRule { id, factionId, condition, warningLine, effectIfIgnored }`
- `DialogueNode { id, factionId, representativeRole, minRepTier, rootLine, recapLine?, options[], disengageOption }`
- `DialogueOption { id, label, nextNode?, opensPanel?, endsEncounter?, effects, requiresItemId?, minRepTier?, highlighted?, onceConsumed?, reliability?, trueLine?, misleadingVariant?, trueEffects?, misleadingEffects? }`
- `Offer { ...existing 13P.9 fields, minRepTier?, reliability? }` — extends the offer shape already defined in `factions_and_trade_concept.md` 13P.9
- `DeceptionProfile { factionId, baseDeceptionChance, fearMultiplier, angerMultiplier, archetypeModifier, maxDeceptionChance }`
- `LinePool { topicId, linePool: { archetypeTag: line }, fallback }`
- `MemoryFlag { flagId, visibility: "local" | "shared", propagationDelayDays?, sourceFactionId, factionNoteTemplates[], journalTemplate, dialogueHintTemplate }`
- `ContactRecap { encounterId, representativeId, factionId, endReason, trustDelta, angerDelta, fearDelta, itemsGained, itemsSpent, newContactStatus, journalLine, unresolvedTopics }`
- `SpecialItem { itemId, displayName, category, currentLocation, sourceLocationId, interestedFactionIds, acquiredOnDay, lastKnownHexId }`
- `CapturedMemberRecord { memberId, status, captorFactionId, capturedOnDay, severity, lastEscalationCheckDay, discoveryClues[], escapeChancePerCheck, resolution }`
- `CaptiveTreatmentProfile { factionId, escalationChancePerCheck, deescalationChancePerCheck, checkIntervalDays }`
- `NegotiationTopic { id, factionId, requiresCapturedMemberId, minRepTier, label, highlighted, nextNode }`

This list doubles as the implementation checklist: each bullet should exist as a concrete type before the Contact Window is considered feature-complete against this concept.

---

## 11. Open Items (tracked separately)

The following gaps were identified while reviewing the full faction concept (13–13P) against this document.

**Resolved in this document:**

- ~~Offer gating reconciliation between Contact Status and Representative Tier~~ → Section 3.4.
- ~~Reliability / deception data model for dialogue lines and offers~~ → Section 8A.
- ~~Per-representative disposition layered on faction-wide Trust/Anger/Fear~~ → Sections 3.5 and 3.6.
- ~~Captured member negotiation flow~~ → Section 8B. Active rescue mission explicitly deferred within that section.
- ~~Leverage object inventory dependency~~ → Section 3.2.1 (Special Item Tracking).
- ~~World generation output for representative rosters~~ → Section 3.7.
- ~~Memory flag → narrative text convention~~ → Section 7A.
- ~~Encounter Card types for faction-initiated requests/demands~~ → Section 2A.

**Open items from the original full-concept review are now fully worked through.** Further gaps found while implementing (or found on a future re-read of 13–13P against this document) should be added here the same way.
