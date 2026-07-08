# Technical Design Notes for AI Coding Agents

Design-level technical notes extracted from the game concept.

The repository's `technical_concept.md` and `AGENTS.md` remain authoritative for architecture and implementation rules.

---

## 25. Technical Design Notes for AI Coding Agents

These notes are not final implementation decisions, but they define important expectations.

### 25.1 Core Data Structures

The game will likely need data structures for:

- world map
- hex tiles
- biomes
- terrain features
- fog of war state
- map knowledge state
- faction territories
- faction attitude
- special locations
- sealed places
- consequential world events
- expedition state
- scouts
- scout reports
- map notes
- base upgrades
- expedition archive
- expedition mandates / goals
- goal success state
- base phase time
- event definitions
- risk resolution
- UI map notes and archive links
- world time
- inter-expedition changes

### 25.2 Hex Tile Data

Each hex may contain:

- coordinates
- biome
- terrain type
- movement cost
- visibility modifier
- hazard level
- faction influence
- special location reference
- fog state
- knowledge state
- last confirmed expedition number or date
- player notes
- discovered symbols
- route information

### 25.3 Faction Data

Each faction may contain:

- name
- archetype tags
- territory influence map
- hidden border behavior
- base attitude toward outsiders
- current attitude toward player
- trust
- fear
- respect
- anger
- curiosity
- need
- threat perception
- aggression level
- cultural signs
- taboo actions
- territorial rules
- values
- preferred biomes
- settlements
- patrol zones
- leadership structure
- leader access requirements
- known representatives
- possible first-contact stages
- faction memories from previous expeditions
- information reliability profile
- internal tensions
- history events
- relationship changes between expeditions


### 25.3A Special Location Data

Each special location may contain:

- location id
- hex coordinate or area
- location type
- visible description
- hidden purpose
- known clues
- required or useful specialists
- possible actions
- risk level
- faction relevance
- taboo associations
- sealed state
- containment purpose, if any
- consequence triggers
- world changes caused by interaction
- archive entries created
- later expedition effects

Some locations should have an obvious surface function and a hidden true purpose.

Example:

- surface: giant wall blocking progress
- hidden purpose: containment barrier preventing something from leaving

### 25.3B Consequential World Event Data

Major player actions may create persistent world events.

Each event may contain:

- event id
- expedition id
- source location
- triggering action
- immediate consequence
- delayed consequence
- affected regions
- affected factions
- affected routes
- new hazards
- archive summary
- visibility to player
- whether later expeditions inherit it


### 25.4 Scout Report Data

Scout reports should be stored as readable text plus structured metadata.

Possible fields:

- scout id
- expedition id
- origin hex
- target area
- generated report text
- reported locations
- confidence level
- risk encountered
- outcome
- whether scout returned
- whether information is confirmed
- hidden truth, if needed for later comparison

The UI should present the report as text first. Structured information should support the simulation, not replace player interpretation.

### 25.5 Map Notes Data

Map notes should be created by the player.

Possible fields:

- hex coordinate or area
- marker type
- note text
- created during expedition id
- current reliability
- optional color/icon
- linked scout report or journal entry


### 25.6 Goal / Mandate Data

Each expedition mandate may contain:

- goal id
- goal type
- title
- initial description
- hidden target data
- known clue list
- related factions
- related special locations
- related archive entries
- success conditions
- partial success conditions
- major success conditions
- failure with knowledge conditions
- current progress state
- discovered evidence
- final report text

The goal system should support incomplete knowledge.

The UI may show the mandate, but should not reveal the solution path.


---
