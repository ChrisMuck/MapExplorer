# Risk and Outcome Resolution Concept

Working Title: **Untitled Expedition Game**

Status: **Proposed specialist concept**

Purpose: This document defines the shared resolution system referenced by `location_archetypes_and_interactions_concept.md` (Section 9, "Risk and Outcome Resolution") and by `world_locations_and_events_concept.md` (Section 23A, "Event and Risk System"). Both documents describe *what* risk should feel like from the player's side. This document defines *how* a risky action is actually resolved end to end, so that any archetype, scout mission or world event can reuse the same pipeline instead of hand-rolled logic per feature.

The existing `location_archetypes_and_interactions_concept.md` remains the source of truth for archetypes, actions and location state.
The existing `world_locations_and_events_concept.md` remains the source of truth for event categories, tone and the original Risk Score input list.
The existing `expedition_and_members_concept.md` remains the source of truth for resources, specialists and member states referenced here.

---

## 1. Core Design Decision

Every risky action in the game — a location action, a scout mission, a route crossing, a spontaneous event choice — resolves through **one shared pipeline**, not bespoke per-feature dice rolls.

> A location action, a scout order and a world event are different *sources* of risk. They are not different *resolution systems*.

The pipeline consumes a declarative **Risk Profile** and an **Outcome Table**, both authored as data, and produces:

1. a **displayed risk estimate** (shown to the player before commitment)
2. a **resolved outcome tier** (determined at commitment)
3. an **effect bundle** (applied through the generic effect registry already defined in the location document)

Concrete features do not implement their own probability logic. They provide inputs and reference an Outcome Table.

---

## 2. Relationship to the Existing Risk Score Model

`world_locations_and_events_concept.md` (23A.1) already lists the inputs that can influence a Risk Score (terrain, faction territory, morale, specialists, supplies, previous decisions, and so on) and states that the player only ever sees a qualitative label (Low / Moderate / High / Extreme / Unknown).

This document keeps that list as the canonical **input catalogue** and adds the missing middle layer: how those inputs become a score, how that score becomes a band, how a band becomes an outcome, and how specialists and clues change any of these steps without the player ever seeing a raw number.

---

## 3. The Resolution Pipeline

Every risky action resolves through the following stages. This is the same pipeline for a location action (Section 6 of the location document, steps 7–10), a scout mission, or a spontaneous event choice.

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

## 4. Risk Inputs and the Raw Risk Score

### 4.1 Input Categories

Risk Inputs are grouped into three categories so authors and systems know how each one is allowed to act:

**Base Risk** — fixed per action/archetype/variant combination. Defined at authoring time. Example: `Disturb` on an `investigation-site` has a higher Base Risk than `Observe`.

**Situational Modifiers** — additive or multiplicative factors from world state: biome danger, faction territory, active location modifiers (`Unstable`, `Contaminated`, `Guarded`), morale, injuries, supplies, distance from base, prior consequence events, time spent at the location.

**Mitigating Factors** — factors that only ever reduce risk or improve the estimate: a present specialist with a `ReduceRisk` contribution, useful equipment, a previously discovered clue about this specific location, an accurate faction relationship.

This grouping exists so that a designer authoring a new action cannot accidentally create a runaway score — Base Risk sets the ceiling, Situational Modifiers move within a bounded range around it, and Mitigating Factors only ever pull the score down, never up.

### 4.2 Raw Risk Score

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

---

## 5. Risk Bands

### 5.1 Band Thresholds Are Data, Not Constants

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

### 5.1A Tuning Guidelines and Worked Examples

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

### 5.2 Band Is What Gets Shown

The player only ever sees the band (`Low / Moderate / High / Extreme / Unknown`), never the score. This matches the existing rule in Section 9.2 of the location document and Section 23A.1 of the world document — this section simply defines the mechanism that produces that label.

---

## 6. Estimate Confidence: Why the Same Danger Can Look Different Twice

A location's actual danger does not change depending on who is looking at it. What changes is how well the expedition can *read* it. This is the mechanic that lets "a suitable specialist or clue may improve the estimate without changing the actual risk" (location doc, 9.2) actually work.

