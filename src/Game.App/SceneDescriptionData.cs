#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Game.Core;
using Newtonsoft.Json.Linq;

namespace Game.App
{

public static class SceneDescriptionValues
{
    public static readonly IReadOnlySet<string> Groups = new HashSet<string>(new[]
    {
        "opening", "interaction-state", "operational-state", "presence-state", "modifier",
        "relation-anonymous", "relation-identified", "history", "member-condition",
        "scout-return", "report-transition", "base-return"
    }, StringComparer.Ordinal);

    public static readonly IReadOnlySet<string> SubjectKinds = new HashSet<string>(new[]
    {
        "location", "contact", "scout-return", "report-event", "base-return"
    }, StringComparer.Ordinal);

    public static readonly IReadOnlySet<string> SourceKinds = new HashSet<string>(new[]
    {
        "direct-observation", "stored-observation", "observable-modifier", "visible-trace",
        "testimony", "signature-identification", "delivered-outcome"
    }, StringComparer.Ordinal);

    public static readonly IReadOnlySet<string> RepetitionPolicies = new HashSet<string>(new[]
    {
        "always", "once-per-state"
    }, StringComparer.Ordinal);
}

public sealed class SceneFragmentScopeDefinition
{
    public SceneFragmentScopeDefinition(IEnumerable<string>? archetypeIds, IEnumerable<string>? variantIds)
    {
        ArchetypeIds = Normalize(archetypeIds);
        VariantIds = Normalize(variantIds);
    }

    public IReadOnlyList<string> ArchetypeIds { get; }
    public IReadOnlyList<string> VariantIds { get; }

    internal static IReadOnlyList<string> Normalize(IEnumerable<string?>? values) =>
        (values ?? Enumerable.Empty<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
}

public sealed class SceneFragmentSourceDefinition
{
    public SceneFragmentSourceDefinition(string kind, int? maxKnownStateAgeDays, bool toleratesDoubt)
    {
        Kind = RequireText(kind, nameof(kind));
        if (!SceneDescriptionValues.SourceKinds.Contains(Kind))
            throw new LocationDataException($"Unknown scene source kind '{Kind}'.");
        if (maxKnownStateAgeDays < 0)
            throw new LocationDataException("maxKnownStateAgeDays must not be negative.");
        MaxKnownStateAgeDays = maxKnownStateAgeDays;
        ToleratesDoubt = toleratesDoubt;
    }

    public string Kind { get; }
    public int? MaxKnownStateAgeDays { get; }
    public bool ToleratesDoubt { get; }

    internal static string RequireText(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new LocationDataException($"{field} must not be empty.");
        return value.Trim();
    }
}

/// <summary>Only player-known predicates may be represented here. Unknown JSON fields are rejected.</summary>
public sealed class SceneFragmentConditionDefinition
{
    internal static readonly IReadOnlySet<string> SupportedFields = new HashSet<string>(new[]
    {
        "knownInteractionStatesAny", "knownOperationalStatesAny", "knownPresenceStatesAny",
        "knownContextTagsAny", "observableModifierIdsAny", "knowledgeLevelAtLeast",
        "requiresDoubtfulLastObservation", "forbidDoubtfulLastObservation", "maxKnownStateAgeDays",
        "contactStatusAny", "identityStage", "missionStatusAny", "memberStatusAny", "teamOutcome",
        "reportReliabilityAtLeast", "reportReliabilityBelow", "hasFindings", "hasLeads",
        "wasOverdue", "hasLostEquipment", "isSecondHandAccount", "isUrgent",
        "hasOwnArchiveEntryForLocation", "currentObservationDiffersFromStored",
        "hasLostExpeditionRecordForLocation"
    }, StringComparer.Ordinal);

