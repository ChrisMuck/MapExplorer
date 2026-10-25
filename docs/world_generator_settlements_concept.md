# Konzept: Siedlungen des Generators im Spielzustand und im Bild

Status: **Entscheidungen des Owners eingetragen (Abschnitt 6); es wurde noch nichts am Code oder an den Daten geändert**

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

1. Hauptstädte, Städte, Dörfer und Fischerdörfer des Generators sind **Orte im Spielzustand**: auffindbar, mit Namen, Besitzerfraktion, Zuständen und Aktionen.
2. Sie sind **Daten**: Varianten, Zustände, Texte und Bilder kommen aus JSON wie die übrigen Generator-Orte.
3. Straßen enden an Siedlungen; die Expeditionsbasis ist ein Ort mit Bild.
4. Wissensehrlichkeit bleibt: Ein Ort erscheint erst, wenn das Gelände bekannt ist; die Fraktionsfarbe zeigt nur, was der Spieler schon weiß.

## 3. Vorschlag

### 3.1 Datenmodell

- Archetyp `contact-site` (keine Änderung). Neue Varianten (vier Stufen: Hauptstadt, Stadt, Dorf, Fischerdorf):

| Variante | Quelle | Bedeutung |
| --- | --- | --- |
| `faction-capital` | `Faction.Seed` | Sitz einer Fraktion: größte Siedlung, Mauer, Tor, mehrere Häuser, Markt |
| `faction-town` | `Faction.Towns` | Stadt: Häuser, Palisade oder Zaun, Tor |
| `faction-village` | neu (Abschnitt 3.3) | Landdorf: wenige Häuser, Zaun |
| `fishing-village` | neu (Abschnitt 3.3) | Fischerdorf an Küste oder Wasser: Hütten, Boote, Trockengestelle |

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

### 3.3 Dörfer (entschieden: im Generator)

Der Generator bekommt eine dritte Stufe. Dörfer sind klein, zahlreicher als Städte, aber **begrenzt**:

- Varianten: `faction-village` (Landdorf) und `fishing-village` (**Fischerdorf**, nur an Küste oder am Wasser). Ein Fischerdorf kann keine Stadt sein; Städte und Hauptstädte
  entstehen weiter nur im Landesinneren des Gebiets (Biom und Abstand wie heute).
- Platzierung in `PlaceSettlements` nach den Städten, im Gebiet der Fraktion, mit eigenem Abstand zu allen Siedlungen. Die Regeln liegen als Daten vor
  (Gruppe `settlements` im Platzierungsdokument, siehe unten), nicht als feste Zahlen im Code.
- **Begrenzung (Startwerte, werden an Testwelten geprüft):**

| Regel | Startwert |
| --- | --- |
| Dörfer je Stadt | 0,6 (aufgerundet nach Zufall, aber nie mehr als der Deckel) |
| Dörfer je Fraktion höchstens | 4 |
| Abstand zu jeder anderen Siedlung mindestens | 3 Felder |
| Dörfer an der Küste werden zu Fischerdörfern | ja; höchstens die Hälfte der Dörfer einer Fraktion |
| Prüfwert im Test | Dörfer je Fraktion höchstens `floor(Städte × 0,6) + 1` und nie mehr als der Deckel; Fischerdörfer höchstens die Hälfte (aufgerundet) |

Zum Vergleich (Mittel aus acht Welten je Größe, drei Fraktionen): 40 x 30 Felder haben 610 Landfelder, 3 Hauptstädte, 10,6 Städte und 22,6 Sonderorte; 90 x 68 haben 3185
Landfelder, 3 Hauptstädte, 14,9 Städte und 93 Sonderorte. Mit den Startwerten kämen im ersten Fall rund 6 Dörfer, im zweiten rund 9 dazu; Siedlungen blieben damit klar in der Minderheit
gegenüber den Orten zum Erkunden.

- Die Regeln sind Daten: Das Platzierungsdokument bekommt einen Abschnitt `settlements` mit `villagesPerTown`, `maxVillagesPerFaction`, `minSpacing`, `coastalVillageVariant`.
- Der Zufallsablauf des Generators ändert sich dadurch; die Prüfsummen der Vergleichswelten (Sonderorte) gelten nur, wenn die Dörfer nach den Sonderorten gewürfelt werden (eigener
  Zufallsstrom für Siedlungen). Das ist die Absicherung: **die Sonderorte bleiben für jeden Seed unverändert**.

