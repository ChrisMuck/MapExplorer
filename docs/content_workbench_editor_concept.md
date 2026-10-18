# Content-Workbench-Editor-Konzept

Status: **Konzept – keine Implementierung; Owner-Entscheidung vor dem Bau erforderlich**

Verwandte Dokumente:

- `next_implementation_concept.md`
- `technical_concept.md`
- `gameconcept/cross_system_json_authoring_schema.md`
- `gameconcept/cross_system_integration_concept.md`
- `gameconcept/world_locations_and_events_concept.md`
- `gameconcept/knowledge_base_and_analysis_concept.md`
- `tutorial_vertical_slice_route_spec.md`

## 1. Entscheidung und Zielbild

Der Editor wird als **Content Workbench** verstanden: eine zusammenhängende Arbeitsoberfläche für
autorisierte Spielinhalte. Er erzeugt weder eine zweite Simulation noch eine neue, grafische
Regelsprache. Seine Aufgabe ist, die vorhandenen JSON-Dokumente, ihre Referenzen und ihre
spielerlesbare Wirkung sicher zusammenzuführen.

Das Ziel ist, dass eine Autorin oder ein Autor neue Ortsinhalte innerhalb der bestehenden
Archetypen, die zugehörigen Ereignisfolgen sowie Texte und Visuals erstellen kann, ohne zwischen
vielen Dateien raten zu müssen, und dabei jederzeit nachvollziehen kann, welche Information
objektive Weltwahrheit und welche Spielerwissen ist.

```text
Wiederverwendbare JSON-Bibliothek             Generator- und Laufzeitdaten

Feste Archetypen, Varianten, Zustände         Generator wählt Platzierung,
Aktionen, Ergebnisse, Findings, Folgen    ->  Gewichtung und passende Kombinationen
Texte, Lokalisierung, Visual-IDs              Laufzeit löst Regeln und Folgen auf

                     Content Workbench
                      - typisierte JSON-Formulare
                      - Referenzgraph
                      - legale Text-/Spielervorschau
                      - Validierung und sichere Speicherung
```

Die Workbench ist ein **Authoring-Werkzeug**, kein Spieler-Client und kein Savegame-Editor.
`Game.Core` und `Game.App` bleiben die einzige Instanz für Regeln, Zustandswechsel, Risiko,
World Phase und Knowledge State.

**Speicherregel:** Jede durch die Workbench erzeugte oder geänderte Inhaltsinformation – Regeln,
Texte, Lokalisierungen, Visual-Referenzen, Generator-Tags und Gewichte – wird ausschließlich in
den manifestierten JSON-Dokumenten gespeichert. Weder C# noch Unity-Prefabs, Szenen oder
ScriptableObjects dürfen eine parallele Inhaltsquelle oder versteckte Content-Regel enthalten.

## 2. Warum ein kombinierter Editor sinnvoll ist

Die aktuellen Inhalte sind bewusst auf mehrere Dokumentfamilien verteilt. Das ist architektonisch
richtig, aber eine inhaltliche Änderung berührt häufig mehrere Ebenen:

| Autorenziel | Betroffene Daten |
| --- | --- |
| „Ein neues verlassenes Lager als möglicher Fundort“ | Variante/Szenario, Zustands- und Content-Profil, Visual-ID, Generator-Tags/Gewichtung |
| „Dort soll eine brauchbare Aufzeichnung gefunden werden“ | Aktion, Outcome/Effekt, Finding, Analyse, Archivtext, Folgefreigabe |
| „Die Reparatur soll eine lokale Reaktion auslösen“ | Aktion/Projekt, Trigger, Konsequenzdefinition, Evidenz, Szene, Fraktionsbezug |
| „Eine Fraktion warnt vor einem solchen Ort“ | Fraktionsprofil, Signatur, territoriale Regel, Kontext-/Generatorregel, Kontakt-/Szeneninhalt |

Eine reine Formularsammlung würde diese Beziehungen wieder verstecken. Die Workbench muss sie
deshalb als **Graph und nachvollziehbare Kette** zeigen, ohne daraus einen freien Node-Graph zu
machen, der die typisierten JSON-Schemata ersetzt.

## 3. Feste Grenzen

### 3.1 Was die Workbench bearbeitet

