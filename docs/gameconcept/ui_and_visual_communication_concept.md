# UI and Visual Communication Concept

Source of truth for information presentation, map notes, reports, archive interaction, portraits and discovery imagery.

The UI organizes evidence but does not solve interpretation.

---

## 25A. UI, Notes and Archive Design

The UI is a core gameplay system.

The player must read, interpret, compare and record uncertain information.

The UI should help the player organize knowledge, but it should not solve the mystery for them.

Core rule:

> The UI organizes information, but does not solve interpretation.

### 25A.1 Map Information Layers

The map should distinguish between different types of knowledge.

Possible knowledge states:

- unknown
- currently confirmed
- reported by scout
- old knowledge
- doubtful
- contradicted
- player assumption

The player should be able to see the difference between:

- what the expedition saw directly
- what a scout reported
- what an old expedition recorded
- what a faction claimed
- what the player personally assumes

The map should never feel magically omniscient.

### 25A.2 Manual Markers

The player can place quick markers on hexes or regions.

Possible marker types:

- danger
- possible faction border
- warning sign
- ruin
- investigate later
- scout disappeared
- safe route
- uncertain report
- water / supplies
- forbidden area
- special location
- possible passage
- old report
- faction contact
- sealed place
- suspected trap

Quick markers support players who do not want to write long notes.

### 25A.3 Free Text Notes

Players may also add free text notes to hexes, regions, reports or special locations.

Examples:

> Three posts with bird symbol. Scout thinks this may be a border. Not confirmed.

> Coastal People warned us not to camp beyond the black stones.

> Old map from Expedition 2 contradicts new scout report.

Free text notes support players who enjoy deeper interpretation.

The game should support both play styles.

### 25A.4 Scout Report UI

Scout reports should be readable as text first.

A scout report may also include structured hints that can be linked to the map.

The UI may offer actions such as:

- create marker from hint
- link report to hex
- mark report as important
- add player note
- compare with old reports
- archive report
- mark as doubtful

The report should not automatically become perfect map knowledge.

### 25A.5 Expedition Journal

The expedition journal records events by expedition day.

Examples:

- Day 3: Mara sent northeast for 2 days.
- Day 5: Mara returned with a report about carved posts.
- Day 7: The expedition found a marked grave.
- Day 9: Supplies are running low.
- Day 11: Camp was watched during the night.
- Day 14: Expedition returned to base.

The journal helps reconstruct what happened.

It is especially important for later expeditions and legacy.

### 25A.6 Archive

The archive stores knowledge across expeditions.

Possible archive categories:

- scout reports
- discovered locations
- faction notes
- symbols and warnings
- old expedition records
- special location files
- recovered journals
- known routes
- unresolved mysteries
- world events
- mandate progress

The archive should be searchable and filterable later.

For the MVP, a simple categorized list is enough.

### 25A.7 Faction Notes

Faction information should appear as observation files, not as transparent diplomacy meters.

Faction notes may include:

- known signs
- suspected territory
- warnings received
- taboo actions
- contact status
- remembered actions
- leader/contact information
- useful rumors
- known reactions
- player notes

The UI should avoid exact visible values such as “Trust: 72.”

Instead, show qualitative information.

Example:

> The Border Wardens have renewed the warning markers near the ravine.

### 25A.8 Linked Knowledge

Information should be linkable.

Examples:

- a scout report linked to a hex
- a faction warning linked to a special location
- an old journal linked to an abandoned camp
- a player note linked to a suspected border
- a symbol linked to multiple discoveries

This is a later expansion, but the design should anticipate it.

### 25A.9 Minimum MVP UI Requirements

The MVP should include:

- hex map with fog of war
- visible expedition day counter
- movement points display
- supplies, medicine, morale and capacity display
- quick map markers
- optional free text notes
- scout report screen
- “create marker from report” option
- expedition journal
- basic archive
- old/current knowledge distinction
- faction notes
- mandate display without revealing solution

This is a lot, but it is central to the game.

If the information UI is weak, the game’s core loop will not work.


---

