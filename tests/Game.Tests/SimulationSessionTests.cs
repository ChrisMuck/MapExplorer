#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Game.App;
using Game.Core;

internal sealed class SimulationSessionTests
{
    public void RunAll()
    {
        CommandHistoryAndRunRecordAreDeterministic();
        SessionUsesTheSameLocationOptionContractAsTheApplication();
        FailingScenarioIncludesReproductionContext();
        FixtureProofScenariosReplayToTheSamePlayerState();
        ScenarioPlaybackStepsSharedCommandsAndNormalDays();
        ScenarioPlaybackRunsEveryProofScenario();
        InteractiveLocationCommandsRemainOptionalToTheScript();
        TeamPreviewUsesTheSharedOptionAvailabilityRulesWithoutChangingTheSession();
    }

    private static void CommandHistoryAndRunRecordAreDeterministic()
    {
        var catalog = LoadCatalog();
        var first = SimulationSession.CreateGenerated(catalog, Request());
        var second = SimulationSession.CreateGenerated(catalog, Request());

        first.SendDirectionalScout(new[] { "scout-1" }, ScoutDirection.East, 1, ScoutMissionFocus.Survey, ScoutMissionBehavior.Cautious);
        second.SendDirectionalScout(new[] { "scout-1" }, ScoutDirection.East, 1, ScoutMissionFocus.Survey, ScoutMissionBehavior.Cautious);
        first.EndDay();
        second.EndDay();

        var firstRecord = first.CreateRunRecord();
        var secondRecord = second.CreateRunRecord();
        AssertEqual(2, firstRecord.Commands.Count, "Session records commands sent through the shared facade");
        AssertEqual(firstRecord.Commands[0].CommandId, secondRecord.Commands[0].CommandId, "Identical command sequence is replayable");
        AssertEqual(first.Game.Knowledge.ScoutReports[0].Leads[0].Summary, second.Game.Knowledge.ScoutReports[0].Leads[0].Summary, "Same seed keeps player-facing leads deterministic");
        AssertEqual(catalog.ContentVersion, firstRecord.ContentVersion, "Run record carries the catalog content version");
    }

    private static void FailingScenarioIncludesReproductionContext()
    {
        var scenario = new DevelopmentScenario
        {
            Id = "scenario-invalid-command",
            Seed = 41027,
            Commands = new List<DevelopmentScenarioCommand> { new() { Kind = "not-a-command" } }
        };
        var result = new DevelopmentScenarioExecutor().Execute(LoadCatalog(), scenario);

        AssertTrue(!result.Success, "Invalid scenario command fails");
        AssertTrue(result.FailureSummary.Contains("seed 41027", StringComparison.Ordinal), "Failure reports the reproducible seed");
        AssertTrue(result.FailureSummary.Contains("command 1", StringComparison.Ordinal), "Failure reports command index");
        AssertTrue(result.Session.Game.World.Traces.Count > 0, "Invalid scenario command records a reproducible trace tail");
    }

    private static void SessionUsesTheSameLocationOptionContractAsTheApplication()
    {
        var scenario = DevelopmentScenarioLoader.LoadFile(Path.Combine(Directory.GetCurrentDirectory(), "tests", "DevelopmentScenarios", "scenario-claimed-crossing.json"));
        var result = new DevelopmentScenarioExecutor().Execute(LoadCatalog(), scenario);
        const string locationId = "location-route-proof";
        var throughSession = result.Session.GetLocationInteraction(locationId);
        var directlyThroughApplication = result.Session.Application.GetLocationInteraction(result.Session.Game, locationId);

        AssertTrue(throughSession.Success && directlyThroughApplication.Success, "Claimed crossing exposes the shared option contract");
        AssertEqual(
            string.Join("|", throughSession.Interaction!.Options.Select(option => $"{option.Action.Id}:{option.IsAvailable}:{option.LockedReason}")),
            string.Join("|", directlyThroughApplication.Interaction!.Options.Select(option => $"{option.Action.Id}:{option.IsAvailable}:{option.LockedReason}")),
            "Session and application expose identical player-visible location options");
    }