- wiederverwendbare Katalogdefinitionen unter `GameData`;
- Spielertexte nach Locale und Visual-Asset-Referenzen;
- deklarative Verweise, Zustandsvoraussetzungen, Effekte und erlaubte Verbindungen;
- Generator-kompatible Auswahl-, Tag- und Gewichtungsdaten, soweit sie bereits vom jeweiligen
  JSON-Schema unterstützt werden;
- gezielte, reproduzierbare Preview-Zustände.

### 3.2 Was sie ausdrücklich nicht tut

- keine direkte Änderung eines laufenden `GameState`, eines Savegames oder von Unity-Szenen;
- keine Positionierung von Orten auf einer Kampagnenkarte; der Generator entscheidet diese aus
  Datenregeln, Tags, Gewichten und Weltkontext;
- keine eigene Berechnung von Risiken, Aktionsergebnissen, Fraktionsreaktionen oder Scout-Reports;
- keine C#-Callbacks, `UnityEvent`s oder Prefab-spezifischen Regeln in Content-Daten;
- keine automatische Erzeugung einer langen Ereigniskette aus einem Knopfdruck;
- keine Anzeige verborgener World-State-Fakten in der Spieler-Vorschau;
- kein Ersetzen von Git-Review, Tests oder Owner-Entscheidungen.

Die Workbench darf eine optionale **Autorensicht** haben, die verborgene Kontext- und
Verweisinformationen sichtbar macht. Diese ist strikt von der **Spieleransicht** getrennt und
farblich/inhaltlich klar gekennzeichnet.

## 4. Informationsmodell: Bibliothek, Instanz und Laufzeit trennen

Die zentrale Editorentscheidung ist dieselbe wie im Spielmodell: Definition, konkrete Welt und
Laufzeitwissen bleiben getrennt.

| Ebene | Beispiel | Darf der Editor speichern? | Darf die Spielervorschau zeigen? |
| --- | --- | --- | --- |
| Bibliothek | `scenario-broken-crossing`, `action-rebuild-bridge`, `finding-*` | Ja, als wiederverwendbare Definition | Nur als Ergebnis einer legalen Abfrage |
| Generatorregel | erlaubte Ankerart, Terrain-/Kontext-Tags, Gewichtung und Kombinationsgrenzen | Ja, wenn das Schema es vorsieht | Nur als Ergebnis einer generierten Vorschau |
| World State | konkret gewürfelter Kontext, gewählte Konsequenzverzweigung, Projektfortschritt | Nicht als Kataloginhalt; nur per Preview-Fork | Nur gemäß `KnowledgeState` |
| Knowledge/Notes | bestätigte Beobachtung, Bericht, Spielernotiz | Nicht als statischer Inhalt; nur per Preview-Szenario | Ja, wenn im Preview verdient/gesetzt |

Damit wird vermieden, dass eine wiederverwendbare Ortsdefinition eine konkrete Position, Fraktion,
spätere Konsequenzverzweigung oder einen als sicher behaupteten Geheimgrund in einen Szenario- oder
Spielertext schreibt. Feste Tutorial-Fixtures dürfen als separate JSON-Testdaten weiterbestehen,
sind aber kein Arbeitsbereich der ersten Workbench.

## 5. Empfohlenes Produkt: eine Workbench mit fünf Arbeitsbereichen

Die Workbench sollte eine Anwendung mit gemeinsamer Auswahl und gemeinsamem Referenzgraphen sein,
nicht fünf getrennte Tools. Eine Auswahl in einem Bereich markiert überall dasselbe stabile Objekt.

### 5.1 Inhaltsbibliothek

Zweck: die wiederverwendbaren Bausteine ohne konkrete Kartenkoordinate verwalten.

Kategorien der ersten Ausbaustufe:

- zunächst feste Archetypen als auswählbarer Rahmen sowie Varianten, Zustandsprofile, Modifier und
  Content-Profile;
- Aktionen, Voraussetzungen, Kosten, Risikoangaben, Projekte, Outcome-Tabellen und Effekte;
- Findings, Finding Tables, Evidenz, Kontext-, Trigger-, Situations- und Konsequenzdefinitionen;
- Fraktionsprofile, Signaturen, territoriale Regeln, Reaktionen, Erinnerungen und Angebote;
- Scout-Aufträge, Foki, Reportvorlagen;
- Szenenfragmente, Szenenrichtlinien, Kontaktprofile, Lokalisierungen und Visual Assets.

Jede Detailseite enthält drei feste Zonen:

1. **Definition** – typisierte Felder, keine freie JSON-Eingabe als Standard.
2. **Verwendungen** – alle eingehenden Referenzen, inklusive Datei und stabiler ID.
3. **Wirkung** – eine legale Vorschau der Aktion, Folge oder Szene.

Ein „Rohes JSON“-Panel darf es für Expertinnen und Experten geben, aber nur mit Schema-Validierung
vor Übernahme und ohne automatisches Umformatieren nicht betroffener Dokumente.

### 5.2 Ketten- und Graphansicht

Zweck: aus einer Autorabsicht eine überprüfbare inhaltliche Kette machen.

Ausgangspunkt ist immer ein Bibliotheksobjekt, etwa die Variante `abandoned-camp`. Die Workbench
zeigt etwa:

```text
Verlassenes Lager (Variante)
  -> Szenario „investigation-site / abandoned-camp“
  -> verfügbare Aktion „Durchsuchen“
  -> Ergebnis/Effekt „AddFinding: survey record“
  -> Rückkehr + Analyse
  -> Erkenntnis / Knowledge Points
  -> Basisaktion „Ingenieur anfordern“
  -> Expedition 2 mit Ingenieur
  -> Aktion „Brücke wieder aufbauen“
  -> Route geöffnet + World Trigger
  -> spätere Evidenz / Reaktion / Szene
```

Der Graph ist eine **Navigation und Diagnose**, keine neue Autorität. Kanten stammen ausschließlich
aus existierenden JSON-Referenzen. Er unterscheidet:

- direkte Referenz;
- bedingte Referenz (Zustand, bekannte Evidenz, Rolle, Kontakt);
- Laufzeitfolge (Trigger/Konsequenz);
- Spielerinformationen (sichtbar, berichtet, bestätigt, verborgen).

Damit kann der Editor etwa „unbenutztes Finding“, „Aktion ohne sichtbaren Ausgang“, „Text ohne
erreichbaren Trigger“ oder „rekursiven Konsequenzkreis“ verständlich melden.

### 5.3 Text-, Lokalisierungs- und Visual-Bearbeitung

Texte sind kein nachgelagerter Anhang, sondern ein gleichwertiger Arbeitsbereich. Jede relevante
Definition verlinkt hier ihre Text-IDs und Visual-IDs; die Workbench bearbeitet die locale-spezifischen
JSON-Einträge, nicht Text in Code oder Prefabs.

- Ortsnamen, sichtbare Beschreibungen und Zustandsformulierungen;
- Aktionsbezeichnungen, Beschreibungen und ehrliche Sperrgründe;
- Ergebnis-, Finding-, Analyse-, Archiv-, Report-, Kontakt-, Rückkehr- und Ereignistexte;
- deutsche Standardtexte zuerst, weitere Locales als separate Einträge mit derselben stabilen Text-ID;
- Visual-Asset-Referenzen mit Placeholder-Fallback und Verwendungsübersicht.

Eine Text-ID kann nur gelöscht oder umbenannt werden, wenn die Referenzansicht dies sicher erlaubt.
Regeltragende Felder und Text bleiben getrennt: Ein Text darf erklären, aber keine verborgene
World-State-Wahrheit behaupten.

### 5.4 Szenen- und Spielerwissensvorschau

Zweck: Spielerkommunikation auf Richtigkeit und Informationshonestät prüfen.

- Auswahl einer Location, eines Kontakts, Reports, Rückkehrs oder Ereignisses;
- Vorschau über die bestehenden `Game.App`-Resolver, nicht durch Textzusammenbau in der UI;
- Umschalter für zulässige bekannte Zustände wie unbekannt, berichtet, bestätigt, alt oder
  zweifelhaft;
- Locale-Auswahl, zunächst Deutsch als Standard;
- Visual-Asset-Karte mit Placeholder-Fallback und Prüfung fehlender Referenzen;
- Vergleich „Autorensicht“ gegen „Spieleransicht“: Die Spieleransicht darf keinen versteckten
  Kontext, konkrete ungelernte Fraktion, exakte unbekannte Zielkoordinate oder gewählten
  Konsequenzzweig verraten.

### 5.5 Validierung, Änderungen und Testläufe

Zweck: aus Content-Änderungen überprüfbare, reviewbare Änderungen machen.

