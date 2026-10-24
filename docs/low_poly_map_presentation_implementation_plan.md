# Umsetzungsplan: Low-Poly-Kartendarstellung mit Ortsbaukasten

Status: **V0 und V1 abgeschlossen (G0, G1 bestätigt); V2a (Resolver, Daten, Brücke aus Daten, inkrementelle Aktualisierung) umgesetzt, Owner-Prüfung offen; V2b (Ersatz der alten Spielansicht) folgt**
Vorgeschlagener Branch je Stufe: `codex/map-presentation-v<N>-<kurzname>`
Umfang: Darstellung der Karte, Orte, Figurengruppen und Tiere gemäß dem Konzept; kein neues Spielsystem.

Zugehöriges Konzept:

- `low_poly_map_presentation_concept.md`

Verwandte Dokumente:

- `map_presentation_simplified_concept.md` (wird angepasst, siehe V0)
- `technical_concept.md`
- `visual_asset_tech_addendum.md`
- `content_workbench_editor_concept.md`
- `gameconcept/location_archetypes_and_interactions_concept.md`
- `gameconcept/factions_and_trade_concept.md`
- `cross_system_foundation_implementation_plan.md` (Vorbild für Aufbau und Teststrategie)

## 1. Ziel

Die Karte zeigt Ortszustand, Wissensgrad, Hindernisse, Expeditionen, Fraktionsgruppen und Tiere als
lesbares Low-Poly-3D-Bild mit drehbarer Kamera. Das Aussehen entsteht aus JSON-Daten über einen
testbaren Resolver in `Game.App`. Unity baut und animiert nur, was der Resolver beschreibt.

Der Plan hält die Projektregeln ein: keine Spielregeln in Szenen oder Prefabs, keine Preisgabe
verborgener Weltwahrheit, Befehle für Spieleraktionen, Tests bei Änderungen an `Game.Core`.

## 2. Verifizierter Ausgangspunkt

Aus dem Code und den Daten geprüft:

- `Game.Core` und `Game.App` sind frei von Unity-Typen; die Tests laufen als Programm in
  `tests/Game.Tests` (`Program.cs`), Entwicklungsszenarien liegen in `tests/DevelopmentScenarios`.
- `UnityHexMapView.cs` hat über 4500 Zeilen und baut die Karte aus Prefabs. Orte werden nach
  `LocationKind` gezeichnet (`BuildCoreFeatures`), nicht nach Archetyp oder Variante.
  `HexMapPrefabLibrary`, `HexMapPlaceholderPrefab` und der Editor-Generator
  `HexMapPrefabGenerator` erzeugen heute Platzhalter-Prefabs.
- Die Weltpfade (`WorldPathKind`: `River`, `Road`, `TerritoryBorder`, `Wall`) werden **nicht** im
  Laufzeit-Snapshot gespeichert, sondern mit der Welt bereitgestellt
  (`WorldRuntimeSnapshot.Restore`). Ein neuer Pfadtyp ändert das Save-Format nicht.
- Sperren zwischen Feldern setzt `MoveExpeditionCommand.IsBlockedByEdgeLocation` anhand von
  Kantenorten und deren `operational`-Zustand. Ein geöffneter Weg (`route-opened-*`) hebt die Sperre auf.
- `Game.Core` kennt `TerrainType` je Feld (unter anderem `Hills`, `Mountain`, `Snow`, `Swamp`),
  aber **keine Höhe**. Die Höhe `Elevation` gibt es nur im Weltgenerator (`WorldGen`).
- Die Tutorialwelt modelliert die Schlucht heute als Pfad `river-gray` vom Typ `River`.
- Der Weltgenerator vergibt Varianten als deutsche Namen. `WorldGenBridge.VariantIdFor` bildet nur vier
  davon auf Katalog-IDs ab (`broken-bridge`, `marked-grave`, `abandoned-camp`, `sealed-gate`); alle
  anderen werden zu `worldgen-<name>` ohne Katalogeintrag.
- **Gefundener Fehler:** In `WorldGenBridge.cs` stehen die Umlaute von „Zerstörte Brücke“ doppelt
  kodiert (`ZerstÃ¶rte BrÃ¼cke`). Der Generator liefert „Zerstörte Brücke“ korrekt, daher passt der
  Vergleich nicht, und generierte Brücken erhalten `worldgen-…` statt `broken-bridge`. Das wird in
  V1a mit einem Test behoben.
