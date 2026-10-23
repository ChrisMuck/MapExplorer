# V2d: Markiertes Grab aus Daten – Stand und Prüfanleitung

Status: **Entwurf zur Owner-Prüfung; Unity-Teil in Unity noch nicht ausgeführt**

Zugehörige Dokumente:

- `low_poly_map_presentation_implementation_plan.md` (Stufe V2)
- `low_poly_map_presentation_v2c_review.md` (Testhilfen, Datenformat)

## 1. Was V2d enthält

Das markierte Grab (`investigation-site` / `marked-grave`) wird aus Daten gezeichnet: Komposition `vis-marked-grave`
(Punktanker, Maßstab 0,65) mit zwölf neuen Teilen in `Visuals/visual-definitions.json`. Es gibt keine neue Unity-Logik
für den Ort selbst; Resolver und Ansicht aus V2c genügen.

| Kanal | Wert | Sichtbar |
| --- | --- | --- |
| Betrieb | versiegelt (`sealed`) | Grabplatte vor dem Eingang, zwei rote Siegelbalken |
| | offen (`open`) | Platte zur Seite gekippt, dunkle Öffnung, Schaufel, Erdhaufen, grünes Licht |
| | gestört (`disturbed`) | Platte halb verschoben, dunkle Öffnung, Erdhaufen |
| Anwesenheit | beobachtet (`watched`) | Figur am Eingang |
| | verlassen (`abandoned`) | Bewuchs (zwei Büsche) |
| | leer, unbekannt (`empty`) | Grundform |
| Erkundung | untersucht | Pfahl der Expedition |
| | durchsucht, erforscht | zusätzlich Grabungsstellen |
| | dokumentiert | zusätzlich Feldtafel |
| unbekannt | keine Beobachtung | nur Hügel, Eingangsrahmen, Platte, Grabsteine, Kreuz (8 Elemente); kein Licht |

## 2. Testhilfe verallgemeinert

Die Entwicklungsansicht hat jetzt „Testort: … (wechseln)“ und „Expedition zum Testort springen“. Die Zustandsknöpfe
(„Ortszustand“) gelten für den gewählten Ort; Kandidaten sind alle Orte mit Punktkomposition (Lager, Grab). In der
Spielszene bleibt es bei `Debug Jump Location Id` (`marked-grave`) und F9, F8, F7, F6.

## 3. Prüfanleitung

1. Unity öffnen, `MapStyleWorld` oder die Spielszene starten.
2. Testort auf `marked-grave` stellen, springen, Betrieb auf versiegelt, offen, gestört schalten: Platte, Öffnung, Licht.
3. Anwesenheit und Erkundung durchschalten; das Grab im Tutorial untersuchen und die Spuren sehen.

## 4. Geprüft und nicht geprüft

Geprüft: alle `Game.Tests` (neu: Elementzahlen je Betriebszustand 10/12/10, Überlagerung mit Anwesenheit und Erkundung,
Licht nur bei offenem Grab, unbeobachtetes Grab 8 neutrale Teile); Code unter `Assets` kompiliert.

Nicht geprüft: Aussehen, Größe, Lichtstärke und Plattenneigung in Unity; Fußpunkt auf dem Relief.

## 5. Offen

Tor, Grenzwarnung, Ufer als nächste Kompositionen; danach Entfernen der alten Darstellungszweige.