- Live-Diagnosen für Feldtypen, IDs und offensichtliche Referenzen;
- vollständige Validierung ausschließlich über `GameDataCatalog` und die bestehenden
  Content-Validatoren;
- Zieltest auswählen: Katalog laden, Tutorial-Route, deterministische Development-Szenarien,
  gezielte Szenenvorschau;
- Ergebnis mit Dokumentpfad, ID, Feldpfad, Schweregrad und verständlicher Fehlerbeschreibung;
- „Warum ist diese Aktion gesperrt?“ als Abfrage der bestehenden App-Schicht;
- Änderungsübersicht nach JSON-Datei, stabilem ID-Objekt und Referenz-Auswirkung;
- keine automatische Speicherung: Änderungen liegen zunächst im Arbeitsspeicher/Entwurf und werden
  erst nach erfolgreicher Validierung explizit geschrieben.

## 6. Kern-Workflow: Ortsinhalt mit Ereignis- und Wissenskette erstellen

Der Editor führt nicht mit einem „Neuer Spezialfall“-Knopf in ein unstrukturiertes Formular.
Stattdessen erstellt er neue Varianten und Szenarien **innerhalb eines vorhandenen Archetyps**:

1. **Absicht festhalten.** Kurzer Titel, primäre Erkundungsfrage und gewünschte Spielerwirkung,
   z. B. „Warum wurde dieser Übergang geschlossen?“
2. **Szenario wählen.** Passenden Archetyp, Variante und ein gültiges Zustandsprofil auswählen.
   Die Workbench zeigt nur kompatible Kombinationen.
3. **Spielbare Optionen festlegen.** Bestehende Aktionen aus Szenario/Profil wählen und bei Bedarf
   neue wiederverwendbare Aktion oder neuen Effekt bewusst als Bibliotheksobjekt anlegen.
4. **Ergebnisse verbinden.** Evidenz, Finding, Zustandswechsel, Projekt, Route oder Trigger als
   deklarative Effekte verknüpfen. Für jede große Aktion bleibt, wo logisch möglich, eine
   Beobachten-/Markieren-/Verlassen-/Rückkehr-Option sichtbar.
5. **Spielerkommunikation schreiben.** Zustands- und Ergebnisfragmente, Locale-Texte und Visual-ID
   zuordnen. Die Vorschau prüft bekannte gegen verborgene Informationen.
6. **Generatorverträglichkeit festlegen.** Erlaubte Ankerarten, Terrain-/Kontext-Tags, Modifier-
   Pools und Gewichtungen innerhalb des bestehenden Schemas angeben. Der Generator entscheidet
   später die konkrete Position und Kombination.
7. **Folgen prüfen.** Im Graph nachsehen, ob die Kette eine offene Frage, eine Rückkehrmotivation
   oder eine spätere Möglichkeit erzeugt – nicht nur eine Währung oder einen Endpunkt.
8. **Validieren und testen.** Katalogvalidierung, generierte bzw. deterministische Preview und
   Spieler-Vorschau ausführen; danach die betroffenen JSON-Dateien bewusst speichern.

## 7. Ereignisse und Konsequenzen kombinieren

„Ereignis“ umfasst drei unterschiedliche Ebenen, die im Editor nicht vermischt werden dürfen:

| Art | Beispiel | Editor-Ort |
| --- | --- | --- |
| unmittelbares Aktionsergebnis | Brücke wird geprüft, ein Finding wird gesichert | Aktion/Outcome/Effekt |
| anstehendes Spielerereignis | beobachteter Bote, überfälliger Scout, Fundmeldung | Situation/Report/Scene-Definition plus Auslöser |
| verzögerte Weltfolge | Route wird genutzt, Warnzeichen erneuert, Gebiet reagiert | Trigger + Konsequenzdefinition + spätere Evidenz/Szene |

Die Workbench erstellt daher keine direkte, ortsgebundene „Wenn Button X, dann Szene Y“-Logik.
Stattdessen verbindet sie neutrale Effekte und Trigger mit wiederverwendbaren Folgen. Für jede
verzögerte Folge muss die Diagnose prüfen:

- gibt es vor einem schweren Effekt ein beobachtbares Frühzeichen?
- gibt es eine berechtigte Informationsquelle für die Spieleransicht?
- ist der Auslöser am Ort/räumlich plausibel verknüpft?
- wird ein konkreter, geheimer Fraktions- oder Weltzustand nicht statisch in den Katalog kopiert?
- sind Branch, Progress und tatsächliche Wirkung weiterhin Laufzeitdaten?

