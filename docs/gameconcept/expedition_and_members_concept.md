# Expedition and Members Concept

Source of truth for expedition flow, daily capacity, team structure, outcomes, character history, camps and conflict.

The player controls one main expedition. It cannot be split; only scouts may temporarily leave it.

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
- decide whether to return to base
- improve the base in limited ways
- launch later expeditions after failure or retirement of a previous expedition

---

## 7. Strategic Decisions During Exploration

The game should create meaningful decisions such as:

- Do I send a scout into unknown territory, knowing they may not return?
- Do I keep the main expedition together and use scouts for limited independent reconnaissance?
- Do I investigate a grave that may contain knowledge, even if it may anger a nearby faction?
- Do I ignore a potentially important discovery because supplies are low?
- Do I trust an old report from a previous expedition?
- Do I enter a territory after seeing repeated warning signs?
- Do I turn back before reaching the next landmark?
- Do I risk a dangerous route because it might bypass a wall or hostile region?

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

## 15A. Expedition Outcomes and Partial Return

An expedition does not always end with either a perfect return or total destruction.

The game supports the following outcome categories:

| Outcome | Base Result | Knowledge Result | Future Possibility |
|---|---|---|---|
| Full Return | expedition reaches base normally | all surviving records, testimony and findings are evaluated | normal recovery and preparation |
| Partial Return | only some members reach base | only knowledge preserved by returning witnesses, records and carried findings is secured | missing members and lost records may be recovered |
| Forced Retreat | expedition returns under pressure | much knowledge may survive, but equipment, findings or records may be abandoned | abandoned material may become a recovery location |
| Captured | expedition or members are held by a faction | no automatic transfer from captured people or equipment | negotiation, exchange, escape or rescue |
| Missing | outcome is unknown | unsecured knowledge remains unavailable | traces, survivors, camps or documents may later be found |
| Confirmed Destroyed | no normal survivors remain | only previously transmitted or later recovered knowledge survives | remains and records may create recovery events |

### Knowledge Preservation Types

Knowledge sources should record how they can survive a partial outcome.

Suggested preservation types:

- **Shared Memory:** broadly known by the main expedition; secured if at least one relevant survivor returns
- **Witness-Bound:** depends on a specific person returning or later being recovered
- **Written Record:** survives only if the journal, map case or copied report reaches the base
- **Physical Finding:** survives only if the object or sample reaches the base
- **Previously Transmitted:** already sent to the base or another reliable contact before the outcome

For the MVP, the system may use authored preservation results rather than simulating every individual paper.

The player-facing summary must explain why information survived or was lost.

Example:

```text
Partial Return

Returned:
- Mara
- Tovin
- Edda

Secured:
- route to the black stones
- Mara's scout report
- sketch of the warning posts

Lost:
- sealed metal fragment
- engineer's field journal
- observations known only to the missing scholar
```

### Recovery

Lost knowledge is not permanently erased from the world.

Later expeditions may recover:

- journals
- map cases
- bodies
- survivor testimony
- abandoned findings
- camp records
- messages
- faction-held belongings

Recovered knowledge may be incomplete, damaged, old or doubtful.

This system should turn failure into future history and new objectives.

---

## 18. Expedition Members

The expedition is not an army.

It is a relatively small group of named people with limited capabilities, histories and personal consequences.

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

There is exactly one player-controlled main expedition.

The player cannot divide it into separately controlled groups.

Only scouts may temporarily leave the main expedition through the scout mission system.

This rule avoids:

- duplicated movement management
- separate supply pools
- simultaneous event resolution
- unclear return states
- excessive tactical micromanagement

Specialists may work on a local action while the expedition remains in the same area, but this does not create a second controllable expedition.

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
- Do we separate the main expedition to forage while others examine a ruin?
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

## 18N. Persistent Member History

Every named expedition member has a persistent personal history.

The history is not limited to one or two traits or memories.

The game may record any number of meaningful events across the character's lifetime.

Examples:

- expeditions joined
- days spent in the field
- locations discovered
- scout missions completed
- injuries survived
- missing periods
- people rescued
- bodies recovered
- faction contacts
- promises made
- warnings ignored
- findings carried home
- major reports authored
- companions lost
- successful negotiations
- taboo violations
- participation in retreats
- recovery after capture
- role in a major discovery

These records may affect:

- biography
- report tone
- available dialogue
- faction recognition
- traits or star progression
- morale reactions
- memorials and archive links
- player attachment

The underlying history may be unbounded.

The UI should remain readable through:

- chronological timeline
- filters
- important-event highlighting
- summary statistics
- links to expeditions, reports, locations and factions

Do not delete old achievements merely to keep the biography short.

---

## 19. Outer Camps and Forward Posts

The MVP supports temporary field camps for resting and local actions.

A temporary camp:

- remains attached to the current expedition
- does not become a second base
- does not recruit or produce resources
- does not have worker assignment or construction management
- may be noticed, disturbed or found by factions
- may leave a persistent trace after the expedition departs

The MVP may also include a small number of authored supply caches or abandoned camps.

The MVP does not include a player-managed network of forward bases.

Possible later features:

- supply cache
- observation post
- cartography post
- healing camp
- safe return point
- recovered or abandoned camp from a previous expedition

These later features must support route decisions without becoming a city-building or logistics-management layer.

## 23B. Conflict and Combat Resolution

The MVP has no tactical battle system and no separate combat screen.

Conflict is resolved through events and choices.

Possible player options include:

- withdraw
- flee
- defend
- negotiate
- surrender Supplies or equipment
- show strength
- protect a wounded member
- accept capture
- attempt a risky breakthrough
- use terrain or a known route
- call on a faction agreement
- sacrifice time or resources

Soldiers and guards matter because they:

- reduce some risks
- unlock defensive options
- protect scouts and specialists
- influence faction fear and respect
- reduce losses during retreat
- improve the chance of surviving violence

They do not create a separate tactical minigame.

Conflict outcomes may include:

- safe withdrawal
- injury
- death
- capture
- lost Supplies
- damaged equipment
- increased faction Anger or Fear
- blocked access
- new knowledge
- delayed return
- missing members

Core rule:

> Combat is a consequence within exploration, not a second main game.

The design may later add more detailed conflict resolution, but it must remain subordinate to exploration, interpretation and consequence.

---
