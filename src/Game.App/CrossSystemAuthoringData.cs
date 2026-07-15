#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Newtonsoft.Json.Linq;

namespace Game.App
{

/// <summary>Permitted values of one persistent state channel for an authored location scenario.</summary>
public sealed class LocationStateChannelDefinition
{
    public LocationStateChannelDefinition(string initialValue, IEnumerable<string> values)
    {
        InitialValue = RequireText(initialValue, nameof(initialValue));
        Values = Normalize(values, nameof(values));
        if (!Values.Contains(InitialValue, StringComparer.Ordinal))
        {
            throw new LocationDataException($"State channel initial value '{InitialValue}' is not in its values list.");
        }
    }

    public string InitialValue { get; }
    public IReadOnlyList<string> Values { get; }

    internal static string RequireText(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new LocationDataException($"{field} must not be empty.");
        return value.Trim();
    }

    internal static IReadOnlyList<string> Normalize(IEnumerable<string>? values, string field)
    {
        var normalized = (values ?? Enumerable.Empty<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (normalized.Count == 0) throw new LocationDataException($"{field} must contain at least one value.");
        return normalized;
    }
}

public sealed class LocationStateProfileDefinition
{
    public LocationStateProfileDefinition(string id, string archetypeId, IReadOnlyDictionary<string, LocationStateChannelDefinition> channels)
    {
        Id = LocationStateChannelDefinition.RequireText(id, nameof(id));
        ArchetypeId = LocationStateChannelDefinition.RequireText(archetypeId, nameof(archetypeId));
        Channels = channels ?? throw new ArgumentNullException(nameof(channels));
        if (Channels.Count == 0) throw new LocationDataException($"State profile '{Id}' must define at least one channel.");
    }

    public string Id { get; }
    public string ArchetypeId { get; }
    public IReadOnlyDictionary<string, LocationStateChannelDefinition> Channels { get; }
}

public sealed class LocationContextActionRuleDefinition
{
    public LocationContextActionRuleDefinition(IEnumerable<string>? knownContextTags, IEnumerable<string>? hiddenContextTags, IEnumerable<string> actionIds, string disclosure)
    {
        KnownContextTags = NormalizeOptional(knownContextTags);
        HiddenContextTags = NormalizeOptional(hiddenContextTags);
        ActionIds = LocationStateChannelDefinition.Normalize(actionIds, nameof(actionIds));
        Disclosure = LocationStateChannelDefinition.RequireText(disclosure, nameof(disclosure));
        if (KnownContextTags.Count == 0 && HiddenContextTags.Count == 0)
        {
            throw new LocationDataException("A context action rule needs a known or hidden context condition.");
        }
    }

    public IReadOnlyList<string> KnownContextTags { get; }
    public IReadOnlyList<string> HiddenContextTags { get; }
    public IReadOnlyList<string> ActionIds { get; }
    public string Disclosure { get; }

    internal static IReadOnlyList<string> NormalizeOptional(IEnumerable<string>? values) => (values ?? Enumerable.Empty<string>())
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value.Trim())
        .Distinct(StringComparer.Ordinal)
        .ToList();
}

/// <summary>
/// Adds normal actions after an authored persistent state has been reached. Conditions across
/// channels are combined with AND; values inside one channel are alternatives.
/// </summary>
public sealed class LocationStateActionRuleDefinition
{
    public LocationStateActionRuleDefinition(
        IEnumerable<string>? interactionStateIds,
        IEnumerable<string>? operationalStateIds,
        IEnumerable<string>? presenceStateIds,
        IEnumerable<string> actionIds)
    {
        InteractionStateIds = LocationContextActionRuleDefinition.NormalizeOptional(interactionStateIds);
        OperationalStateIds = LocationContextActionRuleDefinition.NormalizeOptional(operationalStateIds);
        PresenceStateIds = LocationContextActionRuleDefinition.NormalizeOptional(presenceStateIds);
        ActionIds = LocationStateChannelDefinition.Normalize(actionIds, nameof(actionIds));

        if (InteractionStateIds.Count == 0 && OperationalStateIds.Count == 0 && PresenceStateIds.Count == 0)
        {
            throw new LocationDataException("A state action rule needs at least one state condition.");
        }
    }

