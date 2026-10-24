# V3d: Gefahrenzonen aus Daten – Stand und Prüfanleitung

Status: **Entwurf zur Owner-Prüfung; Unity-Teil in Unity noch nicht ausgeführt**

Zugehörige Dokumente: `low_poly_map_presentation_v3c_review.md`, `low_poly_map_presentation_v3a_review.md` (Ortsgalerie), `low_poly_map_presentation_implementation_plan.md`.

## 1. Was V3d enthält und wie Flächen behandelt werden

Drei Kompositionen für Gefahrenzonen (Maßstab 0,8, Ring 3,8). Die Zonen sind Flächen: der Generator überzieht mehrere Felder mit dem Biom der Gefahr
(Moor, Vulkan, tote Zone), und die Geländeansicht färbt und formt sie bereits. **Die Komposition sitzt auf dem mittleren Feld der Fläche** (Punktanker an der
Hauptkoordinate) und gibt dem Ort sein Merkmal. Das Datenformat bleibt unverändert; ein eigener Flächenmodus (Elemente über alle Felder verstreut) wäre eine
spätere Erweiterung und braucht dann eine Freigabe, weil er das Format ändert.

| Variante | Betrieb im Bild |
| --- | --- |
| `restless-marsh` (Unruhiges Moor) | aktiv: Tümpel, Schilf, Bülten und Dunst; gemieden: Warnschilder; durchquert: Bohlenweg; beschränkt: Absperrung; eingedämmt: kleinere Tümpel, Steinhaufen, Absperrung. Anwesenheit: breitet sich aus: dritter Tümpel und mehr Dunst |
| `volcanic-vent` (Vulkanschlund) | ruhend: Kegel und Krater; schwelend: Glut, schwaches Licht, Rauch; ausbrechend: Lava, Lavastrom, starkes Licht, dunkle Rauchwolke, Asche; gemieden: Warnschilder und Rauch |
| `dead-zone` (Tote Zone) | leblos: kahle Bäume, Asche, Knochen; gemieden: Warnschilder; durchquert: Steinlinie als Pfad; eingedämmt: Steinhaufen im Ring. Anwesenheit: breitet sich aus: größere Aschefläche |

Erkundung bei allen drei: Pfahl (bewertet), Absperrung (eingedämmt) und Feldtafel (dokumentiert); Anwesenheit „beobachtet" zeigt eine Figur.

## 2. Prüfanleitung

Ortsgalerie (Menü **Map Style → Create Site Gallery Scene**): die drei Orte stehen unter `hazard-site`. In der Welt erscheinen sie als Fläche des Gebiets mit dem
Merkmal in der Mitte.

## 3. Geprüft und nicht geprüft

Geprüft: alle `Game.Tests`; `SiteVisualTests` pinnt die Elementzahlen je Zustand; der Validator prüft die Abdeckung.
Nicht geprüft: Aussehen und Größe des Kegels und der Tümpel, Lichter, ob das mittlere Feld der Fläche überall sinnvoll liegt.

## 4. Offen

Verschütteter Pass (Kantenort), Siedlungen, Fraktionsfarbe der Grenzzeichen, Flächenmodus (verstreute Elemente), Brücke in der Galerie.
