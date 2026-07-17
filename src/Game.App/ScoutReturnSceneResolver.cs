#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;

namespace Game.App
{

/// <summary>Builds a scout-return scene from one immutable delivered outcome and its delivered reports.</summary>
public sealed class ScoutReturnSceneResolver
{
    private readonly SceneDescriptionResolver resolver;

    public ScoutReturnSceneResolver(SceneDescriptionCatalog scenes)
    {
        resolver = new SceneDescriptionResolver(scenes ?? throw new ArgumentNullException(nameof(scenes)));
    }

    public SceneDescriptionResult Resolve(GameState game, DeliveredMissionOutcomeState outcome, string? locale = null)
    {
        if (game == null) throw new ArgumentNullException(nameof(game));
        if (outcome == null) throw new ArgumentNullException(nameof(outcome));
        var returned = outcome.ParticipantOutcomes.Where(item => item.Returned).ToList();
        var lead = returned.FirstOrDefault() ?? outcome.ParticipantOutcomes[0];
        var companion = outcome.ParticipantOutcomes.FirstOrDefault(item => item.MemberId != lead.MemberId);
        var reports = outcome.DeliveredReportIds
            .Select(id => game.Knowledge.ScoutReports.FirstOrDefault(report => report.Id == id))
            .Where(report => report != null).Cast<ScoutReportState>().ToList();
        var reliability = reports.Count == 0 ? (int?)null : reports.Min(report => report.Reliability);
        var memberName = MemberName(game, lead.MemberId);
        var companionName = companion == null ? null : MemberName(game, companion.MemberId);
        var title = reports.FirstOrDefault()?.Title ?? memberName;
        return resolver.ResolveScoutReturn(new ScoutReturnSceneView(
            outcome.DeliveryId, title, "Scout-Rückkehr", "placeholder-scout",
            outcome.MissionStatus.ToString(), outcome.ParticipantOutcomes.Select(StatusText),
            TeamOutcome(outcome), reliability, hasFindings: false,
            hasLeads: reports.Any(report => report.Leads.Count > 0), outcome.WasOverdue,
            outcome.LostEquipmentIds.Count > 0, memberName, companionName, DaysOverdue(outcome), locale));
    }

    private static string MemberName(GameState game, string memberId) =>
        game.Expedition.FindMember(memberId)?.Name ?? memberId;

    private static string StatusText(DeliveredMissionParticipantOutcome participant) => participant.DeliveredStatus switch
    {
        DeliveredScoutStatus.Unhurt => "unhurt",
        DeliveredScoutStatus.Injured => "Injured",
        DeliveredScoutStatus.Exhausted => "Exhausted",
        DeliveredScoutStatus.Missing => "Missing",
        DeliveredScoutStatus.Overdue => "Missing",
        _ => "Missing"
    };

    private static string TeamOutcome(DeliveredMissionOutcomeState outcome)
    {
        var returned = outcome.ParticipantOutcomes.Count(item => item.Returned);
        if (returned == 0) return "none-returned";
        return returned == outcome.ParticipantOutcomes.Count ? "all-returned" : "partial-return";
    }

    private static string DaysOverdue(DeliveredMissionOutcomeState outcome)
    {
        var day = outcome.ActualReturnWorldDay ?? outcome.DeliveredWorldDay;
        var days = Math.Max(0, day - outcome.ExpectedReturnWorldDay);
        return days == 1 ? "einen Tag" : $"{days} Tage";
    }
}

}
