# Unity Hex Map View

Unity-Variante der prozeduralen 4X-HexMap-Ansicht zum Vergleich mit der Godot-Version.

## Start

1. Unity Hub oeffnen.
2. Projektordner `C:\Programmierung\MapTest\UnityHexMapView` hinzufuegen.
3. Szene `Assets/Scenes/UnityHexMapView.unity` oeffnen.
4. Die Karte wird durch die Komponente `UnityHexMapView` am GameObject `Unity Hex Map View` automatisch erzeugt.

## Inhalt

- getrennte Gameplay-Daten und visuelle World-Layer: Hexdaten bleiben intern, Default-Ansicht rendert eine kontinuierliche Terrainflaeche
- Debug-/Fallback-Modus fuer rohe Hex-Prismen mit Terrainfarben und Koordinaten
- prozedurale Hex-Prismen mit axialen Koordinaten
- quantisierte Hoehenstufen und dezente Hoehenkonturen
- Biome fuer Wasser, Kueste, Grasland, trockene Ebenen, Wald, Sumpf, Wueste, Huegel, Berge und Schnee
- prozedurale Noise-Texturen fuer einen gemalten Kartenlook
- kontinuierliche Terrain-Meshes pro Biome mit geglaetteten Eckhoehen statt sichtbarer Einzeltile-Prismen
- reduzierte Cliff-Faces nur bei groesseren, spielerisch relevanten Hoehenunterschieden
- unregelmaessige Terrain-Pinselstriche auf einzelnen Hexfeldern
- leicht organische Top-Meshes mit kleiner Bodenunebenheit
- weiche Terrain-Uebergangspatches zwischen passenden Nachbarbiomen
- kleine prozedurale Bodendetails wie Shrubs, Pebbles, Gras- und Schmutzvarianten
- erdigere, entsaettigte Farbpalette im Stil einer gemalten 4X-Karte
- geglaettete Mesh-Ribbons fuer Fluesse, Strassen und Territoriumsgrenzen
- mehrlagige Fluesse mit Terrain-Cut, Uferkanten, variabler Breite und Highlights
- mehrlagige Strassen mit weichen Raendern, Intersections und automatisch erkannten Bruecken
- kleine Siedlungen, organische Waldregionen, Felsen, verbundene Bergketten und Marker
- Forest-Hexes mit variierenden Baumhoehen, Randbaeumen, dunklem Waldboden und visuellen Verbindungen zu Nachbarwaeldern
- Mountain-Hexes mit Peaks, Ridge-Spines, Plateaus, Cliff-Faces und offenen Passseiten
- Diorama-POIs fuer Villages, Ruins, Camps, Watchtowers und Landmarks
- POIs mit prozeduralen Platzhaltern, Click-Collidern, Road-Anbindungen und Labels nur bei Hover/Selection
- Inspector-Toggles fuer continuous terrain layer, painterly view, Terrain-Rohdatenmodus, Hexkoordinaten, Debug-Hexraster, Road/River-Hexpfade und Selection Preview
- orthografische Strategie-Kamera plus Licht-Setup

Die Szene nutzt keine externen Assets. Alles wird zur Laufzeit bzw. im Editor aus Unity-Primitives, Meshes, Materialien und prozeduralen Texturen erzeugt.

## Shared simulation contract

Gameplay commands are routed through `SimulationSession`, created from the same complete
`GameDataCatalog` as the headless runner and Core/App tests. Unity owns only map presentation,
input and panels. Scout reports show qualitative leads (scope, direction, confidence and wording),
never a report-derived map coordinate or automatic target marker.

Siehe auch `VISUAL_MIGRATION_PLAN.md` und `ASSET_MANIFEST.md`.
