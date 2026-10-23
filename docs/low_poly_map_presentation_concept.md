# Konzept: Low-Poly-3D-Kartendarstellung mit Ortsbaukasten

Status: **Vorschlag mit eingearbeiteten Owner-Entscheidungen (siehe 16); keine Implementierung**

Umsetzungsplan: `low_poly_map_presentation_implementation_plan.md`

Dieses Dokument ist bewusst **offen** angelegt. Es beschreibt ein Grundgerüst und Regeln für
Erweiterungen. Der heutige Stand (Archetypen, Varianten, Modifier, Zustandskanäle) ist das
Basiskonzept der Orte; alles Weitere kommt später hinzu und darf ohne Umbau des Konzepts ergänzt
werden.

Verwandte Dokumente:

- `map_presentation_simplified_concept.md` (wird durch dieses Konzept teilweise abgelöst, siehe 2.2)
- `technical_concept.md`
- `visual_asset_tech_addendum.md`
- `ui_ux_concept.md`
- `content_workbench_editor_concept.md`
- `gameconcept/factions_and_trade_concept.md`
- `gameconcept/location_archetypes_and_interactions_concept.md`
- `gameconcept/world_locations_and_events_concept.md`
- `gameconcept/cross_system_json_authoring_schema.md`

## 1. Ziel

Die Karte soll nicht nur zeigen, **wo** etwas ist, sondern **was** dort ist und **in welchem Zustand**
– so, dass Hindernisse, Orte, Expeditionen, Fraktionen und Tiere auf einen Blick lesbar sind und
ihre Veränderung sichtbar wird.

Auslöser war ein konkretes Problem: Eine unpassierbare Brücke ließ sich umgehen, weil der Graben
darunter nicht dargestellt werden konnte. Die Darstellung muss das, was die Spiellogik sperrt, auch
sichtbar sperren.

Leitbild:

> Die Karte ist eine Bühne für den Wissensstand des Spielers: scharf, wo er sicher ist, vage, wo er
> nur gehört hat, leer, wo er nichts weiß.

## 2. Entscheidungen und Abgrenzung

### 2.1 Was dieses Konzept festlegt

- **Low-Poly-3D** mit fester Neigung, **frei drehbarer Kamera** und orthografischer Projektion
  (isometrischer Eindruck bleibt). Die Kamera bewegt sich nie von selbst; der Spieler behält die Kontrolle.
- Orte, Figuren und Tiere werden aus einem **offenen Baukasten** von Teilen zusammengesetzt, nicht
  als Einzelmodell je Ort.
- Das Aussehen eines Ortes ist eine **Funktion** aus Daten (Archetyp, Variante, Zustandskanäle,
  Modifier, Wissensgrad), nicht eine von Hand gesetzte Szene.
- Die Optik von Tageszeit, Wetter und Licht ist **reine Stimmung** und beeinflusst keine
  Spielentscheidung.
- Hindernisse (Graben, Schlucht, Fluss, Mauer) sind **Linien- oder Flächenobjekte** mit Zustand und
  Durchlässen, nicht Feldinhalte.

### 2.2 Was sich gegenüber dem bisherigen Kartenkonzept ändert

| Bisher (`map_presentation_simplified_concept.md`) | Dieses Konzept | Folge |
| --- | --- | --- |
| flache isometrische 2D-Sprites | Low-Poly-3D, drehbare Kamera | vom Owner entschieden |
| keine sichtbare Terrainhöhe | sichtbares Relief **nur optisch** (Gräben, Schluchten, Hügel, Berge) | Höhe bleibt außerhalb der Logik |
| Hexes unsichtbar | unverändert unsichtbar | keine Änderung |
| Hindernisse als Objekte oder Kanten-Visuals | Hindernisse als Linien/Flächen mit Zustand | neue Darstellungsart |
| `LocationKind` steuert die Darstellung | Archetyp, Variante, Modifier steuern sie | Visual-Resolver nötig |
| Gebietsgrenzen als Linie (`TerritoryBorder`) | Gebiete werden nicht als farbige Linien oder Flächen gezeigt, Fraktionen erkennt man an Zeichen (`factions_and_trade_concept.md` 13.1) | vorhandene Grenzlinien nur Debug oder wissensabhängig |

`AGENTS.md` und das bisherige Kartenkonzept müssen mit der Umsetzung entsprechend angepasst werden (Aufgabe in Stufe V0 des Umsetzungsplans).

### 2.3 Feste Grenzen

- Keine Spielregel steckt in Szene, Prefab, `MonoBehaviour` oder Shader.
- Die Darstellung liest Zustände, sie verändert keine.
- Die Darstellung zeigt nur, was der `KnowledgeState` erlaubt (siehe 11).
- Kein Ersatz für Szenentexte, Porträts und Ereignisbilder; die Karte ergänzt sie.
- Kein Terrainhöhen-System in `Game.Core`, keine Sichtlinien- oder Deckungsregeln aus Relief.
- Kein Gefecht auf der Karte (siehe 8.4).

## 3. Leitprinzipien

1. **Daten vor Prefabs.** Zuordnung von Ort zu Teilen, Zuständen und Posen liegt in JSON.
2. **Offen für Neues.** Neue Teile, Varianten, Archetypen, Figuren, Tiere und Modifier lassen sich
   ohne Code ergänzen, solange sie bestehende Teile oder Platzhalter nutzen.
