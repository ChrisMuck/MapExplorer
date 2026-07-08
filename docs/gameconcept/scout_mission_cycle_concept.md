# Scout Mission Cycle Concept

Source of truth for scout orders, mission simulation, report structure, missing scouts and recovery.

This document extends and consolidates Section 8 ("Scouts") of `map_and_exploration_concept.md`. It should be treated as the authoritative source for anything related to the scout order screen, mission resolution and scout report presentation. Cross-references to Direction, Duration, Focus and Behavior definitions still apply as originally written; this document adds the surrounding cycle that was missing: scout selection context, risk communication, outcome mechanics, report typology and long-term consequence.

This document must be read before changing the scout order screen, scout report screen, or scout outcome simulation.

---

## 0. Relationship to Other Documents

- `map_and_exploration_concept.md` §8 defines Direction, Duration, Focus, Behavior, Scout Teams and the base rules for scout loss. Those definitions are not repeated in full here; they are extended.
- `expedition_and_members_concept.md` §18I / §18J define the Scout role and star levels. This document assumes those exist.
- `expedition_and_members_concept.md` §18N defines persistent member history. Every scout mission should write into it.
- `ui_and_visual_communication_concept.md` §25A.4 and §25B.3 define baseline report UI and portrait rules. This document specifies the concrete report types and screen contents built on top of those rules.
- `knowledge_base_and_analysis_concept.md` defines Knowledge Point awards for useful scout reports; unchanged here.

---

## 1. Overview: The Four Phases

A scout mission always moves through four phases:

1. **Order** — the player selects scout(s), direction, duration, focus, behavior.
2. **Journey** — the mission resolves in the background during the World Phase, day by day.
3. **Return** — a report (or an absence of one) reaches the player.
4. **Consequence** — the outcome writes into the scout's history, the archive, the journal and, if unresolved, remains open for future discovery.

The current implementation covers phase 1 (order screen) and a thin version of phase 3 (report card). Phases 2 and 4 are largely undefined. This document fills in all four.

---

## 2. Phase 1: Scout Orders

### 2.1 Existing Parameters

Direction (8 compass sectors), Duration (1–5 days), Focus (terrain, dangers, faction traces, ruins/special locations, safe route, supplies/water, missing scout, border observation) and Behavior (cautious, normal, risky) remain as defined in `map_and_exploration_concept.md` §8.3. No changes proposed to these four parameters themselves.

### 2.2 Scout Roster — Selection Context

The current roster list shows only name and a binary "bereit" status. This discards information the player has earned and needs for a good decision.

Each roster entry should show:

- portrait (placeholder acceptable for MVP)
- name
- star level (0–3, per §18J)
- current condition: ready / recovering / injured / exhausted
- a one-line summary of the most recent mission: *"zuletzt: Nord, Tag 12, unversehrt zurück"* or *"zuletzt: vermisst seit Tag 14"*

A scout with an unresolved missing-status from a previous mission should not appear as selectable; they are gone until the game resolves that status through the Consequence phase.

### 2.3 Team Selection

Selecting two scouts at once forms a team mission, per §8.4. The order screen should visually group them once both are selected, and the summary footer should name both: *"Mira und Tovin nach Ost · 2 Tage · Fokus Erkunden · Risiko mittel"*.

### 2.4 Risk Preview

The order screen should surface a qualitative risk read-out that reacts live to the current selection, without exposing underlying numbers.

Inputs to the read-out:

- known/unknown status of the target direction (from map knowledge state, §10 of `map_and_exploration_concept.md`)
- selected duration
- selected behavior
- whether a team or single scout is sending
- any standing danger marker or prior report flagging that direction

Output is a short qualitative line, not a percentage:

> Risiko: erhöht — unbekanntes Gebiet, riskantes Verhalten, 3 Tage.

> Risiko: gering — bekannte Route, vorsichtiges Verhalten.

### 2.5 Continuity Warnings

If a previous scout report, faction note or map marker already flagged danger, a warning, or a doubtful area in the selected direction, the order screen should surface it before the player commits:

> Vorheriger Bericht: Fremde Präsenz im Osten vermutet (Tag 9).

This connects the order screen to the archive instead of relying on the player's memory of earlier sessions.

