# Cross-System JSON Authoring Schema

## Status and Scope

This document defines the target authoring structure for the exploration, location, scouting,
faction and world-consequence systems. It is a data contract, not an instruction to expose
objective World State to the player UI.

It deliberately separates three kinds of data:

1. **Authoring data** — editable JSON definitions, reusable across worlds.
2. **Generated World State** — concrete locations, factions, claims, connections and resolved
   branches created from the authoring data by the WorldGenerator.
3. **Saved game state** — the changed world, player Knowledge State and Player Notes after play.

The existing files in `UnityHexMapView/Assets/StreamingAssets/GameData` remain the migration
starting point. This schema adds the missing contracts but does not require the current loader to
support every new field yet. Loader and Core changes belong to a later implementation block.

## Non-Negotiable Data Rules

- Static location content never contains a concrete generated faction ID, a map coordinate, a
  territory ID, a claim decision or a resolved consequence branch.
- A static faction profile never contains its generated territory, current need, current Trust,
  Anger or Fear, a known player action or an assigned location.
- `WorldState` stores objective generated facts. `KnowledgeState` stores only earned evidence and
  last-known information. `PlayerNotes` stores hypotheses and personal markers.
- Findings, reports and location inspections create knowledge. They are not a material resource
  loop. Knowledge Points are the sole general base and trade currency. Supplies and medicine
  remain separate expedition consumables: supplies impose return pressure and medicine treats
  injuries or enables aid at locations. Neither creates a harvesting, processing or stockpile loop.
- Every reference is an ID validated at load time. Tags are reusable selectors, never direct
  hidden conclusions.
- IDs are stable, lowercase kebab-case and are never reused for a different meaning.

## Common Document Envelope

Every authored document uses the same envelope:

```json
{
  "documentType": "location-scenario-profiles",
  "schemaVersion": 2,
  "contentVersion": 1,
  "items": []
}
```

- `documentType` selects the loader and validation rules.
- `schemaVersion` changes only when the field structure changes.
- `contentVersion` changes when compatible content changes.
- `items` contains stable authored definitions.

A future `game-data-manifest.json` lists the documents deliberately and makes dependencies visible:

```json
{
  "documentType": "game-data-manifest",
  "schemaVersion": 1,
  "contentVersion": 1,
  "documents": [
    "Locations/Archetypes/archetypes.json",
    "Locations/StateProfiles/state-profiles.json",
    "Locations/ScenarioProfiles/scenario-profiles.json",
    "Locations/Actions/actions.json",
    "Locations/OutcomeTables/outcome-tables.json",
    "Locations/ContentProfiles/content-profiles.json",
    "Findings/findings.json",
    "World/evidence-definitions.json",
    "World/context-definitions.json",
    "World/consequence-definitions.json",
    "World/situation-definitions.json",
    "Factions/factions.json",
    "Factions/signatures.json",
    "Factions/territorial-rules.json",
    "Factions/reaction-rules.json",
    "Factions/offers.json",
    "Scouting/mission-types.json",
    "Scouting/focuses.json",
    "Scouting/report-templates.json",
    "World/world-generation-rules.json"
  ]
}
```

The manifest is a target improvement. Until it is implemented, only files with an already supported
`documentType` may live in the runtime data directory.

## Target Directory Layout

```text
Assets/StreamingAssets/GameData/
  game-data-manifest.json                         # target, explicit load order
  Locations/
    Archetypes/archetypes.json                    # existing
    Variants/variants.json                        # existing
    StateProfiles/state-profiles.json             # new
    ScenarioProfiles/scenario-profiles.json       # new
    Modifiers/modifiers.json                      # existing
    Actions/actions.json                          # existing, expanded
    OutcomeTables/outcome-tables.json             # existing, expanded
    ContentProfiles/content-profiles.json         # existing, presentation only
  Findings/findings.json                          # new
  Scouting/
    mission-types.json                            # existing
    focuses.json                                  # existing
    outcome-tables.json                           # existing
    report-templates.json                         # existing, expanded
  Factions/
    factions.json                                 # existing, expanded profile shape
    signatures.json                               # existing
    territorial-rules.json                        # rename/migration from territory-entry-rules.json
    reaction-rules.json                           # existing, expanded
    offers.json                                   # new, replaces hardcoded offer definitions
    memory-definitions.json                       # new
  World/
    evidence-definitions.json                     # existing, expanded
    context-definitions.json                      # new
    consequence-definitions.json                  # existing, expanded
    situation-definitions.json                    # new
    world-trigger-definitions.json                # existing
    world-generation-rules.json                   # new
    world-susceptibilities.json                   # new, optional after Slice
```

