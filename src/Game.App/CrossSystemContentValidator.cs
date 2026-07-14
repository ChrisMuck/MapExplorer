#nullable enable
using System;
using System.Collections.Generic;
using Game.Core;

namespace Game.App
{

/// <summary>
/// Validates references that cross the boundary between reusable location content and the
/// world/faction definition sets. Individual loaders validate their own documents; this class
/// keeps a location effect from silently referring to a missing world definition at runtime.
/// </summary>
public static class CrossSystemContentValidator
{
    /// <summary>
    /// Validates every location effect that addresses the shared world contracts. Both bundles
    /// must already have passed their own loader validation.
    /// </summary>
    public static void Validate(LocationDataBundle locations, CrossSystemDataBundle crossSystem, CrossSystemAuthoringBundle? authoring = null)
    {
        if (locations == null) throw new ArgumentNullException(nameof(locations));
        if (crossSystem == null) throw new ArgumentNullException(nameof(crossSystem));

        var errors = new List<string>();
        foreach (var action in locations.Definitions.Actions.Values)
        {
            ValidateEffects(
                action.ProjectCompletionEffects,
                $"Action '{action.Id}' project completion",
                crossSystem,
                errors);
        }

        foreach (var table in locations.Definitions.OutcomeTables.Values)
        {
            foreach (var effects in table.EffectBundles)
            {
                ValidateEffects(effects, $"Outcome table '{table.Id}'", crossSystem, errors);
            }
        }

        if (authoring != null)
        {
            ValidateAuthoringDefinitions(locations, crossSystem, authoring, errors);
        }

        if (errors.Count > 0)
        {
            throw new LocationDataException("Cross-system content validation failed:\n - " + string.Join("\n - ", errors));
        }
    }