- Datenbasis liegt in `UnityHexMapView/Assets/StreamingAssets/GameData` mit
  `game-data-manifest.json` (7 Archetypen, 8 Varianten, 8 Zustandsprofile, 30 Modifier,
  19 Visual-Assets, Fraktionssignaturen in `Factions/signatures.json`).
- Der Zustand der Orte im Wissen des Spielers wird über `KnowledgeState` (`FindLocationCondition`,
  `ObserveLocationCondition`) geführt. Die Fraktionsfarbe steckt im Generator als optionales
  `Faction.Color`.

## 3. Arbeitsregeln für alle Stufen

1. **Wissen vor Optik.** Jede Stufe hat Tests, dass nichts über den `KnowledgeState` hinaus gezeigt wird.
2. **Resolver zuerst testbar.** Logik in `Game.App`, Unity nur Abspielen.
3. **Platzhalter bleiben gültig.** Jedes Teil hat einen Fallback; fehlende Teile brechen nichts.
4. **Kleine Schritte.** Jede Stufe ist einzeln nutzbar und entfernt nichts, was noch gebraucht wird.
   Das alte Rendering läuft hinter einem Schalter weiter, bis die neue Darstellung geprüft ist.
5. **`UnityHexMapView.cs` wächst nicht.** Neue Darstellung entsteht in eigenen Klassen; nur
   Einhängepunkte dürfen in der großen Datei stehen.
6. **Review-Punkte** (laut Projektregeln Freigabe durch den Owner): Änderung von `AGENTS.md`, neue
   Pfadart in `Game.Core`, neue Datendokumente im Manifest, Szenen- und Prefab-Architektur.
7. **Daten statt Code** für alles, was Autorinnen und Autoren später ändern sollen.

## 4. Übersicht der Stufen

| Stufe | Inhalt | Größe | Abhängig von | Tor |
| --- | --- | --- | --- | --- |
| V0 | Stilprototyp in Unity, Dokumente anpassen | M | – | G0: Stil bestätigt |
| V1a | `Chasm`-Pfad, Weltdaten, Validierung, Fehlerkorrektur | M | G0 | G1 |
| V1b | Gelände mit Relief, Linienobjekte, Kamera | L | V1a | G1: Brücken-Durchstich sichtbar |
| V2 | Resolver, Datenformat, Tutorial-Orte | L | V1b | G2: Tutorial spielbar |
| V3 | Alle Generator-Orte, neue Varianten, Modifier | XL | V2 | G3 |
| V4 | Figurengruppen und Fraktionen | L | V2 | G4 |
| V5 | Tiere und Spuren | M | V3, V4 | G5 |
| V6 | Anbindung an die Content Workbench | M | V2, Workbench E1 | G6 |

V4 kann parallel zu V3 laufen, wenn zwei Personen oder Sitzungen beteiligt sind; sie berühren
unterschiedliche Dateien.

## 5. Stufe V0 – Stilprototyp

Ziel: Der Owner bestätigt den Look am echten Unity-Rendering, bevor Daten und Logik angefasst werden.

Arbeitspakete:

1. Neue Testszene (zum Beispiel `MapStyleSandbox`) im Unity-Projekt, ohne Anbindung an `GameState`.
2. Erzeugtes Gelände: Relief mit Graben, Hügeln und Bergen, Wasser im Graben, Wände in Erd- und Felsfarbe.
3. Teile aus Grundformen, per Editor-Werkzeug als Prefabs erzeugt (Erweiterung von
   `HexMapPrefabGenerator` oder eigenes Werkzeug): Brücke in vier Formen, Lager, Grab, Tor,
   Turm, Höhle, Zaun, Bäume, Expeditionsgruppe.
4. Wissens-Shader mit den Stufen bestätigt, berichtet, unbekannt; unbekanntes Gelände flach.
5. Archetyp-Markerring mit Symbol und Farbe (Palette aus Konzept 9.2).
6. Orbit-Kamera (Drehung, Zoom, Fokus auf Wunsch), orthografisch.
7. Entwurf der Dokumentänderungen: `AGENTS.md` (Regel zur Kartendarstellung),
   `map_presentation_simplified_concept.md` (als abgelöst markieren), `ui_ux_concept.md` (Kamera,
   Marker). **Nur als Entwurf zur Freigabe.**

Abnahme (G0):

- Der Owner bestätigt Look, Lesbarkeit der Zustände und Wissensgrade, Kamera und Leistung.
- Frame-Zeit auf der Zielhardware ist akzeptabel (Messwert wird im Plan nachgetragen).
- Entscheidung über die Dokumentänderungen.

