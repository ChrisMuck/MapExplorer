# Situational Scene Description Concept

Status: **Confirmed cross-system presentation direction / implementation not yet complete**

This document defines the text-adventure-inspired presentation layer shared by locations,
contacts, scouts, reports and important expedition events. It does not create a second simulation
or permit presentation code to infer hidden truth.

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

## 5. Data-Driven Authoring Contract

Reusable content should support fragment groups such as:

- `openingFragments`
- `interactionStateFragments`
- `operationalStateFragments`
- `presenceStateFragments`
- `modifierFragments`
- `relationFragmentsAnonymous`
- `relationFragmentsIdentified`
- `historyFragments`
- `memberConditionFragments`
- `scoutReturnFragments`
- `reportTransitionFragments`

Each fragment should have a stable ID and may declare:

- applicable archetype, variant, action tag, report type or participant role;
- required known state or visible condition;
- forbidden or superseded states;
- knowledge source and confidence requirement;
- priority, ordering group and repetition policy;
- portrait, scene image or placeholder reference where appropriate.

The exact schema should be introduced incrementally. The MVP needs a small curated library, not
procedural prose generation or a large text corpus.

## 6. Resolver and Architecture

`Game.Core` owns state and player knowledge. `Game.App` owns a shared scene-description projection
that selects authored fragments from the state the player is allowed to know. Unity and WPF render
the returned title, paragraphs, visual reference, known statuses and actions unchanged.

The resolver must not branch on a concrete location ID, faction ID, bridge, grave, gate, Unity panel
or WPF control. Archetype policies may define the characteristic question and fragment ordering;
variants and modifiers provide authored content through data.

A useful presentation result may contain:

```text
title
scene paragraphs
speaker / subject identity as known
portrait or scene visual ID
knowledge provenance
condition and reliability labels
available action projections
```

The result is a projection, not authoritative state and not a replacement for events, reports or
the command system.

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

## 8. Initial Implementation Order

1. Current location scene from profile, known state, observable modifiers and visible history.
2. Faction contact scene with anonymous/identified identity progression.
3. Scout return-state description for success, late, injured, team-split and missing outcomes.
4. Shared report/event transition into structured testimony and actions.
5. Base return summaries and later-expedition revisit history.
6. Only after playtesting, add text variation and repetition control.

## 9. Success Criteria

The system succeeds when players can say:

- I can picture the place or person before choosing.
- The text reflects what happened earlier.
- A returned scout feels like a character, not a generated report.
- I understand what is observed, reported and still uncertain.
- The prose creates atmosphere without hiding costs or available actions.
- WPF and Unity present the same known scene and never reveal hidden truth.

