#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Game.App;

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

public sealed class LocationScenarioActionSetDefinition
{
    public LocationScenarioActionSetDefinition(
        IEnumerable<string>? sharedActionIds,
        IEnumerable<string>? initialAdditionalActionIds,
        int maximumVisibleAdditionalActions,
        IEnumerable<LocationContextActionRuleDefinition>? contextActionRules)
    {
        SharedActionIds = LocationContextActionRuleDefinition.NormalizeOptional(sharedActionIds);
        InitialAdditionalActionIds = LocationContextActionRuleDefinition.NormalizeOptional(initialAdditionalActionIds);
        MaximumVisibleAdditionalActions = maximumVisibleAdditionalActions;
        ContextActionRules = (contextActionRules ?? Enumerable.Empty<LocationContextActionRuleDefinition>()).ToList();
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
    public SituationDefinition(string id, string kind, IEnumerable<string>? possibleSourceKinds, IEnumerable<string>? urgencyLabels, IEnumerable<string>? responseActionTags, IEnumerable<string>? resolutionTags)
    {
        Id = LocationStateChannelDefinition.RequireText(id, nameof(id));
        Kind = LocationStateChannelDefinition.RequireText(kind, nameof(kind));
        PossibleSourceKinds = LocationContextActionRuleDefinition.NormalizeOptional(possibleSourceKinds);
        UrgencyLabels = LocationContextActionRuleDefinition.NormalizeOptional(urgencyLabels);
        ResponseActionTags = LocationStateChannelDefinition.Normalize(responseActionTags, nameof(responseActionTags));
        ResolutionTags = LocationStateChannelDefinition.Normalize(resolutionTags, nameof(resolutionTags));
    }

    public string Id { get; }
    public string Kind { get; }
    public IReadOnlyList<string> PossibleSourceKinds { get; }
    public IReadOnlyList<string> UrgencyLabels { get; }
    public IReadOnlyList<string> ResponseActionTags { get; }
    public IReadOnlyList<string> ResolutionTags { get; }
}

/// <summary>Typed target-schema content that is static and never identifies generated world instances.</summary>
public sealed class CrossSystemAuthoringBundle
{
    public static readonly CrossSystemAuthoringBundle Empty = new(
        Enumerable.Empty<LocationStateProfileDefinition>(), Enumerable.Empty<LocationScenarioProfileDefinition>(),
        Enumerable.Empty<FindingDefinition>(), Enumerable.Empty<ContextDefinition>(), Enumerable.Empty<SituationDefinition>());

    public CrossSystemAuthoringBundle(
        IEnumerable<LocationStateProfileDefinition> stateProfiles,
        IEnumerable<LocationScenarioProfileDefinition> scenarioProfiles,
        IEnumerable<FindingDefinition> findings,
        IEnumerable<ContextDefinition> contexts,
        IEnumerable<SituationDefinition> situations)
    {
        StateProfiles = ToDictionary(stateProfiles, profile => profile.Id, "state profile");
        ScenarioProfiles = ToDictionary(scenarioProfiles, profile => profile.Id, "scenario profile");
        Findings = ToDictionary(findings, finding => finding.Id, "finding");
        Contexts = ToDictionary(contexts, context => context.Id, "context");
        Situations = ToDictionary(situations, situation => situation.Id, "situation");
    }

    public IReadOnlyDictionary<string, LocationStateProfileDefinition> StateProfiles { get; }
    public IReadOnlyDictionary<string, LocationScenarioProfileDefinition> ScenarioProfiles { get; }
    public IReadOnlyDictionary<string, FindingDefinition> Findings { get; }
    public IReadOnlyDictionary<string, ContextDefinition> Contexts { get; }
    public IReadOnlyDictionary<string, SituationDefinition> Situations { get; }

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
        var contexts = new List<ContextDefinition>();
        var situations = new List<SituationDefinition>();

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
                case "context-definitions": contexts.AddRange(items.OfType<JObject>().Select(ParseContext)); break;
                case "situation-definitions": situations.AddRange(items.OfType<JObject>().Select(ParseSituation)); break;
                default: throw new LocationDataException($"Unsupported target-schema documentType '{(string?)document["documentType"]}'.");
            }
        }

        return new CrossSystemAuthoringBundle(stateProfiles, scenarioProfiles, findings, contexts, situations);
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
        return new LocationScenarioProfileDefinition(
            Text(item, "id"), Text(item, "archetypeId"), Text(item, "variantId"), Text(item, "stateProfileId"), Text(item, "contentProfileId"),
            Strings(worldgen, "anchorKinds"), Strings(worldgen, "terrainTagsAny"), Strings(worldgen, "initialModifierPoolIds"), Text(worldgen, "claimEligibility"),
            new LocationScenarioActionSetDefinition(Strings(actions, "sharedActionIds"), Strings(actions, "initialAdditionalActionIds"), Int(actions, "maximumVisibleAdditionalActions"), contextRules),
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

    private static ContextDefinition ParseContext(JObject item) => new(Text(item, "id"), Strings(item, "applicableArchetypeIds"), Strings(item, "contextTags"), Strings(item, "discoverableBy"), Strings(item, "candidateEvidenceIds"), Strings(item, "candidateActionTags"), Strings(item, "candidateConsequenceFamilies"));

    private static SituationDefinition ParseSituation(JObject item)
    {
        var responseTags = (item["responseOptions"] as JArray ?? new JArray()).OfType<JObject>().Select(option => Text(option, "actionTag"));
        return new SituationDefinition(Text(item, "id"), Text(item, "kind"), Strings(item, "possibleSourceKinds"), Strings(item, "urgencyLabels"), responseTags, Strings(item, "resolutionTags"));
    }

    private static string Text(JObject item, string field) => LocationStateChannelDefinition.RequireText((string?)item[field], field);
    private static int Int(JObject item, string field) => (int?)item[field] ?? throw new LocationDataException($"{field} must be an integer.");
    private static IReadOnlyList<string> Strings(JObject item, string field) => (item[field] as JArray ?? new JArray()).Values<string>().Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim()).ToList();
}
