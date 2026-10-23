# Konzept: Low-Poly-3D-Kartendarstellung mit Ortsbaukasten

Status: **Vorschlag – Owner-Freigabe erforderlich; keine Implementierung**

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
  (isometrischer Eindruck bleibt).
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
| flache isometrische 2D-Sprites | Low-Poly-3D, drehbare Kamera | Entscheidung des Owners nötig |
| keine sichtbare Terrainhöhe | sichtbares Relief **nur optisch** (Gräben, Hänge, Gipfel) | Höhe bleibt außerhalb der Logik |
| Hexes unsichtbar | unverändert unsichtbar | keine Änderung |
| Hindernisse als Objekte oder Kanten-Visuals | Hindernisse als Linien/Flächen mit Zustand | neue Darstellungsart |
| `LocationKind` steuert die Darstellung | Archetyp, Variante, Modifier steuern sie | Visual-Resolver nötig |

Bis zur Freigabe bleiben `AGENTS.md` und das bisherige Kartenkonzept maßgeblich.

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
- Die Kamera darf einer Expedition folgen und bei wichtigen, **dem Spieler bekannten** Ereignissen
  sanft fokussieren.
- Die Drehung ist eine reine Ansichtsfunktion; Interaktion (Auswahl, Ziel) nutzt weiterhin die
  logische Hex-Koordinate.

### 4.2 Look

- Flat-shaded Dreiecke mit Farbe pro Fläche; wenige Materialien; keine Texturen im Standardfall.
- Gelände aus einem Mesh mit abgesenkten Bereichen für Gräben, Flussbetten und Schluchten; Wände in
  Erd- oder Felsfarbe.
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
| 2 Linien und Flächen | Fluss, Graben, Straße, Mauer, Grenze, Sumpf, Gefahrenfläche | `WorldPathState` und Flächenanker |
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

### 7.6 Übergänge

Zustandswechsel werden **übergeblendet**, nicht getauscht:

- Teile wachsen ein oder aus, verschieben oder neigen sich weich.
- Ereignishafte Wechsel (Einsturz, Brand, Reparatur) erhalten einen kurzen Effekt (Staub, Funken,
  Rauch).
- Ein Übergang wird nur gezeigt, wenn der Spieler den Wechsel **wissen darf** (siehe 11). Sonst
  bleibt das alte Bild bis zu einem Bericht oder einer Beobachtung.

## 8. Figuren und Tiere

### 8.1 Figuren

- Modularer Aufbau: Körper, Kleidung/Ausrüstung, Rollenprop, Fraktionszeichen.
- **Expeditionsmitglieder** zeigen ihre Rolle durch Ausrüstung und ihren Zustand (Last, Verletzung,
  Erschöpfung) durch Haltung und Kleinteile.
- **Fraktionsangehörige** erkennt man an Silhouette, Ausrüstung und Banner. Die Fraktionsfarbe kommt
  aus den Fraktionsdaten.
- Ist die Fraktion dem Spieler **nicht bekannt**, bleibt die Darstellung neutral (kein Banner, keine
  Fraktionsfarbe, keine verräterische Ausrüstung).
- Verbündet, neutral oder feindlich ergibt sich aus dem bekannten Verhältnis, nicht aus der
  Welt-Wahrheit.

### 8.2 Tiere und Kreaturen

- Artenkatalog mit Größenklasse, Verhalten (Herde, Einzelgänger, Raubtier) und Lebensraum-Tags.
- Der Katalog ist offen; neue Arten nutzen vorhandene Skelett- und Animationsmuster.
- Unbekannte Kreaturen erscheinen als Spuren, Schatten oder Zeichen, bis sie bestätigt sind.

### 8.3 Animation

Bewusst klein gehalten: `idle`, `walk`, `alert`, `act` (Arbeiten, Fressen, Angriff), `down`.
Weitere Posen sind Erweiterungen. Bewegung zwischen Feldern folgt dem Pfad der Logik; die Animation
läuft der Logik hinterher, nie voraus.

### 8.4 Dynamische Situationen

Beispiel: Die Expedition wird im Dschungel von Tieren angegriffen.