### 3.4 Kontakt und Fraktion

Die Aktion „sprechen" beim Kontaktort soll die **besitzende Fraktion** ansprechen. Heute nennt eine Wirkung die Fraktion fest (`AddFactionMemory` mit `FactionId`).
Damit generierte Siedlungen jeder Fraktion gehören können, braucht die Wirkung die Angabe „Fraktion des Ortes" (neuer Platzhalter `owner-of-location`). Das ist die
einzige Änderung an der Spiellogik und braucht Tests (Kontakt, Anger, Gedächtnis). Ohne sie sind Siedlungen sicht- und besuchbar, aber noch ohne Folgen für die Fraktion.

### 3.5a Wissen und Name (entschieden)

- Eine Siedlung erscheint **erst, wenn ihr Feld aufgedeckt ist**, wie jeder Ort. Kontakt verrät nur grobe Richtungen („im Norden", „im Westen"), nie ein Feld; Kontakt deckt kein Gelände auf.
- Vor dem Kontakt zeigt die Karte die **allgemeine Bezeichnung der Variante** (Hauptstadt, Stadt, Dorf, Fischerdorf). Den **Eigennamen** erfährt der Spieler erst durch Kontakt
  (Erkundungszustand „contacted"). Technisch: `SpecialLocationState` trägt den allgemeinen Namen und den Eigennamen getrennt; `KnowledgeState` speichert den bekannten Namen
  (`KnownName`), den die Kontaktaktion setzt; die Karte zeigt `KnownName ?? allgemeine Bezeichnung`. Der Eigenname liegt damit nur in der Wahrheit (`WorldState`), nicht im Spielerwissen.

### 3.5 Darstellung

Drei Kompositionen und eine für die Basis (Punktanker, größerer Maßstab als Einzelorte):

| Variante | Bild |
| --- | --- |
| `faction-capital` | 6 bis 8 Häuser um einen Marktplatz, Mauerring mit Tor und Turm, Fahnen, Rauch, Figurengruppen |
| `faction-town` | 4 bis 5 Häuser, Palisade mit Tor, Rauch, Figuren |
| `faction-village` | 2 bis 3 Häuser, Zaun, Rauch |
| `fishing-village` | 2 bis 3 Häuser auf Pfählen oder am Ufer, Boote, Trockengestelle, Rauch |
| Basis (`BaseCamp`) | **eigenes Design**: befestigtes Expeditionslager mit Zelten, Lagerhaus, Fahnenmast, Feuer und Tor, anders als jede Siedlung (kein Zaun einer Fraktion) |
| Expedition unterwegs | **eigenes Bild** für die Gruppe auf der Karte: Figurengruppe mit Tragtieren und Fahne; Größe nach Zahl der Mitglieder, Haltung nach Zustand (marschiert, lagert, erschöpft) |

- **Zustände im Bild:** offen: Tor offen, Figuren; beschränkt: Tor halb geschlossen, Wachen; geschlossen: Tor zu, Wachen, kein Rauch; verlassen: Dächer eingesunken, Bewuchs, kein Licht.
  Anwesenheit „Wachen": Figuren am Tor. Erkundung: Pfahl, Feldtafel wie bei den anderen Orten.
- **Fraktionsfarbe.** Fahnen und Dachfarbe zeigen die Farbe der Fraktion, wenn der Spieler die Fraktion kennt; sonst neutral (grau/Papier). Dafür braucht es die offene
  Fraktionsfarbe der Grenzzeichen: `FactionState` trägt eine Farbe (aus den Fraktionsdaten statt fest im Generator), `LocationVisualContext` trägt eine **bekannte** Besitzerfarbe,
  Teile dürfen eine Farbe aus dem Kontext beziehen (`colorFrom: "owner"`). Dieselbe Mechanik färbt Grenzzeichen.
- Die Expedition ist kein Ort, sondern eine Einheit der Karte. Ihr Bild gehört zu V4 (Figuren und Gruppen); es bekommt in der Galerie eine eigene Rubrik „Gruppen". Die Basis
  ist ein Ort mit Bild und steht in S3.
- Siedlungen werden in der **Ortsgalerie** (Menü Map Style) wie die übrigen Orte geprüft; für die Fraktionsfarbe bekommt die Galerie einen Farbwähler.

## 4. Stufen

Reihenfolge nach Owner-Entscheid: zuerst Orte, Straßen und Bilder, danach Farbe und Kontakt.

| Stufe | Inhalt | Prüfung |
| --- | --- | --- |
| S0 | Dieses Konzept; Freigabe | Review |
| S1 | Generator: Dörfer und Fischerdörfer (Regeln als Daten, eigener Zufallsstrom, Deckel); Varianten, Profile und Texte für Hauptstadt, Stadt, Dorf, Fischerdorf; Brücke erzeugt Siedlungsorte und den Basisort; Straßen enden an Orten | `Game.Tests`: Anzahl und Deckel für viele Seeds, Sonderorte unverändert (Prüfsummen), Besitzer, Namen, Ids |
| S3 | Kompositionen für Hauptstadt, Stadt, Dorf, Fischerdorf und Basis (Galerie), Zustände im Bild | Galerie, Unity |
| S3b | Bild der Expedition (Gruppe) als eigene Rubrik; gehört zu V4, hier nur vorgemerkt | Galerie |
| S2 | Fraktionsfarbe: Spielzustand, Kontext, Teile mit Farbquelle; Grenzzeichen nutzen sie zuerst; nur bei bekannter Fraktion | `Game.Tests`, Galerie |
| S4 | Kontakt: Wirkung „Fraktion des Ortes", Eigenname erst nach Kontakt (`KnownName`), Richtungshinweis statt Feld | `Game.Tests`, Spieltest |

## 4a. Stand S1 (umgesetzt)

- Generator: `PlaceVillages` nach den Sonderorten, eigener Zufallsstrom (`villages`), Regeln als `SettlementRules` in `PlacementTable` und im Abschnitt `settlements` von
  `location-placement.json`. `Faction.Villages` enthält Dörfer und Fischerdörfer mit Eigennamen. **Die Sonderorte bleiben für jeden Seed unverändert**
  (die 108 Vergleichsprüfsummen gelten weiter).
- Katalog: Varianten `faction-capital`, `faction-town`, `faction-village`, `fishing-village` (Kontaktort, Zustandsprofil `state-first-contact`, Aktionen und Szenen wie `first-contact`),
  Inhaltsprofile mit Textschlüsseln, Eröffnungstexte; die Texte sind Entwürfe.
- Brücke: Hauptstädte, Städte und Dörfer werden Orte (`LocationKind.Settlement`, beansprucht von ihrer Fraktion, `ProperName` getrennt vom angezeigten Namen), die Basis wird der Ort `base-camp`.
  Die weichen Verbindungen der Welt nutzen weiter nur die Sonderorte. Straßen enden an Siedlungen (Test über viele Welten).
- **Fund beim Bau:** Generierte Orte starteten bisher alle im Zustand `untouched/none/unknown`, den die Zustandsprofile der meisten Varianten gar nicht kennen; beim ersten Zugriff
  auf einen Ort hätte die Zustandsprüfung (`ValidateRuntimeState`) eine Ausnahme geworfen. Die Brücke setzt jetzt die Anfangszustände des Zustandsprofils der Variante
  (`GameDataCatalog.VariantInitialStates`); ein Test prüft alle Orte vieler Welten.
- Offen für S3 und S4: Bilder, Fraktionsfarbe, Eigenname erst nach Kontakt (`KnownName`; die Karte zeigt bis dahin die allgemeine Bezeichnung), „Richtung statt Feld".

## 4b. Stand S3 (umgesetzt, in Unity ungeprüft)

Fünf Kompositionen in `Visuals/visual-definitions.json`, geprüft in der Ortsgalerie (Menü Map Style → Create Site Gallery Scene):

| Ort | Bild | Zustände |
| --- | --- | --- |
| `faction-capital` (Hauptstadt) | Mauerring mit sechs Türmen, Torhaus mit zwei Türmen und Flügeltoren, Burg, sechs Häuser, zwei Marktstände, Brunnen, Banner; 48 Elemente | Betrieb: verfügbar und eingeladen: Tore offen und Rauch; geschlossen: Tore zu, kein Rauch; eingeladen: zusätzlich Fahne und Laterne. Anwesenheit: da: Figuren; bewacht: dazu Wachen mit Speeren. Erkundung: Pfahl, Feldtafel, kalte Scheite |
| `faction-town` (Stadt) | Palisadenring mit Tor, fünf Häuser, Marktstand, Brunnen; 43 Elemente | wie oben |
| `faction-village` (Dorf) | Zaun, drei Häuser, Heuhaufen, Feld, ein Schornstein; 29 Elemente | wie oben |
| `fishing-village` (Fischerdorf) | drei Hütten auf Pfählen, Steg, zwei Boote, Trockengestelle, Fischfässer; 25 Elemente | wie oben |
| Expeditionsbasis | Palisade mit Tor, drei Zelte, Lagerhaus, blaue Expeditionsfahne, Feuer mit Licht und Rauch, Kisten, zwei Figuren; 30 Elemente; **immer gleich**, kein Zustand | keine |

- Die Basis hat keinen Archetyp im Katalog. Der Auflöser findet ihr Bild unter dem **Pseudo-Archetyp `base-camp`** (`VisualDefinitionSet.PseudoArchetypeIds`); der Validator lässt ihn für Kompositionen zu.
  Eine Marke (Ring, Expeditionsblau) gehört dazu.
- Fraktionsfarbe fehlt noch (S2): Fahnen und Dächer der Siedlungen sind neutral.
- Das Bild der **Expedition unterwegs** ist noch offen (V4, Gruppen).

## 5. Risiken

- **Mehr Orte pro Welt** (bis ca. 6 je Fraktion). Weiche Verbindungen (`AddGeneratedSoftConnections`) wählen Orte nach Koordinate; mit Siedlungen ändern sich diese Verbindungen
  der generierten Welten. Das ist gewollt, ändert aber Prüfwerte in den Batch-Tests.
- **Wissensehrlichkeit:** Besitzerfarbe und Name verraten die Fraktion. Der Name ist ein Ortsname, der zu den Fraktionsstilen gehört; der Plan dazu ist, dass der Ort seinen Namen erst
  nach Kontakt oder Beobachtung zeigt (heute zeigt die Karte den Namen eines bekannten Ortes). Das ist eine Entscheidung für den Owner (Frage 3).
- **Aufwand der Bilder** (Maßstab, Leistung bei vielen Siedlungen): Kompositionen bleiben klein; Rauch und Lichter nur bei offenen Orten.
- **Alte Prototypen:** Die fünf Tutorialsiedlungen bleiben unverändert, bis S3 Bilder liefert; ihre Umstellung auf die neuen Varianten wäre ein getrennter Schritt.

## 6. Entscheidungen des Owners

| Frage | Entscheidung | Folge |
| --- | --- | --- |
| 1. Dörfer | **Weg 2: Dörfer im Generator**, auch Fischerdörfer; von allen Siedlungsarten darf es nicht zu viele geben | Abschnitt 3.3: eigene Stufe, Deckel, eigener Zufallsstrom, Prüfwert |
| 2. Sichtbarkeit | **Erst bekannt, wenn das Feld aufgedeckt ist**; Kontakt nennt nur Richtungen, keine Koordinate | Abschnitt 3.5a: Kontakt deckt kein Gelände auf |
| 3. Ortsname | **Erst nach Kontakt** | `KnownName` im Spielerwissen (S4); davor allgemeine Bezeichnung |
| 4. Fraktionsfarbe | **Nur bei bekannter Fraktion** | S2 |
| 5. Basis | **Eigenes Design** für die Basis, ebenso ein eigenes Bild für die Expedition unterwegs | Abschnitt 3.5; Expedition gehört zu V4 |
| 6. Reihenfolge | **Erst Orte, Straßen, Bilder; dann Farbe und Kontakt** | Stufen S1, S3, danach S2, S4 |