3. **Platzhalter zuerst.** Jedes Teil hat einen Fallback. Fehlende Assets machen nichts kaputt.
4. **Zustand ist sichtbar.** Jede sinnvolle Zustandsänderung hat eine erkennbare Form.
5. **Wissen ist sichtbar.** Der Wissensgrad ist immer im Bild erkennbar, nicht nur im Tooltip.
6. **Lesbarkeit vor Detail.** Silhouette, Farbe und Größe zählen mehr als Feinheiten.
7. **Wiederverwenden.** Neue Orte entstehen vorrangig durch Kombination, nicht durch neue Modelle.

## 4. Kamera, Look und Zoom

### 4.1 Kamera

- Orthografisch, feste Neigung, Drehung um die Kartenmitte oder den Fokus.
- Zoomstufen sind Detailstufen (siehe 4.3), nicht nur Vergrößerung.
- Die Kamera bewegt sich **nie automatisch** auf Ereignisse. Folgen und Fokussieren geschieht nur
  auf Wunsch des Spielers (Auswahl, Taste, optionaler Folgemodus, den er ein- und ausschaltet).
  Wer ein Ereignis übersieht, übersieht es.
- Die Drehung wird **nicht gespeichert**; nach dem Laden startet die Kamera in Standardausrichtung.
- Die Drehung ist eine reine Ansichtsfunktion; Interaktion (Auswahl, Ziel) nutzt weiterhin die
  logische Hex-Koordinate.

### 4.2 Look

- Flat-shaded Dreiecke mit Farbe pro Fläche; wenige Materialien; keine Texturen im Standardfall.
- Gelände aus einem Mesh mit abgesenkten Bereichen für Gräben, Flussbetten und Schluchten sowie
  erhöhten Bereichen für Hügel und Berge; Wände in Erd- oder Felsfarbe. Das Relief ist reine
  Darstellung, die Hexe bleiben logisch eben.
- Weiche Farbübergänge zwischen Biomen statt Hexgrenzen.
- Schatten sind erwünscht, weil sie Tiefe und Lesbarkeit liefern.
- Tageszeit, Nebel und Wetter sind Stimmung; Nachts sind Lichter und Feuer sichtbar.

### 4.3 Detailstufen durch Zoom

| Stufe | Zweck | Sichtbar |
| --- | --- | --- |
| Übersicht | Planung, Reichweite, Gebiete | Gelände, Linien, Silhouetten großer Objekte, Ortsmarker, Expeditionen als Gruppe |
| Gebiet | Orientierung, Wege, Risiken | zusätzlich Gebäude, Zäune, Wälder, Spuren, Figurengruppen |
| Ort | Begleiten und Erkunden | alle Teile, Zustandsdetails, Figuren einzeln, Tiere, Effekte |

Details erscheinen beim Heranzoomen (LOD). Zoom allein ändert nicht die **Größenverhältnisse** (siehe 10).

## 5. Schichtenmodell der Karte

| Schicht | Inhalt | Quelle |
| --- | --- | --- |
| 1 Gelände | Boden, Relief, Biome | Weltgenerator, `WorldState` |
| 2 Linien und Flächen | Fluss, Schlucht/Graben, Straße, Mauer, Sumpf, Gefahrenfläche | `WorldPathState` und Flächenanker |
| 3 Orte | Bauteile, Kleinteile, Natur an Ankern | Ortsdaten, Visual-Resolver |
| 4 Lebewesen | Expeditionen, Figuren, Tiere | Expeditions- und Ereignisdaten |
| 5 Effekte | Rauch, Feuer, Staub, Nebel, Licht | Zustände und Modifier |
| 6 Wissens-Overlay | Wissensgrad, Alter, Quelle | `KnowledgeState` |
| 7 Marker | Archetyp-Ring, Modifier-Symbole, Auswahl, Reichweite | Visual-Resolver, UI |

Die Schichten sind getrennt austauschbar. Eine Schicht darf fehlen, ohne die anderen zu stören.

## 6. Der Baukasten

### 6.1 Kategorien

Die Kategorien sind **offen**: Neue Kategorien dürfen ergänzt werden, ohne bestehende zu ändern.

| Kategorie | Beispiele | Besonderheit |
| --- | --- | --- |
| Gelände-Feature | Graben/Schlucht, Fluss/Furt, Sumpf, Erdrutsch, Kraterrand, verbrannte Fläche | Linien oder Flächen mit Zustand |
| Bauteil | Brücke/Steg, Zaun/Mauer, Tor, Turm, Haus, Zelt, Eingang, Steinplatte, Ruinenwand, Altar, Statue, Boot | mehrere Formen: intakt, beschädigt, zerstört |
| Kleinteil | Feuerstelle, Kiste, Rucksack, Karren, Fackel, Banner, Schild, Grabstein, Knochen, Gaben, Werkzeug | klein, erzählend |
| Natur | Bäume, Büsche, Kräuter, Beeren, Schilf, Pilze, Felsen, Salz, Holz | Cluster und Streuung |
| Effekt | Rauch, Feuer, Staub, Nebel, Glühen, Funken, Wasser, Entsättigung | Partikel/Shader ohne Modell |
| Figur | Expeditionsmitglieder, Fraktionsangehörige, Kontaktpersonen | siehe 8 |
| Tier | Herden, Raubtiere, Kreaturen | siehe 8 |