## 25B. Portraits and Visual Communication

Important information should have a human, physical or spatial visual anchor.

Core rule:

> Important communication and discoveries should have a face, a place or an object.

Images support memory, identity and emotional weight.

They must not replace readable text or reveal hidden truth.

### 25B.1 Visual Categories

The presentation should support:

- expedition member portraits
- scout report portraits
- faction representative portraits
- silhouettes or obscured speakers
- location and discovery images
- event scene images
- finding or object close-ups
- faction symbols
- archive thumbnails
- unknown and damaged-record placeholders

### 25B.2 Expedition Member Portraits

Every named expedition member should have a portrait or placeholder portrait.

The portrait should appear in:

- team preparation
- member details
- scout assignment
- scout reports
- injury and missing-person events
- return summaries
- archive history
- memorial or recovery records

The same visual identity should persist across expeditions.

The MVP does not require animated expressions or multiple emotional variants.

### 25B.3 Scout Report Presentation

A scout report should show:

- scout portrait
- scout name
- mission direction
- expected and actual return day
- current status
- readable report text
- qualitative confidence when justified
- links to structured hints and map markers

For a two-person scout team, both portraits should be visible.

If only one returns, the missing person remains visibly associated with the report.

A portrait makes the report feel like testimony from a person, not generated system text.

### 25B.4 Faction Contact Presentation

Faction interaction should show the person or communication source involved.

Possible presentation:

- representative portrait
- name or role when known
- faction symbol when known
- background or frame connected to the encounter location
- obscured face or silhouette if identity is unknown
- object or empty landscape if communication is indirect

Examples of indirect communication:

- warning left at camp
- voice from concealment
- returned backpack
- marked arrow
- ritual arrangement
- messenger whose identity is hidden

The UI must not invent a visible speaker when the expedition did not see one.

### 25B.5 Discovery and Location Images

Important discoveries should receive a discovery card containing:

- location or object image
- field observation
- known source
- discovery day
- associated expedition members
- current knowledge status
- available actions
- links to map, notes and archive

Before analysis, the image may show only what was physically observed.

After analysis, the archive may add:

- annotations
- comparison sketches
- translated symbols
- a separate analyzed view
- new linked conclusions

The analyzed presentation must not retroactively pretend the expedition observed more than it did.

### 25B.6 Event Images

Speakerless events should use a relevant scene, object or location image.

Examples:

- broken bridge
- abandoned fire
- blood-stained map case
- renewed warning posts
- sealed container
- empty camp
- damaged boat

Use one clear visual anchor rather than decorative clutter.

### 25B.7 Archive Thumbnails

Archive entries should have thumbnails where useful.

Possible sources:

- member portrait
- faction representative
- location image
- object image
- symbol
- map crop
- placeholder icon

Thumbnails should help recognition and browsing.

They do not need to be unique final art in the MVP.

### 25B.8 Uncertainty and Image Honesty

Images must obey the same uncertainty rules as text.

Do not show:

- the exact attacker if nobody saw them
- the interior of a sealed place before opening it
- a faction leader before their identity is known
- the true meaning of a symbol before analysis
- a dead scout when the scout is only overdue
- exact faction borders

Use instead:

- fog
- silhouettes
- cropped views
- sketches
- damaged images
- object close-ups
- abstract placeholders
- witness-specific depictions

Core rule:

> The image may visualize known evidence, but must not confirm an interpretation the player has not earned.

### 25B.9 Accessibility

No required information should exist only in an image.

Every meaningful visual must be supported by:

- readable text
- labels
- status
- description
- archive metadata

### 25B.10 MVP Asset Scope

The MVP may use placeholders.

Recommended initial set:

- portraits for all eight tutorial expedition members
- at least two or three representative visuals per MVP faction
- one image for each required special location
- a small reusable event-image set
- finding/object placeholders
- archive thumbnail fallbacks
- unknown speaker and unknown location placeholders

The asset pipeline should use stable visual IDs so placeholders can later be replaced without changing game logic.

Final art must not block implementation of the core loop.

---
