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

    public FactionContactSceneResolver(SceneDescriptionCatalog scenes, FactionSignatureDefinitionSet? signatures = null)
    {
        resolver = new SceneDescriptionResolver(scenes ?? throw new ArgumentNullException(nameof(scenes)));
        this.signatures = signatures;
    }

    public FactionContactPresentation Resolve(GameState game, FactionInteractionState interaction)
    {
        if (game == null) throw new ArgumentNullException(nameof(game));
        if (interaction == null) throw new ArgumentNullException(nameof(interaction));
        var faction = game.FindFaction(interaction.FactionId) ?? throw new InvalidOperationException("Active contact references an unknown faction.");
        var identityStage = IdentityStage(game.Knowledge, faction);
        var identified = identityStage == "identified";
        var tags = new List<string> { AttitudeTag(faction) };
        var scene = resolver.ResolveContact(new ContactSceneView(
            interaction.Id, identified ? faction.Name : "Unbekannte Abordnung", "Direkte Begegnung",
            "placeholder-contact", identityStage, faction.ContactStatus.ToString(), tags,
            identified ? faction.Name : null));
        return new FactionContactPresentation(identityStage, scene);
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
