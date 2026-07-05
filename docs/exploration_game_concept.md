# Exploration Game Concept

Working Title: **Untitled Expedition Game**  
Inspiration: **Seven Cities of Gold**, modernized as a hex-based exploration game with an angled/isometric map view similar in readability to **Endless Legend**.

Status: **Early Concept / Living Design Document**  
Primary purpose: This document gives AI coding agents and future contributors a shared understanding of what the game is, what it is not, and which systems matter most.

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

## 4. Visual Style and Map Presentation

The game uses a **modern hex-based world map** with an angled/isometric camera perspective.

The world is divided into hexagonal tiles. Each tile represents a terrain type, biome, point of interest, hazard, faction presence or unknown information state.

The camera should present the world as a living landscape rather than a flat board. Terrain elevation, biome transitions, ruins, settlements, rivers, mountains and landmarks should be visually readable.

### 4.1 Map Design Rule

The map should provide information through observation, not through explicit UI overlays whenever possible.

The player should feel like an explorer interpreting an unknown world, not like a commander reading a fully labeled strategy map.

### 4.2 Fog of War

At the start, almost the entire world is covered by fog of war.

The player gradually reveals information by:

- moving the main expedition
- sending scouts
- finding maps or reports
- talking to factions
- discovering landmarks
- recovering old expedition records
- investigating special places

Revealed map information is not always permanently reliable. Old knowledge may become outdated.

---

## 5. Core Gameplay Loop

The main gameplay loop consists of:

1. Prepare an expedition with limited people and supplies.
2. Explore unknown terrain manually.
3. Send scouts into unrevealed or risky areas.
4. Read and interpret scout reports.
5. Discover clues, ruins, warnings, faction signs and environmental anomalies.
6. Manually annotate the map with assumptions and warnings.
7. Decide whether to investigate, avoid, split the group, return to base or push deeper.
8. Suffer consequences from risk, misinterpretation, hostile territory or poor preparation.
9. Bring unsecured field knowledge, resources and discoveries back to the base.
10. Secure knowledge into the archive and convert its practical value into Knowledge Points.
11. Spend Knowledge Points and base time to prepare deeper future expeditions.

The player should not receive perfect information. Exploration is based on observation, inference and risk.

---

## 5A. Turn-Based Day System

The game is turn-based.

One full turn represents **one expedition day**.

The player should always see the current expedition day clearly.

Example:

> Expedition Day 1  
> Expedition Day 2  
> Expedition Day 17  
> Expedition Day 126

Using full days makes the expedition easier to understand and remember. If a scout disappears on Day 14 and the expedition finds their remains on Day 63, the passage of time should feel meaningful.

### 5A.1 Turn Structure

Each day has two broad phases:

### Player Phase

During the player phase, the player may:

- move the main expedition while movement points remain
- send scouts
- read scout reports
- place or update map notes
- inspect discovered places
- interact with factions
- start or continue multi-day actions
- camp or rest
- decide to return to base

### World Phase

After the player ends the day, the world acts.

During the world phase:

- scouts continue their journeys or return
- scout outcomes are resolved
- factions may move, observe, react or send messages
- hazards may develop
- ongoing consequences may progress
- supplies are consumed
- morale may change
- injuries may worsen or improve
- special locations may change state
- world events are processed

The world should feel alive, but the system should remain understandable.

---

## 5B. Movement Points and Daily Capacity

Each expedition has a number of movement points per day.

Movement points represent the expedition's practical daily capacity for travel and activity.

A normal expedition may start with **4 movement points per day**.

Possible movement point baselines:

- light expedition: 5 movement points per day
- normal expedition: 4 movement points per day
- heavy expedition: 3 movement points per day

Movement points are affected by:

- expedition loadout
- number of people
- terrain
- injuries
- morale
- weather or hazards in later versions
- known roads or routes
- specialist support

### 5B.1 Terrain Movement Costs

Initial terrain cost proposal:

- plains: 1 movement point
- forest: 2 movement points
- hills: 2 movement points
- swamp or dense forest: 3 movement points
- mountains: 3 movement points or blocked without a route
- river crossing: additional cost or special interaction
- known road or safe route: reduced cost, minimum 1
- hostile or unknown territory: may increase risk, but not necessarily movement cost

The player may move across multiple hexes per day as long as enough movement points remain.

### 5B.2 Actions That Use Daily Capacity

Movement points should not only be used for movement.

Other actions may consume movement points or full days.

Examples:

- briefly inspect a location: movement point cost
- carefully investigate a ruin: full day
- negotiate with a faction: partial day or full day
- build a temporary camp: remaining day
- treat injuries: usually requires camp or rest
- open a sealed gate: one or more days
- build a bridge: multi-day project
- search for a missing scout: movement points or full day

Some actions should create the decision:

> Do we spend time here, or do we keep moving?

---

## 5C. Multi-Day Projects

Some actions take multiple days.

Examples:

- building a bridge
- repairing a gate
- clearing a blocked path
- reinforcing a camp
- opening a sealed ruin carefully
- excavating a collapsed entrance
- stabilizing a dangerous structure
- constructing a raft or temporary crossing

A bridge across a ravine may cost three full workdays.

Example:

### Build Bridge Across Ravine

Requirements:

- engineer/architect, or a risky alternative
- tools or bridge-building materials
- enough supplies to remain in place

Cost:

- 3 workdays

Possible consequences:

- supplies are consumed each day
- the expedition remains exposed
- factions may notice the work
- morale may change
- accidents may occur
- the bridge may become a persistent route for later expeditions
- factions or hazards may later destroy or control the bridge

Multi-day projects should make the world feel physical and persistent.

If the expedition builds something meaningful, later expeditions may be able to find and use it.

---

## 5D. Camp and Rest

Camping or resting consumes a full expedition day.

Resting is useful, but never free.

Possible benefits:

- injuries may improve
- morale may improve
- scouts have time to return
- reports can be reviewed
- local area can be observed
- some specialist actions become possible
- the expedition may prepare for return or further travel

Possible risks and costs:

- supplies are still consumed
- factions may notice the camp
- hazards may approach
- morale may fall if the place feels unsafe
- enemies or animals may find the expedition
- staying too long may create new problems

Resting should create a real decision.

The player should ask:

> Is this a safe place to spend a day?

---

## 5E. Supply Consumption

Supplies are consumed at the end of each expedition day.

For the MVP, supply consumption should be simple:

> Each expedition member consumes 1 Supply per day.

Examples:

- 6 people consume 6 Supplies per day.
- 8 people consume 8 Supplies per day.
- 14 people consume 14 Supplies per day.

This makes expedition size meaningful.

More people bring more skills, safety and carrying capacity, but also increase daily supply consumption.

More carriers increase how much the expedition can bring, but carriers also consume supplies.

This creates the intended planning question:

> Do we bring more people to solve more problems, or fewer people to travel farther and consume less?


---

## 6. Player Actions

The player should be able to:

- move the main expedition across the hex map
- reveal nearby terrain
- send one or more scouts
- read scout reports
- place notes and markers on the map
- inspect special locations
- choose whether to investigate, ignore, respect, plunder, seal or mark discoveries
- interact with factions
- decide whether to split the expedition
- decide whether to return to base
- improve the base in limited ways
- launch later expeditions after failure or retirement of a previous expedition

---

## 7. Strategic Decisions During Exploration

The game should create meaningful decisions such as:

- Do I send a scout into unknown territory, knowing they may not return?
- Do I split the group so part of the expedition can explore a ruin while the rest moves on?
- Do I keep everyone together because the expedition is small and vulnerable?
- Do I investigate a grave that may contain knowledge, even if it may anger a nearby faction?
- Do I ignore a potentially important discovery because supplies are low?
- Do I trust an old report from a previous expedition?
- Do I enter a territory after seeing repeated warning signs?
- Do I turn back before reaching the next landmark?
- Do I risk a dangerous route because it might bypass a wall or hostile region?

---

## 8. Scouts

Scouts are a core system.

At the beginning, the player has very limited resources and can only send one or two scouts. Sending a scout is a meaningful risk.

Scouts can:

- reveal terrain
- report signs, ruins, hazards and faction activity
- disappear
- return injured
- return with incomplete information
- misinterpret what they saw
- be captured
- be killed
- leave behind traces, journals, equipment or bodies that may be found later

### 8.1 Scout Death and Recovery

If a scout dies, the player may later discover:

- the body
- a grave
- abandoned equipment
- a journal
- a blood trail
- evidence of capture
- signs of animal attack
- signs of faction execution
- a map fragment
- a final warning

A dead scout should not only be a resource loss. It should also become part of the world’s history and may provide new information.

### 8.2 Scout Reports

Scout reports are not automatically converted into exact map knowledge.

The player must read and understand the report.

Example report types:

- “We saw smoke in the east, but no settlement.”
- “The stones near the river carry the same marks as the grave.”
- “Mara believes we were followed. I am not certain.”
- “Three wooden figures were placed along the ridge. They face west.”
- “We found a path, but it looked too clean, as if maintained.”
- “There are no birds in the valley.”
- “Someone left food at the edge of the forest. It may be a warning or an offering.”

Reports should contain clues, not automatic solutions.


### 8.3 MVP Scout Orders

For the MVP, scouts are sent in a direction rather than to an exact target hex.

The player chooses:

- direction
- duration
- focus
- behavior

### Direction

Possible directions:

- north
- northeast
- southeast
- south
- southwest
- northwest

### Duration

Possible scout mission durations:

- 1 day: short range, safer, limited information
- 2 days: moderate range, moderate risk
- 3 days: deeper scouting, more useful information, higher risk
- up to 5 days: dangerous deep scouting, valuable but risky

A scout who does not return on the expected day creates uncertainty.

The player does not immediately know whether the scout is delayed, hiding, captured, lost or dead.

### Focus

Possible scout focuses:

- terrain
- dangers
- faction traces
- ruins and special locations
- safe route
- supplies or water
- missing scout
- border observation

Focus changes what the scout prioritizes.

A scout focusing on safe routes may ignore a ruin. A scout focusing on ruins may miss subtle faction signs.

### Behavior

For the MVP, scout behavior has three modes:

### Cautious

Lower range, lower risk, fewer discoveries.

The scout is more likely to turn back when warning signs appear.

### Normal

Balanced range, risk and detail.

### Risky

Higher range and more discoveries, but much higher chance of injury, capture, death or misleading interpretation under pressure.

### 8.4 Scout Teams

The player may send either one scout or a two-person scout team.

### Single Scout

Advantages:

- faster
- more discreet
- longer range
- lower supply burden

Disadvantages:

- higher death or disappearance risk
- fewer observations
- no backup if injured

### Two-Person Scout Team

Advantages:

- safer
- better chance of returning
- may produce more reliable reports
- one scout may return even if the other is lost

Disadvantages:

- uses two scouts
- less discreet
- may be easier for factions to notice
- reduces available scouts for other directions

For the MVP, scout teams should remain simple. Later versions may add more complex group behavior.

### 8.5 Scout Report Structure

Scout reports should have two layers.

### Readable Report Text

The main report is written as readable text.

The player should read and interpret it.

Example:

> Mara returned on the evening of the third day. She followed the river northeast. Beyond the second ridge she found three carved posts. She saw no settlement, but there was smoke farther east. She believes the posts may mark a border, but she is not certain.

### Structured Hints

Behind the text, the report may contain structured hints that the UI can display or allow the player to mark.

Examples:

- Hex 12/08: carved posts, possible border marker
- Hex 13/08: smoke seen in distance, unconfirmed
- Region northeast: faction presence suspected
- Scout confidence: medium
- Scout recommendation: continue carefully

The game should not automatically solve the meaning of the report.

The player may choose to create map markers from the hints.

The UI may help record information, but the player should still interpret it.

### 8.6 Scout Loss

A scout may fail to return.

Possible reasons:

- killed by hostile faction
- captured
- lost
- injured and hiding
- delayed by terrain
- killed by hazard
- fled after panic
- entered a sealed or unnatural danger
- deliberately stayed hidden to observe
- betrayed or misled by a faction

The player should initially receive limited information.

Example:

> Mara should have returned today. She did not.

Later, the expedition may discover:

- body
- backpack
- journal
- blood trail
- arrow
- broken tool
- improvised grave
- capture signs
- misleading signs
- nothing at all

A missing scout should create a question on the map.


### 8.7 Scout Traits

Later versions may give scouts simple traits.

Possible traits:

- cautious
- reckless
- experienced
- inexperienced
- good tracker
- good at interpreting ruins
- good at reading faction signs
- poor orientation
- brave
- nervous
- linguistically talented
- prone to panic

Traits should influence scout reports, survival chances and interpretation reliability.

---

## 9. Manual Map Notes

The player should be able to annotate the map manually.

Possible marker types:

- suspected border
- dangerous area
- possible ruin
- scout disappeared here
- warning sign
- suspected faction territory
- safe route
- avoid
- sacred place
- possible ambush
- old knowledge
- doubtful report
- needs later investigation

The game should not automatically solve the map for the player.

Good note-taking should help the player play better.

---

## 10. Map Knowledge States

Map information should have reliability levels.

Possible states:

### Unknown

Never explored.

### Reported

Mentioned by a scout or source, but not confirmed directly.

### Confirmed

Directly explored by the current expedition.

### Old

Discovered by a previous expedition. May be outdated.

### Doubtful

Conflicting reports exist.

### Lost

Once known, but no longer reliable due to major world changes.

These states should help represent uncertainty without overwhelming the UI.

## 10A. Knowledge Economy

Knowledge should not only be passive map progression. It should also be the main strategic resource of the game.

The system uses three knowledge layers.

### Archived Knowledge

Archived Knowledge is permanent information stored at the base.

Examples:

- mapped hexes
- known roads and rivers
- scout reports
- faction notes
- discovered locations
- player notes
- warning signs
- recovered journals
- old expedition records
- known symbols
- confirmed or disproved rumors

Archived Knowledge is not a spendable number.

It remains available even after Knowledge Points are spent.

### Unsecured Field Knowledge

Unsecured Field Knowledge is knowledge currently carried by the active expedition.

It has not yet reached the base.

Examples:

- newly confirmed map areas
- new scout reports
- discovered routes
- new faction contact
- special location discoveries
- recovered journals
- corrected old maps
- confirmed dangers
- new explanations for old mysteries

This knowledge is at risk.

If the expedition returns, it can be secured.

If the expedition is lost, this knowledge may be lost until a future expedition recovers part of it.

The active expedition UI should show the amount of unsecured knowledge currently at risk.

Example:

```text
Unsecured Knowledge: 34
```

This creates push-your-luck pressure:

