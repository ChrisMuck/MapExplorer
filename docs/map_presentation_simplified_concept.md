# MAP_PRESENTATION_SIMPLIFIED_CONCEPT.md

Revised map presentation concept for the Expedition Exploration Game.

Status: **Updated Direction / Supersedes 4X-style map ambitions for MVP**

---

## 1. Core Decision

The MVP should not try to build a full 4X-style terrain map.

That is too expensive for the current stage.

The game will still use a hex grid internally for:

- movement
- terrain data
- fog of war
- knowledge state
- scout reports
- faction influence
- special location placement
- pathfinding
- map notes

But the visible map should be simpler:

> A hidden logical hex grid with a flat isometric 2D presentation layer on top.

The player should not see strong hex outlines during normal play.

Hex borders may exist in debug mode or optional overlay mode, but they should not define the visual identity of the game.

---

## 2. What This Means Visually

The visible map is not a sculpted 4X board.

Instead, the map is:

- flat
- readable
- stylized
- isometric 2D / 2.5D
- built from simple terrain bases and placed objects
- without visible height differences on the hex cells themselves
- without visible grid lines between all hexes

Core rule:

> Hexes are the data structure, not the art style.

---

## 3. No Visible Hex Lines

Normal gameplay should not show black or strong lines between hexes.

Allowed:

- subtle hover highlight
- selected hex outline
- reachable-area highlight
- optional debug grid
- temporary overlay when planning movement

Not allowed as default visual style:

- permanent board-game hex outlines
- strong black grid
- every tile visually isolated from every other tile

The map should feel continuous, even though it is hex-based.

---

## 4. Flat Hexes, No Terrain Height

Hexes should not visually rise or fall.

No MVP terrain height system.

No 3D sculpted terrain.

No height-based blending.

Mountains, hills, walls and ravines are represented as placed map objects or edge/region visuals, not as raised hex geometry.

This keeps the map implementation much simpler.

---

## 5. Isometric 2D Presentation Layer

The map should use an isometric 2D view.

This can be implemented as:

- 2D sprites placed in isometric projection
- simple low-poly-looking 2D objects
- billboarded sprites
- flat terrain cells with object layers
- later possibly lightweight 3D, but not required for MVP

The player should see:

- grass/plains base
- forest clusters
- mountain objects
- hills/rocks
- rivers as flat flowing shapes or segmented sprites
- roads as flat path shapes or segmented sprites
- special location icons/objects
- faction signs
- player annotations

The visual trick is:

> The hex grid controls placement. The art layer hides the grid.

---

## 6. Terrain Representation

### Plains / Grassland

Simple base terrain.

Visuals:

- muted green/brown ground
- subtle texture
- mostly open
- good readability

### Forest

Forest is represented by clusters of tree objects.

Not every forest hex needs a single tree icon.

Use groups of trees to suggest a forest region.

Visuals:

- clusters of low-poly or stylized tree sprites
- denser clusters in forest regions
- softer edges where forest meets plains
- no need for perfect blending in MVP

### Dense Forest / Swamp

Use darker and denser object clusters.

Possible visuals:

- darker tree clusters
- swamp patches
- reeds
- dead trees
- mist overlay later
- darker ground

### Hills

Hills can be represented by:

- small rock objects
- low mound sprites
- brown/green patches
- slope-like decorative shapes

No actual elevation needed.

### Mountains

Mountains are placed obstacle objects.

They may occupy one or several hexes visually.

They should clearly communicate:

- difficult movement
- possible barrier
- landmark
- route planning relevance

Visuals:

- low-poly mountain sprites/objects
- rocky clusters
- ridge-like object chains

Mountains do not require raised terrain.

### Ravines / Cliffs / Walls

Ravines, cliffs and walls are special obstacle visuals.

They may be represented as:

- long edge objects
- dark cracks
- broken bridge object
- wall segment object
- cliff-line sprite

They do not require terrain height.

### Rivers

Rivers should still look continuous.

But implementation can stay simple:

- flat blue path segments
- connected sprite pieces
- simple curve/line renderer

Rivers should not be dots.

The player should be able to follow a river visually.

### Roads / Paths

Roads should also be continuous.

But they can be simple:

- brown path segments
- dashed trail sprites
- connected line renderer
- old road as broken/faded segments

Roads should not be dots.

The player should be able to trace a route visually.

---

## 7. Object-Based Map Layer

The map presentation should support placed objects.

Examples:

```text
TreeClusterObject
MountainObject
RockClusterObject
RoadSegmentObject
RiverSegmentObject
GraveObject
CampObject
WarningPostObject
GateObject
BridgeObject
FactionMarkerObject
PlayerNoteMarkerObject
```

