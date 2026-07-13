#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public enum LocationAnchorKind
{
    Point,
    Edge,
    Area,
    Path
}

public sealed class LocationAnchor
{
    private readonly List<HexCoord> coords;

    private LocationAnchor(LocationAnchorKind kind, IEnumerable<HexCoord> coords)
    {
        Kind = kind;
        this.coords = new List<HexCoord>(coords ?? throw new ArgumentNullException(nameof(coords)));
        if (this.coords.Count == 0)
        {
            throw new ArgumentException("A location anchor needs at least one coordinate.", nameof(coords));
        }

        if (kind == LocationAnchorKind.Edge && this.coords.Count != 2)
        {
            throw new ArgumentException("An edge anchor needs exactly two coordinates.", nameof(coords));
        }
    }

    public LocationAnchorKind Kind { get; }

    public IReadOnlyList<HexCoord> Coords
    {
        get { return coords; }
    }

    public HexCoord PrimaryCoord
    {
        get { return coords[0]; }
    }

    public static LocationAnchor Point(HexCoord coord)
    {
        return new LocationAnchor(LocationAnchorKind.Point, new[] { coord });
    }

    public static LocationAnchor Edge(HexCoord a, HexCoord b)
    {
        if (a.DistanceTo(b) != 1)
        {
            throw new ArgumentException("Edge anchor coordinates must be adjacent.");
        }

        return new LocationAnchor(LocationAnchorKind.Edge, new[] { a, b });
    }

    public static LocationAnchor Area(IEnumerable<HexCoord> coords)
    {
        return new LocationAnchor(LocationAnchorKind.Area, coords);
    }
}

public static class LocationStateChannels
{
    public const string Interaction = "interaction";
    public const string Operational = "operational";
    public const string Presence = "presence";
}

public static class LocationStateIds
{
    public static class Interaction
    {
        public const string Untouched = "untouched";
        public const string Observed = "observed";
        public const string Inspected = "inspected";
        public const string Investigated = "investigated";
        public const string Exhausted = "exhausted";
    }

    public static class Operational
    {
        public const string None = "none";
        public const string Blocked = "blocked";
        public const string RiskyPassage = "risky-passage";
        public const string TemporarilyOpen = "temporarily-open";
        public const string Open = "open";
        public const string Repaired = "repaired";
        public const string Destroyed = "destroyed";
        public const string Sealed = "sealed";
    }

    public static class Presence
    {
        public const string Empty = "empty";
        public const string Occupied = "occupied";
        public const string Watched = "watched";
        public const string Contested = "contested";
        public const string Abandoned = "abandoned";
        public const string Unknown = "unknown";
    }
}

public enum LocationActionRepeatPolicy
{
    Repeatable,
    OncePerLocation,
    OncePerState,
    RepeatableWithCost
}

public enum LocationRequirementKind
{
    OperationalStateAny,
    ModifierActive,
    RolePresent,
    PositionOnOrAdjacent,
    AnchorKind
}

public enum LocationRiskBand
{
    None,
    Low,
    Moderate,
    High,
    Extreme
}

public enum LocationEstimateConfidence
{
    Guess,
    Assessed,
    Confirmed
}

public enum LocationEffectKind
{
    ChangeLocationState,
    StartProject,
    AddUnsecuredKnowledge,
    ConsumeSupplies,
    ChangeMorale,
    AddFactionMemory,
    AddArchiveEntry,
    OpenRoute,
    MoveExpeditionAcrossEdge,
    InjureMember,
    ChangeFactionTrust,
    ChangeFactionAnger,
    ChangeFactionFear,
    AddEvidence,
    RaiseWorldTrigger,
    ScheduleConsequence
}

public sealed class LocationArchetypeDefinition
{
    public LocationArchetypeDefinition(string id, IEnumerable<string> defaultActionIds)
    {
        Id = RequireText(id, nameof(id));
        DefaultActionIds = new List<string>(defaultActionIds ?? throw new ArgumentNullException(nameof(defaultActionIds)));
    }

    public string Id { get; }

    public IReadOnlyList<string> DefaultActionIds { get; }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", name);
        }

        return value;
    }
}

public sealed class LocationVariantDefinition
{
    public LocationVariantDefinition(string id, IEnumerable<string>? addedActionIds = null, IEnumerable<string>? removedActionIds = null)
    {
        Id = RequireText(id, nameof(id));
        AddedActionIds = new List<string>(addedActionIds ?? Enumerable.Empty<string>());
        RemovedActionIds = new List<string>(removedActionIds ?? Enumerable.Empty<string>());
    }

    public string Id { get; }

    public IReadOnlyList<string> AddedActionIds { get; }

