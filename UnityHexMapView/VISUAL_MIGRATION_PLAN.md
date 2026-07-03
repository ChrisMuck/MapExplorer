# Visual Migration Plan

## Current Rendering Analysis

The prototype currently keeps all map logic in `UnityHexMapView.cs`. Axial hex coordinates, terrain choice, elevation, rivers, roads, settlements and decorative features are generated from the same data pass. This makes the map reliable and easy to rebuild, but it also makes the world read as separate board-game prisms: every terrain surface is a hard hex, rivers and roads are ribbons placed over tiles, and forests/mountains are repeated markers.

The safest migration path is to keep the coordinate/data layer intact and add a separate visual layer that can be turned off for debugging. The data remains hex based; the art layer makes neighboring hexes feel like one continuous landscape.

## Migration Steps

1. Keep the existing axial coordinates, terrain enum, elevation values and generated path data as the source of truth.
2. Add inspector toggles for painterly visuals, debug grid visibility and selection preview.
3. Make hex borders subtle by default and stronger only in debug mode.
4. Replace flat color reads with procedural painterly materials and irregular surface brush meshes.
5. Keep river and road data as hex paths, but render them as smoothed ribbons with banks, beds and highlight layers.
6. Replace icon-like forests with varied low-poly tree clusters and darker forest floor masses.
7. Replace single mountain markers with small procedural formations and rocky base masses.
8. Add landmarks as procedural placeholder assets first, then swap in licensed assets later if desired.
9. Improve scene lighting with warm directional light, fill light, fog and muted background color.
10. Later, split the generator into data, terrain mesh, path mesh and landmark systems once the target visual language is proven.

## First Pass Implemented

- `usePainterlyVisualLayer` toggles the organic visual layer.
- `showDebugHexGrid` restores strong readable hex borders for debugging.
- `showSelectionPreview` keeps hover/selected outline placeholders visible.
- Terrain receives procedural noise materials and irregular biome brush overlays.
- Rivers and roads are rendered as smoothed spline-like ribbons.
- Forests now use clusters with varied tree scale, color and placement.
- Mountains now use small formations instead of one centered marker.
- Lighting uses warm sun, cool fill, soft shadows and fog.

## Next High-Value Passes

- Generate one continuous terrain mesh per biome region to remove the visible prism silhouette.
- Add neighbor-aware blending masks at terrain boundaries.
- Route rivers along shared hex edges or valleys instead of tile centers.
- Add bridges where roads cross rivers.
- Add landmark categories: ruins, watchtowers, resource hints, camps and dangerous regions.
- Add a runtime hover system that drives the existing outline preview from mouse position.