Objects are visual representations.

They may be generated from underlying hex data.

They should not contain gameplay rules.

A mountain object does not decide movement cost.

The underlying hex data does.

---

## 8. Data vs Visuals

The internal data model still knows:

```text
Hex 4/1 = forest
Hex 5/1 = mountain
Road connection from Hex 3/1 to Hex 4/1
River connection from Hex 2/2 to Hex 3/2
Special location at Hex 6/3
```

The presentation layer decides:

```text
place tree clusters around forest hexes
place mountain sprite on mountain hex
draw road segment between hex centers
draw river segment between hex centers
place grave object at special location
```

This keeps gameplay and presentation separate.

---

## 9. Knowledge and Fog

Fog of war still applies.

Unknown areas should hide or obscure objects.

Knowledge states:

- Unknown: hidden/darkened area
- Reported: rough/sketched/ghosted indication
- Confirmed: visible object/terrain
- Old: faded
- Doubtful: uncertain overlay
- Lost: muted/greyed

Important:

> Do not show real terrain objects if the player does not know them.

Example:

If a hidden mountain pass exists but has not been discovered, do not show the mountain pass object.

If a scout reports “possible road,” show a dashed/faint road approximation, not a confirmed clean road.

---

## 10. Debug Grid

A visible hex grid is useful during development.

It should exist as a debug toggle.

Debug grid may show:

- hex boundaries
- coordinates
- biome IDs
- movement costs
- road/river connections
- faction influence
- knowledge state

But normal gameplay should hide it.

---

## 11. MVP Map Presentation Scope

MVP map presentation should include:

- flat isometric 2D map
- hidden logical hex grid
- no permanent visible hex lines
- simple terrain base colors
- object clusters for forests
- mountain/rock objects
- simple connected road/path segments
- simple connected river segments
- special location placeholder objects
- faction warning placeholder objects
- player markers/notes
- fog/knowledge overlay
- debug grid toggle

MVP map presentation should not include:

- 3D sculpted terrain
- real terrain height
- complex elevation
- cliff geometry
- full 4X production-map quality
- animated rivers
- complex biome blending
- procedural art perfection
- final terrain assets
- final icons

---

## 12. Implementation Blocks

The implementation should be split into clear blocks.

Do not build everything at once.

### Block 1: Logical Map Foundation

Goal:

> A correct hidden hex map exists.

Implement:

- hex coordinates
- map data
- terrain type per hex
- knowledge state per hex
- movement cost
- selected hex
- debug grid
- simple placeholder rendering

No final UI.

No final art.

### Block 2: Basic Map Presentation

Goal:

> The player sees a simple isometric map, not a debug grid.

Implement:

- isometric coordinate conversion
- no default hex outlines
- terrain base layer
- object placement layer
- forest clusters
- mountain/rock placeholders
- road/river segment placeholders
- expedition marker

### Block 3: UI Overlay

Goal:

> The UI sits over the map without hiding it.

Implement:

- top status bar
- left context panel
- right expedition/report/scout panel
- bottom action bar
- selected hex info
- end day button
- scout report display

### Block 4: Movement and Knowledge Loop

Goal:

> The player can move, reveal, end day and understand map knowledge.

Implement:

- movement command
- movement range feedback
- fog/knowledge overlay
- supplies consumption
- expedition day
- basic markers/notes

### Block 5: Scouts and Reports

Goal:

> Scouts create imperfect information.

Implement:

- send scout
- scout return/overdue
- report popup with portrait
- report hints
- create markers from report
- archive report

### Block 6: Special Locations and Events

Goal:

> The world contains meaningful decision points.

Implement:

- marked grave
- ravine/broken bridge
- abandoned camp
- event popup with image
- discovery card

### Block 7: Base Phase and Second Expedition

Goal:

> Returning matters.

Implement:

- return to base
- archive knowledge
- time passes
- world reaction
- next expedition setup
- engineer unlock/availability
- second expedition with inherited knowledge

---

## 13. Current Decision Summary

The MVP should not attempt a 4X map.

The map should be a hidden flat hex grid with an isometric 2D presentation layer.

Hex cells do not show height.

Hex outlines are not visible in normal play.

Forests, mountains, roads, rivers, warnings and locations are represented by simple objects, sprites or line segments placed on top of the logical grid.

The project should be implemented block by block:

1. basic logical map
2. basic isometric map presentation
3. UI overlay
4. movement and knowledge
5. scouts and reports
6. special locations and events
7. base phase and second expedition