    public SceneFragmentConditionDefinition(JObject source)
    {
        foreach (var property in source.Properties())
            if (!SupportedFields.Contains(property.Name))
                throw new LocationDataException($"Unknown scene condition field '{property.Name}'.");

        KnownInteractionStatesAny = Strings(source, "knownInteractionStatesAny");
        KnownOperationalStatesAny = Strings(source, "knownOperationalStatesAny");
        KnownPresenceStatesAny = Strings(source, "knownPresenceStatesAny");
        KnownContextTagsAny = Strings(source, "knownContextTagsAny");
        ObservableModifierIdsAny = Strings(source, "observableModifierIdsAny");
        ContactStatusAny = Strings(source, "contactStatusAny");
        MissionStatusAny = Strings(source, "missionStatusAny");
        MemberStatusAny = Strings(source, "memberStatusAny");
        KnowledgeLevelAtLeast = OptionalText(source, "knowledgeLevelAtLeast");
        IdentityStage = OptionalText(source, "identityStage");
        TeamOutcome = OptionalText(source, "teamOutcome");
        RequiresDoubtfulLastObservation = OptionalBool(source, "requiresDoubtfulLastObservation");
        ForbidDoubtfulLastObservation = OptionalBool(source, "forbidDoubtfulLastObservation");
        MaxKnownStateAgeDays = OptionalInt(source, "maxKnownStateAgeDays");
        ReportReliabilityAtLeast = OptionalInt(source, "reportReliabilityAtLeast");
        ReportReliabilityBelow = OptionalInt(source, "reportReliabilityBelow");
        HasFindings = OptionalBool(source, "hasFindings");
        HasLeads = OptionalBool(source, "hasLeads");
        WasOverdue = OptionalBool(source, "wasOverdue");
        HasLostEquipment = OptionalBool(source, "hasLostEquipment");
        IsSecondHandAccount = OptionalBool(source, "isSecondHandAccount");
        IsUrgent = OptionalBool(source, "isUrgent");
        HasOwnArchiveEntryForLocation = OptionalBool(source, "hasOwnArchiveEntryForLocation");
        CurrentObservationDiffersFromStored = OptionalBool(source, "currentObservationDiffersFromStored");
        HasLostExpeditionRecordForLocation = OptionalBool(source, "hasLostExpeditionRecordForLocation");

        if (MaxKnownStateAgeDays < 0) throw new LocationDataException("maxKnownStateAgeDays must not be negative.");
        ValidatePercentage(ReportReliabilityAtLeast, "reportReliabilityAtLeast");
        ValidatePercentage(ReportReliabilityBelow, "reportReliabilityBelow");
        ValidateOptional(KnowledgeLevelAtLeast, "knowledgeLevelAtLeast", "Reported", "Confirmed", "OldOrDoubtful");
        ValidateOptional(IdentityStage, "identityStage", "anonymous", "signature-recognised", "identified");
        ValidateOptional(TeamOutcome, "teamOutcome", "all-returned", "partial-return", "none-returned");
        ValidateAll(ContactStatusAny, "contactStatusAny", "Unknown", "Rumored", "Contacted", "Open", "Hostile");
        ValidateAll(MissionStatusAny, "missionStatusAny", "Returned", "Overdue", "ReturnedInjured", "Missing");
        ValidateAll(MemberStatusAny, "memberStatusAny", "Injured", "Exhausted", "Missing", "Dead", "unhurt");
    }

    public IReadOnlyList<string> KnownInteractionStatesAny { get; }
    public IReadOnlyList<string> KnownOperationalStatesAny { get; }
    public IReadOnlyList<string> KnownPresenceStatesAny { get; }
    public IReadOnlyList<string> KnownContextTagsAny { get; }
    public IReadOnlyList<string> ObservableModifierIdsAny { get; }
    public IReadOnlyList<string> ContactStatusAny { get; }
    public IReadOnlyList<string> MissionStatusAny { get; }
    public IReadOnlyList<string> MemberStatusAny { get; }
    public string? KnowledgeLevelAtLeast { get; }
    public string? IdentityStage { get; }
    public string? TeamOutcome { get; }
    public bool? RequiresDoubtfulLastObservation { get; }
    public bool? ForbidDoubtfulLastObservation { get; }
    public int? MaxKnownStateAgeDays { get; }
    public int? ReportReliabilityAtLeast { get; }
    public int? ReportReliabilityBelow { get; }
    public bool? HasFindings { get; }
    public bool? HasLeads { get; }
    public bool? WasOverdue { get; }
    public bool? HasLostEquipment { get; }
    public bool? IsSecondHandAccount { get; }
    public bool? IsUrgent { get; }
    public bool? HasOwnArchiveEntryForLocation { get; }
    public bool? CurrentObservationDiffersFromStored { get; }
    public bool? HasLostExpeditionRecordForLocation { get; }

