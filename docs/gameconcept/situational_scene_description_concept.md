# Situational Scene Description Concept

Status: **Confirmed cross-system presentation direction / elaborated to an implementable
specification; inspection presentation exists as a precursor, shared scene system not yet
implemented. The JSON authoring, validation and localization foundation is implemented; runtime
scene resolution is not.**

This document defines the text-adventure-inspired presentation layer shared by locations,
contacts, scouts, reports and important expedition events. It does not create a second simulation
or permit presentation code to infer hidden truth.

Sections 1–4 define the direction. Sections 5–6 define the authoring and resolver contract.
Section 7 defines writing rules. Section 8 contains the curated MVP starter library. Sections 9–10
define validation and the implementation path.

## 1. Goal

The map brings the expedition to a situation. The scene description makes that situation tangible.
The player should receive a short atmospheric account of what the expedition can currently perceive
and then choose clear actions. Returning later should reveal how the place, people and expedition
history have changed.

The target rhythm is:

```text
arrive or receive someone
  -> read the current scene
  -> notice clues, condition and uncertainty
  -> choose an action
  -> see the immediate result
  -> return later to a scene that remembers what happened
```

Descriptions should evoke classic text adventures without becoming long unbroken prose or hiding
the actionable game state.

## 2. Applies Across the Game

Situational descriptions are a cross-system rule, not a special-location feature. They apply to:

- all seven location archetypes and their future variants;
- faction contacts, guards, messengers, traders and unknown groups;
- scouts returning normally, late, injured, shaken or without a companion;
- overdue and missing-scout states;
- expedition members reporting injuries, discoveries or internal conflict;
- important events, warnings, requests and delayed consequences;
- changed locations revisited by the same or a later expedition;
- return-to-base summaries, recovered findings and memorial entries.

Routine status displays may remain concise. Important communication and discoveries should use a
portrait, scene image or placeholder visual alongside the description.

## 3. Description Composition

A scene is assembled from small authored fragments:

```text
archetype mood and question
+ variant opening image
+ last known interaction / operational / presence state
+ explicitly observable modifiers
+ anonymous or identified actor relationship
+ visible history of earlier actions
+ current participant condition
+ timing and mission outcome context
= current player-facing scene
```

Not every layer appears every time. The resolver selects a small coherent set, removes duplicate or
contradictory fragments and orders concrete observation before interpretation.

### 3.1 Locations

A location description should answer:

- What does the expedition see, hear, smell or physically notice?
- What visible condition is the place in?
- What has visibly changed since the last visit?
- Are there signs of use, danger, ownership, observation or abandonment?
- Which uncertainty characteristic of the archetype remains unresolved?

The same bridge, grave, gate or natural phenomenon must describe its current state rather than reuse
one immutable paragraph.

### 3.2 Contacts

A contact scene may combine:

- number and visible arrangement of people;
- clothing, equipment, insignia and posture;
- apparent condition such as injured, exhausted or prepared;
- known faction identity or an explicitly anonymous description;
- current relationship and remembered expedition behaviour, but only through visible conduct;
- communication source: direct speaker, messenger, symbol, returned object or second-hand account.

Knowing that people are present does not automatically reveal who they are or why they are there.

### 3.3 Returning Scouts

A returned scout should first receive a short **return-state description**, followed by the actual
report. This makes the return feel like a person arriving rather than a report object appearing.

Possible layers include:

- arrival time and manner: at dusk, late, supported by a companion, alone;
- physical state: unhurt, exhausted, soaked, injured, visibly shaken;
- team state: both returned, one returned, one missing;
- carried evidence or visibly lost equipment;
- confidence and ability to report coherently;
- connection to previous missions and personal history where relevant.

Examples:

> Mira returns shortly before dusk, mud to her knees but otherwise unhurt. She keeps looking back
> toward the eastern ridge before beginning her report.

> Tovin reaches the camp alone and with a bandaged arm. Mara, who left with him, is not there. He
> needs a moment before he can explain where they became separated.

The description must not reveal an unseen ambush, hidden captor or true cause merely because the
simulation knows it. An absent scout remains absent until testimony or evidence supports more.

### 3.4 Reports and Events

The scene description and structured information have different jobs:

- scene description communicates presence, atmosphere and immediate condition;
- report body communicates testimony and observations;
- structured hints provide usable links, confidence and actions;
- journal/archive entries preserve chronology and provenance.

Atmospheric prose must not replace reliability labels, costs, known locks, urgency or actionable
controls.

## 4. Information Honesty

Every fragment declares what permits it to appear. Valid sources include:

- direct current observation;
- a previously stored `KnowledgeState` observation;
- an explicitly observable modifier;
- testimony from a named or anonymous source;
- known faction identity or recognised signature;
- visible traces of earlier expedition action;
- a report outcome and member condition actually delivered to the expedition.

Invalid sources include hidden `WorldState`, unobserved faction relations, secret modifier IDs,
undelivered event causes and omniscient narration.

Required progression:

```text
WorldState: faction X guards the place
first earned impression: someone appears to guard the place
identified signs: the guards or signs are associated with faction X
confirmed communication: faction X states or demonstrates its claim
```

Descriptions may suggest interpretation but must distinguish observation from certainty. Phrases
such as *seems*, *suggests*, *cannot yet be identified* and *according to the scout* are useful when
they accurately reflect the knowledge state.

### 4.1 Mapping to Runtime Knowledge

Information honesty is enforceable because every declared source maps to a concrete predicate over
Core state, including the presentation-supporting persistent facts specified in 5.6. The resolver
checks the predicate; a fragment whose predicate fails is ineligible regardless of its other
conditions.

| Source kind | Resolver predicate (existing Core state) |
| --- | --- |
| `direct-observation` | The subject is being observed in this very scene: the expedition stands at the location, performs the inspection, or receives the person right now. |
| `stored-observation` | A `LocationConditionKnowledgeState` exists for the location in `KnowledgeState` (last observed interaction/operational/presence states, observation day, doubtful flag). |
| `observable-modifier` | The modifier was revealed to the player through inspection and is part of the stored or current observation. Its `inspectionText` exists. |
| `visible-trace` | An expedition-caused observable mark exists: `modifier-previously-visited`, `modifier-changed-by-expedition`, or an archive-linked own action at this location. |
| `testimony` | A delivered `ScoutReportState`, contact statement or second-hand account references the subject. The fragment inherits the report's reliability for labelling. |
| `signature-identification` | An earned context tag (`KnowledgeState.KnowsLocationContextTag`) or evidence item links a recognised signature to the subject. Never the generated claim itself. |
| `delivered-outcome` | A mission or expedition outcome has actually been delivered. The source is the immutable delivered outcome snapshot (5.6), not a later reconstruction from mutable member state or the current world day. |

Source predicates form a one-way evidence lattice where explicitly stated: an authorized current
direct observation may satisfy a fragment authored for `stored-observation`, because it provides
the same known condition with stronger and fresher provenance. The result records the actual
provenance as `direct-observation`. Stored knowledge can never satisfy a `direct-observation`
fragment. No other source substitutions are implicit.

Age and doubt are part of honesty: when the stored observation is marked doubtful
(`LocationConditionKnowledgeState.IsDoubtful`) or older than the fragment's `maxKnownStateAgeDays`,
the fragment is skipped unless it explicitly tolerates doubt, and the resolver adds the stale-
knowledge history fragment instead (see 8.2.5). `KnowledgeLevel.OldOrDoubtful` on the tile has the
same effect for map-level scenes.

## 5. Data-Driven Authoring Contract

### 5.1 Documents and Directory Layout

Scene content is cross-system and gets its own document family, following the common envelope
(`documentType`, `schemaVersion`, `contentVersion`, `items`) and manifest registration from the
cross-system JSON authoring schema:

```text
Assets/StreamingAssets/GameData/
  Scenes/
    scene-policies.json            # documentType: scene-policies
    location-fragments.json        # documentType: scene-fragments
    contact-fragments.json         # documentType: scene-fragments
    scout-return-fragments.json    # documentType: scene-fragments
    report-event-fragments.json    # documentType: scene-fragments
    Locales/
      de.json                      # documentType: scene-localization; current default
```

Any number of `scene-fragments` documents may exist; the loader merges them into one catalogue.
Splitting by subject kind keeps authoring files reviewable. All IDs are stable, lowercase
kebab-case and never reused.

Each `scene-fragments` document may declare `defaultsByGroup`. Defaults are ordinary schema fields
applied before validation; an item value overrides its group default, and nested objects are merged
by field. The loader immediately expands every item into a complete effective fragment. Runtime
code never reads defaults. `subjectKind`, `source` and `priority` must exist after expansion;
`appliesTo`, `when`, `supersedesFragmentIds`, `exclusiveTag` and `visualId` receive the explicit
empty defaults shown below, while `repetition` defaults to `always`:

```json
{
  "defaultsByGroup": {
    "operational-state": {
      "subjectKind": "location",
      "source": { "kind": "stored-observation", "maxKnownStateAgeDays": null, "toleratesDoubt": false },
      "priority": 50,
      "repetition": "always",
      "appliesTo": { "archetypeIds": [], "variantIds": [] },
      "when": {},
      "supersedesFragmentIds": [],
      "exclusiveTag": null,
      "visualId": null
    }
  }
}
```

This is authoring normalization, not permissive runtime fallback. A fragment still fails validation
when a required field is absent after expansion.

Modifiers are deliberately **not** re-authored: the existing `inspectionText` in `modifiers.json`
is the modifier fragment. The resolver renders it in the `modifier` ordering group with source
`observable-modifier`. This keeps one source of truth for modifier prose. Modifiers without
`inspectionText` simply contribute no scene text (the validator warns, see Section 9).

### 5.2 Fragment Schema