## 8. Sicherheits- und Versionsregeln

### 8.1 Stabile IDs und Löschung

- neue IDs werden als lowercase kebab-case vorgeschlagen, dürfen aber vor dem Speichern geändert
  werden;
- Umbenennen ist eine explizite Refactoring-Operation mit Referenzvorschau, nie eine Textsuche;
- Löschen ist gesperrt, solange eingehende Referenzen bestehen;
- nach veröffentlichten Saves bleibt Umbenennen/Löschen ein Owner-gesteuertes Migrationsproblem,
  nicht eine Editor-Automatik.

### 8.2 Speicherung

- Der Editor liest die Manifestmenge und schreibt nur geänderte Dokumente.
- Speichern läuft über temporäre Dateien und atomaren Austausch.
- Vor dem Austausch laufen Parser, Schema- und Referenzvalidierung.
- Ein Fehler schreibt keine Teilmenge; der Editor behält den Entwurf und zeigt Diagnosen.
- JSON-Diffs bleiben stabil: keine Sortierung oder Formatänderung unberührter Dokumente.

### 8.3 Zusammenarbeit

- Die Workbench ersetzt keine Pull Requests; sie bereitet kleine, verständliche Diffs vor.
- Jede Änderung dokumentiert betroffene IDs, Ketten und Testresultate.
- Für die erste Version sind keine Live-Kollaboration, Cloud-Synchronisierung oder binäre
  Content-Daten vorgesehen.

## 9. Technische Anschlussarchitektur (späterer Bau)

Die zu bevorzugende Richtung ist ein eigenständiges **C#-WPF-Projekt** namens
`Game.ContentWorkbench`. Es kann die bestehenden `Game.Core`- und `Game.App`-Assemblies,
`GameDataCatalog`, Validatoren und read-only Preview-Abfragen direkt nutzen. Dadurch bleibt es für
Formulare, Referenzgraphen, Batch-Diagnosen und Git-orientierte JSON-Arbeit geeignet.

Projektabhängigkeiten:

```text
Game.ContentWorkbench (WPF, Desktop-Authoring)
  -> Game.App
    -> Game.Core

Game.ContentWorkbench -X-> UnityEngine / Unity-Projekt / Prefab-Daten
Game.Core             -X-> WPF / Unity
```

WPF besitzt ausschließlich die Workbench-Präsentation, Editor-Entwürfe und die sichere
JSON-Schreibkoordination. Regelprüfung, Katalogladen und Spieler-Vorschau werden aus den bereits
vorhandenen App-/Core-Seams bezogen. Damit bleibt der WPF-Editor ein weiterer Client derselben
Content- und Simulationsarchitektur – keine parallele Umsetzung.

Der bereits vorhandene `Game.Simulation.Wpf`-Client ist eine nützliche Referenz für
Projektkonfiguration, Desktop-Start und bestehende Preview-Seams. Er bleibt aber ein
Simulationsinspektor. Die Content Workbench wird nicht in ihn eingebaut und kopiert nicht seinen
umfangreichen Code-behind; sie folgt von Beginn an MVVM.

### 9.1 Projektaufteilung

```text
Game.ContentWorkbench        WPF-Ansichten, ViewModels, Commands und Navigation
  -> Game.ContentAuthoring   JSON-Arbeitsmappe, Entwürfe, Referenzindex, Staging/Write
    -> Game.App              GameDataCatalog, Validatoren, legale Spieler-Vorschau
      -> Game.Core           reine Modelle und Regeln
```

`Game.ContentAuthoring` ist eine Unity- und WPF-freie C#-Bibliothek. Sie verhindert, dass
JSON-Laden, Referenzsuche, Änderungen und Speichertransaktionen in ViewModels oder Code-behind
landen. `Game.ContentWorkbench` richtet sich nach MVVM: Views enthalten nur XAML/Bindungen,
ViewModels enthalten Darstellungszustand und Commands, Dienste bleiben außerhalb der UI.

Die `ContentWorkspace`-Arbeitsmappe lädt nur die im Manifest eingetragenen Dokumente. Sie hält pro
Dokument Quellpfad, Originalinhalt, geparste Arbeitskopie, Entwurfsänderungen, stabile IDs,
eingehende/ausgehende Referenzen und Diagnosen. Sie schreibt **nicht** über den
`GameDataCatalog`: Der Laufzeitkatalog bleibt die Validierungsinstanz, ist aber keine Garantie für
den Erhalt von Dokumentgrenzen und möglichst stabilen JSON-Diffs.