---

## 3. Phase 2: Mission Simulation

This phase is not player-facing in detail, but the outcome logic must be concrete enough that the Confidence label shown later is earned rather than decorative.

### 3.1 Outcome Factors

| Factor | Effect |
|---|---|
| Scout star level | higher reduces loss risk, improves report clarity, reduces misinterpretation |
| Behavior | cautious lowers risk and range; risky raises both and raises misinterpretation chance |
| Duration | each additional day compounds risk and potential information yield |
| Terrain knowledge of target direction | unknown territory raises base risk; previously explored terrain lowers risk but yields less new information |
| Focus | determines which hint category the mission favors; may cause other categories to be missed entirely |
| Team vs. single | team roughly halves total-loss risk, but reduces discretion (factions notice more easily) |
| Local faction presence | raises risk independent of player choices; may be an unseen modifier |

### 3.2 Outcome Branches

Each mission resolves into one of the following:

- **Full success** — normal report, higher confidence
- **Partial / unclear** — report with lower confidence, possibly conflicting hints
- **Delayed return** — arrives 1+ days late; produces an Overdue card until resolved
- **Returns injured** — report delivered, but scout becomes unavailable for a recovery period
- **Missing** — see §5.1
- **Captured** — see §4.4

### 3.3 Confidence Derivation

The Confidence label ("Verlässlichkeit: Mittel") shown on a report must be derived from star level + behavior + terrain knowledge + duration, not freely authored per report. As a starting rule of thumb:

- **Hoch**: veteran or elite scout, cautious or normal behavior, known or lightly unknown terrain
- **Mittel**: normal scout, normal behavior, or any scout under risky behavior in known terrain
- **Niedrig**: beginner scout, risky behavior, deep unknown territory, or long duration (4–5 days)

Confidence should ideally be tracked per hint, not only once for the whole report — see §4.1.

---

## 4. Phase 3: Report Types

The current implementation has exactly one report card shape. The situations it needs to represent are different enough that they need distinct card types.

### 4.1 Type A — Success Report

Builds on `ui_and_visual_communication_concept.md` §25A.4 and §25B.3. A Type A card should contain:

- scout portrait(s) and name(s) — required per §25B.3; for a team, both portraits, with the missing one shown grayed out if only one returned
- mission metadata: direction, planned vs. actual duration, focus, behavior
- readable narrative text (as already modeled: *"Mara returned on the evening of the third day..."*)
- structured hints, each carrying its **own** confidence tag rather than one confidence value for the whole report
- actions:
  - **Öffnen** — full detail view with map link
  - **Marker** — quick-create a marker from the top hint, for players who don't want to dig in
  - **Vergleichen** — compare against an existing report or marker on the same region, surfaced only when a contradiction exists
  - **Als zweifelhaft markieren**
  - **Notiz hinzufügen**
- an automatic Expedition Journal entry (§25A.5), written without player action: *"Tag 4: Mira kehrte von Nordwest-Erkundung zurück."*

### 4.2 Type B — Overdue / Missing Card

Minimal by design — the player should know only what the expedition would know.

> Mira sollte heute zurückgekehrt sein. Sie tat es nicht.

Available actions:

- wait
- send a search party (costs movement points or a full day)
- mark the last known direction on the map

The card remains open (not dismissible) until resolved by §5.1.

### 4.3 Type C — Recovery Report

Generated later, potentially by a different expedition, when remains, a journal, equipment or testimony connected to a previously missing scout are found. Must link back to the original Type B entry so the player can reconstruct the timeline:

> Vermisst seit Tag 14 → gefunden Tag 63.

### 4.4 Type D — Captured / Indirect Contact

Used when information arrives through a faction rather than the scout directly (a returned belonging, a message, a witness account). Follows the indirect-communication rules in §25B.4 — no invented visible speaker if the expedition never saw one.

### 4.5 Report Actions Summary

| Action | Available on |
|---|---|
| Öffnen | Type A |
| Marker | Type A |
| Vergleichen | Type A, when contradiction exists |
| Als zweifelhaft markieren | Type A |
| Notiz hinzufügen | all types |
| Suchtrupp schicken | Type B |
| Letzte Richtung markieren | Type B |
| Zum ursprünglichen Eintrag springen | Type C |

