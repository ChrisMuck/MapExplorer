# Base Camp Screen — scene setup

The base-camp screen is a **separate full-screen UIDocument** shown only when the expedition is at
the base. It is wired to the real simulation (`UnityHexMapView`) and sends commands through the
existing `Request*FromUi` seam — it never mutates world state.

## One-time scene wiring (manual)
1. Create an empty GameObject, e.g. `BaseCampScreen`.
2. Add a **UI Document** component:
   - **Source Asset** = `BaseCampScreen.uxml`.
   - **Panel Settings** = a Panel Settings asset whose **Sort Order is higher** than the expedition
     screen's, so the base screen renders on top when open.
3. Add the **`BaseCampScreenController`** component to the same GameObject.
4. Leave it enabled. The controller starts hidden (`display: none`) and shows itself when opened.

## How it opens / closes
- In-game: on the base field, finish the expedition ("Expedition abschließen"), then the action-bar
  button **"⌂ Basislager"** opens the screen (`UnityHexMapView.RequestOpenBaseCampFromUi()` →
  `BaseCampScreenController.Open()`).
- The title-bar **✕** (`btn-close`) or a successful "Expedition aufbrechen →" hides it again.

## What is wired vs. placeholder (first increment)
- **Team** — pool, person detail (level/skills/traits/gear/bio from the roster), team compose.
- **Base actions** — Rekrutieren / Ingenieur / +Vorräte (Team header), Heilen (person detail),
  "Einen Tag vergehen lassen" (Aufbruch) → real commands, spend knowledge + advance time.
- **Aufbruch** — starts the next expedition from the selected team; unit stock, resource loadout and
  readiness numbers are UI-only placeholders for now.
- **Fraktionen / Archiv** — read-only from `FactionState` / `Base.ArchiveEntries` + scout reports.
- **Basis ausbauen / Wissen auswerten** — mockup layout only (roadmap, not yet functional).

## Fonts
`.serif / .sans / .mono` fall back to the default font unless you assign Font Assets (see the
original design README). Cosmetic only.