- Die Logik löst das Ereignis auf (Risiko, Ergebnis, Folgen).
- Die Darstellung **inszeniert das Ergebnis**: Tiere erscheinen, nähern sich, der Ausgang wird
  gezeigt. Sie entscheidet nichts und simuliert keinen Kampf.
- Die Situation ist zeitlich begrenzt und endet mit einer sichtbaren Spur (Fußabdrücke, Verletzte,
  Beute).
- Was der Spieler sieht, richtet sich nach Beobachtung und Bericht. Ein unbeobachteter Angriff
  erscheint erst als Bericht, nicht als Live-Szene.

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
- Farbpaletten für Fraktionen müssen sich in Farbton oder Träger von den Archetypfarben
  unterscheiden; der Validator warnt bei zu großer Nähe.

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
| berichtet | entsättigt, leicht durchscheinend, Quellenhinweis |
| alt | verblasst, mit Alter; zeigt den **letzten bekannten** Stand |
| unbekannt | leere Fläche oder neutraler Platzhalter, keine Details |

Besondere Regeln:

- Verborgene Zustände (Fraktionsbesitz, Hinterhalt, Fallen) erscheinen nicht, bis sie bekannt sind.
- Der Zustand eines Ortes wird bei Alt-Wissen **nicht** automatisch aktualisiert; die Karte zeigt, was
  man zuletzt wusste.
- Unbekanntes Gelände wird nicht nur ausgeblendet, sondern auch **flach** dargestellt, damit das
  Relief (zum Beispiel ein Graben) nichts verrät.
- Spuren und Zeichen dürfen auf Unbekanntes hinweisen, ohne es zu benennen.

## 12. Datenmodell (Skizze)

Neue Dokumente, die im `game-data-manifest.json` eingetragen werden. Die Feldnamen sind ein
Vorschlag und werden mit den Schemaregeln des Projekts abgeglichen.

- `Visuals/visual-parts.json`: Teile-Katalog (Kategorie, Größenklasse, Formen, Tags, Fallbacks).
- `Visuals/visual-compositions.json`: Kompositionen und Modifier-Schichten.
- `Visuals/visual-markers.json`: Archetypfarben, Symbole, Modifier-Symbole.
- `Visuals/visual-creatures.json`: Figuren- und Tierkataloge.

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

- **`Game.Core`**: unverändert; kein Bezug zu Darstellung. Eine neue Pfadart für Gräben oder
  Schluchten (und ein Zustand für Durchlässe) wäre ein eigenes, vorher abzustimmendes Thema
  (Save-Format).
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

## 14. Lieferstufen

| Stufe | Ergebnis | Nicht enthalten |
| --- | --- | --- |
| V0 | Stilprototyp in einer Unity-Testszene: ein Ausschnitt mit Graben, Brücke, Lager, Expedition in drei Wissensgraden | Anbindung an Spielzustand |
| V1 | Gelände mit Relief und Linienobjekten (Graben, Fluss, Straße) mit Zustand; Brücken-Durchstich | weitere Orte |
| V2 | Visual-Resolver, Teile- und Kompositionsdaten, Tutorial-Orte (6) | Figuren, Tiere |
| V3 | Generator-Orte (Ruine, Höhle, Turm, Ressourcen, Sumpf, Vulkan, Landmarken) und Modifier-Schichten | dynamische Situationen |
| V4 | Figuren, Fraktionsdarstellung, Expeditionsgruppen | Tiere, Angriffe |
| V5 | Tiere, dynamische Situationen, Ereignisinszenierung | – |
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

## 16. Offene Fragen

1. Freigabe des Wechsels von 2D-Isometrie zu Low-Poly-3D mit drehbarer Kamera (Owner).
2. Wie weit darf das optische Relief gehen (nur Gräben und Schluchten oder auch Hügel und Berge)?
3. Darf die Kamera bei Ereignissen automatisch fokussieren, oder nur auf Wunsch?
4. Welche Fraktionen und Tierarten braucht die erste Version?
5. Soll die Drehung der Karte gespeichert werden (Spielereinstellung)?
6. Wie werden Gräben und Schluchten in den Weltdaten modelliert (neue Pfadart, Kantenorte, beides)?
7. Wie viel vom Konzept soll in den Vertical Slice, und was folgt danach?

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