### 6.2 Eigenschaften eines Teils

Jedes Teil hat eine stabile ID und beschreibt sich selbst:

- **Kategorie** und **Größenklasse** (siehe 10),
- **Formen/Posen** (z. B. `intact`, `damaged`, `destroyed`, `repaired`),
- **erlaubte Ankerarten** (Punkt, Fläche, Kante, Pfad),
- **Tags** für Suche und Generator (z. B. `wood`, `stone`, `water`),
- **Fallback-Teil** und **Platzhalterform**,
- **Wissensverhalten** (wie es bei `reported` und `unknown` erscheint).

### 6.3 Formen statt Zustände im Teil

Ein Teil kennt **Formen**, nicht Spielzustände. Welche Form bei welchem Zustand gilt, steht in der
Komposition (siehe 7). So kann dasselbe Teil in vielen Orten verschieden eingesetzt werden.

## 7. Komposition: vom Ort zum Bild

### 7.1 Ablauf

```text
Ort (Anker, Archetyp, Variante, Modifier, Zustandskanäle, Wissen)
  -> Visual-Resolver in Game.App wählt Kompositionen
  -> liefert eine Darstellungsbeschreibung (Teile, Formen, Posen, Effekte, Marker)
  -> Unity baut und animiert sie
```

Der Resolver liegt in `Game.App`, ist testbar und kennt keine Unity-Typen. Unity bekommt nur die
Beschreibung. Das gleiche Muster nutzen bereits Szenentexte und Visual-Assets.

### 7.2 Auswahl und Rangfolge

Kompositionen werden nach **Spezifität** gewählt; die spezifischste passende gewinnt:

1. Variante + Zustand (z. B. `broken-bridge` im Zustand `repaired`),
2. Variante,
3. Archetyp-Vorlage,
4. generischer Platzhalter.

Modifier-Kompositionen sind **Zusatzschichten**: Sie fügen Teile hinzu, tauschen Formen oder
ergänzen Effekte, ersetzen aber nicht die Grundkomposition. Mehrere Modifier werden nach Priorität
geschichtet; Konflikte lösen die bestehenden `incompatibleModifierIds`.

### 7.3 Anker bestimmen den Darstellungsmodus

| Anker | Darstellung |
| --- | --- |
| Punkt | Gruppe von Teilen auf einem Feld |
| Fläche | Boden-Decal oder Shader plus gestreute Teile; Rand weich |
| Kante | Bauteil quer über der Feldgrenze, passend zu einem Gelände-Feature |
| Pfad | Linie plus wiederholte Zeichen in Abständen (Grenzmarken, Gräber am Weg) |

Kantenobjekte sind der Schlüssel für Hindernisse: Der Graben ist eine durchgehende Linie, die
Brücke ein Durchlass in ihr. Ist der Durchlass gesperrt, ist die Lücke sichtbar.

### 7.4 Archetyp-Vorlagen

Jeder Archetyp hat eine Kompositionsregel, damit Orte desselben Typs ähnlich lesbar sind. Die
Vorlage ist ein Standard, den Varianten überschreiben dürfen:

| Archetyp | Regel | Lesbarkeit |
| --- | --- | --- |
| `trace-site` | Kleinteile am Boden, kein Bauwerk | klein, verstreut |
| `investigation-site` | ein Eingang oder Mal plus Marker | zentrale Struktur |
| `route-obstacle` | Gelände-Feature quer zum Weg plus Durchlass-Bauteil | Linie mit Lücke |
| `containment-site` | Eingang plus Verschluss | geschlossene Silhouette |
| `territorial-marker` | Zeichen wiederholt entlang Kante oder Pfad | Reihe aus Markern |
| `contact-site` | Siedlung: Häuser, Zaun, Tor, Rauch, Figuren | größte Gruppe |
| `resource-site` | Naturcluster plus kleines Markerteil | weich, einladend |
| `hazard-site` | Fläche mit Effekt, wenige Teile | großflächig, farbig |
| `landmark-site` | ein großes Einzelobjekt | hohe Silhouette |
| `dynamic-situation` | Figuren, Tiere, Teile, Effekt; zeitlich begrenzt | bewegt |

Neue Archetypen erhalten eine Vorlage oder nutzen die generische Vorlage.

### 7.5 Zustände

Die Zustandskanäle der Ortsdaten (`interaction`, `operational`, `presence`) sind die Quelle. Die
Kompositionen verweisen auf **bestehende State-IDs** aus den Zustandsprofilen. Beispiel
`state-route-crossing`: Betrieb `blocked`, `provisional`, `risky-passage`, `open`, `repaired`,
`destroyed`.

Anzeigenamen wie „intakt“ oder „beschädigt“ in Entwürfen sind Beschriftung; maßgeblich sind die IDs.
Nicht jedes Teil reagiert auf jeden Kanal. Eine Zustandskombination ohne eigene Regel fällt auf die
Grundform zurück.

### 7.5a Zustandskanäle kombinieren

Ein Ort hat drei unabhängige Kanäle. Die Darstellung wird **pro Kanal** beschrieben, nicht pro
Kombination. Bilder entstehen durch Überlagerung:

