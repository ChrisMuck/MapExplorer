# Knowledge, Base and Analysis Concept

Source of truth for Archived Knowledge, Unsecured Field Knowledge, Knowledge Points, findings, analysis and Base Phase progression.

Analysis consumes time and produces knowledge. It never consumes Knowledge Points.

---

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

### Findings and Analysis

Findings are concrete things, traces, samples or records discovered by an expedition that are not
fully understood in the field.

They are the content backbone of the "Wissen auswerten" base tab.

Important rule:

> The base evaluation queue starts empty. Findings enter the queue only after the expedition discovers
> something and brings back enough evidence, testimony, sketches or samples to study.

Examples:

- Fremde Saatkoerner
- Unbekanntes Metall
- Unbekannte Waffe
- Bruchstueck einer Karte
- Geschnitzte Grenzzeichen
- Maskiertes Symbol
- Versiegeltes Gefaess
- Alte Lageraufzeichnungen
- Verfaerbte Knochen
- Fremde Medizin
- Verbrannte Werkzeuge
- Unbekannte Munition
- Stein mit Warnritual
- Wasserprobe aus einem verbotenen Bach

Findings are not generic research tasks.

Each finding should answer at least one player-facing question:

- What is this?
- Who made it?
- Why is it here?
- Is it dangerous?
- Which faction cares about it?
- Does it reveal a route, taboo, warning, technology, resource or history?
- Does it make a previous report more reliable or less reliable?

#### Finding Lifecycle

1. Expedition discovers a finding in the field.
2. The finding is added to the active expedition as unsecured evidence.
3. If the expedition returns, the finding becomes a base analysis item.
4. Analysis consumes base time.
5. Analysis produces an explanation, archive entry and possible Knowledge Points.
6. Some findings unlock map notes, faction dialogue, offers, warnings, routes or future events.
7. If the expedition is lost, physical findings are lost unless a later expedition recovers them.

Some findings may be only recorded instead of physically carried.

Examples:

- sketch of warning signs: can survive as a report if the scout returns
- metal object: must be physically returned
- overheard faction phrase: depends on witness survival
- map fragment: can be damaged or lost

#### MVP Finding Categories

Use authored ranges. Exact values can be picked per item or deterministically within the range when
the item is generated.

| Category | Examples | Analysis Time | Knowledge Reward |
|---|---|---:|---:|
| Minor trace | carved marks, ash pattern, camp remains | 1-2 days | 1-3 Knowledge |
| Plant or food sample | strange seeds, fungus, preserved grain | 2-4 days | 2-6 Knowledge |
| Environmental sample | water, spores, animal remains, soil | 3-6 days | 3-8 Knowledge |
| Map or written fragment | map shard, journal page, coded note | 1-4 days | 3-10 Knowledge |
| Faction sign or object | warning post, grave token, banner, mask mark | 2-5 days | 4-12 Knowledge |
| Technical object | unknown metal, broken device, unusual weapon | 4-8 days | 6-16 Knowledge |
| Dangerous sample | corrupted matter, sealed residue, infected remains | 5-10 days | 8-20 Knowledge |
| Major mystery object | sealed relic, key object, core symbol | 7-14 days | 15-35 Knowledge |
| Lost expedition record | recovered journal, map case, last camp notes | 2-6 days | 5-18 Knowledge |

MVP recommendation:

- most early findings should use 1-5 days and 2-10 Knowledge
- major findings should be rare and use 7-14 days and 15-35 Knowledge
- do not overload the first expedition with too many findings; one or two meaningful items are
  better than a full research queue

#### Analysis Output

Every completed analysis must produce a readable explanation.

Bad result:

```text
4 Knowledge
```

Good result:

```text
Fremde Saatkoerner

The seeds are not native to the coast. They germinate quickly in poor soil and were stored in waxed
cloth, suggesting they were carried for planned travel rather than eaten locally. This may explain
how inland groups survive long dry crossings.

Effect:
- Archive insight added
- +4 Knowledge
- New question: who cultivates these seeds?
```