```json
{
  "id": "frag-loc-route-op-blocked",
  "group": "operational-state",
  "subjectKind": "location",
  "appliesTo": {
    "archetypeIds": ["route-obstacle"],
    "variantIds": []
  },
  "when": {
    "knownOperationalStatesAny": ["blocked"]
  },
  "source": { "kind": "stored-observation", "maxKnownStateAgeDays": null, "toleratesDoubt": false },
  "priority": 50,
  "repetition": "always",
  "supersedesFragmentIds": [],
  "exclusiveTag": null,
  "visualId": null,
  "textId": "scene.fragment.route.operational.blocked"
}
```

Field reference:

- **`id`** — stable kebab-case ID, prefixed `frag-`.
- **`group`** — one of the ordering groups: `opening`, `interaction-state`, `operational-state`,
  `presence-state`, `modifier` (reserved for the derived modifier texts, not authorable),
  `relation-anonymous`, `relation-identified`, `history`, `member-condition`, `scout-return`,
  `report-transition`, `base-return`. The archetype question is not a fragment; it lives in the
  policy (5.4).
- **`subjectKind`** — `location`, `contact`, `scout-return`, `report-event` or `base-return`.
- **`appliesTo`** — optional narrowing by `archetypeIds` and `variantIds`. Fragments must never
  reference a generated location instance ID or a generated faction instance ID. Variant-level
  authoring is allowed (a broken bridge may have its own opening); instance-level is not.
- **`when`** — condition object, see 5.3. All present fields are combined with AND; values inside
  an array are alternatives (OR). An absent field does not constrain.
- **`source`** — required honesty declaration (Section 4.1). `maxKnownStateAgeDays` and
  `toleratesDoubt` refine `stored-observation`. Exactly one source kind per fragment; a sentence
  that would need two sources is two fragments.
- **`priority`** — integer; higher wins when the scene cap forces a choice within a group.
- **`repetition`** — `always` (default) or `once-per-state` (shown once until the referenced state
  changes). Cooldown-based variation is deliberately post-MVP (Section 12).
- **`supersedesFragmentIds`** — when both are eligible, the superseding fragment removes the listed
  ones (e.g. a variant-specific opening supersedes the archetype-generic one).
- **`exclusiveTag`** — at most one eligible fragment per tag appears in a scene; the resolver keeps
  the highest priority. Used for mutually exclusive interpretations.
- **`visualId`** — optional portrait/scene image reference for the scene header.
- **`textId`** — stable localization key. The prose lives only in `Scenes/Locales/*.json`, never in
  C#, a client prefab/control or the rule document. Placeholders in localized text are substituted
  from delivered runtime data; the allowed set per subject kind is: `{memberName}`,
  `{companionName}` (scout-return, report-event), `{daysOverdue}` (scout-return). Every locale must
  preserve the default text's placeholder set. Text never contains numbers from hidden state, exact
  coordinates or generated instance names.

### 5.3 Condition Model (`when`)

All fields are optional; each maps to player-known state only:

| Field | Meaning / backing state |
| --- | --- |
| `knownInteractionStatesAny` | Last known interaction state of the location (`LocationConditionKnowledgeState.InteractionStateId`), or the state just observed on arrival. |
| `knownOperationalStatesAny` | Same for the operational channel. |
| `knownPresenceStatesAny` | Same for the presence channel. |
| `knownContextTagsAny` | Earned context conclusions (`KnowledgeState.KnownLocationContextTags`). |
| `observableModifierIdsAny` | Modifiers revealed to the player at this location. |
| `observableRelationKindsAny` | Relation categories made observable by the current evidence (`claimed`, `watched`, `sacred`, `guarded`, `connected`); never raw relations from `WorldState`. |
| `knowledgeLevelAtLeast` | Tile/location `KnowledgeLevel` (`Reported` or `Confirmed`). |
| `requiresDoubtfulLastObservation` / `forbidDoubtfulLastObservation` | The stored observation's doubtful flag. |
| `maxKnownStateAgeDays` | Days since `LastObservedWorldDay`. Alias for the value on a `stored-observation` source; declaring both with different values is an error. New content should prefer the source field. |
| `contactStatusAny` | `FactionContactStatus` values: `Unknown`, `Rumored`, `Contacted`, `Open`, `Hostile`. |
| `identityStage` | `anonymous`, `signature-recognised` or `identified` — derived, see below. |
| `missionStatusAny` | `ScoutMissionStatus` values: `Returned`, `Overdue`, `ReturnedInjured`, `Missing`. |
| `memberStatusAny` | `ExpeditionMemberStatus` values: `Injured`, `Exhausted`, `Missing`, `Dead`, plus derived `unhurt`. |
| `teamOutcome` | `all-returned`, `partial-return`, `none-returned` — derived from the mission's member list vs. actual returns. |
| `reportReliabilityAtLeast` / `reportReliabilityBelow` | The delivered report's `Reliability` (0–100). |
| `hasFindings` / `hasLeads` | Whether the delivered report or return carries findings/leads. |
| `wasOverdue` | Immutable fact captured when a scout outcome is delivered: `ActualReturnWorldDay > ExpectedReturnWorldDay`. It is never recalculated from the current world day. |
| `hasLostEquipment` | Whether the delivered scout/expedition outcome snapshot contains at least one lost equipment ID. |
| `isSecondHandAccount` | Whether the delivered report explicitly marks its information as testimony rather than the scout's own observation. |
| `isUrgent` | Whether the delivered report/event carries the authored or resolved urgency flag. Presentation never infers urgency from prose. |
| `hasOwnArchiveEntryForLocation` | Whether an archive entry with `SubjectKind = location`, matching `SubjectId`, and an expedition-owned source action exists. |
| `currentObservationDiffersFromStored` | Whether the trigger's newly delivered observation differs from `PreviousKnownCondition` in at least one condition channel. Evaluated before knowledge commit (6.1). |
| `hasLostExpeditionRecordForLocation` | Whether a lost-expedition record has a structured matching `SubjectLocationId`; coordinate or title matching is not permitted. |

**Identity stage derivation.** The resolver computes the stage from earned knowledge only:

```text
anonymous            no earned link between the observed people/signs and any known group
signature-recognised an earned signature/context tag links them to signs seen before,
                     but no confirmed communication has established who they are
identified           FactionContactStatus is Contacted, Open or Hostile for the matched group
```

A `Rumored` contact never yields `identified`: testimony may establish that a group is said to
exist, but not that the people currently observed are that group. It may produce
`signature-recognised` only when an earned signature link exists. A fragment with
`identityStage: "identified"` must use source `signature-identification` or
`testimony` (or `delivered-outcome` for concluded interactions); the validator enforces this
pairing (Section 9).

### 5.4 Scene Policies

`scene-policies.json` defines per subject kind (and per archetype for locations) the characteristic
question, ordering, caps and paragraphing. Policies are the only place that knows presentation
order; fragments never reference each other positionally.

```json
{
  "id": "policy-location-route-obstacle",
  "subjectKind": "location",
  "archetypeId": "route-obstacle",
  "questionTextId": "scene.question.route-obstacle",
  "questionResolvedWhen": { "knownInteractionStatesAny": ["inspected"], "knownOperationalStatesAny": ["open", "repaired"] },
  "ordering": ["opening", "operational-state", "presence-state", "modifier", "history", "relation-anonymous", "relation-identified", "interaction-state"],
  "paragraphing": [["opening"], ["operational-state", "presence-state", "modifier"], ["history", "relation-anonymous", "relation-identified", "interaction-state"]],
  "maxFragments": 6,
  "maxPerGroup": { "modifier": 2, "history": 2 }
}
```

- **`questionTextId`** — localization key rendered as a closing line (visually set apart, e.g. italic) while the archetype's
  characteristic uncertainty is unresolved. `questionResolvedWhen` uses the same condition model as
  fragments; when it matches, the question is dropped.
- **`ordering`** — the group order for this subject kind. Groups not listed never render.
- **`paragraphing`** — groups sharing an inner array join into one paragraph; arrays separate
  paragraphs. Empty paragraphs collapse.
- **`maxFragments` / `maxPerGroup`** — hard caps. When exceeded, lowest priority drops first,
  ties broken deterministically (6.3).

### 5.5 Relationship to Content Profiles

`content-profiles.json` keeps `title`, `subtitle`, `imageId` and `journalText` — identity and
archive wording. Its `flavorByState` dictionary is the predecessor of the state fragment groups and
is **migrated into fragments and then retired** (work package 6 in Section 10). `shortDescription`
remains for map tooltips and list rows; the scene opening replaces `description`.

### 5.6 Presentation-Supporting Persistent Facts

The resolver must not reconstruct historical truth from mutable current state. The following small,
structured records are therefore prerequisites for the affected scene types and are part of the
save model:

```text
DeliveredMissionOutcome {
  missionId
  expectedReturnWorldDay
  actualReturnWorldDay?
  wasOverdue
  participantOutcomes[] { memberId, returned, deliveredStatus }
  lostEquipmentIds[]
  deliveredReportIds[]
}

ArchiveSubjectReference {
  subjectKind           location | contact | mission | expedition | event
  subjectId             stable ID
  sourceActionId?
  sourceExpeditionId?
}

SceneRepetitionEntry {
  subjectKind
  subjectRef
  fragmentId
  stateKey
}
```

`teamOutcome`, `wasOverdue`, member return conditions and equipment loss are derived only from
`DeliveredMissionOutcome`. A member's later recovery, injury or reassignment must not rewrite the
historical return scene. Archive entries carry `ArchiveSubjectReference`; archive text, titles and
coordinates are never parsed to establish a relationship. `LostExpeditionRecord` receives a
structured `SubjectLocationId` where the loss is tied to a location.