## Location Authoring

### 1. Archetypes

`archetypes.json` defines the seven reusable questions of a place. It contains no concrete faction
or outcome.

```json
{
  "id": "route-obstacle",
  "question": "Wie kommen wir hindurch, darum herum oder sicher zurück?",
  "defaultActionIds": ["action-inspect", "action-leave"],
  "allowedAnchorKinds": ["edge", "point"],
  "allowedContextTags": ["structural-failure", "deliberate-destruction", "flood-damage"],
  "allowedConsequenceFamilies": ["route-or-place-change", "social-aftereffect", "knowledge-trail"]
}
```

Initial IDs are `route-obstacle`, `investigation-site`, `territorial-marker`, `contact-site`,
`hazard-site`, `containment-site` and `natural-phenomenon`. There is no `resource-site`.

### 2. State Profiles

`state-profiles.json` defines valid state channels and values for a reusable kind of place. It
keeps location states data-driven without making every archetype a bespoke class.

```json
{
  "id": "state-route-crossing",
  "archetypeId": "route-obstacle",
  "channels": {
    "interaction": { "initial": "untouched", "values": ["untouched", "observed", "inspected"] },
    "operational": {
      "initial": "blocked",
      "values": ["blocked", "provisional", "risky", "open", "repaired", "destroyed", "restricted"]
    },
    "presence": { "initial": "unknown", "values": ["unknown", "watched", "guarded", "abandoned"] }
  }
}
```

`unknown` is a player Knowledge State only when used there. A physical state profile must not use
it to mean that the world itself is undecided.

### 3. Scenario Profiles

`scenario-profiles.json` is the reusable composition layer. It connects an archetype, variant,
state profile, candidate evidence, candidate findings and neutral consequence families. It is the
only location document that describes a complete reusable situation; the content profile remains
presentation-only.

```json
{
  "id": "scenario-broken-crossing",
  "archetypeId": "route-obstacle",
  "variantId": "broken-bridge",
  "stateProfileId": "state-route-crossing",
  "contentProfileId": "content-broken-crossing",
  "worldgen": {
    "anchorKinds": ["edge"],
    "terrainTagsAny": ["river-crossing", "ravine-crossing"],
    "initialModifierPoolIds": ["unstable", "weather-damaged", "deliberately-closed"],
    "claimEligibility": "if-inside-current-territory"
  },
  "actionSet": {
    "sharedActionIds": ["action-inspect", "action-leave"],
    "initialAdditionalActionIds": ["action-assess-crossing"],
    "maximumVisibleAdditionalActions": 3,
    "contextActionRules": [
      {
        "whenKnownContextTagsAny": ["structural-failure"],
        "addActionIds": ["action-construct-temporary-passage"],
        "knownRequirementDisclosure": "show-disabled-when-missing"
      },
      {
        "whenHiddenContextTagsAny": ["deliberate-destruction"],
        "addActionIds": ["action-rebuild-bridge"],
        "knownRequirementDisclosure": "hidden-until-supported"
      }
    ]
  },
  "evidencePoolIds": ["evidence-removed-supports", "evidence-fresh-marker"],
  "findingPoolIds": ["finding-riverway-construction-sample"],
  "consequencePoolIds": ["consequence-route-change"],
  "connectionTags": ["crossing", "old-trade-route", "structural-signature"]
}
```

`claimEligibility` only permits the generator to evaluate a claim. It never selects a faction or
asserts that a claim exists.

### 4. Actions and Outcome Tables

The existing action and outcome-table documents remain reusable. They gain commitment and
disclosure fields, so the UI can distinguish a known missing prerequisite from an unknown risk.