    private static IReadOnlyList<string> Strings(JObject item, string field) =>
        SceneFragmentScopeDefinition.Normalize((item[field] as JArray)?.Values<string>());
    private static string? OptionalText(JObject item, string field) =>
        string.IsNullOrWhiteSpace((string?)item[field]) ? null : ((string)item[field]!).Trim();
    private static bool? OptionalBool(JObject item, string field) => item[field]?.Type == JTokenType.Null ? null : (bool?)item[field];
    private static int? OptionalInt(JObject item, string field) => item[field]?.Type == JTokenType.Null ? null : (int?)item[field];
    private static void ValidatePercentage(int? value, string field)
    {
        if (value is < 0 or > 100) throw new LocationDataException($"{field} must be between 0 and 100.");
    }
    private static void ValidateOptional(string? value, string field, params string[] allowed)
    {
        if (value != null && !allowed.Contains(value, StringComparer.Ordinal)) throw new LocationDataException($"{field} has unknown value '{value}'.");
    }
    private static void ValidateAll(IEnumerable<string> values, string field, params string[] allowed)
    {
        foreach (var value in values)
            if (!allowed.Contains(value, StringComparer.Ordinal)) throw new LocationDataException($"{field} has unknown value '{value}'.");
    }
}

public sealed class SceneFragmentDefinition
{
    public SceneFragmentDefinition(string id, string group, string subjectKind, SceneFragmentScopeDefinition appliesTo,
        SceneFragmentConditionDefinition when, SceneFragmentSourceDefinition source, int priority, string repetition,
        IEnumerable<string>? supersedesFragmentIds, string? exclusiveTag, string? visualId, string textId)
    {
        Id = SceneFragmentSourceDefinition.RequireText(id, nameof(id));
        Group = SceneFragmentSourceDefinition.RequireText(group, nameof(group));
        SubjectKind = SceneFragmentSourceDefinition.RequireText(subjectKind, nameof(subjectKind));
        TextId = SceneFragmentSourceDefinition.RequireText(textId, nameof(textId));
        if (!Id.StartsWith("frag-", StringComparison.Ordinal)) throw new LocationDataException($"Scene fragment id '{Id}' must start with 'frag-'.");
        if (!SceneDescriptionValues.Groups.Contains(Group)) throw new LocationDataException($"Unknown scene group '{Group}'.");
        if (Group == "modifier") throw new LocationDataException("The scene modifier group is reserved for derived modifier text.");
        if (!SceneDescriptionValues.SubjectKinds.Contains(SubjectKind)) throw new LocationDataException($"Unknown scene subject kind '{SubjectKind}'.");
        if (!SceneDescriptionValues.RepetitionPolicies.Contains(repetition)) throw new LocationDataException($"Unknown scene repetition policy '{repetition}'.");
        AppliesTo = appliesTo;
        When = when;
        Source = source;
        if (When.MaxKnownStateAgeDays != null && Source.MaxKnownStateAgeDays != null && When.MaxKnownStateAgeDays != Source.MaxKnownStateAgeDays)
            throw new LocationDataException($"Scene fragment '{Id}' declares conflicting maxKnownStateAgeDays values.");
        Priority = priority;
        Repetition = repetition;
        SupersedesFragmentIds = SceneFragmentScopeDefinition.Normalize(supersedesFragmentIds);
        ExclusiveTag = string.IsNullOrWhiteSpace(exclusiveTag) ? null : exclusiveTag.Trim();
        VisualId = string.IsNullOrWhiteSpace(visualId) ? null : visualId.Trim();
    }

