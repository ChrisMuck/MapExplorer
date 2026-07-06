using System;
using System.Collections.Generic;
using Game.Core;

namespace Game.App
{

public sealed class ScoutMissionResolutionService
{
    public IReadOnlyList<ScoutMissionResolutionResult> ResolveDueMissions(GameState game)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        var results = new List<ScoutMissionResolutionResult>();
        foreach (var mission in game.Expedition.ScoutMissions)
        {
            if (mission.Status != ScoutMissionStatus.Active && mission.Status != ScoutMissionStatus.Overdue)
            {
                continue;
            }

            if (game.World.WorldDay < mission.ExpectedReturnWorldDay)
            {
                continue;
            }

            var outcome = DetermineOutcome(mission, game.World.WorldDay);
            mission.SetStatus(outcome);
            ApplyMemberOutcome(game, mission, outcome);

            ScoutReportState? report = null;
            if (outcome == ScoutMissionStatus.Returned || outcome == ScoutMissionStatus.ReturnedInjured)
            {
                report = CreateReport(game, mission, outcome);
                game.Knowledge.AddScoutReport(report);
                var reportedNewKnowledge = false;
                foreach (var coord in report.RelatedCoords)
                {
                    if (game.World.Map.Contains(coord))
                    {
                        reportedNewKnowledge = reportedNewKnowledge || game.Knowledge.GetTileKnowledge(coord) == KnowledgeLevel.Unknown;
                        game.Knowledge.PromoteTileKnowledge(coord, KnowledgeLevel.Reported);
                    }
                }

                if (reportedNewKnowledge && game.Knowledge.ClaimKnowledgeSource(ScoutKnowledgeSourceId(report)))
                {
                    game.Expedition.AddUnsecuredKnowledge(outcome == ScoutMissionStatus.ReturnedInjured ? 2 : 3);
                }
            }

            results.Add(new ScoutMissionResolutionResult(mission.Id, outcome, report));
        }

        return results;
    }

    private static ScoutMissionStatus DetermineOutcome(ScoutMissionState mission, int worldDay)
    {
        if (mission.Status == ScoutMissionStatus.Overdue)
        {
            return worldDay > mission.ExpectedReturnWorldDay
                ? ScoutMissionStatus.Returned
                : ScoutMissionStatus.Overdue;
        }

        if (mission.Behavior == ScoutMissionBehavior.Cautious)
        {
            return ScoutMissionStatus.Returned;
        }

        if (mission.Behavior == ScoutMissionBehavior.Balanced)
        {
            return ScoutMissionStatus.Overdue;
        }

        return mission.Focus == ScoutMissionFocus.Ruins
            ? ScoutMissionStatus.Missing
            : ScoutMissionStatus.ReturnedInjured;
    }

    private static void ApplyMemberOutcome(GameState game, ScoutMissionState mission, ScoutMissionStatus outcome)
    {
        foreach (var memberId in mission.ScoutMemberIds)
        {
            var member = game.Expedition.FindMember(memberId);
            if (member == null)
            {
                continue;
            }

            switch (outcome)
            {
                case ScoutMissionStatus.Returned:
                    member.SetStatus(ExpeditionMemberStatus.Available);
                    break;
                case ScoutMissionStatus.ReturnedInjured:
                    member.SetStatus(ExpeditionMemberStatus.Injured);
                    break;
                case ScoutMissionStatus.Missing:
                    member.SetStatus(ExpeditionMemberStatus.Missing);
                    break;
                case ScoutMissionStatus.Overdue:
                    member.SetStatus(ExpeditionMemberStatus.Assigned);
                    break;
            }
        }
    }

    private static ScoutReportState CreateReport(GameState game, ScoutMissionState mission, ScoutMissionStatus outcome)
    {
        var relatedCoords = BuildRelatedCoords(game, mission);
        var reliability = ReliabilityFor(mission, outcome);
        var title = $"Scout report: {mission.Direction} {mission.Focus}";
        var body = outcome == ScoutMissionStatus.ReturnedInjured
            ? "The scouts returned hurt and shaken. Their notes are incomplete, but they marked signs worth checking."
            : "The scouts returned with a cautious account of terrain, signs and possible routes. Treat it as useful, not certain.";

        var hints = new List<string>
        {
            HintFor(mission),
            $"Reliability {reliability}/100. Confirm on foot before trusting it fully."
        };

        return new ScoutReportState(
            $"scout-report-{game.Knowledge.ScoutReports.Count + 1}",
            mission.Id,
            title,
            body,
            reliability,
            relatedCoords,
            hints);
    }

    private static IReadOnlyList<HexCoord> BuildRelatedCoords(GameState game, ScoutMissionState mission)
    {
        var coords = new List<HexCoord>();
        var offset = mission.Direction.ToScoutOffset();
        var current = mission.Origin;
        for (var i = 0; i < mission.DurationDays + 1; i++)
        {
            current += offset;
            if (game.World.Map.Contains(current))
            {
                coords.Add(current);
            }
        }

        return coords;
    }

    private static string ScoutKnowledgeSourceId(ScoutReportState report)
    {
        var parts = new List<string>();
        foreach (var coord in report.RelatedCoords)
        {
            parts.Add($"{coord.Q}:{coord.R}");
        }

        return $"scout-report:{string.Join("|", parts)}";
    }

    private static int ReliabilityFor(ScoutMissionState mission, ScoutMissionStatus outcome)
    {
        if (outcome == ScoutMissionStatus.ReturnedInjured)
        {
            return 45;
        }

        return mission.Behavior == ScoutMissionBehavior.Cautious ? 78 : 62;
    }

    private static string HintFor(ScoutMissionState mission)
    {
        switch (mission.Focus)
        {
            case ScoutMissionFocus.Route:
                return "Route signs suggest a possible path, but obstacles may be missing from the report.";
            case ScoutMissionFocus.Resources:
                return "The scouts noted resource traces near the edge of their route.";
            case ScoutMissionFocus.FactionSigns:
                return "The scouts found signs that may mark faction presence or territorial warnings.";
            case ScoutMissionFocus.Ruins:
                return "The scouts describe old stonework and sealed structures, but details are uncertain.";
            default:
                return "The scouts sketched terrain and visibility from the route.";
        }
    }
}
}