Nicht enthalten: Anbindung an den Spielzustand, Daten, Resolver.

## 6. Stufe V1a – Pfadart, Weltdaten, Validierung

Ziel: Die Weltdaten kennen Gräben und Schluchten als sichtbare Linie. Die Sperrlogik bleibt am Kantenort.

Arbeitspakete:

1. `WorldPathKind.Chasm` am Ende des Enums ergänzen (`Game.Core`). Review-Punkt.
2. `WorldGenBridge`: für Kantenhindernisse einen `Chasm`-Pfad erzeugen (Höhensprung und
   „Verschütteter Pass“); Flussüberquerungen verweisen auf den bestehenden `River`-Pfad.
3. `TutorialGameFactory`: `river-gray` als `Chasm` führen, Verlauf unverändert.
4. Validierung: jeder sperrende Kantenort eines `route-obstacle` liegt auf einem `River`- oder
   `Chasm`-Pfad. Meldung mit Ort, Pfad und Kante.
5. Fehler in `WorldGenBridge.VariantIdFor` (doppelt kodierte Umlaute) beheben und mit einem Test
   absichern; Quelldatei auf UTF-8 prüfen.
6. Tabelle `VariantIdFor` zu einer datengetriebenen Zuordnung ausbauen (Generatorname zu
   Varianten-ID), damit neue Varianten ohne Codeänderung angebunden werden. Die Tabelle liegt als
   JSON im Manifest. **Auf V3 verschoben:** Erst dort entstehen die Katalogvarianten, auf die die
   Zuordnung zeigt; vorher wäre sie ein Datendokument ohne Inhalt. Der Fehler selbst ist behoben
   (Punkt 5).

Tests (`Game.Tests`):

- `Chasm`-Pfad wird erzeugt und liegt auf der Hindernis-Kante.
- Validator meldet Hindernis ohne Pfad und Pfad ohne Hindernis (Hinweis).
- Variantenzuordnung für „Zerstörte Brücke“ liefert `broken-bridge`.
- Bestehende Tutorial- und Bewegungstests bleiben grün; Snapshots ohne Änderung ladbar.

Abnahme: alle Tests grün, Save-Kompatibilität bestätigt, Review der Core-Änderung.

Umgesetzt (V1a):

- `WorldPathKind.Chasm`; die Tutorialwelt führt die Schlucht als `chasm-gray`.
- `WorldObstaclePathRules` (`Game.App`): Stützregel, Querlinie und Validierung. Eine Kante gilt als
  gestützt, wenn eines ihrer Felder auf einem `River`- oder `Chasm`-Pfad liegt oder eine Pfadstrecke
  quer über die Kante zwischen den beiden gemeinsamen Nachbarfeldern läuft.
- `WorldGenBridge` erzeugt für jedes Wegehindernis ohne Linie einen `Chasm`-Pfad quer über die Kante.
- Kodierungsfehler behoben: `WorldGenBridge.cs`, ein Testtext und die Expeditions-Anzeige
  (`ExpeditionScreenController.cs`, dort stand „Â·“).
- Tests in `WorldObstaclePathTests.cs`; der Variantentest schlägt mit dem alten Kodierungsfehler fehl.
- Die alte Unity-Ansicht zeichnet `Chasm` vorläufig wie einen Fluss, damit die Schlucht bis V2 sichtbar bleibt.

## 7. Stufe V1b – Gelände, Linien, Kamera

Ziel: Das Brückenproblem ist gelöst: Der Graben läuft sichtbar durch, die Sperre ist erkennbar.

Arbeitspakete:

1. `MapTerrainBuilder` (Unity): erzeugt das Gelände-Mesh aus `TerrainType`, Pfaden und Seed. Hügel
   und Berge erhalten Relief aus dem Geländetyp mit deterministischem Rauschen (Core hat keine
   Höhe). Gräben und Flussbetten werden abgesenkt. Das Relief ist reine Darstellung.
2. **Wissen im Gelände:** Das Mesh wird aus dem bekannten Teil der Karte gebaut. Unbekanntes ist
   flach und pale; berichtetes Gelände ist weicher. Es verrät kein Relief.
3. `MapLineBuilder`: Linienobjekte für `River`, `Chasm`, `Road`, `Wall` mit durchgehender Geometrie,
   Wänden und Wasserfläche. Straßen enden sichtbar an Hindernissen.
4. Darstellung der Sperre: Der Durchlass in der Linie folgt dem `operational`-Zustand des
   Kantenorts (Brücke noch als einfacher Platzhalter).