    public IReadOnlyList<string> InteractionStateIds { get; }
    public IReadOnlyList<string> OperationalStateIds { get; }
    public IReadOnlyList<string> PresenceStateIds { get; }
    public IReadOnlyList<string> ActionIds { get; }

    public bool Matches(SpecialLocationState location)
    {
        if (location == null) throw new ArgumentNullException(nameof(location));
        return Matches(InteractionStateIds, location.InteractionStateId)
            && Matches(OperationalStateIds, location.OperationalStateId)
            && Matches(PresenceStateIds, location.PresenceStateId);
    }

    private static bool Matches(IReadOnlyList<string> allowedStateIds, string currentStateId) =>
        allowedStateIds.Count == 0 || allowedStateIds.Contains(currentStateId, StringComparer.Ordinal);
}

public sealed class LocationScenarioActionSetDefinition
{
    public LocationScenarioActionSetDefinition(
        IEnumerable<string>? sharedActionIds,
        IEnumerable<string>? initialAdditionalActionIds,
        int maximumVisibleAdditionalActions,
        IEnumerable<LocationContextActionRuleDefinition>? contextActionRules,
        IEnumerable<LocationStateActionRuleDefinition>? stateActionRules = null)
    {
        SharedActionIds = LocationContextActionRuleDefinition.NormalizeOptional(sharedActionIds);
        InitialAdditionalActionIds = LocationContextActionRuleDefinition.NormalizeOptional(initialAdditionalActionIds);
        MaximumVisibleAdditionalActions = maximumVisibleAdditionalActions;
        ContextActionRules = (contextActionRules ?? Enumerable.Empty<LocationContextActionRuleDefinition>()).ToList();
        StateActionRules = (stateActionRules ?? Enumerable.Empty<LocationStateActionRuleDefinition>()).ToList();
        if (maximumVisibleAdditionalActions < 0 || maximumVisibleAdditionalActions > 3)
        {
            throw new LocationDataException("maximumVisibleAdditionalActions must be between 0 and 3.");
        }
        if (InitialAdditionalActionIds.Count > maximumVisibleAdditionalActions)
        {
            throw new LocationDataException("Initial additional actions exceed maximumVisibleAdditionalActions.");
        }
    }

    public IReadOnlyList<string> SharedActionIds { get; }
    public IReadOnlyList<string> InitialAdditionalActionIds { get; }
    public int MaximumVisibleAdditionalActions { get; }
    public IReadOnlyList<LocationContextActionRuleDefinition> ContextActionRules { get; }
    public IReadOnlyList<LocationStateActionRuleDefinition> StateActionRules { get; }
}

/// <summary>Reusable, faction-neutral composition of a location archetype, state and possible context.</summary>
public sealed class LocationScenarioProfileDefinition
{
    public LocationScenarioProfileDefinition(
        string id,
        string archetypeId,
        string variantId,
        string stateProfileId,
        string contentProfileId,
        IEnumerable<string>? anchorKinds,
        IEnumerable<string>? terrainTagsAny,
        IEnumerable<string>? initialModifierPoolIds,
        string claimEligibility,
        LocationScenarioActionSetDefinition actionSet,
        IEnumerable<string>? evidencePoolIds,
        IEnumerable<string>? findingPoolIds,
        IEnumerable<string>? consequencePoolIds,
        IEnumerable<string>? connectionTags)
    {
        Id = LocationStateChannelDefinition.RequireText(id, nameof(id));
        ArchetypeId = LocationStateChannelDefinition.RequireText(archetypeId, nameof(archetypeId));
        VariantId = LocationStateChannelDefinition.RequireText(variantId, nameof(variantId));
        StateProfileId = LocationStateChannelDefinition.RequireText(stateProfileId, nameof(stateProfileId));
        ContentProfileId = LocationStateChannelDefinition.RequireText(contentProfileId, nameof(contentProfileId));
        AnchorKinds = LocationStateChannelDefinition.Normalize(anchorKinds, nameof(anchorKinds));
        TerrainTagsAny = LocationContextActionRuleDefinition.NormalizeOptional(terrainTagsAny);
        InitialModifierPoolIds = LocationContextActionRuleDefinition.NormalizeOptional(initialModifierPoolIds);
        ClaimEligibility = LocationStateChannelDefinition.RequireText(claimEligibility, nameof(claimEligibility));
        ActionSet = actionSet ?? throw new ArgumentNullException(nameof(actionSet));
        EvidencePoolIds = LocationContextActionRuleDefinition.NormalizeOptional(evidencePoolIds);
        FindingPoolIds = LocationContextActionRuleDefinition.NormalizeOptional(findingPoolIds);
        ConsequencePoolIds = LocationContextActionRuleDefinition.NormalizeOptional(consequencePoolIds);
        ConnectionTags = LocationContextActionRuleDefinition.NormalizeOptional(connectionTags);
    }