These records do not add new gameplay rules, but they do change persistent data. Their introduction
requires an explicit save-version increment, backward-compatible defaults for older saves, save/load
roundtrip tests and human review before merge. Missing fields in an older save mean "not recorded";
they must not be guessed from the load-day state.

### 5.7 Localization Contract

Scene logic and localized prose are separate authored documents. Fragments and policies contain
stable text IDs; locale documents contain the actual strings:

```json
{
  "documentType": "scene-localization",
  "schemaVersion": 1,
  "contentVersion": 1,
  "locale": "de",
  "fallbackLocale": null,
  "isDefault": true,
  "items": [
    {
      "id": "scene.fragment.route.operational.blocked",
      "text": "Der Weg ist an dieser Stelle vollständig unterbrochen; ohne Eingriff kommt hier niemand hinüber."
    }
  ]
}
```

Exactly one loaded locale is the default; for the MVP it is `de`. Locale lookup uses the requested
locale, then its language/fallback chain, then the default locale. Missing default-locale text is a
load error. Adding a language requires only a new locale JSON plus its manifest entry; rule files,
fragment IDs, resolver code and client code remain unchanged. Translations must preserve placeholder
names exactly. German content may use proper UTF-8 umlauts now that it lives in a dedicated locale
document.

## 6. Resolver and Architecture

`Game.Core` owns state and player knowledge. `Game.App` owns a shared scene-description projection
that selects authored fragments from the state the player is allowed to know. Unity and WPF render
the returned title, paragraphs, visual reference, known statuses and actions unchanged.

The resolver must not branch on a concrete location ID, faction ID, bridge, grave, gate, Unity panel
or WPF control. Archetype policies define the characteristic question and fragment ordering;
variants and modifiers provide authored content through data.

### 6.1 Scene Request

A scene is requested by the command/result layer, never self-initiated by rendering code:

```text
SceneRequest {
  subjectKind          location | contact | scout-return | report-event | base-return
  subjectRef           location instance ID / contact interaction ID / mission ID / report ID
  trigger              arrival | inspection-result | remote-location-view | mission-return |
                       event-delivery | base-return
  currentWorldDay
}
```

The resolver reads `GameState` through a read-only view that exposes only: `KnowledgeState`, the
delivered report/outcome data, the location's player-visible condition for the current trigger,
revealed modifiers, and the authored catalogue. It has no access to `WorldState` context tags,
generated claims, hidden modifiers or undelivered outcomes. This restriction is structural (a
dedicated view type), not a convention.

For `arrival` and `inspection-result`, the application layer creates the view with both
`PreviousKnownCondition` and the authorized `CurrentDeliveredObservation`. The resolver compares
and renders those values before the command commits the new observation to `KnowledgeState`; the
commit happens afterward in the same application transaction. A remote view receives only the
stored observation and cannot satisfy `direct-observation`. It therefore uses a stored-knowledge
opening policy and never combines fresh sensory prose with stale operational state. If a current
condition is plainly observable on arrival, that delivered observation takes precedence over the
stored condition in the scene; the old condition is retained only for history/comparison fragments.

### 6.2 Selection Algorithm

```text
1. Resolve policy       subjectKind (+ archetype for locations); missing policy => error, no scene.
2. Collect candidates   all fragments with matching subjectKind and appliesTo.
3. Source check         evaluate the source predicate (4.1); drop failures.
4. Condition check      evaluate `when` against the read-only view; drop failures.
5. Derive modifiers     for locations: turn each revealed modifier's inspectionText into a
                        synthetic fragment in group `modifier` (priority = 50, source already
                        verified by revelation).
6. Conflict resolution  apply supersedesFragmentIds, then exclusiveTag (keep highest priority),
                        then repetition policy (drop `once-per-state` fragments already shown for
                        the current state key).
7. Caps                 enforce maxPerGroup, then maxFragments: drop lowest priority first;
                        never drop the last remaining `opening` or `scout-return` fragment.
8. Order and assemble   sort by policy ordering, join per paragraphing rules; append the policy
                        question if questionResolvedWhen does not match.
9. Annotate             attach per-paragraph provenance (source kinds), reliability labels from
                        testimony sources, and condition labels from delivered member states.
```

### 6.3 Determinism and Repetition

The MVP is fully deterministic: same knowledge, same states, same scene. Priority ties are broken
by a stable hash of `(subjectRef, fragmentId)` — not by unseeded randomness — so two clients render
identical scenes and snapshot tests are stable.

`once-per-state` tracking is presentation-supporting state: a small set of
`(subjectRef, fragmentId, stateKey)` entries stored with the save (it must survive reload, since a
"you have seen this" fact is player-visible). `stateKey` is the tuple of channel states the
fragment's conditions reference. Text variation pools, weighted alternatives and cooldowns are
deliberately post-MVP (Section 12); the schema stays open for them (a future `variants` array on
the fragment).

The chosen MVP rule is to persist this set, not to approximate it per session. It lives in a
dedicated presentation-support section of the save and is mutated only by the application layer
after a scene has been successfully delivered. The resolver itself remains pure: it receives the
existing entries and returns the entries to append. Unknown fragment IDs in older saves are ignored
so content removal does not break loading. Save migration, versioning and roundtrip coverage are
part of work package 0; no `once-per-state` content may ship before that package is complete.

### 6.4 Presentation Result Contract

```text
SceneDescriptionResult {
  title                  from content profile / report template
  subtitle?              content profile subtitle or subject-kind label
  visualId?              scene image or portrait reference (fragment visualId wins over profile imageId)
  subjectIdentity {
    label                as known: "Unbekannte Gruppe", signature label, or faction presentation label
    identityStage        anonymous | signature-recognised | identified
    portraitId?
  }
  paragraphs [
    { text, fragmentIds[], provenance[] }     provenance: source kinds contributing to the paragraph
  ]
  question?              the policy question, when unresolved
  conditionLabels[]      e.g. member states actually delivered: "verletzt", "erschoepft"
  reliabilityLabels[]    from testimony sources: reliability band + origin ("laut Bericht von ...")
  knowledgeAgeLabel?     "zuletzt bestaetigt vor N Tagen" when built from stored observation
  actions[]              pass-through of the existing action projections; the resolver never
                         creates, filters or reorders actions
}
```

The result is a projection, not authoritative state and not a replacement for events, reports or
the command system. Unity and WPF render it unchanged; neither may append information of its own.

### 6.5 Placement

- `Game.App/SceneDescriptionResolver.cs` — pure selection/assembly, unit-testable without Unity.
- `Game.App/SceneDescriptionCatalog` — loaded fragments and policies (via `CrossSystemDataLoader`
  patterns and the manifest).
- Command results (`InspectLocationResult`, mission completion, event delivery, expedition return)
  carry or expose the `SceneRequest` trigger; the application layer attaches the resolved
  `SceneDescriptionResult`.
- Rendering: one shared view model in each client; no client-side text logic beyond layout.

## 7. Writing Rules

- Prefer two or three concrete sentences over a long lore paragraph.
- Lead with sensory observation, then visible condition, then uncertainty.
- Use active verbs and specific nouns.
- Avoid repeating the title or the same modifier in several sentences.
- Do not describe a place as untouched after it was changed.
- Do not describe a returned scout as healthy when the member state says injured or exhausted.
- Do not name an unknown faction, speaker, cause or location purpose.
- Let different archetypes retain different questions and tone.
- Keep actions, costs and locks visually separate and easy to scan.

Additions for fragment authoring:

- One fragment carries one observation. A sentence that mixes state and interpretation is split.
- Fragment prose is authored in locale documents. German scene text uses normal UTF-8 spelling and
  umlauts; rule documents contain only stable `textId` references.
- Maximum two sentences, target under 220 characters. Openings may use the full budget; state
  fragments should stay to one sentence where possible.
- No mechanics in prose: no percentages, no risk numbers, no action names, no state IDs.
- Interpretation words (*wirkt*, *deutet darauf hin*, *offenbar*, *laut Bericht*) are required
  whenever the source is not direct observation of the fact itself.
- Placeholders only from the allowed set (5.2); write sentences that survive any member name.

## 8. MVP Starter Library

Curated German starter content for the existing Vertical Slice: the seven archetypes/state
profiles, the eight content profiles (Bruecke, Grab, Lager, Grenzwarnung, Tor, Kontaktort, Moor,
Gipfel) and the existing status enums. This library is the authoring draft; it becomes the initial
`Scenes/*.json` files in work package 1 and is not runtime data yet.

All state values referenced below exist in `state-profiles.json`; all modifier IDs in
`modifiers.json`; all enum values in Core. For editorial readability the library blocks below keep
`text` beside their rules. Runtime materialization replaces each such field with a stable `textId`
and places the prose in the locale document from 5.7. The item blocks otherwise omit only fields
supplied by the following normative `defaultsByGroup` declarations. Consequently every loaded
starter item expands to the complete schema from 5.2; there are no prose-only runtime defaults.

```json
{
  "location-fragments.json": {
    "opening": { "subjectKind": "location", "source": { "kind": "direct-observation" }, "priority": 80 },
    "operational-state": { "subjectKind": "location", "source": { "kind": "stored-observation" }, "priority": 50 },
    "presence-state": { "subjectKind": "location", "source": { "kind": "stored-observation" }, "priority": 55 },
    "interaction-state": { "subjectKind": "location", "source": { "kind": "stored-observation" }, "priority": 40 },
    "history": { "subjectKind": "location", "priority": 45 }
  },
  "contact-fragments.json": {
    "relation-anonymous": { "subjectKind": "contact", "source": { "kind": "direct-observation" }, "priority": 70, "when": { "identityStage": "anonymous" } },
    "relation-identified": { "subjectKind": "contact", "priority": 75 }
  },
  "scout-return-fragments.json": {
    "scout-return": { "subjectKind": "scout-return", "source": { "kind": "delivered-outcome" }, "priority": 70 },
    "member-condition": { "subjectKind": "scout-return", "source": { "kind": "delivered-outcome" }, "priority": 60 }
  },
  "report-event-fragments.json": {
    "report-transition": { "subjectKind": "report-event", "source": { "kind": "delivered-outcome" }, "priority": 40 },
    "base-return": { "subjectKind": "base-return", "source": { "kind": "delivered-outcome" }, "priority": 60 }
  }
}
```