- return now and secure it
- continue exploring and gain more
- risk losing everything

### Knowledge Points

Knowledge Points are the spendable resource stored at the base.

They are created when an expedition returns with valid unsecured field knowledge.

Knowledge Points may be used for:

- expedition preparation
- supplies
- medicine
- additional expedition members
- specialists
- equipment
- base upgrades
- analysis
- faction trade
- buying information
- access agreements
- rescue attempts
- recovery missions

Knowledge Points are the economic value of discoveries.

Archived Knowledge itself is never deleted when points are spent.

### Securing Knowledge

The base only receives new usable knowledge when the expedition returns.

During return processing:

1. Unsecured Field Knowledge is reviewed.
2. Duplicate or trivial information is filtered.
3. New information is added to the archive.
4. Knowledge Points are awarded.
5. Reports, maps and notes become persistent.
6. The expedition summary records what was secured.

Recommended MVP rule:

```text
1 secured knowledge value = 1 Knowledge Point
```

The conversion can later depend on archive upgrades, report quality, surviving witnesses, damaged documents, reliability, novelty and purchased information.

### MVP Knowledge Awards

For the MVP, use simple authored values and avoid complex formulas.

Suggested values:

```text
New confirmed hex:              1 Knowledge
New road or river segment:      1 Knowledge
Useful scout report:            2-5 Knowledge
Special location discovered:    8-15 Knowledge
First faction contact:          8-12 Knowledge
Recovered journal or map:       5-15 Knowledge
Major understanding:            authored reward
```

The same information should not repeatedly grant full Knowledge.

Knowledge sources should have stable IDs so the game can prevent farming repeated map movement, repeated scout reports or reselling the same information.

---

## 11. Procedural World

The world is procedurally generated.

It should contain:

- biomes
- rivers
- mountains
- coastlines
- valleys
- forests
- deserts
- swamps
- ruins
- massive landmarks
- special locations
- faction territories
- hidden borders
- dangerous zones
- possible routes and blocked paths

The generated world should feel coherent, not random.

### 11.1 Biomes

Possible biomes:

- plains
- forest
- deep forest
- jungle
- desert
- swamp
- mountains
- highlands
- coast
- riverlands
- tundra
- volcanic lands
- wasteland
- ancient overgrown ruins
- sacred groves
- dead zones

Biomes should influence:

- movement cost
- visibility
- scout risk
- supplies
- faction presence
- special locations
- hazards
- report style

### 11.2 Major Landmarks

The world should contain major landmarks that help orientation and mystery.

Examples:

- a gigantic wall that blocks progress
- a dead city
- an ancient road
- a massive crater
- black stone pillars
- a river that changes color
- a forest no faction enters
- a ruined harbor far inland
- a mountain carved into a face
- a silent valley
- a field of graves
- a tower visible from many hexes

Some landmarks may be visible before they are reachable.


### 11.3 MVP World: Semi-Procedural Island

For the MVP, the world should be a semi-procedural island.

This means the map contains procedural variation, but follows fixed design rules so the core gameplay can be tested reliably.

A fully random world generator is a later feature and should receive its own concept later.

### 11.3.1 Why an Island?

An island is ideal for the MVP because:

- the world has natural boundaries
- cartography feels achievable
- the player can understand the scope
- coasts, rivers and mountains help orientation
- the game does not need to explain why the expedition cannot travel endlessly outward
- the first version can stay focused and testable

### 11.3.2 MVP Map Size

Initial map size proposal:

> approximately 40 x 30 hexes

This should be large enough for meaningful exploration, but small enough to test all core systems.

### 11.3.3 MVP Biomes

Initial MVP biomes:

- plains
- forest
- hills / mountains
- swamp or dense forest
- river / coast

This is enough to test movement costs, visibility, hazards, scout reports and route planning.

### 11.3.4 MVP Island Structure

The island should be structured in broad zones.

### Zone 1: Start Coast

The base begins near the coast or a sheltered bay.

This area is relatively safe and teaches:

- movement
- fog of war
- supplies
- basic scouting
- map markers
- returning to base

### Zone 2: First Unknown

A nearby forest, riverland or hill region introduces:

- warning signs
- possible faction traces
- uncertain scout reports
- first special location
- first suspected border

### Zone 3: Obstacle Region

A ravine, damaged bridge, mountain pass or blocked route teaches specialist-gated exploration.

The tutorial expedition may discover this region but be unable to solve it safely because it has no engineer.

### Zone 4: Dangerous Interior

A swamp, dense forest or hostile territory increases risk.

This area may contain:

- higher scout danger
- stronger faction presence
- missing scout events
- difficult resupply
- unclear warnings

### Zone 5: Mystery Core

The island interior contains the first major mystery.

Possible contents:

- a sealed ruin
- a giant wall
- a locked gate
- a forbidden valley
- a large ancient structure
- a place connected to the campaign goal

The mystery core should not necessarily be solved in the first expedition.

### 11.3.5 MVP Factions

The MVP island should include two factions.

### Faction A: Cautious Border Wardens

This faction is territorial but not automatically hostile.

It teaches:

- hidden borders
- warning signs
- territorial rules
- cautious first contact
- leader access
- consequences for ignoring warnings

### Faction B: Isolationist or Hidden Faction

This faction is more dangerous and harder to understand.

It teaches:

- higher risk scout zones
- missing scouts
- unclear motives
- strict taboos
- fear of a specific place or secret
- hostile or near-hostile contact

Two factions are enough for the MVP.

Additional factions are a later expansion.

### 11.3.6 MVP Special Locations

The MVP island should contain 4 to 6 special locations.

Minimum recommended set:

- one grave or ancient marker
- one ravine, broken bridge or physical obstacle
- one ruin or old structure
- one sealed place with possible consequence
- one major landmark such as a wall, tower or forbidden valley

These should connect to the first expedition goals and teach the core systems.

### 11.3.7 Semi-Procedural Rules

The MVP island should follow fixed generation rules.

Examples:

- the base always starts in a relatively safe coastal region
- at least one river or coastline helps orientation
- Faction A is reachable during early exploration but not immediately visible
- Faction B is placed deeper in the island or behind danger
- at least one obstacle blocks an attractive route
- at least one special location is near a faction taboo
- the main landmark is visible or hinted early, but not immediately solvable
- early scouting should produce useful but uncertain reports
- the first expedition should likely need to return before solving everything

The MVP world should be small but dense.

The goal is not a huge empty world.

The goal is a compact testbed where scouting, movement, supplies, factions, notes, special locations and return pressure all matter.

### 11.3.8 Long-Term World Generation Goal

Later, the game may include a true procedural world generator.

That future generator should support:

- larger maps
- multiple island or continent shapes
- more biomes
- more factions
- dynamic borders
- world history
- ancient structures
- sealed places
- expedition legacy changes
- faction migration
- changing routes
- generated mysteries
- multiple campaign goals

This should be treated as a separate major design topic after the core gameplay works.


---

## 12. Persistent and Dynamic World Elements

The world persists across expeditions, but it changes over time.

### 12.1 Persistent World Elements

Some features are stable across expeditions:

- mountain ranges
- coastlines
- large rivers
- ancient ruins
- massive walls
- unique landmarks
- old roads
- monuments
- sacred sites
- major geological features

### 12.2 Dynamic World Elements

Some features can change between expeditions:

- faction borders
- faction attitudes
- settlements
- patrol zones
- trade routes
- temporary camps
- blocked passages
- dangerous creatures or hazards
- political control of regions
- abandoned camps
- new graves
- ruined villages
- new factions
- collapsed factions

This distinction is important. If everything changes, knowledge becomes worthless. If nothing changes, the world feels dead.

---

## 13. Factions

The world is inhabited by different factions.

Each faction controls an area of the world, but the player does not see the borders directly.

Factions are not simply enemies or allies. They are societies with territory, rules, fears, values, leadership and memory.

Faction behavior can be:

- friendly
- neutral
- suspicious
- hostile
- fearful
- protective
- expansionist
- isolationist
- deceptive
- fragmented
- curious
- help-seeking
- desperate
- extremely territorial

Faction attitude can change depending on what the player does, who the expedition brings, which rules are respected or broken, and what previous expeditions did.

### 13.1 Hidden Faction Borders

Faction borders are not displayed as colored map lines.

The player must infer borders from:

- patrols
- settlement style
- repeated symbols
- architecture
- warnings
- grave markers
- local behavior
- road maintenance
- abandoned border posts
- scout reports
- attacks or warnings
- forbidden zones

Example player interpretation:

> “We have seen the same carved bird symbol on three ridges now. The last two scouts were warned not to go east. This is probably the beginning of that faction’s territory.”

### 13.2 Faction Reactions

Factions should react to behavior, not only dialogue choices.

Possible player actions that affect factions:

- entering sacred land
- ignoring warnings
- stealing resources
- opening graves
- helping a village
- returning an artifact
- hunting in their territory
- killing patrols
- trading with enemies
- respecting burial sites
- healing sick locals
- mapping forbidden places
- building camps too close to settlements
- bringing back the body of one of their people
- sharing knowledge
- lying or hiding discoveries

### 13.3 Faction Change Between Expeditions

Between expeditions:

- a faction may expand
- a faction may collapse
- a faction may split
- a new faction may appear
- old alliances may break
- a friendly faction may become hostile
- a hostile faction may become weakened or desperate
- settlements may move or vanish
- borders may shift

Old faction knowledge is useful, but not always reliable.

---

## 13A. Faction Interaction System

Faction interaction is not a separate 4X diplomacy system.

Faction interaction is a form of exploration.

The player does not only explore terrain. The player also explores cultures, rules, fears, borders, values, taboos and political structures.

A faction should never be only a reputation number. Each faction should feel like a society with its own logic.

Core rule:

> Every faction has an attitude toward the expedition, but also a reason for that attitude.

A faction may welcome visitors, observe them cautiously, warn them away, manipulate them, ask for help or kill anyone who enters its territory.

The important part is that the behavior should make sense once the player understands the faction.

---

## 13B. Faction Attitude Range

Factions should support a wide range of possible reactions to the expedition.

Possible base attitudes:

### Welcoming

The faction is pleased by visitors.

It may offer trade, food, guides, information or shelter. This does not mean it has no rules. A welcoming faction may still react harshly to taboo violations.

### Curious

The faction does not immediately trust the expedition, but wants to observe or understand it.

It may send children, scouts, watchers, traders or messengers before allowing deeper contact.

### Cautious

The faction allows limited contact, but watches the expedition closely.

It may give warnings, restrict movement, demand gifts, require local guides or forbid certain actions.

### Suspicious

The faction assumes the expedition may be dangerous.

It may avoid open contact, follow the expedition, hide settlements, block paths or provide misleading information.

### Territorial

The faction is not necessarily evil, but it strongly protects its land.

Entering certain areas may trigger warnings, pursuit, ambushes or forced removal.

### Hostile

The faction attacks after provocation, past conflict, taboo violation or perceived threat.

The player may still be able to change this attitude later, but not easily.

### Extremely Isolationist

The faction kills or captures almost anyone who enters its territory.

This should be rare and must be clearly supported by clues, warnings or later explanation.

### Deceptively Friendly

The faction appears open, but has hidden motives.

It may use the expedition to access ruins, attack rivals, recover artifacts or spread false information.

### Help-Seeking

The faction actively approaches the expedition because it needs something.

It may need medicine, protection, knowledge, engineering help, mediation, food, tools or assistance against a threat.

### Desperate

The faction behaves unpredictably because it is under pressure.

It may be starving, losing a war, suffering disease, fleeing a disaster or facing internal collapse.

---

## 13C. Faction Values

Each faction should have values that determine what impresses, offends or frightens it.

Possible values:

### Strength

The faction respects armed discipline, courage and the ability to survive danger.

A strong expedition may gain respect, but also increase threat perception.

### Knowledge

The faction respects scholars, interpreters, architects, cartographers or people who understand old places and symbols.

This faction may be impressed by discoveries, careful study and correct interpretation.

### Restraint

The faction respects groups that do not plunder, disturb graves or cross sacred borders.

This faction may value patience and self-control more than bravery.

### Trade

The faction values exchange, gifts, tools, resources and practical benefit.

It may be friendly if the expedition brings useful goods, but offended by unfair trade.

### Help

The faction respects those who solve problems.

Medical aid, engineering work, food assistance or help against hazards may create trust.

### Ritual

The faction requires gestures, offerings, words, silence, clothing rules or entry customs before accepting contact.

Without the right specialist or knowledge, the player may misunderstand these rules.

### Honor

The faction values honesty, directness, courage and keeping promises.

Lying or breaking agreements should have severe consequences.

### Secrecy

The faction hates mapping, writing, drawing symbols or revealing hidden places.

Cartography may be seen as theft, spying or sacrilege.

### Curiosity

The faction is fascinated by outsiders.

It may want tools, stories, maps, medicine or knowledge from the expedition.

### Fear

The faction is deeply afraid of specific places, symbols, diseases, people or historical events.

It may react aggressively if the expedition investigates what it fears.

---

## 13D. Multi-Dimensional Faction State

Faction attitude should not be represented only by a single reputation value.

A faction can respect the expedition and still fear it. It can need the expedition and still hate it. It can be curious and angry at the same time.

Possible faction state values:

- trust
- fear
- respect
- anger
- curiosity
- need
- threat perception
- taboo violation memory
- diplomatic access level

Example:

The expedition enters a forbidden valley and maps old symbols without killing anyone.

Different factions may react differently:

- A knowledge-focused faction may gain respect but also suspicion.
- A religious faction may become angry.
- A warrior faction may gain respect but increase threat perception.
- A desperate faction may ignore the violation because it needs medicine.

The same player action should not have the same effect on every faction.

---

## 13E. Leadership and Contact Structure

Every faction must have a leader or leadership structure.

However, the player should not automatically be able to speak to the highest authority.

The path to leadership contact is part of exploration.

Possible leadership structures:

- single chief
- council of elders
- priest or priestess
- war leader
- merchant lord
- border commander
- spiritual leader
- child ruler with advisors
- hidden leader
- sick or missing leader
- divided council
- rival claimants
- military leader and religious leader in conflict
- village-level representatives under a distant ruler

The game should always provide some possible contact path, even if the faction is hostile.

This contact may begin with:

- border guards
- watchers
- messengers
- local villagers
- traders
- captured scouts
- interpreters
- symbolic warnings
- faction envoys
- indirect communication through another faction

### 13E.1 Access to the Leader

Access to leadership may require:

- respecting territorial rules
- bringing a gift
- returning a body or artifact
- proving strength
- proving knowledge
- helping a village
- surviving a test
- entering with a small unarmed group
- bringing an interpreter
- accepting a local guide
- waiting at a border marker
- stopping map-making in sacred territory
- repairing damage caused by an earlier expedition

