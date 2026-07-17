#nullable enable
using System;
using Game.Core;

namespace Game.App
{

public sealed class OpenFactionInteractionCommand
{
    private readonly AuthoredFactionOfferService? authoredOfferService;
    private readonly FactionContactSceneResolver? sceneResolver;
    private readonly FactionContactProfileService? contactProfileService;

    public OpenFactionInteractionCommand(AuthoredFactionOfferService? authoredOfferService = null,
        FactionContactSceneResolver? sceneResolver = null, FactionContactProfileService? contactProfileService = null)
    {
        this.authoredOfferService = authoredOfferService;
        this.sceneResolver = sceneResolver;
        this.contactProfileService = contactProfileService;
    }

    public FactionInteractionResult Execute(GameState game, string factionId, HexCoord coord)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        if (string.IsNullOrWhiteSpace(factionId))
        {
            throw new ArgumentException("Faction id must not be empty.", nameof(factionId));
        }

        var faction = game.FindFaction(factionId);
        if (faction == null)
        {
            return FactionInteractionResult.Rejected("Unknown faction.");
        }

        var interaction = CreateInteraction(game, faction, coord);
        game.SetActiveFactionInteraction(interaction);
        faction.AddMemory($"interaction-opened:{game.Expedition.ExpeditionNumber}:{game.World.WorldDay}:{coord.Q}:{coord.R}");

        var presentation = sceneResolver?.Resolve(game, interaction);
        return FactionInteractionResult.Opened(interaction,
            presentation?.Scene.Message ?? $"Contact opened with {interaction.Representative.DisplayName}.", presentation);
    }

    private FactionInteractionState CreateInteraction(GameState game, FactionState faction, HexCoord coord)
    {
        var authored = contactProfileService?.Resolve(faction) ?? new FactionContactAuthoredPresentation(
            FactionRepresentativeRole.Watcher, "Beobachtende Person",
            "Eine vorsichtige Person tritt vor, deren Stellung innerhalb der Gruppe unklar bleibt.",
            "Wir hören euch an. Was danach geschieht, ist noch offen.");
        var representative = new FactionRepresentativeState(
            $"{faction.Id}-{authored.Role.ToString().ToLowerInvariant()}", faction.Id,
            authored.RepresentativeName, authored.Role, authored.Description);
        return new FactionInteractionState(
            $"interaction-{faction.Id}-{game.Expedition.ExpeditionNumber}-{game.World.WorldDay}",
            faction.Id,
            faction.Name,
            coord,
            representative,
            AttitudeFor(faction),
            authored.Dialogue,
            authoredOfferService?.BuildOffers(faction) ?? Array.Empty<FactionOfferState>());
    }

    private static string AttitudeFor(FactionState faction)
    {
        if (faction.Anger >= 30)
        {
            return "Tense";
        }

        if (faction.Trust >= 10 || faction.ContactStatus == FactionContactStatus.Open)
        {
            return "Open";
        }

        return "Watchful";
    }

}
}