### 9.2 Konkrete Umsetzungsreihenfolge

1. **WPF-Shell, nur lesend.** `Game.ContentWorkbench` wird als `net8.0-windows`-Projekt in die
   Solution aufgenommen. Die erste Oberfläche enthält Dokumentbaum, typisierte Detailansicht,
   Referenzen/Diagnosen und Spielertext-/Szenenvorschau. Es gibt noch keinen Schreibvorgang.
2. **JSON-Arbeitsmappe und Referenzindex.** `Game.ContentAuthoring` lädt Manifest und Dokumente,
   indiziert IDs und Referenzen und hält Änderungen ausschließlich im Entwurf. Der erste Testumfang
   prüft Manifestgrenzen, stabile Referenzauflösung und unveränderte Dokumente.
3. **Texte und Visual-Referenzen editierbar machen.** Zuerst bearbeitbar werden Locales,
   Orts-/Aktionsbeschreibungen, Finding-/Analyse-/Archivtexte, Szenenfragmente und Visual-IDs.
   Die Vorschau nutzt die vorhandenen App-Resolver für unterschiedliche Wissensstände.
4. **Inhaltsketten innerhalb fester Archetypen.** Danach folgen Varianten, Zustandsprofile,
   Modifier, Aktionen, Voraussetzungen, Kosten, Ergebnisse/Effekte, Findings, Trigger und
   Konsequenzen. Der Referenzgraph wird aus den realen JSON-Kanten abgeleitet, nicht frei
   gezeichnet oder als eigene Regelsprache gespeichert.
5. **Sicheres JSON-Schreiben.** Geänderte Dokumente werden zuerst in einen temporären
   Staging-Ordner geschrieben. Parser, Schema-/Referenzvalidierung, `GameDataCatalog` und die
   gewählten Tests validieren diesen Stand. Erst bei Erfolg ersetzt ein atomarer Vorgang nur die
   tatsächlich geänderten JSON-Dateien; bei Fehler bleibt der Entwurf erhalten und der
   Arbeitsbaum unverändert.
6. **Erste produktive Inhaltsänderungen.** Die schreibende Workbench darf neue Varianten innerhalb
   fixer Archetypen sowie zugehörige Aktionen, Findings, Wissenspunkte, Texte, Visual-Referenzen,
   Folgen und Generator-Tags/Gewichte anlegen. Neue Archetypen, Ortsplatzierung, Savegames und
   Unity-Prefabdaten bleiben außerhalb des Umfangs.

Die Reihenfolge reduziert Risiko: Text und Vorschau beweisen zuerst die Arbeitsmappe und
Informationshonestät, bevor regeltragende Ketten und Speichervorgänge hinzukommen.

Unity bleibt der maßgebliche Ort für die endgültige visuelle Spielprüfung. Eine spätere
Unity-Editor-Brücke darf dieselben Workbench-Dokumente für Asset- und Präsentationsprüfung öffnen,
aber weder einen zweiten Datenspeicher noch eigene Regeln schaffen.

```text
Game.ContentWorkbench (WPF)
  -> Entwurfsmodell / JSON-Adapter
  -> bestehende Loader + GameDataCatalog + Validatoren
  -> read-only Game.App-Preview / SimulationSession-Fork
  -> strukturierte Diagnosen und Szenenvorschau
  -> expliziter, atomarer JSON-Write

Unity-Editor-Brücke (optional, später)
  -> derselbe JSON-Adapter und dieselben Validatoren
  -> nur Visual- und Präsentationsreview
```

Die konkrete UI-Technik ist damit noch nicht endgültig entschieden; die Schnittstellen- und
Datengrenzen sind es.

## 10. Lieferstufen – weiterhin nur Konzept

