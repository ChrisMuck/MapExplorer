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

    public LocationActionCommitment Commitment => Action.Commitment;
}

public sealed class LocationInteractionModel
{
    public LocationInteractionModel(
        SpecialLocationState location,
        IEnumerable<LocationInteractionOption> options,
        LocationContentProfileDefinition? contentProfile = null)
    {
        Location = location ?? throw new ArgumentNullException(nameof(location));
        Options = new List<LocationInteractionOption>(options ?? throw new ArgumentNullException(nameof(options)));
        ContentProfile = contentProfile;
    }

    public SpecialLocationState Location { get; }

    public IReadOnlyList<LocationInteractionOption> Options { get; }

    /// <summary>Authored presentation for this location, or null when no content profile is set.</summary>
    public LocationContentProfileDefinition? ContentProfile { get; }

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
    private readonly Random rng;

    public LocationInteractionService(LocationInteractionDefinitionSet definitions, Random? rng = null)
    {
        this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        this.rng = rng ?? new Random();
    }

    public LocationInteractionDefinitionSet Definitions => definitions;

    /// <summary>
    /// Resolves an action's outcome table for the given risk band into a concrete tier + effect bundle
    /// (concept Section 17.6). Returns null when the action has no outcome table (e.g. project actions).
    /// A forced tier bypasses the weighted roll for deterministic tests.
    /// </summary>
    public LocationOutcomeResolution? ResolveOutcome(
        LocationActionDefinition action,
        LocationRiskBand band,
        LocationOutcomeTier? forcedTier = null,
        IDeterministicRandomSource? deterministicRandom = null)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        var table = definitions.FindOutcomeTable(action.OutcomeTableId);
        if (table == null)
        {
            return null;
        }

