# Tutorial Vertical Slice Route Specification

Status: **Implementation specification — owner review required for player-facing pacing and wording**

Related documents:

- `next_implementation_concept.md`
- `gameconcept/mvp_vertical_slice_concept.md`
- `gameconcept/exploration_game_concept.md`
- `gameconcept/cross_system_integration_concept.md`
- `gameconcept/situational_scene_description_concept.md`

## 1. Purpose

This document converts the MVP promise into one reproducible two-expedition route. It is the
acceptance reference for the deterministic Tutorial campaign, not a general campaign design and
not an instruction to add a quest-marker system.

The tutorial succeeds when a player can truthfully say:

> We did not solve the island, but we learned why the old route matters, chose to return, and came
> back with the means to do something different.

The player receives a **field brief**, not a magical objective arrow:

> The previous coastal survey stopped reporting beyond the old trade route. Establish what can be
> learned safely, and return with records worth preserving.

This text is direction only. There is no mandate tracker, exact target marker, forced completion
state or hidden success check in this implementation phase.

## 2. Current Baseline and Required Change

The player-facing Tutorial session starts in Base Camp before Expedition 1 has departed. The first
screen explains the personnel pool and the departure/readiness area; the player selects the opening
team and uses the ordinary start command. Expedition 1 is created only at that moment. On its first
return, the return scene leads into **Wissen auswerten**, where the player advances ordinary base
time, evaluates the returned record and then prepares the second expedition.

`TutorialGameFactory` supplies a deterministic 40 × 30 world, an eight-person first expedition,
three factions, paths, locations, a base loop and the regular shared simulation path. The catalog
supplies reusable location archetypes, actions, scenes, faction content, scouting content and
visual placeholders.

The first A2 migration replaces the former technical layout, in which the broken ravine at core hex
`2/15` sat next to the base at `1/15`. Manifested location instances now place a coastal contact,
warning, legacy camp and crossing in deliberate route order. A narrow tutorial-campaign fixture
adds the necessary concrete runtime faction relations without placing faction truth in reusable
location profiles. The remaining A2 work is the authored evidence/return/preparation chain, not a
second Unity-only route system.

The implementation retains reusable systems and must not add tutorial-only rules to Unity or a
branch keyed to a concrete location ID in generic location/faction systems.

### 2.1 Current authored map geometry

The logical old survey route starts at the base and remains visibly traceable up to the ravine.
The historical road can be rendered continuously, but its edge at the ravine remains blocked until
the regular `OpenRoute` effect resolves it.

| Route point | Core hex | Purpose |
| --- | --- | --- |
| Coastal base | `1/15` | Start and return point. |
| Coastal landing | `8/15` | Contact opportunity with the Coastal People. |
| Border warning | `12/16` | First territorial interpretation decision. |
| Abandoned camp | `14/17` | Earlier-survey/legacy clue. |
| Marked grave | `12/18` | Optional nearby respectful-investigation branch. |
| Broken ravine edge | `16/17` → `17/17` | First-expedition preparation gate and second-expedition route outcome. |

## 3. Locked Route Shape

The following route is the initial implementation target. Day ranges are pacing targets for a
normal cautious player, not hard gates. A player may take a detour, leave a location or return
early; the resulting incomplete knowledge remains a valid outcome.

