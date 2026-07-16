# Phase 2 Human Review Checklist

Status: **Pending project-owner review**  
Branch: `codex/phase-2-archetype-flows`

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

Record the date, reviewer and observations here before marking Phase 2 complete.

- Reviewer: pending
- Date: pending
- WPF result: pending
- Unity result: pending
- Game-feel decision: pending
- Follow-up issues: pending

Do not mark Block 2.9 or Phase 2 complete until the project owner has performed this review.