5. `MapCameraController`: Orbit, begrenzte Neigung, Zoomstufen Übersicht/Gebiet/Ort, Objektzoom
   auf Wunsch, kein automatisches Folgen. Die Drehung wird nicht gespeichert.
6. Alte Darstellung hinter einem Schalter weiter erhaltbar.
7. `TerritoryBorder` wird nicht als farbige Linie gezeigt (nur Debug); Entscheidung dokumentieren.

Tests:

- Terrain-Builder ist deterministisch (gleicher Seed, gleiches Mesh), prüfbar ohne Szene.
- Wissenstest: unbekannte Felder liefern flache Höhe.
- Lauftests in Unity manuell: Brücken-Durchstich in allen Betriebszuständen.

Abnahme (G1): Der Graben läuft durch, die Brücke ist nicht umgehbar, die Sperre stimmt mit der
Logik überein; Kamera bedienbar; Leistung im Rahmen.

Umgesetzt (V1b) als eigenständige Weltansicht, damit die große Datei `UnityHexMapView.cs` unberührt
bleibt: `MapStyleWorldView` rendert einen echten `GameState` (Tutorial oder generierte Welt).
Bausteine: `MapStyleWorldTerrain` (Relief aus Geländetypen, Schluchten und Flüsse aus Pfaden, Wissen),
`MapStylePolylineIndex`, `MapStyleOrbitCamera`, `MapStyleLighting`. Brücken stehen auf der
Schluchten- oder Flusslinie, quer zur Linie, ihr Zustand ist der **bekannte** Zustand. Die Einbindung
in den eigentlichen Spielablauf (Befehle, UI, Neuaufbau bei neuem Wissen) folgt mit V2, wenn die
Ansicht die alte ersetzt.

## 8. Stufe V2 – Resolver, Daten, Tutorial-Orte

Ziel: Die sechs Tutorial-Orte werden vollständig über Daten und Resolver dargestellt, in allen ihren Zuständen.

Arbeitspakete:

1. Datendokument anlegen und im Manifest eintragen: **ein** Dokument `Visuals/visual-definitions.json`
   mit den Abschnitten `markers`, `parts` und `compositions` (Owner-Entscheidung; später teilbar).
   Review-Punkt.
2. Typen in `Game.Core` oder `Game.App` (je nach Reichweite) und Lader im `GameDataCatalog`;
   Validierung: unbekannte Teile, Zustands-IDs, verbotene Verweise, fehlende Fallbacks.
3. `LocationVisualResolver` in `Game.App`: Eingaben sind Ort, Anker, Archetyp, Variante, Modifier,
   Zustandskanäle und Wissensstand. Ausgabe ist `LocationVisualDescription` (Teile, Formen, Posen,
   Effekte, Marker, Wissensgrad).
4. Auswahlregeln gemäß Konzept 7.2 und Kanalüberlagerung gemäß 7.5a.
5. Unity-Bausteine: `PartLibrary`, `CompositionBuilder`, `TransitionAnimator` (Posen, Ein- und
   Ausblenden, Übergangseffekte), Markerring, Wissens-Shader.
6. Die sechs Tutorial-Orte per Daten abbilden; `BuildLandmarkObject`-Zweig für diese Arten
   abschalten, sobald geprüft.
7. Übergänge nur bei Wissen: Zustandswechsel ohne Beobachtung ändern die Karte nicht.

Tests:

- Pro Variante und Betriebszustand liefert der Resolver die erwartete Beschreibung.
- Überlagerung der Kanäle und Vorrang bei Slot-Konflikten.
- **Wissenstests:** unbekannt ergibt neutralen Marker ohne Details; berichtet ist entsättigt; alt
  zeigt den letzten bekannten Stand und wird nicht aktualisiert; der Ring verrät den Typ nicht vor
  dem Wissen.
- Fallbacks: fehlende Komposition, fehlendes Teil, unbekannter Zustand.
- Tutorialroute läuft durch, Karte und Logik stimmen überein.

Abnahme (G2): Tutorial vollständig spielbar mit der neuen Darstellung; Owner prüft alle
Zustandsübergänge im Spiel.

Owner-Entscheidungen zu V2: ein gemeinsames Datendokument; die neue Ansicht **ersetzt** die alte
vollständig (das Projekt ist ein Spielplatz, bei Problemen wird zurückgesetzt); Aktualisierungen
laufen **inkrementell** (nur geänderte Chunks und Orte, damit später auch Fraktionszüge einzelne Teile
ändern); zuerst die Brücke als Durchstich, dann die übrigen Orte.