| Kanal | Bestimmt | Beispiele |
| --- | --- | --- |
| `operational` | **Grundform** der Bauteile | Brücke intakt/beschädigt/zerstört, Tor offen/versiegelt |
| `presence` | **Lebenszeichen** | Figuren, Feuer, Licht, Rauch, Überwuchs, Fahnen |
| `interaction` | **Spuren der Erkundung** | Markierungen der Expedition, geöffnete Stellen, Fundflaggen |

Regeln:

- Jeder Kanal wählt unabhängig Formen und Zusatzteile. Kombinationen entstehen automatisch.
- Wollen zwei Kanäle denselben Platz (Slot) belegen, gilt der Vorrang
  `operational` vor `presence` vor `interaction`.
- Ein Kanal im Zustand `unknown` oder ohne Regel trägt nichts bei.
- Kombinationen, die zusammen unplausibel aussehen (zum Beispiel `destroyed` mit `occupied`), meldet
  der Validator als Hinweis; die Spiellogik entscheidet, ob sie vorkommen dürfen.
- Nur Kombinationen, die ein eigenes Bild brauchen (selten), erhalten eine eigene Komposition mit
  höherer Spezifität (7.2).

### 7.6 Übergänge

Zustandswechsel werden **übergeblendet**, nicht getauscht:

- Teile wachsen ein oder aus, verschieben oder neigen sich weich.
- Ereignishafte Wechsel (Einsturz, Brand, Reparatur) erhalten einen kurzen Effekt (Staub, Funken,
  Rauch).
- Ein Übergang wird nur gezeigt, wenn der Spieler den Wechsel **wissen darf** (siehe 11). Sonst
  bleibt das alte Bild bis zu einem Bericht oder einer Beobachtung.

## 8. Figuren und Tiere

### 8.1 Figuren

- **Gruppen statt Einzelpersonen.** Eine Expedition erscheint als eine Gruppe von wenigen Figuren,
  nicht als jedes Mitglied einzeln. Wer dabei ist, zeigt die UI bei der Auswahl der Gruppe.
- Modularer Aufbau: Körper, Kleidung/Ausrüstung, Rollenprop, Fraktionszeichen.
- Die Gruppe zeigt ihren Gesamtzustand (Last, Verletzte, Erschöpfung) durch Haltung, Tempo und
  Kleinteile, nicht pro Person.
- **Fraktionsangehörige** (Patrouillen, Posten, Dörfer) erscheinen ebenfalls als Gruppen. Sie werden
  an Silhouette, Ausrüstung und dem Fraktionszeichen erkannt. Die Farbe kommt aus den
  Fraktionsdaten (`Faction.Color`, beim Erstellen der Fraktion festgelegt), das Zeichen aus den
  Signaturen der Fraktion (`signatures.json`).
- Fraktionsfarbe und -zeichen erscheinen **nur**, wenn die Fraktion dem Spieler bekannt ist. Sonst
  bleibt die Darstellung neutral (kein Banner, keine Farbe, keine verräterische Ausrüstung).
- Gebiete werden nicht als farbige Flächen oder Linien gezeigt. Die Karte vermittelt Gebiete über
  wiederholte Zeichen, Patrouillen und Wegmarken, wie in `factions_and_trade_concept.md` 13.1.
- Verbündet, neutral oder feindlich ergibt sich aus dem bekannten Verhältnis, nicht aus der
  Welt-Wahrheit.

### 8.2 Tiere und Kreaturen

- Artenkatalog mit Größenklasse, Verhalten (Herde, Einzelgänger, Raubtier) und Lebensraum-Tags.
- Der Start umfasst **Raubtiere in wenigen Varianten** (zum Beispiel Rudeljäger, Einzeljäger,
  Sumpf- oder Dschungeljäger) und bei Bedarf Herdentiere für Ressourcenorte. Welche Arten wo
  vorkommen, ergibt sich mit den Orten und Biomen und wird später ergänzt.
- Der Katalog ist offen; neue Arten nutzen vorhandene Skelett- und Animationsmuster.
- Unbekannte Kreaturen erscheinen als Spuren, Schatten oder Zeichen, bis sie bestätigt sind.

### 8.3 Animation

Bewusst klein gehalten: `idle`, `walk`, `alert`, `act` (Arbeiten, Fressen, Angriff), `down`.
Weitere Posen sind Erweiterungen. Bewegung zwischen Feldern folgt dem Pfad der Logik; die Animation
läuft der Logik hinterher, nie voraus.

### 8.4 Dynamische Situationen

Beispiel: Die Expedition wird im Dschungel von Tieren angegriffen.

- Dynamische Situationen erscheinen **als Bild und Bericht**, wie bisher (Ereignisbild, Szenentext,
  Archiv). Es gibt keine Live-Inszenierung auf der Karte.
- Die Logik löst das Ereignis auf (Risiko, Ergebnis, Folgen); die Karte zeigt höchstens die
  **Spur** danach: Markierung am Ort, Fußabdrücke, Rauch, zurückgelassene Teile.
- Tiere und Figuren auf der Karte sind Teil des Alltagsbildes (Herden, Patrouillen, Gruppen), nicht
  eines Gefechts.
- Was der Spieler sieht, richtet sich nach Beobachtung und Bericht. Ein unbeobachtetes Ereignis
  erscheint erst als Bericht.
