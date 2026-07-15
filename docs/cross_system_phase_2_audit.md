# Cross-System Phase 2 Audit

Status: **Complete — 2026-07-15**

Scope: Block 2.0 of `cross_system_phase_2_gameplay_plan.md`. This record describes the state
before Phase-2 implementation; it creates no gameplay rules.

## Confirmed Architecture Boundary

The shared interaction pipeline remains in `Game.Core` and `Game.App`. It must not gain a special
case for a location ID, a variant, `LocationKind`, Unity or WPF. A concrete location is resolved
through this hierarchy:

```text
shared command / requirements / effects
  -> registered archetype interaction flow
    -> scenario profile action surface
      -> variant presentation and generated instance state/context
```

An archetype flow is code owned by `Game.App`; it expresses the archetype's recognisable question
and legal decision phases. JSON owns the concrete action IDs, gates, state values, texts, effects
and generated-context candidates. A variant or generated instance never selects a new code path.

## Foundation Status

Blocks 0–9 of `cross_system_foundation_implementation_plan.md` are implemented. Block 10 has its
implementation complete and awaits the owner’s review/playtest release decision. The only
foundation wording that still described a pending runtime option resolver is moved to Phase 2.

## Current Authoring Inventory

| Area | Current content | Audit result |
|---|---|---|
| Slice scenario profiles | Route Obstacle, Investigation Site, Territorial Marker, Containment Site, Contact Site, Hazard Site, Natural Phenomenon | All seven current archetypes now have a catalog-validated Scenario Profile and playable-chain authoring. |
| State profiles | route crossing, marked grave, border warning, sealed gate, first contact | Interaction, operational and presence values are validated, but currently only as valid runtime values. |
| Action vocabulary | observation, inspection, assessment, investigation, documentation, marking, leaving, bypass, crossing, repair, opening, contact, withdrawal and related tags | Suitable base vocabulary exists; `continue`, `secure`, `enter`, `defer` and archetype-specific follow-up composition remain to author. |
| Outcomes/effects | state changes, supplies/morale, routes, projects, evidence, triggers, consequences | Shared effect pipeline exists and already persists state/processes. |
| WPF runner | inspect, local scout, visible location action, project advance, scripted commands and World-truth/trace inspection | Uses the shared session/query path; it contains no per-location action button. Directional controls are a later Phase-2 block. |

## Current Slice Paths and Gaps

| Archetype | Existing path | Required Phase-2 continuation |
|---|---|---|
| Route Obstacle | assess → bypass / temporary passage / crossing / repair project | Repaired, provisional and risky states must expose the applicable next access choices through the Route Obstacle flow. |
| Investigation Site | inspect → document / offering / disturbance | Inspection, respect, recovery and disturbance need state/context-led continuations and a safe defer/return path. |
| Territorial Marker | observe → interpret / cross | Interpretation must make respect, communication or deliberate crossing choices follow from known evidence and current marker state. |
| Containment Site | observe/inspect seal → context-discovered opening | An opened or breached state currently has no data-driven continuation for investigation, local interior scouting, securing or leaving. |
| Contact Site | observe → cautious approach / communicate / withdraw | Contacted, invited and closed states need their own follow-up surface without a first-contact variant branch. |

| Hazard Site | observe then assess | Assessment must reveal avoid, traverse, safe-route and specialist containment choices without a hazard-variant branch. |
| Natural Phenomenon | observe from distance then approach | Approaching must reveal survey and mapping choices with a specialist gate where appropriate. |

## Implemented Resolution

`LocationScenarioActionResolver` validates interaction, operational and presence state against
the selected State Profile, then delegates the action surface to the registered archetype flow.
That flow combines shared actions, bounded initial actions, knowledge-context rules and
state-gated rules. A state-changing action can therefore expose its authored legal continuation
without a location, variant, `LocationKind`, Unity or WPF branch.

## Block 2.1 Contract

1. Add a registered `Game.App` archetype-flow policy selected only from
   `ScenarioProfile.ArchetypeId`.
2. Add compatible Scenario Profile state-action rules for interaction, operational and presence
   channel values.
3. Validate every state value and action reference against its selected archetype/state profile.
4. Re-query the same player option contract after every command and world-state update.
5. Test both boundaries: two instances of one archetype can differ by state/context without
   variant code, while two archetypes follow different registered policies.

## Risks and Follow-up

- The currently exercised world objects remain bridge, gate and grave. The other four authored
  profiles require runner/Unity scenario coverage before human review, but their interaction
  chains use the same archetype-flow contract.
- A generated context remains World Truth until earned evidence supports player-facing disclosure;
  state gates must not become an accidental hidden-context leak.
- The owner must review any player-facing action vocabulary and flow wording before the full Slice
  chains are treated as final.