```json
{
  "id": "action-construct-temporary-passage",
  "label": "Seilübergang anbringen",
  "description": "Eine kleine Gruppe kann die Schlucht danach vorsichtiger überqueren.",
  "commitment": "day-operation",
  "hardRequirements": [
    {
      "kind": "RolePresent",
      "role": "Pioneer",
      "disclosure": "known",
      "unmetReason": "Erfordert eine Pionierin oder einen Pionier."
    }
  ],
  "riskProfile": { "baseRisk": 20, "confidence": "assessed" },
  "outcomeTableId": "outcome-construct-temporary-passage",
  "repeatPolicy": "once-per-state",
  "actionTags": ["repair", "provisional-passage"]
}
```

`commitment` is `immediate`, `day-operation` or `project`. A project adds `durationEstimate`,
`suspensionPolicy` and `completionEffects`; partial progress is runtime state.

Effects use neutral targets. They must not name a static faction:

```json
{
  "id": "effect-route-repair-observed",
  "kind": "raise-world-trigger",
  "referenceId": "trigger-access-changed",
  "targetSelector": "generated-affected-context",
  "text": "Die Veränderung kann von Menschen in der Umgebung bemerkt werden."
}
```

### 5. Content Profiles

The existing `content-profiles.json` remains presentation-only: titles, descriptions, state flavor,
journal wording and visual asset IDs. It does not own actions, outcome rules, claims or factions.

## Knowledge, Findings and Evidence

### 1. Findings

`findings.json` defines physical or documented material that becomes an analysis item only after a
return to base. A finding is not a trade good or material resource.

```json
{
  "id": "finding-unknown-river-metal",
  "category": "sample",
  "sourceTags": ["river-crossing", "old-construction"],
  "fieldDescription": "Ein ungewöhnlich leichtes Metallstück steckt zwischen alten Befestigungen.",
  "requiresPhysicalReturn": true,
  "analysis": {
    "durationDays": { "min": 2, "max": 4 },
    "knowledgePoints": { "min": 2, "max": 6 },
    "archiveKind": "insight",
    "explanation": "Die Legierung wurde nicht mit den üblichen Werkzeugen der Küste verarbeitet.",
    "unlocks": [
      { "kind": "evidence-thread", "id": "thread-old-construction" },
      { "kind": "specialist-reading", "id": "reading-unusual-metallurgy" }
    ]
  },
  "repeatPolicy": "once-per-world"
}
```

### 2. Evidence

`evidence-definitions.json` describes player-facing evidence, not facts. It supports report quality
without letting a veteran scout reveal hidden truth.

```json
{
  "id": "evidence-removed-supports",
  "category": "structural",
  "subjectKinds": ["location", "route"],
  "sourceKinds": ["location-inspection", "local-scout", "directional-scout"],
  "threadTags": ["structural-signature", "old-trade-route"],
  "presentation": {
    "basic": "Mehrere Träger fehlen; der Übergang wirkt nicht einfach nur alt.",
    "veteranScout": "Die fehlenden Träger wurden wahrscheinlich gezielt entfernt, doch der Grund bleibt offen.",
    "worldEvent": "An einem bekannten Übergang zeigen sich Spuren einer früheren gezielten Veränderung."
  },
  "symbolId": null,
  "reliability": "ordinary"
}
```

`symbolId` references a neutral signature definition. The WorldGenerator assigns signature sets to
actual factions; evidence never stores a hidden faction conclusion for the player.

## Generated Context, Consequences and Situations

### 1. Context Definitions

`context-definitions.json` supplies neutral hidden contexts that the generator may assign to a
location. It never assigns a faction.

```json
{
  "id": "context-deliberate-closure",
  "applicableArchetypeIds": ["route-obstacle", "containment-site"],
  "contextTags": ["deliberate-destruction", "restricted-access"],
  "discoverableBy": ["inspection", "local-scout", "engineer", "contact"],
  "candidateEvidenceIds": ["evidence-removed-supports"],
  "candidateActionTags": ["repair", "respect-warning", "request-passage"],
  "candidateConsequenceFamilies": ["social-aftereffect", "route-or-place-change"]
}
```

### 2. Consequences

`consequence-definitions.json` contains generic staged processes. At scheduling time the resolver
chooses and saves a branch; later stages never reroll history.