- Eine spätere Live-Inszenierung wäre eine eigene, vom Owner freizugebende Erweiterung.

## 9. Farbcode

### 9.1 Entscheidung

Der Typ eines Ortes wird über einen **Markerring am Boden** gezeigt, nicht über die Farbe des
Objekts. Der Ring hat eine feste Farbe pro Archetyp und ein kleines Symbol. Die Objekte selbst
behalten natürliche Farben.

Gründe:

- Der Typ ist auch bei vielen Orten schnell erkennbar.
- Die Objektfarben bleiben frei für Zustand (Ruß, Moos, Rost) und Wissen (Sättigung).
- Fraktionsfarben (Banner, Gebiet) und Archetypfarben stören sich nicht, weil sie auf
  verschiedenen Trägern liegen.
- Farbe wird nie allein verwendet: Das Symbol macht den Code auch für Farbenblinde lesbar.

### 9.2 Vorschlag der Palette

Die Farbwerte stammen aus einer farbenblind-freundlichen Palette (Okabe-Ito) und liegen in JSON.

| Archetyp | Farbe | Symbol (Vorschlag) |
| --- | --- | --- |
| Spurenort | Sand | Fußspur |
| Untersuchungsort | Blau | Lupe |
| Wegehindernis | Orange | Bruchlinie |
| Verwahrungsort | Rotviolett | Schloss |
| Territoriales Zeichen | Gelb | Warndreieck |
| Kontaktort | Blaugrün | Handschlag |
| Ressourcenort | Himmelblau | Tropfen/Korb |
| Gefahrenzone | Zinnoberrot | Ausrufezeichen |
| Landmarke | Elfenbein | Stern |
| Dynamische Situation | Violett, pulsierend | Blitz |

### 9.3 Regeln

- Der Ring erscheint **nur für bekannte Orte**. Ist der Typ nicht bekannt, zeigt ein neutraler grauer
  Ring ein Fragezeichen. Der Ring darf den Typ nie früher verraten als der `KnowledgeState`.
- Modifier erscheinen als bis zu drei kleine Symbole am Ring (z. B. besetzt, versiegelt, brennend).
- Der Wissensgrad ändert Sättigung und Kantenschärfe des Rings genauso wie die der Objekte.
- Fraktionsfarben kommen aus den Fraktionsdaten (beim Erstellen der Fraktion festgelegt) und liegen
  auf anderen Trägern (Kleidung, Banner) als die Archetypfarben. Der Validator warnt, wenn eine
  Fraktionsfarbe einer Archetypfarbe zu nahe kommt.
- Fraktionsfarben erscheinen nur bei bekannten Fraktionen (siehe 8.1).

## 10. Maßstab und Zoom

Maßstab und Zoom sind zwei verschiedene Dinge:

- **Maßstab** ist die Größenklasse eines Teils (autorisiert, fest). Ein Turm bleibt größer als ein
  Zelt, egal wie weit man zoomt.
- **Zoom** ändert Ausschnitt und Detailstufe, nicht die Größenverhältnisse.

Größenklassen:

| Klasse | Beispiele | Sichtbar ab |
| --- | --- | --- |
| XS | Kleinteile, Spuren | Ort |
| S | Zelt, Hütte, Tiere, Figuren | Gebiet |
| M | Haus, Brücke, Eingang | Gebiet |
| L | Turm, Statue, Siedlungskern | Übersicht (vereinfacht) |
| XL | Gipfel, Mauer, Landmarke | Übersicht (deutlich) |

Wichtige große Objekte erhalten eine **Fernwirkung**: eine klare Silhouette und gegebenenfalls ein
Leuchtfeuer oder Glühen, damit Landmarken auch in der Übersicht erkennbar sind. Der Markerring
bleibt unabhängig vom Zoom in gleicher Bildschirmgröße lesbar.

Zusätzlich gibt es einen **Objektzoom**: Die Auswahl eines Ortes oder einer Expedition fährt die
Kamera an das Objekt, wie im Schaufenster-Entwurf. Das ist eine Kamerabewegung und ändert keine
Größen.

## 11. Informationsehrlichkeit

Die Darstellung zeigt nie mehr, als der `KnowledgeState` erlaubt.

| Wissensgrad | Darstellung |
| --- | --- |
| bestätigt | scharf, volle Farbe, Details |
| berichtet | entsättigt, leicht durchscheinend |
| alt | verblasst; zeigt den **letzten bekannten** Stand |
| unbekannt | leere Fläche oder neutraler Platzhalter, keine Details |

Besondere Regeln:

- Verborgene Zustände (Fraktionsbesitz, Hinterhalt, Fallen) erscheinen nicht, bis sie bekannt sind.
- Der Zustand eines Ortes wird bei Alt-Wissen **nicht** automatisch aktualisiert; die Karte zeigt, was
  man zuletzt wusste.
- Unbekanntes Gelände wird nicht nur ausgeblendet, sondern auch **flach** dargestellt, damit das
  Relief (zum Beispiel ein Graben) nichts verrät.
- Spuren und Zeichen dürfen auf Unbekanntes hinweisen, ohne es zu benennen.
- **Alter und Sicherheit** einer Information zeigt die Ansicht nicht an. Sie ergeben sich aus den
  Notizen, Berichten und dem Archiv (wie lange her, wie verlässlich, von wem). Die Ansicht
  unterscheidet nur die vier Wissensgrade.

