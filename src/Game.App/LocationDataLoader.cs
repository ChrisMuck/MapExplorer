#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Game.App
{

/// <summary>Thrown when location JSON fails validation (concept Section 19).</summary>
public sealed class LocationDataException : Exception
{
    public LocationDataException(string message) : base(message)
    {
    }
}

/// <summary>
/// Loaded location content: the definition registries plus the placed instances. Built by
/// <see cref="LocationDataLoader"/> from the JSON authoring contract (concept Section 17).
/// </summary>
public sealed class LocationDataBundle
{
    public LocationDataBundle(LocationInteractionDefinitionSet definitions, IReadOnlyList<SpecialLocationState> instances)
    {
        Definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        Instances = instances ?? throw new ArgumentNullException(nameof(instances));
    }

    public LocationInteractionDefinitionSet Definitions { get; }

    public IReadOnlyList<SpecialLocationState> Instances { get; }
}

/// <summary>
/// Parses location JSON documents (concept Section 17) into Core registries and instances. The parser
/// is generic: it keys off <c>documentType</c> and stable IDs, never off concrete variant names.
/// </summary>
public static class LocationDataLoader
{
    private const int SupportedSchemaVersion = 1;

    /// <summary>Loads every <c>*.json</c> under a folder tree. Returns null if the folder is absent.</summary>
    public static LocationDataBundle? LoadFromDirectory(string rootFolder)
    {
        if (string.IsNullOrWhiteSpace(rootFolder) || !Directory.Exists(rootFolder))
        {
            return null;
        }

        var documents = Directory
            .EnumerateFiles(rootFolder, "*.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(File.ReadAllText)
            .ToList();

        return documents.Count == 0 ? null : LoadFromJson(documents);
    }

    public static LocationDataBundle LoadFromJson(IEnumerable<string> jsonDocuments)
    {
        if (jsonDocuments == null)
        {
            throw new ArgumentNullException(nameof(jsonDocuments));
        }

        var archetypes = new List<ArchetypeDto>();
        var variants = new List<VariantDto>();
        var modifiers = new List<ModifierDto>();
        var actions = new List<ActionDto>();
        var outcomeTables = new List<OutcomeTableDto>();
        var contentProfiles = new List<ContentProfileDto>();
        var instances = new List<InstanceDto>();

        foreach (var json in jsonDocuments)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                continue;
            }

            JObject envelope;
            try
            {
                envelope = JObject.Parse(json);
            }
            catch (JsonException ex)
            {
                throw new LocationDataException($"Malformed location JSON document: {ex.Message}");
            }

            var documentType = (string?)envelope["documentType"];
            var schemaVersion = (int?)envelope["schemaVersion"] ?? 0;
            if (schemaVersion != SupportedSchemaVersion)
            {
                throw new LocationDataException($"Unsupported schemaVersion '{schemaVersion}' (expected {SupportedSchemaVersion}).");
            }

            var items = envelope["items"] as JArray ?? new JArray();
            switch (documentType)
            {
                case "location-archetypes": archetypes.AddRange(items.ToObject<List<ArchetypeDto>>()!); break;
                case "location-variants": variants.AddRange(items.ToObject<List<VariantDto>>()!); break;
                case "location-modifiers": modifiers.AddRange(items.ToObject<List<ModifierDto>>()!); break;
                case "location-actions": actions.AddRange(items.ToObject<List<ActionDto>>()!); break;
                case "location-outcome-tables": outcomeTables.AddRange(items.ToObject<List<OutcomeTableDto>>()!); break;
                case "location-content-profiles": contentProfiles.AddRange(items.ToObject<List<ContentProfileDto>>()!); break;
                case "location-instances": instances.AddRange(items.ToObject<List<InstanceDto>>()!); break;
                default:
                    throw new LocationDataException($"Unsupported documentType '{documentType}'.");
            }
        }

        var definitions = BuildDefinitions(archetypes, variants, modifiers, actions, outcomeTables, contentProfiles);
        var builtInstances = instances.Select(instance => BuildInstance(instance, definitions)).ToList();
        Validate(definitions, builtInstances);
        return new LocationDataBundle(definitions, builtInstances);
    }

    private static LocationInteractionDefinitionSet BuildDefinitions(
        List<ArchetypeDto> archetypes,
        List<VariantDto> variants,
        List<ModifierDto> modifiers,
        List<ActionDto> actions,
        List<OutcomeTableDto> outcomeTables,
        List<ContentProfileDto> contentProfiles)
    {
        return new LocationInteractionDefinitionSet(
            archetypes.Select(BuildArchetype),
            variants.Select(BuildVariant),
            modifiers.Select(BuildModifier),
            actions.Select(BuildAction),
            outcomeTables.Select(BuildOutcomeTable),
            contentProfiles.Select(BuildContentProfile));
    }

    private static LocationArchetypeDefinition BuildArchetype(ArchetypeDto dto)
    {
        return new LocationArchetypeDefinition(Require(dto.Id, "archetype.id"), dto.DefaultActionIds ?? new List<string>());
    }

    private static LocationVariantDefinition BuildVariant(VariantDto dto)
    {
        return new LocationVariantDefinition(Require(dto.Id, "variant.id"), dto.AddedActionIds, dto.RemovedActionIds);
    }

    private static LocationModifierDefinition BuildModifier(ModifierDto dto)
    {
        var riskAdjustments = new Dictionary<string, int>();
        if (dto.RiskAdjustments != null)
        {
            foreach (var adjustment in dto.RiskAdjustments)
            {
                if (!string.IsNullOrWhiteSpace(adjustment.ActionId))
                {
                    riskAdjustments[adjustment.ActionId] = adjustment.ScoreDelta;
                }
            }
        }

        return new LocationModifierDefinition(
            Require(dto.Id, "modifier.id"),
            dto.AddedActionIds,
            dto.RemovedActionIds,
            riskAdjustments,
            dto.AppliesWhen?.OperationalStateAny,
            dto.CompatibleArchetypeIds,
            dto.IncompatibleModifierIds);
    }

    private static LocationActionDefinition BuildAction(ActionDto dto)
    {
        var requirements = (dto.HardRequirements ?? new List<RequirementDto>()).Select(BuildRequirement);
        var costs = (dto.Costs ?? new List<CostDto>()).Select(BuildCost);
        var project = dto.Project;
        return new LocationActionDefinition(
            Require(dto.Id, "action.id"),
            Require(dto.Label, "action.label"),
            Require(dto.Description, "action.description"),
            hardRequirements: requirements,
            riskProfile: BuildRiskProfile(dto.RiskProfile),
            outcomeTableId: dto.OutcomeTableId,
            costs: costs,
            repeatPolicy: ParseEnum(dto.RepeatPolicy, LocationActionRepeatPolicy.Repeatable),
            startsProject: project != null,
            projectDurationDays: project?.DurationDays ?? 0,
            projectCompletionEffects: project?.CompletionEffects?.Select(BuildEffect),
            icon: dto.Presentation?.Icon,
            primaryButtonLabel: dto.Presentation?.PrimaryButtonLabel,
            socialRisk: dto.SocialRisk);
    }

    private static LocationRequirementDefinition BuildRequirement(RequirementDto dto)
    {
        var kind = ParseEnum(dto.Kind, LocationRequirementKind.PositionOnOrAdjacent);
        ExpeditionMemberRole? role = null;
        if (!string.IsNullOrWhiteSpace(dto.Role) && Enum.TryParse<ExpeditionMemberRole>(dto.Role, true, out var parsedRole))
        {
            role = parsedRole;
        }

        LocationAnchorKind? anchorKind = null;
        if (!string.IsNullOrWhiteSpace(dto.AnchorKind) && Enum.TryParse<LocationAnchorKind>(dto.AnchorKind, true, out var parsedAnchor))
        {
            anchorKind = parsedAnchor;
        }

        return new LocationRequirementDefinition(
            $"req-{dto.Kind}".ToLowerInvariant(),
            kind,
            dto.Values,
            role,
            anchorKind,
            string.IsNullOrWhiteSpace(dto.UnmetReason) ? "Requirement is not met." : dto.UnmetReason!);
    }

    private static LocationCostDefinition BuildCost(CostDto dto)
    {
        return new LocationCostDefinition(ParseEnum(dto.Kind, LocationCostKind.Supplies), dto.Amount, dto.Timing ?? "onCommit");
    }

    private static LocationRiskProfileDefinition BuildRiskProfile(RiskProfileDto? dto)
    {
        if (dto == null)
        {
            return new LocationRiskProfileDefinition(0, confidence: LocationEstimateConfidence.Assessed);
        }

        return new LocationRiskProfileDefinition(
            dto.BaseRisk,
            dto.BaseRiskByOperationalState,
            ParseEnum(dto.Confidence, LocationEstimateConfidence.Guess));
    }

    private static LocationOutcomeTableDefinition BuildOutcomeTable(OutcomeTableDto dto)
    {
        var tableId = Require(dto.Id, "outcomeTable.id");
        var weightsByBand = new Dictionary<LocationRiskBand, IReadOnlyList<LocationOutcomeTierWeight>>();
        if (dto.Tiers != null)
        {
            foreach (var pair in dto.Tiers)
            {
                if (!Enum.TryParse<LocationRiskBand>(pair.Key, true, out var band))
                {
                    throw new LocationDataException($"Outcome table '{tableId}' references unknown risk band '{pair.Key}'.");
                }

                weightsByBand[band] = pair.Value
                    .Select(weight => new LocationOutcomeTierWeight(ParseTier(weight.Tier, tableId), weight.Weight))
                    .ToList();
            }
        }

        var effectBundles = new Dictionary<LocationOutcomeTier, IReadOnlyList<LocationEffectDefinition>>();
        if (dto.EffectBundles != null)
        {
            foreach (var pair in dto.EffectBundles)
            {
                effectBundles[ParseTier(pair.Key, tableId)] = pair.Value.Select(BuildEffect).ToList();
            }
        }

        return new LocationOutcomeTableDefinition(
            tableId,
            weightsByBand,
            effectBundles,
            dto.AppliesTo?.ArchetypeId,
            dto.AppliesTo?.ActionId);
    }

    private static int effectCounter;

    private static LocationEffectDefinition BuildEffect(EffectDto dto)
    {
        var id = string.IsNullOrWhiteSpace(dto.Id) ? $"effect-{dto.Kind}-{effectCounter++}" : dto.Id!;
        return new LocationEffectDefinition(
            id,
            ParseEnum(dto.Kind, LocationEffectKind.AddArchiveEntry),
            Require(dto.Text, "effect.text"),
            dto.StateChannel,
            dto.StateId,
            dto.Amount,
            dto.FactionId,
            dto.Memory,
            dto.Selection,
            dto.Severity,
            dto.ReferenceId,
            dto.DelayDays);
    }

    private static LocationContentProfileDefinition BuildContentProfile(ContentProfileDto dto)
    {
        return new LocationContentProfileDefinition(
            Require(dto.Id, "contentProfile.id"),
            Require(dto.Title, "contentProfile.title"),
            dto.Subtitle,
            dto.ShortDescription,
            dto.Description,
            dto.FlavorByState,
            dto.ImageId,
            dto.JournalText?.Discovered,
            dto.JournalText?.Resolved);
    }

    private static SpecialLocationState BuildInstance(InstanceDto dto, LocationInteractionDefinitionSet definitions)
    {
        var anchor = BuildAnchor(dto.Anchor, dto.Id);
        var coord = anchor.Kind == LocationAnchorKind.Edge && anchor.Coords.Count == 2
            ? anchor.Coords[1]
            : anchor.PrimaryCoord;

        var name = dto.Name;
        if (string.IsNullOrWhiteSpace(name))
        {
            name = definitions.FindContentProfile(dto.ContentProfileId)?.Title ?? dto.Id;
        }

        var kind = ParseEnum(dto.LocationKind, LocationKind.Landmark);
        return new SpecialLocationState(
            Require(dto.Id, "instance.id"),
            kind,
            coord,
            name!,
            anchor,
            dto.ArchetypeId,
            dto.VariantId,
            dto.ModifierIds,
            dto.ContentProfileId,
            interactionStateId: OrDefault(dto.InitialState?.Interaction, LocationStateIds.Interaction.Untouched),
            operationalStateId: OrDefault(dto.InitialState?.Operational, LocationStateIds.Operational.None),
            presenceStateId: OrDefault(dto.InitialState?.Presence, LocationStateIds.Presence.Unknown),
            factionIds: dto.FactionIds);
    }

    private static LocationAnchor BuildAnchor(AnchorDto? dto, string? instanceId)
    {
        if (dto?.Hexes == null || dto.Hexes.Count == 0)
        {
            throw new LocationDataException($"Instance '{instanceId}' has no anchor hexes.");
        }

        var coords = dto.Hexes.Select(pair => new HexCoord(pair[0], pair[1])).ToList();
        var kind = ParseEnum(dto.Kind, LocationAnchorKind.Point);
        switch (kind)
        {
            case LocationAnchorKind.Edge:
                return LocationAnchor.Edge(coords[0], coords[1]);
            case LocationAnchorKind.Area:
                return LocationAnchor.Area(coords);
            default:
                return LocationAnchor.Point(coords[0]);
        }
    }

    private static void Validate(LocationInteractionDefinitionSet definitions, IReadOnlyList<SpecialLocationState> instances)
    {
        var errors = new List<string>();

        foreach (var archetype in definitions.Archetypes.Values)
        {
            foreach (var actionId in archetype.DefaultActionIds)
            {
                if (!definitions.Actions.ContainsKey(actionId))
                {
                    errors.Add($"Archetype '{archetype.Id}' references unknown action '{actionId}'.");
                }
            }
        }

        foreach (var action in definitions.Actions.Values)
        {
            if (action.OutcomeTableId != null && !definitions.OutcomeTables.ContainsKey(action.OutcomeTableId))
            {
                errors.Add($"Action '{action.Id}' references unknown outcome table '{action.OutcomeTableId}'.");
            }
        }

        foreach (var table in definitions.OutcomeTables.Values)
        {
            foreach (var band in table.Bands)
            {
                foreach (var weight in table.WeightsForBand(band))
                {
                    if (weight.Weight > 0 && !table.HasEffectBundle(weight.Tier))
                    {
                        errors.Add($"Outcome table '{table.Id}' band '{band}' weights tier '{weight.Tier}' but has no effect bundle for it.");
                    }
                }
            }
        }

        // Modifier compatibility references must resolve (§12.2 / §19 "modifier incompatibilities").
        foreach (var modifier in definitions.Modifiers.Values)
        {
            foreach (var archetypeId in modifier.CompatibleArchetypeIds)
            {
                if (!definitions.Archetypes.ContainsKey(archetypeId))
                {
                    errors.Add($"Modifier '{modifier.Id}' declares compatibility with unknown archetype '{archetypeId}'.");
                }
            }

            foreach (var otherId in modifier.IncompatibleModifierIds)
            {
                if (!definitions.Modifiers.ContainsKey(otherId))
                {
                    errors.Add($"Modifier '{modifier.Id}' declares incompatibility with unknown modifier '{otherId}'.");
                }
            }
        }

        foreach (var instance in instances)
        {
            if (instance.ArchetypeId != null && !definitions.Archetypes.ContainsKey(instance.ArchetypeId))
            {
                errors.Add($"Instance '{instance.Id}' references unknown archetype '{instance.ArchetypeId}'.");
            }

            if (instance.VariantId != null && !definitions.Variants.ContainsKey(instance.VariantId))
            {
                errors.Add($"Instance '{instance.Id}' references unknown variant '{instance.VariantId}'.");
            }

            if (instance.ContentProfileId != null && !definitions.ContentProfiles.ContainsKey(instance.ContentProfileId))
            {
                errors.Add($"Instance '{instance.Id}' references unknown content profile '{instance.ContentProfileId}'.");
            }

            foreach (var modifierId in instance.ModifierIds)
            {
                if (!definitions.Modifiers.ContainsKey(modifierId))
                {
                    errors.Add($"Instance '{instance.Id}' references unknown modifier '{modifierId}'.");
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new LocationDataException("Location data validation failed:\n - " + string.Join("\n - ", errors));
        }
    }

    private static LocationOutcomeTier ParseTier(string? value, string tableId)
    {
        if (Enum.TryParse<LocationOutcomeTier>(value, true, out var tier))
        {
            return tier;
        }

        throw new LocationDataException($"Outcome table '{tableId}' references unknown outcome tier '{value}'.");
    }

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct
    {
        return !string.IsNullOrWhiteSpace(value) && Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : fallback;
    }

    private static string OrDefault(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value!;
    }

    private static string Require(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new LocationDataException($"Missing required field '{field}'.");
        }

        return value!;
    }

    // ---- DTOs (Newtonsoft binds case-insensitively) ----------------------

    private sealed class ArchetypeDto
    {
        public string? Id { get; set; }
        public List<string>? DefaultActionIds { get; set; }
    }

    private sealed class VariantDto
    {
        public string? Id { get; set; }
        public string? ArchetypeId { get; set; }
        public List<string>? AddedActionIds { get; set; }
        public List<string>? RemovedActionIds { get; set; }
    }

    private sealed class ModifierDto
    {
        public string? Id { get; set; }
        public List<string>? AddedActionIds { get; set; }
        public List<string>? RemovedActionIds { get; set; }
        public AppliesWhenDto? AppliesWhen { get; set; }
        public List<RiskAdjustmentDto>? RiskAdjustments { get; set; }
        public List<string>? CompatibleArchetypeIds { get; set; }
        public List<string>? IncompatibleModifierIds { get; set; }
    }

    private sealed class AppliesWhenDto
    {
        public List<string>? OperationalStateAny { get; set; }
    }

    private sealed class RiskAdjustmentDto
    {
        public string? ActionId { get; set; }
        public int ScoreDelta { get; set; }
    }

    private sealed class ActionDto
    {
        public string? Id { get; set; }
        public string? Label { get; set; }
        public string? Description { get; set; }
        public List<RequirementDto>? HardRequirements { get; set; }
        public List<CostDto>? Costs { get; set; }
        public RiskProfileDto? RiskProfile { get; set; }
        public string? OutcomeTableId { get; set; }
        public string? RepeatPolicy { get; set; }
        public ProjectDto? Project { get; set; }
        public PresentationDto? Presentation { get; set; }
        public bool SocialRisk { get; set; }
    }

    private sealed class RequirementDto
    {
        public string? Kind { get; set; }
        public List<string>? Values { get; set; }
        public string? Role { get; set; }
        public string? AnchorKind { get; set; }
        public string? UnmetReason { get; set; }
    }

    private sealed class CostDto
    {
        public string? Kind { get; set; }
        public int Amount { get; set; }
        public string? Timing { get; set; }
    }

    private sealed class RiskProfileDto
    {
        public int BaseRisk { get; set; }
        public Dictionary<string, int>? BaseRiskByOperationalState { get; set; }
        public string? Confidence { get; set; }
    }

    private sealed class ProjectDto
    {
        public int DurationDays { get; set; }
        public List<EffectDto>? CompletionEffects { get; set; }
    }

    private sealed class PresentationDto
    {
        public string? Icon { get; set; }
        public string? PrimaryButtonLabel { get; set; }
    }

    private sealed class OutcomeTableDto
    {
        public string? Id { get; set; }
        public AppliesToDto? AppliesTo { get; set; }
        public Dictionary<string, List<TierWeightDto>>? Tiers { get; set; }
        public Dictionary<string, List<EffectDto>>? EffectBundles { get; set; }
    }

    private sealed class AppliesToDto
    {
        public string? ArchetypeId { get; set; }
        public string? ActionId { get; set; }
    }

    private sealed class TierWeightDto
    {
        public string? Tier { get; set; }
        public int Weight { get; set; }
    }

    private sealed class EffectDto
    {
        public string? Id { get; set; }
        public string? Kind { get; set; }
        public string? Text { get; set; }
        public int Amount { get; set; }
        public string? StateChannel { get; set; }
        public string? StateId { get; set; }
        public string? FactionId { get; set; }
        public string? Memory { get; set; }
        public string? Selection { get; set; }
        public string? Severity { get; set; }
        public string? ReferenceId { get; set; }
        public int DelayDays { get; set; }
    }

    private sealed class ContentProfileDto
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Subtitle { get; set; }
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public Dictionary<string, string>? FlavorByState { get; set; }
        public string? ImageId { get; set; }
        public JournalDto? JournalText { get; set; }
    }

    private sealed class JournalDto
    {
        public string? Discovered { get; set; }
        public string? Resolved { get; set; }
    }

    private sealed class InstanceDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? LocationKind { get; set; }
        public string? ArchetypeId { get; set; }
        public string? VariantId { get; set; }
        public AnchorDto? Anchor { get; set; }
        public List<string>? ModifierIds { get; set; }
        public List<string>? FactionIds { get; set; }
        public string? ContentProfileId { get; set; }
        public InitialStateDto? InitialState { get; set; }
    }

    private sealed class AnchorDto
    {
        public string? Kind { get; set; }
        public List<List<int>>? Hexes { get; set; }
    }

    private sealed class InitialStateDto
    {
        public string? Knowledge { get; set; }
        public string? Interaction { get; set; }
        public string? Operational { get; set; }
        public string? Presence { get; set; }
    }
}
}