Each actual rule file repeats its own relevant declaration inside its valid document envelope; the
combined block above is a compact specification of those four envelopes. Work package 1 must not
copy a bare item array without its defaults, and it must move every editorial `text` to a locale
entry referenced by `textId`.

### 8.1 Scene Policies

```json
{
  "documentType": "scene-policies",
  "schemaVersion": 1,
  "contentVersion": 1,
  "items": [
    {
      "id": "policy-location-route-obstacle",
      "subjectKind": "location",
      "archetypeId": "route-obstacle",
      "question": "Wie kommen wir hindurch, darum herum oder sicher zurueck?",
      "questionResolvedWhen": { "knownOperationalStatesAny": ["open", "repaired"] },
      "ordering": ["opening", "operational-state", "presence-state", "modifier", "history", "relation-anonymous", "relation-identified", "interaction-state"],
      "paragraphing": [["opening"], ["operational-state", "presence-state", "modifier"], ["history", "relation-anonymous", "relation-identified", "interaction-state"]],
      "maxFragments": 6,
      "maxPerGroup": { "modifier": 2, "history": 2 }
    },
    {
      "id": "policy-location-investigation-site",
      "subjectKind": "location",
      "archetypeId": "investigation-site",
      "question": "Was ist hier geschehen, und was laesst sich davon noch sichern?",
      "questionResolvedWhen": { "knownInteractionStatesAny": ["documented"] },
      "ordering": ["opening", "operational-state", "presence-state", "modifier", "history", "relation-anonymous", "relation-identified", "interaction-state"],
      "paragraphing": [["opening"], ["operational-state", "presence-state", "modifier"], ["history", "relation-anonymous", "relation-identified", "interaction-state"]],
      "maxFragments": 6,
      "maxPerGroup": { "modifier": 2, "history": 2 }
    },
    {
      "id": "policy-location-territorial-marker",
      "subjectKind": "location",
      "archetypeId": "territorial-marker",
      "question": "Wessen Zeichen ist das, und was verlangt es von uns?",
      "questionResolvedWhen": { "knownInteractionStatesAny": ["interpreted", "documented"] },
      "ordering": ["opening", "operational-state", "presence-state", "modifier", "history", "relation-anonymous", "relation-identified", "interaction-state"],
      "paragraphing": [["opening"], ["operational-state", "presence-state", "modifier"], ["history", "relation-anonymous", "relation-identified", "interaction-state"]],
      "maxFragments": 6
    },
    {
      "id": "policy-location-contact-site",
      "subjectKind": "location",
      "archetypeId": "contact-site",
      "question": "Wer ist hier, und welche Absicht steht hinter der Begegnung?",
      "questionResolvedWhen": { "contactStatusAny": ["Contacted", "Open", "Hostile"] },
      "ordering": ["opening", "presence-state", "operational-state", "relation-anonymous", "relation-identified", "modifier", "history", "interaction-state"],
      "paragraphing": [["opening"], ["presence-state", "operational-state", "relation-anonymous", "relation-identified"], ["modifier", "history", "interaction-state"]],
      "maxFragments": 6
    },
    {
      "id": "policy-location-hazard-site",
      "subjectKind": "location",
      "archetypeId": "hazard-site",
      "question": "Wie weit reicht die Gefahr, und was duerfen wir riskieren?",
      "questionResolvedWhen": { "knownOperationalStatesAny": ["restricted", "contained"] },
      "ordering": ["opening", "operational-state", "presence-state", "modifier", "history", "interaction-state"],
      "paragraphing": [["opening"], ["operational-state", "presence-state", "modifier"], ["history", "interaction-state"]],
      "maxFragments": 6
    },
    {
      "id": "policy-location-containment-site",
      "subjectKind": "location",
      "archetypeId": "containment-site",
      "question": "Warum wurde dieser Ort verschlossen, und was haelt der Verschluss zurueck?",
      "questionResolvedWhen": { "knownContextTagsAny": ["containment-purpose-known"] },
      "ordering": ["opening", "operational-state", "presence-state", "modifier", "history", "relation-anonymous", "relation-identified", "interaction-state"],
      "paragraphing": [["opening"], ["operational-state", "presence-state", "modifier"], ["history", "relation-anonymous", "relation-identified", "interaction-state"]],
      "maxFragments": 6
    },
    {
      "id": "policy-location-natural-phenomenon",
      "subjectKind": "location",
      "archetypeId": "natural-phenomenon",
      "question": "Was verraet uns dieser Ort ueber das Land ringsum?",
      "questionResolvedWhen": { "knownOperationalStatesAny": ["mapped"] },
      "ordering": ["opening", "operational-state", "presence-state", "modifier", "history", "interaction-state"],
      "paragraphing": [["opening"], ["operational-state", "presence-state", "modifier"], ["history", "interaction-state"]],
      "maxFragments": 6
    },
    {
      "id": "policy-contact-event",
      "subjectKind": "contact",
      "ordering": ["relation-anonymous", "relation-identified", "member-condition", "history", "report-transition"],
      "paragraphing": [["relation-anonymous", "relation-identified"], ["member-condition", "history"], ["report-transition"]],
      "maxFragments": 5
    },
    {
      "id": "policy-scout-return",
      "subjectKind": "scout-return",
      "ordering": ["scout-return", "member-condition", "report-transition"],
      "paragraphing": [["scout-return", "member-condition"], ["report-transition"]],
      "maxFragments": 5
    },
    {
      "id": "policy-report-event",
      "subjectKind": "report-event",
      "ordering": ["report-transition", "member-condition"],
      "paragraphing": [["report-transition", "member-condition"]],
      "maxFragments": 3
    },
    {
      "id": "policy-base-return",
      "subjectKind": "base-return",
      "ordering": ["base-return", "member-condition", "report-transition"],
      "paragraphing": [["base-return"], ["member-condition"], ["report-transition"]],
      "maxFragments": 5
    }
  ]
}
```

Note: `containment-purpose-known` is a placeholder context tag; the actual tag comes from the
containment scenario's context definitions when they are authored.

### 8.2 Location Fragments (`location-fragments.json`)

#### 8.2.1 Openings (per variant, source `direct-observation`, priority 80)

Variant openings supersede nothing yet (no generic archetype openings in the MVP); each is the
mandatory first paragraph on arrival.

```json
[
  { "id": "frag-open-broken-bridge", "group": "opening", "subjectKind": "location",
    "appliesTo": { "variantIds": ["broken-bridge"] }, "source": { "kind": "direct-observation" }, "priority": 80,
    "text": "Geborstene Balken haengen schraeg ueber der Schlucht, und unten reisst das Wasser an den gestuerzten Traegern. Der Wind draengt in Boeen ueber den offenen Spalt." },
  { "id": "frag-open-marked-grave", "group": "opening", "subjectKind": "location",
    "appliesTo": { "variantIds": ["marked-grave"] }, "source": { "kind": "direct-observation" }, "priority": 80,
    "text": "Aufgeschichtete Steine und geschnitzte Zeichen fassen die Grabstelle sauber ein. Es riecht nach feuchter Erde, und jemand hat hier lange Sorgfalt aufgewendet." },
  { "id": "frag-open-abandoned-camp", "group": "opening", "subjectKind": "location",
    "appliesTo": { "variantIds": ["abandoned-camp"] }, "source": { "kind": "direct-observation" }, "priority": 80,
    "text": "Eine kalte Feuerstelle, verstreute Ausruestung und in den Boden getretene Spuren erzaehlen von einem hastigen Aufbruch. Ueber dem Platz liegt es still." },
  { "id": "frag-open-border-warning", "group": "opening", "subjectKind": "location",
    "appliesTo": { "variantIds": ["border-warning-sign"] }, "source": { "kind": "direct-observation" }, "priority": 80,
    "text": "Geschnitzte Pfaehle stehen in gleichmaessigen Abstaenden am Wegrand, die Zeichen darauf frisch nachgezogen. Wer sie gesetzt hat, wollte gesehen werden." },
  { "id": "frag-open-sealed-gate", "group": "opening", "subjectKind": "location",
    "appliesTo": { "variantIds": ["sealed-gate"] }, "source": { "kind": "direct-observation" }, "priority": 80,
    "text": "Schwere Riegel und mehrfach gesicherte Verschluesse halten das alte Tor zu. Die verwitterten Zeichen darauf wirken weniger wie Schmuck als wie eine Warnung." },
  { "id": "frag-open-first-contact", "group": "opening", "subjectKind": "location",
    "appliesTo": { "variantIds": ["first-contact"] }, "source": { "kind": "direct-observation" }, "priority": 80,
    "text": "Der Rastplatz ist bewusst hergerichtet: freigehaltener Boden, ein Zeichen auf Augenhoehe, ein Platz zum Sitzen. Jemand rechnet hier mit Besuchern." },
  { "id": "frag-open-restless-marsh", "group": "opening", "subjectKind": "location",
    "appliesTo": { "variantIds": ["restless-marsh"] }, "source": { "kind": "direct-observation" }, "priority": 80,
    "text": "Dunst haengt zwischen den Grasinseln, und der Boden gibt unter jedem Schritt anders nach. Aus dem dunklen Wasser steigen traege Blasen auf." },
  { "id": "frag-open-wind-carved-peak", "group": "opening", "subjectKind": "location",
    "appliesTo": { "variantIds": ["wind-carved-peak"] }, "source": { "kind": "direct-observation" }, "priority": 80,
    "text": "Die Felsnadel ragt weithin sichtbar ueber die Wege, vom Wind zu einer glatten, unverwechselbaren Form geschliffen. Von hier oben muesste sich das Land neu ordnen lassen." }
]
```

