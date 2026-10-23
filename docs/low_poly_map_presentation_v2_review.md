# V2a: Resolver, Visual-Daten und Brücke aus Daten – Stand und Prüfanleitung

Status: **Entwurf zur Owner-Prüfung; Unity-Teil in Unity noch nicht ausgeführt**

Zugehörige Dokumente:

- `low_poly_map_presentation_concept.md` (Abschnitte 7 und 12)
- `low_poly_map_presentation_implementation_plan.md` (Stufe V2)
- `low_poly_map_presentation_v1_review.md`

## 1. Was V2a enthält

Die Brücke wird vollständig aus Daten gezeichnet, und zwar über einen testbaren Resolver. Die neue
Ansicht aktualisiert sich inkrementell.

### Daten und Logik (getestet)

| Änderung | Datei |
| --- | --- |
| Datendokument: Marker, 13 Teile, Komposition der Brücke (42 Elemente) | `UnityHexMapView/Assets/StreamingAssets/GameData/Visuals/visual-definitions.json` |
| Typen, Lader und Validierung des Dokuments | `src/Game.App/VisualDefinitions.cs` |
| Resolver: `Resolve`, `ResolveForPlayer`, `ResolveWorldTruth` | `src/Game.App/LocationVisualResolver.cs` |
| Katalog lädt das Dokument (optional) und validiert es | `src/Game.App/GameDataCatalog.cs` |
| Eintrag im Manifest | `game-data-manifest.json` |
| Tests | `tests/Game.Tests/VisualDefinitionTests.cs` (Dokumentzahl in `GameDataCatalogTests.cs` von 37 auf 38) |

Regeln des Resolvers:

- Er ist eine reine Funktion aus Ortsdefinition und Wissen. Clients entscheiden nichts.
- `ResolveForPlayer` liefert nichts für unbekanntes Gelände und nutzt den **zuletzt beobachteten**
  Zustand, nie den aktuellen der Welt. Ohne Beobachtung gilt der Zustand `unknown`; die Brücke zeigt
  dann nur Schlucht und Pfosten.
- Die Auswahl ist spezifisch zuerst: Komposition der Variante, dann des Archetyps, sonst nur der
  Marker.
- Die Komposition der Brücke deckt alle Betriebszustände des Zustandsprofils ab; die Validierung
  verlangt das.

### Unity (nur kompiliert und rechnerisch geprüft)

| Datei | Zweck |
| --- | --- |
| `MapStyleVisualKit.cs` | Teile aus den Grundformen des Dokuments als Meshes (zwischengespeichert) |
| `MapStyleLocationView.cs` | Zeichnet eine Beschreibung; aktualisiert nur geänderte Elemente; Ring und Wissensdarstellung |
| `MapStyleWorldTerrain.cs` | Geländechunks und `UpdateKnowledge` (liefert nur betroffene Chunks) |
| `MapStyleWorldView.cs` | Aktualisiert alle 0,25 Sekunden; Brücke aus Daten; Entwicklerknöpfe |

## 2. Prüfanleitung

1. Projekt in Unity öffnen und kompilieren lassen (neue Dateien erhalten `.meta`-Dateien).
2. Szene `MapStyleWorld.unity` öffnen und Play drücken (Menü **Map Style → Create World Scene** legt sie an).
3. Im Bedienfeld unter „Entwickleraktionen“ (sie ändern den Spielzustand der Testwelt direkt):
   - **Erkunden +1 um die Expedition:** Das Gelände wird bestätigt; nur einzelne Chunks werden neu
     aufgebaut, die Statuszeile zeigt „zuletzt n von 54 Chunks neu (x ms)“.
   - **Brückenzustand beobachten:** Die Brücke zeigt ihren beobachteten Zustand.
   - **Welt: Brückenzustand wechseln:** Die Welt ändert den Zustand, das Bild **nicht**, bis erneut
     beobachtet wird. Danach wechselt die Brücke weich: Planken wachsen ein oder aus, Gerüst erscheint.
4. „Weltwahrheit“ zeigt die Welt unabhängig vom Wissen.

## 3. Was geprüft wurde und was nicht

Geprüft:

- Alle `Game.Tests` laufen durch, einschließlich der neuen Tests: Dokument lädt, Zahl der sichtbaren
  Elemente je Zustand (offen 20, beschädigt 19, repariert 23, zerstört 7, unbekannt 4), Posen,
  Wissensverhalten, Entwicklersicht, Rückfälle und Validierungsfehler.
- Die MapStyle-Skripte kompilieren gegen die Unity-Assemblies.
- Rechenprobe des Chunkaufbaus außerhalb von Unity: 54 Chunks bei der Tutorialkarte; eine geänderte
  Kachel betrifft einen Chunk, ein Erkundungsradius von 3 betrifft 5 Chunks; ohne Änderung wird nichts neu
  gebaut; Waldkacheln verteilen sich vollständig auf die Chunks.

Nicht geprüft:

- Aussehen und Übergänge der datengetriebenen Brücke in Unity, die tatsächliche Aufbauzeit eines
  Chunks, das Verhalten im Spiel mit Befehlen (V2b).
- Die übrigen Orte (Lager, Dorf, Turm, Grab) laufen weiter über die Prototyp-Modelle im Code.

## 4. Fragen für die Prüfung

1. Sieht die Brücke aus Daten genauso aus wie die frühere aus Code, und wirken die Übergänge gut?
2. Ist die Aktualisierung bei „Erkunden +1“ ohne Ruckler?
3. Ist „nur Schlucht und Pfosten“ für eine unbeobachtete Brücke verständlich?

## 5. Bekannte Einschränkungen

- Die Brücke sitzt auf der Höhe des echten Geländes; auf nur berichtetem Gelände (abgeflacht) kann sie
  leicht darüber schweben.
- **Platzierung (nach der ersten Sichtprüfung verbessert):** Die Brücke stand an der Stelle der Linie, die
  der blockierenden Kante am nächsten liegt; bei der Tutorialwelt war das eine breite Wasserfläche, in der
  die Pfosten im Wasser standen. Jetzt prüft die Ansicht Stellen entlang der Linie nahe der Kante, misst
  die Breite zwischen den Ufern, wählt die schmalste, zentriert die Brücke zwischen den Ufern und streckt
  sie auf die Breite (Faktor 0,8 bis 1,5). Rechenprobe außerhalb von Unity: Tutorial Breite 3,0 statt
  keiner auffindbaren Ufer an der alten Stelle; generierte Welten 2,0 bis 3,5.
- **Brückenhöhe (zweite Korrektur):** Die Ufer wurden zuerst am Wasserrand gemessen; dort lag das Deck rund
  einen Meter unter der Kante (Tutorial: Höhe -0,78 statt 0,27). Jetzt gilt die Oberkante: Das Deck liegt
  auf dem Gelände, das nicht mehr von der Schlucht oder vom Flussbett abgesenkt ist (Tutorial: Breite 4,5,
  Streckung 1,15, Höhe 0,27).
- Bäume meiden die Stellen gezeichneter Orte; dadurch verraten Lücken in bekanntem Wald keine
  unbekannten Orte, solange deren Feld unbekannt ist.
- Die Ansicht liegt noch nicht im Spielablauf: Feldwahl, Befehle, Kamera und Overlays der alten Ansicht
  fehlen (V2b).