    public IReadOnlyList<string> RemovedActionIds { get; }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", name);
        }

        return value;
    }
}

public sealed class LocationModifierDefinition
{
    public LocationModifierDefinition(
        string id,
        IEnumerable<string>? addedActionIds = null,
        IEnumerable<string>? removedActionIds = null,
        IReadOnlyDictionary<string, int>? riskAdjustmentsByActionId = null,
        IEnumerable<string>? appliesWhenOperationalStateIds = null,
        IEnumerable<string>? compatibleArchetypeIds = null,
        IEnumerable<string>? incompatibleModifierIds = null)
    {
        Id = RequireText(id, nameof(id));
        AddedActionIds = new List<string>(addedActionIds ?? Enumerable.Empty<string>());
        RemovedActionIds = new List<string>(removedActionIds ?? Enumerable.Empty<string>());
        RiskAdjustmentsByActionId = new Dictionary<string, int>(riskAdjustmentsByActionId ?? new Dictionary<string, int>());
        AppliesWhenOperationalStateIds = new List<string>(appliesWhenOperationalStateIds ?? Enumerable.Empty<string>());
        CompatibleArchetypeIds = new List<string>(compatibleArchetypeIds ?? Enumerable.Empty<string>());
        IncompatibleModifierIds = new List<string>(incompatibleModifierIds ?? Enumerable.Empty<string>());
    }

    public string Id { get; }

    public IReadOnlyList<string> AddedActionIds { get; }

    public IReadOnlyList<string> RemovedActionIds { get; }

    public IReadOnlyDictionary<string, int> RiskAdjustmentsByActionId { get; }

    public IReadOnlyList<string> AppliesWhenOperationalStateIds { get; }

    /// <summary>Archetypes this modifier may attach to (§12.2). Empty = compatible with any.</summary>
    public IReadOnlyList<string> CompatibleArchetypeIds { get; }

    /// <summary>Modifiers that must not be active alongside this one (§12.2).</summary>
    public IReadOnlyList<string> IncompatibleModifierIds { get; }

    public bool IsCompatibleWithArchetype(string? archetypeId)
    {
        return CompatibleArchetypeIds.Count == 0 ||
            (!string.IsNullOrWhiteSpace(archetypeId) && CompatibleArchetypeIds.Contains(archetypeId));
    }

    public bool AppliesTo(SpecialLocationState location)
    {
        return AppliesWhenOperationalStateIds.Count == 0 || AppliesWhenOperationalStateIds.Contains(location.OperationalStateId);
    }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", name);
        }

        return value;
    }
}

public sealed class LocationRequirementDefinition
{
    public LocationRequirementDefinition(
        string id,
        LocationRequirementKind kind,
        IEnumerable<string>? values = null,
        ExpeditionMemberRole? requiredRole = null,
        LocationAnchorKind? requiredAnchorKind = null,
        string unmetReason = "Requirement is not met.")
    {
        Id = RequireText(id, nameof(id));
        Kind = kind;
        Values = new List<string>(values ?? Enumerable.Empty<string>());
        RequiredRole = requiredRole;
        RequiredAnchorKind = requiredAnchorKind;
        UnmetReason = RequireText(unmetReason, nameof(unmetReason));
    }

    public string Id { get; }

    public LocationRequirementKind Kind { get; }

    public IReadOnlyList<string> Values { get; }

    public ExpeditionMemberRole? RequiredRole { get; }

    public LocationAnchorKind? RequiredAnchorKind { get; }

    public string UnmetReason { get; }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", name);
        }

        return value;
    }
}

public sealed class LocationRiskProfileDefinition
{
    public LocationRiskProfileDefinition(
        int baseRisk,
        IReadOnlyDictionary<string, int>? baseRiskByOperationalStateId = null,
        LocationEstimateConfidence confidence = LocationEstimateConfidence.Guess)
    {
        BaseRisk = baseRisk;
        BaseRiskByOperationalStateId = new Dictionary<string, int>(baseRiskByOperationalStateId ?? new Dictionary<string, int>());
        Confidence = confidence;
    }

    public int BaseRisk { get; }

    public IReadOnlyDictionary<string, int> BaseRiskByOperationalStateId { get; }

    public LocationEstimateConfidence Confidence { get; }
}

