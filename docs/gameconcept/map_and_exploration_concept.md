# Map and Exploration Concept

Source of truth for map presentation, scouts, Fog of War, map knowledge and repeated travel.

This document must be read before changing map rendering, scouting, route handling or knowledge overlays.

---

## 4. Visual Style and Map Presentation

The world uses a hidden logical hex grid for movement, terrain data, discovery state and simulation.

The hex grid is not the default art style.

Normal play presents the world as a flat isometric 2D/2.5D landscape placed over the logical grid.

Core visual direction:

- no permanent visible hex outlines during normal play
- no sculpted terrain elevation on individual hex cells
- no full 3D terrain construction for the MVP
- optional hex overlays for debugging, selection and movement planning
- terrain base colors and textures form a continuous readable landscape
- forests use clustered trees that visually cross the boundaries of individual cells
- mountains use per-cell or multi-cell decorative objects without requiring seamless puzzle meshes
- roads and rivers use connected visual segments and remain traceable
- settlements, ruins, graves, warning posts and other locations use placed objects, sprites or billboards
- map objects communicate terrain and meaning without exposing hidden world truth

The player should perceive a continuous world whose underlying logic happens to use hexes.

Core rule:

> Hexes are the data structure, not the art style.

### 4.1 Map Layers

The map presentation should use separate conceptual layers:

1. logical terrain and movement data
2. terrain base presentation
3. decorative terrain objects
4. roads and rivers
5. locations, faction signs and landmarks
6. Fog of War and knowledge-state presentation
7. expedition, scout and player markers
8. temporary selection, path and debug overlays

This separation allows the visual presentation to change without changing the simulation.

### 4.2 Forests

Forests should be represented by clusters of varied trees rather than one tree icon per hex.

A forest cell may use:

- dense clusters when surrounded by forest
- medium clusters near internal variation
- sparse clusters near a forest edge
- scattered trees on adjacent non-forest cells

The tree placement may visually overlap cell boundaries.

The simulation remains cell-based.

### 4.3 Mountains

Mountains should not require a complete puzzle-piece system for the MVP.

Mountain cells may place:

- large peaks
- smaller ridges
- rocky clusters
- foothills
- scattered rocks on neighboring cells

Variation, overlap and hidden grid lines should make neighboring mountain cells read as a range.

Optional authored multi-cell decorative mountain groups may later be added for major landmarks. They do not replace the logical terrain cells beneath them.

### 4.4 Map Design Rule

The map should communicate through observation rather than constant explicit overlays.

The player should feel like an explorer interpreting a landscape, not like a commander reading a fully labeled strategy board.

Temporary overlays are allowed for:

- current selection
- reachable area
- planned movement
- specific map tools
- developer debugging

### 4.5 Fog of War and Knowledge Presentation

At the start, almost the entire world is covered by Fog of War.

The player gradually reveals information by:

- moving the main expedition
- sending scouts
- finding maps or reports
- talking to factions
- discovering landmarks
- recovering old expedition records
- investigating special places

Revealed information is not always permanently reliable.

The visual presentation must distinguish what is:

- unknown
- reported
- confirmed
- old
- doubtful
- lost or no longer reliable

The map must never reveal objective WorldState information that the expedition or archive does not know.

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

Scout orders use eight approximate compass sectors:

- north
- northeast
- east
- southeast
- south
- southwest
- west
- northwest

These directions are human navigation instructions, not the six adjacency directions of the logical hex grid.

A scout sent northeast for three days does not move to one neighboring hex and stop. The mission follows an approximate northeast bearing through multiple cells and may deviate because of:

- terrain
- roads and rivers
- hazards
- faction signs
- mission focus
- cautious or risky behavior
- discovered obstacles
- the scout's traits and decisions

The simulation may internally select a path or directional corridor through the hex grid.

The player gives a broad field instruction, not a sequence of hex coordinates.
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

## 15B. Automated Travel on Known Routes

Repeated travel through already understood territory should not require the player to click every known hex again.

The game should support automated travel over eligible known routes.

Automated travel is not teleportation.

It still:

- consumes expedition days
- consumes Supplies
- applies Medicine, Morale and Capacity rules
- advances the world phase
- allows faction and route events
- may be interrupted
- may reveal that old information is no longer reliable

A route segment is eligible only when it is sufficiently known and currently considered travel-safe.

Possible requirements:

- confirmed map knowledge
- known connected path
- no unresolved blocking hazard
- required faction permission where applicable
- acceptable route confidence
- recent enough verification

Automated travel stops when:

- the route becomes blocked
- a faction intercepts the expedition
- a serious event occurs
- the expedition reaches unknown territory
- the player-defined destination is reached
- Supplies, injury or Morale require intervention
- route knowledge proves outdated

### Failure and Route Revalidation

A route whose latest known use ended in expedition failure, disappearance or an uncertain outcome cannot automatically be assumed safe.

Affected route sections are downgraded to Old or Doubtful.

A replacement expedition must revalidate them through:

- direct travel
- scout report
- reliable faction information
- recovered records
- a recent successful journey

Core rule:

> Successful knowledge enables convenience. Failure restores uncertainty where the outcome made the route questionable.

The first MVP implementation may limit automated travel to one clearly defined known route between the base and an explored frontier point.

---