Some factions may not speak until the expedition has demonstrated the correct kind of value.

Examples:

- A strength-focused faction grants audience after the expedition survives a dangerous route.
- A knowledge-focused faction grants audience after the expedition correctly identifies an old symbol.
- A help-seeking faction contacts the expedition first because it needs medical aid.
- A territorial faction only sends warnings until the expedition stops crossing its border.

---

## 13F. First Contact Progression

Faction contact should usually develop in stages.

Possible stages:

### Unknown

The player sees only signs, settlements, smoke, patrols, architecture or altered terrain.

### Observed

The faction knows about the expedition and watches it, but does not openly communicate.

### Warning

The faction sends indirect or direct warnings.

Examples:

- marked trees
- arrows in the ground
- blocked paths
- drums
- burned signs
- stones arranged across a road
- dead animals
- symbolic offerings
- a messenger who says only one sentence

### First Contact

The expedition has a direct encounter.

Language may be limited. The encounter may involve gestures, trade attempts, warnings, fear or confrontation.

### Limited Exchange

The faction allows basic interaction.

Possible interactions:

- trade
- request for supplies
- basic questions
- information exchange
- permission to pass
- local guide offer
- demand to leave

### Audience

The expedition speaks with a leader, council, envoy or important representative.

This may unlock deeper information, special agreements or major consequences.

### Trust

The faction shares meaningful information or asks for help.

It may reveal:

- hidden routes
- safe camps
- forbidden places
- rival factions
- warnings about ruins
- historical knowledge
- the meaning of symbols

### Practical Alliance

This is not a 4X alliance.

It means practical cooperation:

- guided passage
- food access
- shared warnings
- rescue of captured scouts
- safe camping areas
- limited support
- diplomatic protection in nearby regions

### Broken Trust

The faction feels betrayed.

Old agreements may become invalid. Future expeditions may inherit the damage.

### Expulsion or War

The faction actively hunts, blocks, expels or attacks the expedition.

Even then, later contact may be possible through intermediaries, leadership change or major assistance.

---

## 13G. Faction-Initiated Interaction

Factions should not only wait for the player.

They may actively approach the expedition.

Examples:

- A messenger warns the expedition not to enter a forest.
- A village asks for medicine.
- A leader requests help repairing a bridge, wall or water system.
- A faction demands the return of an artifact.
- A faction asks the expedition to destroy or hide a map.
- A faction offers a guide through dangerous land.
- A faction warns against a ruin it refuses to enter.
- A faction returns a captured scout, but keeps the scout’s notes.
- A faction demands the body of one of its dead.
- A faction asks why earlier expeditions entered its land.
- A faction secretly asks for help against its own leader.
- A faction tries to redirect the expedition toward a rival’s territory.

Faction-initiated interactions should feel like situations, not standard quests.

The player should decide whether to trust, ignore, negotiate, investigate or exploit them.

---

## 13H. Territorial Rules

Each faction may have territorial rules.

These rules should be discoverable through signs, warnings, behavior, scout reports and direct contact.

Possible rules:

- outsiders may use roads, but not forests
- outsiders may trade, but not camp
- outsiders may pass only with a guide
- outsiders must leave weapons at a border marker
- outsiders must not draw maps
- outsiders must not copy symbols
- outsiders must not touch graves
- outsiders must not speak to certain people directly
- outsiders must enter with a small group, not a full expedition
- outsiders must leave offerings at sacred stones
- outsiders must not cross rivers or ridges
- outsiders may enter only during certain times
- outsiders must not hunt in faction land
- outsiders must not build camps near settlements
- outsiders may not ask about specific ruins or historical events

The player can:

- follow the rules
- misunderstand the rules
- ignore the rules
- deliberately break the rules
- negotiate exceptions
- ask another faction to explain them
- return later with the right specialist

Breaking rules should not always lead to combat immediately. It may create fear, anger, distrust, surveillance, refusal of contact or future consequences.

---

## 13I. Faction Information and Reliability

Factions possess knowledge about the world, but they are not always honest, complete or correct.

Faction information may be:

- true
- partially true
- mythologized
- outdated
- intentionally false
- politically motivated
- misunderstood
- symbolic rather than literal
- useful but incomplete
- accurate only within their territory

Factions may:

- warn truthfully
- lie out of fear
- hide sacred locations
- exaggerate danger
- downplay their own weakness
- blame rivals
- give false maps
- refuse to explain symbols
- interpret ancient technology as religion
- treat old history as myth
- manipulate the expedition into solving their problem

Diplomacy should still require interpretation.

The player should read faction statements the same way they read scout reports: carefully.

---

## 13J. Faction Memory Across Expeditions

Factions should remember meaningful actions by previous expeditions.

This memory should persist across expeditions when appropriate.

Examples of faction memories:

- “A previous expedition opened our grave.”
- “A previous expedition returned our dead.”
- “A previous expedition mapped forbidden land.”
- “A previous expedition healed our sick.”
- “A previous expedition lied to our leader.”
- “A previous expedition killed a border guard.”
- “A previous expedition accepted our guide and respected the path.”
- “A previous expedition stole an artifact.”
- “A previous expedition vanished after entering the forbidden valley.”
- “A previous expedition brought soldiers to our sacred border.”

Faction memory should influence:

- trust
- fear
- respect
- anger
- access to leaders
- willingness to trade
- willingness to guide
- treatment of scouts
- inherited reputation of later expeditions

The world should feel like it remembers, but not perfectly.

Some memories may be distorted, exaggerated or used politically by faction leaders.

---

## 13K. Internal Faction Tensions

Factions may have internal disagreements.

This should be optional for early versions, but important for later depth.

Examples:

- the leader wants peace, but warriors want expulsion
- traders want contact, priests forbid it
- young members are curious, elders are afraid
- one village asks for help against official orders
- a border commander acts more aggressively than the leader intends
- a faction has two rival leaders
- a sick leader creates a power struggle
- a religious group controls access to ruins
- a faction is splitting into two future factions
- a desperate village lies to the expedition to survive

Internal tensions can create conflicting signals.

This supports the core fantasy: the player is not just reading a simple diplomacy screen. The player is trying to understand a living society.

---

## 13L. Faction Archetypes

Faction archetypes can be used during world generation, but they should be combinable rather than rigid.

Possible archetypes:

### The Hospitable

Welcomes visitors, values gifts and stories, but still has strict customs.

### The Border Wardens

Not evil, but highly territorial. They protect a border, pass, wall, valley or secret.

### The Scholars

Value knowledge, symbols, ruins and interpretation. May respect educated expeditions.

### The Warrior Society

Respects strength, discipline and courage. May despise weakness or deception.

### The Traumatized

Fears outsiders because of past violence, disease, betrayal or disaster.

### The Keepers

Protect a place, object, secret, ruin, creature or historical truth.

### The Traders

Open and practical, but opportunistic. May sell information or manipulate routes.

### The Desperate

Needs help. May be honest, manipulative or dangerous depending on pressure.

### The Fanatics

Driven by strict belief, taboo or prophecy. Hard to negotiate with unless rules are understood.

### The Fragmented

No unified response. Different villages, leaders or subgroups behave differently.

### The Deceivers

Use friendliness, gifts or false information to manipulate the expedition.

### The Dying

A collapsing faction. It may ask for help, lash out or guard its last secret.

### The Hidden

The player sees signs of the faction long before understanding who or where they are.

---

## 13M. Leader Conversations

Leader conversations should exist, but should not become huge RPG dialogue trees.

They should offer a small number of meaningful choices based on:

- expedition composition
- known symbols
- previous actions
- faction values
- gifts
- specialists present
- old expedition history
- scout reports
- current faction state

Possible leader conversation options:

- request passage
- ask about a landmark
- ask about a missing scout
- offer trade
- offer medical aid
- return a body
- return an artifact
- apologize for a violation
- promise to avoid a place
- ask about the giant wall
- share a discovery
- hide a discovery
- demand access
- display strength
- show knowledge of symbols
- ask for a guide
- request permission to camp
- negotiate map-making rights
- refuse a demand

Some options require specialists or knowledge.

Examples:

- “We can treat your sick.” Requires medic and supplies.
- “We understand this symbol.” Requires interpreter/scholar and prior evidence.
- “We can repair the bridge.” Requires architect/engineer and tools.
- “We brought back your dead.” Requires recovered body or evidence.
- “We will not map the sacred valley.” Requires knowledge of the taboo.

Leader conversations should create consequences, not simply provide lore.

---

## 13N. Team Composition and Faction Perception

Factions should react to who the expedition brings.

Examples:

- Many soldiers may impress a warrior faction but frighten a peaceful or traumatized faction.
- Scholars may impress knowledge-focused factions but anger factions that protect secrets.
- A medic may create trust with a suffering village.
- An architect may be valuable to a faction with damaged structures.
- A cartographer may be seen as a spy by secretive factions.
- A negotiator may reduce first-contact risk.
- A small group may be accepted where a large expedition would be blocked.
- Visible weapons may increase respect, fear or anger depending on the faction.

The player should consider faction expectations when planning an expedition.

This makes team composition part of exploration, not just risk management.

---

## 13O. Design Rule: Diplomacy as Exploration

Diplomacy should support the same core loop as terrain exploration.

The player observes, interprets, tests, risks, learns and updates assumptions.

The game should avoid fully transparent diplomacy mechanics such as:

- exact visible reputation numbers
- automatic “this faction likes this” tooltips for every action
- always-visible borders
- guaranteed safe dialogue choices
- simple good/evil faction labels

Instead, faction systems should emphasize:

- clues
- behavior
- memory
- uncertainty
- leadership access
- territorial rules
- cultural values
- consequences
- imperfect information

The player should feel that understanding a faction is as important as discovering a ruin or crossing a mountain range.


---

## 13P. MVP Faction System

For the MVP, the island should contain **three factions**.

This is enough to show a useful range of faction behavior without making the first prototype too complex.

The three MVP factions are:

1. a friendly/open faction
2. a cautious territorial faction
3. a dangerous hidden/isolationist faction

This gives the player three important lessons:

- some factions can help
- some factions have rules and borders that must be understood
- some factions can be deadly in clearly warned zones

### 13P.1 MVP Faction Data

For the MVP, each faction should have:

- name
- hidden territory / influence area
- visible signs
- base attitude
- contact status
- trust
- anger
- fear
- main value
- main taboo
- leader or contact person
- access requirement for meaningful conversation
- reaction to border violation
- reaction to help
- memory of major expedition actions

The internal values do not need to be shown as exact numbers.

The player should read the faction state through descriptions, reports, behavior and visible changes.

Examples:

- “Watchers remain at the treeline.”
- “The warning markers have been renewed.”
- “The messenger no longer keeps a hand on his weapon.”
- “Your camp is watched through the night.”
- “The village refuses trade after what happened at the grave.”

### 13P.2 MVP Contact Status

MVP contact status values:

- Unknown
- Traces Discovered
- Observed
- Warned
- First Contact
- Audience Possible
- Trusted
- Hostile

This should be enough for the first playable version.

Later versions can add more subtle states and internal faction politics.

### 13P.3 MVP Internal Values

The MVP uses three internal values.

### Trust

Trust increases when the expedition:

- respects warnings
- helps faction members
- returns bodies or artifacts
- keeps promises
- avoids taboo violations
- approaches with appropriate team composition
- withdraws when asked

### Anger

Anger increases when the expedition:

- ignores warnings
- enters forbidden territory
- opens graves
- steals artifacts
- kills faction members
- maps forbidden places
- breaks promises
- opens sealed places linked to the faction

### Fear

Fear increases when the expedition:

- brings many soldiers
- acts aggressively
- displays unknown tools or weapons
- survives places the faction fears
- collects dangerous knowledge
- breaks seals
- returns from forbidden zones

Fear is not the same as anger.

A faction may fear the expedition without hating it. A fearful faction may avoid contact, sabotage routes, hide settlements or send warnings instead of attacking directly.

### 13P.4 MVP Faction 1: Coastal People / River Villages

Working names:

- Coastal People
- River Villages
- Shore Villages

This faction is open, friendly and locally focused.

Purpose in the MVP:

- introduce positive faction interaction
- teach basic trade
- teach that local people understand the world better than the expedition
- provide rumors and warnings
- show that factions are not simply enemies
- create emotional stakes if later consequences harm them

Base attitude:

- friendly
- curious
- cautious but welcoming

Main value:

- practical help and fair exchange

Main taboo:

- bringing danger from the island interior back toward their villages

Possible visible signs:

- fishing camps
- river markers
- small docks
- smoke columns
- woven warning charms
- maintained paths
- boats or drying nets

Possible interactions:

- trade supplies
- ask about nearby terrain
- ask about warning signs
- ask about the Grenzwächter
- ask about old expeditions
- offer medicine
- help repair a dock or water system
- ask about the island interior

Possible knowledge:

- local routes
- nearby hazards
- rumors about the interior
- warnings about border markers
- stories about missing people
- incomplete knowledge about the wall or sealed places

Important rule:

The Coastal People should be helpful, but not omniscient.

Their knowledge is local and shaped by stories, fear and experience.

### 13P.5 MVP Faction 2: Border Wardens

Working names:

- Border Wardens
- Grenzwächter
- Keepers of the Line

This faction is cautious, territorial and rule-driven.

Purpose in the MVP:

- teach hidden borders
- teach warning signs
- teach territorial rules
- teach that respecting a faction can open contact
- teach that aggression is not always the right solution

Base attitude:

- cautious
- territorial
- not automatically hostile

Main value:

- restraint

Main taboo:

- entering or mapping forbidden zones

Possible visible signs:

- carved border posts
- stone lines
- warning arrows in the ground
- cloth bands on trees
- watch platforms
- old graves along a boundary
- arranged stones
- smoke signals

Possible leader/contact:

- a border elder
- a commander
- a spokesperson
- a small council

Access requirement examples:

- stop after receiving a warning
- leave a forbidden zone
- return a body
- avoid opening a grave
- approach with a small group
- bring scholar/mediator
- come without aggressive posture

Possible interactions:

- request passage
- ask about warning markers
- ask why a region is forbidden
- offer apology for crossing a border
- return remains
- promise not to map sacred ground
- ask about the wall or sealed ruin

Important reveal potential:

The Border Wardens may not be protecting territory from outsiders.

They may be maintaining a boundary because something beyond it should not be disturbed.

### 13P.6 MVP Faction 3: Hidden / Isolationist Faction

Working names:

- The Hidden
- The Verborgenen
- The Silent Ones
- The Inner Watchers

