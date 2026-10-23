# V1: Schluchtlinie, Weltgelände und Kamera – Stand und Prüfanleitung

Status: **Entwurf zur Owner-Prüfung (Tor G1); in Unity noch nicht ausgeführt**

Zugehörige Dokumente:

- `low_poly_map_presentation_concept.md`
- `low_poly_map_presentation_implementation_plan.md` (Stufen V1a und V1b)
- `low_poly_map_presentation_v0_review.md`

## 1. Was V1 enthält

### V1a – Daten und Logik (getestet)

| Änderung | Datei |
| --- | --- |
| Neue Pfadart `WorldPathKind.Chasm` | `src/Game.Core/WorldPathKind.cs` |
| Regeln: Kante wird durch Fluss- oder Schluchtlinie gestützt, Querlinie, Validierung | `src/Game.App/WorldObstaclePathRules.cs` |
| Generator: Wegehindernis ohne Linie erhält eine `Chasm`-Linie quer über die Kante | `src/Game.App/WorldGenBridge.cs` |
| Tutorialwelt: Schlucht als `chasm-gray` statt `river-gray` | `src/Game.App/TutorialGameFactory.cs` |
| Kodierungsfehler behoben („Zerstörte Brücke“, Testtext, Expeditions-Anzeige „Â·“) | `WorldGenBridge.cs`, `Program.cs`, `ExpeditionScreenController.cs` |
| Alte Unity-Ansicht zeichnet `Chasm` vorläufig wie einen Fluss | `UnityHexMapView.cs` |
| Tests | `tests/Game.Tests/WorldObstaclePathTests.cs`, Anpassung in `Program.cs` |

Das Save-Format ändert sich nicht: Pfade werden nicht im Laufzeit-Snapshot gespeichert.

### V1b – Weltansicht (nur kompiliert und rechnerisch geprüft)

Eine eigenständige Ansicht `MapStyleWorldView`, die einen echten `GameState` zeigt. Sie liest nur und
ändert keinen Spielzustand.

| Datei | Zweck |
| --- | --- |
| `MapStyleWorldTerrain.cs` | Relief aus `TerrainType` je Hexfeld, Schluchten und Flüsse als Rinnen entlang der Pfade, Straßen als Farbe, Wissen flacht Unbekanntes ab und blasst es aus |
| `MapStylePolylineIndex.cs` | Abstand zu Linien (Schlucht, Fluss, Straße) |
| `MapStyleWorldView.cs` | Tutorial oder generierte Welt, Brücken auf der Linie, Marker, Expedition, Wälder, Bedienfeld |
| `MapStyleOrbitCamera.cs` | Drehen, Zoom, Schwenken; bewegt sich nie von selbst, Drehung wird nicht gespeichert |
| `MapStyleLighting.cs` | Tageszeitstimmung (aus dem Prototyp herausgelöst) |

Regeln der Ansicht:

- Brücken stehen an der Stelle der Schluchten- oder Flusslinie, die der blockierenden Kante am
  nächsten liegt, und überspannen die Linie quer.
- Der Brückenzustand ist der dem Spieler **bekannte** Zustand der Ortsbedingung. Ohne Beobachtung zeigt
  die Brücke nur Schlucht und Pfosten (neuer Zustand „unbekannt“).
- Orte auf unbekannten Feldern werden nicht gezeichnet.
- Basislager, Siedlungen und Wachtürme nutzen die Prototyp-Modelle; andere Orte erhalten einen
  neutralen Marker in der Archetypfarbe. Die Zuordnung nach Variante folgt mit V2.
- Gebietsgrenzen werden nicht gezeichnet.

## 2. Prüfanleitung

1. Projekt in Unity öffnen und kompilieren lassen. Neue Dateien in den Paketen (`src/Game.App`) erhalten
   dabei `.meta`-Dateien.
2. Menü **Map Style → Create World Scene (real world data)** ausführen, Szene öffnen, Play drücken.
3. Bedienung: Quelle Tutorial oder Generiert (Seed eingeben), Ansicht Spielerwissen oder Weltwahrheit,
   Brücken in allen Zuständen anzeigen („nur Ansicht“), Tageszeit, Kamera (Ziehen oder Q/E drehen,
   WASD schwenkt, Rechtsklick-Ziehen schwenkt, Mausrad zoomt).