```json
{
  "id": "consequence-route-change",
  "family": "route-or-place-change",
  "triggerTagsAny": ["repair", "open", "destroy", "flood-damage"],
  "branches": [
    {
      "id": "local-use-returns",
      "weight": 70,
      "stages": [
        {
          "id": "early-signs",
          "delayDays": 2,
          "evidenceIds": ["evidence-fresh-traffic"],
          "deliveryChannels": ["local-scout", "world-report"],
          "situationCandidateIds": [],
          "worldEffects": [{ "kind": "change-route-use", "stateId": "used" }]
        },
        {
          "id": "aftermath",
          "delayDays": 5,
          "evidenceIds": ["evidence-route-context-changed"],
          "worldEffects": [{ "kind": "create-connection", "connectionTag": "renewed-access" }]
        }
      ],
      "endState": "persistent-changed-route"
    }
  ],
  "responseTags": ["investigate", "repair", "negotiate", "warn", "document"]
}
```

The initial families are `propagation`, `release`, `route-or-place-change`, `social-aftereffect`,
`knowledge-trail`, `rescue-or-assistance` and `external-world-crisis`. A branch has one primary
family and may transition to one secondary family only.

### 3. Situations

`situation-definitions.json` defines reusable player-facing direct situations. The generated
instance supplies the actual source, location, pressure and deadline.

```json
{
  "id": "situation-request-help-with-crossing",
  "kind": "request",
  "possibleSourceKinds": ["faction", "local-group", "individual"],
  "urgencyLabels": ["there-is-still-time", "soon", "immediate"],
  "responseOptions": [
    { "id": "accept", "actionTag": "assist" },
    { "id": "ask-for-time", "actionTag": "promise-return" },
    { "id": "offer-alternative", "actionTag": "organize-help" },
    { "id": "decline", "actionTag": "decline" }
  ],
  "resolutionTags": ["help-provided", "promise-kept", "promise-broken", "expired"]
}
```

An unknown faction source is presented as an unknown group, messenger, sign or patrol until a
credible identification is earned.

## Factions

### 1. Faction Profiles

`factions.json` defines reusable faction profiles. A generated faction instance picks one, receives
a name, territory, signature set and runtime state from the WorldGenerator.

```json
{
  "id": "profile-guarded-boundary-keepers",
  "presentation": {
    "label": "Zurückhaltende Grenzhüter",
    "portraitSetId": "portrait-set-boundary-keepers"
  },
  "primaryValue": "restraint",
  "secondaryValue": "secrecy",
  "primaryTaboo": "disturb-protected-place",
  "territorialRuleIds": ["rule-ask-before-crossing", "rule-do-not-map-inner-route"],
  "signaturePoolIds": ["signature-carved-posts", "signature-woven-bands"],
  "observationChannels": ["border-patrol", "renewed-marker", "traveller-rumour"],
  "contactStyle": "guarded",
  "reactionModeIds": ["observe", "signal", "contact", "restrict"],
  "initialContactStatus": "unknown"
}
```

Only `trust`, `anger` and `fear` are numeric runtime relationship values. Current need, fear,
shortage, disease or route damage is a generated World Situation, not an authored faction meter.

### 2. Signatures, Territorial Rules and Reactions

```json
{
  "id": "signature-carved-posts",
  "forms": ["carved-post", "cut-bark", "boundary-stone"],
  "evidenceTags": ["territory", "warning", "renewed-mark"],
  "visualSetId": "signature-set-carved-posts"
}
```

```json
{
  "id": "rule-ask-before-crossing",
  "discoverableBy": ["territorial-marker", "contact", "scout-report"],
  "restrictedActionTags": ["cross", "map", "repair"],
  "respectActionTags": ["request-passage", "withdraw", "communicate"]
}
```

```json
{
  "id": "reaction-repaired-access",
  "when": {
    "triggerTagsAny": ["repair", "infrastructure"],
    "requiresGeneratedContextAny": ["claimed", "watched", "restricted-access"],
    "requiresObservation": true
  },
  "modePool": ["observe", "signal", "contact", "restrict", "support"],
  "memoryTags": ["changed-access"],
  "deliveryDelay": "contextual"
}
```

`offers.json` moves current hardcoded faction offers into data. Offers reference profile rules and
generated contact state rather than a static location or a hardcoded concrete faction instance.

```json
{
  "id": "offer-knowledge-for-regional-account",
  "eligibleProfileTags": ["trade", "practical-help"],
  "requiresContactStatusAny": ["first-contact", "trusted"],
  "knowledgePointCost": 6,
  "effects": [{ "kind": "grant-contact-report", "reportTemplateId": "report-regional-account" }],
  "repeatPolicy": "once-per-faction"
}
```