| Stufe | Ergebnis | Nicht enthalten |
| --- | --- | --- |
| E0: Autorenszenarien | 5–8 echte Änderungsaufgaben, Zeit-/Fehlerprotokoll und Owner-Entscheidung | Kein Tool-Code |
| E1: Lesen/Diagnostik | Katalogbaum, Referenzgraph, Text-/Szenen- und Spielerwissensvorschau | Schreiben, neue Simulation |
| E2: Sichere Inhaltsbearbeitung | Varianten, Zustände, Generator-Tags/Gewichte, Texte und Beziehungen mit atomarem Write | Ortsplatzierung, Save-Editor |
| E3: Bibliotheksbearbeitung | Szenarien, Aktionen, Findings, Szenen, Visuals mit Referenzrefactoring | Freier Node-Graph, C#-Regeln |
| E4: optionale Unity-Brücke | Visuelle Asset- und Präsentationsprüfung mit gleicher Datenbasis | Prefab-Regeln, paralleler Speicher |

E0 ist der nächste notwendige Schritt. Erst wenn die realen Authoring-Probleme bestätigen, dass
Referenzen, Texte und Vorschau wiederholt Zeit kosten, wird E1 beauftragt.

## 11. Owner-Entscheidungen vor einer Umsetzung

1. Soll die Workbench ausschließlich von dir genutzt werden oder auch von Content Designerinnen
   und Designern? Das bestimmt die erforderliche Formularklarheit und Rechte-/Reviewtiefe.
2. Reicht die Spieler-Szenenvorschau zunächst aus, oder ist früh eine Unity-Assetansicht für
   sinnvolle Präsentationsprüfung nötig?
3. Welche fünf häufigsten Änderungen sollen E0 konkret messen (z. B. Ort, Finding-Kette,
   Fraktionswarnung, verzögerte Folge, Text-/Visual-Revision)?
4. Ab welchem Zeitpunkt gelten stabile IDs als save-relevant und benötigen Migrationen?
5. Soll die erste schreibende Version nur Varianten/Inhalte innerhalb bestehender Archetypen
   ändern dürfen oder auch die gesamte wiederverwendbare Bibliothek?

Bis diese Entscheidungen getroffen sind, sind Templates, Katalogvalidierung, die Tutorialroute und
gezielte Spieler-Previews der richtige Produktionsweg.

## 12. Archetypen: bewusst nicht Teil der ersten Editor-Version

Die Archetypen sind in der ersten Workbench **fest**. Autorinnen und Autoren wählen einen
vorhandenen Archetyp und erstellen darin neue Varianten, Zustandsprofile, Aktionen, Findings,
Folgen und Texte. Dadurch lässt sich echte Inhaltsvielfalt schaffen, ohne die gemeinsame
Erkundungsfrage, Zustandslogik und Validierung eines Archetyps bei jeder Änderung neu zu erfinden.

Beispiele für die erste Version:

- ein neues `investigation-site` mit eigener Variante, Fundkette und Analyse;
- ein neuer `route-obstacle` mit anderer Aktion, Projektfolge und Konsequenz;
- ein neuer `contact-site` mit eigener Kommunikation, Evidenz und Fraktionsreaktion.

Ein tatsächlich neuer Archetyp – etwa ein Ritualplatz mit einer neuen dominanten
Erkundungsfrage – ist eine spätere, eigene Architekturentscheidung. Vorher muss geprüft werden,
welche heutigen Core-/App- und Validator-Annahmen noch an festen Archetypen hängen. Das Zielbild
bleibt auch dann: Archetypdefinition, erlaubte Zustände/Aktionen und Präsentationsvertrag liegen
in JSON, nicht in C# oder Prefabs. Dieser Ausbau wird aber erst nach praktischer Erprobung der
festen Archetypen beauftragt.

## 13. Erfolgskriterien

Die Content Workbench ist erst dann erfolgreich, wenn eine reale Autorin oder ein Autor:

- einen neuen Ortsinhalt innerhalb eines vorhandenen Archetyps aus gültigen JSON-Bausteinen
  erzeugen kann;
- nachvollziehen kann, welche Aktion welches Finding/Ereignis/welche Folge auslöst;
- fehlende Texte, Visuals und Referenzen vor dem Playtest findet;
- sicher prüfen kann, was ein Spieler in einem bestimmten Wissenszustand sieht;
- nur kleine, valide und gut reviewbare JSON-Diffs erzeugt; und
- dafür weder Gameplay-Code noch Unity-Prefabs ändern muss.

Sie ist nicht erfolgreich, wenn sie die vorhandenen Typen und Validatoren dupliziert, eine zweite
Simulation aufbaut oder den Erstellungsprozess mit einem unprüfbaren Node-Graphen komplizierter
macht.