4. Worauf achten: Läuft die Schlucht oder der Fluss sichtbar durch? Steht die Brücke in der Schlucht?
   Kann man die Brücke nicht seitlich umgehen? Passt die Brücke zum bekannten Zustand? Sind
   unbekannte Gebiete flach und blass?

## 3. Was geprüft wurde und was nicht

Geprüft:

- Alle `Game.Tests` laufen durch, einschließlich der neuen Tests. Der Variantentest schlägt fehl, wenn
  der Kodierungsfehler wieder eingebaut wird.
- Generierte Welten (acht Seeds): jede blockierende Kante hat eine Fluss- oder Schluchtlinie, keine
  Schluchtlinie ist unbenutzt.
- Die MapStyle-Skripte kompilieren gegen die Unity-6000.5.2f1-Assemblies, auch die neuen
  Weltansicht-Bausteine mit `Game.Core` und `Game.App`.
- Rechenprobe des Geländes außerhalb von Unity (Tutorialwelt mit 1200 Feldern, rund 176 mal 114
  Einheiten): Hexumrechnung ohne Fehler; die Schlucht ist am Querschnitt rund 3 Einheiten breit und
  2 Einheiten tief; rund 33.000 Höhenproben dauern etwa 100 Millisekunden; Brückenposition auf der
  Linie berechenbar. Bei einer generierten Welt (Seed 42) liegt das Hindernis auf einem Fluss.

Nicht geprüft (Unity-Batch ohne Lizenz):

- Aussehen, Leistung und Bedienung im Editor, das Verhalten der Schatten bei großen Karten
  (die Kamera sitzt bewusst nah, weil die URP-Schattendistanz 80 beträgt; die Zoomgrenze liegt bei 36).
- Die Wälder, Marker und die Beschriftungen auf großen Karten.
- Die Szene `MapStyleWorld.unity` entsteht erst über das Menü.

## 3a. Korrektur nach der ersten Sichtprüfung

Die erste Prüfung in Unity zeigte die Karte verzerrt (als Parallelogramm). Ursache: Die Tutorialwelt
speichert Versatzkoordinaten (Spalte, Zeile), generierte Welten axiale Koordinaten, und die bestehende
Kartenansicht rechnet beides in ein gemeinsames Gitter um. Die neue Ansicht behandelte alles als axial.
Seitdem übernimmt `MapStyleHexLayout` diese Umrechnung. Rechenprobe: Die Tutorialkarte füllt ihr
Begrenzungsrechteck zu 100 Prozent (176 mal 114 Einheiten); mit der alten, falschen Umrechnung waren es
237 mal 114 Einheiten mit leeren Ecken. Küsten sind außerdem etwas flacher, damit einzelne Seefelder
weniger wie Gruben wirken.

## 4. Fragen für die Freigabe (G1)

1. Ist die Schlucht als sichtbarer Graben lesbar und die Brücke dort glaubwürdig platziert?
2. Wirkt das Relief für Hügel und Berge gut, oder soll es kräftiger oder flacher sein?
3. Ist der Zustand „unbekannt“ der Brücke (nur Schlucht und Pfosten) verständlich?
4. Reichen Zoomgrenze und Schwenken für die Tutorialkarte (rund 176 mal 114 Einheiten)?
5. Soll die Karte bei neuem Wissen sofort neu aufgebaut werden (V2) oder inkrementell pro Chunk?

## 5. Bekannte Einschränkungen

- Die Ansicht baut das Gelände einmal auf; bei neuem Wissen oder geändertem Zustand wird sie nicht
  aktualisiert (V2).
- Ein einziges Mesh für die ganze Karte; Chunks und Detailstufen folgen, falls die Leistung es verlangt.
- Der Weltgenerator liefert Höhenwerte, die `Game.Core` nicht kennt; das Relief entsteht deshalb aus
  dem Geländetyp.
- Die Datenzuordnung der Generatorvarianten auf Katalog-IDs bleibt in V3.