### 4.6 Journal Integration

Every resolved mission (any type) writes one line into the Expedition Journal automatically. This is presentation only — it does not replace the report card, it makes the day-by-day history reconstructable per §25A.5.

---

## 5. Phase 4: Consequence and Persistence

### 5.1 Missing Scout Chain

1. Type B card appears; player has no details.
2. Card stays open in the Reports tab until resolved or until the expedition leaves the area for good.
3. Random or location-bound follow-up events may surface partial evidence over subsequent days (blood trail, gear, a grave).
4. If the expedition departs without resolution, the entry moves into the archive as **ungeklärt** (unresolved) and becomes available as a hook for a future expedition.
5. If the scout is later confirmed dead, their personal history entry (§18N) is closed, with an optional memorial/archive link.

### 5.2 Scout History Integration

Every mission, regardless of outcome, appends to the scout's persistent history (§18N):

- missions completed, by direction and day
- injuries survived
- missing periods
- reports authored
- companions lost, if on a team mission

This is what makes a 2★ veteran scout feel different from an anonymous unit, and what makes losing one costly beyond the raw resource.

### 5.3 Archive Integration

All report types, once resolved, are filed into the Archive (§25A.6) with their reliability state intact (Reported / Confirmed / Old / Doubtful / Lost, per §10 of the Map and Exploration Concept). Spending Knowledge Points never removes an archived report.

### 5.3.1 Route-Level Decay

Knowledge decay should not only apply at the scale of whole faction territories (§15.3 of `expedition_and_members_concept.md`). Individual routes and safety markers should carry an implicit shelf life.

Rule: a route or marker holding **Confirmed** or **Safe** status downgrades automatically to **Old** after a threshold number of expedition days without reverification — regardless of whether any world event or failure triggered it. Recent travel, a fresh scout report, or reliable faction confirmation resets the clock.

This extends the failure-triggered downgrade already defined in §15B of `map_and_exploration_concept.md` (a route that ended in failure or disappearance is downgraded immediately) to also apply gradually over time, even when nothing went wrong. A `Safe`-marked path from 40 days ago should feel less trustworthy than one confirmed yesterday, without requiring a dramatic trigger.

### 5.4 Memorial

The base should include a visible Memorial for expedition members — scouts in particular — who died or remain permanently missing, especially veterans (2★ and above per §18J of `expedition_and_members_concept.md`).

The Memorial is a simple, low-interaction base fixture, not a management screen:

- portrait, name, star level
- short summary drawn from their persistent history (§18N): missions completed, notable discoveries, cause and day of death or disappearance
- link back into the Archive entry that closed their status (§5.1)

This gives the emotional weight already required by the design ("a dead or missing veteran should feel different from losing an anonymous resource") a concrete, visible home, rather than leaving it as text buried in history logs.

---

## 6. Scout Traits (Future Scope)

Retained from `map_and_exploration_concept.md` §8.7 for continuity: cautious, reckless, experienced, inexperienced, good tracker, good at interpreting ruins, good at reading faction signs, poor orientation, brave, nervous, linguistically talented, prone to panic. These are not required for the MVP mission cycle above, but the outcome model in §3.1 should be built so traits can later modify the same factor table without restructuring it.

---

## 7. Faction Reaction to Spotted Scouts

Scouting should not be a risk-free information tap. A spotted scout should leave a trace in how a faction behaves afterward, even if the player never sees an exact number for it.

### 7.1 Detection

Each mission carries a chance of the scout being noticed by a local faction. Detection likelihood is driven by the same factors already defined in §3.1:

- Behavior (risky increases detection chance; cautious reduces it)
- Duration (longer missions raise cumulative detection chance)
- scout star level (higher reduces it)
- local faction presence in the target region

Detection is not necessarily reported to the player directly. It should instead quietly raise a **Faction Awareness** state for that faction in that region.

### 7.2 Faction Awareness Tiers

Awareness should use the same qualitative approach as Confidence and Map Knowledge States — no exposed numbers.

