# Konzept: Generator-Orte aus JSON-Daten

Status: **Entwurf zur Owner-Freigabe; es wurde noch nichts am Generator geändert**

Zugehörige Dokumente:

- `low_poly_map_presentation_implementation_plan.md` (Stufe V3, Arbeitspaket 2, und Abschnitt 13a)
- `content_workbench_editor_concept.md`
- `technical_concept.md`

## 1. Ausgangslage (geprüft)

Der Orte-Rahmen ist seit PR 8 datengetrieben: Archetypen, Varianten, Modifikatoren, Aktionen, Zustands-, Szenario-, Inhaltsprofile
und Ergebnistabellen liegen unter `UnityHexMapView/Assets/StreamingAssets/GameData/Locations`. Der **Generator** wählt seine Orte
aber aus festen C#-Listen:

| Wo | Was steht im Code |
| --- | --- |
| `src/WorldGen/WorldGenerator.cs` (ab Zeile 913) | Die 30 Ortsnamen je Gruppe, Anzahl (`D(1.1)` usw.), Auswahlbewertung (`Remote`, Höhe, Küste, Wassernähe), Gelände- und Flächenregeln, Vorbelegung `ancient` oder `neutral` |
| `src/Game.App/WorldGenBridge.cs` | `ArchetypeIdFor`, `VariantIdFor`, `KindFor`, `ModifierIdFor`; unbekannte Namen fallen auf `worldgen-<name>` zurück |

Folgen:

- Von den 30 Generator-Orten haben nur Brücke, Grab, Lager und Tor eine Katalogvariante. Die übrigen 26 laufen ohne
  Zustandsprofil, Aktionen, Texte und Bild.
- Die Katalogvarianten `restless-marsh` und `wind-carved-peak` haben ein Szenarioprofil (mit `worldgen`-Block), werden vom
  Generator aber nie ausgewählt.
- Ein neuer Ort braucht heute Code- **und** Datenänderungen. Die Werkbank (`content_workbench_editor_concept.md`) kann den
  Generator daher nicht erweitern.

## 2. Ziel

1. Welche Orte der Generator kennt, wie oft und wo er sie platziert, steht in JSON.
2. Ein neuer Ort ist eine Datenänderung: Variante, Profile, Platzierungsregel, Bild. Kein C# nötig.
3. Der Generator bleibt **deterministisch**: Mit den mitgelieferten Daten entsteht für jeden Seed dieselbe Welt wie heute
   (Absicherung durch einen Vergleichstest, Abschnitt 6).
4. `WorldGen` bleibt ein eigenständiges Paket ohne Verweis auf `Game.App` (`WorldGen.asmdef` hat keine Referenzen).

## 3. Architektur

```
GameData/World/location-placement.json   (neu, Abschnitt 4)
        │  geladen und geprüft von Game.App (GameDataCatalog, optionales Dokument wie visual-definitions)
        ▼
Game.App: LocationPlacementCatalog ──► GenerationParams.Placement (einfache Datentypen aus WorldGen)
        ▼
WorldGen: PlaceSpecials() liest Placement statt fester Listen
        ▼
WorldGenBridge: Variant-Id kommt aus den Daten, nicht mehr aus Namensvergleichen
```

- `WorldGen` bekommt eigene schlichte Typen (`PlacementGroup`, `PlacementVariant`, ...). Sie enthalten keine JSON- oder
  Katalogabhängigkeit; `Game.App` übersetzt den Katalog in diese Typen.
- Ist `Placement` leer (`null`), benutzt der Generator eine eingebaute Standardtabelle, die das heutige Verhalten genau abbildet.
  So laufen bestehende Werkzeuge und Tests weiter, und die Standardtabelle dient als Vergleichswert für die JSON-Daten.
- Der Validator prüft: jede Variante existiert im Katalog, hat Szenario- und Zustandsprofil, Archetyp passt, Anker erlaubt,
  Gelände- und Biomnamen sind bekannt, Gewichte positiv, Formeln endlich.

## 4. Datenformat (Vorschlag)

Neues Dokument `World/location-placement.json` (`documentType: location-placement`, im Manifest, optional im Katalog).

```json
{
  "documentType": "location-placement",
  "schemaVersion": 1,
  "groups": [
    {
      "id": "placement-trace-sites",
      "archetypeId": "investigation-site",
      "generatorArchetype": "Spurenort",
      "count": { "perThousandLandCells": 2.86, "scaleWithDensity": true, "min": 1 },
      "anchor": "Point",
      "candidates": "road-cells",
      "score": [ { "term": "remote", "weight": -0.5 }, { "constant": 1 } ],
      "origin": "neutral",
      "variants": [
        { "variantId": "abandoned-camp",    "weight": 1 },
        { "variantId": "cold-fireplace",    "weight": 1 },
        { "variantId": "broken-cart",       "weight": 1 },
        { "variantId": "old-boat-camp",     "weight": 1 }
      ]
    }
  ]
}
```

Bausteine:

- **count**: Anzahl aus der Landfläche (heute `nLand / 350 × k × Dichte`), mit Minimum und Maximum und optionalen Stufen
  (`nLand >= 6000`), wie sie `nVolc` und `nDead` heute haben.
- **candidates**: benannte Kandidatenmengen, die der Generator liefert (`land`, `road-cells`, `swamp-cells`, `border-of-owner`,
  `river-or-steep-road-edges`). Sie bleiben im Code, weil sie Generatorwissen brauchen; die Daten wählen nur eine aus.
- **score**: Summe aus Termen des Generators (`remote`, `elevation`, `coastal`, `near-water`) mit Gewichten. Neue Terme sind
  eine kleine Codeerweiterung, neue Mischungen nicht.
