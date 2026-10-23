# V2c: Verlassenes Lager aus Daten – Stand und Prüfanleitung

Status: **Entwurf zur Owner-Prüfung; Unity-Teil in Unity noch nicht ausgeführt**

Zugehörige Dokumente:

- `low_poly_map_presentation_concept.md` (Abschnitte 7.5a und 12)
- `low_poly_map_presentation_implementation_plan.md` (Stufe V2)
- `low_poly_map_presentation_v2b_review.md`

## 1. Was V2c enthält

Das verlassene Lager (`investigation-site` / `abandoned-camp`) wird vollständig aus Daten gezeichnet. Dabei wurde das
Datenformat so erweitert, dass es auch Licht, Rauch und überlagerte Zustandskanäle trägt.

### Datenformat (getestet)

| Erweiterung | Zweck |
| --- | --- |
| `kind: light` | Punktlicht mit Farbe, Stärke und Reichweite (Lagerfeuer) |
| `kind: smoke` | Rauchwolke mit Farbe und Größe |
| `scale` an der Komposition | Das Lager wird auf der Karte kleiner gezeichnet (0,65) |
| `plain` an der Komposition | Zustände ohne eigenes Bild (`interaction: untouched`, `operational: none`); sie zählen für die Abdeckungsprüfung als abgedeckt und werden gegen das Zustandsprofil validiert |

Neue Datei-Inhalte: 14 weitere Teile und die Komposition `vis-abandoned-camp` in
`Visuals/visual-definitions.json`.

### Das Lager im Bild

| Kanal | Wert | Sichtbar |
| --- | --- | --- |
| Anwesenheit | verlassen (`abandoned`) | Zelte eingesunken, Ascheplatz, umgekippte Kiste, zerrissene Fahne, kein Licht |
| | kürzlich benutzt (`recently-used`) | Zelte stehen, Feuer mit Licht und Rauch, Kiste aufrecht, Fahne |
| | beobachtet (`watched`) | wie kürzlich benutzt, dazu zwei Figuren |
| Betrieb | geräumt (`cleared`) | Steinhaufen |
| | unsicher (`unsafe`) | Warnschild |
| Erkundung | untersucht (`inspected`) | Pfahl der Expedition |
| | durchsucht, erforscht (`searched`, `investigated`) | zusätzlich Grabungsstellen |
| | dokumentiert (`documented`) | zusätzlich Feldtafel |
| unbekannt | keine Beobachtung | nur Zelte (stehend), Holzscheite und Fahnenmast; kein Feuer, Licht oder Rauch |

Die Kanäle überlagern sich ohne Sonderfälle (Konzept 7.5a).

### Unity (nur kompiliert)

- `MapStyleLocationView` kennt jetzt Lichter (Reichweite und Stärke folgen dem Maßstab des Ortes) und Rauch.
- `MapStyleMapController` setzt Orte mit Punktanker auf das Hexfeld und passt sie dem Gelände an; Kantenorte wie die Brücke
  wie bisher.
- Die Beschriftung zeigt den bekannten Zustand ohne unbekannte Kanäle.

## 2. Prüfanleitung

1. Unity öffnen und kompilieren lassen.
2. In der Entwicklungsansicht (`MapStyleWorld`) das Tutorial laden und zum Lager gehen oder per „Erkunden +1“ aufdecken.
   Das Lager erscheint zunächst nur mit Zelten (Zustand unbekannt).
3. In der Spielszene das Lager erreichen und untersuchen: Der Zustand folgt der Beobachtung (Pfahl, Grabungen, Feldtafel
   je nach Fortschritt).
4. Bei Nacht (Entwicklungsansicht, Tageszeit) leuchtet das Feuer, wenn das Lager benutzt wirkt.

## 3. Was geprüft wurde und was nicht

Geprüft:

- Alle `Game.Tests` laufen durch. Neue Tests: Zahl der sichtbaren Elemente je Anwesenheit, Betrieb und Erkundung
  (verlassen 11, benutzt 13, beobachtet 15; Steinhaufen oder Schild plus 1; Spuren plus 1, 3 und 4), unbeobachtetes Lager
  (8 neutrale Teile), Licht und Rauch nur bei benutztem Lager, `plain` zählt als abgedeckt und wird validiert, ein Licht
  braucht eine Farbe.
- Der Code unter `Assets` kompiliert gegen Unity, URP und die frisch gebauten Spielassemblies.

Nicht geprüft:

- Aussehen und Helligkeit des Lagers in Unity (Lichtstärke, Rauch), Übergänge bei Zustandswechseln, Leistung.
- Das Lager an der Spielposition: Fußpunkt und Größe auf dem Relief.

## 4. Bekannte Einschränkungen

- Die Expeditionsbasis (`BaseCamp`) und andere Orte nutzen weiter die Prototyp-Modelle oder Marker.
- Ein unbeobachtetes Lager zeigt stehende Zelte als neutrale Grundform; das ist keine Aussage über Anwesenheit.
- Grab, Tor, Grenzwarnung und Ufer folgen als nächste Kompositionen.