Aufteilung:

**V2a – Durchstich Brücke (umgesetzt, in der Entwicklungsansicht)**

- `Visuals/visual-definitions.json` mit Markern, 13 Teilen und der Komposition `vis-broken-bridge`
  (42 Elemente); im Manifest eingetragen; im `GameDataCatalog` als optionales Dokument geladen und
  gegen Archetypen, Varianten und Zustandsprofile validiert (`VisualDefinitions.cs`).
- `LocationVisualResolver` (`Game.App`): `Resolve`, `ResolveForPlayer` (nur Wissen, letzter beobachteter
  Zustand, nichts auf unbekanntem Gelände) und `ResolveWorldTruth` (Entwicklersicht).
- Unity: `MapStyleVisualKit` (Teile aus Grundformen), `MapStyleLocationView` (zeichnet eine
  Beschreibung, aktualisiert nur geänderte Elemente), Geländechunks mit Teilaktualisierung
  (`MapStyleWorldTerrain.UpdateKnowledge`, `BuildChunkMesh`), Weltansicht mit Aktualisierung alle
  0,25 Sekunden und Entwicklerknöpfen.
- Tests: `VisualDefinitionTests.cs`.

**V2b – Ersatz der alten Spielansicht (Schritte 1 bis 3 umgesetzt, in Unity ungeprüft)**

**V2c – weitere Orte aus Daten (Verlassenes Lager umgesetzt, in Unity ungeprüft)**

Das Datenformat kennt Licht- und Rauchelemente, `scale` und `plain`. Das verlassene Lager
(`vis-abandoned-camp`, 24 Elemente) zeigt die drei Zustandskanäle überlagert: Anwesenheit bestimmt Zelte,
Feuer, Licht, Rauch, Kisten, Fahne und Figuren, der Betrieb Steinhaufen oder Warnschild, die Erkundung Spuren der
Expedition (Pfahl, Grabungen, Feldtafel). Offen: Tor, Grenzwarnung, Ufer. Einzelheiten:
`low_poly_map_presentation_v2c_review.md`.

**V2d – Markiertes Grab aus Daten (umgesetzt, in Unity ungeprüft)**

`vis-marked-grave` (21 Elemente): Betrieb versiegelt (rote Siegelbalken), offen (Platte zur Seite, dunkle Öffnung, Schaufel,
grünes Licht) oder gestört (Platte halb verschoben); Anwesenheit beobachtet (Figur) oder verlassen (Bewuchs); Erkundung
wie beim Lager. Die Testhilfen der Entwicklungsansicht gelten für jeden Ort mit Punktkomposition. Einzelheiten:
`low_poly_map_presentation_v2d_review.md`.

**V2e – Tor, Grenzwarnung, Ufer aus Daten (umgesetzt, in Unity ungeprüft)**

Alle sechs Tutorial-Orte kommen jetzt aus Kompositionen: `vis-sealed-gate`, `vis-border-warning`, `vis-coastal-landing`.
Einzelheiten: `low_poly_map_presentation_v2e_review.md`. Offen: Wege an Brückenenden.

**V2f – Orts-Modelle des klassischen Renderers entfernt**

`BuildLandmarkObject` und die Zweige für Grab, Lager, Schlucht, Ruine und Landmarke sind gelöscht. Der klassische Rückfall
(Shader fehlt, Schalter aus, Prozedural-Modus) zeichnet nur noch Gelände, Siedlungen, Türme, Minen und Mauern; Orte ohne
Meshes erscheinen dort nur über die Overlays. Chunk-Renderer, Prozedural-Modus und die Prefab-Bibliothek bleiben bewusst
bestehen (Entscheidung des Owners: nur Orts-Modelle). Die Prefab-Felder (`gravePrefabs` usw.) sind jetzt ungenutzt.

**V2g – Wege an Brückenenden (umgesetzt, in Unity ungeprüft)**

Sobald eine Brücke platziert ist, zieht `MapStyleMapController.LinkRoadToBridgeEnd` von jedem Brückenende eine kurze Wegstrecke
zum nächsten Weg auf trockenem Land (höchstens 6 Einheiten; ein Weg direkt am Ende bleibt unberührt). Die Strecke liegt im
Wegeindex, färbt also das Gelände und hält Wald frei. Bekannte Grenze: Wird eine Brücke erst später bekannt, werden nur die
von der Aufdeckung betroffenen Geländeblöcke neu gebaut; ragt die Strecke in einen anderen Block, folgt dessen Färbung
erst bei dessen nächster Aktualisierung.