#### 8.2.2 Operational-State Fragments (source `stored-observation`, priority 60)

Written variant-neutral per archetype so future variants reuse them; a variant may later add a
superseding specific fragment.

```json
[
  { "id": "frag-loc-route-op-blocked", "group": "operational-state", "appliesTo": { "archetypeIds": ["route-obstacle"] },
    "when": { "knownOperationalStatesAny": ["blocked"] },
    "text": "Der Weg ist an dieser Stelle vollstaendig unterbrochen; ohne Eingriff kommt hier niemand hinueber." },
  { "id": "frag-loc-route-op-provisional", "group": "operational-state", "appliesTo": { "archetypeIds": ["route-obstacle"] },
    "when": { "knownOperationalStatesAny": ["provisional"] },
    "text": "Eine behelfsmaessige Passage ueberspannt das Hindernis. Sie traegt, aber niemand wuerde ihr schwere Lasten anvertrauen." },
  { "id": "frag-loc-route-op-risky", "group": "operational-state", "appliesTo": { "archetypeIds": ["route-obstacle"] },
    "when": { "knownOperationalStatesAny": ["risky-passage"] },
    "text": "Ein Uebergang ist moeglich, doch jeder Schritt darauf bleibt ein Risiko." },
  { "id": "frag-loc-route-op-open", "group": "operational-state", "appliesTo": { "archetypeIds": ["route-obstacle"] },
    "when": { "knownOperationalStatesAny": ["open"] },
    "text": "Der Weg ist frei; vom frueheren Hindernis bleibt nur, was am Rand liegt." },
  { "id": "frag-loc-route-op-repaired", "group": "operational-state", "appliesTo": { "archetypeIds": ["route-obstacle"] },
    "when": { "knownOperationalStatesAny": ["repaired"] },
    "text": "Der Uebergang ist instand gesetzt und traegt wieder." },
  { "id": "frag-loc-route-op-destroyed", "group": "operational-state", "appliesTo": { "archetypeIds": ["route-obstacle"] },
    "when": { "knownOperationalStatesAny": ["destroyed"] },
    "text": "Vom frueheren Uebergang ist nichts Brauchbares geblieben." },

  { "id": "frag-loc-grave-op-sealed", "group": "operational-state", "appliesTo": { "archetypeIds": ["investigation-site"] },
    "when": { "knownOperationalStatesAny": ["sealed"] },
    "text": "Die Stelle ist verschlossen, die Abdeckung unversehrt." },
  { "id": "frag-loc-grave-op-open", "group": "operational-state", "appliesTo": { "archetypeIds": ["investigation-site"] },
    "when": { "knownOperationalStatesAny": ["open"] },
    "text": "Die Stelle liegt offen." },
  { "id": "frag-loc-grave-op-disturbed", "group": "operational-state", "appliesTo": { "archetypeIds": ["investigation-site"] },
    "when": { "knownOperationalStatesAny": ["disturbed"] },
    "text": "Die Anordnung der Steine und Zeichen ist sichtbar gestoert worden." },

  { "id": "frag-loc-marker-op-standing", "group": "operational-state", "appliesTo": { "archetypeIds": ["territorial-marker"] },
    "when": { "knownOperationalStatesAny": ["standing"] },
    "text": "Die Markierung steht unveraendert und wird offenbar instand gehalten." },
  { "id": "frag-loc-marker-op-respected", "group": "operational-state", "appliesTo": { "archetypeIds": ["territorial-marker"] },
    "when": { "knownOperationalStatesAny": ["respected"] },
    "text": "Die Expedition ist bisher vor der erkennbaren Grenze geblieben." },
  { "id": "frag-loc-marker-op-violated", "group": "operational-state", "appliesTo": { "archetypeIds": ["territorial-marker"] },
    "when": { "knownOperationalStatesAny": ["violated"] },
    "text": "Die Grenze wurde ueberschritten. Wer die Zeichen gesetzt hat, koennte das erkennen." },

  { "id": "frag-loc-gate-op-sealed", "group": "operational-state", "appliesTo": { "archetypeIds": ["containment-site"] },
    "when": { "knownOperationalStatesAny": ["sealed"] },
    "text": "Der Verschluss sitzt fest; nichts deutet darauf hin, dass ihn seit langem jemand geoeffnet hat." },
  { "id": "frag-loc-gate-op-opened", "group": "operational-state", "appliesTo": { "archetypeIds": ["containment-site"] },
    "when": { "knownOperationalStatesAny": ["opened"] },
    "text": "Der Verschluss ist geoeffnet. Was er zurueckhalten sollte, ist damit nicht laenger gesichert." },
  { "id": "frag-loc-gate-op-breached", "group": "operational-state", "appliesTo": { "archetypeIds": ["containment-site"] },
    "when": { "knownOperationalStatesAny": ["breached"] },
    "text": "Der Verschluss ist gewaltsam durchbrochen und bietet keine verlaessliche Sicherung mehr." },

  { "id": "frag-loc-contact-op-available", "group": "operational-state", "appliesTo": { "archetypeIds": ["contact-site"] },
    "when": { "knownOperationalStatesAny": ["available"] },
    "text": "Der Ort wirkt weiterhin offen fuer eine Annaeherung." },
  { "id": "frag-loc-contact-op-closed", "group": "operational-state", "appliesTo": { "archetypeIds": ["contact-site"] },
    "when": { "knownOperationalStatesAny": ["closed"] },
    "text": "Der Zugang ist derzeit erkennbar verwehrt; wer hier war, empfaengt im Moment niemanden." },
  { "id": "frag-loc-contact-op-invited", "group": "operational-state", "appliesTo": { "archetypeIds": ["contact-site"] },
    "when": { "knownOperationalStatesAny": ["invited"] },
    "text": "Ein deutlich platziertes Zeichen laedt zu einem weiteren Kontakt ein." },

  { "id": "frag-loc-hazard-op-active", "group": "operational-state", "appliesTo": { "archetypeIds": ["hazard-site"] },
    "when": { "knownOperationalStatesAny": ["active"] },
    "text": "Die Gefahr ist gegenwaertig; nichts hier ist im Moment verlaesslich sicher." },
  { "id": "frag-loc-hazard-op-avoided", "group": "operational-state", "appliesTo": { "archetypeIds": ["hazard-site"] },
    "when": { "knownOperationalStatesAny": ["avoided"] },
    "text": "Die Expedition haelt Abstand; die unmittelbare Gefahr bleibt bestehen." },
  { "id": "frag-loc-hazard-op-traversed", "group": "operational-state", "appliesTo": { "archetypeIds": ["hazard-site"] },
    "when": { "knownOperationalStatesAny": ["traversed"] },
    "text": "Es gibt einen belegten Weg hindurch, doch sicherer ist das Gebiet dadurch nicht geworden." },
  { "id": "frag-loc-hazard-op-restricted", "group": "operational-state", "appliesTo": { "archetypeIds": ["hazard-site"] },
    "when": { "knownOperationalStatesAny": ["restricted"] },
    "text": "Ein begrenzter, brauchbarer Verlauf am Rand ist bekannt und markiert." },
  { "id": "frag-loc-hazard-op-contained", "group": "operational-state", "appliesTo": { "archetypeIds": ["hazard-site"] },
    "when": { "knownOperationalStatesAny": ["contained"] },
    "text": "Die unmittelbare Gefahr ist eingegrenzt; der Ort bleibt vorsichtshalber markiert." },

  { "id": "frag-loc-landmark-op-stable", "group": "operational-state", "appliesTo": { "archetypeIds": ["natural-phenomenon"] },
    "when": { "knownOperationalStatesAny": ["stable"] },
    "text": "Der Ort steht unveraendert als ferner, unverwechselbarer Bezugspunkt." },
  { "id": "frag-loc-landmark-op-accessible", "group": "operational-state", "appliesTo": { "archetypeIds": ["natural-phenomenon"] },
    "when": { "knownOperationalStatesAny": ["accessible"] },
    "text": "Ein gangbarer Zugang nach oben ist bekannt." },
  { "id": "frag-loc-landmark-op-mapped", "group": "operational-state", "appliesTo": { "archetypeIds": ["natural-phenomenon"] },
    "when": { "knownOperationalStatesAny": ["mapped"] },
    "text": "Der Ort ist als verlaesslicher Kartenbezug festgehalten; von hier laesst sich die Umgebung einordnen." }
]
```

The effective `subjectKind`, `source` and priority for these entries come from the normative
`operational-state` defaults declared at the start of Section 8.

#### 8.2.3 Presence-State Fragments (archetype-generic, source `stored-observation`, priority 55)

