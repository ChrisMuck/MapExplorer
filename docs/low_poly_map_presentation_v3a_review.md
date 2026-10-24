# V3a: Untersuchungs- und Spurenorte des Generators aus Daten – Stand und Prüfanleitung

Status: **Entwurf zur Owner-Prüfung; Unity-Teil in Unity noch nicht ausgeführt**

Zugehörige Dokumente: `low_poly_map_presentation_implementation_plan.md` (Stufe V3), `low_poly_map_presentation_v2e_review.md`, `world_generator_location_data_concept.md`.

## 1. Was V3a enthält

Sechs Kompositionen in `Visuals/visual-definitions.json` (Punktanker, Maßstab 0,65), erzeugt aus gemeinsamen Teilen (Steine, Holz, Zelt-Figuren,
Erkundungsspuren der früheren Orte). Keine neue Unity-Logik; Resolver und Ansicht aus V2 genügen.

| Variante | Betrieb im Bild | Anwesenheit | Elemente |
| --- | --- | --- | --- |
| `old-ruin` (Alte Ruine) | bröckelnd: Mauerreste; abgestützt: Holzstützen; eingestürzt: Wand und Pfeiler gekippt, mehr Geröll | beobachtet: Figur; verlassen: Bewuchs | 17 |
| `stone-circle` (Steinkreis) | ruhend: sieben Steine und Altar; erwacht: leuchtende Runenscheibe und Licht; gebrochen: zwei Steine umgestürzt | wie oben | 17 |
| `ruined-shrine` (Verfallener Schrein) | verfallen: Dach hängt; gepflegt: Blumen und Kerzenlicht; geschändet: Dach und Götzenbild gekippt, roter Fleck | wie oben | 17 |
| `cold-fireplace` (Kalte Feuerstelle) | kalt: Asche und Scheite; glimmend: Glut, Licht und Rauch; geräumt: Steinhaufen | benutzt: Schlafrolle und Kiste; beobachtet: zwei Figuren | 21 |
| `broken-cart` (Zerbrochener Karren) | zerbrochen: Säcke, Kiste, Rad; geplündert: Lumpen; geborgen: Bretterstapel | benutzt: frische Spuren; beobachtet: zwei Figuren | 17 |
| `old-boat-camp` (Altes Bootslager) | verlassen: gekippter Rumpf; ausgebessert: Mast, Flicken, Ruder; in Benutzung: Segel, Trockengestell, Feuer mit Licht und Rauch | benutzt: Schlafrolle und Kiste; beobachtet: zwei Figuren | 19 |

Die Erkundung zeigt wie bei Lager und Grab Pfahl, Grabungsstellen und Feldtafel.

## 2. Prüfanleitung

### Ortsgalerie (neu)

Menü **Map Style → Create Site Gallery Scene (all places, switchable states)** legt die Szene `Assets/MapStyle/Scenes/MapStyleSiteGallery.unity` an und
speichert sie (wie bei „Create Sandbox Scene"). Beim Start zeigt die Galerie alle Orte, die eine Punktkomposition haben (heute: Lager, Grab, Tor,
Grenzwarnung, Ufer und die sechs Orte aus V3a; jede künftige Komposition erscheint von selbst).

- **Ort wählen:** Liste „Alle Orte" links, Knöpfe „zurück" und „weiter", Bild-Hoch/Bild-Runter oder `[` `]`.
- **Zustände:** je Kanal (Betrieb, Anwesenheit, Erkundung) eine Knopfreihe mit den Werten des Zustandsprofils. Der Ort startet im Anfangszustand.
- **Reihe:** „Reihe: Betrieb" (oder Anwesenheit, Erkundung) stellt alle Werte eines Kanals nebeneinander, die anderen Kanäle bleiben wie gewählt.
- **Wissen:** „Zustand dem Spieler unbekannt" zeigt den Ort ohne Beobachtung; die Leiste bestätigt, alt, gemeldet zeigt die Kenntnisstufen.
- **Tageszeit** und Kamera (Drehen mit Q/E, Ziehen, Zoomen) wie in der Entwicklungsansicht der Welt.

Die Galerie nutzt denselben Auflöser und dieselbe Ortsansicht wie das Spiel, auf einer flachen Platte. Brücke (Kantenort) und Gefahrenzonen mit Fläche
kommen später dazu.

### In der Welt

Entwicklungsansicht (`MapStyleWorld`) mit generierter Welt: Orte dieser Varianten kommen je nach Seed vor. Testort wechseln, springen, Zustandsreihen
durchschalten. In der Spielszene `Debug Jump Location Id` auf die Id eines Ortes setzen (z. B. `worldgen-location-3`).

## 3. Geprüft und nicht geprüft

Geprüft: alle `Game.Tests`; neu `SiteVisualTests` mit den Elementzahlen je Betriebs-, Anwesenheits- und Erkundungszustand (und unbeobachtet) für alle sechs
Varianten. Der Validator prüft, dass jeder Zustandswert gezeigt, gepost oder als schlicht gelistet ist.

Nicht geprüft: die Ortsgalerie selbst (nur kompiliert), Aussehen, Größe und Lichtstärke in Unity; die Plattenneigungen und Kippposen; Fußpunkt auf dem Relief.

## 4. Offen

Landmarken (4), Ressourcen (6), Verwahrung (2: Höhle, Schwelle), Grenzzeichen (3), Pass, Gefahrenzonen (Flächenmodus), Siedlungen.