public sealed class LocationEffectDefinition
{
    public LocationEffectDefinition(
        string id,
        LocationEffectKind kind,
        string text,
        string? stateChannel = null,
        string? stateId = null,
        int amount = 0,
        string? factionId = null,
        string? memory = null,
        string? selection = null,
        string? severity = null,
        string? referenceId = null,
        int delayDays = 0)
    {
        Id = RequireText(id, nameof(id));
        Kind = kind;
        Text = RequireText(text, nameof(text));
        StateChannel = string.IsNullOrWhiteSpace(stateChannel) ? null : stateChannel;
        StateId = string.IsNullOrWhiteSpace(stateId) ? null : stateId;
        Amount = amount;
        FactionId = string.IsNullOrWhiteSpace(factionId) ? null : factionId;
        Memory = string.IsNullOrWhiteSpace(memory) ? null : memory;
        Selection = string.IsNullOrWhiteSpace(selection) ? null : selection;
        Severity = string.IsNullOrWhiteSpace(severity) ? null : severity;
        ReferenceId = string.IsNullOrWhiteSpace(referenceId) ? null : referenceId;
        DelayDays = delayDays;
    }

    public string Id { get; }

    public LocationEffectKind Kind { get; }

    public string Text { get; }

    public string? StateChannel { get; }

    public string? StateId { get; }

    public int Amount { get; }

    public string? FactionId { get; }

    public string? Memory { get; }

    /// <summary>Member-selection strategy for member-targeting effects (e.g. "randomActiveMember").</summary>
    public string? Selection { get; }

    /// <summary>Severity hint for member-targeting effects (e.g. "Wounded").</summary>
    public string? Severity { get; }

    /// <summary>Stable evidence, trigger or consequence definition ID for cross-system effects.</summary>
    public string? ReferenceId { get; }

    /// <summary>World-day delay for a scheduled consequence. The resolved branch is stored immediately.</summary>
    public int DelayDays { get; }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", name);
        }

        return value;
    }
}

public sealed class LocationActionDefinition
{
    public LocationActionDefinition(
        string id,
        string label,
        string description,
        IEnumerable<LocationRequirementDefinition>? hardRequirements = null,
        LocationRiskProfileDefinition? riskProfile = null,
        string? outcomeTableId = null,
        IEnumerable<LocationCostDefinition>? costs = null,
        LocationActionRepeatPolicy repeatPolicy = LocationActionRepeatPolicy.Repeatable,
        bool startsProject = false,
        int projectDurationDays = 0,
        IEnumerable<LocationEffectDefinition>? projectCompletionEffects = null,
        string? icon = null,
        string? primaryButtonLabel = null,
        bool socialRisk = false,
        IEnumerable<string>? actionTags = null)
    {
        Id = RequireText(id, nameof(id));
        Label = RequireText(label, nameof(label));
        Description = RequireText(description, nameof(description));
        HardRequirements = new List<LocationRequirementDefinition>(hardRequirements ?? Enumerable.Empty<LocationRequirementDefinition>());
        RiskProfile = riskProfile ?? new LocationRiskProfileDefinition(0, confidence: LocationEstimateConfidence.Assessed);
        OutcomeTableId = string.IsNullOrWhiteSpace(outcomeTableId) ? null : outcomeTableId;
        Costs = new List<LocationCostDefinition>(costs ?? Enumerable.Empty<LocationCostDefinition>());
        RepeatPolicy = repeatPolicy;
        StartsProject = startsProject;
        ProjectDurationDays = projectDurationDays;
        ProjectCompletionEffects = new List<LocationEffectDefinition>(projectCompletionEffects ?? Enumerable.Empty<LocationEffectDefinition>());
        Icon = string.IsNullOrWhiteSpace(icon) ? null : icon;
        PrimaryButtonLabel = string.IsNullOrWhiteSpace(primaryButtonLabel) ? null : primaryButtonLabel;
        SocialRisk = socialRisk;
        ActionTags = (actionTags ?? Enumerable.Empty<string>())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (StartsProject && ProjectDurationDays < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(projectDurationDays), projectDurationDays, "A project action needs a positive duration.");
        }
    }

    public string Id { get; }

    public string Label { get; }

    public string Description { get; }

    public IReadOnlyList<LocationRequirementDefinition> HardRequirements { get; }

    public LocationRiskProfileDefinition RiskProfile { get; }

    /// <summary>Id of the band-weighted outcome table this action resolves through, or null for project actions.</summary>
    public string? OutcomeTableId { get; }

    public IReadOnlyList<LocationCostDefinition> Costs { get; }

    public LocationActionRepeatPolicy RepeatPolicy { get; }

    public bool StartsProject { get; }

    public int ProjectDurationDays { get; }

    public IReadOnlyList<LocationEffectDefinition> ProjectCompletionEffects { get; }

    public string? Icon { get; }

    public string? PrimaryButtonLabel { get; }

    /// <summary>When true, risk is driven by faction attitude rather than physical danger (§9.4B).</summary>
    public bool SocialRisk { get; }

    /// <summary>Neutral semantic tags such as repair, disturb or map, used by world and faction rules.</summary>
    public IReadOnlyList<string> ActionTags { get; }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", name);
        }

        return value;
    }
}

