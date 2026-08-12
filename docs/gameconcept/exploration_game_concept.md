# Exploration Game Concept

Working Title: **Untitled Expedition Game**

Status: **Living Master Concept / Source-of-Truth Index**

This document contains the stable identity, locked decisions and core loop.

Detailed systems are maintained in the linked specialist documents. AI coding agents should read this file first and then open only the specialist documents relevant to the task.

---

## Document Map

- [`map_and_exploration_concept.md`](map_and_exploration_concept.md): map presentation, scouts, Fog of War, notes, knowledge states and automated travel
- [`expedition_and_members_concept.md`](expedition_and_members_concept.md): day loop, movement, resources in the field, team composition, outcomes, partial return, members, camps and conflict
- [`knowledge_base_and_analysis_concept.md`](knowledge_base_and_analysis_concept.md): knowledge economy, findings, analysis, archive, Base Phase and upgrades
- [`factions_and_trade_concept.md`](factions_and_trade_concept.md): faction logic, territories, contact, memory, trade and the three MVP factions
- [`cross_system_integration_concept.md`](cross_system_integration_concept.md): shared contract connecting locations, scouts, factions, evidence, triggers and delayed consequences
- [`world_locations_and_events_concept.md`](world_locations_and_events_concept.md): world generation, persistence, special locations, mysteries, events, consequences and tone
- [`ui_and_visual_communication_concept.md`](ui_and_visual_communication_concept.md): information UI, reports, portraits, discovery images and archive presentation
- [`situational_scene_description_concept.md`](situational_scene_description_concept.md): text-adventure-inspired scene descriptions shared by locations, contacts, scouts and important events
- [`mvp_vertical_slice_concept.md`](mvp_vertical_slice_concept.md): goal system, MVP scope, required slice content and explicit non-goals
- [`technical_design_notes_for_agents.md`](technical_design_notes_for_agents.md): design-level data structures and agent guidance
- [`open_questions.md`](open_questions.md): only unresolved questions
- [`../next_implementation_concept.md`](../next_implementation_concept.md): proposed next phase for the curated tutorial, content workflow and editor decision

The former single-file concept is preserved as [`exploration_game_concept_full_updated.md`](exploration_game_concept_full_updated.md).

---

## 0. Locked Current Decisions

This section is the current source of truth.

If an older passage elsewhere in the design documents conflicts with this section, this section wins until the conflict is corrected.

- Engine: **Unity 6.5 + C#**
- The game is an exploration and interpretation game, not a 4X game.
- The map uses a hidden logical hex grid.
- Normal play uses a flat isometric 2D/2.5D presentation without permanent visible hex outlines.
- Hex cells do not have sculpted terrain elevation.
- Forests, mountains, settlements, ruins and landmarks are represented by placed visual objects or sprites.
- Rivers and roads are visually connected and traceable.
- The MVP world is a semi-procedural island.
- The MVP contains three territorial factions: the Coastal People, the Border Wardens and the Hidden Ones.
- The Hidden Ones control land and are extremely aggressive inside clearly warned or forbidden territory.
- Scout orders use eight approximate compass sectors, not the six adjacency directions of the logical hex grid.
- A scout mission covers multiple hexes according to direction, duration, focus and behavior.
- There is exactly one player-controlled main expedition.
- The expedition cannot be split. Only scouts may temporarily leave it.
- The four expedition resources are Supplies, Medicine, Morale and Capacity.
- Knowledge Points are the base progression and trade resource.
- Archived Knowledge, Unsecured Field Knowledge and Knowledge Points remain separate concepts.
- Findings analysis costs base time and produces knowledge. It never costs Knowledge Points.
- Purchased information reduces or removes the independent-discovery bonus, but does not remove the base value of a discovery.
- The MVP has no tactical battle system and no separate combat screen.
- Conflict is resolved through events, choices, roles, risks and consequences.
- Known confirmed routes may support automated travel, but travel still consumes days and Supplies and may be interrupted.
- Route sections connected to a failed, missing or uncertain expedition outcome must be revalidated before automated travel can safely use them.
- Expedition members are named characters with persistent, unbounded histories of meaningful actions, memories and consequences.
- Important reports, contacts, discoveries and archive entries use portraits or contextual images, including placeholders during the MVP.
- The first expedition should return with incomplete but useful knowledge.
- The second expedition should be meaningfully better because of what the first expedition learned.

---

## 1. Core Identity

This is an **exploration and interpretation game**.

The player leads expeditions into a procedurally generated, unknown, changing world. The goal is not to conquer, survive indefinitely, build a city, optimize an economy or dominate other factions.

The goal is to **understand the world**.

Each expedition may succeed, fail, disappear or be forced to retreat. Later expeditions can continue the investigation by using incomplete maps, old reports and recovered knowledge from previous attempts.

The world is persistent, but not static.

> The player does not expand an empire.  
> The player leads an expedition.

> The world remembers previous expeditions, and the player inherits their imperfect knowledge.

---

## 2. Genre Positioning

The game visually uses a modern hex-based strategy map, but it is **not** a traditional 4X game.

The focus is not conquest, empire building or large-scale warfare. The focus is exploration, discovery, risk management, limited resources, interpretation and understanding an unknown world.

The game should avoid becoming:

- a 4X game
- a city builder
- a survival crafting game
- a complex economy simulator
- a conquest game
- a tactical combat game as its main identity

Combat may exist, but it is not the main attraction. Violence should be one possible consequence among many, not the primary gameplay loop.

---

## 3. Design Pillars

### 3.1 Exploration First

The main pleasure of the game is discovering and understanding an unknown world.