This faction is dangerous, hidden and difficult to understand.

Purpose in the MVP:

- teach that some zones are truly dangerous
- create missing scout events
- introduce lethal consequences in clearly warned areas
- make the player question who is really protecting what
- connect to the deeper mystery of the island

Base attitude:

- isolationist
- secretive
- potentially lethal in forbidden zones

Main value:

- secrecy or containment

Main taboo:

- approaching the sealed ruin, inner forest, old gate or forbidden core

Possible visible signs:

- deliberately erased tracks
- false paths
- silent forest areas
- masked markers
- objects taken from missing scouts
- stones placed across paths
- trees marked from the hidden side
- no open settlements
- signs of observation without direct contact

Contact approach:

The player should not immediately meet the true leader.

First contact may happen through:

- a masked messenger
- a voice from concealment
- a returned object
- a warning left at camp
- a captured scout released with a message
- indirect contact through another faction

Lethality rule:

This faction may kill scouts or expedition members, but only in clearly dangerous zones or after warning signs.

A cautious player should be able to recognize increasing danger before disaster.

Possible interactions later:

- stop entering the forbidden zone
- return something taken
- explain why the expedition came
- ask about missing scout
- promise not to open a sealed place
- show evidence of understanding the warnings
- negotiate limited access

Important reveal potential:

The Hidden may appear hostile because they guard something dangerous.

They may not be enemies.

They may be the faction that best understands the island’s deepest danger.

### 13P.7 MVP Faction World Phase Actions

In the MVP, factions do not need full simulation.

During the world phase, they may perform simple reactions:

- place or renew warning signs
- observe the expedition
- move a patrol
- intercept a scout
- frighten or mislead a scout
- capture or kill a scout in dangerous zones
- send a messenger
- block a route
- hide a settlement
- refuse trade
- offer contact
- react to taboo violation
- change contact status

This is enough to make the world feel reactive.

### 13P.8 MVP Faction Interaction Menu

When contact is possible, the player may choose from a small set of options.

Possible MVP options:

- request passage
- ask about warning signs
- ask about a special location
- ask about a missing scout
- offer trade
- offer help
- apologize for a violation
- promise to avoid a location
- show a discovered symbol or object
- withdraw peacefully

Role-based options:

- Medic: offer medical help
- Engineer: offer repair or construction help
- Scholar/Mediator: interpret signs or speak respectfully
- Soldier: show strength or offer protection
- Scout: present observed route or evidence

Dialogue should create consequences.

It should not become a large RPG dialogue tree in the MVP.


---

## 14. Special Locations

The world contains special locations that can be discovered.

Special locations should create decisions, not only give rewards.

Some locations are dangerous because of what they contain. Others are dangerous because the player may misunderstand why they exist.

Examples:

- graves
- ruins
- ancient temples
- abandoned camps
- giant walls
- strange towers
- forbidden forests
- sacred stones
- crashed or buried structures
- old battlefields
- hidden passes
- sealed gates
- lost settlements
- caves
- unknown machines
- natural wonders
- corrupted areas

### 14.1 Special Location Decisions

A special location may offer choices:

- investigate
- avoid
- mark for later
- send a small team
- keep the expedition together
- camp nearby
- ask a faction about it
- seal it
- open it
- map it only
- plunder it
- return an item
- leave an offering
- destroy it
- record symbols
- send a scout first

Choices should have consequences.

### 14.2 Example: Grave

Possible actions:

- open the grave and risk angering a faction
- examine it without disturbing it
- mark it on the map
- leave it untouched
- bury found remains respectfully
- take an artifact
- return later with a specialist

Possible outcomes:

- gain historical knowledge
- gain a useful item
- anger a local faction
- trigger a warning
- discover a scout’s body
- find a map fragment
- learn a symbol
- create a long-term curse, rumor or reputation effect

### 14.3 Example: Giant Wall

A giant wall may:

- block progress
- indicate an ancient border
- protect something
- keep something contained
- require a route around it
- contain a gate
- be guarded by a faction
- have old warning signs
- become meaningful only after several expeditions

The wall should not only be treated as an obstacle.

It should create questions:

- Is it meant to stop us from entering?
- Is it meant to stop something from leaving?
- Are the defenses facing the expected direction?
- Who built it?
- Who maintains it now?
- Why do factions nearby fear it?
- What happens if the expedition opens a gate or creates a breach?

The wall should not necessarily be solved immediately.

Misunderstanding the wall may have major consequences.

---

## 14A. Consequential Discoveries

Major discoveries should not be neutral.

Some locations contain knowledge, resources, access to new regions or major historical truth, but interacting with them may permanently change the world.

The player should understand that opening, breaking, stealing, sealing, burning, mapping or revealing something can have consequences beyond the current expedition.

Examples of consequential actions:

- opening a sealed ruin
- breaking a wall or gate
- disturbing a grave
- removing an artifact
- copying forbidden symbols
- revealing a hidden route
- mapping sacred territory
- helping one faction against another
- destroying a seal
- repairing an old machine
- entering a forbidden valley
- bringing an object back to base
- telling a faction what was found
- hiding a discovery from everyone

Consequences may include:

- faction anger or panic
- new hostile zones
- changed faction borders
- abandoned settlements
- new routes opening
- old routes becoming unsafe
- released creatures or factions
- disease, corruption or supernatural effects spreading
- new scout report patterns
- later expeditions inheriting new dangers
- previously hostile factions becoming understandable
- friendly factions losing trust
- major archive updates
- new long-term mysteries

The game should allow the player to make dangerous choices, but major consequences should feel earned, not random.

A player who triggers a disaster should usually be able to look back and think:

> The warning signs were there. I misunderstood them or ignored them.

---

## 14B. Ambiguous Purpose of Structures

Special locations should not only ask:

> How can the player overcome this?

They should also ask:

> What was this place for, and what happens if the player misunderstands it?

A structure may not mean what it first appears to mean.

Examples:

### Wall

A giant wall is not only an obstacle. It is a question.

The player should ask:

- Was it built to keep us out?
- Was it built to keep something in?
- Who built it?
- Who maintains it?
- Why do nearby factions fear it?
- Are the warnings meant for intruders, or for those who might open it?
- What happens if the expedition creates a breach?

Possible clues:

- defenses face inward, not outward
- gates are barred from the outside
- watchtowers look into the enclosed region
- nearby factions live away from the wall
- old graves are all on one side
- claw marks, burn marks or impact damage are on the inside
- warnings describe sealing, hunger, silence or containment
- local myths call it a cage, mouth, lid or oath rather than a border

### Grave

A grave may honor the dead, warn against the dead, hide a shameful truth or seal something that should not return.

The player should ask:

- Was this person respected or feared?
- Was the grave built to remember them or contain them?
- Why is the grave marked but untouched?
- Why do locals leave offerings but refuse to speak the name?

### Temple

A temple may be a sacred place, a political symbol, an old control room, a quarantine station, a prison or a warning system.

### Road

A road may be a trade route, pilgrimage path, military corridor, evacuation route or trap.

### Tower

A tower may be a lookout, signal station, prison, lure, monument or containment mechanism.

Discovery is not complete when the location is found.

Discovery is complete only when the player begins to understand what the location means.

---

## 14C. Sealed Places

Some places are sealed for a reason.

A sealed ruin, wall, gate, tomb, cave, underground city or ancient structure may have been closed to protect something valuable.

It may also have been closed to contain something dangerous.

The expedition should not automatically know which is true.

The player must interpret clues, faction warnings, architecture, old reports and environmental signs before deciding what to do.

Possible sealed places:

- sealed ruins
- underground cities
- tomb complexes
- walled valleys
- collapsed temples
- locked gates
- buried machines
- quarantine zones
- cursed forests
- ancient prisons
- flooded chambers
- sealed mines
- forbidden islands

Possible reasons for sealing:

- to protect treasure or knowledge
- to hide shameful history
- to contain disease
- to imprison a faction
- to trap creatures
- to stop a supernatural force
- to isolate the dead
- to prevent an old machine from restarting
- to protect the outside world
- to protect the inside world from outsiders
- to preserve a pact between factions
- to prevent memory or knowledge from spreading

The player should be able to:

- leave the place sealed
- mark it for later
- ask nearby factions about it
- study symbols
- send scouts around it
- open it carefully
- break it by force
- return with specialists
- reinforce the seal
- partially investigate without opening
- negotiate access
- ignore warnings and open it anyway

Opening a sealed place should be a serious decision.

---

## 14D. Supernatural and Unnatural Elements

The game may include supernatural, unnatural or mythic elements.

These elements should be rare, mysterious and consequential.

They should not turn the game into a monster-hunting game.

The focus remains exploration, interpretation and consequence.

Possible supernatural or unnatural elements:

- a sealed “zombie” faction trapped inside a ruin
- dead people who do not decay
- a forest that imitates voices of past expeditions
- a sickness that behaves like possession
- a faction that does not age but cannot leave its territory
- a river that causes memory loss
- an ancient ruin that changes those who sleep near it
- a wall that seems grown rather than built
- creatures that only appear after a seal is broken
- a valley where old maps become unreliable
- a place where scouts return with impossible reports
- an underground population that is not fully alive
- a curse that may actually be disease, technology, ritual or something unknown

Supernatural elements should often be interpreted differently by different factions.

One faction may call it a curse.

Another may call it a disease.

Another may call it punishment.

Another may know it as an old mistake.

Another may deny it exists.

The player should not always know which explanation is correct.

---

## 14E. Example: Sealed Ruin Containing a Dangerous Faction

A sealed ruin may contain a trapped hostile or unnatural faction.

For example, a “zombie” faction may be sealed inside an ancient ruin.

This does not need to be simple fantasy undead. It could be:

- diseased survivors
- cursed inhabitants
- altered humans
- immortal but decayed people
- victims of an old ritual
- a faction changed by an ancient machine
- a society that survived underground for too long
- dead bodies animated by unknown means
- a misunderstood supernatural phenomenon

The important point is not the monster type.

The important point is the consequence of opening the seal.

If the expedition opens the ruin, possible consequences include:

- the faction escapes
- nearby settlements are abandoned
- faction borders shift as people flee
- old routes become unsafe
- scouts begin disappearing in nearby regions
- other factions blame the expedition
- a previously hostile faction becomes understandable because it was guarding the seal
- later expeditions inherit a more dangerous world
- the archive records the player’s responsibility
- the player may need to find a way to contain the threat again

This should not be a surprise trap with no warning.

Possible warning clues:

- doors are reinforced from the outside
- locks and bars are designed to resist pressure from within
- nearby factions leave offerings but never enter
- animals avoid the area
- scouts hear movement below stone
- old reports mention scratching, chanting or hunger
- symbols warn of containment, not treasure
- a local leader panics when the expedition brings tools near the ruin
- previous expeditions marked the place but refused to open it
- one faction kills trespassers near the ruin but does not enter it themselves

The player may still choose to open it.

The game should respect that choice and let the consequences unfold.

---

## 14F. Consequences Across Expeditions

Major consequences should persist across expeditions.

If one expedition breaks a seal, later expeditions should not return to an unchanged world.

Possible long-term effects:

- a region becomes more dangerous
- a faction disappears
- a faction relocates
- a faction becomes hostile toward all later expeditions
- a new faction appears
- a new threat spreads
- old camps are destroyed
- old notes become dangerously outdated
- recovered journals reveal what went wrong
- the base archive marks the event as a turning point
- other factions refer to “the expedition that opened it”
- later expeditions may attempt repair, containment or negotiation

This strengthens the legacy system.

Expeditions do not only discover the world.

They can damage it, heal it, awaken it, expose it or change its future.

---

## 14G. Design Rule: Discovery Is Not Always Good

The game should avoid the assumption that every discovery is automatically positive.

Some discoveries are gifts.

Some are warnings.

Some are responsibilities.

Some are temptations.

Some are traps.

Some should have remained buried.

The player should feel curiosity, but also caution.

The core question should often be:

> Should we know this?  
> Should we open this?  
> Should we tell others?  
> Can we live with what happens next?


---

## 14H. Standard Special Location Model

Special locations should use a common structure.

This prevents every location from becoming a one-off scripted exception.

A special location is not only a point on the map.

A special location is a decision situation.

Each special location should contain:

- visible type
- hidden purpose
- current state
- discovery method
- visible clues
- hidden clues
- faction links
- possible actions
- required or helpful specialists
- action costs
- immediate consequences
- delayed consequences
- knowledge gained
- map changes
- archive entries
- persistence across expeditions

### 14H.1 Visible Type

What the expedition sees first.

Examples:

- grave
- old marker
- ravine
- broken bridge
- sealed gate
- abandoned camp
- ancient tower
- wall segment
- ruined village
- strange stone circle

### 14H.2 Hidden Purpose

What the location actually is or was meant to do.

Examples:

- warning
- border marker
- containment seal
- sacred site
- quarantine point
- old road system
- trap
- hidden entrance
- memorial
- prison
- false ruin
- abandoned research site
- faction claim marker

The hidden purpose should not be obvious immediately.

The player learns it through clues, specialists, faction interaction and later expeditions.

### 14H.3 Current State

Possible states:

- undiscovered
- reported
- discovered
- marked
- superficially inspected
- thoroughly investigated
- partially opened
- opened
- sealed
- reinforced
- damaged
- triggered
- abandoned
- active
- dangerous
- changed by previous expedition

State must persist across expeditions.

A later expedition should be able to find a place already opened, damaged, marked, reinforced or changed.

### 14H.4 Discovery Method

A location may be discovered by:

- direct expedition movement
- scout report
- faction rumor
- old map
- previous expedition archive
- recovered journal
- visible landmark
- random event
- following tracks or signs

Discovery method affects reliability.

A scout report may be uncertain. Direct exploration is more reliable. Old archive knowledge may be outdated.

### 14H.5 Clues

Every important special location should provide clues.

Clues may be incomplete, but major consequences should feel earned.

Examples:

- doors are barred from outside
- animals avoid the area
- offerings are fresh
- old reports mention sounds under stone
- symbols resemble earlier warnings
- no footprints cross a boundary
- defenses face inward
- a scout feels watched
- local factions refuse to speak the place’s name

If a player makes a disastrous decision, the player should usually be able to look back and realize that warning signs existed.

### 14H.6 Faction Links

Special locations often connect to factions.

Examples:

- a grave is sacred to the Border Wardens
- a sealed gate is guarded by the Hidden
- an abandoned fishing camp matters to the Coastal People
- a wall is feared by multiple factions
- a road crosses invisible territory

Faction links create social consequences.

The question is not only:

> What happens if we open this?

It is also:

> Who cares that we opened this?

### 14H.7 Possible Actions