        var tier = forcedTier ?? RollTier(table.WeightsForBand(band), deterministicRandom);
        return new LocationOutcomeResolution(tier, table.EffectsForTier(tier));
    }

    private LocationOutcomeTier RollTier(IReadOnlyList<LocationOutcomeTierWeight> weights, IDeterministicRandomSource? deterministicRandom)
    {
        var total = 0;
        foreach (var weight in weights)
        {
            total += weight.Weight;
        }

        if (total <= 0)
        {
            return weights.Count > 0 ? weights[0].Tier : LocationOutcomeTier.Success;
        }

        var roll = deterministicRandom != null ? deterministicRandom.NextInt(total) : rng.Next(total);
        var accumulated = 0;
        foreach (var weight in weights)
        {
            accumulated += weight.Weight;
            if (roll < accumulated)
            {
                return weight.Tier;
            }
        }

        return weights[weights.Count - 1].Tier;
    }

    public LocationInteractionModel BuildInteraction(
        SpecialLocationState location,
        ExpeditionState expedition,
        IReadOnlyList<FactionState>? linkedFactions = null,
        IReadOnlyList<string>? scenarioBaseActionIds = null)
    {
        if (location == null)
        {
            throw new ArgumentNullException(nameof(location));
        }

        if (expedition == null)
        {
            throw new ArgumentNullException(nameof(expedition));
        }

        var contentProfile = definitions.FindContentProfile(location.ContentProfileId);
        if (string.IsNullOrWhiteSpace(location.ArchetypeId))
        {
            return new LocationInteractionModel(location, Enumerable.Empty<LocationInteractionOption>(), contentProfile);
        }

        if (!definitions.Archetypes.TryGetValue(location.ArchetypeId, out var archetype))
        {
            return new LocationInteractionModel(location, Enumerable.Empty<LocationInteractionOption>(), contentProfile);
        }

        var activeModifiers = ActiveModifiers(location).ToList();
        var actionIds = ResolveActionIds(location, archetype, activeModifiers, scenarioBaseActionIds);
        var options = new List<LocationInteractionOption>();

        foreach (var actionId in actionIds)
        {
            if (!definitions.Actions.TryGetValue(actionId, out var action))
            {
                continue;
            }

            // Repeat policy (§7.6): an exhausted action is shown but locked, not farmed.
            var lockedReason = IsRepeatExhausted(action, location)
                ? "Bereits durchgefuehrt."
                : FirstUnmetRequirement(action, location, expedition, activeModifiers)
                    ?? FirstUnpayableCost(action, expedition);
            var rawRisk = CalculateRawRisk(action, location, activeModifiers, linkedFactions);
            options.Add(new LocationInteractionOption(
                action,
                string.IsNullOrWhiteSpace(lockedReason),
                lockedReason,
                rawRisk,
                ToBand(rawRisk),
                action.RiskProfile.Confidence));
        }

        return new LocationInteractionModel(location, options, contentProfile);
    }

    /// <summary>Repeat-policy key for consumed-action tracking (§7.6).</summary>
    public static string RepeatKey(LocationActionDefinition action, SpecialLocationState location)
    {
        return action.RepeatPolicy == LocationActionRepeatPolicy.OncePerState
            ? $"{action.Id}@{location.OperationalStateId}"
            : action.Id;
    }

    private static bool IsRepeatExhausted(LocationActionDefinition action, SpecialLocationState location)
    {
        switch (action.RepeatPolicy)
        {
            case LocationActionRepeatPolicy.OncePerLocation:
            case LocationActionRepeatPolicy.OncePerState:
                return location.HasResolvedAction(RepeatKey(action, location));
            default:
                return false;
        }
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
        IReadOnlyList<LocationModifierDefinition> activeModifiers,
        IReadOnlyList<string>? scenarioBaseActionIds)
    {
        var usesScenarioProfile = scenarioBaseActionIds != null;
        var actionIds = new List<string>(scenarioBaseActionIds ?? archetype.DefaultActionIds);

        if (!usesScenarioProfile && !string.IsNullOrWhiteSpace(location.VariantId) && definitions.Variants.TryGetValue(location.VariantId, out var variant))
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
                return requirement.Disclosure == LocationRequirementDisclosure.Known
                    ? requirement.UnmetReason
                    : "Noch nicht verfuegbar.";
            }
        }

        return null;
    }

    private static string? FirstUnpayableCost(LocationActionDefinition action, ExpeditionState expedition)
    {
        if (action.Commitment == LocationActionCommitment.DayOperation && expedition.MovementPoints == 0)
        {
            return "Keine Tageskapazitaet mehr. Die Expedition muss den Tag beenden, bevor sie diese Arbeit beginnen kann.";
        }

        foreach (var cost in action.Costs)
        {
            if (cost.Amount <= 0)
            {
                continue;
            }

            switch (cost.Kind)
            {
                case LocationCostKind.MovementPoints:
                    if (expedition.MovementPoints < cost.Amount)
                    {
                        return "Nicht genug Bewegungspunkte.";
                    }

                    break;
                case LocationCostKind.Supplies:
                    if (expedition.Supplies < cost.Amount)
                    {
                        return "Nicht genug Vorraete.";
                    }

                    break;
                case LocationCostKind.Medicine:
                    if (expedition.Medicine < cost.Amount)
                    {
                        return "Nicht genug Medizin.";
                    }

                    break;
                case LocationCostKind.Morale:
                    if (expedition.Morale < cost.Amount)
                    {
                        return "Nicht genug Moral.";
                    }

                    break;
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
        IReadOnlyList<LocationModifierDefinition> activeModifiers,
        IReadOnlyList<FactionState>? linkedFactions)
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

        // Social risk (§9.4B): faction attitude drives risk for social actions rather than danger.
        if (action.SocialRisk && linkedFactions != null)
        {
            foreach (var faction in linkedFactions)
            {
                risk += Round(faction.Anger, 4) + Round(faction.Fear, 4) - Round(faction.Trust, 4);
            }
        }

        return Math.Max(0, risk);
    }

    private static int Round(int value, int divisor)
    {
        return (int)Math.Round(value / (double)divisor, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Recovery Check (§9.9): when a member-ending effect would apply on a bad outcome, a softer roll
    /// decides whether the member is spared. A present Medic improves the odds; forced value for tests.
    /// </summary>
    public LocationRecoveryOutcome RollRecovery(bool improved, LocationRecoveryOutcome? forced = null)
    {
        if (forced.HasValue)
        {
            return forced.Value;
        }

        var preservedChance = BaseRecoveryChance + (improved ? RecoveryImprovementBonus : 0);
        var roll = rng.Next(100);
        if (roll < preservedChance)
        {
            return LocationRecoveryOutcome.Preserved;
        }

        return roll < preservedChance + PartialRecoveryBand
            ? LocationRecoveryOutcome.PartiallyPreserved
            : LocationRecoveryOutcome.Lost;
    }

    private const int BaseRecoveryChance = 35;
    private const int RecoveryImprovementBonus = 25;
    private const int PartialRecoveryBand = 30;

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