    public string Id { get; }
    public string Group { get; }
    public string SubjectKind { get; }
    public SceneFragmentScopeDefinition AppliesTo { get; }
    public SceneFragmentConditionDefinition When { get; }
    public SceneFragmentSourceDefinition Source { get; }
    public int Priority { get; }
    public string Repetition { get; }
    public IReadOnlyList<string> SupersedesFragmentIds { get; }
    public string? ExclusiveTag { get; }
    public string? VisualId { get; }
    public string TextId { get; }
}

public sealed class ScenePolicyDefinition
{
    public ScenePolicyDefinition(string id, string subjectKind, string? archetypeId, string questionTextId,
        SceneFragmentConditionDefinition questionResolvedWhen, IEnumerable<string> ordering,
        IEnumerable<IEnumerable<string>> paragraphing, int maxFragments, IReadOnlyDictionary<string, int> maxPerGroup)
    {
        Id = SceneFragmentSourceDefinition.RequireText(id, nameof(id));
        SubjectKind = SceneFragmentSourceDefinition.RequireText(subjectKind, nameof(subjectKind));
        QuestionTextId = SceneFragmentSourceDefinition.RequireText(questionTextId, nameof(questionTextId));
        if (!SceneDescriptionValues.SubjectKinds.Contains(SubjectKind)) throw new LocationDataException($"Unknown scene subject kind '{SubjectKind}'.");
        ArchetypeId = string.IsNullOrWhiteSpace(archetypeId) ? null : archetypeId.Trim();
        QuestionResolvedWhen = questionResolvedWhen;
        Ordering = SceneFragmentScopeDefinition.Normalize(ordering);
        Paragraphing = paragraphing.Select(group => (IReadOnlyList<string>)SceneFragmentScopeDefinition.Normalize(group)).ToList();
        if (Ordering.Count == 0 || maxFragments < 1) throw new LocationDataException($"Scene policy '{Id}' needs ordering and a positive maxFragments.");
        MaxFragments = maxFragments;
        MaxPerGroup = maxPerGroup;
    }

    public string Id { get; }
    public string SubjectKind { get; }
    public string? ArchetypeId { get; }
    public string QuestionTextId { get; }
    public SceneFragmentConditionDefinition QuestionResolvedWhen { get; }
    public IReadOnlyList<string> Ordering { get; }
    public IReadOnlyList<IReadOnlyList<string>> Paragraphing { get; }
    public int MaxFragments { get; }
    public IReadOnlyDictionary<string, int> MaxPerGroup { get; }
}

public sealed class SceneLocaleDefinition
{
    public SceneLocaleDefinition(string locale, string? fallbackLocale, bool isDefault, IReadOnlyDictionary<string, string> texts)
    {
        Locale = NormalizeLocale(locale);
        FallbackLocale = string.IsNullOrWhiteSpace(fallbackLocale) ? null : NormalizeLocale(fallbackLocale);
        IsDefault = isDefault;
        Texts = texts;
        if (Texts.Count == 0) throw new LocationDataException($"Scene locale '{Locale}' contains no texts.");
    }

    public string Locale { get; }
    public string? FallbackLocale { get; }
    public bool IsDefault { get; }
    public IReadOnlyDictionary<string, string> Texts { get; }

    internal static string NormalizeLocale(string? locale)
    {
        var value = SceneFragmentSourceDefinition.RequireText(locale, nameof(locale)).Replace('_', '-');
        var parts = value.Split('-', StringSplitOptions.RemoveEmptyEntries);
        return string.Join("-", parts.Select((part, index) => index == 0 ? part.ToLowerInvariant() : part.ToUpperInvariant()));
    }
}

/// <summary>Localized scene prose. Missing requested locales fall back deterministically to German/default content.</summary>
public sealed class SceneTextCatalog
{
    private readonly IReadOnlyDictionary<string, SceneLocaleDefinition> locales;

    public SceneTextCatalog(IEnumerable<SceneLocaleDefinition> locales)
    {
        try { this.locales = locales.ToDictionary(item => item.Locale, StringComparer.OrdinalIgnoreCase); }
        catch (ArgumentException) { throw new LocationDataException("Scene locale IDs must be unique."); }
        var defaults = this.locales.Values.Where(item => item.IsDefault).ToList();
        if (defaults.Count != 1) throw new LocationDataException("Exactly one scene locale must be marked as default.");
        DefaultLocale = defaults[0].Locale;
        ValidateFallbacks();
    }

    public string DefaultLocale { get; }
    public IReadOnlyCollection<SceneLocaleDefinition> Locales => locales.Values.ToList();