Possible actions include:

- ignore
- mark for later
- inspect briefly
- investigate thoroughly
- send scout around it
- camp nearby and observe
- leave offering
- open
- seal
- reinforce
- break
- repair
- cross
- search for alternate route
- take object
- leave object
- ask faction about it
- return with specialist
- copy symbols
- refuse to disturb it

The player should often have the option to leave something alone.

### 14H.8 Specialists

Specialists should unlock options, reduce risk or improve interpretation.

They should not only function as simple keys.

Examples:

- Scout: checks surroundings, tracks, hidden observers and alternate routes
- Soldier: reduces danger during forced entry or retreat
- Carrier: enables heavy tools, supplies, recovery of large finds
- Medic: handles disease, contamination, injured people or humanitarian choices
- Engineer: bridges, gates, ruins, structures, mechanisms and safe excavation
- Scholar/Mediator: symbols, taboos, faction meaning and careful communication

### 14H.9 Costs

Special location actions may cost:

- movement points
- one full day
- multiple days
- supplies
- medicine
- tools
- morale
- faction trust
- scout risk
- injury risk

Costs should connect special locations to expedition planning.

### 14H.10 Consequences

Special locations can have immediate and delayed consequences.

Immediate consequences:

- route opened
- injury
- discovery
- artifact found
- faction anger
- morale change
- supplies lost
- new marker added
- scout disappears
- location changes state

Delayed consequences:

- faction memory changes
- new danger zone appears
- sealed threat escapes
- old route becomes unsafe
- new route remains available
- later expedition inherits changed location
- settlement is abandoned
- faction asks for help or revenge
- archive updates with new interpretation

Delayed consequences are especially important for the legacy system.

---

## 14I. MVP Special Locations

The MVP should include a small number of special locations, each teaching a different part of the game.

Recommended MVP set:

### 1. Marked Grave

Purpose:

- teaches respect, taboo and faction memory
- shows that not every discovery should be opened
- connects to the Border Wardens

Possible actions:

- leave untouched
- mark
- inspect without disturbing
- open
- leave offering
- ask faction about it
- return with scholar/mediator

Possible consequences:

- respectful handling increases trust
- opening may anger the Border Wardens
- the grave may contain knowledge or warning
- wrong handling may block leader access

### 2. Deep Ravine / Broken Bridge

Purpose:

- teaches specialist-gated exploration
- shows that some routes must be revisited later
- introduces multi-day projects

Possible actions:

- mark for later
- search for another route
- attempt dangerous crossing
- send scout along the edge
- return with engineer
- build bridge

Possible consequences:

- bridge creates persistent route
- dangerous crossing may injure or kill someone
- construction may attract faction attention
- returning later with engineer feels meaningful

### 3. Abandoned Camp of Earlier Expedition

Purpose:

- teaches legacy
- introduces old reports and unreliable knowledge
- gives emotional connection to earlier failures

Possible actions:

- search camp
- recover journal
- recover supplies
- bury remains
- compare notes with archive
- investigate cause of failure
- mark danger

Possible consequences:

- gain old map information
- gain misleading or outdated information
- discover a missing-person clue
- trigger faction memory
- find evidence of nearby danger

### 4. Sealed Ruin or Gate

Purpose:

- teaches consequential discovery
- shows that opening is not always good
- connects to hidden faction and deeper mystery

Possible actions:

- leave sealed
- mark for later
- study symbols
- ask factions
- send scout around it
- open carefully
- force open
- reinforce seal

Possible consequences:

- gain major knowledge
- anger or panic factions
- release contained danger
- create new danger zone
- change later expeditions

### Optional 5. Distant Wall Landmark

Purpose:

- creates long-term mystery
- provides orientation
- raises the question of containment versus exclusion

The wall should not be solved immediately.

The player should wonder:

> Was this built to keep us out, or to keep something in?

---

## 14J. Future Special Location Generation

For the MVP, special locations may be hand-authored or semi-procedurally placed.

This is acceptable for the first playable version because the core mechanics must be tested in a controlled environment.

However, the long-term game should not rely only on fixed predefined special locations.

Later versions should include a **procedural special location generator**.

The generator should create locations from structured components:

- location type
- visible appearance
- hidden purpose
- clue set
- faction links
- possible actions
- required/helpful specialists
- immediate consequences
- delayed consequences
- world-state impact
- archive entries
- possible false interpretations
- persistence rules

The goal is not to generate generic “points of interest.”

The goal is to generate meaningful mysteries and decisions.

A generated special location should still ask questions such as:

- Who built this?
- Why was it abandoned?
- Is it warning us or inviting us?
- Who cares if we disturb it?
- Is it keeping us out or keeping something in?
- What happens if we misunderstand it?

### 14J.1 Future Generation Quality Rules

Procedurally generated special locations should follow these rules:

1. They must have a purpose beyond loot.
2. They must provide fair clues.
3. They should connect to at least one faction, route, hazard or larger mystery.
4. They should offer more than one possible player response.
5. They should support delayed consequences.
6. They should be able to persist across expeditions.
7. They should sometimes remain unsolved for later expeditions.
8. They should support uncertainty and misinterpretation.
9. They should not all be dangerous.
10. They should not all be beneficial.

### 14J.2 Future Generation Examples

Possible generated combinations:

- grave + containment purpose + Border Warden taboo
- abandoned camp + misleading old map + hidden faction activity
- broken bridge + old trade route + faction dispute
- sealed tower + disease risk + Coastal People rumor
- stone circle + false warning + real route marker
- wall gate + internal damage + fear-based faction myth
- ruined village + no bodies + old expedition note
- cave entrance + offerings + unexplained sounds
- old road + patrol avoidance + route to hidden region

The special location generator should be treated as a major future design topic, separate from the MVP.


---

## 15. Expedition Legacy System

An expedition can fail.

Failure does not necessarily end the game. Instead, a later expedition may be launched into the same world and build upon limited knowledge left behind by earlier expeditions.

The player does not restart from zero.

However, inherited knowledge is incomplete, outdated or partially unreliable.

### 15.1 Expedition Failure

An expedition can fail in different ways:

- all members die
- the expedition runs out of supplies
- morale collapses
- the group is forced to retreat
- the expedition becomes trapped
- the base is abandoned
- a dominant faction expels the expedition
- disease, injury or environmental hazards make progress impossible
- the expedition achieves too little before resources are exhausted
- the expedition loses too many key people
- the expedition disappears without explanation

### 15.2 After Failure

When an expedition fails, the player may start a new expedition into the same world.

The new expedition may inherit:

- partial maps
- known landmarks
- old faction notes
- recovered reports
- base ruins or abandoned outposts
- warnings from previous failures
- scout journals
- suspected borders
- known dangerous areas
- known safe routes
- unclear rumors
- limited reputation effects, depending on what happened

The world advances before the next expedition begins.

If the expedition returned successfully, its field knowledge is secured and converted into Knowledge Points.

If the expedition is lost, its unsecured field knowledge is not automatically secured. It may become a future recovery objective: another expedition might find journals, bodies, map cases, abandoned camps, witness accounts or damaged records.

Lost expeditions should create history, not only punishment.

The base should always be able to launch a weak emergency expedition even if Knowledge Points reach zero.

### 15.3 Knowledge Can Become Outdated

Knowledge gained by previous expeditions is valuable but not always permanently reliable.

Between expeditions, the world may evolve:

- faction territories can expand, shrink or collapse
- settlements can be abandoned, destroyed or founded
- trade routes can appear or disappear
- special locations can change state
- hostile zones can become safe
- safe zones can become hostile
- rumors and old reports can become misleading
- routes can become blocked
- new paths can open

The player must distinguish between confirmed current knowledge, old knowledge and assumptions.

### 15.4 Expedition Archive

The player has access to an archive containing selected information from previous expeditions.

The archive may include:

- old maps
- scout reports
- last known locations
- discovered ruins
- warnings
- faction notes
- sketches of symbols
- causes of expedition failure
- recovered journals
- uncertain assumptions made by previous expeditions
- evidence found later by newer expeditions

The archive is useful, but not perfectly reliable.

The archive is also distinct from Knowledge Points.

Spending Knowledge Points never removes reports, notes, maps or discovered facts from the archive.

---

## 16. Base System

The player starts with a small base.

The base is not a city-building system. It is a support structure for exploration.

The base may provide:

- scouts
- supplies
- medical help
- equipment
- cartography tools
- storage
- specialists
- expedition preparation
- limited research based on discovered knowledge
- archive access

### 16.1 Limited Base Upgrades

Possible upgrades:

- scout quarters: allows more scouts or better scout recovery
- cartographer room: improves map annotation and archive functions
- infirmary: improves recovery of injured expedition members
- supply depot: increases expedition supply capacity
- workshop: unlocks better expedition tools
- archive: preserves more knowledge between expeditions
- signal tower: improves communication or scout return chance
- training yard: improves basic survival chances
- diplomatic tent: helps interpret faction behavior

Upgrades should support exploration, not become the main game.

### 16.2 No Deep Production Economy

The base should avoid complex production chains.

The player should not spend most of the game optimizing:

- wood production
- stone production
- food chains
- worker assignments
- building adjacency
- city layout
- taxation
- army recruitment

The base should remain light and purposeful.

---

## 16A. MVP Base Phase and Meta-Progression

The base is not a city-building system.

The base is an expedition center.

The player returns to base to preserve knowledge, recover people, prepare better, analyze discoveries and launch future expeditions.

The core question at the base is:

> What did we learn, and how do we prepare the next expedition better?

### 16A.1 Base Phase

When an expedition returns, the game enters a Base Phase.

The Base Phase is not just a menu.

Time passes during the Base Phase.

This is important because the world must be able to react to what the expedition did.

Core rule:

> Preparation costs time. Time changes the world.

During the Base Phase, the player may:

- review expedition outcome
- archive reports
- preserve map knowledge
- treat injured people
- recruit replacements
- prepare the next expedition
- unlock or assign specialists
- analyze artifacts or symbols
- choose base upgrades
- compare old and new knowledge
- start a new expedition

### 16A.2 Base Time

Base actions may consume days or weeks.

Examples:

- archive reports: immediate or 1–2 days
- treat light injuries: 3–7 days
- treat serious injuries: 7–14 days or longer
- recruit new people: 7 days or more
- request a specialist: 10–20 days
- analyze an artifact: 3–10 days
- prepare technical equipment: 1–5 days
- create bridge-building materials: several days
- upgrade base facilities: weeks

The MVP can keep this simple, but the concept should be clear:

Time does not stop when the expedition returns.

### 16A.3 World Reactions During Base Phase

While the base prepares, the world may react.

Possible world reactions:

- factions renew warning signs
- factions spread news about the expedition
- a route becomes blocked
- a village is abandoned
- a bridge is discovered, used, damaged or destroyed
- a captured scout is moved
- a hidden faction removes evidence
- a dangerous zone expands
- a released threat spreads
- a friendly faction asks for help
- an old camp is disturbed
- a special location changes state
- faction anger, fear or trust changes
- a new rumor arrives at the base

The player should not be able to freeze the world by returning to base.

However, time pressure should be fair.

The MVP should avoid hard campaign deadlines unless a specific event or mandate creates one.

### 16A.4 Fast Return vs Careful Preparation

The player should often choose between speed and preparation.

Examples:

### Leave Again Quickly

Advantages:

- less time for the world to react
- faster follow-up on fresh clues
- better chance to find missing scouts or unstable situations

Disadvantages:

- injured people may not be ready
- less analysis is completed
- less equipment is prepared
- fewer specialists are available

### Prepare Carefully

Advantages:

- better equipment
- healed members
- analyzed clues
- specialist options
- stronger expedition

Disadvantages:

- more time passes
- factions may react
- routes may change
- opportunities may disappear
- dangers may spread

Time is a strategic cost.

### 16A.5 MVP Base Functions

For the MVP, the base should support:

- expedition assembly
- member recovery
- recruitment
- knowledge archive
- map knowledge preservation
- limited analysis
- limited base upgrades
- start next expedition

This is enough for the first playable version.

### 16A.6 MVP Base Upgrades

MVP upgrades should be few and clear.

Possible upgrades:

### Expanded Quarters

Increases maximum expedition size.

Progression:

- start: 8 people
- upgrade 1: 10 people
- upgrade 2: 12 people
- upgrade 3: 16 people

### Cartography Room

Improves map notes, old map layers and knowledge comparison.

### Infirmary

Improves injury recovery and reduces post-expedition death risk.

### Scout Lodge

Improves scout recruitment and possibly scout recovery.

### Workshop

Unlocks technical equipment, bridge-building materials and improved tools.

### Archive

Preserves more knowledge and makes old reports easier to search.

The base should not use complex production chains.

### 16A.7 Upgrade Currency

The base should not be upgraded with classic city-builder resources such as wood, stone or iron.

Instead, upgrades use **Knowledge Points**.

Knowledge Points represent the practical value generated by discoveries:

- institutional trust
- support from backers
- logistical preparation
- training value
- trade value
- improved planning
- useful maps and reports
- credibility gained by returning with evidence

Knowledge Points are gained by:

- returning successfully with unsecured field knowledge
- completing mandates
- achieving Failure With Knowledge
- discovering important locations
- mapping new regions
- recovering lost reports
- making faction contact
- understanding special locations
- bringing back useful evidence
- preserving valuable knowledge

Progression should reward exploration and understanding.

The player should not farm raw materials to build a base.

Spending Knowledge Points does not erase the underlying archived knowledge.

It represents converting that knowledge into practical support.

### 16A.8 What Persists Between Expeditions

The following should persist:

- archived reports
- map knowledge, with age/reliability state
- discovered special locations
- player notes
- faction notes
- major faction memories
- surviving expedition members and their star levels
- base upgrades
- built bridges or changed routes
- opened, sealed, damaged or changed locations
- released dangers or resolved dangers
- completed or partially completed mandates
- world consequences
- base Knowledge Points
- lost expedition records and possible recovery leads

The following should not be assumed permanently reliable:

- current safety of a route
- exact faction borders
- temporary camps
- unconfirmed scout reports
- rumors
- old maps
- old safe paths
- assumptions made by previous expeditions

Knowledge should be valuable, but not absolute.

Unsecured field knowledge from an active expedition only persists if the expedition returns or if a later recovery event explicitly preserves part of it.

### 16A.9 Meta-Progression Pillars

Meta-progression should come from four sources:

### Archived Knowledge

The player understands the world better.

### Experienced Survivors

Named expedition members become more valuable as they survive and level up.

### Base Improvements

The base enables better preparation and larger or more specialized expeditions.

### Changed World State

The world remembers what the player did.