    private static void FixtureProofScenariosReplayToTheSamePlayerState()
    {
        foreach (var fileName in new[] { "scenario-claimed-crossing.json", "scenario-sealed-containment.json", "scenario-directional-lead.json" })
        {
            var scenario = DevelopmentScenarioLoader.LoadFile(Path.Combine(Directory.GetCurrentDirectory(), "tests", "DevelopmentScenarios", fileName));
            var first = new DevelopmentScenarioExecutor().Execute(LoadCatalog(), scenario);
            var exported = JsonSerializer.Serialize(first.CreateRunRecord());
            var imported = JsonSerializer.Deserialize<DevelopmentScenarioRunRecord>(exported)
                ?? throw new InvalidOperationException("Scenario run record was not deserialized.");
            var second = imported.Replay(LoadCatalog());

            AssertTrue(first.Success && second.Success, $"Fixture scenario '{fileName}' succeeds and its exported record replays");
            AssertEqual(first.Session.Game.World.WorldDay, second.Session.Game.World.WorldDay, $"Fixture scenario '{fileName}' replays to the same world day");
            AssertEqual(
                string.Join("|", first.Session.Game.Knowledge.Evidence.Select(item => item.DefinitionId)),
                string.Join("|", second.Session.Game.Knowledge.Evidence.Select(item => item.DefinitionId)),
                $"Fixture scenario '{fileName}' replays to the same player knowledge");

            foreach (var location in first.Session.Game.World.Locations)
            {
                var firstInteraction = first.Session.Application.GetLocationInteraction(first.Session.Game, location.Id);
                var secondInteraction = second.Session.Application.GetLocationInteraction(second.Session.Game, location.Id);
                AssertEqual(firstInteraction.Success, secondInteraction.Success, $"Fixture scenario '{fileName}' replays to the same location knowledge");
                if (!firstInteraction.Success) continue;
                var firstOptions = firstInteraction.Interaction!.Options
                    .Select(option => $"{option.Action.Id}:{option.IsAvailable}:{option.LockedReason}");
                var secondOptions = secondInteraction.Interaction!.Options
                    .Select(option => $"{option.Action.Id}:{option.IsAvailable}:{option.LockedReason}");
                AssertEqual(string.Join("|", firstOptions), string.Join("|", secondOptions), $"Fixture scenario '{fileName}' replays to the same player-visible options");
            }
        }
    }

    private static void ScenarioPlaybackStepsSharedCommandsAndNormalDays()
    {
        var scenario = DevelopmentScenarioLoader.LoadFile(Path.Combine(Directory.GetCurrentDirectory(), "tests", "DevelopmentScenarios", "scenario-directional-lead.json"));
        var playback = DevelopmentScenarioPlayback.Create(LoadCatalog(), scenario);

        var firstStep = playback.ExecuteNext();
        AssertTrue(firstStep.Success, "Scenario playback delegates the next authored command to the shared session");
        var originalWorldDay = playback.Session.Game.World.WorldDay;
        var advance = playback.AdvanceDays(2);

        AssertTrue(advance.Success && advance.AdvancedDays == 2, "Scenario playback advances normal end-day/world phases without a WPF rule path");
        AssertEqual(originalWorldDay + 2, playback.Session.Game.World.WorldDay, "Scenario playback advances the shared world's day state");
        AssertTrue(playback.HasManualTimeAdvance, "Manual time advance is marked so an unreplayable ad-hoc run is not exported as scripted");
        AssertEqual(3, playback.Session.CommandHistory.Count, "Scenario command and both time advances are recorded by the shared session");
    }