Analysis should teach the player something about the world.

#### Data-Driven Finding Definitions

Findings should be authored in external JSON files so content can be expanded without changing code.

Suggested file location:

```text
Assets/GameData/Findings/*.json
```

Suggested definition shape:

```json
{
  "id": "finding-foreign-seeds",
  "displayName": "Fremde Saatkoerner",
  "category": "PlantSample",
  "rarity": "Common",
  "sourceTags": ["abandoned-camp", "coastal-route", "food"],
  "minAnalysisDays": 2,
  "maxAnalysisDays": 4,
  "minKnowledgeReward": 2,
  "maxKnowledgeReward": 6,
  "requiresPhysicalReturn": true,
  "requiredSpecialistRoles": ["Scholar"],
  "relatedFactionIds": ["coastal-people"],
  "relatedLocationKinds": ["AbandonedCamp"],
  "fieldDescription": "Small dark seeds wrapped in waxed cloth.",
  "analysisTitle": "A travel crop, not local food",
  "analysisExplanation": "The seeds germinate quickly in poor soil and were packed for transport. Someone uses them to survive long inland crossings.",
  "archiveKind": "Insight",
  "unlocks": [
    { "kind": "MapQuestion", "id": "who-cultivates-travel-crop" },
    { "kind": "FactionTopic", "id": "ask-coastal-about-inland-crop" }
  ],
  "repeatPolicy": "OncePerSource"
}
```

Rules:

- `id` is stable and unique.
- `sourceTags` let map generation place findings in appropriate contexts.
- `minAnalysisDays` / `maxAnalysisDays` and `minKnowledgeReward` / `maxKnowledgeReward` define the
  authored range.
- the chosen actual values should be deterministic for a generated world seed.
- `fieldDescription` is what the expedition can observe before analysis.
- `analysisExplanation` is the player-facing payoff after analysis.
- `unlocks` can point to map questions, faction topics, trade offers, warnings, routes or events.
- `repeatPolicy` prevents farming.

#### Finding Tables

Normal exploration findings should usually come from authored **Finding Tables** rather than a
single guaranteed finding. This provides variation between generated locations without turning
findings into material loot or adding location-specific code. A direct finding reference remains
valid for deliberate narrative discoveries that must always occur.

A Finding Table contains weighted finding candidates and may also contain an explicit empty
result. Entries can be gated by archetype, interaction/operational/presence state, generated
context tags and already acquired findings. Hidden context influences the roll in World State but
is never disclosed by the player UI.

Rules:

- table resolution uses the deterministic world random source;
- the resolved result, including an empty result, is stored immediately in World State;
- loading, reopening a panel or repeating a query never rolls again;
- unavailable entries are removed before weights are evaluated;
- a finding's own repeat policy still prevents duplicate farming;
- table repeat policy controls whether the table rolls once per location, once per state or for a
  specifically repeatable action;
- an empty result is a valid atmospheric outcome and does not require compensation loot;
- only acquired findings enter the unsecured field inventory and normal return/analysis lifecycle;
- guaranteed story findings continue to use a direct `AddFinding` effect.

Suggested authoring shape:

```json
{
  "id": "finding-table-restless-marsh",
  "rolls": 1,
  "repeatPolicy": "once-per-location",
  "entries": [
    { "findingId": "finding-marsh-water-sample", "weight": 40 },
    {
      "findingId": "finding-unusual-spores",
      "weight": 25,
      "requiresContextTagsAny": ["contaminated", "spreading"]
    },
    {
      "findingId": "finding-animal-traces",
      "weight": 20,
      "requiresContextTagsAny": ["creature-presence"]
    },
    { "findingId": null, "weight": 15 }
  ]
}
```

---

### Analysis Cost Rule

Analysis of findings always costs base time and produces understanding.

It never costs Knowledge Points.

Normal MVP analysis requires only:

- a secured finding, record, sample or sufficient testimony
- an available analysis slot
- the required number of base days

Complex analysis may later require one or more prerequisites:

