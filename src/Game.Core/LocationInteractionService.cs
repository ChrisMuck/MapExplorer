#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class LocationInteractionOption
{
    public LocationInteractionOption(
        LocationActionDefinition action,
        bool isAvailable,
        string? lockedReason,
        int rawRisk,
        LocationRiskBand riskBand,
        LocationEstimateConfidence confidence)
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
        IsAvailable = isAvailable;
        LockedReason = lockedReason;
        RawRisk = rawRisk;
        RiskBand = riskBand;
        Confidence = confidence;
    }

    public LocationActionDefinition Action { get; }

    public bool IsAvailable { get; }

    public string? LockedReason { get; }

    public int RawRisk { get; }

    public LocationRiskBand RiskBand { get; }

    public LocationEstimateConfidence Confidence { get; }
}

public sealed class LocationInteractionModel
{
    public LocationInteractionModel(SpecialLocationState location, IEnumerable<LocationInteractionOption> options)
    {
        Location = location ?? throw new ArgumentNullException(nameof(location));
        Options = new List<LocationInteractionOption>(options ?? throw new ArgumentNullException(nameof(options)));
    }

    public SpecialLocationState Location { get; }

    public IReadOnlyList<LocationInteractionOption> Options { get; }

    public LocationInteractionOption? FindOption(string actionId)
    {
        foreach (var option in Options)
        {
            if (option.Action.Id == actionId)
            {
                return option;
            }
        }

        return null;
    }
}

public sealed class LocationInteractionService
{
    private readonly LocationInteractionDefinitionSet definitions;

    public LocationInteractionService(LocationInteractionDefinitionSet definitions)
    {
        this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
    }

    public LocationInteractionModel BuildInteraction(SpecialLocationState location, ExpeditionState expedition)
    {
        if (location == null)
        {
            throw new ArgumentNullException(nameof(location));
        }

        if (expedition == null)
        {
            throw new ArgumentNullException(nameof(expedition));
        }

        if (string.IsNullOrWhiteSpace(location.ArchetypeId))
        {
            return new LocationInteractionModel(location, Enumerable.Empty<LocationInteractionOption>());
        }

        if (!definitions.Archetypes.TryGetValue(location.ArchetypeId, out var archetype))
        {
            return new LocationInteractionModel(location, Enumerable.Empty<LocationInteractionOption>());
        }

        var activeModifiers = ActiveModifiers(location).ToList();
        var actionIds = ResolveActionIds(location, archetype, activeModifiers);
        var options = new List<LocationInteractionOption>();

        foreach (var actionId in actionIds)
        {
            if (!definitions.Actions.TryGetValue(actionId, out var action))
            {
                continue;
            }

            var lockedReason = FirstUnmetRequirement(action, location, expedition, activeModifiers);
            var rawRisk = CalculateRawRisk(action, location, activeModifiers);
            options.Add(new LocationInteractionOption(
                action,
                string.IsNullOrWhiteSpace(lockedReason),
                lockedReason,
                rawRisk,
                ToBand(rawRisk),
                action.RiskProfile.Confidence));
        }

        return new LocationInteractionModel(location, options);
    }

    public IReadOnlyList<LocationModifierDefinition> ActiveModifiers(SpecialLocationState location)
    {
        var result = new List<LocationModifierDefinition>();
        foreach (var modifierId in location.ModifierIds)
        {
            if (definitions.Modifiers.TryGetValue(modifierId, out var modifier) && modifier.AppliesTo(location))
            {
                result.Add(modifier);
            }
        }

        return result;
    }