This is not a traditional tech tree.

The main progression is:

> We know more. We prepare better. We go farther.


---

## 17. Resources

Resources exist to create meaningful exploration decisions.

Possible resources:

- supplies
- food
- medicine
- tools
- expedition members
- scouts
- morale
- carrying capacity
- time
- trust/reputation
- information

Important: **Information is also a resource.**

For the current MVP direction, this resource is formalized as:

- archived knowledge: permanent records
- unsecured field knowledge: expedition-carried value at risk
- Knowledge Points: spendable base resource

Resources should limit expedition range and force return decisions, but should not turn the game into a survival simulator.

The player should feel logistical pressure without being forced into constant micromanagement.


### 17.1 MVP Resource Model

For the MVP, use four main expedition resources:

### Supplies

Food, water and basic consumables.

Consumed daily based on expedition size.

### Medicine

Used to treat injuries, sickness and some faction-help actions.

### Morale

Represents willingness to continue.

Morale may fall because of hunger, death, missing scouts, dangerous places, long expeditions or frightening discoveries.

Low morale may force rest, reduce effectiveness or trigger return pressure.

### Capacity

Limits what the expedition can carry.

Capacity is increased mainly by carriers.

Capacity is used by supplies, medicine, tools, trade goods and special equipment.

The MVP should avoid separate food, water, fuel and crafting resources.

### Knowledge Points

Knowledge Points are not an expedition survival resource.

They are a base resource created when an expedition returns and secures field knowledge.

Knowledge Points may be spent during base preparation on:

- supplies
- medicine
- additional members
- specialists
- equipment
- limited base upgrades
- analysis
- faction trade
- bought information
- recovery missions

The MVP should keep Knowledge Points as a single currency.

Do not add separate knowledge categories until the single-resource model has been playtested.

The player must never become hard-locked by reaching zero Knowledge Points.

At zero Knowledge Points, the base should still allow a weak emergency expedition with minimal supplies, no specialist advantage and low carrying capacity.

---

## 18. Expedition Members

The expedition is not an army.

It is a relatively small group of people with limited capabilities.

Possible roles:

- leader
- scouts
- cartographer
- medic
- guard
- interpreter
- archaeologist
- negotiator
- porter
- local guide
- engineer

The player may need to decide whether to split the group.

Example:

- send two people to explore a ruin
- keep the main group moving
- leave guards at a camp
- send wounded people back
- send a scout ahead
- assign a specialist to study symbols

Splitting the group should create risk and opportunity.

---

## 18A. Expedition Range and Return Pressure

The player should not be able to wander endlessly with one expedition.

Each expedition has a practical range determined by supplies, people, injuries, morale, terrain, scout losses, equipment and specialist availability.

The game should regularly create pressure to return to base, regroup or launch a better-prepared later expedition.

Return pressure should come from understandable expedition limits, not arbitrary timers.

Possible reasons to return:

- food is running low
- medicine is running low
- too many expedition members are injured
- scouts have died or disappeared
- morale is breaking down
- the expedition lacks a required specialist
- the group reaches terrain it cannot cross
- hostile faction pressure becomes too high
- equipment is damaged
- a major discovery needs analysis at the base
- the expedition has gathered more information than it can safely act on
- a route back may soon become unsafe

The player should often face the question:

> Do we push farther, or do we return while we still can?

This is important for pacing. The game should be built around repeated expeditions that go farther over time, not one endless journey.

---

## 18B. Expedition Planning and Team Composition

Before launching an expedition, the player chooses who and what to bring.

The expedition has limited capacity. Bringing more of one role means bringing less of another.

This creates meaningful tradeoffs.

Examples:

- More guards increase safety, but reduce specialist options.
- More scouts improve map coverage, but reduce direct expedition strength.
- More porters increase supplies, but do not help with ruins, negotiation or hazards.
- More specialists unlock more actions, but may make the expedition more vulnerable.
- More medical staff improve survival after injuries, but take space from scouts or soldiers.
- More interpreters improve faction and symbol understanding, but do not help in combat.

The player should not be able to bring every useful role at once, especially early in the game.

The expedition should feel like a carefully prepared team, not an army and not a generic stack of units.

### 18B.1 Example Composition Choices

A cautious expedition:

- more guards
- more supplies
- fewer specialists
- safer travel
- fewer options at ruins or obstacles

A research expedition:

- archaeologist
- architect or engineer
- interpreter
- fewer soldiers
- better at understanding ruins and structures
- more vulnerable in hostile territory

A scouting expedition:

- several scouts
- light supplies
- high movement
- good for revealing terrain
- weak at investigating complex locations

A diplomatic expedition:

- negotiator
- interpreter
- gifts or trade goods
- fewer guards
- better chance of peaceful faction contact
- higher risk if diplomacy fails

A heavy expedition:

- porters
- guards
- engineer
- more supplies
- slower movement
- better for crossing difficult terrain or establishing camps

---

## 18C. Specialist-Gated Exploration

Some locations, obstacles and decisions require specific specialists, tools or preparation.

This should not feel like artificial key-lock design. It should feel logical.

Examples:

- A deep ravine blocks progress unless the expedition has an architect, engineer or bridge-building equipment.
- Ancient inscriptions can be copied by anyone, but only an interpreter or scholar can attempt immediate understanding.
- A damaged gate can be forced with soldiers, repaired with an engineer, bypassed by scouts or studied by an architect.
- A disease-ridden village can be ignored, helped with a medic or misunderstood without one.
- A sacred burial site can be examined respectfully with an anthropologist/interpreter, but may offend factions if handled carelessly.
- A collapsed ruin can be entered safely with an engineer or dangerously without one.
- A hostile faction encounter may be avoided by scouts, negotiated by a diplomat or escalated by soldiers.
- A river crossing may require a guide, engineer, boat equipment or a discovered ford.
- A strange machine may require a scholar, mechanic or later base analysis.

Specialists should create options, not only hard blocks.

If the player lacks the right specialist, there should often still be alternatives:

- mark the location for later
- attempt a risky solution
- search for another route
- ask a faction for help
- return with the right person
- send a scout around the obstacle
- build an outer camp nearby
- recover partial information only

This supports the main game fantasy: each expedition discovers more than it can immediately solve.

---

## 18D. Obstacles and Expedition Readiness

The world should contain obstacles that force the player to think about preparation.

Obstacle examples:

### Natural Obstacles

- ravines
- cliffs
- mountains
- deep rivers
- swamps
- deserts
- glaciers
- dense jungle
- volcanic fields
- storms
- disease zones

### Built or Ancient Obstacles

- walls
- gates
- collapsed bridges
- sealed ruins
- ancient roads
- watchtowers
- traps
- locked mechanisms
- forbidden markers
- broken machines

### Social Obstacles

- faction borders
- sacred territory
- hostile patrols
- villages that refuse entry
- areas under conflict
- taboo zones
- trade routes controlled by factions

Each obstacle should be readable through clues before it becomes a disaster whenever possible.

The player should feel rewarded for preparation and careful interpretation.

---

## 18E. Field Resupply

The expedition can sometimes restock or extend its range while away from base.

However, field resupply should be uncertain, situational and tied to exploration.

Possible resupply methods:

- hunting
- foraging
- fishing
- finding fresh water
- trading with factions
- receiving gifts
- discovering abandoned supplies
- recovering equipment from old camps
- building a supply cache
- returning to an outer camp
- finding remains of previous expeditions
- using local guides
- repairing equipment in the field

Field resupply should not remove the need to return to base.

It should create interesting decisions:

- Do we spend time hunting, risking delay and discovery?
- Do we trade with a faction and reveal our presence?
- Do we use supplies from an old abandoned camp, even if we do not know why it was abandoned?
- Do we split the group to forage while others examine a ruin?
- Do we consume emergency supplies now or save them for the return trip?

Some regions may offer good resupply opportunities. Others may be almost impossible to survive in without preparation.

---

## 18F. Expedition Loadout

An expedition may carry limited equipment.

Possible equipment categories:

- food
- medicine
- tools
- ropes
- bridge-building materials
- mapping tools
- trade goods
- gifts
- weapons
- protective gear
- boats or boat materials
- excavation tools
- signal equipment
- repair kits
- tents
- symbol reference materials

Equipment should create meaningful choices, but not excessive micromanagement.

The player should make broad loadout decisions rather than count every single item.

Example:

- light supplies: faster movement, shorter range
- balanced supplies: normal movement, moderate range
- heavy supplies: slower movement, longer range
- technical equipment: better at obstacles, less room for food
- diplomatic goods: better faction options, less room for tools or medicine

---

## 18G. Return, Analysis and Next Expedition

Returning to base should not feel like failure.

Returning is part of the loop.

At the base, the player can:

- preserve map knowledge
- add reports to the archive
- analyze artifacts
- study symbols
- treat injuries
- recruit or train new expedition members
- adjust future expedition composition
- decide which old mysteries to revisit
- prepare for known obstacles
- compare new reports with old knowledge

A successful expedition may not solve everything. It may simply return with enough knowledge to make the next expedition better.

Example sequence:

1. Expedition 1 discovers a ravine and cannot cross it.
2. The player marks the location and returns.
3. At base, the player prepares an architect/engineer and bridge materials.
4. Expedition 2 crosses the ravine and finds a ruined road.
5. A scout disappears beyond the road.
6. Expedition 3 returns later with more guards and a tracker.

This is the desired rhythm: discover, retreat, prepare, return, go farther.

---

## 18H. Design Rule: Limits Create Exploration

The game should use limits to make exploration more meaningful.

Limits should include:

- limited people
- limited supplies
- limited knowledge
- limited specialists
- limited carrying capacity
- limited trust with factions
- limited time before conditions change
- limited ability to solve every discovery immediately

The player should often discover things they cannot yet understand, cross or safely investigate.

This is not a weakness. This is part of the core game.


---

## 18I. MVP Expedition Model

For the MVP, expedition members have fixed roles.

Later versions may add secondary skills, personality traits or cross-training, but the MVP should remain simple and readable.

Each expedition member has:

- name
- role
- star level
- current state
- injury status
- experience
- expedition history
- optional short flavor text

Every person should have a name.

Losses should matter emotionally. A dead or missing veteran should feel different from losing an anonymous resource.

### 18I.1 MVP Roles

The MVP uses six roles.

### Scout

Scouts are used for advance exploration.

They can be sent in directions away from the main expedition. They reveal terrain, discover clues, observe danger, identify possible faction borders and generate scout reports.

Higher-level scouts:

- survive more often
- detect danger earlier
- produce clearer reports
- identify uncertain borders more reliably
- may return before entering obviously deadly situations

### Soldier / Guard

Soldiers protect the expedition.

They reduce risk from hostile encounters, ambushes, dangerous creatures, panic situations and difficult retreats.

They do not solve every problem.

More soldiers may impress strength-focused factions, but may frighten peaceful, secretive or traumatized factions.

### Carrier

Carriers increase expedition capacity.

They allow the expedition to bring more supplies, medicine, tools, trade goods and technical equipment.

Carriers are central to expedition range, but they also consume supplies and may be vulnerable in danger.

Losing carriers can force an expedition to abandon equipment or return early.

### Medic

Medics treat injuries and sickness.

They reduce death risk, improve recovery and can make it possible to continue after accidents.

A medic may also create faction interaction options, such as helping a sick village.

Without a medic, injuries should become a serious reason to return to base.

### Engineer / Architect

Engineers or architects solve physical and structural problems.

They help with:

- bridges
- ravines
- gates
- collapsed ruins
- walls
- mechanisms
- camps
- repairs
- safe excavation

Without an engineer, some obstacles may be impossible, slower or much riskier.

### Scholar / Mediator

For the MVP, scholar and mediator are combined into one role.

This role helps with:

- symbols
- ruins
- warnings
- faction rules
- first contact
- cultural interpretation
- old records
- unclear signs

The scholar/mediator does not automatically understand the world, but reduces misinterpretation and unlocks better options.

Later versions may split this into separate roles, such as scholar, interpreter, diplomat or anthropologist.

---

## 18J. Star Level System

Each expedition member has a star level.

Initial proposal:

- 0 stars: beginner
- 1 star: experienced
- 2 stars: veteran
- 3 stars: elite

Star levels represent practical field experience.

Higher star levels improve role performance.

Examples:

- a veteran scout writes better reports and survives more often
- a veteran soldier handles ambushes better
- a veteran medic saves more injured people
- a veteran carrier handles difficult travel better
- a veteran engineer completes projects faster or with fewer accidents
- a veteran scholar/mediator recognizes signs and social risks more reliably

Experience should not be handed out constantly.

For the MVP:

- people gain experience mainly by returning from expeditions
- dangerous successful actions may provide additional experience
- dead or missing people do not gain experience
- veterans are difficult to replace

Losing a high-level person should be painful.

---

## 18K. MVP Tutorial Expedition

The first tutorial expedition is predefined.

This helps the game teach the core systems in a controlled way.

Starting expedition size:

> 8 people

Tutorial composition:

- 2 scouts
- 2 soldiers
- 2 carriers
- 1 medic
- 1 scholar/mediator
- 0 engineers

The tutorial expedition is intentionally limited.

It can explore, survive, scout, interpret some signs and handle basic danger, but it cannot solve every obstacle.

This should teach the player an important rule:

> Exploration is not about solving everything immediately.  
> Some discoveries must be marked, archived and revisited by a later, better-prepared expedition.

Example tutorial learning sequence:

1. The player moves from the base and reveals nearby hexes.
2. The player sends a scout in a direction.
3. The scout returns with a report.
4. The player places a map marker based on the report.
5. The expedition discovers a warning sign.
6. The scholar/mediator gives a partial interpretation.
7. The expedition discovers an obstacle such as a ravine or damaged gate.
8. The player learns that an engineer would be required for a safe solution.
9. Supplies, injury, scout danger or morale create pressure to return.
10. The expedition returns to base with knowledge for the next attempt.

---

## 18L. Expedition Size and Base Growth

At the start, the maximum expedition size is 8 people.

Base upgrades may slowly increase expedition size.

Initial proposal:

- start: 8 people
- upgrade level 1: 10 people
- upgrade level 2: 12 people
- upgrade level 3: 16 people

The expedition should remain small enough that individual names and losses matter.

The game should not become army management.

Special late-game missions may later allow exceptions, but the normal structure should remain expedition-focused.

---

## 18M. MVP Capacity Model

For the MVP, capacity should be simple.

Initial proposal:

- each non-carrier provides 2 capacity
- each carrier provides 6 capacity

Capacity is used for:

- supplies
- medicine
- tools
- trade goods
- technical equipment
- special expedition gear

Each person consumes 1 Supply per day regardless of role.