    public string Resolve(string textId, string? requestedLocale = null)
    {
        var key = SceneFragmentSourceDefinition.RequireText(textId, nameof(textId));
        var locale = ResolveLocale(requestedLocale);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (locale != null && visited.Add(locale.Locale))
        {
            if (locale.Texts.TryGetValue(key, out var text)) return text;
            locale = locale.FallbackLocale != null && locales.TryGetValue(locale.FallbackLocale, out var fallback)
                ? fallback
                : null;
        }
        if (locales[DefaultLocale].Texts.TryGetValue(key, out var defaultText)) return defaultText;
        throw new LocationDataException($"Scene text '{key}' does not exist in locale '{DefaultLocale}'.");
    }

    public bool ContainsInDefaultLocale(string textId) => locales[DefaultLocale].Texts.ContainsKey(textId);

    private SceneLocaleDefinition ResolveLocale(string? requestedLocale)
    {
        if (string.IsNullOrWhiteSpace(requestedLocale)) return locales[DefaultLocale];
        var normalized = SceneLocaleDefinition.NormalizeLocale(requestedLocale);
        if (locales.TryGetValue(normalized, out var exact)) return exact;
        var language = normalized.Split('-')[0];
        return locales.TryGetValue(language, out var neutral) ? neutral : locales[DefaultLocale];
    }

    private void ValidateFallbacks()
    {
        foreach (var locale in locales.Values)
        {
            if (locale.FallbackLocale != null && !locales.ContainsKey(locale.FallbackLocale))
                throw new LocationDataException($"Scene locale '{locale.Locale}' references unknown fallback locale '{locale.FallbackLocale}'.");
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var current = locale;
            while (current.FallbackLocale != null)
            {
                if (!seen.Add(current.Locale)) throw new LocationDataException($"Scene locale fallback cycle starts at '{locale.Locale}'.");
                current = locales[current.FallbackLocale];
            }
        }
    }
}

public sealed class SceneDescriptionCatalog
{
    public SceneDescriptionCatalog(IEnumerable<SceneFragmentDefinition> fragments, IEnumerable<ScenePolicyDefinition> policies, SceneTextCatalog texts)
    {
        Fragments = Unique(fragments, item => item.Id, "scene fragment");
        Policies = Unique(policies, item => item.Id, "scene policy");
        Texts = texts;
        SceneDescriptionContentValidator.ValidateStructure(this);
    }

    public IReadOnlyDictionary<string, SceneFragmentDefinition> Fragments { get; }
    public IReadOnlyDictionary<string, ScenePolicyDefinition> Policies { get; }
    public SceneTextCatalog Texts { get; }

    private static IReadOnlyDictionary<string, T> Unique<T>(IEnumerable<T> values, Func<T, string> id, string kind)
    {
        try { return values.ToDictionary(id, StringComparer.Ordinal); }
        catch (ArgumentException) { throw new LocationDataException($"{kind} IDs must be unique."); }
    }
}

public static class SceneDescriptionDataLoader
{
    public static SceneDescriptionCatalog LoadFromJson(IEnumerable<string> documents)
    {
        var fragments = new List<SceneFragmentDefinition>();
        var policies = new List<ScenePolicyDefinition>();
        var locales = new List<SceneLocaleDefinition>();
        foreach (var json in documents ?? throw new ArgumentNullException(nameof(documents)))
        {
            JObject document;
            try { document = JObject.Parse(json); }
            catch (Exception ex) { throw new LocationDataException($"Malformed scene JSON document: {ex.Message}"); }
            if ((int?)document["schemaVersion"] != 1) throw new LocationDataException("Scene documents must use schemaVersion 1.");
            switch ((string?)document["documentType"])
            {
                case "scene-fragments": ParseFragments(document, fragments); break;
                case "scene-policies": ParsePolicies(document, policies); break;
                case "scene-localization": locales.Add(ParseLocale(document)); break;
                default: throw new LocationDataException($"Unsupported scene documentType '{(string?)document["documentType"]}'.");
            }
        }
        return new SceneDescriptionCatalog(fragments, policies, new SceneTextCatalog(locales));
    }