- **variants**: Varianten mit Gewicht und optionalen Bedingungen (`requires: ["near-water"]`, `biomeAny`, `coastal`). Damit
  entfallen feste Wenn-Ketten wie „Fischgrund nur am Wasser". Die Variantenwahl kann dabei auch vom gewählten Feld abhängen
  (Ressourcenorte).
- **origin**: `ancient`, `neutral`, `faction-owned` mit den heutigen Wahrscheinlichkeiten (`claimChance` usw.), ebenfalls als Daten.
- **anchor**: `Point`, `Edge`, `Area` mit Flächengröße (`areaSize`) und erzwungenem Biom (`forcedBiome`) für Vulkanschlund und Tote Zone.

Der Anzeigename der Variante kommt aus `Scenes/Locales/de.json`, nicht mehr aus dem Generator-String.

## 5. Vorgehen in Stufen

Jede Stufe auf eigenem Branch und mit eigenem PR, jeweils vom Owner gemergt.

| Stufe | Inhalt | Prüfung |
| --- | --- | --- |
| G0 | Dieses Konzept; Owner-Freigabe | Review |
| G1 | `WorldGen`-Typen, eingebaute Standardtabelle, Generator liest `Placement`; **kein** neues Verhalten | Vergleichstest: bisherige Welten sind für viele Seeds Byte für Byte gleich |
| G2 | JSON-Format, Loader, Validator; Standardwerte als JSON (ersetzen die eingebaute Tabelle) | derselbe Vergleichstest mit JSON-Daten; Validatortests |
| G3 | 26 Katalogvarianten mit Zustands-, Szenario-, Inhaltsprofilen und Texten (einfaches Zustandsprofil „steht") | Katalogtests, Dokumentzahl im Manifest |
| G4 | Flächenanker für Gefahrenzonen im Datenformat der Bilder (`anchorMode: area`) und Ortsbilder je Gruppe (V3) | Unity-Prüfung durch den Owner |

Die Reihenfolge der Bilder in G4 folgt `low_poly_map_presentation_implementation_plan.md` (Spuren und Untersuchung, Landmarken,
Ressourcen, Verwahrung, Grenzzeichen, Gefahrenzonen, Siedlungen).

## 6. Risiken und Absicherung

- **Zufallsreihenfolge.** Der Generator zieht seine Zufallszahlen in fester Reihenfolge. Wer Schleifen oder Auswahl umbaut,
  verändert jede Welt. Deshalb zuerst die Standardtabelle (G1) mit Vergleichstest, erst danach Daten (G2). Gewichtete
  Auswahl aus Daten muss mit `_rng.Pick` über gleich lange Listen dieselbe Zahl verbrauchen.
- **Speicherstände.** Ein gespeichertes Spiel enthält die erzeugten Orte samt Variant-Ids. Die Ids der vier schon abgebildeten
  Varianten bleiben gleich; die 26 neuen Ids ersetzen `worldgen-<name>`. Alte Spielstände mit `worldgen-…`-Ids müssen
  weiterhin laden. Das ist eine Save-Format-Frage und braucht Owner-Entscheid (Frage 2 unten).
- **Nicht in JSON gehen:** Terrain, Biome, Flüsse und Fraktionen bleiben im Generator. Dieses Konzept betrifft nur die
  Sonderorte (`PlaceSpecials`) und deren Abbildung.
- **Offener Punkt 13a** (Brücken an sinnlosen Stellen) hängt an der Kandidatenmenge `river-or-steep-road-edges`. Sie wird in G1
  nicht verändert, ist danach aber als Daten einstellbar.

## 7. Entscheidungen des Owners

| Frage | Entscheidung | Folge |
| --- | --- | --- |
| 2. Alte Spielstände mit `worldgen-<name>` | **verwerfen** | keine Abbildungstabelle; Spielstände aus der Zeit vor G3 werden nicht mehr geladen. Die Version des Speicherformats wird erhöht, ein alter Stand meldet das klar statt abzustürzen. |
| 3. Sprachen | **mit Schlüsseln für weitere Sprachen** | Jede Variante bekommt einen Textschlüssel (`location.variant.<id>.name`); `de.json` liefert den deutschen Text, weitere Sprachdateien können folgen. Der Generator gibt keine Anzeigenamen mehr aus. |
| 4. Grenzzeichen | **mit Fraktionsfarbe, wenn einfach möglich** | Die Komposition bekommt ein Farbfeld, das der Besitzer des Ortes setzt (z. B. Farbe von Banner, Band oder Schädel). Der Resolver reicht die Fraktionsfarbe aus den Fraktionsdaten durch. Der Umfang wird in G4 geprüft; geht es nicht einfach, bleibt es bei einer Farbe je Variante. |
| 5. Zustände der 26 Varianten | **gleich sinnvolle Zustände** | Jede neue Variante bekommt ein eigenes Zustandsprofil (z. B. Leuchtturm: erloschen, brennend, eingestürzt; Schrein: verfallen, gepflegt). Die Liste je Ort entsteht in G3 und geht vorher zur Freigabe zum Owner, weil sie Autorendaten sind. |
| 1. Zuordnung Variante zu Gruppe | **fest zugeordnet** | Jede Variante steht in genau einer Gruppe. Aktionen, Texte und Zustände hängen am Archetyp; ein Ort in einer anderen Kategorie wird als eigene Variante doppelt angelegt. Der Validator meldet eine Variante, die in zwei Gruppen vorkommt, oder deren Archetyp nicht zur Gruppe passt. |