```json
[
  { "id": "frag-loc-presence-unknown", "group": "presence-state",
    "when": { "knownPresenceStatesAny": ["unknown"] },
    "text": "Ob jemand diesen Ort nutzt oder beobachtet, laesst sich derzeit nicht sagen." },
  { "id": "frag-loc-presence-watched", "group": "presence-state",
    "when": { "knownPresenceStatesAny": ["watched"] },
    "text": "Frische Spuren und freigehaltene Sichtlinien sprechen dafuer, dass der Ort beobachtet wird." },
  { "id": "frag-loc-presence-guarded", "group": "presence-state",
    "when": { "knownPresenceStatesAny": ["guarded"] },
    "text": "Eine Bewachung ist offen erkennbar; wer sich naehert, wird gesehen." },
  { "id": "frag-loc-presence-abandoned", "group": "presence-state",
    "when": { "knownPresenceStatesAny": ["abandoned"] },
    "text": "Es gibt keine frischen Zeichen von Nutzung, Pflege oder Bewachung." },
  { "id": "frag-loc-presence-present", "group": "presence-state", "appliesTo": { "archetypeIds": ["contact-site"] },
    "when": { "knownPresenceStatesAny": ["present"] },
    "text": "Mindestens eine Person ist erkennbar anwesend und verhaelt sich ruhig." },
  { "id": "frag-loc-presence-empty", "group": "presence-state", "appliesTo": { "archetypeIds": ["investigation-site"] },
    "when": { "knownPresenceStatesAny": ["empty"] },
    "text": "Im Moment haelt sich niemand erkennbar an der Stelle auf." },
  { "id": "frag-loc-presence-spreading", "group": "presence-state", "appliesTo": { "archetypeIds": ["hazard-site"] },
    "when": { "knownPresenceStatesAny": ["spreading"] },
    "text": "Junge Spuren ausserhalb des aelteren Kernbereichs deuten darauf hin, dass sich die Gefahr ausbreitet." }
]
```

#### 8.2.4 Interaction-State Fragments (source `stored-observation`, priority 40)

Own-progress lines; deliberately low priority so they drop first under the cap.

```json
[
  { "id": "frag-loc-ia-untouched", "group": "interaction-state",
    "when": { "knownInteractionStatesAny": ["untouched", "unapproached"] },
    "text": "Niemand aus der Expedition hat diesen Ort bisher aus der Naehe untersucht." },
  { "id": "frag-loc-ia-inspected", "group": "interaction-state",
    "when": { "knownInteractionStatesAny": ["inspected"] },
    "text": "Der Zustand wurde bereits einmal vor Ort aufgenommen." },
  { "id": "frag-loc-ia-searched", "group": "interaction-state", "appliesTo": { "archetypeIds": ["investigation-site"] },
    "when": { "knownInteractionStatesAny": ["searched"] },
    "text": "Die zugaenglichen Bereiche wurden bereits nach Spuren abgesucht." },
  { "id": "frag-loc-ia-investigated", "group": "interaction-state",
    "when": { "knownInteractionStatesAny": ["investigated"] },
    "text": "Die fruehere Untersuchung hat verwertbare Beobachtungen hinterlassen." },
  { "id": "frag-loc-ia-documented", "group": "interaction-state",
    "when": { "knownInteractionStatesAny": ["documented"] },
    "text": "Der bekannte Zustand ist vollstaendig dokumentiert." },
  { "id": "frag-loc-ia-observed", "group": "interaction-state", "appliesTo": { "archetypeIds": ["territorial-marker", "natural-phenomenon"] },
    "when": { "knownInteractionStatesAny": ["observed"] },
    "text": "Form und Anordnung wurden bereits aus sicherer Entfernung beobachtet." },
  { "id": "frag-loc-ia-interpreted", "group": "interaction-state", "appliesTo": { "archetypeIds": ["territorial-marker"] },
    "when": { "knownInteractionStatesAny": ["interpreted"] },
    "text": "Die Bedeutung der Zeichen ist inzwischen verstanden." },
  { "id": "frag-loc-ia-assessed", "group": "interaction-state", "appliesTo": { "archetypeIds": ["hazard-site"] },
    "when": { "knownInteractionStatesAny": ["assessed"] },
    "text": "Die sichtbaren Risiken und moeglichen Wege wurden bereits eingeschaetzt." },
  { "id": "frag-loc-ia-approached", "group": "interaction-state", "appliesTo": { "archetypeIds": ["contact-site", "natural-phenomenon"] },
    "when": { "knownInteractionStatesAny": ["approached"] },
    "text": "Die Expedition hat sich diesem Ort schon einmal sichtbar genaehert." },
  { "id": "frag-loc-ia-contacted", "group": "interaction-state", "appliesTo": { "archetypeIds": ["contact-site"] },
    "when": { "knownInteractionStatesAny": ["contacted"] },
    "text": "Hier hat bereits eine erste Begegnung stattgefunden; sie hat Spuren und Fragen hinterlassen." },
  { "id": "frag-loc-ia-withdrawn", "group": "interaction-state", "appliesTo": { "archetypeIds": ["contact-site"] },
    "when": { "knownInteractionStatesAny": ["withdrawn"] },
    "text": "Beim letzten Mal hat die Expedition Abstand gehalten und sich zurueckgezogen." },
  { "id": "frag-loc-ia-surveyed", "group": "interaction-state", "appliesTo": { "archetypeIds": ["natural-phenomenon"] },
    "when": { "knownInteractionStatesAny": ["surveyed"] },
    "text": "Der Ort wurde bereits vermessen und mit der Umgebung in Beziehung gesetzt." },
  { "id": "frag-loc-ia-secured", "group": "interaction-state", "appliesTo": { "archetypeIds": ["containment-site"] },
    "when": { "knownInteractionStatesAny": ["secured"] },
    "text": "Nach der letzten Untersuchung wurde der Zugang von der Expedition gesichert." }
]
```

#### 8.2.5 History Fragments (priority 45)

```json
[
  { "id": "frag-hist-previously-visited", "group": "history", "source": { "kind": "visible-trace" },
    "when": { "observableModifierIdsAny": ["modifier-previously-visited"] },
    "text": "Alte Lager- und Arbeitsspuren zeigen, dass schon einmal eine Expedition hier gewesen ist." },
  { "id": "frag-hist-changed-by-expedition", "group": "history", "source": { "kind": "visible-trace" },
    "when": { "observableModifierIdsAny": ["modifier-changed-by-expedition"] },
    "text": "Werkzeugspuren und zurueckgelassene Markierungen stammen erkennbar von einem frueheren Eingriff einer Expedition." },
  { "id": "frag-hist-own-recent-work", "group": "history", "source": { "kind": "visible-trace" },
    "when": { "hasOwnArchiveEntryForLocation": true },
    "text": "Die Spuren der eigenen Arbeit sind hier noch deutlich zu erkennen." },
  { "id": "frag-hist-stale-knowledge", "group": "history", "source": { "kind": "stored-observation", "toleratesDoubt": true },
    "when": { "requiresDoubtfulLastObservation": true },
    "text": "Der letzte verlaessliche Eindruck von diesem Ort liegt laenger zurueck; seither kann sich hier vieles veraendert haben." },
  { "id": "frag-hist-changed-since-visit", "group": "history", "source": { "kind": "direct-observation" },
    "when": { "currentObservationDiffersFromStored": true },
    "text": "Etwas ist hier anders als beim letzten Besuch; die Erinnerung und das, was vor der Expedition liegt, passen nicht mehr zusammen." },
  { "id": "frag-hist-lost-expedition-trace", "group": "history", "source": { "kind": "visible-trace" },
    "when": { "hasLostExpeditionRecordForLocation": true },
    "text": "Zurueckgelassene Ausruestung erinnert daran, dass eine fruehere Expedition von hier nicht zurueckgekehrt ist." }
]
```

The three derived flags (`hasOwnArchiveEntryForLocation`, `currentObservationDiffersFromStored`,
`hasLostExpeditionRecordForLocation`) are computed by the read-only view only from the structured
subject references and observation pair defined in 5.6 and 6.1; they never parse prose, match titles
or coordinates, or touch hidden state.

### 8.3 Contact Fragments (`contact-fragments.json`)

Anonymous fragments (source `direct-observation`, `identityStage: "anonymous"`, priority 70). They
apply to `subjectKind: "contact"` and to contact scenes raised at any location:

```json
[
  { "id": "frag-rel-anon-small-group", "group": "relation-anonymous",
    "text": "Drei, vielleicht vier Gestalten halten sich in Sichtweite, ohne sich zu naehern. Wer sie sind, laesst sich aus der Entfernung nicht sagen." },
  { "id": "frag-rel-anon-prepared", "group": "relation-anonymous",
    "text": "Die Fremden wirken vorbereitet: Ausruestung geordnet, Haltung wach, keine hastigen Bewegungen." },
  { "id": "frag-rel-anon-injured", "group": "relation-anonymous",
    "text": "Mindestens eine Person der Gruppe ist erkennbar verletzt oder erschoepft; die anderen halten sich schuetzend in ihrer Naehe." },
  { "id": "frag-rel-anon-messenger", "group": "relation-anonymous",
    "text": "Eine einzelne Person kommt langsam und mit sichtbar leeren Haenden naeher - eher Bote als Bedrohung, aber sicher ist das nicht." },
  { "id": "frag-rel-anon-signal", "group": "relation-anonymous",
    "text": "Statt einer Begegnung gibt es nur ein Zeichen: bewusst platziert, an die Expedition gerichtet, ohne Absender." },
  { "id": "frag-rel-anon-observing", "group": "relation-anonymous",
    "text": "Die Gruppe beobachtet die Expedition offen, erwidert aber keine Annaeherung." }
]
```

Which anonymous fragment applies is decided by the generated contact event's observable facts
(group size, arrangement, condition, communication form) exposed through the read-only view — not
by the resolver guessing.

Identified fragments (priority 75; identity pairing rules per 5.3):