    public string Id { get; }
    public string ArchetypeId { get; }
    public string VariantId { get; }
    public string StateProfileId { get; }
    public string ContentProfileId { get; }
    public IReadOnlyList<string> AnchorKinds { get; }
    public IReadOnlyList<string> TerrainTagsAny { get; }
    public IReadOnlyList<string> InitialModifierPoolIds { get; }
    public string ClaimEligibility { get; }
    public LocationScenarioActionSetDefinition ActionSet { get; }
    public IReadOnlyList<string> EvidencePoolIds { get; }
    public IReadOnlyList<string> FindingPoolIds { get; }
    public IReadOnlyList<string> ConsequencePoolIds { get; }
    public IReadOnlyList<string> ConnectionTags { get; }
}

public sealed class FindingAnalysisDefinition
{
    public FindingAnalysisDefinition(int minDurationDays, int maxDurationDays, int minKnowledgePoints, int maxKnowledgePoints, string archiveKind, string explanation)
    {
        if (minDurationDays < 0 || maxDurationDays < minDurationDays) throw new LocationDataException("Finding analysis duration range is invalid.");
        if (minKnowledgePoints < 0 || maxKnowledgePoints < minKnowledgePoints) throw new LocationDataException("Finding analysis Knowledge range is invalid.");
        MinDurationDays = minDurationDays;
        MaxDurationDays = maxDurationDays;
        MinKnowledgePoints = minKnowledgePoints;
        MaxKnowledgePoints = maxKnowledgePoints;
        ArchiveKind = LocationStateChannelDefinition.RequireText(archiveKind, nameof(archiveKind));
        Explanation = LocationStateChannelDefinition.RequireText(explanation, nameof(explanation));
    }

    public int MinDurationDays { get; }
    public int MaxDurationDays { get; }
    public int MinKnowledgePoints { get; }
    public int MaxKnowledgePoints { get; }
    public string ArchiveKind { get; }
    public string Explanation { get; }
}

public sealed class FindingDefinition
{
    public FindingDefinition(string id, string category, string fieldDescription, bool requiresPhysicalReturn, FindingAnalysisDefinition analysis, IEnumerable<string>? sourceTags = null, string? repeatPolicy = null)
    {
        Id = LocationStateChannelDefinition.RequireText(id, nameof(id));
        Category = LocationStateChannelDefinition.RequireText(category, nameof(category));
        FieldDescription = LocationStateChannelDefinition.RequireText(fieldDescription, nameof(fieldDescription));
        RequiresPhysicalReturn = requiresPhysicalReturn;
        Analysis = analysis ?? throw new ArgumentNullException(nameof(analysis));
        SourceTags = LocationContextActionRuleDefinition.NormalizeOptional(sourceTags);
        RepeatPolicy = string.IsNullOrWhiteSpace(repeatPolicy) ? "once-per-world" : repeatPolicy.Trim();
    }