    private static void ParseFragments(JObject document, ICollection<SceneFragmentDefinition> target)
    {
        var defaults = document["defaultsByGroup"] as JObject ?? new JObject();
        foreach (var raw in (document["items"] as JArray ?? new JArray()).OfType<JObject>())
        {
            var group = Text(raw, "group");
            var item = defaults[group] is JObject groupDefaults ? (JObject)groupDefaults.DeepClone() : new JObject();
            item.Merge(raw, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace, MergeNullValueHandling = MergeNullValueHandling.Merge });
            item["appliesTo"] ??= new JObject();
            item["when"] ??= new JObject();
            item["repetition"] ??= "always";
            item["supersedesFragmentIds"] ??= new JArray();
            var scope = item["appliesTo"] as JObject ?? throw new LocationDataException($"Fragment '{Text(item, "id")}' appliesTo must be an object.");
            var source = item["source"] as JObject ?? throw new LocationDataException($"Fragment '{Text(item, "id")}' has no effective source.");
            target.Add(new SceneFragmentDefinition(
                Text(item, "id"), Text(item, "group"), Text(item, "subjectKind"),
                new SceneFragmentScopeDefinition(Strings(scope, "archetypeIds"), Strings(scope, "variantIds")),
                new SceneFragmentConditionDefinition(item["when"] as JObject ?? new JObject()),
                new SceneFragmentSourceDefinition(Text(source, "kind"), (int?)source["maxKnownStateAgeDays"], (bool?)source["toleratesDoubt"] ?? false),
                Int(item, "priority"), Text(item, "repetition"), Strings(item, "supersedesFragmentIds"),
                (string?)item["exclusiveTag"], (string?)item["visualId"], Text(item, "textId")));
        }
    }

    private static void ParsePolicies(JObject document, ICollection<ScenePolicyDefinition> target)
    {
        foreach (var item in (document["items"] as JArray ?? new JArray()).OfType<JObject>())
        {
            var maxPerGroup = (item["maxPerGroup"] as JObject ?? new JObject()).Properties()
                .ToDictionary(property => property.Name, property => (int)property.Value, StringComparer.Ordinal);
            var paragraphing = (item["paragraphing"] as JArray ?? new JArray()).OfType<JArray>()
                .Select(group => group.Values<string>().Where(value => value != null).Cast<string>());
            target.Add(new ScenePolicyDefinition(Text(item, "id"), Text(item, "subjectKind"), (string?)item["archetypeId"],
                Text(item, "questionTextId"), new SceneFragmentConditionDefinition(item["questionResolvedWhen"] as JObject ?? new JObject()),
                Strings(item, "ordering"), paragraphing, Int(item, "maxFragments"), maxPerGroup));
        }
    }

    private static SceneLocaleDefinition ParseLocale(JObject document)
    {
        var texts = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var item in (document["items"] as JArray ?? new JArray()).OfType<JObject>())
        {
            var id = Text(item, "id");
            if (!texts.TryAdd(id, Text(item, "text"))) throw new LocationDataException($"Scene text id '{id}' is duplicated in one locale.");
        }
        return new SceneLocaleDefinition(Text(document, "locale"), (string?)document["fallbackLocale"], (bool?)document["isDefault"] ?? false, texts);
    }

    private static string Text(JObject item, string field) => SceneFragmentSourceDefinition.RequireText((string?)item[field], field);
    private static int Int(JObject item, string field) => (int?)item[field] ?? throw new LocationDataException($"{field} must be an integer.");
    private static IReadOnlyList<string> Strings(JObject item, string field) => SceneFragmentScopeDefinition.Normalize((item[field] as JArray)?.Values<string>());
}

public static class SceneDescriptionContentValidator
{
    private static readonly Regex PlaceholderPattern = new("\\{[a-zA-Z][a-zA-Z0-9]*\\}", RegexOptions.Compiled);