```json
[
  { "id": "frag-rel-sig-recognised", "group": "relation-identified",
    "when": { "identityStage": "signature-recognised" }, "source": { "kind": "signature-identification" },
    "text": "Die Zeichen und die Machart der Ausruestung passen zu Spuren, die die Expedition schon einmal gesehen hat." },
  { "id": "frag-rel-sig-rumored", "group": "relation-identified",
    "when": { "identityStage": "signature-recognised", "contactStatusAny": ["Rumored"] }, "source": { "kind": "testimony" },
    "text": "Die sichtbaren Zeichen passen zu Berichten ueber eine bisher nur vom Hoerensagen bekannte Gemeinschaft; bestaetigt ist die Zuordnung nicht." },
  { "id": "frag-rel-ident-contacted", "group": "relation-identified",
    "when": { "identityStage": "identified", "contactStatusAny": ["Contacted"] }, "source": { "kind": "delivered-outcome" },
    "text": "Die Gruppe gehoert erkennbar zu der Gemeinschaft, mit der es bereits eine erste Begegnung gab." },
  { "id": "frag-rel-ident-open", "group": "relation-identified",
    "when": { "identityStage": "identified", "contactStatusAny": ["Open"] }, "source": { "kind": "delivered-outcome" },
    "text": "Der Umgang ist von den frueheren Begegnungen gepraegt: wachsam, aber ohne Feindseligkeit." },
  { "id": "frag-rel-ident-hostile", "group": "relation-identified",
    "when": { "identityStage": "identified", "contactStatusAny": ["Hostile"] }, "source": { "kind": "delivered-outcome" },
    "text": "Die Haltung ist unmissverstaendlich abweisend; die letzten Begegnungen haben Spuren hinterlassen." }
]
```

### 8.4 Scout-Return Fragments (`scout-return-fragments.json`)

Source `delivered-outcome` unless noted; priority 70 for arrival lines, 60 for condition/report
lines. `{memberName}` and `{companionName}` come from the delivered mission data.

```json
[
  { "id": "frag-scout-return-on-time", "group": "scout-return",
    "when": { "missionStatusAny": ["Returned"], "teamOutcome": "all-returned" },
    "text": "{memberName} kehrt zur erwarteten Zeit zurueck, den Staub des Weges noch an der Kleidung." },
  { "id": "frag-scout-return-late", "group": "scout-return",
    "when": { "missionStatusAny": ["Returned"], "wasOverdue": true },
    "text": "{memberName} kehrt spaeter zurueck als vereinbart. Die Tage dazwischen haben sichtbare Spuren hinterlassen." },
  { "id": "frag-scout-return-injured", "group": "scout-return",
    "when": { "missionStatusAny": ["ReturnedInjured"] },
    "text": "{memberName} kehrt verletzt zurueck; der Verband ist unterwegs angelegt worden und muss erneuert werden." },
  { "id": "frag-scout-return-team-split", "group": "scout-return",
    "when": { "teamOutcome": "partial-return" },
    "text": "{memberName} erreicht das Lager allein. {companionName}, gemeinsam aufgebrochen, ist nicht dabei. Es braucht einen Moment, bevor {memberName} erklaeren kann, wo sich ihre Wege getrennt haben." },
  { "id": "frag-scout-overdue-notice", "group": "scout-return",
    "when": { "missionStatusAny": ["Overdue"] },
    "text": "{memberName} ist ueberfaellig. Aus der Richtung, in die der Auftrag fuehrte, gibt es bisher kein Zeichen." },
  { "id": "frag-scout-missing-notice", "group": "scout-return",
    "when": { "missionStatusAny": ["Missing"] },
    "text": "Von {memberName} fehlt jede Nachricht. Niemand hat eine Rueckkehr beobachtet, und niemand kann sagen, was geschehen ist." },
  { "id": "frag-scout-return-with-evidence", "group": "scout-return", "priority": 55,
    "when": { "missionStatusAny": ["Returned", "ReturnedInjured"], "hasFindings": true },
    "text": "Was {memberName} mitgebracht hat, liegt sorgfaeltig verschnuert auf dem Tisch: Belege, die fuer sich sprechen sollen." },
  { "id": "frag-scout-return-lost-equipment", "group": "scout-return", "priority": 55,
    "when": { "missionStatusAny": ["Returned", "ReturnedInjured"], "hasLostEquipment": true },
    "text": "Ein Teil der Ausruestung fehlt; {memberName} musste sie unterwegs zuruecklassen." },
  { "id": "frag-scout-report-confident", "group": "scout-return", "priority": 50,
    "when": { "missionStatusAny": ["Returned"], "reportReliabilityAtLeast": 70 },
    "text": "Der Bericht kommt geordnet und ohne Zoegern; {memberName} wirkt sicher in dem, was gesehen wurde." },
  { "id": "frag-scout-report-shaken", "group": "scout-return", "priority": 50,
    "when": { "reportReliabilityBelow": 40 },
    "source": { "kind": "direct-observation" },
    "text": "Der Bericht kommt stockend und in Bruchstuecken; was {memberName} gesehen hat, laesst sich noch nicht zu einem Bild fuegen." },
  { "id": "frag-scout-return-looks-back", "group": "scout-return", "priority": 45,
    "when": { "missionStatusAny": ["Returned", "ReturnedInjured"], "reportReliabilityBelow": 60 },
    "source": { "kind": "direct-observation" },
    "text": "{memberName} blickt waehrend des Berichts immer wieder in die Richtung zurueck, aus der der Weg hierher fuehrte." }
]
```

`wasOverdue`, `hasLostEquipment` and `teamOutcome` come from the immutable
`DeliveredMissionOutcome` defined in 5.6. They are not inferred from the current world day or the
members' current status.

Member-condition fragments (source `delivered-outcome`; `frag-member-shaken` uses
`direct-observation` because "shaken" is a visible impression, not a stored member state):

```json
[
  { "id": "frag-member-unhurt", "group": "member-condition",
    "when": { "memberStatusAny": ["unhurt"] },
    "text": "Aeusserlich ist {memberName} unversehrt." },
  { "id": "frag-member-injured", "group": "member-condition",
    "when": { "memberStatusAny": ["Injured"] },
    "text": "{memberName} ist verletzt; die Versorgung unterwegs war notduerftig." },
  { "id": "frag-member-exhausted", "group": "member-condition",
    "when": { "memberStatusAny": ["Exhausted"] },
    "text": "{memberName} ist erschoepft und braucht Ruhe, bevor an einen neuen Auftrag zu denken ist." },
  { "id": "frag-member-supported", "group": "member-condition",
    "when": { "memberStatusAny": ["Injured", "Exhausted"], "teamOutcome": "all-returned" },
    "text": "{memberName} wird von {companionName} gestuetzt und haelt sich nur mit Muehe auf den Beinen." },
  { "id": "frag-member-shaken", "group": "member-condition", "source": { "kind": "direct-observation" },
    "when": { "reportReliabilityBelow": 40 },
    "text": "{memberName} wirkt gefasst, aber der Blick bleibt unruhig." }
]
```

### 8.5 Report Transitions and Base Return (`report-event-fragments.json`)

```json
[
  { "id": "frag-report-transition-default", "group": "report-transition", "priority": 40,
    "text": "Erst danach beginnt der eigentliche Bericht." },
  { "id": "frag-report-transition-map", "group": "report-transition", "priority": 45,
    "when": { "hasLeads": true },
    "text": "Ueber der Karte geordnet, ergibt sich aus den Notizen das Folgende." },
  { "id": "frag-report-transition-secondhand", "group": "report-transition", "priority": 50,
    "when": { "isSecondHandAccount": true }, "source": { "kind": "testimony" },
    "text": "Was folgt, ist nicht selbst gesehen, sondern so wiedergegeben, wie es {memberName} berichtet wurde." },
  { "id": "frag-report-transition-urgent", "group": "report-transition", "priority": 55,
    "when": { "isUrgent": true },
    "text": "Noch bevor jemand fragt, kommt die eine Nachricht, die nicht warten kann." },

  { "id": "frag-base-return-complete", "group": "base-return",
    "when": { "teamOutcome": "all-returned" },
    "text": "Die Expedition erreicht die Basis vollzaehlig. Was gesammelt wurde, geht in die Auswertung; was geschehen ist, in das Archiv." },
  { "id": "frag-base-return-losses", "group": "base-return",
    "when": { "teamOutcome": "partial-return" },
    "text": "Die Expedition kehrt zurueck, aber nicht vollzaehlig. Die Luecke ist beim Abzaehlen der Ausruestung genauso deutlich wie am Feuer." },
  { "id": "frag-base-return-empty-handed", "group": "base-return",
    "when": { "teamOutcome": "all-returned", "hasFindings": false },
    "text": "Die Expedition kehrt ohne verwertbare Funde zurueck. Geblieben sind Wegwissen, Eindruecke und offene Fragen." }
]
```

### 8.6 Worked Example

Arrival at the broken bridge. Before arrival, the last stored observation was four days old and
described it as provisionally passable. The authorized current observation confirms that condition,
reveals `modifier-watched`, and still provides no identified group:

```text
Titel: Zerstoerte Bruecke — Streckenhindernis
[opening / direct-observation]
Geborstene Balken haengen schraeg ueber der Schlucht, und unten reisst das Wasser an den
gestuerzten Traegern. Der Wind draengt in Boeen ueber den offenen Spalt.

[operational-state + presence-state + modifier / direct-observation, observable-modifier]
Eine behelfsmaessige Passage ueberspannt das Hindernis. Sie traegt, aber niemand wuerde ihr
schwere Lasten anvertrauen. Frische Spuren und bewusst freigehaltene Sichtlinien deuten auf
Beobachtung hin.

[interaction-state / previous stored-observation]
Der Zustand wurde bereits einmal vor Ort aufgenommen.

Frage: Wie kommen wir hindurch, darum herum oder sicher zurueck?
Wissensstand: bei der Ankunft neu bestaetigt; vorheriger Stand vor 4 Tagen.
[Aktionen unveraendert aus der Aktionsprojektion]
```

The same location opened through `remote-location-view` omits the sensory opening and presents only
the four-day-old stored state with its age/doubt label. It must not reuse the arrival scene.

## 9. Validation Rules

