# Expedition-Screen (Layout 3a) — Unity UI Toolkit Startpaket

Übertragung des HTML-Mockups **3a** (Feldbuch, aufklappbares Panel) nach Unity.

## Dateien
- `ExpeditionScreen.uxml` — Layout (Top-Bar, linkes Kontext-Panel, Menü-Rail, angedocktes Panel, Aktionsleiste)
- `ExpeditionScreen.uss` — Styles/Tokens (Farben, Abstände, Typo) — entspricht den Inline-Styles im Mockup
- `ExpeditionScreenController.cs` — Auf-/Zuklappen der Rail + Sektions-Umschaltung

## Einrichtung
1. Ordner `Assets/UI/` im Projekt anlegen und die drei Dateien hineinlegen.
2. In der Szene ein leeres GameObject erstellen → Component **UI Document** hinzufügen.
3. Beim UI Document ein **Panel Settings**-Asset zuweisen (Assets → Create → UI Toolkit → Panel Settings Asset) und **Source Asset** = `ExpeditionScreen.uxml`.
4. `ExpeditionScreenController.cs` auf dasselbe GameObject legen.
5. Play drücken — Rail-Buttons rechts klappen das Panel auf/zu.

## Wichtig: Die Karte ist NICHT Teil dieses UI
Die `.map-area` in der UXML ist **transparent**. Sie ist nur ein Overlay-Bereich.
Deine Hex-Weltkarte rendert darunter — z.B.:
- eigene Kamera + Tilemap/Mesh/Sprites, oder
- eine RenderTexture, die hinter dem UIDocument liegt.

Das UIDocument sollte einen transparenten Hintergrund haben (Panel Settings → Clear Color aus), damit die Karte durchscheint. Alternativ das Interface auf einem separaten, überlagerten UIDocument fahren.

## Fonts zuweisen
Im Mockup: **Spectral** (Serifen-Erzähltext), **IBM Plex Sans** (UI), **IBM Plex Mono** (Daten/Koordinaten).
1. TTFs importieren → Rechtsklick → **Create → Text → Font Asset** (SDF).
2. In `ExpeditionScreen.uss` bei den Klassen `.serif`, `.sans`, `.mono` je eine Regel ergänzen:
   ```
   .serif { -unity-font-definition: url("project://database/Assets/UI/Fonts/Spectral-SDF.asset"); }
   .sans  { -unity-font-definition: url("project://database/Assets/UI/Fonts/IBMPlexSans-SDF.asset"); }
   .mono  { -unity-font-definition: url("project://database/Assets/UI/Fonts/IBMPlexMono-SDF.asset"); }
   ```
   (Die Klassen `serif`/`sans`/`mono` sind in der UXML bereits gesetzt.)

## Platzhalter-Grafiken
Die grauen `.avatar`-Kästchen sind Portrait-/Thumbnail-Platzhalter. Ersetze sie
später durch `<ui:VisualElement>` mit `background-image` oder ein `<ui:Image>`.
CSS-Streifenmuster aus dem Mockup gibt es in USS nicht — nutze eine Textur.

## State-Anbindung (später)
Der Controller steuert nur Darstellung. Gemäß `UI_UX_CONCEPT.md` Abschnitt 19:
- Werte (Vorräte, Moral, MP, Berichte …) aus deinem `KnowledgeState`/Model lesen und
  auf die benannten Labels schreiben (`value-supplies`, `coord`, `side-panel-title`, …).
- Buttons (Bewegen, Späher senden, Tag beenden) rufen Commands in deiner
  Application/Core-Schicht auf — nicht direkt Weltzustand ändern.

## Hinweis zur Unity-Version
Getestete USS-Features (translate, transition, :hover, ScrollView) laufen ab
Unity 2021 LTS. `gap` wurde bewusst NICHT verwendet (nutzt `margin`), damit es
auch mit älteren Versionen kompatibel ist.