## 12. Datenmodell (Skizze)

Neue Dokumente, die im `game-data-manifest.json` eingetragen werden. Die Feldnamen sind ein
Vorschlag und werden mit den Schemaregeln des Projekts abgeglichen.

**Entscheidung:** Zunächst ein gemeinsames Dokument `Visuals/visual-definitions.json` (documentType
`visual-definitions`) mit den Abschnitten `markers` (Archetypfarbe und Ringlücken), `parts`
(Teile aus Grundformen) und `compositions` (Elemente je Archetyp oder Variante). Es lässt sich
später aufteilen, zum Beispiel um Figuren- und Tierkataloge. Das Dokument ist im Katalog optional;
ohne Dokument fällt jeder Ort auf einen schlichten Marker zurück.

Aufbau eines Elements: Teil, Position, Drehung, optional `shows` (Zustandskanal und Zustands-IDs, in
denen es sichtbar ist) und `poses` (Versatz, Drehung, Skalierung je Zustand). Statt eines Teils kann ein
Element ein Licht (`kind: light`, Farbe, Stärke, Reichweite) oder Rauch (`kind: smoke`, Farbe, Größe) sein.
Eine Komposition kann mit `scale` verkleinert werden und mit `plain` Zustände nennen, die bewusst kein
eigenes Bild haben (zum Beispiel `interaction: untouched`); sie zählen dann als abgedeckt. Zustands-IDs sind die
der Zustandsprofile; der Sonderwert `unknown` gilt, wenn der Spieler den Zustand nicht kennt. Die
Validierung prüft Archetyp, Variante, Teile und Zustands-IDs und verlangt, dass eine Komposition
jeden Wert eines Kanals abdeckt, auf den sie reagiert.

Beispiel (gekürzt):

```json
{
  "compositionId": "vis-broken-bridge",
  "match": { "archetypeId": "route-obstacle", "variantId": "broken-bridge" },
  "parts": [
    { "partId": "terrain-ditch", "anchorMode": "edge" },
    { "partId": "bridge-deck",
      "shapeBy": { "channel": "operational", "map": {
        "blocked": "damaged", "risky-passage": "damaged", "provisional": "repaired",
        "open": "intact", "repaired": "repaired", "destroyed": "destroyed" } } },
    { "partId": "sign-warning",
      "shownWhen": { "channel": "operational", "in": ["blocked", "risky-passage", "destroyed"] } }
  ]
}
```

Auflösungsregeln:

- Fehlt eine Komposition, gilt die Archetyp-Vorlage; fehlt auch sie, der generische Platzhalter.
- Fehlt ein Teil, gilt dessen Fallback-Teil, zuletzt ein neutraler Block mit Beschriftung.
- Unbekannte Felder in neueren Dokumenten werden ignoriert und gemeldet, nicht verworfen.
- IDs bleiben stabil und sind klein geschrieben mit Bindestrich; Umbenennen ist eine bewusste
  Migration.

### 12.1 Neuen Ort hinzufügen – Prüfliste

1. Archetyp und Variante in den Ortsdaten anlegen (wie bisher).
2. Vorhandene Teile wählen; fehlen Teile, neue Teile mit Platzhalter anlegen.
3. Komposition schreiben: Teile, Formen je Zustand, Modifier-Verhalten.
4. Ring/Symbol prüfen; Wissensgrade im Schaufenster ansehen.
5. Validator ausführen (fehlende Teile, unbekannte State-IDs, verbotene Verweise).

Das Content-Workbench-Konzept wäre der natürliche Ort für diesen Ablauf.

## 13. Umsetzung in Unity (Richtung)

- **`Game.Core`**: kein Bezug zu Darstellung. Eine kleine Ergänzung: `WorldPathKind` erhält am Ende
  den Wert `Chasm` (Schlucht oder Graben). Der Zustand der Brücke bleibt ausschließlich am
  Kantenort (`operational`); der Pfad trägt keinen eigenen Zustand. Pfade werden nicht im
  Laufzeit-Snapshot gespeichert, sondern mit der Weltgenerierung bereitgestellt; daher ändert sich
  das Save-Format nicht. Siehe 13.2.
- **`Game.App`**: Visual-Resolver, Wissens- und Zustandsabfrage, Darstellungsbeschreibung.
- **Unity**:
  - Teile-Bibliothek (Prefabs mit Platzhalterformen), Pooling,
  - Geländebau (Mesh, abgesenkte Bereiche, Linien als Spline/Mesh),
  - Übergangs- und Posen-Animator (Datenpose zu Transform),
  - Wissens-Shader (Sättigung, Transparenz, Flachheit),
  - Kamera (Orbit, Zoomstufen, Fokus).
- Die Datei `UnityHexMapView.cs` (über 4500 Zeilen) sollte nicht weiter wachsen; neue Darstellung
  entsteht in eigenen Bausteinen.
- Prefabs enthalten Geometrie und Materialien, keine Regeln.

### 13.1 Assets

- Die ersten Teile werden **erzeugt**: aus einfachen Grundformen per Code oder Werkzeug (so wie im
  Schaufenster-Entwurf) und als Prefabs abgelegt. Sie sind Platzhalter mit gutem Aussehen.