### 6.1 Confidence Levels

```text
Unknown       — no reliable read at all; shown as "Unknown risk"
Guess         — a rough read; shown band may be a range ("Moderate to High")
Assessed      — a solid read; shown band is a single label
Confirmed     — near-certain read; shown band is a single label with high trust
```

### 6.2 Confidence Inputs

- a present specialist whose role improves interpretation (e.g. Scout, Scholar)
- a previously discovered clue that specifically describes this location or hazard type
- prior confirmed experience with the same variant or modifier combination
- archived knowledge from a previous expedition about this exact location

### 6.3 Confidence Does Not Change the Real Risk

Confidence only changes what stage 5 (Present Risk Estimate) shows. Stage 2–3 (Raw Risk Score, Risk Band) are computed the same way regardless of confidence. A player with `Guess` confidence facing an actually `Low` risk location might see "Moderate to High" — this is the game being honestly uncertain, not the game punishing them for lacking a specialist.

This separation is what prevents two failure modes at once: the game never lies about danger existing, and it never requires a specific specialist just to see the truth — only to see it *clearly*.

---

## 7. Outcome Tables

### 7.1 Structure

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

### 7.1A Table Scope: Archetype-Level, Content-Flavored

Outcome Tables are authored at the **archetype + action** level (e.g. one table for `action-investigate` on `investigation-site`), not per variant. A marked grave and a ruined shrine share the same tier weights and the same set of Effect Bundle *slots* — what differs is which concrete clue, finding or journal text fills each slot, and that comes from the location's Content Profile (already defined in the location document, Section 3.1).

This keeps archetype-specific tables practical: choosing them does not mean every one of ten `investigation-site` variants needs its own authored table — it means `investigation-site` needs one table per action, and ten Content Profiles supply the flavor.

### 7.2 Weight Adjustments

Mitigating Factors and specialist contributions may shift weight between tiers rather than only affecting the Raw Risk Score. This is the mechanism behind the `ReduceRisk` and `ImproveRecovery` specialist contributions (location doc, Section 8):

- **Reduce Risk** shifts weight from `Severe Failure`/`Failure` toward `Success With Cost`/`Partial Result`. It does not create outcomes that could not otherwise happen; it makes the bad ones less likely and the survivable ones more likely.
- **Improve Recovery** does not touch the initial roll. It triggers a second, smaller roll *only when the initial result lands on Failure or Severe Failure*, to determine whether findings, records or injured members are preserved anyway (see Section 9).

### 7.3 Selection

Once the Risk Band and any weight adjustments are applied, the resolver picks a single Outcome Tier using a weighted random selection, seeded per action instance (see Section 10 on determinism).

---

## 8. From Outcome Tier to Effect Bundle

Each Outcome Tier for a given action maps to an **Effect Bundle** — a list of effects drawn from the generic effect registry already defined in the location document (Section 9.4: add clue, injure member, change faction Trust, schedule consequence, and so on).

```text
Action: "action-investigate" on investigation-site / marked-grave
Outcome Tier: Success With Cost
Effect Bundle:
  - add-finding: "Bruchstück einer Karte"
  - add-clue: "warden-grave-symbol"
  - change-morale: -1
  - add-faction-memory: border-wardens, "grave disturbed respectfully"
```

Effect Bundles are authored alongside the Outcome Table, not computed procedurally. This keeps every possible outcome inspectable and testable ahead of time, which matters for the Authoring Validation rules already defined in the location document (Section 19): "outcome tables with no valid outcome" is only checkable if every tier's Effect Bundle actually exists.

---

## 9. Recovery Resolution (Failure Does Not Mean Erasure)

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

---

## 10. Save Points, Crash Safety and Seeding

**Locked decision:** the game only saves at the Base Camp, between expeditions. There is no manual save-and-reload during an active expedition, and no in-game "load checkpoint" screen exists at any point.

### 10.1 Why the Save Policy Already Solves Most of the Reroll Problem