This creates a clear tradeoff:

- more carriers increase range and loadout options
- more carriers also increase daily supply consumption
- fewer carriers mean shorter range and fewer tools
- too few carriers may force early return


---

## 19. Outer Camps and Forward Posts

The game may include small temporary expedition camps.

These are not cities.

Possible camp types:

- temporary camp
- supply cache
- observation post
- cartography post
- healing camp
- safe return point
- abandoned camp from a previous expedition

Camps exist to support deeper exploration.

They should create strategic route decisions without becoming a management-heavy system.

---

## 20. Journal and Clue System

The game should avoid a traditional quest log whenever possible.

Instead, it should use a journal or archive of observations.

Example journal entries:

- “Several scouts mention black stone pillars north of the river.”
- “The signs near the grave resemble those found near the delta.”
- “The people of Talan refuse to enter the western forest.”
- “Expedition 2 marked this road as safe, but no one has checked it in years.”
- “A scout disappeared after crossing the third ridge.”
- “The giant wall may continue further south.”

The journal may organize knowledge, but should not over-explain it.

Avoid entries like:

- “Quest: Find the black stone pillars. 0/3”
- “Objective: Enter the forbidden forest.”
- “Faction border discovered.”

The player should interpret, not simply follow instructions.

---

## 21. Mystery Layers

The world should contain layers of mystery.

### Layer 1: Terrain

The player learns geography, routes, biomes and hazards.

### Layer 2: Factions

The player learns who lives where, how they behave and what they protect.

### Layer 3: Signs and Taboos

The player learns which warnings matter and what certain symbols mean.

### Layer 4: Special Places

The player investigates ruins, graves, walls, sacred sites, sealed places and anomalies.

The player learns that some places are not only hidden, but deliberately contained.

### Layer 5: Larger Truth

The player slowly understands the deeper history or secret of the world.

The game should motivate exploration not only through map completion, but through understanding connections.

---

## 22. Possible Long-Term Goals

The game needs an overall goal, but not a narrow linear path.

Possible high-level goals:

- map an unknown continent
- find a lost expedition
- discover the origin of a giant wall
- establish a safe route through the world
- understand why factions fear certain places
- recover enough knowledge to make a final decision
- reveal the truth behind an ancient civilization
- decide whether the world should be opened, protected, abandoned or sealed

The final goal should be about understanding, not conquest.

---

## 22A. MVP Goal System / Expedition Mandates

The game should support both free exploration and goal-based play.

The goal system should provide direction without turning the game into a linear quest-driven experience.

The player should not simply follow markers.

The player should investigate, interpret and decide.

### 22A.1 Two Play Modes

### Free Exploration Mode

The player explores the island without a fixed primary objective.

The focus is:

- mapping the island
- discovering special locations
- learning faction rules
- improving the archive
- launching better expeditions
- understanding the world at the player's own pace

### Expedition Mandate Mode

A campaign or expedition receives a primary mandate.

The mandate gives the player a reason to explore, but should not fully reveal how to achieve it.

A mandate should act as a compass, not as a quest marker.

Example:

> Find out what happened to the previous expedition.

The game should not immediately show the exact location. The player must gather clues, speak to factions, send scouts, read reports and follow evidence.

---

## 22B. MVP Goal Types

The MVP should include three goal types.

### 22B.1 Find a Specific Place

The player must locate a place somewhere on the island.

Examples:

- find the old signal station
- find the source of the black stone markers
- find the entrance to the island interior
- find the sealed gate
- find the abandoned camp
- find the first visible section of the wall

This goal type tests:

- exploration
- scout reports
- fog of war
- map notes
- landmark clues
- special location discovery

The location should not be revealed directly.

The player may learn about it through:

- scout reports
- faction rumors
- old maps
- recovered journals
- visible landmarks
- repeated symbols
- local warnings

### 22B.2 Establish Contact With a Faction

The player must establish meaningful contact with a specific faction.

Examples:

- establish peaceful contact with the Border Wardens
- gain an audience with a faction leader
- convince a faction to allow limited passage
- open trade with the Coastal People
- make first indirect contact with the Hidden

This goal type tests:

- hidden faction borders
- warning signs
- contact stages
- faction rules
- team composition
- specialist options
- trust, anger and fear values
- leader access requirements

The goal should not be satisfied by merely seeing a faction member.

It requires meaningful interaction, such as:

- first conversation
- trade agreement
- passage permission
- audience with a leader
- accepted apology
- successful help action
- recovered scout negotiation

### 22B.3 Discover the Fate of a Previous Expedition

The player must learn what happened to an earlier expedition.

Examples:

- find the abandoned camp
- recover a journal
- identify where the expedition turned off its route
- discover whether a scout was killed, captured or misled
- learn which faction encountered them
- find out whether they opened something they should not have opened

This goal type fits the legacy system especially well.

It tests:

- old knowledge
- unreliable reports
- map notes
- archive use
- special locations
- scout loss
- faction memories
- emotional consequence

The player may discover partial truth before discovering the full truth.

The truth may also be uncomfortable.

A previous expedition may have caused harm, broken a taboo, opened a sealed place or lied to a faction.

---

## 22C. Goal Success Levels

Goals should not only have binary success or failure.

Use success levels.

### Failure

The expedition fails to make meaningful progress and returns, disappears or collapses.

Even failure may still leave traces or limited knowledge.

### Failure With Knowledge

The expedition does not complete the mandate, but discovers useful information.

Examples:

- the expedition does not find the sealed gate, but finds symbols that point toward it
- the expedition fails to contact the Border Wardens, but learns what behavior angers them
- the expedition does not solve the earlier expedition's fate, but finds their last camp

This is important.

The game should reward learning even when the expedition fails.

### Partial Success

The expedition makes clear progress but does not fully complete the goal.

Examples:

- finds the approximate region of the target location
- establishes indirect contact but not audience
- finds evidence of the previous expedition but not the final explanation

### Success

The expedition completes the mandate.

Examples:

- target location found and confirmed
- meaningful faction contact established
- previous expedition's fate understood well enough to report

### Major Success

The expedition completes the mandate and gains additional valuable knowledge or avoids major harm.

Examples:

- finds the location and identifies its hidden purpose
- establishes contact without violating taboos
- discovers the previous expedition's fate and recovers their archive
- completes the goal while preserving faction trust
- solves the objective without releasing or worsening a danger

---

## 22D. Goal Design Rules

Goal design should follow these rules:

1. A goal provides direction, not a fixed path.
2. A goal should require exploration and interpretation.
3. A goal should not rely on obvious quest markers.
4. A goal should be solvable in more than one way when possible.
5. A goal may produce partial success.
6. Failure can still produce useful knowledge.
7. A goal should connect to the world, not exist as a separate checklist.
8. A goal should interact with factions, special locations, scout reports or the archive.
9. A completed goal may create new questions.
10. Some goals may become harder or easier because of previous expedition actions.

---

## 22E. Later Goal Types

Later versions may add more goal types.

Possible future goals:

- map the entire island or continent
- find the golden city
- understand the purpose of the wall
- find a safe route through the island interior
- decide what to do with a sealed danger
- rescue a missing scout
- prevent a released threat from reaching the coast
- mediate between factions
- recover an artifact without angering its guardians
- establish a lasting safe route
- determine whether a ruin should remain sealed
- uncover the truth behind a major faction myth
- find why maps become unreliable in a region
- confirm or disprove an old expedition report
- evacuate or help a threatened village

These should be added only after the MVP loop is working.


---

## 23. Consequences

Failure should not always mean immediate game over.

Possible consequences:

- a scout dies
- a scout disappears
- a scout is captured
- the expedition loses supplies
- morale drops
- a character is injured
- a faction becomes suspicious
- a faction becomes hostile
- a route becomes blocked
- a camp is abandoned
- a ruin is sealed
- a sealed place is opened
- a contained threat is released
- an unnatural danger spreads
- a faction flees, collapses or changes behavior
- old knowledge becomes invalid
- a future expedition inherits bad reputation
- future expeditions inherit a changed world
- the expedition must retreat

The player should feel consequences, but not be punished randomly.

---

## 23A. Event and Risk System

The game needs an event and risk system that creates uncertainty without feeling random or unfair.

The player should be able to learn how danger works.

Risk should be readable through clues, terrain, reports, faction behavior and previous experience.

Core rule:

> Dangerous outcomes should feel earned, not arbitrary.

### 23A.1 Risk Score

Internally, risky actions may use a Risk Score.

Risk Score may be influenced by:

- terrain
- biome danger
- faction territory
- warning zones
- scout behavior
- scout star level
- expedition size
- number of soldiers
- morale
- injuries
- distance from base
- supplies remaining
- specialists present
- special location state
- previous player decisions
- triggered world consequences

The player does not need to see exact numbers.

The UI should use qualitative descriptions.

Examples:

- low risk
- moderate risk
- high risk
- extreme risk
- unknown risk

Risk estimates may be more accurate if the expedition has the right specialist or better knowledge.

### 23A.2 Risk Outcomes

Risk does not only mean damage.

Possible outcomes:

- nothing happens
- useful clue
- unclear clue
- false interpretation
- resource gain
- resource loss
- morale change
- injury
- delayed return
- lost equipment
- faction reaction
- new marker
- new event trigger
- delayed consequence
- capture
- death
- world state change

A dangerous action may produce knowledge even if it also causes harm.

### 23A.3 Scout Status and Overdue Scouts

An overdue scout is not automatically dead.

A missing or late scout should create uncertainty.

Possible scout states:

- On Mission
- Expected Back
- Overdue
- Returned
- Returned Injured
- Returned Disturbed
- Missing
- Captured
- Dead

The player should only know “Captured” or “Dead” if evidence exists.

A scout may return late because:

- they got lost
- they were followed
- they hid from a patrol
- they were injured
- they found something important and stayed longer
- they were delayed by terrain
- they were captured and released
- they were deliberately misled
- they panicked
- they avoided returning by the original route

An overdue scout should create a question on the map, not an instant answer.

Example:

> Mara should have returned today. She did not.

Later, Mara may:

- return unharmed
- return injured
- return disturbed
- return with valuable information
- return without equipment
- return with a warning
- not return at all
- be found dead
- be found captured
- be found through traces only

### 23A.4 Event Categories

The MVP should support five event categories.

### Scout Events

Examples:

- scout returns with report
- scout overdue
- scout returns injured
- scout returns disturbed
- scout disappears
- scout finds signs of another scout
- scout finds a route
- scout is seen by a faction

### Faction Events

Examples:

- warning sign appears
- messenger arrives
- camp is watched
- trade is offered
- route is blocked
- faction reacts to taboo
- faction contact becomes possible
- faction refuses contact
- captured scout is returned

### Location Events

Examples:

- sounds behind sealed gate
- grave has fresh offerings
- abandoned camp has changed
- ruin partially collapses
- old symbols become readable
- hidden entrance is found
- seal weakens

### Spontaneous One-Off Events

These are one-time or rare events that can occur during movement, exploration or camping.

Examples:

- found sealed box
- stranger offers map
- wounded stranger appears
- abandoned pack found
- strange lights near camp
- old trail appears after rain
- animal leads or misleads the expedition
- someone leaves food near the camp

These events should fit the region, biome, faction situation or history.

They should not feel like random loot boxes.

### Consequence Events

These are caused by earlier decisions.

Examples:

- a released threat spreads
- a village is abandoned
- a faction blocks a route
- warnings intensify
- old bridge is destroyed
- a faction blames the expedition
- an opened sealed place affects nearby regions
- a previous lie is exposed
- a saved faction sends help

Consequence Events are central to the legacy system.

### 23A.5 Spontaneous Event Design

Spontaneous events should create small decisions with incomplete information.

Example: Found Sealed Box

A box lies between roots. It is dry although the ground is wet. A carved symbol marks the lid.

Possible actions:

- open it
- leave it
- mark it
- take it to base
- ask scholar/mediator to inspect it
- ask a faction about it later

Possible outcomes:

- useful map
- old symbol clue
- trap
- disease
- faction property
- worthless object
- delayed consequence

Example: Stranger With Map

A stranger offers to sell a map of the island interior.

Possible actions:

- buy
- refuse
- negotiate
- question them
- watch them
- follow them
- threaten them

Possible outcomes:

- real map
- outdated map
- partially true map
- false map
- trap
- faction reaction
- useful symbol
- wasted trade goods

Spontaneous events should support the core theme:

> Information may be valuable, false, dangerous or misunderstood.

### 23A.6 Event Data Model

Each event may contain:

- event id
- event type
- trigger condition
- biome or region requirements
- faction requirements
- location requirements
- risk level
- description text
- player options
- required or helpful roles
- immediate effects
- delayed effects
- journal entry
- archive entry
- repeat rule
- persistence impact

For one-off events, repeat rule should usually be false.

### 23A.7 MVP Event Scope

The MVP should start with a small event set.

Suggested minimum:

- 3 scout events
- 3 faction events
- 3 location events
- 3 spontaneous one-off events
- 2 consequence events

This is enough to test the system.

Later versions can expand the event library significantly.


---

## 24. MVP Scope

The first playable version should focus on the core identity.


## 24A. Vertical Slice Definition

The MVP exists to answer one question:

> Is this actually fun to play?

The MVP is not meant to prove final content volume, final graphics, full procedural generation or complete faction depth.

It must prove the core loop.

The Vertical Slice succeeds if the first expedition returns with incomplete but useful knowledge, and the player immediately understands how a better-prepared second expedition can go farther.

In German:

> Die erste Expedition schafft nicht alles, aber sie macht die zweite Expedition besser.

That is the heart of the game.

### 24A.1 Vertical Slice Goal

The player starts with the predefined tutorial expedition at the base.

The player explores a small island region, sends scouts, finds clues, encounters faction signs, discovers a location that cannot be solved immediately and returns to base with useful knowledge.

Then the player starts a second expedition that benefits from what the first expedition learned.

The slice must show:

- exploration
- uncertainty
- scout reports
- manual notes
- hidden borders
- return pressure
- base phase
- legacy knowledge
- better preparation
- persistent world reaction

### 24A.2 Target Playtime

Target playtime:

> 30–45 minutes

Suggested structure:

- first expedition: 20–30 minutes
- base phase: 5 minutes
- second expedition: 10–15 minutes

The second expedition does not need to complete a full campaign.

It only needs to demonstrate that previous knowledge matters.

### 24A.3 Vertical Slice Map

The current MVP target map uses a rectangular 40 x 30 hex field.

The playable slice should still feel dense. A 40 x 30 map is acceptable if the starting region, early faction clues, first routes and first special locations are placed close enough to avoid empty travel.

The slice should be small but dense.

