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
    public static void Validate(LocationDataBundle locations, CrossSystemDataBundle crossSystem)
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

        if (errors.Count > 0)
        {
            throw new LocationDataException("Cross-system content validation failed:\n - " + string.Join("\n - ", errors));
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
