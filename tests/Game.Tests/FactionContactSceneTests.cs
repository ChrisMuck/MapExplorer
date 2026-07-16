#nullable enable
using System;
using System.IO;
using System.Linq;
using Game.App;
using Game.Core;

internal sealed class FactionContactSceneTests
{
    public void RunAll()
    {
        UnknownContactRemainsAnonymous();
        RumoredSignatureCanBeRecognisedWithoutNamingFaction();
        EstablishedContactMayNameFaction();
        AuthoredMemoryChangesVisibleContactHistory();
        DeliveredFactionEventUsesTheSharedContactScene();
        AnonymousDeliveredEventDoesNotRevealFactionIdentity();
    }

    private static void UnknownContactRemainsAnonymous()
    {
        var (catalog, command, game, faction) = Create(FactionContactStatus.Unknown);
        var result = command.Execute(game, faction.Id, game.Expedition.Position);
        AssertTrue(result.Success && result.Presentation != null, "Unknown contact resolves a structured scene");
        AssertEqual("anonymous", result.Presentation!.IdentityStage, "Unknown faction has anonymous identity stage");
        AssertFalse(result.Presentation.Scene.Message.Contains(faction.Name, StringComparison.Ordinal), "Anonymous scene does not expose the internal faction name");
        AssertTrue(FragmentIds(result).Contains("frag-contact-anonymous"), "Anonymous contact fragment is selected");
        AssertEqual(FactionRepresentativeRole.Guard, result.Interaction!.Representative.Role,
            "Neutral-cautious contact style selects the guarded JSON representative profile without using faction id");
        AssertTrue(result.Interaction.DialogueText.Contains("weshalb ihr hier seid", StringComparison.Ordinal),
            "Representative dialogue is resolved from localized contact-profile text");
    }

    private static void RumoredSignatureCanBeRecognisedWithoutNamingFaction()
    {
        var (catalog, command, game, faction) = Create(FactionContactStatus.Rumored);
        var signature = catalog.CrossSystem.FactionSignatures.FindForProfile(faction.SignatureProfileId)!;
        game.Knowledge.AddEvidence(new EvidenceState("evidence-signature", "evidence-directional-sign-trace",
            EvidenceSourceKind.ScoutReport, EvidenceKnowledgeState.Reported, "Wiederkehrende Zeichen wurden gemeldet.", symbolId: signature.Id));

        var result = command.Execute(game, faction.Id, game.Expedition.Position);
        AssertEqual("signature-recognised", result.Presentation!.IdentityStage, "Matching earned evidence permits signature recognition");
        AssertTrue(FragmentIds(result).Contains("frag-contact-signature"), "Signature-recognition fragment is selected");
        AssertFalse(result.Presentation.Scene.Message.Contains(faction.Name, StringComparison.Ordinal), "Rumored signature still does not expose the faction name");
    }

    private static void EstablishedContactMayNameFaction()
    {
        var (_, command, game, faction) = Create(FactionContactStatus.Contacted);
        var result = command.Execute(game, faction.Id, game.Expedition.Position);
        AssertEqual("identified", result.Presentation!.IdentityStage, "Contacted faction is identified");
        AssertTrue(result.Presentation.Scene.Message.Contains(faction.Name, StringComparison.Ordinal), "Identified scene may name the faction");
        AssertTrue(FragmentIds(result).Contains("frag-contact-identified"), "Identified contact fragment is selected");
    }

    private static void AuthoredMemoryChangesVisibleContactHistory()
    {
        var (_, command, game, faction) = Create(FactionContactStatus.Contacted);
        faction.AddMemory("memory-route-restored-welcome");

        var result = command.Execute(game, faction.Id, game.Expedition.Position);

        AssertTrue(FragmentIds(result).Contains("frag-contact-history-recognition"),
            "An authored memory exposes its generic visible-conduct fragment");
        AssertTrue(result.Presentation!.Scene.Message.Contains("frühere Hilfe", StringComparison.Ordinal),
            "Contact history is localized scene content rather than faction-specific command text");
    }

