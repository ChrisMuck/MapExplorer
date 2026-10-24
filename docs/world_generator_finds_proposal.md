# Vorschlag: Funde, Beweise und Folgen der 23 Generator-Varianten

Status: **vom Owner freigegeben und umgesetzt, siehe `world_generator_location_data_concept.md` (Stand Funde und Beweise)**

Zugehörig: `world_generator_location_data_concept.md` (Stand G3), `world_generator_variant_states_proposal.md`.

## 1. Wie Funde, Beweise und Folgen heute funktionieren (geprüft)

| Baustein | Quelle im Spiel | Folge für die neuen Varianten |
| --- | --- | --- |
| **Fund** über den Pool des Szenarioprofils (`findingPoolIds`) | Beim Untersuchen eines Ortes (`InspectLocationCommand`) wählt der Dienst einen Fund aus dem Pool der Variante. | Reicht aus, damit jede Variante etwas hergibt. Pool pro Variante, keine Codeänderung. |
| **Fund** über Fundtabellen (`RollFindingTable`) | Aktionen wie „Untersuchen" (Untersuchungsorte), „Inneres untersuchen" (Verwahrungsorte), „Gefahr eindämmen" (Gefahrenzonen) und „Aufnehmen" (Naturorte) würfeln auf einer Tabelle **je Archetyp**. | **Problem:** Die Tabellen enthalten nur Funde der Tutorial-Orte. Eine Quelle würfelt heute den Gipfelfund, eine Ruine die Grabinschrift. Die Tabelleneinträge brauchen einen Variantenfilter. |
| **Beweis** (`evidence-…`) | Der Generator hängt dem Ort Beweiskeime an (`evidence-old-structure` usw.); der Späherbericht nimmt den ersten. | Neue Varianten bekommen einen eigenen Beweis. Der Generator setzt ihn an die erste Stelle, damit Späher etwas Ortstypisches melden. Das ändert die Prüfsummen der Vergleichswelten (gewollt, wird begründet erneuert). |
| **Folge** (`consequence-…`) | Wird nur durch Aktionen ausgelöst, die eine Folge einplanen (Grab stören, Brücke instand setzen, Siegel öffnen). | Die neuen Varianten haben bewusst keine Aktion dieser Art. Siehe Abschnitt 4. |

## 2. Technische Änderung (klein)

- Fundtabellen-Einträge bekommen das optionale Feld `requiresVariantIdsAny` (neben dem bestehenden `requiresContextTagsAny`).
  Die vier vorhandenen Tabellen werden entsprechend gefiltert (Gipfelfunde nur für `wind-carved-peak`, Moorfunde nur für `restless-marsh`,
  Grabinschrift nur für `marked-grave`, Siegelfragment für Tor und Höhle).
- Je Archetyp kommen Einträge für die neuen Varianten dazu; ein Eintrag ohne Treffer bleibt leer, wie bisher.
- `PlacementGroup`-Varianten bekommen im JSON ein optionales `evidenceSeedId`; der Generator stellt es dem Ort voran.
- Der Validator prüft: jeder Fund in einer Tabelle oder einem Pool existiert; jeder Variantenfilter nennt eine vorhandene Variante.

## 3. Funde und Beweise je Variante

Zwei Funde je Variante (erster im Pool, beide in der Tabelle der passenden Aktion) und ein Beweis. Alle `once-per-world`.
Belohnungsstufen (Wissenspunkte nach Auswertung, Dauer in Tagen):

| Art | Punkte | Tage | Bedarf Rückführung |
| --- | --- | --- | --- |
| Aufzeichnung (`record`) | 2 bis 4 | 1 bis 2 | nein |
| Probe (`sample`) | 2 bis 6 | 2 bis 4 | ja |
| Gegenstand (`object`) | 3 bis 6 | 2 bis 3 | ja |
| Dokument (`document`) | 6 bis 12 | 2 | ja |

