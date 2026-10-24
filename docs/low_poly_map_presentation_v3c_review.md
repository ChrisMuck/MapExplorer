# V3c: Verwahrungsorte und Grenzzeichen aus Daten – Stand und Prüfanleitung

Status: **Entwurf zur Owner-Prüfung; Unity-Teil in Unity noch nicht ausgeführt**

Zugehörige Dokumente: `low_poly_map_presentation_v3b_review.md`, `low_poly_map_presentation_v3a_review.md` (Ortsgalerie), `low_poly_map_presentation_implementation_plan.md`.

## 1. Was V3c enthält

Fünf Kompositionen (Punktanker, Maßstab 0,65) aus gemeinsamen und neuen Teilen. Anwesenheit: bei Verwahrungsorten beobachtet eine, bewacht zwei Wachen,
verlassen Bewuchs; bei Grenzzeichen beobachtet ein, bewacht zwei Späher. Erkundung: Verwahrungsorte zeigen Pfahl (untersucht), Grabungen (erforscht),
Verstrebung (gesichert) und Feldtafel (dokumentiert); Grenzzeichen Pfahl (beobachtet), Feldtafel (gedeutet) und Vermessungshaufen (dokumentiert).

| Variante | Betrieb im Bild |
| --- | --- |
| `sealed-cave` (Verschlossene Höhle) | versiegelt: drei Steine und roter Siegelbalken vor dem Eingang; offen: Steine beiseite, dunkle Öffnung und kühles Licht; eingestürzt: Felsbrocken |
| `warded-threshold` (Gebannte Schwelle) | gebannt: leuchtender Bannkreis, Zeichen und Licht; geschwächt: matter Kreis; gebrochen: Schwelle schief, Türsturz gefallen, Geröll |
| `border-stones` (Grenzsteine) | stehend: fünf Steine mit Zeichen; respektiert: Linie heller Steine davor; verletzt: zwei Steine umgestürzt, Boden zertreten |
| `skull-marks` (Schädelmarken) | stehend: vier Pfähle mit Schädeln; respektiert: Steinlinie; verletzt: zwei Pfähle umgestürzt, Boden zertreten |
| `wind-ribbons` (Bänder im Wind) | stehend: drei Stangen mit je drei farbigen Bändern; respektiert: Steinlinie; verletzt: eine Stange mit Bändern umgestürzt |

Die Farbe der Grenzzeichen nach Besitzer (Entscheidung des Owners, „wenn einfach möglich") ist **noch nicht** umgesetzt: Dafür muss der Auflöser die
Fraktionsfarbe des Ortes durchreichen und die Ansicht Teile einfärben können. Das ist ein eigener Schritt.

## 2. Prüfanleitung

Ortsgalerie (Menü **Map Style → Create Site Gallery Scene**): die fünf Orte stehen unter `containment-site` und `territorial-marker`; „Reihe: Betrieb"
zeigt die drei Zustände nebeneinander.

## 3. Geprüft und nicht geprüft

Geprüft: alle `Game.Tests`; `SiteVisualTests` pinnt die Elementzahlen je Betriebs-, Anwesenheits-, Erkundungs- und unbeobachtetem Zustand
(die Schleifen laufen je Kanal getrennt, weil die Kanäle unterschiedlich viele Werte haben); der Validator prüft die Abdeckung.

Nicht geprüft: Aussehen, Größe, Kippposen und Lichter in Unity.

## 4. Offen

Verschütteter Pass (Kantenort wie die Brücke), Gefahrenzonen (Flächenmodus), Siedlungen, Fraktionsfarbe der Grenzzeichen, Brücke in der Galerie.