    public static void ValidateStructure(SceneDescriptionCatalog catalog)
    {
        foreach (var fragment in catalog.Fragments.Values)
        {
            if (!catalog.Texts.ContainsInDefaultLocale(fragment.TextId))
                throw new LocationDataException($"Scene fragment '{fragment.Id}' references missing default-locale text '{fragment.TextId}'.");
            ValidateSourcePairing(fragment);
            ValidateLocalizedText(catalog, fragment.TextId, fragment.SubjectKind);
            foreach (var supersededId in fragment.SupersedesFragmentIds)
                if (!catalog.Fragments.ContainsKey(supersededId)) throw new LocationDataException($"Scene fragment '{fragment.Id}' supersedes unknown fragment '{supersededId}'.");
        }
        ValidateSupersedesCycles(catalog);

        foreach (var policy in catalog.Policies.Values)
        {
            if (!catalog.Texts.ContainsInDefaultLocale(policy.QuestionTextId))
                throw new LocationDataException($"Scene policy '{policy.Id}' references missing question text '{policy.QuestionTextId}'.");
            foreach (var group in policy.Ordering.Concat(policy.Paragraphing.SelectMany(group => group)).Concat(policy.MaxPerGroup.Keys))
                if (!SceneDescriptionValues.Groups.Contains(group)) throw new LocationDataException($"Scene policy '{policy.Id}' references unknown group '{group}'.");
            var paragraphGroups = policy.Paragraphing.SelectMany(group => group).ToList();
            if (paragraphGroups.Count != paragraphGroups.Distinct(StringComparer.Ordinal).Count() || !policy.Ordering.OrderBy(x => x).SequenceEqual(paragraphGroups.OrderBy(x => x), StringComparer.Ordinal))
                throw new LocationDataException($"Scene policy '{policy.Id}' paragraphing must cover each ordered group exactly once.");
            foreach (var cap in policy.MaxPerGroup)
                if (cap.Value < 1) throw new LocationDataException($"Scene policy '{policy.Id}' group cap for '{cap.Key}' must be positive.");
        }
    }

    public static void ValidateReferences(SceneDescriptionCatalog catalog, LocationDataBundle locations, CrossSystemAuthoringBundle authoring)
    {
        foreach (var fragment in catalog.Fragments.Values)
        {
            foreach (var archetypeId in fragment.AppliesTo.ArchetypeIds)
                if (!locations.Definitions.Archetypes.ContainsKey(archetypeId)) throw new LocationDataException($"Scene fragment '{fragment.Id}' references unknown archetype '{archetypeId}'.");
            foreach (var variantId in fragment.AppliesTo.VariantIds)
                if (!locations.Definitions.Variants.ContainsKey(variantId)) throw new LocationDataException($"Scene fragment '{fragment.Id}' references unknown variant '{variantId}'.");
            foreach (var modifierId in fragment.When.ObservableModifierIdsAny)
                if (!locations.Definitions.Modifiers.ContainsKey(modifierId)) throw new LocationDataException($"Scene fragment '{fragment.Id}' references unknown modifier '{modifierId}'.");
            ValidateStateValues(fragment, authoring);
        }
        foreach (var policy in catalog.Policies.Values)
            if (policy.ArchetypeId != null && !locations.Definitions.Archetypes.ContainsKey(policy.ArchetypeId))
                throw new LocationDataException($"Scene policy '{policy.Id}' references unknown archetype '{policy.ArchetypeId}'.");
    }

    private static void ValidateStateValues(SceneFragmentDefinition fragment, CrossSystemAuthoringBundle authoring)
    {
        if (fragment.SubjectKind != "location") return;
        var profiles = authoring.StateProfiles.Values
            .Where(profile => fragment.AppliesTo.ArchetypeIds.Count == 0 || fragment.AppliesTo.ArchetypeIds.Contains(profile.ArchetypeId, StringComparer.Ordinal))
            .ToList();
        ValidateChannel(fragment, profiles, LocationStateChannels.Interaction, fragment.When.KnownInteractionStatesAny);
        ValidateChannel(fragment, profiles, LocationStateChannels.Operational, fragment.When.KnownOperationalStatesAny);
        ValidateChannel(fragment, profiles, LocationStateChannels.Presence, fragment.When.KnownPresenceStatesAny);
    }