- a specific specialist
- a base facility
- a tool
- a comparison sample
- a translated symbol set
- access to a faction expert
- a safe containment method

Missing prerequisites pause or lock the analysis. They do not turn analysis into a Knowledge Point fee.

Core rule:

> Analysis spends time to create knowledge. It does not spend knowledge to create knowledge.

### Purchased Information and Discovery Rewards

Buying information reduces risk and may remove part of the reward for independent discovery.

Each relevant discovery may define:

```text
Base Discovery Reward
Independent Discovery Bonus
```

Example:

```text
Base Discovery Reward:          15 Knowledge
Independent Discovery Bonus:    10 Knowledge
```

If the expedition finds the location independently:

```text
Total reward: 25 Knowledge
```

If the expedition previously bought a sufficiently useful hint:

```text
Total reward: 15 Knowledge
```

The purchased hint removes the independent-discovery bonus, but the discovery itself remains valuable.

A vague rumor may reduce only part of the bonus. A detailed map or guide may remove it completely.

The reward system must record which information assisted the discovery so the same hint cannot be exploited repeatedly.

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

### 16A.1A Current Base Camp Implementation

The current Unity prototype has a dedicated Base Camp screen. This is now part of the MVP loop, not
only a later meta-progression idea.

The Base Camp screen currently covers:

- team review and expedition team composition
- member detail view with role, status, traits, skills, gear and biography
- base actions such as healing, recruitment, engineer request, supply preparation and base time
  advancement
- next expedition setup with selected members, porter/soldier support units, rations, medicine and
  readiness preview
- limited base upgrades paid with Knowledge Points
- knowledge evaluation queue that matures over base days and can create archived insights
- faction overview
- typed, searchable archive with reports, notes, insights and treaties

Design boundary:

> Base Camp is the preparation and memory hub of the expedition game. It must not become a separate
> city-builder, production chain or 4X management layer.

Open MVP work remains:

- tighten the normal return / lost expedition / replacement expedition loop in the UI
- make archive, reports and faction notes useful for planning the next expedition
- connect more discoveries to the evaluation queue instead of relying on seeded tutorial items
- balance Knowledge Point income, upgrade costs, recovery time and loadout pressure
- keep Base Camp decisions readable and fast so they support exploration instead of replacing it

### 16A.1B Field Finds And Analysis Items

The "Wissen auswerten" tab should start empty at the beginning of a new game. Analysis items appear
only after the expedition finds something in the world and returns it, reports it or preserves enough
evidence for the base to study it.

This is the Base Camp presentation of the Findings system defined in the Knowledge Economy section.
The field discovery is exciting because it is concrete; the base analysis is exciting because it
explains what the discovery means.

Examples of analysis-worthy finds:

- Fremde Saatkörner
- Unbekanntes Metall
- Unbekannte Waffe
- Bruchstück einer Karte
- Geschnitzte Pfähle or other border signs
- Masked symbols, sealed objects or ritual fragments
- Unusual medicine, spores, animal remains or contaminated water samples
- Old camp records, coded letters or broken tools

Design rules:

- A find should have a stable source id so it cannot be farmed repeatedly.
- A find should record where it came from and who reported or carried it back.
- Some finds require successful return to base; if the expedition is lost, these items are lost too
  unless a later expedition recovers them.
- Analysis should take base time and may be limited by evaluator capacity.
- Analysis results create archived insights and may award Knowledge Points, reveal safer routes,
  unlock faction dialogue, unlock offers or clarify special-location risks.
- The player should understand why an item is analyzable; it should not appear as abstract research.
- The UI should show the field description before analysis and the explanation after analysis.

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

For the MVP, use four expedition resources plus one separate base progression and trade resource:

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
- faction trade
- bought information
- recovery missions

The MVP should keep Knowledge Points as a single currency.

Do not add separate knowledge categories until the single-resource model has been playtested.

The player must never become hard-locked by reaching zero Knowledge Points.

At zero Knowledge Points, the base should still allow a weak emergency expedition with minimal supplies, no specialist advantage and low carrying capacity.

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