| Variante | Fund 1 | Fund 2 | Beweis (Späherbericht, Entwurf) |
| --- | --- | --- | --- |
| `old-ruin` | Steinmetzzeichen an den Mauerresten (Aufzeichnung) | Mörtelprobe aus dem Fundament (Probe) | Die Späher fanden Mauerreste mit Spuren früherer Aufräumarbeiten. |
| `stone-circle` | Ausrichtung der Steine zu Sichtachsen (Aufzeichnung) | Abreibung eingeritzter Zeichen (Dokument) | Die Späher sahen angebrannte Stellen in der Mitte des Kreises. |
| `ruined-shrine` | Opfergabe aus dem Schrein (Gegenstand) | Weiheinschrift (Dokument) | Die Späher fanden Reste von Gaben, die nicht alt aussehen. |
| `cold-fireplace` | Ascheprobe (Probe) | Strichliste im Stein (Aufzeichnung) | Die Späher schätzen, dass das Feuer vor einigen Tagen erlosch. |
| `broken-cart` | Frachtliste im Wrack (Dokument) | Probe vom gebrochenen Achsholz (Probe) | Die Späher fanden Radspuren, die abrupt enden. |
| `old-boat-camp` | Pechprobe vom Rumpf (Probe) | Küstenskizze im Boot (Aufzeichnung) | Die Späher sahen, dass das Boot zuletzt ausgebessert wurde. |
| `sealed-cave` | Siegelstein vom Eingang (Gegenstand) | Wandzeichen im Vorraum (Aufzeichnung) | Die Späher fanden frische Kratzspuren am Verschluss. |
| `warded-threshold` | Abreibung der Bannzeichen (Dokument) | Bruchstück des Türsturzes (Gegenstand) | Die Späher meiden die Schwelle; Tiere tun es offenbar auch. |
| `blocked-pass` | Geröllprobe (Probe) | alte Wegmarken unter dem Schutt (Aufzeichnung) | Die Späher fanden Trittspuren, die vor dem Geröll umkehren. |
| `freshwater-spring` | Wasserprobe (Probe) | Aufzeichnung der Schüttung (Aufzeichnung) | Die Späher sahen Tiertrittspuren, die zur Quelle führen. |
| `fishing-grounds` | Netzrest (Gegenstand) | Fangbericht (Aufzeichnung) | Die Späher fanden Reusen und Spuren häufigen Gebrauchs. |
| `healing-herbs` | Pflanzenprobe (Probe) | Aufzeichnung zur Verwendung (Aufzeichnung) | Die Späher bemerkten abgeschnittene Stängel. |
| `timber-stand` | Jahresringprobe (Probe) | Schnittspuren, die auf Werkzeug hindeuten (Aufzeichnung) | Die Späher fanden Stümpfe und Schleifspuren. |
| `salt-deposit` | Salzkrustenprobe (Probe) | Arbeitsgerät aus dem Salz (Gegenstand) | Die Späher sahen Spuren von Abbau und Transport. |
| `berry-grove` | Beerenprobe (Probe) | Aufzeichnung der Tierspuren (Aufzeichnung) | Die Späher fanden zertretene Stellen zwischen den Sträuchern. |
| `volcanic-vent` | Schwefelprobe (Probe) | Aufzeichnung der Hitzeverteilung (Aufzeichnung) | Die Späher meldeten Rauch und einen Geruch nach Schwefel. |
| `dead-zone` | Bodenprobe (Probe) | Aufzeichnung der Grenzlinie (Aufzeichnung) | Die Späher kehrten an der Grenzlinie um; hinter ihr gibt es kein Leben. |
| `signal-tower` | Signalcode auf der Plattform (Dokument) | Aufzeichnung der Sichtlinien (Aufzeichnung) | Die Späher sahen Reste eines Feuers auf der Plattform. |
| `colossus-statue` | Inschrift am Sockel (Dokument) | Gesteinsprobe (Probe) | Die Späher meldeten Risse und frische Abplatzungen. |
| `old-lighthouse` | Linsenbruchstück (Gegenstand) | Wachbuch des Leuchtturms (Dokument) | Die Späher sahen Glassplitter vor dem Turm. |
| `border-stones` | Abreibung der Grenzzeichen (Dokument) | Aufzeichnung des Grenzverlaufs (Aufzeichnung) | Die Späher fanden gesetzte Steine in gleichen Abständen. |
| `skull-marks` | Aufzeichnung der Markierung (Aufzeichnung) | Knochenprobe (Probe) | Die Späher fanden aufgesteckte Schädel; einige sind frisch. |
| `wind-ribbons` | Aufzeichnung der Bänderfarben (Aufzeichnung) | Probe vom Stangenholz (Probe) | Die Späher sahen Bänder, die zuletzt erneuert wurden. |

Für `restless-marsh`, `wind-carved-peak`, `border-warning-sign`, `marked-grave`, `abandoned-camp`, `sealed-gate` und `broken-bridge` ändere ich nur
den Variantenfilter der Tabellen; ihre Funde bleiben.

## 4. Folgen

Eine Folge entsteht nur, wenn eine Aktion sie einplant. Die neuen Varianten haben keine Aktionen, die ihren Zustand ändern (Abschnitt 3 von
`world_generator_variant_states_proposal.md`: nur Aktionen, deren Zustandsänderungen das Profil kennt). Eine Folge ohne Auslöser wäre toter Text.
Vorschlag:

- Jetzt keine neuen Folgen. Zustandsänderungen der neuen Orte entstehen später durch Weltzüge und Ereignisse (zum Beispiel Quelle versiegt,
  Leuchtturm wird entzündet), die dann ihre eigenen Folgen mitbringen.
- Ausnahme: `sealed-cave` verwendet „Siegel öffnen"; deren Ergebnis löst bereits den Weltauslöser `location-seal-broken` aus, den
  `consequence-seal-broken` aufgreift. Hier ist nichts nachzutragen.

## 5. Fragen

1. Die Änderung der Tabellen (Variantenfilter, Abschnitt 2) und des Beweiskeims im Generator: einverstanden? Die Prüfsummen der Vergleichswelten
   ändern sich dadurch einmalig, weil die Orte einen weiteren Beweiskeim tragen.
2. Die Funde und Beweise aus Abschnitt 3: einverstanden, oder Streichungen und Änderungen? Die Texte sind wie bei den Varianten Entwürfe.
3. Keine neuen Folgen jetzt (Abschnitt 4): einverstanden?