    public string Id { get; }
    public string Category { get; }
    public string FieldDescription { get; }
    public bool RequiresPhysicalReturn { get; }
    public FindingAnalysisDefinition Analysis { get; }
    public IReadOnlyList<string> SourceTags { get; }
    public string RepeatPolicy { get; }
}

public sealed class FindingTableEntryDefinition
{
    public FindingTableEntryDefinition(string? findingId, int weight, IEnumerable<string>? interactionStateIds = null,
        IEnumerable<string>? operationalStateIds = null, IEnumerable<string>? presenceStateIds = null,
        IEnumerable<string>? requiredContextTagsAny = null)
    {
        FindingId = string.IsNullOrWhiteSpace(findingId) ? null : findingId.Trim();
        Weight = weight > 0 ? weight : throw new ArgumentOutOfRangeException(nameof(weight), "Finding table weight must be positive.");
        InteractionStateIds = LocationContextActionRuleDefinition.NormalizeOptional(interactionStateIds);
        OperationalStateIds = LocationContextActionRuleDefinition.NormalizeOptional(operationalStateIds);
        PresenceStateIds = LocationContextActionRuleDefinition.NormalizeOptional(presenceStateIds);
        RequiredContextTagsAny = LocationContextActionRuleDefinition.NormalizeOptional(requiredContextTagsAny);
    }

    public string? FindingId { get; }
    public int Weight { get; }
    public IReadOnlyList<string> InteractionStateIds { get; }
    public IReadOnlyList<string> OperationalStateIds { get; }
    public IReadOnlyList<string> PresenceStateIds { get; }
    public IReadOnlyList<string> RequiredContextTagsAny { get; }

    public bool Matches(SpecialLocationState location) =>
        (InteractionStateIds.Count == 0 || InteractionStateIds.Contains(location.InteractionStateId, StringComparer.Ordinal)) &&
        (OperationalStateIds.Count == 0 || OperationalStateIds.Contains(location.OperationalStateId, StringComparer.Ordinal)) &&
        (PresenceStateIds.Count == 0 || PresenceStateIds.Contains(location.PresenceStateId, StringComparer.Ordinal)) &&
        (RequiredContextTagsAny.Count == 0 || RequiredContextTagsAny.Any(location.ContextTags.Contains));
}

public sealed class FindingTableDefinition
{
    public FindingTableDefinition(string id, int rolls, string repeatPolicy, IEnumerable<FindingTableEntryDefinition> entries)
    {
        Id = LocationStateChannelDefinition.RequireText(id, nameof(id));
        Rolls = rolls;
        RepeatPolicy = LocationStateChannelDefinition.RequireText(repeatPolicy, nameof(repeatPolicy));
        Entries = new List<FindingTableEntryDefinition>(entries ?? throw new ArgumentNullException(nameof(entries)));
    }
    public string Id { get; }
    public int Rolls { get; }
    public string RepeatPolicy { get; }
    public IReadOnlyList<FindingTableEntryDefinition> Entries { get; }
}

public sealed class ContextDefinition
{
    public ContextDefinition(string id, IEnumerable<string> applicableArchetypeIds, IEnumerable<string> contextTags, IEnumerable<string>? discoverableBy, IEnumerable<string>? candidateEvidenceIds, IEnumerable<string>? candidateActionTags, IEnumerable<string>? candidateConsequenceFamilies)
    {
        Id = LocationStateChannelDefinition.RequireText(id, nameof(id));
        ApplicableArchetypeIds = LocationStateChannelDefinition.Normalize(applicableArchetypeIds, nameof(applicableArchetypeIds));
        ContextTags = LocationStateChannelDefinition.Normalize(contextTags, nameof(contextTags));
        DiscoverableBy = LocationContextActionRuleDefinition.NormalizeOptional(discoverableBy);
        CandidateEvidenceIds = LocationContextActionRuleDefinition.NormalizeOptional(candidateEvidenceIds);
        CandidateActionTags = LocationContextActionRuleDefinition.NormalizeOptional(candidateActionTags);
        CandidateConsequenceFamilies = LocationContextActionRuleDefinition.NormalizeOptional(candidateConsequenceFamilies);
    }