    private IReadOnlyList<string> ResolveActionIds(
        SpecialLocationState location,
        LocationArchetypeDefinition archetype,
        IReadOnlyList<LocationModifierDefinition> activeModifiers)
    {
        var actionIds = new List<string>(archetype.DefaultActionIds);

        if (!string.IsNullOrWhiteSpace(location.VariantId) && definitions.Variants.TryGetValue(location.VariantId, out var variant))
        {
            ApplySequentialLayer(actionIds, variant.AddedActionIds, variant.RemovedActionIds);
        }

        var modifierAdds = new HashSet<string>();
        var modifierRemoves = new HashSet<string>();
        foreach (var modifier in activeModifiers)
        {
            foreach (var actionId in modifier.AddedActionIds)
            {
                modifierAdds.Add(actionId);
            }

            foreach (var actionId in modifier.RemovedActionIds)
            {
                modifierRemoves.Add(actionId);
            }
        }

        foreach (var actionId in modifierAdds)
        {
            if (!modifierRemoves.Contains(actionId) && !actionIds.Contains(actionId))
            {
                actionIds.Add(actionId);
            }
        }

        foreach (var actionId in modifierRemoves)
        {
            actionIds.Remove(actionId);
        }

        return actionIds;
    }

    private static void ApplySequentialLayer(List<string> actionIds, IReadOnlyList<string> adds, IReadOnlyList<string> removes)
    {
        foreach (var actionId in removes)
        {
            actionIds.Remove(actionId);
        }

        foreach (var actionId in adds)
        {
            if (!actionIds.Contains(actionId))
            {
                actionIds.Add(actionId);
            }
        }
    }

    private static string? FirstUnmetRequirement(
        LocationActionDefinition action,
        SpecialLocationState location,
        ExpeditionState expedition,
        IReadOnlyList<LocationModifierDefinition> activeModifiers)
    {
        foreach (var requirement in action.HardRequirements)
        {
            if (!IsRequirementMet(requirement, location, expedition, activeModifiers))
            {
                return requirement.UnmetReason;
            }
        }

        return null;
    }

    private static bool IsRequirementMet(
        LocationRequirementDefinition requirement,
        SpecialLocationState location,
        ExpeditionState expedition,
        IReadOnlyList<LocationModifierDefinition> activeModifiers)
    {
        switch (requirement.Kind)
        {
            case LocationRequirementKind.OperationalStateAny:
                return requirement.Values.Contains(location.OperationalStateId);
            case LocationRequirementKind.ModifierActive:
                return activeModifiers.Any(modifier => requirement.Values.Contains(modifier.Id));
            case LocationRequirementKind.RolePresent:
                return requirement.RequiredRole.HasValue && expedition.Members.Any(member =>
                    member.Role == requirement.RequiredRole.Value && member.Status != ExpeditionMemberStatus.Missing && member.Status != ExpeditionMemberStatus.Dead);
            case LocationRequirementKind.PositionOnOrAdjacent:
                return location.Anchor.Coords.Any(coord => expedition.Position == coord || expedition.Position.DistanceTo(coord) == 1);
            case LocationRequirementKind.AnchorKind:
                return requirement.RequiredAnchorKind.HasValue && location.Anchor.Kind == requirement.RequiredAnchorKind.Value;
            default:
                return false;
        }
    }

    private static int CalculateRawRisk(
        LocationActionDefinition action,
        SpecialLocationState location,
        IReadOnlyList<LocationModifierDefinition> activeModifiers)
    {
        var risk = action.RiskProfile.BaseRisk;
        if (action.RiskProfile.BaseRiskByOperationalStateId.TryGetValue(location.OperationalStateId, out var stateRisk))
        {
            risk = stateRisk;
        }

        foreach (var modifier in activeModifiers)
        {
            if (modifier.RiskAdjustmentsByActionId.TryGetValue(action.Id, out var delta))
            {
                risk += delta;
            }
        }

        return Math.Max(0, risk);
    }

    private static LocationRiskBand ToBand(int rawRisk)
    {
        if (rawRisk <= 0)
        {
            return LocationRiskBand.None;
        }

        if (rawRisk < 25)
        {
            return LocationRiskBand.Low;
        }

        if (rawRisk < 45)
        {
            return LocationRiskBand.Moderate;
        }

        if (rawRisk < 70)
        {
            return LocationRiskBand.High;
        }

        return LocationRiskBand.Extreme;
    }
}
}