- Jedes Teil hat eine stabile ID. Austausch oder Erweiterung durch bessere Modelle ändert Daten
  und Logik nicht.
- Eine Qualitätsstufe pro Teil (`placeholder`, `generated`, `final`) zeigt, was noch fehlt.

### 13.2 Gräben und Schluchten in den Weltdaten

Entscheidung:

- Neue Pfadart `WorldPathKind.Chasm` für durchgehende Gräben und Schluchten.
- Flüsse bleiben `River`. Eine Brücke über einen Fluss liegt auf einem `River`-Pfad, eine über
  eine Schlucht auf einem `Chasm`-Pfad.
- Die Sperre bleibt, wo sie ist: am Kantenort (`LocationAnchorKind.Edge`) und seinem
  `operational`-Zustand. Der Pfad ist nur die sichtbare Linie. So gibt es eine einzige Quelle der
  Wahrheit.
- Eine Validierung stellt sicher, dass jeder sperrende Kantenort eines Wegehindernisses auf einem
  `River`- oder `Chasm`-Pfad liegt, damit Karte und Logik übereinstimmen.
- Der Weltgenerator erzeugt den `Chasm`-Pfad zusammen mit dem Hindernis (heute: „Verschütteter
  Pass“ und Höhensprünge); die Tutorialwelt stellt `river-gray` entsprechend um.
- Der Pfad selbst wird nicht im Laufzeit-Snapshot gespeichert, daher entsteht kein Migrationsbedarf.

## 14. Lieferstufen

| Stufe | Ergebnis | Nicht enthalten |
| --- | --- | --- |
| V0 | Stilprototyp in einer Unity-Testszene: ein Ausschnitt mit Graben, Brücke, Lager, Expedition in drei Wissensgraden | Anbindung an Spielzustand |
| V1 | Gelände mit Relief und Linienobjekten (Graben, Fluss, Straße) mit Zustand; Brücken-Durchstich | weitere Orte |
| V2 | Visual-Resolver, Teile- und Kompositionsdaten, Tutorial-Orte (6) | Figuren, Tiere |
| V3 | Generator-Orte (Ruine, Höhle, Mine, Siedlung, Turm, Ressourcen, Sumpf, Vulkan, Landmarken) und Modifier-Schichten | Figuren, Tiere |
| V4 | Figurengruppen, Fraktionsdarstellung (Farbe und Zeichen nur bei bekannten Fraktionen), Expeditionsgruppen | Tiere |
| V5 | Tiere (Raubtiere, Herden) und Spuren von Ereignissen auf der Karte | Live-Inszenierung von Ereignissen (nicht geplant) |
| V6 | Workbench-Anbindung: Teile und Kompositionen bearbeiten, Vorschau | – |

Jede Stufe ist einzeln nutzbar; der Rest der Darstellung bleibt davon unberührt.

## 15. Risiken

| Risiko | Gegenmaßnahme |
| --- | --- |
| Asset-Aufwand wächst durch Varianten | Baukasten, Platzhalter, Priorisierung nach Spielrelevanz |
| Zu viel Detail, schlechte Lesbarkeit | Silhouette zuerst, LOD, Größenklassen |
| Darstellung verrät Weltwahrheit | Wissensregeln im Resolver, Tests mit Wissensständen |
| Darstellung driftet von der Logik ab | Zustände nur aus Logik, Tests der Resolver-Ausgabe |
| Große Unity-Datei wächst weiter | eigene Bausteine, Resolver in `Game.App` |
| Leistung (viele Teile, Schatten) | Pooling, LOD, Teile nur im Sichtbereich |
| Stil-Wechsel kostet Zeit | V0 als Stilprobe vor allem anderen |

## 16. Entscheidungen und offene Fragen

### 16.1 Vom Owner entschieden

| Thema | Entscheidung |
| --- | --- |
| Stilwechsel | Low-Poly-3D mit drehbarer Kamera ist beschlossen |
| Relief | optisch auch für Hügel und Berge, nicht nur Gräben |
| Kamera | bewegt sich nie von selbst; Fokus und Folgen nur auf Wunsch |
| Kameradrehung | wird nicht gespeichert |
| Fraktionen | die drei MVP-Fraktionen aus `world_locations_and_events_concept.md` 11.3.5 |
| Tiere | zunächst Raubtiere in wenigen Varianten; weitere folgen mit den Orten |
| Gräben in den Weltdaten | beste Passung: `WorldPathKind.Chasm` (13.2) |
| Umfang | alles wird umgesetzt, in den Stufen V0 bis V6 |
| Zustandskanäle | Kombination durch Überlagerung pro Kanal (7.5a) |
| Alter und Sicherheit | stehen in den Notizen, nicht in der Ansicht |
| fehlende Orte | werden ergänzt (Anhang A.2) |
| Höhle | Variante des Untersuchungsorts |
| Fraktionsfarben | kommen aus den Fraktionsdaten, bei der Erstellung festgelegt |
| Figuren | Gruppen statt Einzelpersonen |
| Dynamische Situationen | nur als Bild und Bericht |
| Assets | zunächst erzeugt, später austauschbar |

### 16.2 Noch offen