Umgesetzt (V2b): Die Spielszene zeichnet Gelände, Orte und Wasser mit dem Low-Poly-Relief (`MapStyleMapController`,
Schalter `useMapStyleRenderer`), die Feldwahl läuft über das Relief, Overlays liegen auf der Geländehöhe,
Q und E drehen die Karte, Aktualisierung nach Befehlen und alle 0,25 Sekunden. Einzelheiten und Prüfanleitung:
`low_poly_map_presentation_v2b_review.md`. Offen aus der Liste unten: Overlays als eigene Linienobjekte,
Entfernen der alten Zweige, übrige Tutorial-Orte als Kompositionen.

- Die neue Ansicht rendert im echten Spielablauf (`UnityHexMapView`, Befehle, UI): Gelände, Linien,
  Orte, Expedition; Feldwahl und Hover über das Relief (Strahlen gegen das Gelände); Kamera; Auswahl-,
  Reichweiten-, Marker- und Notizdarstellung; Nebel aus dem Wissen.
- Aktualisierung nach Befehlen und Weltzügen über denselben Weg wie V2a.
- Danach die übrigen fünf Tutorial-Orte als Kompositionen (Grab, Lager, Tor, Grenzwarnung, Ufer) und
  Abschalten der alten Darstellungszweige (`BuildLandmarkObject`, Chunk-Renderer).

> **V3-Vorarbeit (erledigt):** Die Generator-Orte kommen aus JSON-Regeln (`World/location-placement.json`) und haben 30 Katalogvarianten mit
> Zuständen und Texten. Siehe `world_generator_location_data_concept.md` (Stand G1 bis G3). Die Bilder der Varianten (Arbeitspaket 3 unten)
> sind der nächste Schritt; Flächenanker für Gefahrenzonen sind noch offen.
>
> **V3a (umgesetzt, in Unity ungeprüft):** Untersuchungs- und Spurenorte (Alte Ruine, Steinkreis, Verfallener Schrein, Kalte Feuerstelle,
> Zerbrochener Karren, Altes Bootslager) als Kompositionen; siehe `low_poly_map_presentation_v3a_review.md`.

## 9. Stufe V3 – Generator-Orte, neue Varianten, Modifier

Ziel: Alle heute vom Generator erzeugten Orte und die ergänzten Varianten haben ein Bild.

Arbeitspakete:

1. **Neue Varianten als Daten** (Review-Punkt, da Autorendaten): Höhle (`investigation-site`),
   Mine (`resource-site`), Siedlung und Dorf (`contact-site`). Jeweils Variante, Szenarioprofil,
   Zustandsprofil, Inhaltsprofil, Texte (`de.json`), Visual-Platzhalter und Zuordnung im Generator.
   Diese Arbeit folgt `content_workbench_editor_concept.md` und den vorhandenen Schemas.
2. Weitere Generatorvarianten als Katalogvarianten anlegen (Anhang A.3 des Konzepts), damit sie
   nicht mehr als `worldgen-<name>` ohne Definition laufen.
3. Teile und Kompositionen für alle Gruppen: Untersuchungs-, Verwahrungs-, Spuren-,
   Ressourcenorte, Gefahrenzonen, Landmarken, Grenzzeichen (Pfadanker), Wegehindernisse.
4. Ankerarten: Punkt, Fläche (Decal und Streuung), Kante, Pfad (Zeichen in Abständen).
5. Modifier-Schichten für alle 30 Modifier, mit eigenen Teilen nur dort, wo nötig.
6. Archetyp-Vorlagen (Konzept 7.4) und generischer Platzhalter für unbekannte Varianten.
7. Leistungsmaßnahmen: Pooling, LOD nach Größenklasse, Sichtbarkeit nach Zoomstufe.

Tests:

- **Abdeckungstest:** jede Katalogvariante und jeder Modifier hat Komposition oder Fallback.
- Generator-Läufe mit festen Seeds: jeder platzierte Ort wird aufgelöst, keiner fällt auf einen
  ungültigen Zustand.
- Wissens- und Fallbacktests wie in V2 für die neuen Gruppen.

Abnahme (G3): Eine generierte Welt (mehrere Seeds) ist vollständig dargestellt; Leistungsmessung
mit vielen Orten im Rahmen.

## 10. Stufe V4 – Figurengruppen und Fraktionen

Ziel: Expeditionen und Fraktionsgruppen sind lesbar, ohne Weltwahrheit zu verraten.