| Beat | Target day range | Observable player experience | Required persistent result |
| --- | --- | --- | --- |
| 0. Departure | 1 | The field brief, map fog, supplies, movement points and expedition composition are visible. The absent engineer is not yet advertised as the solution. | Expedition 1 begins with two scouts, two guards, two carriers, medic and scholar; no engineer. |
| 1. Scout lead | 2–3 | Send one or two scouts to the broadly indicated old-route direction. The returning report mentions a damaged crossing, old camp traces or repeated warning signs only approximately. | Delivered scout report and neutral evidence; the report does not reveal an exact remote location. |
| 2. Coastal information | 3–4 | At a visible coast/river contact opportunity, the expedition can withdraw or communicate. A helpful answer gives incomplete practical advice: do not camp beyond the black stones; the inland crossing is unreliable. | Player-visible contact/signature knowledge and a report/archive reference. The Coastal People are not a quest dispenser. |
| 3. Border warning | 4–6 | Posts, cloth or a marked grave establish that land is inhabited and watched. The player can observe, interpret, respect, mark or deliberately cross. | Earned anonymous/identified boundary evidence, an optional player marker/note, and any normal faction memory caused by conduct. |
| 4. Legacy site | 5–7 | An abandoned survey camp provides partial evidence: a damaged route sketch, a journal fragment or a named trace. It is evidence, not a complete explanation or material-loot loop. | A finding/evaluation input or durable archive lead connects the earlier survey to the crossing. |
| 5. Broken route | 6–9 | The old crossing/ravine is reached after prior clues. Inspection reveals a real route problem. The safe durable repair is visibly locked by the missing engineer; risky crossing, bypass/scouting, mark, leave and return remain meaningful where their ordinary requirements allow. | Last-known blocked condition, documented evidence and a return-worthy preparation need persist. No action automatically solves the route. |
| 6. Return decision | 7–10 | Supplies, elapsed time, an overdue-scout risk, injury or credible territorial warning make returning sensible. None is a scripted unavoidable loss. | On successful return, unsecured knowledge/findings transfer into normal base/archive/evaluation flow. |
| 7. Base preparation | 8–12 world days | Return scene summarises what is known and uncertain. The player evaluates at least one item, spends time/Knowledge Points and can request/recruit the engineer or use an equivalent existing base action. | Expedition 2 can include an engineer through normal roster/loadout commands. Existing notes/reports/faction knowledge remain visible. |
| 8. Second expedition | 10–15 | The player reuses old map knowledge and returns to the crossing. Engineer-gated repair/secure/project work is now available through the regular location-action contract. | A persistent route/local-state change and any authored normal world/faction response demonstrate that expedition two is not a reset. |

The sealed gate, the Hidden Ones and the distant wall may be visible clues or optional later
investigations. They must not compete with the crossing as the tutorial's required first-loop
decision. The Hidden Ones must have real warned territory, but their first function is to teach
that unexplained land is not empty, not to force a combat or failure sequence.

## 4. Knowledge-Honesty Requirements

At every beat, player-facing information is constrained as follows:

| Subject | Player may learn | Player must not learn yet |
| --- | --- | --- |
| Scout lead | Direction, scope, confidence, terrain/sign impressions and a neutral lead. | Exact unseen coordinate, true faction identity or resolved consequence branch. |
| Coastal information | Testimony/warning and contact identity after ordinary contact. | Objective ownership of all inland hexes or a guaranteed safe solution. |
| Border sign/grave | Visible signs, an anonymous watched/claimed/sacred impression, then earned interpretation. | Faction identity before evidence/contact justifies it; exact boundary geometry. |
| Legacy camp | What was physically found and a partial connection to an earlier survey. | A complete account of the survey's fate. |
| Broken route | Observed blocked condition, visible instability and known specialist lock wording. | Hidden cause, all risk rolls or future faction/process branches. |
| Base/second expedition | Secured archive knowledge and normal preparation options. | Omniscient confirmation that the repair is consequence-free. |

Every important report, contact, discovery, return and route state has a scene description and a
valid placeholder visual ID. The visual may show an observed exterior, sign, person or object; it
must never reveal concealed WorldState truth.

## 5. Required Tutorial Data

The target data model separates reusable content from this fixed journey.

### 5.1 Reusable catalog content

Use existing catalog document types for:

- route-obstacle, territorial-marker, investigation-site and contact-site scenario profiles;
- actions, requirements, results, state profiles, findings, evidence and consequence definitions;
- faction signatures, reactions, offers and territory-entry rules;
- scout focuses, report templates and outcomes;
- scene fragments, localization and visual assets.