public sealed class LocationInteractionDefinitionSet
{
    public LocationInteractionDefinitionSet(
        IEnumerable<LocationArchetypeDefinition> archetypes,
        IEnumerable<LocationVariantDefinition> variants,
        IEnumerable<LocationModifierDefinition> modifiers,
        IEnumerable<LocationActionDefinition> actions,
        IEnumerable<LocationOutcomeTableDefinition>? outcomeTables = null,
        IEnumerable<LocationContentProfileDefinition>? contentProfiles = null)
    {
        Archetypes = ToDictionary(archetypes, item => item.Id);
        Variants = ToDictionary(variants, item => item.Id);
        Modifiers = ToDictionary(modifiers, item => item.Id);
        Actions = ToDictionary(actions, item => item.Id);
        OutcomeTables = ToDictionary(outcomeTables ?? Enumerable.Empty<LocationOutcomeTableDefinition>(), item => item.Id);
        ContentProfiles = ToDictionary(contentProfiles ?? Enumerable.Empty<LocationContentProfileDefinition>(), item => item.Id);
    }

    public IReadOnlyDictionary<string, LocationArchetypeDefinition> Archetypes { get; }

    public IReadOnlyDictionary<string, LocationVariantDefinition> Variants { get; }

    public IReadOnlyDictionary<string, LocationModifierDefinition> Modifiers { get; }

    public IReadOnlyDictionary<string, LocationActionDefinition> Actions { get; }

    public IReadOnlyDictionary<string, LocationOutcomeTableDefinition> OutcomeTables { get; }

    public IReadOnlyDictionary<string, LocationContentProfileDefinition> ContentProfiles { get; }

    public LocationOutcomeTableDefinition? FindOutcomeTable(string? id)
    {
        return !string.IsNullOrWhiteSpace(id) && OutcomeTables.TryGetValue(id!, out var table) ? table : null;
    }

    public LocationContentProfileDefinition? FindContentProfile(string? id)
    {
        return !string.IsNullOrWhiteSpace(id) && ContentProfiles.TryGetValue(id!, out var profile) ? profile : null;
    }

    /// <summary>Whether a modifier may attach to an archetype (§12.2). Unknown modifiers are allowed.</summary>
    public bool IsModifierCompatible(string? archetypeId, string modifierId)
    {
        return !Modifiers.TryGetValue(modifierId, out var modifier) || modifier.IsCompatibleWithArchetype(archetypeId);
    }

    /// <summary>
    /// Validates a modifier set for an archetype (§12.2): every modifier compatible with the archetype
    /// and no two active modifiers mutually incompatible. Returns the human-readable errors (empty = ok).
    /// </summary>
    public IReadOnlyList<string> ValidateModifierSet(string? archetypeId, IReadOnlyList<string> modifierIds)
    {
        var errors = new List<string>();
        if (modifierIds == null)
        {
            return errors;
        }

        foreach (var modifierId in modifierIds)
        {
            if (!Modifiers.TryGetValue(modifierId, out var modifier))
            {
                continue;
            }

            if (!modifier.IsCompatibleWithArchetype(archetypeId))
            {
                errors.Add($"Modifier '{modifierId}' is not compatible with archetype '{archetypeId}'.");
            }

            foreach (var otherId in modifierIds)
            {
                if (otherId != modifierId && modifier.IncompatibleModifierIds.Contains(otherId))
                {
                    errors.Add($"Modifiers '{modifierId}' and '{otherId}' are declared incompatible.");
                }
            }
        }

        return errors;
    }

    private static IReadOnlyDictionary<string, T> ToDictionary<T>(IEnumerable<T> items, Func<T, string> getId)
    {
        return new Dictionary<string, T>((items ?? throw new ArgumentNullException(nameof(items))).ToDictionary(getId));
    }
}

public sealed class LocationProjectState
{
    public LocationProjectState(string actionId, int requiredProgress)
    {
        if (requiredProgress < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredProgress), requiredProgress, "Project progress must be at least one.");
        }

        ActionId = RequireText(actionId, nameof(actionId));
        RequiredProgress = requiredProgress;
    }

    public string ActionId { get; }

    public int RequiredProgress { get; }

    public int Progress { get; private set; }

    public bool IsComplete
    {
        get { return Progress >= RequiredProgress; }
    }

    public void Advance()
    {
        if (!IsComplete)
        {
            Progress += 1;
        }
    }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", name);
        }

        return value;
    }
}
}
