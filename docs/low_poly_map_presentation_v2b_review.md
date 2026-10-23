# V2b (Schritte 1 bis 3): Low-Poly-Karte im echten Spielablauf – Stand und Prüfanleitung

Status: **Entwurf zur Owner-Prüfung; in Unity noch nicht ausgeführt**

Zugehörige Dokumente:

- `low_poly_map_presentation_implementation_plan.md` (Stufe V2, Abschnitt V2b)
- `low_poly_map_presentation_v2_review.md`

## 1. Was V2b jetzt enthält

Die bestehende Spielszene (`UnityHexMapView`) zeichnet ihre Karte standardmäßig mit dem Low-Poly-Relief. Spiel,
Befehle, UI und Overlays laufen weiter über den bisherigen Code; ersetzt wurden Gelände, Orte, Wasser und Feldwahl.

| Teil | Umsetzung |
| --- | --- |
| Zeichnen | Neue Klasse `MapStyleMapController` (ohne UI und Kamera). Sie enthält die Logik der Weltansicht: Gelände in Chunks, Wald, Mauern, Orte aus dem Resolver, Brücken, Marker. Die Entwicklungsansicht nutzt dieselbe Klasse. |
| Einbindung | `UnityHexMapView`: Schalter `useMapStyleRenderer` (Standard an). Ist er an und der Shader `MapStyle/LowPolyLit` vorhanden, entfallen der alte Chunk-Renderer, die alten Features, Küstenfähnchen und Wasserebenen. Sonst bleibt alles wie vorher. |
| Maßstab | Die Karte hängt in einer Gruppe, die mit `hexSize / 2,5` skaliert ist. Position, Kamera und Overlays der alten Ansicht passen dadurch ohne Umrechnung. |
| Aktualisierung | Nach jedem Befehl über `RefreshKnowledgeOverlays()` und zusätzlich alle 0,25 Sekunden. Es werden nur geänderte Chunks und Orte aktualisiert. |
| Wissen | Der Nebel entfällt. Unbekanntes ist flach und blass, Berichtetes blass, wie in der Entwicklungsansicht. `showKnowledgeFog = false` oder das Debug-Raster zeigt die Wahrheit. |
| Feldwahl | `TryGetPointerHex` schießt den Mausstrahl gegen das angezeigte Relief (statt gegen eine Ebene). Hover, Klick, Rechtsklick und Bewegen nutzen weiter dieselben Funktionen. |
| Overlays | Auswahl, Reichweite, Hover, Marker, Notizen und die Expeditionsfigur der alten Ansicht liegen jetzt auf der Geländehöhe (bei Overlays an der höchsten Stelle des Feldes, damit sie über Hängen bleiben). |
| Kamera | Q und E drehen die Karte um die Blickmitte; WASD, Mausrad und Rechts- oder Mittelklick-Ziehen wie bisher. Die Drehung wird nicht gespeichert. |
| Orte | Brücke aus Daten; Lager, Siedlung und Wachturm als Prototyp-Modelle (verkleinert); alle anderen Orte als neutraler Marker in der Archetypfarbe; Mauern als Quader mit Türmen. Das Hexfeld mit der Expedition behält die alte Marker-Figur. |

## 2. Prüfanleitung

1. Unity öffnen, kompilieren lassen und die Spielszene starten (wie bisher), Tutorial oder generierte Kampagne.
2. Karte, Hover und Klick prüfen: Reichweitenfelder, Bewegen, Rechtsklick-Untersuchen, Orte untersuchen.
3. Mit Q und E drehen. Prüfen, ob Hover und Klick danach noch das richtige Feld treffen.
4. Der Brückenort im Tutorial: Nach dem Bewegen dorthin erscheint der beobachtete Zustand.
5. Zurück zur alten Ansicht: In der Szene `Use Map Style Renderer` am Objekt `UnityHexMapView` ausschalten.

## 3. Was geprüft wurde und was nicht

Geprüft:

- Der gesamte Code unter `Assets` (ohne Editor) kompiliert gegen Unity, URP und die Spielassemblies (null Fehler).
- Rechenprobe der Feldwahl außerhalb von Unity: Tutorialkarte, 400 zufällige Felder, Kamerablick wie in
  `BuildCamera` und drei Drehungen (0, 90, 200 Grad). Mit Wissen: alle Treffer exakt. Ohne Wissen (volles
  Relief): 96 bis 99 Prozent exakt; der Rest sind Felder, die ein Berg vom Kamerastrahl verdeckt, dort trifft der
  Strahl zu Recht das Feld davor.

Nicht geprüft (kein Unity-Start möglich):

- Aussehen im Spiel: Helligkeit mit den drei bestehenden Lichtern, Schatten, Nebel (`RenderSettings.fog`) auf dem
  neuen Gelände, Kameraausschnitt auf der skalierten Karte.
- Leistung (Chunk-Aufbau im Spiel, Aktualisierung alle 0,25 Sekunden).
- Höhe und Lage der Overlays auf Hängen, Wirkung von UI-Blockern auf die neue Feldwahl.
- Das Ende des Aufbaus bei neuen Kampagnen (`Rebuild` räumt die Karte auf; die Meshes werden freigegeben).

## 4. Bekannte Einschränkungen

- Die Orte außer Brücke sind noch Prototyp-Modelle oder Marker; Kompositionen folgen mit V2c/V3.
- Die Kamera hat weiter den festen isometrischen Winkel der alten Ansicht (nur Drehung und Zoom).
- Straßen sind nur Bodenfarbe (siehe Plan, Abschnitt 13a); Gebietsgrenzen werden nicht gezeichnet.
- Der Ozean außerhalb der Karte ist die Hintergrundfarbe der Kamera; ein größerer Wasserrand ist vorbereitet.
- Der Shader wird per `Shader.Find` gesucht; in einem Build muss er in „Always Included Shaders“ stehen oder im Feld
  `Map Style Shader` der Szene zugewiesen werden.