    private static void DeliveredFactionEventUsesTheSharedContactScene()
    {
        var (catalog, _, game, faction) = Create(FactionContactStatus.Contacted);
        faction.AddMemory("memory-route-restored-welcome");
        var eventState = new EventState("event-generated-contact", EventKind.FactionReaction,
            "Eine Abordnung nähert sich", faction.Name, "Eine Botin wartet am Rand des Lagers.",
            new[] { new EventOptionState("answer", "Antworten", "Die Expedition antwortet.", EventOptionEffectKind.None) },
            game.Expedition.Position, faction.Id);
        game.Events.Enqueue(eventState);

        var presentation = new GameApplication(catalog).GetCurrentEventContactPresentation(game);

        AssertTrue(presentation != null, "Faction reaction event resolves a delivered contact scene");
        AssertTrue(presentation!.Scene.Message.Contains(eventState.Body, StringComparison.Ordinal),
            "The already-delivered authored event body becomes the scene opening");
        AssertTrue(presentation.Scene.Message.Contains("frühere Hilfe", StringComparison.Ordinal),
            "Visible faction history is composed into the delivered event scene");
        AssertFalse(FragmentIds(presentation).Contains("frag-contact-opening"),
            "Delivered event prose supersedes the generic direct-contact opening");
    }

    private static void AnonymousDeliveredEventDoesNotRevealFactionIdentity()
    {
        var (catalog, _, game, faction) = Create(FactionContactStatus.Rumored);
        var eventState = new EventState("event-anonymous-contact", EventKind.FactionReaction,
            "Zeichen am Wegrand", faction.Name, "Über Nacht wurde ein neues Zeichen aufgestellt.",
            new[] { new EventOptionState("note", "Notieren", "Das Zeichen wurde notiert.", EventOptionEffectKind.None) },
            game.Expedition.Position, faction.Id);
        game.Events.Enqueue(eventState);

        var presentation = new GameApplication(catalog).GetCurrentEventContactPresentation(game);

        AssertEqual("anonymous", presentation!.IdentityStage, "Event without an earned faction reference remains anonymous");
        AssertFalse(presentation.Scene.Message.Contains(faction.Name, StringComparison.Ordinal),
            "Anonymous event scene cannot infer the matching world faction");
        AssertFalse(string.Equals(presentation.Scene.Subtitle, faction.Name, StringComparison.Ordinal),
            "Internal event faction reference cannot leak through its source label");
        AssertTrue(FragmentIds(presentation).Contains("frag-contact-anonymous"),
            "Anonymous relationship wording is composed into the delivered event");
    }

    private static (GameDataCatalog Catalog, OpenFactionInteractionCommand Command, GameState Game, FactionState Faction) Create(FactionContactStatus status)
    {
        var catalog = LoadCatalog();
        var signature = catalog.CrossSystem.FactionSignatures.All.OrderBy(item => item.Id, StringComparer.Ordinal).First();
        var faction = new FactionState("generated-faction", "Interner Fraktionsname", status,
            signatureProfileId: signature.ProfileId);
        var map = HexMapState.CreateFilled(new HexMapBounds(3, 3), TerrainType.Grassland);
        var expedition = new ExpeditionState(1, new HexCoord(1, 1),
            new[] { new ExpeditionMemberState("scout", "Mira", ExpeditionMemberRole.Scout) });
        var game = new GameState(new WorldState(map), new KnowledgeState(), new PlayerNotesState(), expedition,
            new BaseState(HexCoord.Zero), factions: new[] { faction });
        var resolver = new FactionContactSceneResolver(catalog.Scenes, catalog.CrossSystem.FactionSignatures, catalog.Authoring);
        var profiles = new FactionContactProfileService(catalog.Scenes, catalog.CrossSystem.FactionProfiles);
        return (catalog, new OpenFactionInteractionCommand(sceneResolver: resolver, contactProfileService: profiles), game, faction);
    }

    private static string[] FragmentIds(FactionInteractionResult result) => result.Presentation!.Scene.Paragraphs
        .SelectMany(paragraph => paragraph.FragmentIds).ToArray();

    private static string[] FragmentIds(FactionContactPresentation presentation) => presentation.Scene.Paragraphs
        .SelectMany(paragraph => paragraph.FragmentIds).ToArray();

    private static GameDataCatalog LoadCatalog()
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "UnityHexMapView", "Assets", "StreamingAssets", "GameData");
        return GameDataCatalog.LoadFromDirectory(root) ?? throw new InvalidOperationException("Game data was not loaded.");
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!Equals(expected, actual)) throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void AssertFalse(bool condition, string message) => AssertTrue(!condition, message);
}
