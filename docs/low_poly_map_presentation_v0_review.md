# V0-Stilprototyp: Stand, Prüfanleitung und Dokumententwurf

Status: **Entwurf zur Owner-Prüfung (Tor G0); in Unity noch nicht ausgeführt**

Zugehörige Dokumente:

- `low_poly_map_presentation_concept.md`
- `low_poly_map_presentation_implementation_plan.md` (Stufe V0)

## 1. Was V0 enthält

Ein eigenständiger Prototyp unter `UnityHexMapView/Assets/MapStyle/`. Er ist nicht an `GameState`
angebunden, enthält keine Spielregeln und verändert keine bestehende Szene oder Datei.

| Datei | Zweck |
| --- | --- |
| `Shaders/MapStyleLit.shader` | URP-Shader: Vertexfarben, Flat-Shading, Schatten, Zusatzlichter, Wissensparameter `_Pale` |
| `Scripts/MapStyleMeshBuilder.cs` | Low-Poly-Grundformen (Box, Prisma/Kegel, Ikosaeder, Kuppel, Ring) mit harten Kanten |
| `Scripts/MapStyleTerrain.cs` | Gelände mit Relief (Hügel, Berge, durchgehender Graben), Wissensfassung mit flachem Unbekanntem |
| `Scripts/MapStyleLocation.cs` | Teile mit Zustandsliste, Posen, weichem Übergang, Rauch, Licht, Staub, Wissensdarstellung, Archetyp-Ring |
| `Scripts/MapStyleLocations.cs` | Rezepte für Grab, Höhle, Dorf, Wachturm, Lager, Brücke, Expeditionsgruppe, Wald, Route |
| `Scripts/MapStyleSandbox.cs` | Landschaft und Schaufenster, Orbit-Kamera, Tageszeit, Bedienfeld |
| `Editor/MapStyleSandboxEditor.cs` | Menü „Map Style/Create Sandbox Scene“ und Screenshot-Batch |

Umgesetzt aus dem Plan (V0):

1. Sandbox-Szene (wird per Menü erzeugt, siehe unten).
2. Gelände mit Relief, Graben, Hügeln, Bergen und Wasser.
3. Teile aus Grundformen: Brücke in vier Zuständen, Lager, Grab, Höhle, Dorf mit Zaun, Wachturm, Wald,
   Expeditionsgruppe, Route.
4. Wissensdarstellung: bestätigt, berichtet (blass), unbekannt (nur Steinhaufen); unbekanntes
   Gelände ist flach.
5. Archetyp-Ring pro Ort (Farben nach Konzept 9.2; die Symbole sind durch unterschiedlich viele
   Lücken im Ring nur angedeutet).
6. Orbit-Kamera: Ziehen oder Q/E dreht, Mausrad zoomt, die Kamera bewegt sich nie von selbst.
7. Dokumententwurf: Abschnitt 4 dieses Dokuments.

## 2. Prototyp starten

1. Unity 6000.5.2f1 öffnen, Projekt `UnityHexMapView` laden und kompilieren lassen.
2. Menü **Map Style → Create Sandbox Scene** ausführen. Das legt
   `Assets/MapStyle/Scenes/MapStyleSandbox.unity` an.
3. Szene öffnen und Play drücken.
4. Bedienung: Ansicht **Landschaft** oder **Schaufenster**, Brücke und Orte umschalten, Wissen
   wechseln, Tageszeit ziehen, „Zustände durchlaufen“ zeigt die Übergänge.

Referenzbilder lassen sich ohne Editorfenster rendern (Batch-Modus braucht eine gültige
Unity-Lizenz auf diesem Rechner):

```bash
Unity.exe -batchmode -projectPath UnityHexMapView -executeMethod MapExplorer.MapStyle.EditorTools.MapStyleSandboxEditor.CaptureBatch -captureDir <Ordner> -quit
```

Die Bilder entstehen als PNG (Landschaft in drei Wissensfassungen, Brücke in vier Zuständen, Nacht,
Schaufenster in mehreren Zuständen und Wissensstufen).

## 3. Was geprüft wurde und was nicht

Geprüft:

- Alle C#-Dateien des Prototyps kompilieren fehlerfrei gegen die Unity-6000.5.2f1-Assemblies
  (Kompilierprüfung außerhalb von Unity, null Fehler und Warnungen).
- Es wurden keine bestehenden Dateien geändert; `Game.Core`, `Game.App` und die Daten sind unberührt.

**Nicht geprüft** (der Batch-Start von Unity scheiterte an der Lizenz: „No valid Unity Editor license
found“; das lässt sich nur mit angemeldeter Unity-Lizenz beheben):

- Der Shader wurde noch nicht von Unity kompiliert. Mögliche Anpassungen: Schlüsselwörter für
  Schatten und Zusatzlichter, Schattenpass über `UsePass`.
- Das tatsächliche Aussehen (Farben, Lichtstärken, Proportionen) und die Leistung sind nicht
  gemessen. Lichtstärken und Umgebungslicht sind Startwerte und müssen nach Sicht justiert werden.
- Die Szene und die `.meta`-Dateien entstehen erst beim ersten Öffnen in Unity.

## 4. Fragen für die Freigabe (G0)

1. Passen Look, Farben und Lesbarkeit der Orte und Zustände?
2. Sind die Wissensgrade im Bild gut unterscheidbar (bestätigt, berichtet, unbekannt)?
3. Funktionieren Kamera, Drehung und Zoom angenehm?
4. Ist die Leistung ausreichend? (Bildzeit im Editor und im Build messen und im Plan nachtragen.)
5. Ist die Wahl „Teile als einzelne Objekte“ für die spätere Umsetzung akzeptabel, oder sollen Teile
   später zu einem Mesh pro Ort verschmolzen werden?

## 5. Entwurf der Dokumentänderungen (noch nicht angewendet)

Nach Freigabe von G0 vorzuschlagen, jeweils nur nach Owner-Review:

**`AGENTS.md`**

- Zeile 37 und Zeilen 109/110: „flat isometric 2D map“ ersetzen durch „low-poly 3D map with a
  rotatable orthographic camera and presentation-only visual relief; no permanent visible hex
  outlines“.
- Neue Regel: „Map visuals must follow the knowledge state; relief and objects must not reveal
  unknown world truth.“

**`docs/map_presentation_simplified_concept.md`**

- Kopf: Status auf „Teilweise abgelöst durch `low_poly_map_presentation_concept.md`“ ändern.
- Abschnitte 4 (flache Hexe, keine Höhe) und 5 (2D-Isometrie) mit Verweis auf das neue Konzept
  kennzeichnen; Abschnitte 1 bis 3 (verborgenes Raster, keine Hexlinien) bleiben gültig.

**`docs/ui_ux_concept.md`**

- Kartenbedienung (Zeile 588 ff.): Mausrad-Zoom beibehalten, Kameradrehung ergänzen.
- Zeilen 256 und 599: „flat isometric“ durch „low-poly 3D with rotatable camera“ ersetzen.

**`docs/technical_concept.md`**

- Zeilen 599, 614, 789 bis 804, 920 und 933: Beschreibung der Kartenansicht anpassen; Abschnitt
  zur Koordinatenumrechnung bleibt, die Projektion ist nun orthografisch mit Orbit.