New reusable content is allowed only if it remains useful outside this single map. Examples include
an abandoned-survey-camp content profile, a report fragment for a damaged route sketch, or a
faction warning fragment.

### 5.2 Tutorial fixture data

Add a single deterministic tutorial-definition document under the normal game-data root. It owns
only fixed campaign composition:

- map dimensions, seed/version and base coordinate;
- terrain/path overrides needed for the curated route;
- fixed placement and initial state of tutorial locations/landmarks;
- the initial roster/loadout and permitted base preparation availability;
- faction territory/warning placement for this map;
- route-beat identifiers used by smoke tests; and
- optional deterministic outcome selection only where a reproducible teaching result is essential.

It may reference reusable IDs but may not copy their rule/effect definitions. It contains no Unity
paths, no saved GameState and no player-facing prose that belongs in localization documents.

The fixture needs a typed loader, catalog validation and a `TutorialGameFactory` entry point. The
factory remains responsible only for assembling normal `GameState` from validated fixture data; it
must not interpret tutorial beat IDs to apply special rules.

## 6. Acceptance Tests

The tutorial smoke suite must prove the following through `SimulationSession` and ordinary command
paths:

0. The player-facing tutorial session begins in base preparation, with no active Expedition 1;
   selecting a valid team through the normal loadout command creates Expedition 1.

1. The Tutorial campaign loads the declared fixture deterministically and catalog validation rejects
   invalid fixture references or out-of-bounds placement.
2. The initial team is the approved eight-person composition and has no engineer.
3. The scout mission returns normal approximate player knowledge and does not disclose an exact
   remote location or faction identity.
4. The contact and warning beats are reachable without hidden-truth UI disclosure.
5. The legacy site produces a normal finding/archive/evaluation handoff, not direct Knowledge Point
   currency or a hardcoded tutorial reward.
6. The crossing's safe durable action is locked before Expedition 2 by the normal engineer
   requirement and the available action set retains a leave, mark or defer path.
7. Returning transfers the appropriate unsecured knowledge through the ordinary base loop.
8. A normal base action/loadout allows Expedition 2 to include the engineer.
9. The same crossing query exposes the changed legal action after that preparation; completion
   persists the ordinary location/route result.
10. Unity, WPF and headless execution query the same player-visible option IDs and known lock
    reasons for the equivalent tutorial state.

These tests do not prescribe a single player route. They verify that the intended evidence chain,
return decision and second-expedition difference are possible, fair and deterministic.

## 7. Implementation Sequence

1. Introduce and validate the typed tutorial-definition fixture without altering generic game rules.
2. Move the current hardcoded tutorial placement/composition into that fixture, preserving all
   existing baseline tests.
3. Reposition/author the four required beats and add the contact/legacy links described above.
4. Add the two-expedition smoke scenarios and use them to tune initial supplies, day cost and
   preparation availability. The current branch uses one fixed 12-Knowledge analysis reward for
   the abandoned-survey record solely to prove this chain; owner playtesting must confirm or
   replace that value before merge.
5. Add player-facing scenes, placeholders and restrained teaching copy only after the full path is
   mechanically possible.
6. Run owner-led playtests before treating text, pace or route geometry as final.

## 8. Owner Review Decisions Still Needed

The following are deliberately not guessed by implementation:

- exact desired difficulty of a risky first-expedition crossing;
- whether the tutorial starts with a known Coastal People contact or earns first contact in play;
- which concrete legacy clue is emotionally strongest and culturally appropriate;
- the target starting supplies/day consumption after a first end-to-end playthrough;
- whether the engineer is recruited, requested through Knowledge Points or unlocked by analysis;
- final German wording and visual tone of the field brief, warnings and base-return scenes.

Implementation can supply data-driven alternatives and test them, but these are player-facing
design decisions requiring owner review before the final tutorial path is frozen.