A single risky action can no longer be reloaded and retried in isolation — the smallest unit a player could even attempt to "redo" is the entire expedition since the last camp visit, which costs enough time and decisions to be self-limiting rather than an exploit. Seeded RNG (10.3) is therefore not the primary defense against reroll-fishing; the save policy already provides that.

### 10.2 Crash-Safe Suspend Point

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

### 10.3 Seeding Recommendation

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

## 10A. Scout Mission Resolution: A Distinct Pipeline Variant

**Locked decision:** scout missions do not resolve through the single-commit pipeline in Section 3. A scout mission spans multiple world-phase days with no player decision point in between, and its outcome space (`world_locations_and_events_concept.md`, 23A.3: On Mission, Expected Back, Overdue, Returned, Returned Injured, Returned Disturbed, Missing, Captured, Dead) is a state machine, not a single tier result.

### 10A.1 Dispatch: Shared Stages, Adapted

Stages 1–5 of the shared pipeline still apply at the moment the player issues a scout order: Risk Inputs (direction, biome, faction territory, scout star level, expedition composition) produce a Raw Risk Score, a Risk Band, and an Estimate Confidence, shown before the player commits the scout. This is the same "honest but possibly blurry" estimate as any location action.

### 10A.2 Per-Leg Resolution Reuses the Shared Outcome Table Structure

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

### 10A.3 Final State Resolution

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

### 10A.4 Overdue Handling

If the final resolution or an escalation flag produces `Overdue`, this is deliberately *not* an immediate answer (world doc, 23A.3: "an overdue scout should create a question on the map, not an instant answer"). The resolved state exists in World Truth immediately (the Definition/Instance/Player-Knowledge split from the location document, Section 3.2, applies here too), but Player Knowledge only updates to `Returned` / `Captured` / `Dead` once the expedition gains evidence for it — the scout arriving late, a later expedition finding traces, or a faction interaction.

### 10A.5 Recovery Check Still Applies

If a leg or the final resolution would result in `Dead` or `Captured`, the Recovery Check from Section 9 still runs before that result is locked in — a scout's fate is not exempt from the same fairness mechanism a location action gets.

---

## 11. Fairness Rules

These extend the fairness rule already stated in the location document (11.4, Containment Site): *"Major irreversible consequences should have discoverable warning evidence."*

1. No Outcome Table may assign `Severe Failure` a nonzero weight at a `Low` Risk Band unless the action is explicitly flagged `IgnoresLowRiskFloor` (reserved for a small number of authored "this was never actually safe" moments, used deliberately, not by accident).
2. Every action that can end an expedition member's story (death, permanent capture) must have a Recovery Check available, even if that check has a low success chance. There is no action in the MVP that skips Section 9 entirely.
3. Estimate Confidence may be low, but the displayed band must never be a strictly *lower* danger than the true band. If the game is uncertain, it may show a wider range or "Unknown" — it may never show "Low" when the truth is "Extreme."
4. An `Unknown` displayed risk is itself information: it should be rare enough that the player learns to be cautious around it, not a routine default that becomes meaningless noise.

---

## 12. Data Structures

Extending the definition types already listed in the location document (Section 3.1):