The content validator applies these checks to `scene-fragments`, `scene-policies` and
`scene-localization` documents.
Errors reject the content; warnings are reported:

1. **Error** — unknown `group`, `subjectKind`, source kind, archetype ID, variant ID, modifier ID,
   enum value or placeholder token.
2. **Error** — a state value in `when` that does not exist in any state profile of the fragment's
   applicable archetypes.
3. **Error** — a fragment or policy referencing a generated location instance ID or generated
   faction instance ID (same rule as the cross-system schema).
4. **Error** — a fragment without an effective `source` after `defaultsByGroup` expansion, or with a source kind implausible for its
   group (e.g. `relation-identified` with `direct-observation`; `opening` with `testimony`).
5. **Error** — `identityStage: "identified"` without `contactStatusAny`, or paired with a source
   other than `signature-identification`, `testimony` or `delivered-outcome`.
6. **Error** — authored fragments in the reserved `modifier` group.
7. **Error** — text over the length budget (two sentences / 240 characters) or authored literal
   digits, state IDs or action IDs. Validated placeholders such as `{daysOverdue}` may render digits
   at runtime.
8. **Error** — a policy `ordering`/`paragraphing` naming an unknown group, or `paragraphing` not
   covering every group in `ordering`.
9. **Error** — `supersedesFragmentIds` or `exclusiveTag` cycles/self-references.
10. **Warning** — a state-profile channel value with no eligible fragment for its archetype
    (coverage gap: the scene falls back to omitting that layer).
11. **Warning** — a modifier without `inspectionText` (contributes no scene text; currently
    `modifier-trapped` and `modifier-old-knowledge-linked`).
12. **Warning** — two fragments of the same group with identical conditions and overlapping
    `appliesTo` but no priority difference (nondeterministic-looking authoring).
13. **Error** — a field required by the effective fragment schema is still missing after defaults
    expansion, or a document default conflicts with an item value in a way forbidden by the schema.
14. **Error** — a condition field used by a fragment has no registered type, backing-state mapping
    and derivation rule from 5.3.
15. **Error** — a fragment/policy references a `textId` missing from the default locale, more or
    fewer than one default locale is loaded, or a locale fallback is missing/cyclic.
16. **Error** — a translation changes the default text's placeholder set or violates the same
    length/literal-digit constraints.

Additionally, the existing rule 10 of the cross-system schema applies unchanged: nothing in a
resolved scene may expose hidden claims, contexts, exact hexes or unresolved branches without a
valid evidence source.

## 10. Implementation Path

Work packages, in order. Resolver and rendering packages do not change gameplay rules, but package
0 deliberately extends persistent supporting data and therefore requires save-compatibility and
human review.

0. **Persistent facts and provenance.** Add `DeliveredMissionOutcome`, structured archive subject
   references, `SubjectLocationId` for applicable lost-expedition records and the dedicated
   `SceneRepetitionEntry` save section from 5.6. Increment the save version and define backward-
   compatible defaults; never reconstruct missing historical facts from current state.
   *Tests:* old-save migration, new-save roundtrip, delayed report viewing, later member-state
   mutation preserving the historical outcome, unknown fragment IDs ignored. **Human review is
   required before merge because this changes persistent data and save compatibility.**
1. **Authoring foundation.** New `documentType` values `scene-fragments`, `scene-policies` and
   `scene-localization`;
   loader support in `CrossSystemDataLoader`/`GameDataCatalog`; `defaultsByGroup` normalization;
   manifest entries; validator rules from Section 9. Land the Section 8 library as complete valid
   `Scenes/*.json` documents, including each file's envelope, defaults and default-locale texts.
   *Tests:* loader/normalization roundtrip, fully expanded effective-fragment snapshots, every
   validator rule with a rejecting fixture.
   **Implementation status:** loader, catalog, manifest, structural/reference validation, locale
   fallback and the German location starter set (all seven archetypes/eight current variants) are
   implemented. Contact, scout-return and report-event rule files are registered extension points;
   their fragment inventories land with packages 3–5 when the required delivered runtime facts are
   available.
2. **Resolver core for locations and inspection migration.** Build `SceneDescriptionResolver` in
   `Game.App` with the read-only view, source predicates, observation sequencing, selection
   algorithm (6.2) and result contract (6.4). Adapt and replace the existing
   `LocationInspectionPresentationResolver` path rather than creating a parallel presentation
   system. Preserve its modifier inspection text and anonymous/identified claimant behaviour while
   moving those rules into data-driven fragments. Wire arrival, remote view and
   `InspectLocationCommand`.
   *Tests:* deterministic snapshot scenes for the worked example, remote-view honesty and one scene
   per archetype; hidden context/unrevealed modifier/undelivered outcome never surfaces; previous-
   versus-current comparison occurs before knowledge commit; doubt/age handling.
   **Implementation status:** the read-only location view, deterministic localized resolver,
   structured result, inspection-command migration, remote location view and generic
   anonymous/identified relation fragments are implemented. Inspection coverage exists for all
   seven archetypes, and a faction is named only for `Contacted`, `Open` or `Hostile`. Stored sources
   enforce their authored age/doubt contract; doubtful observations receive explicit uncertainty
   wording. Supersession and exclusive-tag selection are active. Arrival scenes are emitted by
   movement for every reached location anchor and commit the directly observed condition only after
   resolution; they do not execute the inspection action. Broader age content/tests and the final
   broader age-content coverage remains in this package. The content-profile parity gate is active:
   every authored state needs a matching localized scene fragment, and `flavorByState` has been
   removed from the runtime model and location content JSON.
3. **Contact scenes.** Identity-stage derivation from `FactionContactStatus`, earned signature
   context tags and delivered contact events; wire faction interaction and situation delivery.
   *Tests:* anonymous → signature-recognised → identified progression as knowledge grows, plus no
   identified fragment while status is `Unknown` or `Rumored`; rumored testimony remains explicitly
   unconfirmed.
   **Implementation status:** the generic `ContactSceneView`, localized contact policy and initial
   anonymous/signature-recognised/identified plus visible-attitude fragments are implemented. The
   active faction-interaction UI resolves this read-only presentation and suppresses internal names
   and roles until identity is established. Signature recognition requires matching earned evidence;
   `Rumored` alone is insufficient. Representative role, description and dialogue are selected from
   localized JSON contact profiles through reusable faction `contactStyle`; concrete faction IDs no
   longer choose representatives or dialogue. Authored faction-memory definitions expose generic
   visible-conduct tags to contact fragments; remembered help, resentment and broken promises
   therefore alter the scene without branching on a concrete faction ID or narrating the hidden
   memory directly. Faction-reaction events are also rendered through this shared contact projection:
   their already-delivered authored body replaces the generic opening, while identity, visible
   attitude and history obey the same knowledge gates as a direct interaction. This completes the
   initial MVP contact-scene package; broader report, urgent-event and base-return scenes belong to
   package 5.
   Contact availability, territory reactions, representative selection and production offers are
   resolved through authored profiles/rules. Runtime commands contain no concrete faction-ID
   branches; an application instance without a content catalog receives only neutral fallback
   presentation and no fabricated faction-specific offers.
4. **Scout returns.** Wire mission completion to the immutable delivered outcome: mission status,
   actual return timing, participant outcomes, lost equipment, team outcome, report reliability and
   findings/leads. The scene precedes the existing report presentation.
   *Tests:* one snapshot per `ScoutMissionStatus`, on-time/overdue viewed immediately and later,
   partial return, lost equipment and low reliability.
5. **Reports, events and base return.** Report transitions, urgent events, base-return summaries
   and lost-expedition memorial fragments, using structured subject references only.
6. **Content-profile migration with parity gate.** Move every `flavorByState` text into validated
   state fragments. Before removal, capture snapshot coverage for every current content profile,
   all seven archetypes and both clients. Remove `flavorByState`,
   `LocationContentProfileDefinition.FlavorForState` and their call sites only after the new resolver
   meets that parity gate. Content profiles keep title, subtitle, shortDescription, imageId and
   journal text; no client fallback to the old dictionary remains.
7. **Client rendering.** One shared scene view model; Unity panel and WPF control render title,
   paragraphs, question, labels, visual and untouched action projections. Verify both clients show
   the identical scene for the same save.
8. **Playtest pass.** Only after playtesting: variation pools, repetition cooldowns and additional
   fragments (Section 12).

Dependency note: package 1 can proceed alongside package 0. Packages 2–3 depend on 1; packages 4–5
depend on 0 and 1; package 6 depends on 2; package 7 begins with 2 and grows with 3–5. Persisted
`once-per-state` content cannot ship before package 0. The save-model package and the final feel/
wording pass require human review under the repository policy.

## 11. Success Criteria

The system succeeds when players can say:

- I can picture the place or person before choosing.
- The text reflects what happened earlier.
- A returned scout feels like a character, not a generated report.
- I understand what is observed, reported and still uncertain.
- The prose creates atmosphere without hiding costs or available actions.
- WPF and Unity present the same known scene and never reveal hidden truth.

And when the implementation can prove:

- every rendered sentence traces to a fragment ID with a validated source, or to the policy
  question;
- the same save renders byte-identical scenes on both clients;
- no test can construct a scene that leaks hidden `WorldState`.

## 12. Deferred Decisions

Deliberately out of MVP scope, schema kept open for them:

- **Text variation** — `variants` array per fragment with deterministic seeded choice; repetition
  cooldowns beyond `once-per-state`.
- **Additional locale content** — the localization contract and German default are implemented;
  translated locale files and language-selection UI remain deferred until translations exist.
- **Faction-cultural tone** — per-faction wording flavors for identified contact fragments.
- **Member personality in scenes** — persistent character history influencing return descriptions
  beyond the delivered mission facts.
- **Scene images beyond placeholders** — final art direction owns `visualId` content.
