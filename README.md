# Endless Style Hex Map View

Kleine Godot-4.7-Demo fuer eine rein visuelle HexMap-Ansicht im Stil einer 4X-Fantasy-Strategiekarte.

## Inhalt

- prozedural generierte Hexkarte mit axialen Koordinaten
- erhoeht gerenderte Hex-Prismen mit dunklen Kanten
- Biome fuer Wasser, Kueste, Grasland, Wald, Huegel, Berge und Schnee
- prozedurale Terrain-Texturen fuer einen gemalten 4X-Kartenlook
- schrittweise Hoehenabstufungen mit dezenten Hoehenkonturen
- organische Fluss- und Strassen-Ribbons ueber mehrere Hexfelder
- kleine Siedlungen als Strassenknotenpunkte
- einfache prozedurale Details wie Baeume, Felsen, Berge und Kuestenmarker
- orthografische Strategiekamera mit langsamem Orbit
- Licht, Nebel, Wasserflaeche und Filmtone-Mapping fuer eine malerische Ansicht

## Start

Das Projekt in Godot 4.7 oeffnen und die Szene `res://scenes/HexMapView.tscn` starten.

Ein Preview-Bild kann per Startargument `--capture-preview` als `res://preview_hex_map.png` geschrieben werden.

Es werden keine externen Assets benoetigt. Alles ist prozedural aus Godot-Meshes und Materialien aufgebaut, damit die Ansicht direkt laeuft und spaeter leicht gegen freie Art-Assets ausgetauscht werden kann.

## Unity-Vergleich

Eine Unity-Variante liegt im Ordner `UnityHexMapView`. Dort die Szene `Assets/Scenes/UnityHexMapView.unity` oeffnen.