```text
RiskProfileDefinition
├── baseRisk
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

These integrate directly with the `RequirementDefinition`, `EffectDefinition` and `LocationActionDefinition` types already defined in the location document — a `RiskProfileDefinition` and `OutcomeTableDefinition` are referenced by ID from an action definition, the same way `ContentProfileId` is referenced today.

---

## 13. Decision Log

### Locked

1. **Save policy.** The game saves only at Base Camp; there is no in-expedition save/load screen.
2. **Crash-safe suspend point.** One hidden, always-latest suspend snapshot, written atomically only after a resolution fully completes. It is never player-loadable and never allows returning to an earlier moment (Section 10.2).
3. **Numeric scale.** The Raw Risk Score uses a fixed **0-100** range, with Base Risk and modifier magnitude guidelines (Section 5.1A) chosen so the 25/50/75 default bands hold up under worked examples.
4. **Outcome Table scope.** Tables are authored per **archetype + action**, not per variant; Content Profiles supply variant flavor (Section 7.1A).
5. **Scout missions.** Resolved through the per-leg tick + priority-ordered final resolution variant (Section 10A), reusing the same Outcome Table structure as location actions rather than inventing a second system.
6. **Seeding.** Retained for reproducibility and balancing, not as the primary anti-scum mechanism (Section 10.3).

### Remaining Tuning Tasks (numbers to playtest, not open design questions)

1. Whether 25/50/75 hold up once real content is authored across all ten archetypes, or need per-archetype adjustment (Section 5.1).
2. The exact Minor-to-Major escalation probabilities per biome/faction-territory combination for scout legs (Section 10A.2) — a balancing pass, not a structural decision.
3. Whether five escalation flags (Spotted, Injured, Lost, Exhausted, Trailed) cover enough narrative variety once actual scout mission content is authored, or whether a sixth is needed for a specific faction or biome.

---

## 14. MVP Implementation Boundary

### Required for MVP

- Raw Risk Score computation (Section 4)
- Risk Band thresholds per action (Section 5)
- Crash-safe suspend point (Section 10.2) — this is a fairness-critical baseline, not a nice-to-have, even for the Vertical Slice
- Estimate Confidence with at least `Guess` and `Assessed` (Section 6) - `Unknown` and `Confirmed` can follow shortly after
- Outcome Tables with at minimum `Success`, `Success With Cost`, `Failure` for MVP actions (full six-tier range not required everywhere)
- Recovery Check for any action that can injure, capture or kill a named member (Section 9)
- Fairness Rule 1 and 2 from Section 11 (no unwarned severe failure at Low risk; recovery always available for member-ending outcomes)
- Scout dispatch estimate (Section 10A.1) using the same shared Stages 1-5 as location actions

### Deferred

- Full six-tier Outcome Tables for every action
- `Confirmed` confidence tier and its authoring requirements
- Full per-leg Scout Mission resolution (Section 10A.2-10A.4) - the Vertical Slice may use a simplified single end-of-mission roll against the same input catalogue, with the per-leg tick system following once the core loop is validated
- Deterministic seeding infrastructure (Section 10.3) - acceptable to use ordinary RNG for the Vertical Slice, since the save policy and suspend point already limit reroll-scumming without it

---

## 15. Acceptance Criteria

1. A designer can author a new action's danger by filling in a `RiskProfileDefinition` and referencing an `OutcomeTableDefinition`, without writing code.
2. The same pipeline resolves a location action, and — via the 10A variant — a scout mission outcome.
3. Two players facing the identical true risk but different specialists/clues can see different displayed risk bands, while the actual resolution odds remain identical.
4. No player-visible path exists for the displayed risk to understate true danger (Fairness Rule 3).
5. A failed `Disturb` action on a `marked-grave` can still preserve a finding through the Recovery Check, producing a "damaged but present" result rather than simple loss.
6. Authoring validation (location doc, Section 19) can detect an Outcome Table with a tier that has no Effect Bundle.
7. Force-quitting the app at any point during an expedition and relaunching resumes at the exact latest resolved state, with no option to reach an earlier one.
8. A scout mission with no escalation flags set cannot resolve to Captured or Dead on its final day.

---

## 16. Recommended Repository Integration

1. Add this file as `risk_and_outcome_resolution_concept.md`, linked from `exploration_game_concept.md` alongside the other specialist documents.
2. Update Section 9 of `location_archetypes_and_interactions_concept.md` to reference this document by name instead of the generic phrase "shared event and risk system."
3. Update Section 23A of `world_locations_and_events_concept.md` similarly — its Risk Score input list becomes the canonical input catalogue for Section 4 of this document.
4. Cross-check the per-leg tick model (Section 10A) against the scout order structure (direction, duration, focus, behavior) in `map_and_exploration_concept.md` before authoring the first `ScoutMissionOutcomeTable`.
5. Add the Base-Camp-only save policy and crash-safe suspend point (Section 10) as explicit locked decisions in `exploration_game_concept.md`'s Section 0, since they affect more systems than just risk resolution.