    public string Id { get; }
    public IReadOnlyList<string> ApplicableArchetypeIds { get; }
    public IReadOnlyList<string> ContextTags { get; }
    public IReadOnlyList<string> DiscoverableBy { get; }
    public IReadOnlyList<string> CandidateEvidenceIds { get; }
    public IReadOnlyList<string> CandidateActionTags { get; }
    public IReadOnlyList<string> CandidateConsequenceFamilies { get; }
}

public sealed class SituationDefinition
{
    public SituationDefinition(string id, string kind, IEnumerable<string>? possibleSourceKinds, IEnumerable<string>? urgencyLabels, IEnumerable<string>? responseActionTags, IEnumerable<string>? resolutionTags,
        int brokenPromiseTrustDelta = 0, string? brokenPromiseMemoryId = null)
    {
        Id = LocationStateChannelDefinition.RequireText(id, nameof(id));
        Kind = LocationStateChannelDefinition.RequireText(kind, nameof(kind));
        PossibleSourceKinds = LocationContextActionRuleDefinition.NormalizeOptional(possibleSourceKinds);
        UrgencyLabels = LocationContextActionRuleDefinition.NormalizeOptional(urgencyLabels);
        ResponseActionTags = LocationStateChannelDefinition.Normalize(responseActionTags, nameof(responseActionTags));
        ResolutionTags = LocationStateChannelDefinition.Normalize(resolutionTags, nameof(resolutionTags));
        BrokenPromiseTrustDelta = brokenPromiseTrustDelta;
        BrokenPromiseMemoryId = string.IsNullOrWhiteSpace(brokenPromiseMemoryId) ? null : brokenPromiseMemoryId.Trim();
    }

    public string Id { get; }
    public string Kind { get; }
    public IReadOnlyList<string> PossibleSourceKinds { get; }
    public IReadOnlyList<string> UrgencyLabels { get; }
    public IReadOnlyList<string> ResponseActionTags { get; }
    public IReadOnlyList<string> ResolutionTags { get; }
    public int BrokenPromiseTrustDelta { get; }
    public string? BrokenPromiseMemoryId { get; }
}

/// <summary>A reusable offer effect. It may grant expedition logistics or information, never materials.</summary>
public sealed class FactionOfferContentEffectDefinition
{
    public FactionOfferContentEffectDefinition(string kind, string? referenceId = null, int supplies = 0, int medicine = 0)
    {
        Kind = LocationStateChannelDefinition.RequireText(kind, nameof(kind));
        ReferenceId = string.IsNullOrWhiteSpace(referenceId) ? null : referenceId.Trim();
        if (supplies < 0 || medicine < 0) throw new LocationDataException("Faction offer logistics amounts must not be negative.");
        Supplies = supplies;
        Medicine = medicine;
    }

    public string Kind { get; }
    public string? ReferenceId { get; }
    public int Supplies { get; }
    public int Medicine { get; }
}

/// <summary>
/// Static faction-offer content. It selects reusable profile values, never a generated faction
/// instance, territory or specific location.
/// </summary>
public sealed class FactionOfferContentDefinition
{
    public FactionOfferContentDefinition(string id, string title, string description, IEnumerable<string> eligibleProfileTags, IEnumerable<string>? requiredContactStatuses, int knowledgePointCost, IEnumerable<FactionOfferContentEffectDefinition> effects, string repeatPolicy)
    {
        Id = LocationStateChannelDefinition.RequireText(id, nameof(id));
        Title = LocationStateChannelDefinition.RequireText(title, nameof(title));
        Description = LocationStateChannelDefinition.RequireText(description, nameof(description));
        EligibleProfileTags = LocationStateChannelDefinition.Normalize(eligibleProfileTags, nameof(eligibleProfileTags));
        RequiredContactStatuses = LocationContextActionRuleDefinition.NormalizeOptional(requiredContactStatuses);
        if (knowledgePointCost < 0) throw new LocationDataException("Faction offer Knowledge cost must not be negative.");
        KnowledgePointCost = knowledgePointCost;
        Effects = (effects ?? throw new ArgumentNullException(nameof(effects))).ToList();
        if (Effects.Count == 0) throw new LocationDataException($"Faction offer '{Id}' must define an effect.");
        RepeatPolicy = LocationStateChannelDefinition.RequireText(repeatPolicy, nameof(repeatPolicy));
    }