    private static void ValidateChannel(SceneFragmentDefinition fragment, IEnumerable<LocationStateProfileDefinition> profiles, string channelId, IEnumerable<string> values)
    {
        var known = profiles.Where(profile => profile.Channels.ContainsKey(channelId)).SelectMany(profile => profile.Channels[channelId].Values).ToHashSet(StringComparer.Ordinal);
        foreach (var value in values)
            if (!known.Contains(value)) throw new LocationDataException($"Scene fragment '{fragment.Id}' references unknown {channelId} state '{value}'.");
    }

    private static void ValidateSourcePairing(SceneFragmentDefinition fragment)
    {
        if (fragment.Group == "opening" && fragment.Source.Kind == "testimony") throw new LocationDataException($"Scene fragment '{fragment.Id}' uses testimony for an opening.");
        if (fragment.Group == "relation-identified" && fragment.Source.Kind == "direct-observation") throw new LocationDataException($"Scene fragment '{fragment.Id}' identifies a relation from direct observation alone.");
        if (fragment.When.IdentityStage == "identified")
        {
            if (fragment.When.ContactStatusAny.Count == 0) throw new LocationDataException($"Scene fragment '{fragment.Id}' identifies a group without contact status.");
            if (fragment.When.ContactStatusAny.Any(status => status is "Unknown" or "Rumored"))
                throw new LocationDataException($"Scene fragment '{fragment.Id}' identifies a group whose contact status is not established.");
            if (fragment.Source.Kind is not ("signature-identification" or "testimony" or "delivered-outcome"))
                throw new LocationDataException($"Scene fragment '{fragment.Id}' uses invalid source '{fragment.Source.Kind}' for identified relation.");
        }
    }

    private static void ValidateLocalizedText(SceneDescriptionCatalog catalog, string textId, string subjectKind)
    {
        var allowed = subjectKind switch
        {
            "scout-return" => new HashSet<string>(new[] { "{memberName}", "{companionName}", "{daysOverdue}" }, StringComparer.Ordinal),
            "report-event" => new HashSet<string>(new[] { "{memberName}", "{companionName}" }, StringComparer.Ordinal),
            _ => new HashSet<string>(StringComparer.Ordinal)
        };
        var defaultText = catalog.Texts.Resolve(textId);
        if (defaultText.Length > 240) throw new LocationDataException($"Scene text '{textId}' exceeds 240 characters.");
        foreach (Match match in PlaceholderPattern.Matches(defaultText))
            if (!allowed.Contains(match.Value)) throw new LocationDataException($"Scene text '{textId}' uses unsupported placeholder '{match.Value}'.");
        if (Regex.IsMatch(defaultText, "[0-9]")) throw new LocationDataException($"Scene text '{textId}' contains authored literal digits.");
        var expected = PlaceholderPattern.Matches(defaultText).Cast<Match>().Select(match => match.Value).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        foreach (var locale in catalog.Texts.Locales.Where(locale => locale.Texts.ContainsKey(textId)))
        {
            var localizedText = locale.Texts[textId];
            if (localizedText.Length > 240) throw new LocationDataException($"Scene text '{textId}' locale '{locale.Locale}' exceeds 240 characters.");
            if (Regex.IsMatch(localizedText, "[0-9]")) throw new LocationDataException($"Scene text '{textId}' locale '{locale.Locale}' contains authored literal digits.");
            var actual = PlaceholderPattern.Matches(localizedText).Cast<Match>().Select(match => match.Value).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (!expected.SequenceEqual(actual, StringComparer.Ordinal)) throw new LocationDataException($"Scene text '{textId}' locale '{locale.Locale}' changes its placeholder contract.");
        }
    }

    private static void ValidateSupersedesCycles(SceneDescriptionCatalog catalog)
    {
        foreach (var fragment in catalog.Fragments.Values)
            Visit(fragment.Id, new HashSet<string>(StringComparer.Ordinal), new HashSet<string>(StringComparer.Ordinal));

        void Visit(string id, HashSet<string> path, HashSet<string> complete)
        {
            if (complete.Contains(id)) return;
            if (!path.Add(id)) throw new LocationDataException($"Scene fragment supersedes cycle contains '{id}'.");
            foreach (var next in catalog.Fragments[id].SupersedesFragmentIds) Visit(next, path, complete);
            path.Remove(id);
            complete.Add(id);
        }
    }
}

}