- **Unaware** — no reason to suspect outside activity
- **Suspicious** — isolated sighting, faction is uncertain
- **Alert** — repeated sightings, faction actively watches the region
- **Hostile Response** — faction acts against the perceived intrusion

Awareness rises with detected missions in the same faction's territory and decays slowly over time if the player avoids further activity there.

### 7.3 Consequences

Awareness should surface diegetically, through report language and world state, not through a meter:

- renewed or additional warning markers (*"the border markers looked freshly renewed"*)
- increased patrol presence, raising risk for future scouts and for the main expedition passing through
- a faction may preemptively seal, move or fortify something the player has not investigated yet
- ambush risk rises if the main expedition later travels through an Alert or Hostile region
- the faction may attempt to specifically intercept or capture a scout on a future mission into the same territory
- rarely, the faction may send its own observer toward the player's camp, producing a report where the expedition itself feels watched

### 7.4 Player-Facing Signal

Faction Notes (§25A.7 of `ui_and_visual_communication_concept.md`) should reflect the current Awareness tier as a short qualitative line, e.g.:

> Die Grenzwächter haben die Warnmarkierungen an der Schlucht erneuert.

The Risk Preview on the order screen (§2.4) should also incorporate Awareness: a region at Alert or Hostile should read as elevated risk even if terrain and duration alone would suggest otherwise.

### 7.5 Strategic Tension

This creates an explicit tradeoff at the point of sending a scout: cautious missions preserve a region's low Awareness for longer, useful for continued reconnaissance, while risky missions extract more information quickly at the cost of burning that region's stealth budget.

---

## 8. Follow-up Questions (Interview Mechanic)

A returned scout should not be a one-shot text dump. The player should be able to ask a small number of follow-up questions, recovering detail the standard report filtered out — at a cost, and without ever guaranteeing a clean answer.

### 8.1 Why Detail Gets Filtered

Mission Focus (§3.1, inherited from `map_and_exploration_concept.md` §8.3) already causes some categories of observation to be deprioritized or missed by the main report. A scout sent with Focus: Ruinen may have walked past a danger sign without mentioning it in the readable report, simply because it wasn't the mission's priority.

Follow-up questions give the player a way to ask about what the report didn't prioritize, not a way to unlock hidden "true" information.

### 8.2 Mechanic

On a Type A report card, an additional action **Nachfragen** opens a short set of contextual question topics, generally the Focus categories the mission did not use, plus any hint the report already flagged as ambiguous.

Example:

> Fokus war: Ruinen
> Mögliche Nachfragen: Gefahren? Fraktionsspuren? Route?
>
> Du fragst: "Habt ihr unterwegs auf Gefahren geachtet?"
> Mira: "Nicht besonders. Aber... jetzt wo du fragst, es gab weniger Vögel als sonst, weiter nördlich."

Follow-up questions should be limited per report (for example, up to two), to avoid turning the interview into a free faucet for complete information.

### 8.3 Answer Quality

The quality of the answer should depend on the same factors that shape the report itself:

- scout star level: veterans give sharper, more specific answers
- scout condition: an injured or exhausted scout gives vaguer answers or may simply not remember clearly
- behavior mode: a scout who moved cautiously had more attention available than one who moved fast and risky

A low-star or shaken scout might answer with uncertainty rather than a clean fact:

> "Ich bin nicht sicher. Es war schon dunkel."

### 8.4 Ambiguous Hints

Follow-up questions are also the natural tool for probing a hint the report already marked as vague:

> Bericht: "Die Route wirkte zu sauber, als wäre sie gepflegt."
> Nachfrage: "Was meinst du mit 'zu sauber'?"
> Antwort: "Keine Blätter, keine umgestürzten Äste. Als würde sie regelmäßig genutzt."

This can shift a hint's confidence tag (§4.1) from Doubtful toward Reported, but should not resolve interpretation outright — per the core UI rule, the game organizes information, it does not solve it (§25A.1 of `ui_and_visual_communication_concept.md`).

### 8.5 Relationship and History

Interview outcomes should feed into the scout's persistent history (§5.2 / §18N): a scout interviewed often and treated well may over time give better answers, tying naturally into a future rapport or trust mechanic, without requiring one for the MVP version of this feature.

---

## 9. Recurring Symbols and Mystery Threads

