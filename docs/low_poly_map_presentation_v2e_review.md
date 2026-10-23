# V2e: Tor, Grenzwarnung und Ufer aus Daten – Stand und Prüfanleitung

Status: **Entwurf zur Owner-Prüfung; Unity-Teil in Unity noch nicht ausgeführt**

Zugehörige Dokumente: `low_poly_map_presentation_implementation_plan.md`, `low_poly_map_presentation_v2d_review.md`.

## 1. Was V2e enthält

Drei weitere Kompositionen in `Visuals/visual-definitions.json`, alle Punktanker, keine neue Unity-Logik. Damit haben alle
sechs Tutorial-Orte ein Bild aus Daten.

| Ort | Komposition | Betrieb | Anwesenheit | Erkundung |
| --- | --- | --- | --- | --- |
| Versiegeltes Tor | `vis-sealed-gate` (Maßstab 0,65) | versiegelt: roter Siegelbalken; geöffnet: Flügel schwingen auf, blaues Licht; aufgebrochen: Flügel liegen schief, Geröll | beobachtet: eine Wache; bewacht: zwei; verlassen: Bewuchs | untersucht: Pfahl; erforscht: Grabungsstelle; gesichert: Verstrebung; dokumentiert: Feldtafel |
| Grenzwarnung | `vis-border-warning` (0,8) | stehend: Pfosten, Schild, Steine; respektiert: ordentliche Steinreihe; verletzt: umgeknickt, Boden zertreten | beobachtet: ein Späher; bewacht: zwei | beobachtet: Pfahl; gedeutet: Feldtafel; dokumentiert: Vermessungshaufen |
| Ufer | `vis-coastal-landing` (0,65) | verfügbar: Steg, Boot, Hütte; geschlossen: Absperrungen; eingeladen: Banner und Laterne | anwesend: zwei Personen; bewacht: dazu zwei Speere | angenähert: Pfahl; Kontakt: Feldtafel; zurückgezogen: Pfahl und kalte Holzreste |

Ein Ort ohne Beobachtung zeigt nur die neutralen Grundteile (Tor 7, Grenzwarnung 5, Ufer 5 Elemente).

## 2. Prüfanleitung

Entwicklungsansicht oder Spielszene: Testort wechseln (Tor, Grenzwarnung, Ufer), springen und die Zustandsknöpfe
durchschalten. In der Spielszene `Debug Jump Location Id` auf `sealed-gate`, `border-warning` oder `coastal-landing`.

## 3. Geprüft und nicht geprüft

Geprüft: alle `Game.Tests` (neue Tests für jeden Ort mit Elementzahlen je Zustand und Überlagerung der Kanäle; die
Abdeckungsprüfung des Validators für alle Zustandswerte). Nicht geprüft: Aussehen, Größe, Lichter und Türschwung in Unity;
das Ufer wird nicht gezielt an der Küste ausgerichtet.

## 4. Offen

Alte Darstellungszweige (`BuildLandmarkObject`, klassischer Chunk-Renderer) entfernen; Wege, die an Brückenenden anschließen.