Arbeitspakete:

1. Figurenbaukasten (Körper, Ausrüstung, Rollenprops) als erzeugte Teile; Animationen `idle`,
   `walk`, `alert`, `act`, `down`.
2. Gruppendarstellung: Expedition (Zustand der Gruppe, nicht pro Person), Patrouillen, Posten,
   Dorfbewohner.
3. Fraktionsdarstellung aus `Faction.Color` und den Signaturen; **nur bei bekannter Fraktion**,
   sonst neutral. Gebiete bleiben ohne Farbfläche und ohne Linie.
4. Bewegung folgt dem Pfad der Logik und läuft ihr nach, nie voraus.
5. Resolver-Erweiterung `GroupVisualDescription` mit denselben Wissensregeln.
6. Validator: Fraktionsfarbe zu nah an einer Archetypfarbe ergibt einen Hinweis.

Tests:

- Unbekannte Fraktion erscheint neutral; bekannte mit Farbe und Zeichen.
- Verbündet, neutral oder feindlich folgt dem **bekannten** Verhältnis.
- Gruppengröße und Zustand ergeben sich aus der Logik.

Abnahme (G4): Expedition begleiten und Fraktionsbegegnung im Tutorial prüfen.

## 11. Stufe V5 – Tiere und Spuren

Ziel: Raubtiere und Herdentiere sind Teil von Orten, Ereignisse hinterlassen Spuren.

Arbeitspakete:

1. Artenkatalog (`Visuals/visual-creatures.json`) mit wenigen Raubtier-Varianten und Herdentieren;
   Lebensraum-Tags aus Biom und Ortsvariante.
2. Tiere erscheinen **nur** als Teil von Orten und Zuständen (Raubtiergebiet, Tierherde,
   Dschungel- und Sumpfjäger) oder als Spuren. Es gibt keine freien, nicht erklärten Tiere,
   die verborgene Welt verraten könnten.
3. Spuren von Ereignissen (Fußabdrücke, Rauch, zurückgelassene Teile, Markierung am Ort), sichtbar
   gemäß Wissen. Die Ereignisse selbst bleiben Bild und Bericht.
4. Animationen: Herde, Raubtier `idle`, `alert`, `act`.

Tests: Tiere folgen Ort und Wissensgrad; unbeobachtete Ereignisse erzeugen keine Spuren vor dem Bericht.

Abnahme (G5): Dschungel- und Sumpfort mit Raubtierspuren prüfen.

## 12. Stufe V6 – Anbindung an die Content Workbench

Ziel: Teile und Kompositionen sind in der Workbench bearbeitbar und mit Vorschau prüfbar.

Arbeitspakete (abhängig vom Stand der Workbench E1/E2):

1. Visual-Dokumente in die Dokumentfamilien der Workbench aufnehmen (Referenzindex, Validierung).
2. Vorschau über den Resolver (ohne Unity), optional Unity-Vorschau über die Testszene aus V0.
3. Prüfungen: fehlende Teile, ungenutzte Teile, Zustandslücken, Wissensvorschau.

Abnahme (G6): Eine neue Variante entsteht nur durch Daten und erscheint korrekt im Spiel.

## 13. Querschnitt

### 13.1 Leistung

Vorgaben (Messwerte werden in V0 festgelegt): Zeitbudget pro Bild, Zahl sichtbarer Teile, Zahl der
Lichter und Schatten. Maßnahmen: Pooling, LOD, Sichtbarkeitsprüfung, günstige Materialien, Limit für
gleichzeitig aktive Lichter.

### 13.2 Tests und Qualitätssicherung

- Alle Logik in `Game.App` mit Tests in `Game.Tests`; Unity-Teil mit manuellen Prüflisten je Stufe.
- Regressionsprüfung des Tutorialverlaufs nach jeder Stufe.
- Sichtprüfungen mit Screenshots der Wissensstände und Zustände pro Ort (Referenzbilder im
  Repository, soweit sinnvoll).

### 13.3 Dokumentation

- Konzept bei jeder Stufe aktualisieren (Entscheidungen, Teilekatalog).
- `AGENTS.md`, `map_presentation_simplified_concept.md`, `ui_ux_concept.md` nach G0 anpassen.
- `gameconcept/CHANGELOG.md` ergänzen.

### 13.4 Refactoring der großen Unity-Datei