The player should frequently ask:

- What is this place?
- Who controls this region?
- Is this warning real?
- Can this route be trusted?
- What happened here?
- Should I investigate or avoid this?
- What did my scout actually mean in this report?

### 3.2 Knowledge Is Progression

Knowledge is the primary progression system.

The player does not become stronger mainly through weapons, armies or production chains, but through understanding the world better.

Knowledge has three separate meanings:

- archived knowledge: permanent information stored at the base
- unsecured field knowledge: valuable information carried by the active expedition
- Knowledge Points: the spendable practical value generated when an expedition returns

Spending Knowledge Points does not delete archived knowledge. It represents converting discoveries into support, credibility, logistics, training, equipment, specialists, trade value and better preparation.

Examples of valuable knowledge:

- which biomes are dangerous
- where faction territories probably begin or end
- which signs indicate warnings, borders, taboos or sacred sites
- which routes are safe
- which scout reports are reliable
- which factions react aggressively to certain actions
- which ruins are useful, dangerous or politically sensitive
- which old information may no longer be reliable

Core loop:

> Explore -> gain field knowledge -> decide when to return -> secure knowledge -> spend Knowledge Points -> prepare a stronger expedition.

### 3.3 Uncertainty Creates Tension

The player should not receive perfect information.

Scout reports, signs, environmental clues and encounters provide hints, but not always complete answers. The player must interpret what they find.

The game should be fair, but not fully explicit. Information may be incomplete, old, uncertain or conflicting.

### 3.4 Consequences Over Combat

Violence exists, but most tension comes from:

- limited supplies
- risk
- misinterpretation
- faction reactions
- loss of scouts or expedition members
- ruined trust
- blocked routes
- dangerous discoveries
- outdated knowledge
- hard decisions under uncertainty

### 3.5 The Map Is a Mystery

Faction borders, dangerous zones and important locations are not clearly revealed by the UI.

The player must infer territory and danger through observation:

- settlements
- architecture
- patrols
- flags or symbols
- ruined camps
- grave markers
- cultural signs
- terrain modifications
- behavior of local groups
- scout reports
- repeated warnings

The game should not automatically solve the map for the player.

### 3.6 The Base Supports Exploration

The player has a start base that can be upgraded in limited ways.

The base provides resources, personnel, scouts, equipment and logistical support, but it is not the main focus of the game.

The base exists to make deeper exploration possible.

---

## 5. Core Gameplay Loop

The main gameplay loop consists of:

1. Prepare an expedition with limited people and supplies.
2. Explore unknown terrain manually.
3. Send scouts into unrevealed or risky areas.
4. Read and interpret scout reports.
5. Discover clues, ruins, warnings, faction signs and environmental anomalies.
6. Manually annotate the map with assumptions and warnings.
7. Decide whether to investigate, avoid, return to base or push deeper.
8. Suffer consequences from risk, misinterpretation, hostile territory or poor preparation.
9. Bring unsecured field knowledge, resources and discoveries back to the base.
10. Secure knowledge into the archive and convert its practical value into Knowledge Points.
11. Spend Knowledge Points and base time to prepare deeper future expeditions.

The player should not receive perfect information. Exploration is based on observation, inference and risk.

---

## 27. Design Rules

The following rules should guide future decisions.

1. Exploration is the core.
2. Knowledge is progression and a base/trade resource.
3. The map should not explain itself too easily.
4. Hexes are the data structure, not the art style.
5. Combat is a consequence, not the main loop.
6. The base supports exploration, but does not dominate.
7. Factions react to behavior, team composition and past expedition history.
8. Diplomacy is a form of exploration.
9. Failure should create history.
10. Old knowledge should be useful, but not always safe.
11. Special locations should create choices, not only rewards.
12. The player should interpret clues instead of following obvious quest markers.
13. There is one main expedition; only scouts leave it temporarily.
14. Analysis spends time and creates knowledge.
15. Images may reinforce evidence, but must not reveal unearned truth.
16. The game should remain focused and not drift into 4X, tactical combat, survival crafting or city-building.

---

## 29. Current Concept Summary

This game is about leading expeditions into an unknown procedurally generated world.

The player explores a fog-covered hex map, sends scouts, reads reports, interprets signs, marks the map manually, investigates special places and slowly learns how the world works.

Knowledge is both progression and resource.

The archive preserves what the player has learned. The active expedition carries unsecured field knowledge that can be lost. Returning to base secures that knowledge and creates Knowledge Points that can be spent on preparation, equipment, specialists, base upgrades, trade, information and recovery missions.

Spending Knowledge Points does not erase archived knowledge.

Factions control invisible territories and react to the player’s actions, team composition and previous expedition history. They have leaders or leadership structures, territorial rules, cultural values, memories and their own reasons for being friendly, cautious, hostile or desperate. Their borders, attitudes and even existence may change between expeditions.

Expeditions can fail, but failure is not a simple reset. Later expeditions inherit imperfect knowledge from earlier ones. The world persists, changes and remembers.

If an expedition returns, its knowledge is secured. If it is lost, its unsecured knowledge is lost at first and may later become a recovery objective through journals, remains, camps, witnesses or recovered map cases.

The core fantasy is not conquest.

The core fantasy is this:

> We enter the unknown, we lose people, we leave traces, we learn, and the next expedition goes farther.

The MVP should prove that this loop is fun before the game expands in content, graphics, procedural generation or complexity.

But discovery is not always safe.

Some places were hidden, sealed or abandoned for a reason. The expedition may uncover knowledge, but it may also release dangers, break old agreements or permanently change the world.
