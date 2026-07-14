using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;

namespace Game.App
{

public sealed class ScoutMissionResolutionService
{
    private readonly EvidenceDefinitionSet? evidenceDefinitions;
    private readonly FactionSignatureDefinitionSet? factionSignatures;
    private readonly ScoutContentDefinitionSet? scoutContent;

    public ScoutMissionResolutionService(EvidenceDefinitionSet? evidenceDefinitions = null, FactionSignatureDefinitionSet? factionSignatures = null, ScoutContentDefinitionSet? scoutContent = null)
    {
        this.evidenceDefinitions = evidenceDefinitions;
        this.factionSignatures = factionSignatures;
        this.scoutContent = scoutContent;
    }

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
                AddLocationSurroundingsEvidence(game, mission, report);
                AddDirectionalLeadEvidence(game, mission, report);
                if (report.Leads.Count > 0 && game.Knowledge.ClaimKnowledgeSource(ScoutKnowledgeSourceId(mission)))
                {
                    game.Expedition.AddUnsecuredKnowledge(outcome == ScoutMissionStatus.ReturnedInjured ? 2 : 3);
                }
            }

            results.Add(new ScoutMissionResolutionResult(mission.Id, outcome, report));
        }

        return results;
    }

    private ScoutMissionStatus DetermineOutcome(ScoutMissionState mission, int worldDay)
    {
        var configured = scoutContent?.FindOutcome(mission);
        if (configured != null)
        {
            return configured.Outcome;
        }

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

    private ScoutReportState CreateReport(GameState game, ScoutMissionState mission, ScoutMissionStatus outcome)
    {
        var sampledCoords = BuildRelatedCoords(game, mission);
        var reliability = ReliabilityFor(game, mission, outcome);
        var template = scoutContent?.FindReport(mission, outcome);
        var title = template?.Title ?? $"Scout report: {mission.Direction} {mission.Focus}";
        var body = template?.Body ?? (outcome == ScoutMissionStatus.ReturnedInjured
            ? "The scouts returned hurt and shaken. Their notes are incomplete, but they marked signs worth checking."
            : "The scouts returned with a cautious account of terrain, signs and possible routes. Treat it as useful, not certain.");

        var hints = new List<string>
        {
            template?.Hint ?? HintFor(mission),
            $"Reliability {reliability}/100. Confirm on foot before trusting it fully."
        };
        var leads = BuildLeads(game, mission, sampledCoords, reliability);
        hints.AddRange(leads.Select(lead => lead.Summary));

        return new ScoutReportState(
            $"scout-report-{game.Knowledge.ScoutReports.Count + 1}",
            mission.Id,
            title,
            body,
            reliability,
            Array.Empty<HexCoord>(),
            hints,
            leads);
    }

    private IReadOnlyList<ScoutLeadState> BuildLeads(GameState game, ScoutMissionState mission, IReadOnlyList<HexCoord> sampledCoords, int reliability)
    {
        if (mission.MissionTypeId == "location-surroundings")
        {
            var location = game.World.Locations.FirstOrDefault(item => item.Id == mission.TargetLocationId);
            if (location == null) return Array.Empty<ScoutLeadState>();
            var relation = location.FactionRelations.FirstOrDefault();
            var faction = relation == null ? null : game.FindFaction(relation.FactionId);
            var symbolId = factionSignatures?.FindForProfile(faction?.SignatureProfileId)?.Id;
            var summary = relation == null
                ? "In der unmittelbaren Umgebung finden sich keine eindeutigen Spuren regelmaessiger Kontrolle."
                : "In der unmittelbaren Umgebung finden sich wiederkehrende Zeichen, Nutzungsspuren oder Beobachtungspunkte.";
            return new[] { new ScoutLeadState(ScoutLeadKind.LocalContext, ScoutLeadScope.Local, mission.Direction, reliability, summary, symbolId, location.Id) };
        }

        foreach (var location in game.World.Locations)
        {
            if (!location.Anchor.Coords.Any(sampledCoords.Contains) || game.Knowledge.GetTileKnowledge(location.Coord) == KnowledgeLevel.Confirmed)
            {
                continue;
            }

            return new[]
            {
                new ScoutLeadState(
                    ScoutLeadKind.LocationSighting,
                    ScoutLeadScope.Directional,
                    mission.Direction,
                    reliability,
                    $"Irgendwo im {DirectionText(mission.Direction)} liegt eine auffaellige Struktur oder Spur. Sie ist nicht bestaetigt.")
            };
        }

        return new[]
        {
            new ScoutLeadState(LeadKindFor(mission.Focus), ScoutLeadScope.Directional, mission.Direction, reliability, HintFor(mission))
        };
    }

    private static IReadOnlyList<HexCoord> BuildRelatedCoords(GameState game, ScoutMissionState mission)
    {
        var coords = new List<HexCoord>();
        var seen = new HashSet<HexCoord>();
        var offset = mission.Direction.ToScoutOffset();
        for (var step = 1; step <= mission.DurationDays; step++)
        {
            var routeCenter = OffsetFromOrigin(mission.Origin, offset, step);
            AddReportedCoord(game, coords, seen, routeCenter);

            foreach (var neighbor in routeCenter.Neighbors())
            {
                AddReportedCoord(game, coords, seen, neighbor);
            }
        }

        return coords;
    }

    private static HexCoord OffsetFromOrigin(HexCoord origin, HexCoord offset, int steps)
    {
        return new HexCoord(origin.Q + offset.Q * steps, origin.R + offset.R * steps);
    }

    private static void AddReportedCoord(GameState game, List<HexCoord> coords, HashSet<HexCoord> seen, HexCoord coord)
    {
        if (!game.World.Map.Contains(coord) || !seen.Add(coord))
        {
            return;
        }

        coords.Add(coord);
    }

    private static string ScoutKnowledgeSourceId(ScoutMissionState mission)
    {
        return $"scout-report:{mission.MissionTypeId}:{mission.Origin.Q}:{mission.Origin.R}:{mission.Direction}:{mission.DurationDays}:{mission.Focus}";
    }

    private static int ReliabilityFor(GameState game, ScoutMissionState mission, ScoutMissionStatus outcome)
    {
        var starLevel = mission.ScoutMemberIds
            .Select(id => game.Expedition.FindMember(id)?.StarLevel ?? 0)
            .DefaultIfEmpty(0)
            .Average();
        var reliability = 58 + (int)Math.Round(starLevel * 11) + (mission.Behavior == ScoutMissionBehavior.Cautious ? 8 : 0) - Math.Max(0, mission.DurationDays - 1) * 4;
        if (outcome == ScoutMissionStatus.ReturnedInjured)
        {
            reliability -= 20;
        }
        return Math.Max(25, Math.Min(92, reliability));
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

    private static ScoutLeadKind LeadKindFor(ScoutMissionFocus focus)
    {
        return focus switch
        {
            ScoutMissionFocus.Route => ScoutLeadKind.RouteHint,
            ScoutMissionFocus.FactionSigns => ScoutLeadKind.FactionSignature,
            ScoutMissionFocus.Ruins => ScoutLeadKind.LocationSighting,
            ScoutMissionFocus.Resources => ScoutLeadKind.EnvironmentalChange,
            _ => ScoutLeadKind.WitnessTrace
        };
    }

    private static string DirectionText(ScoutDirection direction)
    {
        return direction switch
        {
            ScoutDirection.North => "Norden",
            ScoutDirection.NorthEast => "Nordosten",
            ScoutDirection.East => "Osten",
            ScoutDirection.SouthEast => "Suedosten",
            ScoutDirection.South => "Sueden",
            ScoutDirection.SouthWest => "Suedwesten",
            ScoutDirection.West => "Westen",
            ScoutDirection.NorthWest => "Nordwesten",
            _ => "gewählten Sektor"
        };
    }

    private void AddLocationSurroundingsEvidence(GameState game, ScoutMissionState mission, ScoutReportState report)
    {
        if (string.IsNullOrWhiteSpace(mission.TargetLocationId))
        {
            return;
        }

        var location = game.World.Locations.FirstOrDefault(item => item.Id == mission.TargetLocationId);
        if (location == null)
        {
            return;
        }

        var relation = location.FactionRelations.FirstOrDefault();
        var definitionId = relation == null
            ? "evidence-location-surroundings-quiet"
            : location.EvidenceSeedIds.FirstOrDefault() ?? "evidence-location-surroundings-signs";
        var fallbackText = relation == null
            ? "Die Scouts fanden keine eindeutigen Zeichen, dass jemand diesen Ort regelmaessig kontrolliert."
            : "Die Scouts fanden wiederkehrende Zeichen, Wege oder Spuren. Jemand scheint diesen Ort zu beachten.";
        var definition = evidenceDefinitions?.Find(definitionId);
        var text = definition?.ScoutReportText ?? fallbackText;
        var signatureId = definition?.SymbolId;
        if (relation != null)
        {
            var faction = game.FindFaction(relation.FactionId);
            signatureId = factionSignatures?.FindForProfile(faction?.SignatureProfileId)?.Id ?? signatureId;
        }
        var evidenceId = $"evidence-scout-{report.Id}-{location.Id}";
        game.Knowledge.AddEvidence(new EvidenceState(
            evidenceId,
            definitionId,
            EvidenceSourceKind.ScoutReport,
            EvidenceKnowledgeState.Reported,
            text,
            subjectLocationId: location.Id,
            symbolId: signatureId,
            confidence: report.Reliability));

        if (relation != null)
        {
            game.World.EscalateFactionAwareness(relation.FactionId, $"location-region:{location.Id}");
        }
    }

    private void AddDirectionalLeadEvidence(GameState game, ScoutMissionState mission, ScoutReportState report)
    {
        if (mission.MissionTypeId != "directional-recon" || report.Leads.Count == 0) return;
        var lead = report.Leads[0];
        var definitionId = lead.Kind switch
        {
            ScoutLeadKind.LocationSighting => "evidence-directional-location-sighting",
            ScoutLeadKind.RouteHint => "evidence-directional-route-hint",
            ScoutLeadKind.FactionSignature => "evidence-directional-sign-trace",
            ScoutLeadKind.EnvironmentalChange => "evidence-directional-environmental-change",
            ScoutLeadKind.HazardIndication => "evidence-directional-hazard-trace",
            _ => "evidence-directional-witness-trace"
        };
        var definition = evidenceDefinitions?.Find(definitionId);
        var text = definition?.ScoutReportText ?? lead.Summary;
        game.Knowledge.AddEvidence(new EvidenceState(
            $"evidence-scout-{report.Id}-directional",
            definitionId,
            EvidenceSourceKind.ScoutReport,
            EvidenceKnowledgeState.Reported,
            text,
            symbolId: lead.SymbolId,
            confidence: lead.Confidence));
    }
}
}