    public string Id { get; }
    public string Title { get; }
    public string Description { get; }
    public IReadOnlyList<string> EligibleProfileTags { get; }
    public IReadOnlyList<string> RequiredContactStatuses { get; }
    public int KnowledgePointCost { get; }
    public IReadOnlyList<FactionOfferContentEffectDefinition> Effects { get; }
    public string RepeatPolicy { get; }
}

/// <summary>Stable semantic memory key and its neutral provenance. Runtime supplies the faction and day.</summary>
public sealed class FactionMemoryDefinition
{
    public FactionMemoryDefinition(string id, string description, IEnumerable<string>? provenanceTags = null)
    {
        Id = LocationStateChannelDefinition.RequireText(id, nameof(id));
        Description = LocationStateChannelDefinition.RequireText(description, nameof(description));
        ProvenanceTags = LocationContextActionRuleDefinition.NormalizeOptional(provenanceTags);
    }

    public string Id { get; }
    public string Description { get; }
    public IReadOnlyList<string> ProvenanceTags { get; }
}

/// <summary>Typed target-schema content that is static and never identifies generated world instances.</summary>
public sealed class CrossSystemAuthoringBundle
{
    public static readonly CrossSystemAuthoringBundle Empty = new(
        Enumerable.Empty<LocationStateProfileDefinition>(), Enumerable.Empty<LocationScenarioProfileDefinition>(),
        Enumerable.Empty<FindingDefinition>(), Enumerable.Empty<ContextDefinition>(), Enumerable.Empty<SituationDefinition>(),
        Enumerable.Empty<FactionOfferContentDefinition>(), Enumerable.Empty<FactionMemoryDefinition>());

    public CrossSystemAuthoringBundle(
        IEnumerable<LocationStateProfileDefinition> stateProfiles,
        IEnumerable<LocationScenarioProfileDefinition> scenarioProfiles,
        IEnumerable<FindingDefinition> findings,
        IEnumerable<ContextDefinition> contexts,
        IEnumerable<SituationDefinition> situations,
        IEnumerable<FactionOfferContentDefinition> factionOffers,
        IEnumerable<FactionMemoryDefinition> factionMemories,
        IEnumerable<FindingTableDefinition>? findingTables = null)
    {
        StateProfiles = ToDictionary(stateProfiles, profile => profile.Id, "state profile");
        ScenarioProfiles = ToDictionary(scenarioProfiles, profile => profile.Id, "scenario profile");
        Findings = ToDictionary(findings, finding => finding.Id, "finding");
        FindingTables = ToDictionary(findingTables ?? Enumerable.Empty<FindingTableDefinition>(), table => table.Id, "finding table");
        Contexts = ToDictionary(contexts, context => context.Id, "context");
        Situations = ToDictionary(situations, situation => situation.Id, "situation");
        FactionOffers = ToDictionary(factionOffers, offer => offer.Id, "faction offer");
        FactionMemories = ToDictionary(factionMemories, memory => memory.Id, "faction memory");
    }

    public IReadOnlyDictionary<string, LocationStateProfileDefinition> StateProfiles { get; }
    public IReadOnlyDictionary<string, LocationScenarioProfileDefinition> ScenarioProfiles { get; }
    public IReadOnlyDictionary<string, FindingDefinition> Findings { get; }
    public IReadOnlyDictionary<string, FindingTableDefinition> FindingTables { get; }
    public IReadOnlyDictionary<string, ContextDefinition> Contexts { get; }
    public IReadOnlyDictionary<string, SituationDefinition> Situations { get; }
    public IReadOnlyDictionary<string, FactionOfferContentDefinition> FactionOffers { get; }
    public IReadOnlyDictionary<string, FactionMemoryDefinition> FactionMemories { get; }

