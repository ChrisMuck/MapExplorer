#nullable enable
using System;
using Game.Core;

namespace Game.App
{

public sealed class EndDayCommand
{
    private readonly int suppliesPerDay;
    private readonly ScoutMissionResolutionService scoutMissionResolutionService;

    public EndDayCommand(int suppliesPerDay = 2, ScoutMissionResolutionService? scoutMissionResolutionService = null)
    {
        if (suppliesPerDay < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(suppliesPerDay), suppliesPerDay, "Supply consumption must not be negative.");
        }

        this.suppliesPerDay = suppliesPerDay;
        this.scoutMissionResolutionService = scoutMissionResolutionService ?? new ScoutMissionResolutionService();
    }

    public EndDayResult Execute(GameState game)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        if (game.Expedition.Status != ExpeditionStatus.Active)
        {
            return EndDayResult.Rejected("Expedition is not active.");
        }

        var consumed = Math.Min(game.Expedition.Supplies, suppliesPerDay);
        game.Expedition.ConsumeSupplies(suppliesPerDay);
        game.Expedition.AdvanceExpeditionDay();
        game.World.AdvanceDays(1);
        var scoutResolutions = scoutMissionResolutionService.ResolveDueMissions(game);

        return EndDayResult.Advanced(game.World.WorldDay, game.Expedition.ExpeditionDay, consumed, scoutResolutions);
    }
}
}