It should avoid large empty areas.

### 24A.4 Required Slice Features

The Vertical Slice should include:

- start base
- fog of war
- expedition day counter
- movement points
- supplies, medicine, morale and capacity
- unsecured field knowledge shown during expedition
- predefined 8-person tutorial expedition
- named expedition members
- fixed MVP roles
- scouts with direction, duration, focus and behavior
- scout reports
- manual markers
- free text notes
- at least two biomes plus coast or river
- three active MVP factions: Coastal People, Border Wardens and Hidden Ones
- at least three special locations
- return to base
- base phase with time passing
- archive update
- Knowledge Points awarded on successful return
- at least one basic Knowledge Point spending choice
- inherited knowledge for second expedition
- second expedition with at least one changed preparation option

### 24A.5 Tutorial Expedition in the Slice

The tutorial expedition is predefined:

- 2 scouts
- 2 soldiers
- 2 carriers
- 1 medic
- 1 scholar/mediator
- 0 engineers

This is intentional.

The first expedition should be capable but incomplete.

It should discover something that requires a later better-prepared expedition.

### 24A.6 Suggested First Expedition Flow

### Day 1–2

The player leaves the base and learns:

- movement
- fog of war
- supplies
- day counter

### Day 2–4

The player sends a scout.

The scout returns with a report containing terrain clues, smoke, markers or uncertainty.

The player creates a map marker or note.

### Day 4–6

The expedition finds first faction signs.

Examples:

- carved posts
- stones
- cloth bands
- warning arrows
- distant watchers

The scholar/mediator gives partial interpretation, not certainty.

### Day 6–8

The expedition meets the friendly faction or a representative from a river/coastal village.

This teaches:

- factions can help
- local knowledge matters
- warnings may be useful
- not all information is complete

### Day 8–10

The expedition finds a ravine, broken bridge or obstacle.

Without an engineer, the safe solution is unavailable.

The player may:

- mark it
- search for another route
- send a scout along the edge
- attempt a dangerous crossing
- return later

### Day 10–12

The expedition finds an abandoned camp or evidence of a previous expedition.

This teaches:

- old knowledge
- recovered journals
- partial truth
- legacy
- emotional consequence

### Day 12+

Return pressure appears.

Possible reasons:

- supplies are low
- a scout is overdue
- someone is injured
- morale drops
- faction warning escalates
- the expedition lacks the needed specialist

The player returns to base.

### 24A.7 Base Phase in the Slice

After returning, the Base Phase shows:

- days passing
- people who returned
- injuries
- experience gained
- reports archived
- map knowledge preserved
- notes preserved
- discoveries summarized
- world reactions

Example world reactions:

- Border Wardens renew warning markers near the ravine.
- Coastal People report smoke in the interior.
- The abandoned camp was disturbed after the expedition left.
- A missing scout has still not been found.

The player can then prepare a second expedition.

The second expedition should unlock at least one new meaningful choice, such as bringing an engineer.

### 24A.8 Second Expedition in the Slice

The second expedition should demonstrate legacy.

The player sees:

- old map knowledge
- old markers
- old notes
- old scout reports
- known obstacle
- known suspected border

The player can return to the ravine with an engineer.

Possible result:

- build bridge as multi-day project
- open a persistent route
- trigger faction observation
- create a world-state change

The goal is not to finish the whole game.

The goal is to make the player feel:

> Now I understand why returning mattered.

### 24A.9 Factions in the Slice

The slice should include at least two visible/active factions.

Recommended:

### Coastal People / River Village

Directly contactable.

They teach:

- basic trade
- local knowledge
- friendly contact
- incomplete but useful warnings

### Border Wardens

Mostly indirect at first.

They teach:

- hidden borders
- warning signs
- territorial rules
- faction reaction to player behavior

The Hidden can be present only as a hinted danger.

For example:

- a scout report mentions a silent forest
- a risky scout does not return
- a marker warns against entering deeper territory
- the Coastal People refuse to speak clearly about the interior

Full interaction with the Hidden is not required for the first slice.

### 24A.10 Special Locations in the Slice

The slice should include at least three special locations.

Recommended:

### Marked Grave

Teaches:

- taboo
- respect
- faction memory
- decision not to disturb

### Deep Ravine / Broken Bridge

Teaches:

- specialist-gated exploration
- return and preparation
- multi-day projects
- persistent route changes

### Abandoned Camp

Teaches:

- earlier expedition legacy
- old reports
- partial truth
- emotional stakes

Optional:

### Sealed Gate / Ruin

May be hinted but not fully solved.

It teaches that some places were sealed for a reason.

### Distant Wall Landmark

May appear as a distant visual or rumor.

It should create curiosity, not require full implementation.

### 24A.11 Vertical Slice Success Criteria

The slice is successful if testers say things like:

- I want to know what is beyond the ravine.
- I want to understand why that faction warned me.
- I want to return with a better team.
- I care if my scout does not return.
- My notes helped.
- The world feels like it has reasons.
- I did not understand everything, but I learned enough to continue.
- Returning to base felt useful, not like failure.
- The second expedition felt stronger because of the first.

The slice is not successful if testers say:

- I am just uncovering hexes.
- I do not understand why I should return.
- Scout reports feel irrelevant.
- Factions feel like generic enemies.
- The base feels like a menu with no meaning.
- Events feel random and unfair.
- I do not care who dies.
- I do not feel any reason to take notes.
- The second expedition feels like starting over.

### 24A.12 Explicit Non-Goals for Vertical Slice

The Vertical Slice does not need:

- final graphics
- full world generator
- many biomes
- many factions
- complex combat
- full diplomacy
- complex base management
- large story
- procedural special location generator
- faction wars
- seasons
- large content library
- final balancing
- polished animations
- complete audio
- non-human faction implementation
- full supernatural system

The slice should prove the game feel.

Nothing more.



### 24.1 MVP Must Include

- hex map
- angled/isometric-style presentation or placeholder equivalent
- turn-based expedition day system
- movement points and terrain movement costs
- fog of war
- semi-procedural MVP island
- procedural biome variation
- player starting base
- base phase with time passing
- world reactions during base phase
- main expedition movement
- limited supplies
- daily supply consumption based on expedition size
- MVP resource model: Supplies, Medicine, Morale, Capacity
- expedition range / return pressure
- named expedition members
- fixed MVP roles
- star-level progression
- predefined tutorial expedition
- basic expedition team composition
- basic specialist-gated obstacles
- scouts
- scout direction/duration/focus/behavior orders
- scout reports
- missing scout outcomes
- manual map markers
- standard special location model
- MVP special locations: marked grave, ravine/broken bridge, abandoned camp, sealed ruin/gate
- special locations
- basic consequential discovery system
- simple sealed-place logic
- three MVP factions: friendly/open, territorial/cautious, hidden/dangerous
- simple faction territories
- hidden faction borders
- basic faction attitude system using Trust, Anger and Fear
- basic faction territorial rules
- basic faction leader/contact interaction
- faction memory for major actions
- expedition failure
- starting a new expedition in the same world
- old map knowledge from previous expeditions
- MVP goal system with free exploration and expedition mandates
- three MVP goal types: find place, establish faction contact, discover previous expedition fate
- goal success levels including Failure With Knowledge
- event and risk system
- scout, faction, location, spontaneous and consequence events
- basic UI for markers, free text notes, journal and archive

### 24.2 MVP Should Avoid

- complex combat
- complex city building
- large tech trees
- large-scale economy
- army management
- full 4X diplomacy
- deep survival crafting
- too many unit types
- overly detailed faction simulation

### 24.3 Later Expansion Ideas

- individual scout traits
- deeper expedition member roles
- more complex scout reports
- recovered bodies and journals
- dynamic faction wars
- faction collapse and emergence
- deeper leadership structures
- internal faction tensions
- faction-initiated requests
- language and symbol interpretation
- ruins with multi-step investigation
- procedural special location generator
- more special location types
- deeper sealed-place consequences
- supernatural or unnatural threats
- expedition member personalities
- deeper archive system
- linked knowledge UI
- old map comparison tools
- outer camps
- old routes becoming unsafe
- procedural world history
- multi-expedition mysteries
- expanded event library
- weather and seasonal effects
- diseases and long-term injuries
- hidden legendary locations
- multiple endings based on understanding

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

## 26. Tone and Atmosphere

The tone should be mysterious, exploratory and tense.

The player should feel:

- curiosity
- uncertainty
- wonder
- caution
- responsibility
- loss when scouts die
- satisfaction when old clues finally make sense
- tension when entering unknown territory
- pride when a new expedition benefits from old knowledge

The world should feel ancient, inhabited and dangerous, but not necessarily evil.

Factions should not be simple enemies. They should have reasons, fears, taboos and histories.

---

## 26A. Setting Tone

The setting should be a mysterious post-catastrophic island world.

The expedition is not entering an empty or unclaimed world.

It enters a world with history, rules, inhabitants, borders, taboos, old wounds and dangerous misunderstandings.

Core rule:

> The expedition does not explore an empty world.  
> It enters a world that already has rules.

### 26A.1 What the Setting Is Not

The setting should avoid:

- colonial fantasy
- religious mission framing
- “bringing civilization” narratives
- simple primitive-versus-civilized framing
- treating local factions as obstacles only
- treating the expedition as automatically superior
- reducing factions to enemies, resources or quest dispensers

The expedition has no automatic right to everything it finds.

### 26A.2 Expedition Role

The expedition may be motivated by:

- cartography
- rescue
- research
- contact
- safety assessment
- searching for previous expeditions
- understanding an island-wide threat
- finding a safe route
- investigating old ruins or sealed places

The expedition wants to understand, but it begins with limited knowledge and many wrong assumptions.

### 26A.3 Local Knowledge

Local factions often understand the world better than the expedition.

Their knowledge may be:

- practical
- fragmented
- mythologized
- fearful
- symbolic
- politically distorted
- incomplete
- deliberately hidden

But it should not be treated as inferior.

A faction may describe a sealed ruin as cursed. Another may describe it as diseased. Another may call it a prison. Another may refuse to name it.

The player must interpret these perspectives carefully.

### 26A.4 Non-Human or Altered Groups

The setting may include non-human, altered or unnatural groups.

These groups should not be simple monster factions.

They should have:

- territory
- reasons
- communication methods
- needs
- fears
- signs
- taboos
- memory
- possible interaction paths

Examples:

- an amphibious group controlling rivers and swamps
- an underground people with a different sense of time
- altered humans changed by an ancient ruin
- a faction that communicates through signs, light, sound or offerings
- a group that avoids humans because humans once broke a seal
- a people who cannot leave a specific territory

The player may not immediately understand that communication is happening.

Example scout report tone:

> I do not think they failed to answer us.  
> I think we failed to understand that they had answered.

### 26A.5 Supernatural Tone

Supernatural or unnatural elements may exist.

They should be:

- rare
- mysterious
- consequential
- tied to places, history or old mistakes
- open to multiple interpretations

They should not turn the game into a monster-hunting game.

The core remains exploration and interpretation.


---

## 27. Design Rules

The following rules should guide future decisions.

1. Exploration is the core.
2. Knowledge is progression.
3. The map should not explain itself too easily.
4. Combat is a consequence, not the main loop.
5. The base supports exploration, but does not dominate.
6. Factions react to behavior, team composition and past expedition history.
7. Diplomacy is a form of exploration.
8. Failure should create history.
8. Old knowledge should be useful, but not always safe.
9. Special locations should create choices, not only rewards.
10. The player should interpret clues instead of following obvious quest markers.
11. The game should remain focused and not drift into 4X, survival crafting or city-building.

---

## 28. Open Questions

These questions should be answered later.

### Answered MVP Decisions So Far

The following MVP decisions are currently established:

- The game is turn-based.
- One round equals one expedition day.
- The player phase is followed by a world phase.
- The expedition can move across multiple hexes per day if movement points allow it.
- Resting/camping consumes a full day.
- Supplies are consumed daily based on expedition size.
- MVP resources are Supplies, Medicine, Morale and Capacity.
- MVP expedition members have fixed roles.
- Every expedition member has a name.
- Star levels range from 0 to 3.
- The first tutorial expedition is predefined.
- The first tutorial expedition has 8 members.
- The MVP world is a semi-procedural island.
- The MVP uses three factions: friendly/open, cautious/territorial and hidden/dangerous.
- The MVP uses Trust, Anger and Fear as basic internal faction values.
- The MVP uses a standard special location model.
- MVP special locations include a marked grave, ravine/broken bridge, abandoned camp and sealed ruin/gate.
- A true random world generator is a later major design topic.
- Procedural special location generation is a later major design topic.
- The MVP supports free exploration mode and expedition mandate mode.
- MVP mandate types are: find a specific place, establish contact with a faction, discover the fate of a previous expedition.
- Goals use success levels, including Failure With Knowledge.
- The Base Phase consumes time and allows world reactions.
- The UI supports both quick markers and free text notes.
- The setting is a mysterious post-catastrophic island world, not colonial or missionary framing.
- Non-human or altered groups are possible later, but must have logic and interaction paths.
- The MVP uses an internal risk score with qualitative risk presentation.
- Overdue scouts are not automatically dead.
- The MVP event system includes scout, faction, location, spontaneous one-off and consequence events.
- The Vertical Slice exists to test whether the core loop is fun.
- The Vertical Slice succeeds if the first expedition makes the second expedition meaningfully better.


### Setting

- Is the world historical, fantasy, science-fantasy or alternate history?
- Is there magic, lost technology, religion, myth, or only human societies?
- Why are expeditions being sent?
- Who funds them?
- What is the player’s role exactly?

### World Goal

- What is the long-term mystery?
- Is there a central secret?
- Is the giant wall important to the endgame?
- What does “understanding the world” mean mechanically?

### Expeditions

- How many people are in an expedition?
- Does the player control named characters or abstract groups?
- Can expedition members gain experience?
- Can expedition members refuse orders?

### Scouts

- Are scouts named characters?
- How much can scout reports be trusted?
- Can scouts lie?
- Can scouts defect or join factions?

### Factions

- How many factions should exist per world?
- Are factions human, non-human or mixed?
- Can factions communicate with the player at the start?
- Can the player learn languages or symbols?

### Base

- Is the base permanent?
- Can the base be attacked or abandoned?
- Does the base move?
- Can later expeditions find the ruins of old bases?

### Failure

- What exactly ends an expedition?
- How much knowledge survives failure?
- How much does the world change between expeditions?
- Is there a final game over state?

### UI

- How are uncertain map states visualized?
- How are old notes distinguished from current knowledge?
- How are scout reports linked to the map?
- How much should the UI help the player interpret clues?

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
