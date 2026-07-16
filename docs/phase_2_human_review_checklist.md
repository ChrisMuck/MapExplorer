# Phase 2 Human Review Checklist

Status: **Accepted for Phase-2 closure; detailed game-feel tuning deferred**
Integration: Phase-2 implementation merged into `master`; closure recorded on
`codex/close-phase-2`

This review is the final manual gate in Block 2.9. Automated tests establish deterministic rule
parity; they do not decide whether the chains are readable, useful or enjoyable.

## Preparation

1. Build without an app host: `dotnet build src/Game.Simulation.Wpf/Game.Simulation.Wpf.csproj --no-restore /p:UseAppHost=false`.
2. Start the WPF runner manually and load each `tests/DevelopmentScenarios/scenario-*.json` file.
3. Open the matching generated or fixture state in Unity through the normal `Game.App` adapter.
4. Keep developer World Truth confined to the WPF causality/debug views. Compare the player-facing
   option IDs, availability and known requirement text in both clients.

## Archetype paths

For each of the seven initial archetypes, verify the chain below through its scenario-profile rules,
not through a location-kind or concrete-object branch.

| Archetype | WPF proof path | Human question |
|---|---|---|
| route-obstacle | `scenario-claimed-crossing.json` | Is the crossing choice legible, and can the player leave or defer when repair is gated? |
| investigation-site | `scenario-investigation-respectful.json`, `scenario-investigation-disturbing.json` | Are respectful and disturbing choices distinguishable before committing? |
| territorial-marker | `scenario-territorial-respect.json`, `scenario-ignored-warning.json` | Is the warning understood without revealing hidden faction truth? |
| containment-site | `scenario-sealed-containment.json` | Does opening create an immediate, understandable investigate/secure/leave decision? |
| contact-site | `scenario-contact-choice.json`, `scenario-contact-promise-expired.json` | Are contact commitments and their later consequences clear? |
| hazard-site | `scenario-hazard-safe-route.json` | Does a missing specialist constrain the choice without creating a dead end? |
| natural-phenomenon | `scenario-natural-phenomenon-survey.json` | Does observation lead naturally to approach/survey/map choices? |

## Cross-system paths

- Run `scenario-directional-lead.json`, `scenario-directional-two-scout-team.json`,
  `scenario-directional-injured-low-confidence.json` and `scenario-directional-overdue.json`.
  Confirm local scouting and directional scouting feel distinct and never expose exact hidden
  coordinates.
- Run `scenario-unobserved-remote-change.json`. Confirm Unity retains the last known location
  condition until the expedition earns a new observation.
- Run `scenario-neutral-external-crisis.json`. Confirm the crisis is answerable without requiring a
  faction reaction that did not cause it.
- Use WPF directional movement buttons to reach one adjacent hex. Confirm the normal movement cost,
  terrain rejection and knowledge update match Unity.
- For each state-changing action, confirm the refreshed UI offers a meaningful continuation plus a
  leave, mark or defer choice where logically possible.
- Inspect causality in WPF: action, outcome, state transition, world trigger, scheduled consequence,
  warning and response should be explainable in order.

## Sign-off record

The owner reviewed representative scenarios and explicitly accepted the functional Phase-2 scope.
This sign-off does not claim a final wording, balance or UX pass over every scenario; those remain
normal later playtest work.

- Reviewer: project owner
- Date: 2026-07-16
- Scenario result: representative tested scenarios function as intended for Phase-2 closure
- Cross-client result: automated shared-contract/parity gates accepted as the current release gate;
  further hands-on WPF/Unity comparison remains desirable during later playtests
- Game-feel decision: sufficient to continue; fine tuning is deferred
- Follow-up issues: improve decision context through the shared situational scene-description
  system; revisit wording, costs and flow clarity during subsequent playtests

**Decision:** Block 2.9 and Phase 2 are complete. The next player-facing priority is the
knowledge-honest scene-description foundation, because readable situational context is necessary
for meaningful decisions.
