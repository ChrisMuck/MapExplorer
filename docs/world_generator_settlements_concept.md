# Konzept: Siedlungen des Generators im Spielzustand und im Bild

Status: **Entwurf zur Owner-Freigabe; es wurde noch nichts am Code oder an den Daten geändert**

Zugehörige Dokumente:

- `world_generator_location_data_concept.md` (Generatorregeln, Varianten aus Daten)
- `low_poly_map_presentation_concept.md` (Abschnitt 7 und Anhang A.2: Siedlung und Dorf)
- `low_poly_map_presentation_implementation_plan.md` (Stufe V3)
- `gameconcept/cross_system_integration_concept.md` (Abschnitt zu Archetypen: bewohnte Orte bleiben Kontaktorte)

## 1. Ausgangslage (geprüft)

| Beobachtung | Fundstelle |
| --- | --- |
| Der Generator erzeugt je Fraktion eine **Hauptstadt** (`Faction.Seed`, `IsCapital`) und bis zu fünf **Städte** (`Faction.Towns`; Anzahl = Gebietsfelder / `CellsPerTown`, höchstens `MaxTowns`), jeweils mit Ortsnamen im Namensstil der Fraktion. | `WorldGenerator.PlaceSettlements`, `AssignNames` |
| Die **Straßen** verbinden Hauptstädte untereinander und jede Hauptstadt mit ihren Städten. | `WorldGenerator.BuildRoads` |
| `WorldGenBridge` übernimmt **keine** Siedlung in den Spielzustand. Übernommen werden nur Sonderorte (`Specials`), Gelände, Straßen, Flüsse und die Besitzerfraktion je Feld. | `WorldGenBridge.Bridge` |
| Folge: Auf generierten Karten gibt es **keine Dörfer oder Städte**, und die Straßen führen von Nirgendwo nach Nirgendwo. Auch die Expeditionsbasis ist dort kein Ort, nur eine Koordinate (`BaseLocation`). | derselbe Ort, `TutorialGameFactory.CreateGenerated` |
| Im Tutorial gibt es fünf **einfache Siedlungsmarken** (`LocationKind.Settlement` ohne Archetyp, Aktionen oder Zustände) und die Basis, fest im Code; sie werden mit dem alten Prototyp-Dorf gezeichnet (`MapStyleLocations.Village`). | `TutorialGameFactory.CreateTutorialLocations`, `MapStyleMapController` |
| Fraktionskontakt läuft über den Kontaktort `first-contact` (Archetyp `contact-site`, Aktionen beobachten, vorsichtig nähern, sprechen, handeln, Durchgang erbitten, zurückziehen). | `scenario-profiles.json`, `archetypes.json` |
| Der Generator kennt eine Fraktionsfarbe (`Faction.Color`, „nur zur Darstellung"); der Spielzustand (`FactionState`) trägt sie nicht. | `WorldTypes.cs` |
| Das Konzept legt fest: Bewohnte Siedlungen und Außenposten bleiben **Varianten des Kontaktortes**, es gibt keinen eigenen Siedlungsarchetyp. | `cross_system_integration_concept.md` |

## 2. Ziel

1. Hauptstädte und Städte (und optional Dörfer) des Generators sind **Orte im Spielzustand**: auffindbar, mit Namen, Besitzerfraktion, Zuständen und Aktionen.
2. Sie sind **Daten**: Varianten, Zustände, Texte und Bilder kommen aus JSON wie die übrigen Generator-Orte.
3. Straßen enden an Siedlungen; die Expeditionsbasis ist ein Ort mit Bild.
4. Wissensehrlichkeit bleibt: Ein Ort erscheint erst, wenn das Gelände bekannt ist; die Fraktionsfarbe zeigt nur, was der Spieler schon weiß.

## 3. Vorschlag

### 3.1 Datenmodell

- Archetyp `contact-site` (keine Änderung). Neue Varianten:

| Variante | Quelle | Bedeutung |
| --- | --- | --- |
| `faction-capital` | `Faction.Seed` | Sitz einer Fraktion: größte Siedlung, Mauer, Tor, mehrere Häuser, Markt |
| `faction-town` | `Faction.Towns` | Stadt: Häuser, Palisade oder Zaun, Tor |
| `faction-village` | neu (Abschnitt 3.3) | kleiner Weiler: wenige Häuser, Zaun |

- Jede Variante bekommt wie die anderen Generator-Orte Zustandsprofil, Szenarioprofil, Inhaltsprofil, Textschlüssel, Szenen- und Eröffnungstexte (Entwurf).
- **Zustände (Vorschlag).** Betrieb: `open` (Anfang), `restricted` (Zutritt beschränkt), `closed` (schließt sich Fremden), `abandoned` (verlassen); Anwesenheit:
  `unknown`, `present`, `guarded`; Erkundung: `unapproached`, `approached`, `contacted`, `withdrawn`. Der Betrieb folgt später aus Weltzügen und Fraktionshaltung
  (heute noch nicht), die Erkundung aus den Aktionen.
- **Namen.** Der Ortsname ist der erzeugte Eigenname (`PlaceName`), kein Katalogtext. Das ist neu gegenüber den anderen Generator-Orten, deren Anzeigename aus der Variante stammt.
  Der Spielzustand trägt den Namen ohnehin als Freitext (`SpecialLocationState.Name`).
- **Besitzer.** Jede Siedlung trägt die Beziehung „beansprucht" zu ihrer Fraktion (`LocationFactionRelationKind.Claimed`). Das nutzt die vorhandene Beziehungsliste.
- **Kennung** wie bisher `worldgen-location-<n>`; Reihenfolge: zuerst die Sonderorte (unverändert), danach Siedlungen in Fraktionsreihenfolge. So bleiben die Ids der bestehenden Orte gleich.

### 3.2 Übernahme in den Spielzustand

- Nur die **Brücke** ändert sich: `WorldGenBridge.BuildLocations` erzeugt zusätzlich einen Ort je Hauptstadt und Stadt. Die Platzierung im Generator bleibt unberührt,
  die Prüfsummen der Vergleichswelten gelten weiter (sie decken Sonderorte, nicht die Brücke ab; eine zusätzliche Prüfsumme über die Siedlungsorte kommt dazu).
- Die Fraktionsregeln bleiben datengetrieben; ein späteres `location-placement.json`-Gegenstück für Siedlungen ist nicht nötig, weil die Anzahl aus den vorhandenen
  Generatorparametern (`CellsPerTown`, `MaxTowns`) folgt.
- Die **Expeditionsbasis** wird in generierten Welten zu einem Ort der Art `BaseCamp` (wie im Tutorial), damit sie ein Bild und eine Marke bekommt.
  Das ändert keine Spielregel.

### 3.3 Dörfer (Entscheidung offen)

Der Generator kennt nur zwei Stufen. Für kleine Weiler (Konzept Anhang A.2) gibt es zwei Wege:

1. **Nur Hauptstadt und Stadt** (Vorschlag für den ersten Schnitt). Der Dorf-Look kann später als Variante dazukommen, ohne etwas umzubauen.
2. **Dörfer im Generator**: je Stadt null bis zwei Dörfer als Satelliten im eigenen Gebiet, mit eigener Regel in `PlacementTable` (Gruppe `settlements`, Kandidaten „Gebietsfelder
   mit Abstand zu Städten"). Das verändert Weltaufbau und Zufallsfolge (die Prüfsummen der Vergleichswelten ändern sich einmal gewollt).

### 3.4 Kontakt und Fraktion

Die Aktion „sprechen" beim Kontaktort soll die **besitzende Fraktion** ansprechen. Heute nennt eine Wirkung die Fraktion fest (`AddFactionMemory` mit `FactionId`).
Damit generierte Siedlungen jeder Fraktion gehören können, braucht die Wirkung die Angabe „Fraktion des Ortes" (neuer Platzhalter `owner-of-location`). Das ist die
einzige Änderung an der Spiellogik und braucht Tests (Kontakt, Anger, Gedächtnis). Ohne sie sind Siedlungen sicht- und besuchbar, aber noch ohne Folgen für die Fraktion.

### 3.5 Darstellung

Drei Kompositionen und eine für die Basis (Punktanker, größerer Maßstab als Einzelorte):

| Variante | Bild |
| --- | --- |
| `faction-capital` | 6 bis 8 Häuser um einen Marktplatz, Mauerring mit Tor und Turm, Fahnen, Rauch, Figurengruppen |
| `faction-town` | 4 bis 5 Häuser, Palisade mit Tor, Rauch, Figuren |
| `faction-village` | 2 bis 3 Häuser, Zaun, Rauch |
| Basis (`BaseCamp`) | Zelte, Lagerfeuer, Flagge der Expedition |

- **Zustände im Bild:** offen: Tor offen, Figuren; beschränkt: Tor halb geschlossen, Wachen; geschlossen: Tor zu, Wachen, kein Rauch; verlassen: Dächer eingesunken, Bewuchs, kein Licht.
  Anwesenheit „Wachen": Figuren am Tor. Erkundung: Pfahl, Feldtafel wie bei den anderen Orten.
- **Fraktionsfarbe.** Fahnen und Dachfarbe zeigen die Farbe der Fraktion, wenn der Spieler die Fraktion kennt; sonst neutral (grau/Papier). Dafür braucht es die offene
  Fraktionsfarbe der Grenzzeichen: `FactionState` trägt eine Farbe (aus den Fraktionsdaten statt fest im Generator), `LocationVisualContext` trägt eine **bekannte** Besitzerfarbe,
  Teile dürfen eine Farbe aus dem Kontext beziehen (`colorFrom: "owner"`). Dieselbe Mechanik färbt Grenzzeichen.
- Siedlungen werden in der **Ortsgalerie** (Menü Map Style) wie die übrigen Orte geprüft; für die Fraktionsfarbe bekommt die Galerie einen Farbwähler.

## 4. Stufen

| Stufe | Inhalt | Prüfung |
| --- | --- | --- |
| S0 | Dieses Konzept; Freigabe | Review |
| S1 | Varianten, Profile, Texte; Brücke erzeugt Siedlungsorte und Basisort; Straßen enden an Orten; Tests (Anzahl, Besitzer, Namen, Reihenfolge der Ids) | `Game.Tests` |
| S2 | Fraktionsfarbe: Spielzustand, Kontext, Teile mit Farbquelle; Grenzzeichen nutzen sie zuerst | `Game.Tests`, Galerie |
| S3 | Kompositionen für Hauptstadt, Stadt (und Dorf, falls Weg 2), Basis | Galerie, Unity |
| S4 | Wirkung „Fraktion des Ortes" (Abschnitt 3.4) und Kontaktablauf | `Game.Tests`, Spieltest |
| S5 (optional) | Dörfer im Generator (Abschnitt 3.3, Weg 2) | Prüfsummen neu begründet |

## 5. Risiken

- **Mehr Orte pro Welt** (bis ca. 6 je Fraktion). Weiche Verbindungen (`AddGeneratedSoftConnections`) wählen Orte nach Koordinate; mit Siedlungen ändern sich diese Verbindungen
  der generierten Welten. Das ist gewollt, ändert aber Prüfwerte in den Batch-Tests.
- **Wissensehrlichkeit:** Besitzerfarbe und Name verraten die Fraktion. Der Name ist ein Ortsname, der zu den Fraktionsstilen gehört; der Plan dazu ist, dass der Ort seinen Namen erst
  nach Kontakt oder Beobachtung zeigt (heute zeigt die Karte den Namen eines bekannten Ortes). Das ist eine Entscheidung für den Owner (Frage 3).
- **Aufwand der Bilder** (Maßstab, Leistung bei vielen Siedlungen): Kompositionen bleiben klein; Rauch und Lichter nur bei offenen Orten.
- **Alte Prototypen:** Die fünf Tutorialsiedlungen bleiben unverändert, bis S3 Bilder liefert; ihre Umstellung auf die neuen Varianten wäre ein getrennter Schritt.

## 6. Fragen an den Owner

1. Dörfer: Weg 1 (nur Hauptstadt und Stadt, Dorf später) oder Weg 2 (Dörfer im Generator)?
2. Sollen Siedlungen zu Beginn **unbekannt** sein und erst beim Aufdecken des Feldes erscheinen (Vorschlag), oder sollen Hauptstädte immer bekannt sein?
3. Soll der **Ortsname** einer Siedlung sofort auf der Karte stehen, oder erst nach Kontakt (vorher „Siedlung")?
4. Fraktionsfarbe nur bei bekannter Fraktion (Vorschlag) oder immer?
5. Die Basis als Ort mit Bild: einverstanden?
6. Reihenfolge: erst S1 (Orte und Straßen) und S3 (Bilder), danach S2 und S4 (Farbe und Kontakt), oder die Farbe zuerst?
