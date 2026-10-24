# V3b: Landmarken und Ressourcenorte aus Daten – Stand und Prüfanleitung

Status: **Entwurf zur Owner-Prüfung; Unity-Teil in Unity noch nicht ausgeführt**

Zugehörige Dokumente: `low_poly_map_presentation_v3a_review.md` (Ortsgalerie), `low_poly_map_presentation_implementation_plan.md` (Stufe V3).

## 1. Was V3b enthält

Zehn Kompositionen für Orte des Archetyps „natürliches Phänomen" (Punktanker, Maßstab 0,65), erzeugt aus gemeinsamen und neuen Teilen. Alle Orte
zeigen die Erkundung mit Pfahl (beobachtet bis dokumentiert), Vermessungsstativ (vermessen, dokumentiert) und Feldtafel (dokumentiert); Anwesenheit:
beobachtet zeigt eine Figur, verlassen Bewuchs. Ein unbeobachteter Ort zeigt nur seine neutrale Grundform (Bäume stehen, Kräuter wachsen, die Quelle ist klar).

| Variante | Betrieb im Bild |
| --- | --- |
| `signal-tower` (Signalturm) | dunkel: Turm mit kalter Feuerschale; entzündet: Flamme, Licht, Rauch; verfallen: Schaft und Plattform gekippt, Geröll |
| `colossus-statue` (Kolossstatue) | stehend; rissig: Risse und leichte Neigung; umgestürzt: Rumpf, Kopf und Arme liegen am Boden, Geröll |
| `old-lighthouse` (Alter Leuchtturm) | dunkel: Lampe aus Glas; an: leuchtende Lampe und Lichtkegel; eingestürzt: Turm gekippt, Lampe und Dach am Boden |
| `wind-carved-peak` (Windgeformter Gipfel, Gipfelmal) | stabil: Felsnadel; zugänglich: Stufen; kartiert: Stufen und Gipfelfahne |
| `freshwater-spring` (Süßwasserquelle) | klar: blaues Wasser; getrübt: braunes Wasser; versiegt: rissiger Schlamm |
| `fishing-grounds` (Fischgrund) | reich: fünf Fische; dünn: zwei Fische und Netzgestell; leergefischt: nur Netzgestell |
| `healing-herbs` (Heilkräuter) | wachsend: sechs Pflanzen mit Blüte; abgeerntet: Stummel; verdorrt: braune Büschel |
| `timber-stand` (Holzbestand) | stehend: fünf Bäume; teils gefällt: drei Bäume, zwei Stümpfe, Holzstapel; gerodet: fünf Stümpfe und Holzstapel |
| `salt-deposit` (Salzvorkommen) | offen: Kruste und Salzhaufen; abgebaut: Kruste, ein Haufen, Spitzhacke, Säcke; erschöpft: dünne Kruste |
| `berry-grove` (Beerenhain) | reif: Sträucher mit Beeren; abgeerntet: fast keine Beeren; befallen: dunkle Faulstellen |

## 2. Prüfanleitung

Menü **Map Style → Create Site Gallery Scene**: die Galerie listet die zehn Orte unter dem Archetyp `natural-phenomenon`. „Reihe: Betrieb" zeigt
alle drei Zustände nebeneinander.

## 3. Geprüft und nicht geprüft

Geprüft: alle `Game.Tests`; `SiteVisualTests` pinnt die Elementzahlen je Betriebs-, Anwesenheits-, Erkundungs- und unbeobachtetem Zustand für alle
zehn Orte; der Validator prüft die Abdeckung aller Zustandswerte.

Nicht geprüft: Aussehen, Größe, Proportionen von Turm, Leuchtturm und Statue, Kippposen, Lichtstärke von Feuer und Leuchtfeuer in Unity.

## 4. Offen

Verwahrung (Höhle, Schwelle), Grenzzeichen (Grenzsteine, Schädelmarken, Bänder im Wind), Pass, Gefahrenzonen (Flächenmodus), Siedlungen; Brücke in der Galerie.
