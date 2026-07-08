# World, Locations and Events Concept

Source of truth for the semi-procedural island, persistent and dynamic world state, special locations, mysteries, events, consequences and setting tone.

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

The MVP island contains three territorial factions.

All three have land or influence areas that affect movement, scouting, events and risk.

### Faction A: Coastal People / River Villages

This faction is relatively open and friendly.

It teaches:

- basic trade
- local knowledge
- positive first contact
- practical help
- incomplete but useful information

### Faction B: Border Wardens

This faction is cautious, territorial and rule-driven.

It teaches:

- hidden borders
- warning signs
- territorial rules
- restrained contact
- consequences for ignoring warnings

### Faction C: Hidden Ones

This faction controls a deeper region and is extremely aggressive inside its territory.

It teaches:

- escalating warning signs
- genuinely dangerous land
- missing or captured scouts
- indirect communication
- lethal consequences after clear warnings
- the difference between hostility and understandable motives

The Hidden Ones are not merely a rumor or background threat.

Their land exists on the map and their territorial reactions are active in the MVP.

Direct leader contact may remain difficult or rare.

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
- the Coastal People are reachable during early exploration but are not immediately visible
- the Border Wardens occupy a route or boundary that becomes important during early or mid exploration
- the Hidden Ones control deeper dangerous territory behind escalating warnings or difficult terrain
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
- leverage object links
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

If a generated location contains or points toward a leverage object, the generator must also define why that object matters:

- which faction wants it
- which offer, conversation option or leader access it can unlock
- whether taking, returning or trading it changes trust, anger or fear
- whether the information survives a successful return to base
- whether it is lost if the expedition dies or disappears

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