`UnityHexMapView.cs` wird nicht umgebaut, sondern schrittweise verschlankt: Gelände und Linien in
V1b, Orte in V2, Figuren in V4. Pro Stufe werden die ersetzten Abschnitte hinter dem Schalter
entfernt, sobald der Owner sie freigegeben hat.

## 13a. Bekannte Punkte für später

Bei der Sichtprüfung von V2a aufgefallen und bewusst vertagt:

| Punkt | Beobachtung | Zuordnung |
| --- | --- | --- |
| Straße führt nicht zur Brücke | Straßen sind nur Bodenfarbe entlang ihrer Pfadpunkte und enden vor der Schlucht; die Brücke steht an der besten Stelle der Schlucht, nicht auf dem Straßenverlauf. | erledigt in V2g: kurze Wegstrecken von den Brückenenden zum nächsten Weg (Straßen bleiben Bodenfarbe) |
| Generierte Brücke an unpassender Stelle | Der Generator setzt das Wegehindernis auf eine Straßenkante entlang eines Flusses oder an einem Höhensprung; das ergibt nicht immer einen sinnvollen Übergang (zum Beispiel am Berg ohne Wasser). | V3: Platzierung im Generator neu bewerten, etwa nur dort, wo eine Straße einen Fluss oder eine Schlucht wirklich kreuzt |

Nach G3 vom Owner zur Umsetzung aufgenommen:

| Punkt | Beobachtung | Zuordnung |
| --- | --- | --- |
| Ortsnamen als Textschlüssel (erledigt, siehe Konzept „Stand Textschlüssel") | Namen, Untertitel und Beschreibungen der Orte stehen als Klartext im Inhaltsprofil (`content-profiles.json`) und, bei den älteren Varianten, mit Umschrift (`Zerstoerte Bruecke`). Weitere Sprachen sind so nicht möglich. | eigener Schritt nach G3: Textschlüssel im Inhaltsprofil (`titleTextId`, `subtitleTextId`, `shortDescriptionTextId`, `descriptionTextId`, `journalText`), Auflösung über `Scenes/Locales`, Umstellung aller Varianten und der Oberfläche, Umlaute wieder korrekt |
| Funde, Beweise und Folgen der neuen Varianten (Funde und Beweise erledigt, Folgen bewusst keine; siehe Konzept „Stand Funde und Beweise") | Die 23 neuen Varianten haben leere Fund-, Beweis- und Folgenpools; es gibt dort noch nichts zu finden. Aktionen mit Zustandsänderungen außerhalb des Zustandsprofils fehlen (z. B. Kartieren bei Naturorten, Behelfsübergang beim Pass). | eigener Schritt nach G3: je Variante Funde (`finding-…`), Beweise (`evidence-…`) und Folgen (`consequence-…`) entwerfen, Listen vor dem Anlegen dem Owner vorlegen, fehlende Zustände und Aktionen ergänzen |

## 14. Risiken

| Risiko | Gegenmaßnahme |
| --- | --- |
| Stil bewährt sich nicht in Unity | V0 mit Tor G0 vor allem anderen |
| Aufwand der Teile wächst | erzeugte Teile, Platzhalter, Priorisierung nach Spielrelevanz |
| Relief verrät Weltwahrheit | Gelände wird aus Wissen gebaut; Tests |
| Karte und Logik laufen auseinander | Sperre am Kantenort, Validierung, Tutorialtests |
| Leistung bei vielen Orten | Pooling, LOD, Messung ab V0 |
| Datenformat passt später nicht | Schema im Review, offene Felder, Version im Dokument |
| Generatorvarianten ohne Definition | V1a-Zuordnung, V3-Abdeckungstest |
| Doppelt kodierte Umlaute auch anderswo | Prüfung der Quelldateien auf Kodierung in V1a |

## 15. Nicht im Umfang

- Echte Terrainhöhe in der Logik, Sichtlinien, Deckung.
- Live-Inszenierung von Ereignissen auf der Karte.
- Finale Kunstassets und Audio.
- Neue Archetypen oder neue Spielsysteme.
- Änderung des Save-Formats (V1a bestätigt, dass keine nötig ist).

## 16. Review-Punkte für den Owner

1. Freigabe von V0 (Beginn) und danach G0 (Stil).
2. Änderung der Projektregeln in `AGENTS.md` und den Kartendokumenten.
3. Neue Pfadart `Chasm` in `Game.Core`.
4. Neue Datendokumente im Manifest und deren Schemas.
5. Neue Varianten (Höhle, Mine, Siedlung, Dorf) einschließlich Texten.
6. Entscheidung, wann die alte Darstellung entfernt wird.
