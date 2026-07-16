#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;

namespace Game.App
{

/// <summary>Builds a contact scene exclusively from visible interaction state and earned knowledge.</summary>
public sealed class FactionContactSceneResolver
{
    private readonly SceneDescriptionResolver resolver;
    private readonly FactionSignatureDefinitionSet? signatures;
    private readonly CrossSystemAuthoringBundle? authoring;

    public FactionContactSceneResolver(SceneDescriptionCatalog scenes, FactionSignatureDefinitionSet? signatures = null,
        CrossSystemAuthoringBundle? authoring = null)
    {
        resolver = new SceneDescriptionResolver(scenes ?? throw new ArgumentNullException(nameof(scenes)));
        this.signatures = signatures;
        this.authoring = authoring;
    }

    public FactionContactPresentation Resolve(GameState game, FactionInteractionState interaction)
    {
        if (game == null) throw new ArgumentNullException(nameof(game));
        if (interaction == null) throw new ArgumentNullException(nameof(interaction));
        var faction = game.FindFaction(interaction.FactionId) ?? throw new InvalidOperationException("Active contact references an unknown faction.");
        var title = IdentityStage(game.Knowledge, faction) == "identified" ? faction.Name : "Unbekannte Abordnung";
        return ResolveView(game, faction, interaction.Id, title, "Direkte Begegnung", null);
    }

    /// <summary>Resolves only information already delivered by a faction-reaction event.</summary>
    public FactionContactPresentation? ResolveDeliveredEvent(GameState game, EventState eventState)
    {
        if (game == null) throw new ArgumentNullException(nameof(game));
        if (eventState == null) throw new ArgumentNullException(nameof(eventState));
        if (eventState.Kind != EventKind.FactionReaction) return null;
        var faction = eventState.FactionId == null ? null : game.FindFaction(eventState.FactionId);
        var identityStage = faction == null ? "anonymous" : IdentityStage(game.Knowledge, faction);
        var safeSource = faction == null || identityStage == "identified"
            ? eventState.Source
            : identityStage == "signature-recognised" ? "Nicht bestätigte Abordnung" : "Unbekannte Quelle";
        return ResolveView(game, faction, eventState.Id, eventState.Title, safeSource, eventState.Body);
    }

    private FactionContactPresentation ResolveView(GameState game, FactionState? faction, string subjectRef,
        string title, string? subtitle, string? deliveredText)
    {
        var identityStage = faction == null ? "anonymous" : IdentityStage(game.Knowledge, faction);
        var identified = faction != null && identityStage == "identified";
        var tags = new List<string>();
        if (faction != null)
        {
            tags.Add(AttitudeTag(faction));
            tags.AddRange(VisibleMemoryTags(faction));
        }
        var scene = resolver.ResolveContact(new ContactSceneView(subjectRef, title, subtitle,
            "placeholder-contact", identityStage, faction?.ContactStatus.ToString() ?? "Unknown", tags,
            identified ? faction!.Name : null, deliveredText: deliveredText));
        return new FactionContactPresentation(identityStage, scene);
    }

    private IEnumerable<string> VisibleMemoryTags(FactionState faction)
    {
        if (authoring == null) return Enumerable.Empty<string>();
        return faction.Memories
            .Select(memoryId => authoring.FactionMemories.TryGetValue(memoryId, out var definition) ? definition : null)
            .Where(definition => definition != null)
            .SelectMany(definition => definition!.ContactSceneTags)
            .Distinct(StringComparer.Ordinal);
    }

    private string IdentityStage(KnowledgeState knowledge, FactionState faction)
    {
        if (faction.ContactStatus is FactionContactStatus.Contacted or FactionContactStatus.Open or FactionContactStatus.Hostile)
            return "identified";
        var signatureId = signatures?.FindForProfile(faction.SignatureProfileId)?.Id;
        var recognised = signatureId != null && knowledge.Evidence.Any(item => item.SymbolId == signatureId &&
            item.KnowledgeState != EvidenceKnowledgeState.Contradicted);
        return recognised ? "signature-recognised" : "anonymous";
    }

    private static string AttitudeTag(FactionState faction)
    {
        if (faction.Anger >= 30 || faction.ContactStatus == FactionContactStatus.Hostile) return "attitude:tense";
        if (faction.Trust >= 10 || faction.ContactStatus == FactionContactStatus.Open) return "attitude:open";
        return "attitude:watchful";
    }
}

public sealed class FactionContactPresentation
{
    public FactionContactPresentation(string identityStage, SceneDescriptionResult scene)
    {
        IdentityStage = string.IsNullOrWhiteSpace(identityStage) ? throw new ArgumentException("Identity stage is required.", nameof(identityStage)) : identityStage;
        Scene = scene ?? throw new ArgumentNullException(nameof(scene));
    }

    public string IdentityStage { get; }
    public SceneDescriptionResult Scene { get; }
}
}