    private static IReadOnlyDictionary<string, T> ToDictionary<T>(IEnumerable<T>? values, Func<T, string> id, string kind)
    {
        try
        {
            return (values ?? throw new ArgumentNullException(nameof(values))).ToDictionary(id, StringComparer.Ordinal);
        }
        catch (ArgumentException)
        {
            throw new LocationDataException($"{kind} IDs must be unique.");
        }
    }
}

/// <summary>Parser for target schema v2 authoring documents. It deliberately owns no world state.</summary>
public static class CrossSystemAuthoringDataLoader
{
    public static CrossSystemAuthoringBundle LoadFromJson(IEnumerable<string> jsonDocuments)
    {
        if (jsonDocuments == null) throw new ArgumentNullException(nameof(jsonDocuments));
        var stateProfiles = new List<LocationStateProfileDefinition>();
        var scenarioProfiles = new List<LocationScenarioProfileDefinition>();
        var findings = new List<FindingDefinition>();
        var findingTables = new List<FindingTableDefinition>();
        var contexts = new List<ContextDefinition>();
        var situations = new List<SituationDefinition>();
        var factionOffers = new List<FactionOfferContentDefinition>();
        var factionMemories = new List<FactionMemoryDefinition>();

        foreach (var json in jsonDocuments.Where(json => !string.IsNullOrWhiteSpace(json)))
        {
            JObject document;
            try { document = JObject.Parse(json); }
            catch (Exception ex) { throw new LocationDataException($"Malformed target-schema JSON document: {ex.Message}"); }
            if ((int?)document["schemaVersion"] != 2)
            {
                throw new LocationDataException("Target-schema authoring documents must use schemaVersion 2.");
            }

            var items = document["items"] as JArray ?? new JArray();
            switch ((string?)document["documentType"])
            {
                case "location-state-profiles": stateProfiles.AddRange(items.OfType<JObject>().Select(ParseStateProfile)); break;
                case "location-scenario-profiles": scenarioProfiles.AddRange(items.OfType<JObject>().Select(ParseScenarioProfile)); break;
                case "findings": findings.AddRange(items.OfType<JObject>().Select(ParseFinding)); break;
                case "finding-tables": findingTables.AddRange(items.OfType<JObject>().Select(ParseFindingTable)); break;
                case "context-definitions": contexts.AddRange(items.OfType<JObject>().Select(ParseContext)); break;
                case "situation-definitions": situations.AddRange(items.OfType<JObject>().Select(ParseSituation)); break;
                case "faction-offers": factionOffers.AddRange(items.OfType<JObject>().Select(ParseFactionOffer)); break;
                case "faction-memory-definitions": factionMemories.AddRange(items.OfType<JObject>().Select(ParseFactionMemory)); break;
                default: throw new LocationDataException($"Unsupported target-schema documentType '{(string?)document["documentType"]}'.");
            }
        }

        return new CrossSystemAuthoringBundle(stateProfiles, scenarioProfiles, findings, contexts, situations, factionOffers, factionMemories, findingTables);
    }

    private static LocationStateProfileDefinition ParseStateProfile(JObject item)
    {
        var channels = new Dictionary<string, LocationStateChannelDefinition>(StringComparer.Ordinal);
        foreach (var property in (item["channels"] as JObject ?? new JObject()).Properties())
        {
            var channel = property.Value as JObject ?? throw new LocationDataException($"State channel '{property.Name}' must be an object.");
            channels.Add(property.Name, new LocationStateChannelDefinition(Text(channel, "initial"), Strings(channel, "values")));
        }
        return new LocationStateProfileDefinition(Text(item, "id"), Text(item, "archetypeId"), channels);
    }

    private static LocationScenarioProfileDefinition ParseScenarioProfile(JObject item)
    {
        var worldgen = item["worldgen"] as JObject ?? new JObject();
        var actions = item["actionSet"] as JObject ?? new JObject();
        var contextRules = (actions["contextActionRules"] as JArray ?? new JArray()).OfType<JObject>().Select(rule =>
            new LocationContextActionRuleDefinition(Strings(rule, "whenKnownContextTagsAny"), Strings(rule, "whenHiddenContextTagsAny"), Strings(rule, "addActionIds"), Text(rule, "knownRequirementDisclosure"))).ToList();
        var stateRules = (actions["stateActionRules"] as JArray ?? new JArray()).OfType<JObject>().Select(rule =>
            new LocationStateActionRuleDefinition(
                Strings(rule, "whenInteractionStatesAny"),
                Strings(rule, "whenOperationalStatesAny"),
                Strings(rule, "whenPresenceStatesAny"),
                Strings(rule, "addActionIds"))).ToList();
        return new LocationScenarioProfileDefinition(
            Text(item, "id"), Text(item, "archetypeId"), Text(item, "variantId"), Text(item, "stateProfileId"), Text(item, "contentProfileId"),
            Strings(worldgen, "anchorKinds"), Strings(worldgen, "terrainTagsAny"), Strings(worldgen, "initialModifierPoolIds"), Text(worldgen, "claimEligibility"),
            new LocationScenarioActionSetDefinition(Strings(actions, "sharedActionIds"), Strings(actions, "initialAdditionalActionIds"), Int(actions, "maximumVisibleAdditionalActions"), contextRules, stateRules),
            Strings(item, "evidencePoolIds"), Strings(item, "findingPoolIds"), Strings(item, "consequencePoolIds"), Strings(item, "connectionTags"));
    }