    private static void ScenarioPlaybackRunsEveryProofScenario()
    {
        var scenarioDirectory = Path.Combine(Directory.GetCurrentDirectory(), "tests", "DevelopmentScenarios");
        foreach (var path in Directory.GetFiles(scenarioDirectory, "scenario-*.json").OrderBy(Path.GetFileName))
        {
            var fileName = Path.GetFileName(path);
            var scenario = DevelopmentScenarioLoader.LoadFile(path);
            var result = DevelopmentScenarioPlayback.Create(LoadCatalog(), scenario).RunToCompletion();
            AssertTrue(result.Success, $"The presentation-neutral playback used by WPF runs proof scenario '{fileName}': {result.FailureSummary}");
        }
    }

    private static void InteractiveLocationCommandsRemainOptionalToTheScript()
    {
        var scenario = DevelopmentScenarioLoader.LoadFile(Path.Combine(Directory.GetCurrentDirectory(), "tests", "DevelopmentScenarios", "scenario-claimed-crossing.json"));
        var playback = DevelopmentScenarioPlayback.Create(LoadCatalog(), scenario);

        var inspect = playback.InspectLocation(new HexCoord(2, 1));
        var scout = playback.ScoutLocationSurroundings("location-route-proof", new[] { "scout-1" });
        var rebuild = playback.ResolveLocationAction("location-route-proof", "action-rebuild-bridge");

        AssertTrue(inspect.Success, "Interactive playback can inspect a known location without consuming a scripted command");
        AssertTrue(inspect.Message.Contains("eingestuerzte Handelsbruecke"), "Inspection uses the shared content-profile description instead of the generic archive fallback");
        AssertTrue(scout.Success && scout.Report != null, "Interactive playback can resolve a selected local scout search immediately without consuming a scripted command");
        AssertEqual(2, scout.MovementPointCost, "Interactive local scout search spends its JSON-defined movement cost");
        AssertEqual(1, playback.Session.Game.World.WorldDay, "Interactive local scout search does not advance the day");
        AssertTrue(rebuild.Success, "Interactive playback can resolve a selected shared location option without consuming a scripted command");
        AssertEqual(0, playback.NextCommandIndex, "Direct inspector commands do not force the next scenario script command");
        AssertTrue(playback.HasInteractiveCommands, "Direct inspector commands are marked so a script-only run record cannot misrepresent the path");
    }

    private static void TeamPreviewUsesTheSharedOptionAvailabilityRulesWithoutChangingTheSession()
    {
        var scenario = DevelopmentScenarioLoader.LoadFile(Path.Combine(Directory.GetCurrentDirectory(), "tests", "DevelopmentScenarios", "scenario-claimed-crossing.json"));
        var playback = DevelopmentScenarioPlayback.Create(LoadCatalog(), scenario);
        var active = playback.Session.Game.Expedition;
        var scoutOnlyPreview = new ExpeditionState(
            active.ExpeditionNumber,
            active.Position,
            active.Members.Where(member => member.Role == ExpeditionMemberRole.Scout),
            active.ExpeditionDay,
            active.MovementPoints,
            active.MaxMovementPoints,
            active.Supplies,
            active.Medicine,
            active.Morale,
            active.Capacity,
            active.Status,
            active.UnsecuredKnowledge);

        var interaction = playback.Session.GetLocationInteraction("location-route-proof", scoutOnlyPreview);
        var rebuild = interaction.Interaction!.Options.Single(option => option.Action.Id == "action-rebuild-bridge");

        AssertTrue(interaction.Success, "A hypothetical team uses the normal shared location option query");
        AssertFalse(rebuild.IsAvailable, "Bridge repair is unavailable in the team preview without an engineer");
        AssertEqual(2, playback.Session.Game.Expedition.Members.Count, "Read-only team preview does not change the active expedition");
    }

    private static GameDataCatalog LoadCatalog()
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "UnityHexMapView", "Assets", "StreamingAssets", "GameData");
        return GameDataCatalog.LoadFromDirectory(root) ?? throw new InvalidOperationException("Game data was not loaded.");
    }

    private static WorldGenerationRequest Request() => new() { Seed = 41027, Width = 24, Height = 18, FactionCount = 3 };

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
    }

    private static void AssertTrue(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    private static void AssertFalse(bool value, string message)
    {
        if (value) throw new InvalidOperationException(message);
    }
}