1. Welche Raubtier-Varianten genau, und für welche Biome? (mit den Orten festlegen)
2. Wie viele Figuren zeigt eine Gruppe je nach Größe (zum Beispiel 3 bis 6)?
3. Wie sehen Fraktionsgruppen konkret aus, bevor ihr Zeichen bekannt ist?
4. Welche Mine- und Siedlungsvarianten sind als Daten gewünscht (Anhang A.2)?
5. Wie viel Detail ist im Tutorialumfang (V2) nötig, bevor die Stilprobe (V0) bestätigt ist?
6. Wie wird `TerritoryBorder` in Unity künftig behandelt (Debug oder entfernt)?

## 17. Nicht-Ziele

- Keine echte 3D-Terrainlogik, keine Höhenregeln.
- Keine Kampf- oder Echtzeitsimulation auf der Karte.
- Keine finalen Kunstassets, kein Audio-Polish vor dem Kernspiel.
- Keine Darstellung, die Entscheidungen beeinflusst (Wetter, Tageszeit).
- Kein Ersatz für Porträts, Szenenbilder und Szenentexte.

## Anhang A: Ausgangsstand der Orte (Basiskonzept)

Die folgende Zuordnung zeigt, wie der heutige Stand durch den Baukasten abgedeckt wäre. Sie ist ein
Startpunkt, kein Abschluss.

### A.1 Tutorial-Orte

| Ort | Archetyp | Teile (Beispiel) |
| --- | --- | --- |
| Uferzeichen | `contact-site` | Steg, Boot, Pfosten, Figur, Rauch |
| Zerstörte Brücke | `route-obstacle` | Graben, Brückendeck, Pfosten, Warnschild |
| Markiertes Grab | `investigation-site` | Hügel, Steinplatte, Grabsteine, Siegel |
| Verlassenes Lager | `investigation-site` | Zelte, Feuerstelle, Kisten, Fahne |
| Grenzwarnung | `territorial-marker` | Pfosten, Warnpfeil, Bänder |
| Versiegeltes Tor | `containment-site` | Torbogen, Mauerwerk, Siegel, Ketten |

### A.2 Weitere Varianten im Katalog

| Variante | Archetyp | Teile (Beispiel) |
| --- | --- | --- |
| Unruhiges Moor | `hazard-site` | Sumpffläche, Schilf, Nebel, Blasen |
| Windgeformter Gipfel | `natural-phenomenon` | Fels/Gipfel, Bänder, Wind-Effekt |

**Zu ergänzende Varianten** (Owner-Entscheidung, Ortsdaten noch anzulegen):

| Variante | Archetyp | Teile (Beispiel) |
| --- | --- | --- |
| Höhle | `investigation-site` | Felsmasse, Eingang (offen/blockiert/eingestürzt), Feuerstelle, Geröll |
| Mine | `resource-site` | Stollen mit Balken, Loren, Schutthalde, Fackeln, Eingang (offen/versiegelt/eingestürzt) |
| Siedlung | `contact-site` | Häuser, Mauer, Turm, Tor, Markt, Rauch, Figurengruppen (größer als das Dorf) |
| Dorf | `contact-site` | Häuser, Zaun, Tor, Rauch, Figurengruppen |

Die Ortsarten werden in den Ortsdaten angelegt; Teile und Kompositionen entstehen dazu im Baukasten.

### A.3 Vom Generator vergebene Ortsnamen

| Gruppe | Beispiele | Teile (Beispiel) |
| --- | --- | --- |
| Untersuchungsorte | Alte Ruine, Vergessenes Grab, Steinkreis, Verfallener Schrein | Ruinenwand, Hügel, Statue/Monolith, Altar |
| Verwahrungsorte | Versiegeltes Tor, Verschlossene Höhle, Gebannte Schwelle | Tor, Eingang, Siegel |
| Spurenorte | Verlassenes Lager, Kalte Feuerstelle, Zerbrochener Karren, Altes Bootslager | Zelt, Feuerstelle, Karren, Boot |
| Ressourcenorte | Quelle, Fischgrund, Heilkräuter, Holzbestand, Salzvorkommen, Beerenhain | Wasser, Natur, Salz, Holz |
| Gefahrenzonen | Giftiger Sumpf, Vulkanschlund, Tote Zone | Sumpf, Krater, verbrannte Fläche, Nebel |
| Landmarken | Signalturm, Gipfelmal, Kolossstatue, Alter Leuchtturm | Turm, Fels, Statue |
| Grenzzeichen | Grenzsteine, Warnzeichen, Schädelmarken, Bänder im Wind | Pfosten, Knochen, Bänder (Pfad) |
| Wegehindernisse | Zerstörte Brücke, Verschütteter Pass | Graben, Brücke, Geröll |

### A.4 Geplante Beispielvarianten

Das Archetypen-Konzept nennt je Archetyp rund zehn weitere Beispielvarianten (etwa Quarantäneeingang,
Händlerkarawane, Kraterrand, Erinnerungsfluss). Sie werden durch Kombination vorhandener Teile
abgedeckt; nur neue Silhouetten brauchen neue Teile.

### A.5 Modifier als Schichten

Die 30 Modifier der Ortsdaten wirken als Zusatzschichten (Teile, Formen, Effekte, Ringsymbole). Nur
wenige brauchen eigene Teile; die meisten kombinieren vorhandene (zum Beispiel `modifier-collapsed`,
`modifier-flooded`, `modifier-burning`, `modifier-sealed`, `modifier-occupied`, `modifier-guarded`).