Individual scout reports already describe symbols, marks and signs as clues (§8.2 of `map_and_exploration_concept.md`: carved posts, warning marks, faction signs). This section defines what happens when the *same* symbol resurfaces across different reports, different regions, or even different expeditions separated by many in-world days.

### 9.1 Stable Symbol IDs

Every distinct symbol, motif or recurring mark in the world should carry a stable Symbol ID, following the same anti-farming and continuity principle already used for Findings (§16A.1B of `knowledge_base_and_analysis_concept.md`). Any scout report, faction note or discovery that references the symbol is tagged with its Symbol ID.

### 9.2 Mystery Threads

A **Mystery Thread** becomes visible to the player once at least two independent sightings reference the same Symbol ID — regardless of how far apart in time, region or expedition they occurred. This directly benefits from the Expedition Archive's persistence across expedition failure and campaign length (§15.4 of `expedition_and_members_concept.md`).

Example:

> Symbol: drei geschnitzte Pfähle mit Vogelmarkierung
> Gesichtet: Späher Mara, Expedition 1, Tag 12, Nordost
> Gesichtet: Späher Tovin, Expedition 3, Tag 88, Süd
> Verknüpfung: möglicher Grenzmarker derselben Fraktion, weit auseinanderliegend

### 9.3 The Archive Gets a Symbol View

The Archive (§25A.6 of `ui_and_visual_communication_concept.md`) should gain a browsing category: **Symbole & wiederkehrende Zeichen**, listing every known Symbol ID along with all linked sightings (location, day, expedition, reporting scout).

This view only aggregates raw sightings. It does not auto-solve meaning, consistent with the UI design rule that the interface organizes evidence but does not interpret it for the player (§25A.1).

### 9.4 Conflicting Descriptions

Because Focus and misinterpretation (§3.1, §8.7 of `map_and_exploration_concept.md`) can cause two scouts to describe the same symbol differently — "drei Pfähle" versus "geschnitzte Stelen" — the Archive should let the player manually merge two sightings into one thread if they believe they match, or leave them separate if uncertain. The game should never auto-merge these on the player's behalf.

### 9.5 Long-Term Payoff

A Mystery Thread with enough corroborating sightings may eventually become eligible for base analysis, converting scattered sightings into an actual Finding entry (per the Findings system in `knowledge_base_and_analysis_concept.md`) and awarding Knowledge Points accordingly.

The primary reward, however, is narrative coherence across a whole campaign — reinforcing the design goal that lost expeditions and old reports create history, not only punishment (§15.2 of `expedition_and_members_concept.md`).

---

## 10. Future Enhancements (Optional)

The following ideas are worth keeping on record but are not committed design. They may be added later without restructuring the core mission cycle above.

### 10.1 Independent Double-Scouting for Verification

Sending two scouts independently (not as a paired team, per §2.3 / §8.4 of `map_and_exploration_concept.md`) into the same direction, so their reports can corroborate or contradict each other. This would let the player earn confidence through a deliberate decision rather than only through an unseen star-level formula.

### 10.2 Per-Mission Equipment Choice

A small, mission-scoped equipment decision separate from the full expedition loadout (§18F of `expedition_and_members_concept.md`): giving a scout rope, trade goods, or signal equipment for a specific mission. Rope could reduce risk on a mountainous bearing; trade goods could allow an attempt at peaceful faction contact instead of automatic avoidance.

---

## 11. Open Questions

- Should per-hint confidence (§4.1) be shown as a tag, an icon, or only through report tone/wording?
- How many unresolved Type B cards can exist at once before the game forces the player to address them (e.g. before returning to base)?
- Does a team mission produce one merged narrative or two short first-person accounts when perspectives differ?
- Should the Risk Preview (§2.4) ever be wrong on purpose (i.e., faction presence unknown to the player skews it), or should it always reflect true known risk factors only?
- How quickly should Faction Awareness (§7.2) decay, and should the player ever get a direct signal of the current tier, or only indirect narrative cues?
- Should follow-up questions (§8) ever risk a wrong or misleading answer, the same way primary reports can misinterpret events?
- What is the false-positive risk of letting players manually merge symbol sightings (§9.4) that are not actually the same thread?