    private static void ValidateAuthoringDefinitions(
        LocationDataBundle locations,
        CrossSystemDataBundle crossSystem,
        CrossSystemAuthoringBundle authoring,
        ICollection<string> errors)
    {
        foreach (var profile in authoring.StateProfiles.Values)
        {
            if (!locations.Definitions.Archetypes.ContainsKey(profile.ArchetypeId))
            {
                errors.Add($"State profile '{profile.Id}' references unknown archetype '{profile.ArchetypeId}'.");
            }
        }

        foreach (var scenario in authoring.ScenarioProfiles.Values)
        {
            if (!locations.Definitions.Archetypes.ContainsKey(scenario.ArchetypeId)) errors.Add($"Scenario profile '{scenario.Id}' references unknown archetype '{scenario.ArchetypeId}'.");
            // The legacy Core variant definition intentionally does not retain its authored
            // archetype. The target scenario profile is the first typed owner of that pairing;
            // migration of the legacy DTO follows once profiles replace legacy variant routing.
            if (!locations.Definitions.Variants.ContainsKey(scenario.VariantId)) errors.Add($"Scenario profile '{scenario.Id}' references unknown variant '{scenario.VariantId}'.");
            if (!authoring.StateProfiles.TryGetValue(scenario.StateProfileId, out var stateProfile)) errors.Add($"Scenario profile '{scenario.Id}' references unknown state profile '{scenario.StateProfileId}'.");
            else if (stateProfile.ArchetypeId != scenario.ArchetypeId) errors.Add($"Scenario profile '{scenario.Id}' combines state profile '{scenario.StateProfileId}' with a different archetype.");
            if (!locations.Definitions.ContentProfiles.ContainsKey(scenario.ContentProfileId)) errors.Add($"Scenario profile '{scenario.Id}' references unknown content profile '{scenario.ContentProfileId}'.");
            foreach (var actionId in scenario.ActionSet.SharedActionIds.Concat(scenario.ActionSet.InitialAdditionalActionIds).Concat(scenario.ActionSet.ContextActionRules.SelectMany(rule => rule.ActionIds)))
            {
                if (!locations.Definitions.Actions.ContainsKey(actionId)) errors.Add($"Scenario profile '{scenario.Id}' references unknown action '{actionId}'.");
            }
            foreach (var modifierId in scenario.InitialModifierPoolIds)
            {
                if (!locations.Definitions.Modifiers.ContainsKey(modifierId)) errors.Add($"Scenario profile '{scenario.Id}' references unknown modifier '{modifierId}'.");
            }
            foreach (var evidenceId in scenario.EvidencePoolIds)
            {
                if (crossSystem.Evidence.Find(evidenceId) == null) errors.Add($"Scenario profile '{scenario.Id}' references unknown evidence '{evidenceId}'.");
            }
            foreach (var findingId in scenario.FindingPoolIds)
            {
                if (!authoring.Findings.ContainsKey(findingId)) errors.Add($"Scenario profile '{scenario.Id}' references unknown finding '{findingId}'.");
            }
            foreach (var consequenceId in scenario.ConsequencePoolIds)
            {
                if (crossSystem.FindConsequence(consequenceId) == null) errors.Add($"Scenario profile '{scenario.Id}' references unknown consequence '{consequenceId}'.");
            }
        }

        foreach (var context in authoring.Contexts.Values)
        {
            foreach (var archetypeId in context.ApplicableArchetypeIds)
            {
                if (!locations.Definitions.Archetypes.ContainsKey(archetypeId)) errors.Add($"Context '{context.Id}' references unknown archetype '{archetypeId}'.");
            }
            foreach (var evidenceId in context.CandidateEvidenceIds)
            {
                if (crossSystem.Evidence.Find(evidenceId) == null) errors.Add($"Context '{context.Id}' references unknown evidence '{evidenceId}'.");
            }
        }

        foreach (var consequence in crossSystem.Consequences.Values)
        {
            foreach (var stage in consequence.Branches.SelectMany(branch => branch.Stages))
            {
                foreach (var situationId in stage.SituationDefinitionIds)
                {
                    if (!authoring.Situations.ContainsKey(situationId))
                    {
                        errors.Add($"Consequence '{consequence.Id}' stage '{stage.Id}' references unknown situation '{situationId}'.");
                    }
                }
            }
        }

        foreach (var offer in authoring.FactionOffers.Values)
        {
            foreach (var profileTag in offer.EligibleProfileTags)
            {
                if (!crossSystem.FactionProfiles.All.Any(profile => profile.Values.Contains(profileTag, StringComparer.Ordinal)))
                {
                    errors.Add($"Faction offer '{offer.Id}' references unknown faction profile tag '{profileTag}'.");
                }
            }
            foreach (var effect in offer.Effects)
            {
                if (effect.Kind.Contains("material", StringComparison.OrdinalIgnoreCase) || effect.Kind.Contains("harvest", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"Faction offer '{offer.Id}' defines forbidden material-economy effect '{effect.Kind}'.");
                }
            }
        }
    }

    private static void ValidateEffects(
        IEnumerable<LocationEffectDefinition> effects,
        string source,
        CrossSystemDataBundle crossSystem,
        ICollection<string> errors)
    {
        foreach (var effect in effects)
        {
            switch (effect.Kind)
            {
                case LocationEffectKind.AddEvidence:
                    ValidateReference(source, effect, "evidence", effect.ReferenceId != null && crossSystem.Evidence.Find(effect.ReferenceId) != null, errors);
                    break;
                case LocationEffectKind.RaiseWorldTrigger:
                    ValidateReference(source, effect, "world trigger", effect.ReferenceId != null && crossSystem.FindTrigger(effect.ReferenceId) != null, errors);
                    break;
                case LocationEffectKind.ScheduleConsequence:
                    ValidateReference(source, effect, "consequence", effect.ReferenceId != null && crossSystem.FindConsequence(effect.ReferenceId) != null, errors);
                    break;
            }
        }
    }

    private static void ValidateReference(
        string source,
        LocationEffectDefinition effect,
        string referenceKind,
        bool exists,
        ICollection<string> errors)
    {
        if (exists)
        {
            return;
        }

        var reference = string.IsNullOrWhiteSpace(effect.ReferenceId) ? "<missing>" : effect.ReferenceId;
        errors.Add($"{source} effect '{effect.Id}' references unknown {referenceKind} '{reference}'.");
    }
}
}
