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

## Current wiring
- **Team** - pool, person detail (level/skills/traits/gear/bio from the roster), selected team and
  healing/recruiting base actions.
- **Base actions** - recruit, request engineer, prepare supplies, heal selected member and advance
  base time through real commands. These actions spend Knowledge Points and/or advance world day.
- **Aufbruch** - starts the next expedition from the selected team. Porter/soldier unit stock,
  rations, medicine and readiness are backed by `BaseUnitStockState`, `ExpeditionReadiness` and the
  loadout overload of `StartNewExpeditionCommand`.
- **Basis ausbauen** - persistent upgrade cards backed by `BaseUpgradeState` /
  `StartUpgradeCommand`.
- **Wissen auswerten** - evaluation queue backed by `EvaluationQueueState`, `AdvanceBaseTimeCommand`
  and `EvaluateKnowledgeItemCommand`.
- **Fraktionen** - read-only from player-known `FactionState` entries only; unknown factions are
  not listed merely because they exist in World Truth.
- **Archiv** - typed archive entries plus scout reports, with filters and search.

## Still prototype-level
- Costs, durations, readiness thresholds and upgrade effects are tuning placeholders.
- More generated expedition discoveries still need to feed the evaluation/archive loop.
- Final portraits, archive thumbnails and polished content are intentionally not in scope yet.
- Keep new rules in `Game.Core` / `Game.App`; this controller should remain a presentation bridge.

## Fonts
`.serif / .sans / .mono` fall back to the default font unless you assign Font Assets (see the
original design README). Cosmetic only.
