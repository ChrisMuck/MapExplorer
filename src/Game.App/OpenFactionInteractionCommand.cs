#nullable enable
using System;
using Game.Core;

namespace Game.App
{

public sealed class OpenFactionInteractionCommand
{
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

        if (faction.Id == "hidden-ones" && faction.ContactStatus != FactionContactStatus.Open)
        {
            return FactionInteractionResult.Rejected("This faction is not willing to speak directly.");
        }

        var interaction = CreateInteraction(game, faction, coord);
        game.SetActiveFactionInteraction(interaction);
        faction.AddMemory($"interaction-opened:{game.Expedition.ExpeditionNumber}:{game.World.WorldDay}:{coord.Q}:{coord.R}");

        return FactionInteractionResult.Opened(interaction, $"Contact opened with {interaction.Representative.DisplayName}.");
    }

    private static FactionInteractionState CreateInteraction(GameState game, FactionState faction, HexCoord coord)
    {
        var representative = RepresentativeFor(faction);
        return new FactionInteractionState(
            $"interaction-{faction.Id}-{game.Expedition.ExpeditionNumber}-{game.World.WorldDay}",
            faction.Id,
            faction.Name,
            coord,
            representative,
            AttitudeFor(faction),
            DialogueFor(faction),
            FactionInteractionDefinitions.BuildOffers(game, faction));
    }

    private static FactionRepresentativeState RepresentativeFor(FactionState faction)
    {
        switch (faction.Id)
        {
            case "coastal-people":
                return new FactionRepresentativeState(
                    "coastal-messenger",
                    faction.Id,
                    "River Messenger",
                    FactionRepresentativeRole.Messenger,
                    "A messenger from the river villages, cautious but not hostile.");
            case "border-wardens":
                return new FactionRepresentativeState(
                    "warden-guard",
                    faction.Id,
                    "Warden Scout",
                    FactionRepresentativeRole.Guard,
                    "A border scout who speaks for the patrol, not for the leaders.");
            default:
                return new FactionRepresentativeState(
                    $"{faction.Id}-watcher",
                    faction.Id,
                    "Watcher",
                    FactionRepresentativeRole.Watcher,
                    "A cautious representative whose authority is unclear.");
        }
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

    private static string DialogueFor(FactionState faction)
    {
        switch (faction.Id)
        {
            case "coastal-people":
                return "You have come far from your shore camp. We can trade a little, but we will not speak for every village.";
            case "border-wardens":
                return "You crossed watched land. A warning is not a wall, but it is still a warning. Bring proof of respect if you want more than words.";
            default:
                return "The representative watches the expedition carefully and waits for a reason to continue.";
        }
    }

}
}
