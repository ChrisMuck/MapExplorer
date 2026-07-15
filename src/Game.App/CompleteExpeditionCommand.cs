#nullable enable
using System;
using Game.Core;

namespace Game.App
{

public sealed class CompleteExpeditionCommand
{
    public const int NormalPreparationDays = 3;
    private readonly FindingAnalysisHandoffService? findingAnalysisHandoffService;

    public CompleteExpeditionCommand(FindingAnalysisHandoffService? findingAnalysisHandoffService = null)
    {
        this.findingAnalysisHandoffService = findingAnalysisHandoffService;
    }

    public CompleteExpeditionResult Execute(GameState game)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        if (game.Expedition.Status != ExpeditionStatus.Active)
        {
            return CompleteExpeditionResult.Rejected("Expedition is not active.");
        }

        if (game.Expedition.Position != game.Base.Location)
        {
            return CompleteExpeditionResult.Rejected("The expedition must return to base before it can be completed.");
        }

        foreach (var mission in game.Expedition.ScoutMissions)
        {
            if (mission.Status == ScoutMissionStatus.Active || mission.Status == ScoutMissionStatus.Overdue)
            {
                return CompleteExpeditionResult.Rejected("All scout missions must be resolved before completing the expedition.");
            }
        }

        var securedKnowledge = game.Expedition.ClearUnsecuredKnowledge();
        game.Base.AddKnowledgePoints(securedKnowledge);
        var returnedFindings = findingAnalysisHandoffService?.TransferReturnedFindings(game) ?? Array.Empty<EvaluationItemState>();

        game.Expedition.SetStatus(ExpeditionStatus.Returned);
        game.Base.ScheduleNextExpedition(ExpeditionStatus.Returned, game.World.WorldDay, NormalPreparationDays);
        var archiveEntry = $"Expedition {game.Expedition.ExpeditionNumber} returned on world day {game.World.WorldDay} after {game.Expedition.ExpeditionDay} day(s). Reports, notes and faction observations were secured. Knowledge secured: {securedKnowledge}. Findings queued for analysis: {returnedFindings.Count}.";
        game.Base.AddArchiveEntry(new ArchiveEntryState(
            archiveEntry,
            ArchiveEntryKind.Bericht,
            $"Expedition {game.Expedition.ExpeditionNumber}",
            game.World.WorldDay,
            ArchiveReliability.Hoch));
        game.Base.SecureCurrentExpeditionArchiveEntries();

        // Survivors return to the persistent base roster (their injuries/status carry over).
        game.Roster.AbsorbReturningMembers(game.Expedition);

        return CompleteExpeditionResult.Completed(
            game.Base.Location,
            game.Expedition.ExpeditionNumber,
            game.Expedition.ExpeditionDay,
            game.Base.NextExpeditionAvailableWorldDay,
            securedKnowledge,
            game.Base.KnowledgePoints,
            archiveEntry);
    }
}
}
