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
        InteractiveDirectionalScoutUsesSharedMissionLifecycle();
        InteractiveDirectionalMovementUsesSharedAdjacentMoveCommand();
        InteractiveCompletionExposesTheSharedBaseReturnScene();
        DeeperLocationWorkConsumesDailyMovementCapacity();
        DayOperationsConsumeTheRemainingDayCapacity();
        InspectionUsesObservableContextWithoutRevealingUnknownFaction();
        InspectionNamesOnlyAnAlreadyKnownFaction();
        InspectionDoesNotRevealAnUnobservableFactionRelation();
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
        AssertTrue(inspect.Scene?.Paragraphs.SelectMany(paragraph => paragraph.FragmentIds).Contains("frag-open-broken-bridge") == true,
            "Inspection uses the shared localized scene opening instead of the generic archive fallback");
        AssertTrue(scout.Success && scout.Report != null, "Interactive playback can resolve a selected local scout search immediately without consuming a scripted command");
        AssertEqual(2, scout.MovementPointCost, "Interactive local scout search spends its JSON-defined movement cost");
        AssertEqual(1, playback.Session.Game.World.WorldDay, "Interactive local scout search does not advance the day");
        AssertTrue(rebuild.Success, "Interactive playback can resolve a selected shared location option without consuming a scripted command");
        AssertEqual(0, playback.NextCommandIndex, "Direct inspector commands do not force the next scenario script command");
        AssertTrue(playback.HasInteractiveCommands, "Direct inspector commands are marked so a script-only run record cannot misrepresent the path");
    }

    private static void InteractiveCompletionExposesTheSharedBaseReturnScene()
    {
        var scenario = new DevelopmentScenario { Id = "interactive-base-return", Seed = 41027 };
        var playback = DevelopmentScenarioPlayback.Create(LoadCatalog(), scenario);

        var result = playback.CompleteExpedition();
        var scene = playback.Session.GetBaseReturnPresentation(result);

        AssertTrue(result.Success, "WPF playback completion delegates to the shared application command");
        AssertTrue(scene != null && scene.Paragraphs.SelectMany(paragraph => paragraph.FragmentIds)
            .Contains("frag-base-return-complete"), "Interactive completion exposes the shared JSON base-return scene");
        AssertTrue(playback.HasInteractiveCommands, "Interactive completion prevents script-only export from misrepresenting the path");
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

    private static void InteractiveDirectionalScoutUsesSharedMissionLifecycle()
    {
        var scenario = DevelopmentScenarioLoader.LoadFile(Path.Combine(Directory.GetCurrentDirectory(), "tests", "DevelopmentScenarios", "scenario-directional-lead.json"));
        var playback = DevelopmentScenarioPlayback.Create(LoadCatalog(), scenario);

        var sent = playback.SendDirectionalScout(
            new[] { "scout-1" }, ScoutDirection.East, 1, ScoutMissionFocus.Ruins, ScoutMissionBehavior.Cautious);

        AssertTrue(sent.Success && sent.Mission != null, "Interactive WPF-facing control delegates to the shared directional mission command");
        AssertEqual(ScoutMissionStatus.Active, sent.Mission!.Status, "Directional mission starts in the shared active state");
        AssertEqual(0, playback.Session.Game.Knowledge.ScoutReports.Count, "Sending a mission does not create an immediate report");

        var advanced = playback.AdvanceDays(1);

        AssertTrue(advanced.Success, "Normal day advancement resolves the interactive directional mission");
        AssertTrue(playback.Session.Game.Knowledge.ScoutReports.Any(report => report.MissionId == sent.Mission.Id),
            "Directional report appears only through the normal mission lifecycle");
        AssertTrue(playback.Session.Game.Knowledge.ScoutReports.All(report => !report.HasExactCoordinates),
            "Interactive directional reports preserve the no-exact-coordinate contract");
    }

    private static void InteractiveDirectionalMovementUsesSharedAdjacentMoveCommand()
    {
        var scenario = DevelopmentScenarioLoader.LoadFile(Path.Combine(Directory.GetCurrentDirectory(), "tests", "DevelopmentScenarios", "scenario-directional-lead.json"));
        var playback = DevelopmentScenarioPlayback.Create(LoadCatalog(), scenario);
        var beforeMovement = playback.Session.Game.Expedition.MovementPoints;

        var moved = playback.MoveExpedition(HexDirection.East);

        AssertTrue(moved.Success, "WPF-facing direction pad delegates to the shared adjacent movement command");
        AssertEqual(new HexCoord(2, 1), playback.Session.Game.Expedition.Position, "Direction pad moves exactly one logical neighboring hex");
        AssertEqual(beforeMovement - moved.Cost, playback.Session.Game.Expedition.MovementPoints, "Direction pad uses normal terrain movement cost");
        AssertEqual(KnowledgeLevel.Confirmed, playback.Session.Game.Knowledge.GetTileKnowledge(new HexCoord(2, 1)),
            "Direction pad uses normal movement knowledge revelation");
        AssertEqual(0, playback.NextCommandIndex, "Interactive movement does not consume or replace the authored script path");
        AssertTrue(playback.HasInteractiveCommands, "Interactive movement prevents a script-only run record from misrepresenting the path");
    }

    private static void DeeperLocationWorkConsumesDailyMovementCapacity()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "tests", "DevelopmentScenarios", "scenario-natural-phenomenon-survey.json");
        var result = new DevelopmentScenarioExecutor().Execute(LoadCatalog(), DevelopmentScenarioLoader.LoadFile(path));
        var interaction = result.Session.GetLocationInteraction("location-peak").Interaction!;
        var map = interaction.Options.Single(option => option.Action.Id == "action-map-surroundings");
        var leave = interaction.Options.Single(option => option.Action.Id == "action-leave");

        AssertTrue(result.Success, "Natural-phenomenon proof path still resolves through shared actions");
        AssertEqual(1, result.Session.Game.Expedition.MovementPoints, "Approach and survey consume three of four daily movement points");
        AssertFalse(map.IsAvailable, "Further fieldwork locks when its authored movement cost exceeds remaining daily capacity");
        AssertTrue(map.LockedReason!.Contains("Bewegungspunkte", StringComparison.Ordinal), "Fieldwork lock explains the known movement-point requirement");
        AssertTrue(leave.Action.Description.Contains("Bereits vorgenommene Untersuchungen", StringComparison.Ordinal), "Generic leave wording remains true after prior intervention");

        AssertTrue(result.Session.EndDay().Success, "Player can explicitly finish the day after fieldwork");
        AssertEqual(4, result.Session.Game.Expedition.MovementPoints, "A new day restores the normal movement capacity");
        AssertTrue(result.Session.GetLocationInteraction("location-peak").Interaction!.FindOption("action-map-surroundings")!.IsAvailable,
            "Persistent location state allows fieldwork to continue on the next day");
    }

    private static void DayOperationsConsumeTheRemainingDayCapacity()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "tests", "DevelopmentScenarios", "scenario-hazard-safe-route.json");
        var scenario = DevelopmentScenarioLoader.LoadFile(path);
        scenario.InitialState!.Members.Add(new DevelopmentScenarioMember { Id = "medic-1", Name = "Tala", Role = "Medic", StarLevel = 1 });
        scenario.Metadata.SelectedTeamMemberIds.Add("medic-1");
        scenario.Commands.RemoveAt(scenario.Commands.Count - 1);
        scenario.Assertions.Clear();
        var playback = DevelopmentScenarioPlayback.Create(LoadCatalog(), scenario);
        while (playback.HasNextCommand) AssertTrue(playback.ExecuteNext().Success, "Hazard setup reaches the assessed state");

        var active = playback.Session.Game.Expedition;
        var zeroCapacityPreview = new ExpeditionState(active.ExpeditionNumber, active.Position, active.Members, active.ExpeditionDay,
            0, active.MaxMovementPoints, active.Supplies, active.Medicine, active.Morale, active.Capacity, active.Status, active.UnsecuredKnowledge);
        var locked = playback.Session.GetLocationInteraction("location-hazard", zeroCapacityPreview).Interaction!.FindOption("action-contain-hazard")!;
        AssertFalse(locked.IsAvailable, "A day operation cannot start after daily capacity is exhausted");
        AssertTrue(locked.LockedReason!.Contains("Tag beenden", StringComparison.Ordinal), "Day-operation lock tells the player how to continue");

        var contained = playback.ResolveLocationAction("location-hazard", "action-contain-hazard");
        AssertTrue(contained.Success, "Available specialist day operation resolves through the shared command");
        AssertEqual(0, active.MovementPoints, "Day operation commits every remaining movement point without a UI-specific rule");
        AssertEqual(1, active.ExpeditionDay, "Day operation does not secretly end the day");
    }

    private static void InspectionUsesObservableContextWithoutRevealingUnknownFaction()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "tests", "DevelopmentScenarios", "scenario-sealed-containment.json");
        var playback = DevelopmentScenarioPlayback.Create(LoadCatalog(), DevelopmentScenarioLoader.LoadFile(path));
        var result = playback.InspectLocation(new HexCoord(2, 1));

        AssertTrue(result.Success, "Profile-backed containment inspection succeeds");
        AssertTrue(result.Scene != null, "Inspection exposes the shared structured scene result");
        AssertTrue(result.Message.Contains("Schwere Riegel", StringComparison.Ordinal), "Variant opening comes from localized scene fragments");
        AssertTrue(result.Message.Contains("weiterhin bewacht", StringComparison.Ordinal), "Observable guarded modifier contributes an authored first impression");
        AssertTrue(result.Message.Contains("Urheber lassen sich noch nicht bestimmen", StringComparison.Ordinal), "Unknown claimant remains a general observed relation");
        AssertFalse(result.Message.Contains("Unknown Keepers", StringComparison.Ordinal), "Objective faction identity is not exposed before contact knowledge exists");
        AssertFalse(result.Message.Contains("modifier-", StringComparison.Ordinal), "Player-facing inspection never exposes technical modifier ids");

        var restored = KnowledgeRuntimeSnapshotSerializer.Deserialize(KnowledgeRuntimeSnapshotSerializer.Serialize(playback.Session.Game.Knowledge));
        AssertTrue(restored.KnowsLocationContextTag("location-containment-proof", "inspection-modifier:modifier-guarded"), "Observed modifier impression persists in KnowledgeState");
        AssertTrue(restored.KnowsLocationContextTag("location-containment-proof", "inspection-relation:guarded"), "Observed anonymous relation persists without WorldState lookup");
        AssertFalse(restored.KnownLocationContextTags("location-containment-proof").Any(tag => tag.StartsWith("inspection-faction:", StringComparison.Ordinal)),
            "Unknown faction identity is absent from saved player knowledge");
    }

    private static void InspectionNamesOnlyAnAlreadyKnownFaction()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "tests", "DevelopmentScenarios", "scenario-sealed-containment.json");
        var playback = DevelopmentScenarioPlayback.Create(LoadCatalog(), DevelopmentScenarioLoader.LoadFile(path));
        playback.Session.Game.FindFaction("faction-1")!.SetContactStatus(FactionContactStatus.Contacted);

        var result = playback.InspectLocation(new HexCoord(2, 1));

        AssertTrue(result.Success && result.Message.Contains("Unknown Keepers", StringComparison.Ordinal), "An already known faction may be named in the observed guarded relation");
        AssertTrue(playback.Session.Game.Knowledge.KnowsLocationContextTag("location-containment-proof", "inspection-faction:faction-1"),
            "Earned claimant identification is recorded separately from the objective relation");
    }

    private static void InspectionDoesNotRevealAnUnobservableFactionRelation()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "tests", "DevelopmentScenarios", "scenario-sealed-containment.json");
        var scenario = DevelopmentScenarioLoader.LoadFile(path);
        scenario.InitialState!.Locations[0].ModifierIds.Remove("modifier-guarded");
        var playback = DevelopmentScenarioPlayback.Create(LoadCatalog(), scenario);

        var result = playback.InspectLocation(new HexCoord(2, 1));

        AssertTrue(result.Success, "Location inspection remains possible without a visible faction modifier");
        AssertFalse(result.Message.Contains("Urheber lassen sich noch nicht bestimmen", StringComparison.Ordinal), "Objective guarded relation is not even anonymously revealed without observable signs");
        AssertFalse(playback.Session.Game.Knowledge.KnownLocationContextTags("location-containment-proof").Any(tag => tag.StartsWith("inspection-relation:", StringComparison.Ordinal)),
            "Unobservable objective relation is absent from KnowledgeState");
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
