#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;

namespace Game.App
{

public enum SceneTrigger
{
    Arrival,
    InspectionResult,
    RemoteLocationView,
    MissionReturn,
    EventDelivery,
    BaseReturn
}

public sealed class SceneParagraphResult
{
    public SceneParagraphResult(string text, IEnumerable<string> fragmentIds, IEnumerable<string> provenance)
    {
        Text = Require(text, nameof(text));
        FragmentIds = Normalize(fragmentIds);
        Provenance = Normalize(provenance);
    }

    public string Text { get; }
    public IReadOnlyList<string> FragmentIds { get; }
    public IReadOnlyList<string> Provenance { get; }

    private static string Require(string value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value must not be empty.", name) : value.Trim();
    private static IReadOnlyList<string> Normalize(IEnumerable<string> values) =>
        values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).Distinct(StringComparer.Ordinal).ToList();
}

/// <summary>Shared presentation projection. Clients render it and do not append inferred information.</summary>
public sealed class SceneDescriptionResult
{
    public SceneDescriptionResult(string title, string? subtitle, string? visualId,
        IEnumerable<SceneParagraphResult> paragraphs, string? question, string? knowledgeAgeLabel = null)
    {
        Title = string.IsNullOrWhiteSpace(title) ? throw new ArgumentException("Scene title must not be empty.", nameof(title)) : title.Trim();
        Subtitle = Normalize(subtitle);
        VisualId = Normalize(visualId);
        Paragraphs = (paragraphs ?? throw new ArgumentNullException(nameof(paragraphs))).ToList();
        if (Paragraphs.Count == 0) throw new ArgumentException("A scene needs at least one paragraph.", nameof(paragraphs));
        Question = Normalize(question);
        KnowledgeAgeLabel = Normalize(knowledgeAgeLabel);
    }

    public string Title { get; }
    public string? Subtitle { get; }
    public string? VisualId { get; }
    public IReadOnlyList<SceneParagraphResult> Paragraphs { get; }
    public string? Question { get; }
    public string? KnowledgeAgeLabel { get; }
    public string Message => string.Join(Environment.NewLine + Environment.NewLine,
        Paragraphs.Select(paragraph => paragraph.Text).Concat(Question == null ? Array.Empty<string>() : new[] { Question }));

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class ObservableModifierScenePart
{
    public ObservableModifierScenePart(string modifierId, string text)
    {
        ModifierId = string.IsNullOrWhiteSpace(modifierId) ? throw new ArgumentException("Modifier id must not be empty.", nameof(modifierId)) : modifierId.Trim();
        Text = string.IsNullOrWhiteSpace(text) ? throw new ArgumentException("Modifier text must not be empty.", nameof(text)) : text.Trim();
    }

    public string ModifierId { get; }
    public string Text { get; }
}

/// <summary>Read-only contact projection containing only visible or already-earned information.</summary>
public sealed class ContactSceneView
{
    public ContactSceneView(string subjectRef, string title, string? subtitle, string? visualId,
        string identityStage, string contactStatus, IEnumerable<string>? knownContextTags,
        string? subjectLabel = null, string? locale = null, string? deliveredText = null)
    {
        SubjectRef = Require(subjectRef, nameof(subjectRef));
        Title = Require(title, nameof(title));
        Subtitle = Normalize(subtitle);
        VisualId = Normalize(visualId);
        IdentityStage = Require(identityStage, nameof(identityStage));
        ContactStatus = Require(contactStatus, nameof(contactStatus));
        KnownContextTags = (knownContextTags ?? Enumerable.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim()).Distinct(StringComparer.Ordinal).ToList();
        SubjectLabel = Normalize(subjectLabel);
        Locale = Normalize(locale);
        DeliveredText = Normalize(deliveredText);
    }

    public string SubjectRef { get; }
    public string Title { get; }
    public string? Subtitle { get; }
    public string? VisualId { get; }
    public string IdentityStage { get; }
    public string ContactStatus { get; }
    public IReadOnlyList<string> KnownContextTags { get; }
    public string? SubjectLabel { get; }
    public string? Locale { get; }
    /// <summary>Player-facing text already delivered by an event; never objective event causality.</summary>
    public string? DeliveredText { get; }