    private static FindingDefinition ParseFinding(JObject item)
    {
        var analysis = item["analysis"] as JObject ?? throw new LocationDataException($"Finding '{Text(item, "id")}' requires analysis.");
        var duration = analysis["durationDays"] as JObject ?? new JObject();
        var knowledge = analysis["knowledgePoints"] as JObject ?? new JObject();
        return new FindingDefinition(Text(item, "id"), Text(item, "category"), Text(item, "fieldDescription"), (bool?)item["requiresPhysicalReturn"] ?? false,
            new FindingAnalysisDefinition(Int(duration, "min"), Int(duration, "max"), Int(knowledge, "min"), Int(knowledge, "max"), Text(analysis, "archiveKind"), Text(analysis, "explanation")),
            Strings(item, "sourceTags"), (string?)item["repeatPolicy"]);
    }

    private static FindingTableDefinition ParseFindingTable(JObject item) => new(
        Text(item, "id"), Int(item, "rolls"), Text(item, "repeatPolicy"),
        (item["entries"] as JArray ?? new JArray()).OfType<JObject>().Select(entry => new FindingTableEntryDefinition(
            (string?)entry["findingId"], Int(entry, "weight"), Strings(entry, "whenInteractionStatesAny"),
            Strings(entry, "whenOperationalStatesAny"), Strings(entry, "whenPresenceStatesAny"), Strings(entry, "requiresContextTagsAny"))));

    private static ContextDefinition ParseContext(JObject item) => new(Text(item, "id"), Strings(item, "applicableArchetypeIds"), Strings(item, "contextTags"), Strings(item, "discoverableBy"), Strings(item, "candidateEvidenceIds"), Strings(item, "candidateActionTags"), Strings(item, "candidateConsequenceFamilies"));

    private static SituationDefinition ParseSituation(JObject item)
    {
        var responseTags = (item["responseOptions"] as JArray ?? new JArray()).OfType<JObject>().Select(option => Text(option, "actionTag"));
        return new SituationDefinition(Text(item, "id"), Text(item, "kind"), Strings(item, "possibleSourceKinds"), Strings(item, "urgencyLabels"), responseTags, Strings(item, "resolutionTags"),
            (int?)item["brokenPromiseTrustDelta"] ?? 0, (string?)item["brokenPromiseMemoryId"]);
    }

    private static FactionOfferContentDefinition ParseFactionOffer(JObject item)
    {
        var effects = (item["effects"] as JArray ?? new JArray()).OfType<JObject>().Select(effect => new FactionOfferContentEffectDefinition(
            Text(effect, "kind"),
            (string?)effect["referenceId"] ?? (string?)effect["reportTemplateId"],
            (int?)effect["supplies"] ?? 0,
            (int?)effect["medicine"] ?? 0));
        return new FactionOfferContentDefinition(Text(item, "id"), Text(item, "title"), Text(item, "description"), Strings(item, "eligibleProfileTags"), Strings(item, "requiresContactStatusAny"), Int(item, "knowledgePointCost"), effects, Text(item, "repeatPolicy"));
    }

    private static FactionMemoryDefinition ParseFactionMemory(JObject item) => new(Text(item, "id"), Text(item, "description"), Strings(item, "provenanceTags"));

    private static string Text(JObject item, string field) => LocationStateChannelDefinition.RequireText((string?)item[field], field);
    private static int Int(JObject item, string field) => (int?)item[field] ?? throw new LocationDataException($"{field} must be an integer.");
    private static IReadOnlyList<string> Strings(JObject item, string field) => (item[field] as JArray ?? new JArray()).Values<string>().Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim()).ToList();
}
}