`memory-definitions.json` supplies stable semantic memory IDs and their player-facing provenance.
Runtime faction memories record the generated faction instance, source event and day.

## Scouting

The existing scouting documents remain, with these additions:

- `mission-types.json` declares `scope` as `local-surroundings` or `directional-sector`.
- `focuses.json` uses `locations`, `routes`, `hazards`, `signatures`, `traces`, `witnesses` and
  `environmental-change`; it does not use a material-resource focus.
- `report-templates.json` provides quality bands (`inexperienced`, `trained`, `veteran`) and
  deliberately approximate directions, never a target hex.
- local reports may create a restricted outward lead, but may not reveal broad routes or exact
  remote places.

```json
{
  "id": "report-tracks-east",
  "scope": "local-surroundings",
  "qualityText": {
    "inexperienced": "Spuren führen offenbar weiter nach Osten.",
    "veteran": "Mehrere unterschiedlich alte Spuren führen nach Osten; sie wirken wie regelmäßiger Verkehr, nicht wie ein einzelner Durchgang."
  },
  "lead": { "kind": "directional", "direction": "east", "precision": "vague" }
}
```

## World Generation Rules

`world-generation-rules.json` owns reusable selection and validation rules, not concrete worlds.

```json
{
  "id": "slice-soft-links",
  "appliesTo": "vertical-slice",
  "minimumSoftConnections": 2,
  "allowedConnectionKinds": ["shared-signature", "finding-to-context", "route-to-place"],
  "forbidRequiredOrder": true,
  "forbidStaticFactionAssignment": true
}
```

Generated World State then records the concrete result:

```json
{
  "locationInstanceId": "location-17",
  "scenarioProfileId": "scenario-broken-crossing",
  "anchor": { "kind": "edge", "hexes": [[12, 8], [13, 8]] },
  "generatedContextTags": ["deliberate-destruction", "claimed"],
  "generatedClaim": { "factionInstanceId": "faction-2", "reasonTag": "restricted-access" },
  "resolvedConsequenceBranches": [],
  "state": { "operational": "blocked", "interaction": "untouched" }
}
```

This is save data, not a file in the authored content catalogue.

## Required Cross-Reference Validation

The content validator must reject:

1. unknown IDs in any reference field;
2. a scenario profile that combines incompatible archetype, variant, state profile or anchor kind;
3. action references that exceed the location's visible-action cap without a contextual condition;
4. a consequence that lacks an early observable stage before a severe impact stage;
5. a direct situation without a valid response option or resolution path;
6. a static location, action, consequence or finding that names a generated faction instance;
7. a faction profile with more than one primary value, more than one secondary value or more than
   one primary taboo;
8. a material-resource reward instead of knowledge, a finding, access, a report or another
   explicitly defined world/relationship effect;
9. a Vertical Slice generation rule with fewer than two soft connections;
10. a result that exposes hidden generated claim, context, exact target hex or resolved branch to
    `KnowledgeState` without a valid evidence source.

## Migration Notes

- Keep the current six location documents, but add `state-profiles.json` and
  `scenario-profiles.json`; do not overload `content-profiles.json` with rules.
- Expand the current evidence and consequence documents rather than creating per-location special
  code.
- Replace the legacy `Resource Site` terminology with findings, evidence and Knowledge Points.
- Migrate the current hardcoded `FactionInteractionDefinitions` offers and leverage definitions to
  `Factions/offers.json` and finding/evidence references.
- Rename `territory-entry-rules.json` to `territorial-rules.json` only when the loader migration
  can preserve old saves and references.
- Do not place target-only documents in the runtime data directory before the manifest and loaders
  understand their `documentType`.

## First Implementation Block After Schema Approval

1. Add the manifest-aware content loader and reference validator.
2. Add the new Location State Profile and Scenario Profile loaders.
3. Add Findings and data-driven faction offer loaders.
4. Extend evidence, consequences and situations with the staged contracts above.
5. Update WorldGenerator to create instances, claims, connections and resolved branches from these
   definitions.
6. Add a five-profile Vertical Slice fixture and tests for two generated soft connections.