    private static string Require(string value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value must not be empty.", name) : value.Trim();
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>Read-only projection of one immutable delivered scout-mission outcome.</summary>
public sealed class ScoutReturnSceneView
{
    public ScoutReturnSceneView(string subjectRef, string title, string? subtitle, string? visualId,
        string missionStatus, IEnumerable<string> memberStatuses, string teamOutcome,
        int? reportReliability, bool hasFindings, bool hasLeads, bool wasOverdue,
        bool hasLostEquipment, string memberName, string? companionName, string daysOverdue,
        string? locale = null)
    {
        SubjectRef = Require(subjectRef, nameof(subjectRef));
        Title = Require(title, nameof(title));
        Subtitle = Normalize(subtitle);
        VisualId = Normalize(visualId);
        MissionStatus = Require(missionStatus, nameof(missionStatus));
        MemberStatuses = (memberStatuses ?? throw new ArgumentNullException(nameof(memberStatuses)))
            .Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).Distinct(StringComparer.Ordinal).ToList();
        TeamOutcome = Require(teamOutcome, nameof(teamOutcome));
        ReportReliability = reportReliability;
        HasFindings = hasFindings;
        HasLeads = hasLeads;
        WasOverdue = wasOverdue;
        HasLostEquipment = hasLostEquipment;
        MemberName = Require(memberName, nameof(memberName));
        CompanionName = Normalize(companionName);
        DaysOverdue = Require(daysOverdue, nameof(daysOverdue));
        Locale = Normalize(locale);
    }

    public string SubjectRef { get; }
    public string Title { get; }
    public string? Subtitle { get; }
    public string? VisualId { get; }
    public string MissionStatus { get; }
    public IReadOnlyList<string> MemberStatuses { get; }
    public string TeamOutcome { get; }
    public int? ReportReliability { get; }
    public bool HasFindings { get; }
    public bool HasLeads { get; }
    public bool WasOverdue { get; }
    public bool HasLostEquipment { get; }
    public string MemberName { get; }
    public string? CompanionName { get; }
    public string DaysOverdue { get; }
    public string? Locale { get; }

    private static string Require(string value, string name) => string.IsNullOrWhiteSpace(value)
        ? throw new ArgumentException("Value must not be empty.", name) : value.Trim();
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>Read-only projection of an event already delivered to the expedition.</summary>
public sealed class ReportEventSceneView
{
    public ReportEventSceneView(string subjectRef, string title, string? subtitle, string? visualId,
        string deliveredText, IEnumerable<string>? knownContextTags, string? locale = null)
    {
        SubjectRef = Require(subjectRef, nameof(subjectRef));
        Title = Require(title, nameof(title));
        Subtitle = Normalize(subtitle);
        VisualId = Normalize(visualId);
        DeliveredText = Require(deliveredText, nameof(deliveredText));
        KnownContextTags = (knownContextTags ?? Enumerable.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim()).Distinct(StringComparer.Ordinal).ToList();
        Locale = Normalize(locale);
    }

    public string SubjectRef { get; }
    public string Title { get; }
    public string? Subtitle { get; }
    public string? VisualId { get; }
    public string DeliveredText { get; }
    public IReadOnlyList<string> KnownContextTags { get; }
    public string? Locale { get; }

    private static string Require(string value, string name) => string.IsNullOrWhiteSpace(value)
        ? throw new ArgumentException("Value must not be empty.", name) : value.Trim();
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>Immutable presentation facts emitted by one completed expedition.</summary>
public sealed class BaseReturnSceneView
{
    public BaseReturnSceneView(string subjectRef, string title, string? subtitle, string? visualId,
        string teamOutcome, bool hasFindings, IEnumerable<string>? knownContextTags, string? locale = null)
    {
        SubjectRef = Require(subjectRef, nameof(subjectRef));
        Title = Require(title, nameof(title));
        Subtitle = Normalize(subtitle);
        VisualId = Normalize(visualId);
        TeamOutcome = Require(teamOutcome, nameof(teamOutcome));
        HasFindings = hasFindings;
        KnownContextTags = (knownContextTags ?? Enumerable.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim()).Distinct(StringComparer.Ordinal).ToList();
        Locale = Normalize(locale);
    }

    public string SubjectRef { get; }
    public string Title { get; }
    public string? Subtitle { get; }
    public string? VisualId { get; }
    public string TeamOutcome { get; }
    public bool HasFindings { get; }
    public IReadOnlyList<string> KnownContextTags { get; }
    public string? Locale { get; }

    private static string Require(string value, string name) => string.IsNullOrWhiteSpace(value)
        ? throw new ArgumentException("Value must not be empty.", name) : value.Trim();
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>
/// Read-only, already-authorized location observation. It contains no hidden context, modifier or
/// faction claim that the inspection did not make visible.
/// </summary>
public sealed class LocationSceneView
{
    public LocationSceneView(string subjectRef, SceneTrigger trigger, string archetypeId, string variantId,
        string title, string? subtitle, string? visualId, string interactionStateId, string operationalStateId,
        string presenceStateId, int currentWorldDay, LocationConditionKnowledgeState? previousKnownCondition,
        KnowledgeLevel knowledgeLevel, IEnumerable<string>? knownContextTags,
        IEnumerable<ObservableModifierScenePart>? observableModifiers, IEnumerable<string>? observableRelationKinds,
        string identityStage = "anonymous", string contactStatus = "Unknown", string? subjectLabel = null,
        string? knowledgeAgeLabel = null, string? locale = null)
    {
        SubjectRef = Require(subjectRef, nameof(subjectRef));
        Trigger = trigger;
        ArchetypeId = Require(archetypeId, nameof(archetypeId));
        VariantId = Require(variantId, nameof(variantId));
        Title = Require(title, nameof(title));
        Subtitle = Normalize(subtitle);
        VisualId = Normalize(visualId);
        InteractionStateId = Require(interactionStateId, nameof(interactionStateId));
        OperationalStateId = Require(operationalStateId, nameof(operationalStateId));
        PresenceStateId = Require(presenceStateId, nameof(presenceStateId));
        CurrentWorldDay = currentWorldDay;
        PreviousKnownCondition = previousKnownCondition;
        KnowledgeLevel = knowledgeLevel;
        KnownContextTags = NormalizeList(knownContextTags);
        ObservableModifiers = (observableModifiers ?? Enumerable.Empty<ObservableModifierScenePart>()).ToList();
        ObservableRelationKinds = NormalizeList(observableRelationKinds);
        IdentityStage = Require(identityStage, nameof(identityStage));
        ContactStatus = Require(contactStatus, nameof(contactStatus));
        SubjectLabel = Normalize(subjectLabel);
        KnowledgeAgeLabel = Normalize(knowledgeAgeLabel);
        Locale = Normalize(locale);
    }

    public string SubjectRef { get; }
    public SceneTrigger Trigger { get; }
    public string ArchetypeId { get; }
    public string VariantId { get; }
    public string Title { get; }
    public string? Subtitle { get; }
    public string? VisualId { get; }
    public string InteractionStateId { get; }
    public string OperationalStateId { get; }
    public string PresenceStateId { get; }
    public int CurrentWorldDay { get; }
    public LocationConditionKnowledgeState? PreviousKnownCondition { get; }
    public KnowledgeLevel KnowledgeLevel { get; }
    public IReadOnlyList<string> KnownContextTags { get; }
    public IReadOnlyList<ObservableModifierScenePart> ObservableModifiers { get; }
    public IReadOnlyList<string> ObservableRelationKinds { get; }
    public string IdentityStage { get; }
    public string ContactStatus { get; }
    public string? SubjectLabel { get; }
    public string? KnowledgeAgeLabel { get; }
    public string? Locale { get; }
    public bool HasCurrentObservation => Trigger is SceneTrigger.Arrival or SceneTrigger.InspectionResult;
    public bool CurrentObservationDiffersFromStored => PreviousKnownCondition != null &&
        (PreviousKnownCondition.InteractionStateId != InteractionStateId ||
         PreviousKnownCondition.OperationalStateId != OperationalStateId ||
         PreviousKnownCondition.PresenceStateId != PresenceStateId);

    private static string Require(string value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value must not be empty.", name) : value.Trim();
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static IReadOnlyList<string> NormalizeList(IEnumerable<string>? values) =>
        (values ?? Enumerable.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).Distinct(StringComparer.Ordinal).ToList();
}

/// <summary>Pure deterministic fragment selection and assembly for authorized location views.</summary>
public sealed class SceneDescriptionResolver
{
    private readonly SceneDescriptionCatalog catalog;

    public SceneDescriptionResolver(SceneDescriptionCatalog catalog)
    {
        this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public SceneDescriptionResult ResolveLocation(LocationSceneView view)
    {
        if (view == null) throw new ArgumentNullException(nameof(view));
        var policy = catalog.Policies.Values.SingleOrDefault(item => item.SubjectKind == "location" && item.ArchetypeId == view.ArchetypeId)
            ?? throw new LocationDataException($"No location scene policy exists for archetype '{view.ArchetypeId}'.");

        var eligible = catalog.Fragments.Values
            .Where(fragment => fragment.SubjectKind == "location")
            .Where(fragment => Applies(fragment, view))
            .Where(fragment => SourceAllowed(fragment.Source, view))
            .Where(fragment => Matches(fragment.When, view))
            .Select(fragment => new ResolvedFragment(fragment.Id, fragment.Group,
                Interpolate(catalog.Texts.Resolve(fragment.TextId, view.Locale), view), fragment.Source.Kind, fragment.Priority,
                fragment.SupersedesFragmentIds, fragment.ExclusiveTag))
            .ToList();

        eligible.AddRange(view.ObservableModifiers.Select(modifier => new ResolvedFragment(
            "modifier:" + modifier.ModifierId, "modifier", modifier.Text, "observable-modifier", 50,
            Array.Empty<string>(), null)));
        var selected = Select(policy, eligible, view.SubjectRef);
        var paragraphs = AssembleParagraphs(policy, selected);
        if (paragraphs.Count == 0) throw new LocationDataException($"Scene for '{view.SubjectRef}' contains no eligible fragment.");
        var questionResolved = !(view.Trigger == SceneTrigger.RemoteLocationView && view.PreviousKnownCondition?.IsDoubtful == true)
            && Matches(policy.QuestionResolvedWhen, view);
        var question = questionResolved ? null : catalog.Texts.Resolve(policy.QuestionTextId, view.Locale);
        return new SceneDescriptionResult(view.Title, view.Subtitle, view.VisualId, paragraphs, question, view.KnowledgeAgeLabel);
    }

    public SceneDescriptionResult ResolveContact(ContactSceneView view)
    {
        if (view == null) throw new ArgumentNullException(nameof(view));
        var policy = catalog.Policies.Values.SingleOrDefault(item => item.SubjectKind == "contact" && item.ArchetypeId == null)
            ?? throw new LocationDataException("No generic contact scene policy exists.");
        var eligible = catalog.Fragments.Values.Where(fragment => fragment.SubjectKind == "contact")
            .Where(fragment => ContactSourceAllowed(fragment.Source, view))
            .Where(fragment => Matches(fragment.When, view))
            .Select(fragment => new ResolvedFragment(fragment.Id, fragment.Group,
                Interpolate(catalog.Texts.Resolve(fragment.TextId, view.Locale), view), fragment.Source.Kind, fragment.Priority,
                fragment.SupersedesFragmentIds, fragment.ExclusiveTag)).ToList();
        if (view.DeliveredText != null)
        {
            eligible.Add(new ResolvedFragment("delivered:" + view.SubjectRef, "opening", view.DeliveredText,
                "delivered-outcome", 90, new[] { "frag-contact-opening" }, null));
        }
        var selected = Select(policy, eligible, view.SubjectRef);
        var paragraphs = AssembleParagraphs(policy, selected);
        if (paragraphs.Count == 0) throw new LocationDataException($"Contact scene for '{view.SubjectRef}' contains no eligible fragment.");
        var question = Matches(policy.QuestionResolvedWhen, view) ? null : catalog.Texts.Resolve(policy.QuestionTextId, view.Locale);
        return new SceneDescriptionResult(view.Title, view.Subtitle, view.VisualId, paragraphs, question);
    }

    public SceneDescriptionResult ResolveScoutReturn(ScoutReturnSceneView view)
    {
        if (view == null) throw new ArgumentNullException(nameof(view));
        var policy = catalog.Policies.Values.SingleOrDefault(item => item.SubjectKind == "scout-return" && item.ArchetypeId == null)
            ?? throw new LocationDataException("No generic scout-return scene policy exists.");
        var eligible = catalog.Fragments.Values.Where(fragment => fragment.SubjectKind == "scout-return")
            .Where(fragment => fragment.Source.Kind is "delivered-outcome" or "direct-observation")
            .Where(fragment => Matches(fragment.When, view))
            .Select(fragment => new ResolvedFragment(fragment.Id, fragment.Group,
                Interpolate(catalog.Texts.Resolve(fragment.TextId, view.Locale), view), fragment.Source.Kind, fragment.Priority,
                fragment.SupersedesFragmentIds, fragment.ExclusiveTag)).ToList();
        var selected = Select(policy, eligible, view.SubjectRef);
        var paragraphs = AssembleParagraphs(policy, selected);
        if (paragraphs.Count == 0) throw new LocationDataException($"Scout-return scene for '{view.SubjectRef}' contains no eligible fragment.");
        var question = Matches(policy.QuestionResolvedWhen, view) ? null : catalog.Texts.Resolve(policy.QuestionTextId, view.Locale);
        return new SceneDescriptionResult(view.Title, view.Subtitle, view.VisualId, paragraphs, question);
    }

    public SceneDescriptionResult ResolveReportEvent(ReportEventSceneView view)
    {
        if (view == null) throw new ArgumentNullException(nameof(view));
        var policy = catalog.Policies.Values.SingleOrDefault(item => item.SubjectKind == "report-event" && item.ArchetypeId == null)
            ?? throw new LocationDataException("No generic report-event scene policy exists.");
        var eligible = catalog.Fragments.Values.Where(fragment => fragment.SubjectKind == "report-event")
            .Where(fragment => fragment.Source.Kind is "delivered-outcome" or "direct-observation" or "testimony")
            .Where(fragment => Matches(fragment.When, view))
            .Select(fragment => new ResolvedFragment(fragment.Id, fragment.Group,
                catalog.Texts.Resolve(fragment.TextId, view.Locale), fragment.Source.Kind, fragment.Priority,
                fragment.SupersedesFragmentIds, fragment.ExclusiveTag)).ToList();
        eligible.Add(new ResolvedFragment("delivered:" + view.SubjectRef, "opening", view.DeliveredText,
            "delivered-outcome", 90, Array.Empty<string>(), null));
        var selected = Select(policy, eligible, view.SubjectRef);
        var paragraphs = AssembleParagraphs(policy, selected);
        if (paragraphs.Count == 0) throw new LocationDataException($"Report-event scene for '{view.SubjectRef}' contains no eligible fragment.");
        var question = Matches(policy.QuestionResolvedWhen, view) ? null : catalog.Texts.Resolve(policy.QuestionTextId, view.Locale);
        return new SceneDescriptionResult(view.Title, view.Subtitle, view.VisualId, paragraphs, question);
    }

    public SceneDescriptionResult ResolveBaseReturn(BaseReturnSceneView view)
    {
        if (view == null) throw new ArgumentNullException(nameof(view));
        var policy = catalog.Policies.Values.SingleOrDefault(item => item.SubjectKind == "base-return" && item.ArchetypeId == null)
            ?? throw new LocationDataException("No generic base-return scene policy exists.");
        var eligible = catalog.Fragments.Values.Where(fragment => fragment.SubjectKind == "base-return")
            .Where(fragment => fragment.Source.Kind == "delivered-outcome")
            .Where(fragment => Matches(fragment.When, view))
            .Select(fragment => new ResolvedFragment(fragment.Id, fragment.Group,
                catalog.Texts.Resolve(fragment.TextId, view.Locale), fragment.Source.Kind, fragment.Priority,
                fragment.SupersedesFragmentIds, fragment.ExclusiveTag)).ToList();
        var selected = Select(policy, eligible, view.SubjectRef);
        var paragraphs = AssembleParagraphs(policy, selected);
        if (paragraphs.Count == 0) throw new LocationDataException($"Base-return scene for '{view.SubjectRef}' contains no eligible fragment.");
        var question = Matches(policy.QuestionResolvedWhen, view) ? null : catalog.Texts.Resolve(policy.QuestionTextId, view.Locale);
        return new SceneDescriptionResult(view.Title, view.Subtitle, view.VisualId, paragraphs, question);
    }

    private static bool Applies(SceneFragmentDefinition fragment, LocationSceneView view) =>
        (fragment.AppliesTo.ArchetypeIds.Count == 0 || fragment.AppliesTo.ArchetypeIds.Contains(view.ArchetypeId, StringComparer.Ordinal)) &&
        (fragment.AppliesTo.VariantIds.Count == 0 || fragment.AppliesTo.VariantIds.Contains(view.VariantId, StringComparer.Ordinal));

    private static bool SourceAllowed(SceneFragmentSourceDefinition source, LocationSceneView view)
    {
        if (!view.HasCurrentObservation && source.Kind == "stored-observation")
        {
            var known = view.PreviousKnownCondition;
            if (known == null || known.IsDoubtful && !source.ToleratesDoubt) return false;
            if (source.MaxKnownStateAgeDays != null && view.CurrentWorldDay - known.ObservedWorldDay > source.MaxKnownStateAgeDays) return false;
        }
        return source.Kind switch
        {
            "direct-observation" => view.HasCurrentObservation,
            "stored-observation" => view.HasCurrentObservation || view.PreviousKnownCondition != null,
            "signature-identification" => view.IdentityStage is "signature-recognised" or "identified",
            "observable-modifier" => false,
            "visible-trace" => false,
            "testimony" => false,
            "delivered-outcome" => false,
            _ => false
        };
    }

    private static bool Matches(SceneFragmentConditionDefinition when, LocationSceneView view)
    {
        if (!MatchesAny(when.KnownInteractionStatesAny, view.InteractionStateId)) return false;
        if (!MatchesAny(when.KnownOperationalStatesAny, view.OperationalStateId)) return false;
        if (!MatchesAny(when.KnownPresenceStatesAny, view.PresenceStateId)) return false;
        if (!MatchesAnyOverlap(when.KnownContextTagsAny, view.KnownContextTags)) return false;
        if (!MatchesAnyOverlap(when.ObservableModifierIdsAny, view.ObservableModifiers.Select(item => item.ModifierId))) return false;
        if (!MatchesAnyOverlap(when.ObservableRelationKindsAny, view.ObservableRelationKinds)) return false;
        if (!MatchesAny(when.ContactStatusAny, view.ContactStatus)) return false;
        if (when.IdentityStage != null && when.IdentityStage != view.IdentityStage) return false;
        if (when.KnowledgeLevelAtLeast != null && KnowledgeRank(view.KnowledgeLevel) < KnowledgeRank(when.KnowledgeLevelAtLeast)) return false;
        if (when.RequiresDoubtfulLastObservation == true && view.PreviousKnownCondition?.IsDoubtful != true) return false;
        if (when.ForbidDoubtfulLastObservation == true && view.PreviousKnownCondition?.IsDoubtful == true) return false;
        if (when.MaxKnownStateAgeDays != null && !view.HasCurrentObservation &&
            (view.PreviousKnownCondition == null || view.CurrentWorldDay - view.PreviousKnownCondition.ObservedWorldDay > when.MaxKnownStateAgeDays)) return false;
        if (when.CurrentObservationDiffersFromStored != null && when.CurrentObservationDiffersFromStored != view.CurrentObservationDiffersFromStored) return false;
        if (when.HasFindings != null || when.HasLeads != null || when.WasOverdue != null || when.HasLostEquipment != null || when.HasCompanion != null ||
            when.IsSecondHandAccount != null || when.IsUrgent != null || when.HasOwnArchiveEntryForLocation != null ||
            when.HasLostExpeditionRecordForLocation != null || when.MissionStatusAny.Count > 0 || when.MemberStatusAny.Count > 0 || when.TeamOutcome != null ||
            when.ReportReliabilityAtLeast != null || when.ReportReliabilityBelow != null) return false;
        return true;
    }

    private static bool MatchesAny(IReadOnlyList<string> expected, string actual) => expected.Count == 0 || expected.Contains(actual, StringComparer.Ordinal);

    private static string Interpolate(string text, LocationSceneView view) =>
        text.Replace("{subjectLabel}", view.SubjectLabel ?? string.Empty, StringComparison.Ordinal);

    private static string Interpolate(string text, ContactSceneView view) =>
        text.Replace("{subjectLabel}", view.SubjectLabel ?? string.Empty, StringComparison.Ordinal);

    private static string Interpolate(string text, ScoutReturnSceneView view) => text
        .Replace("{memberName}", view.MemberName, StringComparison.Ordinal)
        .Replace("{companionName}", view.CompanionName ?? string.Empty, StringComparison.Ordinal)
        .Replace("{daysOverdue}", view.DaysOverdue, StringComparison.Ordinal);

    private static bool ContactSourceAllowed(SceneFragmentSourceDefinition source, ContactSceneView view) => source.Kind switch
    {
        "direct-observation" => true,
        "signature-identification" => view.IdentityStage is "signature-recognised" or "identified",
        "testimony" => true,
        "delivered-outcome" => true,
        _ => false
    };

    private static bool Matches(SceneFragmentConditionDefinition when, ContactSceneView view)
    {
        if (!MatchesAnyOverlap(when.KnownContextTagsAny, view.KnownContextTags)) return false;
        if (!MatchesAny(when.ContactStatusAny, view.ContactStatus)) return false;
        if (when.IdentityStage != null && when.IdentityStage != view.IdentityStage) return false;
        return when.KnownInteractionStatesAny.Count == 0 && when.KnownOperationalStatesAny.Count == 0 &&
            when.KnownPresenceStatesAny.Count == 0 && when.ObservableModifierIdsAny.Count == 0 &&
            when.ObservableRelationKindsAny.Count == 0 && when.KnowledgeLevelAtLeast == null &&
            when.MissionStatusAny.Count == 0 && when.MemberStatusAny.Count == 0 && when.TeamOutcome == null &&
            when.ReportReliabilityAtLeast == null && when.ReportReliabilityBelow == null && when.HasFindings == null &&
            when.HasLeads == null && when.WasOverdue == null && when.HasLostEquipment == null && when.HasCompanion == null &&
            when.IsSecondHandAccount == null && when.IsUrgent == null;
    }

    private static bool Matches(SceneFragmentConditionDefinition when, ScoutReturnSceneView view)
    {
        if (!MatchesAny(when.MissionStatusAny, view.MissionStatus)) return false;
        if (!MatchesAnyOverlap(when.MemberStatusAny, view.MemberStatuses)) return false;
        if (when.TeamOutcome != null && when.TeamOutcome != view.TeamOutcome) return false;
        if (when.ReportReliabilityAtLeast != null && (view.ReportReliability == null || view.ReportReliability < when.ReportReliabilityAtLeast)) return false;
        if (when.ReportReliabilityBelow != null && (view.ReportReliability == null || view.ReportReliability >= when.ReportReliabilityBelow)) return false;
        if (when.HasFindings != null && when.HasFindings != view.HasFindings) return false;
        if (when.HasLeads != null && when.HasLeads != view.HasLeads) return false;
        if (when.WasOverdue != null && when.WasOverdue != view.WasOverdue) return false;
        if (when.HasLostEquipment != null && when.HasLostEquipment != view.HasLostEquipment) return false;
        if (when.HasCompanion != null && when.HasCompanion != (view.CompanionName != null)) return false;
        return when.KnownInteractionStatesAny.Count == 0 && when.KnownOperationalStatesAny.Count == 0 &&
            when.KnownPresenceStatesAny.Count == 0 && when.KnownContextTagsAny.Count == 0 &&
            when.ObservableModifierIdsAny.Count == 0 && when.ObservableRelationKindsAny.Count == 0 &&
            when.KnowledgeLevelAtLeast == null && when.ContactStatusAny.Count == 0 && when.IdentityStage == null &&
            when.IsSecondHandAccount == null && when.IsUrgent == null &&
            when.HasOwnArchiveEntryForLocation == null && when.CurrentObservationDiffersFromStored == null &&
            when.HasLostExpeditionRecordForLocation == null;
    }

    private static bool Matches(SceneFragmentConditionDefinition when, ReportEventSceneView view)
    {
        if (!MatchesAnyOverlap(when.KnownContextTagsAny, view.KnownContextTags)) return false;
        return when.KnownInteractionStatesAny.Count == 0 && when.KnownOperationalStatesAny.Count == 0 &&
            when.KnownPresenceStatesAny.Count == 0 && when.ObservableModifierIdsAny.Count == 0 &&
            when.ObservableRelationKindsAny.Count == 0 && when.KnowledgeLevelAtLeast == null &&
            when.ContactStatusAny.Count == 0 && when.IdentityStage == null && when.MissionStatusAny.Count == 0 &&
            when.MemberStatusAny.Count == 0 && when.TeamOutcome == null && when.ReportReliabilityAtLeast == null &&
            when.ReportReliabilityBelow == null && when.HasFindings == null && when.HasLeads == null &&
            when.WasOverdue == null && when.HasLostEquipment == null && when.HasCompanion == null &&
            when.IsSecondHandAccount == null && when.IsUrgent == null && when.HasOwnArchiveEntryForLocation == null &&
            when.CurrentObservationDiffersFromStored == null && when.HasLostExpeditionRecordForLocation == null;
    }

    private static bool Matches(SceneFragmentConditionDefinition when, BaseReturnSceneView view)
    {
        if (!MatchesAnyOverlap(when.KnownContextTagsAny, view.KnownContextTags)) return false;
        if (when.TeamOutcome != null && when.TeamOutcome != view.TeamOutcome) return false;
        if (when.HasFindings != null && when.HasFindings != view.HasFindings) return false;
        return when.KnownInteractionStatesAny.Count == 0 && when.KnownOperationalStatesAny.Count == 0 &&
            when.KnownPresenceStatesAny.Count == 0 && when.ObservableModifierIdsAny.Count == 0 &&
            when.ObservableRelationKindsAny.Count == 0 && when.KnowledgeLevelAtLeast == null &&
            when.ContactStatusAny.Count == 0 && when.IdentityStage == null && when.MissionStatusAny.Count == 0 &&
            when.MemberStatusAny.Count == 0 && when.ReportReliabilityAtLeast == null && when.ReportReliabilityBelow == null &&
            when.HasLeads == null && when.WasOverdue == null && when.HasLostEquipment == null && when.HasCompanion == null &&
            when.IsSecondHandAccount == null && when.IsUrgent == null && when.HasOwnArchiveEntryForLocation == null &&
            when.CurrentObservationDiffersFromStored == null && when.HasLostExpeditionRecordForLocation == null;
    }
    private static bool MatchesAnyOverlap(IEnumerable<string> expected, IEnumerable<string> actual)
    {
        var required = expected.ToList();
        return required.Count == 0 || required.Intersect(actual, StringComparer.Ordinal).Any();
    }

    private static int KnowledgeRank(KnowledgeLevel level) => level switch
    {
        KnowledgeLevel.Unknown => 0,
        KnowledgeLevel.OldOrDoubtful => 1,
        KnowledgeLevel.Reported => 2,
        KnowledgeLevel.Confirmed => 3,
        _ => 0
    };
    private static int KnowledgeRank(string level) => level switch
    {
        "OldOrDoubtful" => 1,
        "Reported" => 2,
        "Confirmed" => 3,
        _ => 0
    };

    private static IReadOnlyList<ResolvedFragment> Select(ScenePolicyDefinition policy, IEnumerable<ResolvedFragment> candidates, string subjectRef)
    {
        var allowedGroups = policy.Ordering.ToHashSet(StringComparer.Ordinal);
        var eligible = candidates.Where(item => allowedGroups.Contains(item.Group)).ToList();
        var superseded = eligible.SelectMany(item => item.SupersedesFragmentIds).ToHashSet(StringComparer.Ordinal);
        eligible.RemoveAll(item => superseded.Contains(item.Id));
        var exclusiveWinners = eligible.Where(item => item.ExclusiveTag != null)
            .GroupBy(item => item.ExclusiveTag!, StringComparer.Ordinal)
            .Select(group => group.OrderByDescending(item => item.Priority).ThenBy(item => StableRank(subjectRef, item.Id)).First())
            .ToHashSet();
        eligible.RemoveAll(item => item.ExclusiveTag != null && !exclusiveWinners.Contains(item));

        var selected = eligible
            .GroupBy(item => item.Group, StringComparer.Ordinal)
            .SelectMany(group => group.OrderByDescending(item => item.Priority).ThenBy(item => StableRank(subjectRef, item.Id))
                .Take(policy.MaxPerGroup.TryGetValue(group.Key, out var cap) ? cap : int.MaxValue))
            .ToList();

        while (selected.Count > policy.MaxFragments)
        {
            var openingCount = selected.Count(item => item.Group == "opening");
            var remove = selected.Where(item => item.Group != "opening" || openingCount > 1)
                .OrderBy(item => item.Priority).ThenByDescending(item => StableRank(subjectRef, item.Id)).FirstOrDefault();
            if (remove == null) break;
            selected.Remove(remove);
        }
        return selected;
    }

    private static List<SceneParagraphResult> AssembleParagraphs(ScenePolicyDefinition policy, IReadOnlyList<ResolvedFragment> selected)
    {
        var result = new List<SceneParagraphResult>();
        foreach (var paragraphGroups in policy.Paragraphing)
        {
            var groupOrder = paragraphGroups.Select((group, index) => (group, index)).ToDictionary(item => item.group, item => item.index, StringComparer.Ordinal);
            var parts = selected.Where(item => groupOrder.ContainsKey(item.Group))
                .OrderBy(item => groupOrder[item.Group]).ThenByDescending(item => item.Priority).ThenBy(item => item.Id, StringComparer.Ordinal).ToList();
            if (parts.Count == 0) continue;
            result.Add(new SceneParagraphResult(string.Join(" ", parts.Select(item => item.Text)), parts.Select(item => item.Id), parts.Select(item => item.Provenance)));
        }
        return result;
    }

    private static ulong StableRank(string subjectRef, string fragmentId)
    {
        var hash = 14695981039346656037UL;
        foreach (var character in subjectRef + "|" + fragmentId)
        {
            hash ^= character;
            hash *= 1099511628211UL;
        }
        return hash;
    }

    private sealed record ResolvedFragment(string Id, string Group, string Text, string Provenance, int Priority,
        IReadOnlyList<string> SupersedesFragmentIds, string? ExclusiveTag);
}

}
