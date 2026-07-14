#nullable enable
using System;
using System.Collections.Generic;
using Game.Core;

namespace Game.App
{

/// <summary>Moves returned field findings into the existing base evaluation queue.</summary>
public sealed class FindingAnalysisHandoffService
{
    private readonly CrossSystemAuthoringBundle authoring;

    public FindingAnalysisHandoffService(CrossSystemAuthoringBundle authoring)
    {
        this.authoring = authoring ?? throw new ArgumentNullException(nameof(authoring));
    }

    public IReadOnlyList<EvaluationItemState> TransferReturnedFindings(GameState game)
    {
        if (game == null) throw new ArgumentNullException(nameof(game));

        var transferred = new List<EvaluationItemState>();
        foreach (var finding in game.Expedition.ClearFieldFindings())
        {
            if (!authoring.Findings.TryGetValue(finding.DefinitionId, out var definition)) continue;

            var random = new WorldDeterministicRandomSource(game.World);
            var duration = RollInclusive(random, definition.Analysis.MinDurationDays, definition.Analysis.MaxDurationDays);
            var reward = RollInclusive(random, definition.Analysis.MinKnowledgePoints, definition.Analysis.MaxKnowledgePoints);
            var item = new EvaluationItemState(
                finding.Id,
                definition.FieldDescription,
                finding.SourceLocationId,
                duration,
                reward,
                definition.Analysis.Explanation);
            game.Base.EvaluationQueue.Add(item);
            transferred.Add(item);
            game.Base.AddArchiveEntry(new ArchiveEntryState(
                $"Fund eingegangen: {definition.FieldDescription}. Die Auswertung beginnt im Archiv.",
                ArchiveEntryKind.Bericht,
                "Rückkehrende Expedition",
                game.World.WorldDay,
                ArchiveReliability.Hoch));
        }

        return transferred;
    }

    private static int RollInclusive(WorldDeterministicRandomSource random, int minimum, int maximum)
    {
        return minimum == maximum ? minimum : minimum + random.NextInt(maximum - minimum + 1);
    }
}
}
