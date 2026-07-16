using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Game.App;
using Game.Core;
using Microsoft.Win32;

namespace Game.Simulation.Wpf;

public partial class MainWindow : Window
{
    private GameDataCatalog? catalog;
    private DevelopmentScenarioPlayback? playback;
    private IReadOnlyList<TestTeamMemberEntry> testTeamPool = Array.Empty<TestTeamMemberEntry>();
    private readonly HashSet<string> selectedTestTeamMemberIds = new(StringComparer.Ordinal);
    private bool testTeamSelectionInitialized;
    private bool isRefreshingTestTeamSelection;

    public MainWindow()
    {
        InitializeComponent();
        ScoutDirectionSelector.ItemsSource = Enum.GetValues<ScoutDirection>();
        ScoutDurationSelector.ItemsSource = Enumerable.Range(1, 5).ToArray();
        ScoutFocusSelector.ItemsSource = Enum.GetValues<ScoutMissionFocus>();
        ScoutBehaviorSelector.ItemsSource = Enum.GetValues<ScoutMissionBehavior>();
        ScoutDirectionSelector.SelectedItem = ScoutDirection.North;
        ScoutDurationSelector.SelectedItem = 1;
        ScoutFocusSelector.SelectedItem = ScoutMissionFocus.Survey;
        ScoutBehaviorSelector.SelectedItem = ScoutMissionBehavior.Cautious;
        TryInitializeCatalogAndScenarios();
    }

    private void TryInitializeCatalogAndScenarios()
    {
        try
        {
            catalog = GameDataCatalog.LoadFromDirectory(FindRequiredDirectory("UnityHexMapView", "Assets", "StreamingAssets", "GameData"));
            var scenarios = FindRequiredDirectory("tests", "DevelopmentScenarios");
            ScenarioSelector.ItemsSource = Directory.GetFiles(scenarios, "*.json")
                .OrderBy(Path.GetFileName)
                .Select(path => new ScenarioEntry(path));
            ScenarioSelector.SelectedIndex = 0;
            SessionStatus.Text = "Bereit. Ein Szenario laden, um dieselbe Anwendungssimulation zu untersuchen wie Unity.";
        }
        catch (Exception error)
        {
            SessionStatus.Text = $"Initialisierung fehlgeschlagen: {error.Message}";
        }
    }

    private void LoadSelectedScenario(object sender, RoutedEventArgs e)
    {
        if (ScenarioSelector.SelectedItem is ScenarioEntry entry) LoadScenario(entry.Path);
    }

    private void LoadScenarioFile(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Development scenarios (*.json)|*.json|All files (*.*)|*.*" };
        if (dialog.ShowDialog(this) == true) LoadScenario(dialog.FileName);
    }

    private void LoadScenario(string path)
    {
        try
        {
            EnsureCatalog();
            var scenario = DevelopmentScenarioLoader.LoadFile(path);
            playback = DevelopmentScenarioPlayback.Create(catalog!, scenario);
            ResetTestTeamSelection();
            ShowScenarioMetadata(scenario);
            SessionStatus.Text = $"Szenario '{scenario.Id}' geladen. Seed {scenario.Seed}; {scenario.Commands.Count} Skriptbefehle.";
            RefreshInspector();
        }
        catch (Exception error)
        {
            SessionStatus.Text = $"Szenario konnte nicht geladen werden: {error.Message}";
        }
    }

    private void LoadRunRecord(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Simulation run records (*.json)|*.json|All files (*.*)|*.*" };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            EnsureCatalog();
            var record = JsonSerializer.Deserialize<DevelopmentScenarioRunRecord>(File.ReadAllText(dialog.FileName), JsonOptions)
                ?? throw new InvalidOperationException("Der Run-Record ist leer.");
            var result = record.Replay(catalog!);
            playback = DevelopmentScenarioPlayback.Create(catalog!, result.Scenario);
            playback.RunToCompletion();
            ResetTestTeamSelection();
            ShowScenarioMetadata(result.Scenario);
            SessionStatus.Text = $"Run-Record '{result.ScenarioId}' reproduziert: {(result.Success ? "erfolgreich" : result.FailureSummary)}";
            RefreshInspector();
        }
        catch (Exception error)
        {
            SessionStatus.Text = $"Run-Record konnte nicht geladen werden: {error.Message}";
        }
    }

    private void ExecuteNextCommand(object sender, RoutedEventArgs e)
    {
        if (!TryGetPlayback(out var current)) return;
        var result = current.ExecuteNext();
        SessionStatus.Text = result.Success
            ? $"Skriptbefehl {result.CommandIndex + 1} ({result.Command!.Kind}) ausgeführt."
            : $"Skriptbefehl konnte nicht ausgeführt werden: {result.Error}";
        RefreshInspector();
    }

    private void RunRemainingCommands(object sender, RoutedEventArgs e)
    {
        if (!TryGetPlayback(out var current)) return;
        var result = current.RunToCompletion();
        SessionStatus.Text = result.Success
            ? $"Szenario '{result.ScenarioId}' abgeschlossen."
            : result.FailureSummary;
        RefreshInspector();
    }

    private void AdvanceOneDay(object sender, RoutedEventArgs e) => AdvanceDays(1);

    private void AdvanceSpecifiedDays(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(FastForwardDays.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var days) || days < 1 || days > 365)
        {
            SessionStatus.Text = "Bitte 1 bis 365 Tage angeben.";
            return;
        }

        AdvanceDays(days);
    }

    private void AdvanceDays(int days)
    {
        if (!TryGetPlayback(out var current)) return;
        var result = current.AdvanceDays(days);
        SessionStatus.Text = result.Success
            ? $"{result.AdvancedDays} Tag(e) über die normale Tages- und Weltphase fortgeschrieben."
            : $"Fortschreiben nach {result.AdvancedDays} Tag(en) beendet: {result.Error}";
        RefreshInspector();
    }

    private void SaveScriptedRun(object sender, RoutedEventArgs e)
    {
        if (!TryGetPlayback(out var current)) return;
        if (current.HasInteractiveCommands)
        {
            SessionStatus.Text = "Der Lauf enthält direkt ausgeführte Befehle. Für einen reproduzierbaren Script-Record müssen diese Entscheidungen als Szenariobefehle festgehalten werden.";
            return;
        }

        var result = current.RunToCompletion();
        var dialog = new SaveFileDialog
        {
            Filter = "Simulation run record (*.json)|*.json",
            FileName = $"{result.ScenarioId}-run.json"
        };
        if (dialog.ShowDialog(this) != true) return;

        File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(result.CreateRunRecord(), JsonOptionsIndented));
        SessionStatus.Text = $"Reproduzierbarer Skript-Run gespeichert: {dialog.FileName}";
        RefreshInspector();
    }

    private void RefreshInspector()
    {
        if (playback == null) return;
        var game = playback.Session.Game;
        PlayerStateList.ItemsSource = BuildPlayerState(game);
        PlayerReportsList.ItemsSource = BuildPlayerReports(game);
        var selectedCommandId = (PlayerOptionsList.SelectedItem as LocationCommandEntry)?.Id;
        var locationCommands = BuildLocationCommands(playback.Session, game);
        PlayerOptionsList.ItemsSource = locationCommands;
        PlayerOptionsList.SelectedItem = locationCommands.FirstOrDefault(entry => entry.Id == selectedCommandId) ?? locationCommands.FirstOrDefault();
        var selectedScoutId = (AvailableScoutSelector.SelectedItem as AvailableScoutEntry)?.Id;
        var scouts = BuildAvailableScouts(game);
        AvailableScoutSelector.ItemsSource = scouts;
        AvailableScoutSelector.SelectedItem = scouts.FirstOrDefault(entry => entry.Id == selectedScoutId) ?? scouts.FirstOrDefault();
        RefreshDirectionalScouts(game, scouts);
        MovementStateText.Text = $"Position: {game.Expedition.Position} · Bewegungspunkte: {game.Expedition.MovementPoints}/{game.Expedition.MaxMovementPoints} · Status: {game.Expedition.Status}";
        WorldLocationsList.ItemsSource = BuildWorldLocations(game);
        WorldFactionsList.ItemsSource = BuildWorldFactions(game);
        WorldProcessesList.ItemsSource = BuildWorldProcesses(game);
        CausalityList.ItemsSource = BuildCausality(game);
        RefreshTestTeam(game);
        UpdateLocationCommandSelection();
    }

    private void MoveExpeditionDirection(object sender, RoutedEventArgs e)
    {
        if (!TryGetPlayback(out var current)) return;
        if (sender is not Button { Tag: string directionText } ||
            !Enum.TryParse<HexDirection>(directionText, out var direction))
        {
            MovementResultText.Text = "Die gewählte Hex-Richtung ist ungültig.";
            return;
        }

        var result = current.MoveExpedition(direction);
        MovementResultText.Text = result.Success
            ? $"Bewegt: {direction}. Normale Bewegungskosten: {result.Cost}."
            : $"Bewegung {direction} abgelehnt: {result.Error}";
        SessionStatus.Text = result.Success
            ? $"Expedition über den gemeinsamen Bewegungsbefehl nach {direction} bewegt. Kosten: {result.Cost}."
            : result.Error ?? "Bewegung abgelehnt.";
        RefreshInspector();
    }

    private void RefreshDirectionalScouts(GameState game, IReadOnlyList<AvailableScoutEntry> availableScouts)
    {
        var selectedIds = DirectionalScoutList.SelectedItems.OfType<AvailableScoutEntry>()
            .Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        DirectionalScoutList.ItemsSource = availableScouts;
        foreach (var scout in availableScouts.Where(item => selectedIds.Contains(item.Id)).Take(2))
        {
            DirectionalScoutList.SelectedItems.Add(scout);
        }

        DirectionalMissionList.ItemsSource = game.Expedition.ScoutMissions
            .Where(mission => mission.MissionTypeId == "directional-recon")
            .Select(mission => $"{string.Join(" + ", mission.ScoutMemberIds.Select(id => MemberName(game, id)))} | {mission.Direction}, {mission.DurationDays} Tag(e), {mission.Focus}, {mission.Behavior} | Rückkehr Tag {mission.ExpectedReturnWorldDay} | {mission.Status}")
            .ToList();
        var directionalMissionIds = game.Expedition.ScoutMissions
            .Where(mission => mission.MissionTypeId == "directional-recon")
            .Select(mission => mission.Id).ToHashSet(StringComparer.Ordinal);
        DirectionalReportList.ItemsSource = game.Knowledge.ScoutReports
            .Where(report => directionalMissionIds.Contains(report.MissionId))
            .Select(report => $"{report.Title} | Verlässlichkeit {report.Reliability}% | {string.Join("; ", report.Leads.Select(lead => $"{lead.Direction}/{lead.Scope}: {lead.Summary} ({lead.Confidence}%)"))}")
            .ToList();
        UpdateDirectionalScoutOrder();
    }

    private static string MemberName(GameState game, string memberId) =>
        game.Expedition.Members.FirstOrDefault(member => member.Id == memberId)?.Name ?? memberId;

    private void DirectionalScoutSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DirectionalScoutList.SelectedItems.Count <= 2)
        {
            UpdateDirectionalScoutOrder();
            return;
        }

        foreach (var added in e.AddedItems.Cast<object>().ToList()) DirectionalScoutList.SelectedItems.Remove(added);
        SessionStatus.Text = "Eine Richtungsmission kann höchstens zwei Scouts entsenden.";
        UpdateDirectionalScoutOrder();
    }

    private void DirectionalScoutParametersChanged(object sender, SelectionChangedEventArgs e) => UpdateDirectionalScoutOrder();

    private void UpdateDirectionalScoutOrder()
    {
        if (DirectionalScoutOrderSummary == null || DirectionalScoutList == null) return;
        var scouts = DirectionalScoutList.SelectedItems.OfType<AvailableScoutEntry>().ToList();
        var valid = scouts.Count is 1 or 2 &&
                    ScoutDirectionSelector.SelectedItem is ScoutDirection &&
                    ScoutDurationSelector.SelectedItem is int &&
                    ScoutFocusSelector.SelectedItem is ScoutMissionFocus &&
                    ScoutBehaviorSelector.SelectedItem is ScoutMissionBehavior;
        DirectionalScoutOrderSummary.Text = valid
            ? $"{string.Join(" und ", scouts.Select(item => item.DisplayName))} nach {ScoutDirectionSelector.SelectedItem} · {ScoutDurationSelector.SelectedItem} Tag(e) · Fokus {ScoutFocusSelector.SelectedItem} · {ScoutBehaviorSelector.SelectedItem}. Bericht erst nach normalem Tagesfortschritt."
            : "Ein oder zwei freie Scouts und alle Auftragsparameter auswählen.";
        SendDirectionalScoutButton.IsEnabled = valid;
    }

    private void SendDirectionalScout(object sender, RoutedEventArgs e)
    {
        if (!TryGetPlayback(out var current)) return;
        var scouts = DirectionalScoutList.SelectedItems.OfType<AvailableScoutEntry>().Select(item => item.Id).ToList();
        if (scouts.Count is < 1 or > 2 ||
            ScoutDirectionSelector.SelectedItem is not ScoutDirection direction ||
            ScoutDurationSelector.SelectedItem is not int duration ||
            ScoutFocusSelector.SelectedItem is not ScoutMissionFocus focus ||
            ScoutBehaviorSelector.SelectedItem is not ScoutMissionBehavior behavior)
        {
            SessionStatus.Text = "Der Richtungsauftrag ist unvollständig.";
            return;
        }

        var result = current.SendDirectionalScout(scouts, direction, duration, focus, behavior);
        SessionStatus.Text = result.Success
            ? $"Richtungsmission '{result.Mission!.Id}' entsandt. Erwartete Rückkehr: Welttag {result.Mission.ExpectedReturnWorldDay}."
            : result.Error ?? "Richtungsmission konnte nicht entsandt werden.";
        RefreshInspector();
    }

    private void ShowScenarioMetadata(DevelopmentScenario scenario)
    {
        var metadata = scenario.Metadata;
        ScenarioPurpose.Text = string.IsNullOrWhiteSpace(metadata.Purpose) ? "Kein Zweck beschrieben." : metadata.Purpose;
        ScenarioStartingKnowledge.Text = metadata.StartingKnowledge.Count == 0
            ? "Startwissen: keines ausgewiesen."
            : $"Startwissen: {string.Join("; ", metadata.StartingKnowledge)}";
        var names = scenario.InitialState?.Members
            .Where(member => metadata.SelectedTeamMemberIds.Contains(member.Id, StringComparer.Ordinal))
            .Select(member => $"{member.Name} ({member.Role})") ?? Enumerable.Empty<string>();
        ScenarioSelectedTeam.Text = names.Any() ? string.Join(", ", names) : "Kein Testteam ausgewiesen.";
        ScenarioExpectedProcess.Text = string.IsNullOrWhiteSpace(metadata.ExpectedWorldProcess)
            ? "Erwarteter Prozess: keiner."
            : $"Erwarteter Prozess: {metadata.ExpectedWorldProcess}";
        ScenarioDecisionPoint.Text = string.IsNullOrWhiteSpace(metadata.OutstandingDecisionPoint)
            ? "Offene Entscheidung: keine beschrieben."
            : $"Offene Entscheidung: {metadata.OutstandingDecisionPoint}";
    }

    private void TestTeamSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isRefreshingTestTeamSelection || playback == null) return;
        selectedTestTeamMemberIds.Clear();
        foreach (var member in TestTeamMemberList.SelectedItems.OfType<TestTeamMemberEntry>())
        {
            selectedTestTeamMemberIds.Add(member.Id);
        }

        RefreshTestTeamPreview(playback.Session.Game);
    }

    private void SelectAllAvailableTestMembers(object sender, RoutedEventArgs e)
    {
        SetTestTeamSelection(testTeamPool.Where(member => member.IsAvailable).Select(member => member.Id));
    }

    private void SelectActiveExpeditionTestMembers(object sender, RoutedEventArgs e)
    {
        if (playback == null) return;
        SetTestTeamSelection(playback.Session.Game.Expedition.Members.Select(member => member.Id));
    }

    private void ResetTestTeamSelection()
    {
        testTeamSelectionInitialized = false;
        selectedTestTeamMemberIds.Clear();
        testTeamPool = Array.Empty<TestTeamMemberEntry>();
    }

    private void RefreshTestTeam(GameState game)
    {
        testTeamPool = BuildTestTeamPool(game);
        if (!testTeamSelectionInitialized)
        {
            selectedTestTeamMemberIds.Clear();
            foreach (var member in game.Expedition.Members)
            {
                selectedTestTeamMemberIds.Add(member.Id);
            }
            testTeamSelectionInitialized = true;
        }
        else
        {
            selectedTestTeamMemberIds.RemoveWhere(id => testTeamPool.All(member => member.Id != id));
        }

        isRefreshingTestTeamSelection = true;
        TestTeamMemberList.ItemsSource = testTeamPool;
        TestTeamMemberList.SelectedItems.Clear();
        foreach (var member in testTeamPool.Where(member => selectedTestTeamMemberIds.Contains(member.Id)))
        {
            TestTeamMemberList.SelectedItems.Add(member);
        }
        isRefreshingTestTeamSelection = false;
        RefreshTestTeamPreview(game);
    }

    private void SetTestTeamSelection(IEnumerable<string> memberIds)
    {
        if (playback == null) return;
        selectedTestTeamMemberIds.Clear();
        foreach (var memberId in memberIds)
        {
            selectedTestTeamMemberIds.Add(memberId);
        }
        RefreshTestTeam(playback.Session.Game);
    }

    private void RefreshTestTeamPreview(GameState game)
    {
        var selectedMembers = testTeamPool
            .Where(member => member.IsAvailable && selectedTestTeamMemberIds.Contains(member.Id))
            .ToList();
        var unavailableSelected = testTeamPool
            .Where(member => !member.IsAvailable && selectedTestTeamMemberIds.Contains(member.Id))
            .ToList();
        var roleSummary = selectedMembers.Count == 0
            ? "Keine einsatzbereite Person ausgewählt."
            : string.Join(", ", selectedMembers.GroupBy(member => member.Role).Select(group => $"{group.Key}: {group.Count()}"));
        TestTeamSummary.Text = $"{selectedMembers.Count} einsatzbereite Person(en): {roleSummary}."
            + (unavailableSelected.Count == 0 ? string.Empty : $" Nicht eingesetzt: {string.Join(", ", unavailableSelected.Select(member => member.DisplayName))}.");

        if (playback == null)
        {
            TestTeamOptionsList.ItemsSource = Array.Empty<TestTeamOptionEntry>();
            return;
        }

        var previewExpedition = BuildPreviewExpedition(game, selectedMembers);
        TestTeamOptionsList.ItemsSource = BuildTestTeamOptions(playback.Session, game, previewExpedition);
    }

    private void PlayerOptionsSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateLocationCommandSelection();
    }

    private void UpdateLocationCommandSelection()
    {
        if (PlayerOptionsList.SelectedItem is not LocationCommandEntry entry)
        {
            SelectedLocationCommandDetails.Text = "Einen Ortsbefehl auswählen. Die optionalen Skriptbefehle werden dadurch nicht ausgelöst.";
            ExecuteLocationCommandButton.IsEnabled = false;
            return;
        }

        SelectedLocationCommandDetails.Text = entry.Details;
        ExecuteLocationCommandButton.IsEnabled = entry.IsAvailable;
        ExecuteLocationCommandButton.Content = entry.ExecuteLabel;
    }

    private void ExecuteSelectedLocationCommand(object sender, RoutedEventArgs e)
    {
        if (!TryGetPlayback(out var current)) return;
        if (PlayerOptionsList.SelectedItem is not LocationCommandEntry entry)
        {
            SessionStatus.Text = "Zuerst einen Ortsbefehl auswählen.";
            return;
        }

        if (!entry.IsAvailable)
        {
            SessionStatus.Text = entry.LockedReason ?? "Dieser Ortsbefehl ist derzeit nicht verfügbar.";
            return;
        }

        switch (entry.Kind)
        {
            case LocationCommandKind.Inspect:
            {
                var result = current.InspectLocation(entry.Coord);
                SessionStatus.Text = result.Success ? $"{entry.LocationName}: {result.Message}" : result.Error ?? "Betrachten fehlgeschlagen.";
                break;
            }
            case LocationCommandKind.ScoutSurroundings:
            {
                if (AvailableScoutSelector.SelectedItem is not AvailableScoutEntry scout)
                {
                    SessionStatus.Text = "Für die Umgebungssuche muss ein freier Scout gewählt werden.";
                    return;
                }

                var result = current.ScoutLocationSurroundings(entry.LocationId, new[] { scout.Id });
                SessionStatus.Text = result.Success
                    ? $"{scout.DisplayName} untersucht die Umgebung von {entry.LocationName}. (-{result.MovementPointCost} Bewegung) {result.Report?.Body}"
                    : result.Error ?? "Umgebungssuche fehlgeschlagen.";
                break;
            }
            case LocationCommandKind.LocationAction:
            {
                var result = current.ResolveLocationAction(entry.LocationId, entry.ActionId!);
                SessionStatus.Text = result.Success
                    ? $"{entry.LocationName}: {result.OutcomeLabel ?? "Aktion ausgeführt."} {string.Join(" ", result.EffectTexts)}"
                    : result.Error ?? "Ortsaktion fehlgeschlagen.";
                break;
            }
            case LocationCommandKind.AdvanceProject:
            {
                var result = current.AdvanceLocationProject(entry.LocationId);
                SessionStatus.Text = result.Success
                    ? $"{entry.LocationName}: Projektfortschritt ausgeführt. {string.Join(" ", result.EffectTexts)}"
                    : result.Error ?? "Projekt konnte nicht fortgesetzt werden.";
                break;
            }
            default:
                SessionStatus.Text = "Unbekannter Ortsbefehl.";
                return;
        }

        RefreshInspector();
    }

    private static IReadOnlyList<string> BuildPlayerState(GameState game)
    {
        var expedition = game.Expedition;
        var lines = new List<string>
        {
            $"Welt-/Expeditionstag: {game.World.WorldDay} / {expedition.ExpeditionDay}",
            $"Position: {expedition.Position}; Bewegung: {expedition.MovementPoints}/{expedition.MaxMovementPoints}",
            $"Vorräte: {expedition.Supplies}; Medizin: {expedition.Medicine}; Moral: {expedition.Morale}",
            $"Bekannte Felder: {game.Knowledge.KnownTiles.Count}; Belege: {game.Knowledge.Evidence.Count}; Berichte: {game.Knowledge.ScoutReports.Count}"
        };
        lines.AddRange(game.Knowledge.Evidence.Select(item => $"Beleg [{item.KnowledgeState}, {item.Confidence}%]: {item.PlayerText}"));
        return lines;
    }

    private static IReadOnlyList<string> BuildPlayerReports(GameState game)
    {
        var lines = new List<string>();
        foreach (var report in game.Knowledge.ScoutReports)
        {
            lines.Add($"Bericht [{report.Reliability}%] {report.Title}: {report.Body}");
            lines.AddRange(report.Leads.Select(lead => $"  Hinweis [{lead.Scope}, {lead.Direction}, {lead.Confidence}%]: {lead.Summary}"));
            lines.AddRange(report.Hints.Select(hint => $"  Notiz: {hint}"));
        }

        return lines.Count == 0 ? new[] { "Noch keine Scout-Berichte." } : lines;
    }

    private static IReadOnlyList<LocationCommandEntry> BuildLocationCommands(SimulationSession session, GameState game)
    {
        var commands = new List<LocationCommandEntry>();
        var freeScoutCount = BuildAvailableScouts(game).Count;
        var localScoutMovementCost = session.Application.DataCatalog?.CrossSystem.ScoutContent
            .FindMissionType("location-surroundings")?.MovementPointCost ?? 2;
        foreach (var location in game.World.Locations.Where(location => location.Anchor.Coords.Any(coord => game.Knowledge.GetTileKnowledge(coord) == KnowledgeLevel.Confirmed)))
        {
            commands.Add(new LocationCommandEntry(
                $"{location.Id}:inspect",
                location.Id,
                location.Name,
                location.Coord,
                LocationCommandKind.Inspect,
                actionId: null,
                label: "Betrachten",
                details: "Untersucht den Ort direkt. Dies kann lokale Fakten, Funde und neue Optionen sichtbar machen.",
                isAvailable: !location.IsInspected,
                lockedReason: location.IsInspected ? "Dieser Ort wurde bereits betrachtet." : null));

            var nearLocation = location.Anchor.Coords.Any(coord => coord == game.Expedition.Position || coord.DistanceTo(game.Expedition.Position) == 1);
            var scoutReason = !nearLocation
                ? "Die Expedition muss am Ort oder neben ihm stehen."
                : freeScoutCount == 0 ? "Kein freier Scout ist verfügbar."
                : game.Expedition.MovementPoints < localScoutMovementCost
                    ? $"Nicht genug Bewegungspunkte (benötigt: {localScoutMovementCost})."
                    : null;
            commands.Add(new LocationCommandEntry(
                $"{location.Id}:surroundings",
                location.Id,
                location.Name,
                location.Coord,
                LocationCommandKind.ScoutSurroundings,
                actionId: null,
                label: "Umgebung absuchen",
                details: $"Lässt einen gewählten freien Scout die unmittelbare Umgebung absuchen. Der Bericht liegt sofort vor und kostet {localScoutMovementCost} Bewegungspunkte; der Tag endet dadurch nicht.",
                isAvailable: scoutReason == null,
                lockedReason: scoutReason));

            var query = session.GetLocationInteraction(location.Id);
            if (!query.Success || query.Interaction == null) continue;
            commands.AddRange(query.Interaction.Options.Select(option => new LocationCommandEntry(
                $"{location.Id}:action:{option.Action.Id}",
                location.Id,
                location.Name,
                location.Coord,
                LocationCommandKind.LocationAction,
                option.Action.Id,
                option.Action.Label,
                $"{option.Action.Description} Kosten: {LocationActionCostText(option.Action)}. Risiko: {option.RiskBand} ({option.Confidence}). Bindung: {option.Commitment}.",
                option.IsAvailable,
                option.LockedReason)));

            if (location.ActiveProject != null)
            {
                var project = location.ActiveProject;
                commands.Add(new LocationCommandEntry(
                    $"{location.Id}:project:{project.ActionId}",
                    location.Id,
                    location.Name,
                    location.Coord,
                    LocationCommandKind.AdvanceProject,
                    project.ActionId,
                    "Projekt fortsetzen",
                    $"Projekt '{project.ActionId}': Fortschritt {project.Progress}/{project.RequiredProgress}.",
                    !project.IsComplete,
                    project.IsComplete ? "Das Projekt ist bereits abgeschlossen." : null));
            }
        }

        return commands;
    }

    private static string LocationActionCostText(LocationActionDefinition action)
    {
        var parts = action.Costs.Select(cost => $"{cost.Amount} {cost.Kind}").ToList();
        if (action.Commitment == LocationActionCommitment.DayOperation)
        {
            parts.Add("restliche Tageskapazitaet");
        }
        else if (action.StartsProject)
        {
            parts.Add($"{action.ProjectDurationDays} Projekttage");
        }

        return parts.Count == 0 ? "keine Tageskapazitaet" : string.Join(", ", parts);
    }

    private static IReadOnlyList<AvailableScoutEntry> BuildAvailableScouts(GameState game) => game.Expedition.Members
        .Where(member => member.Role == ExpeditionMemberRole.Scout && member.Status == ExpeditionMemberStatus.Available)
        .Select(member => new AvailableScoutEntry(member.Id, $"{member.Name} ({member.StarLevel} Sterne)"))
        .ToList();

    private static IReadOnlyList<TestTeamMemberEntry> BuildTestTeamPool(GameState game)
    {
        var pool = new Dictionary<string, TestTeamMemberEntry>(StringComparer.Ordinal);
        foreach (var member in game.Roster.Members)
        {
            pool[member.Id] = new TestTeamMemberEntry(member.Id, member.Name, member.Role, member.Status, starLevel: 0, "Basisroster");
        }

        foreach (var member in game.Expedition.Members)
        {
            pool[member.Id] = new TestTeamMemberEntry(member.Id, member.Name, member.Role, member.Status, member.StarLevel, "Aktive Expedition");
        }

        return pool.Values
            .OrderBy(member => member.Role)
            .ThenBy(member => member.DisplayName, StringComparer.Ordinal)
            .ToList();
    }

    private static ExpeditionState BuildPreviewExpedition(GameState game, IReadOnlyList<TestTeamMemberEntry> selectedMembers)
    {
        var expedition = game.Expedition;
        return new ExpeditionState(
            expedition.ExpeditionNumber,
            expedition.Position,
            selectedMembers.Select(member => new ExpeditionMemberState(member.Id, member.Name, member.Role, ExpeditionMemberStatus.Available, member.StarLevel)),
            expedition.ExpeditionDay,
            expedition.MovementPoints,
            expedition.MaxMovementPoints,
            expedition.Supplies,
            expedition.Medicine,
            expedition.Morale,
            expedition.Capacity,
            expedition.Status,
            expedition.UnsecuredKnowledge);
    }

    private static IReadOnlyList<TestTeamOptionEntry> BuildTestTeamOptions(
        SimulationSession session,
        GameState game,
        ExpeditionState previewExpedition)
    {
        var options = new List<TestTeamOptionEntry>();
        foreach (var location in game.World.Locations.Where(location => location.Anchor.Coords.Any(coord => game.Knowledge.GetTileKnowledge(coord) == KnowledgeLevel.Confirmed)))
        {
            var query = session.GetLocationInteraction(location.Id, previewExpedition);
            if (!query.Success || query.Interaction == null) continue;
            options.AddRange(query.Interaction.Options.Select(option => new TestTeamOptionEntry(
                location.Name,
                option.Action.Label,
                option.IsAvailable,
                option.LockedReason)));
        }

        return options;
    }

    private static IReadOnlyList<string> BuildWorldLocations(GameState game)
    {
        var lines = new List<string>();
        foreach (var location in game.World.Locations)
        {
            var relations = location.FactionRelations.Count == 0
                ? "neutral"
                : string.Join(", ", location.FactionRelations.Select(relation => $"{relation.FactionId}:{relation.Kind}"));
            lines.Add($"{location.Id} | {location.Name} [{location.Kind}] bei {location.Coord}; Zustand {location.OperationalStateId}; Beziehungen: {relations}");
            if (location.ContextTags.Count > 0) lines.Add($"  Kontext: {string.Join(", ", location.ContextTags)}");
        }

        lines.AddRange(game.World.Connections.Select(connection => $"Verbindung {connection.Id}: {connection.SourceId} → {connection.TargetId} [{connection.Kind}] seit Tag {connection.CreatedWorldDay}"));
        return lines;
    }

    private static IReadOnlyList<string> BuildWorldFactions(GameState game)
    {
        var lines = game.Factions.Select(faction =>
            $"{faction.Id} | Kontakt {faction.ContactStatus}; Vertrauen {faction.Trust}, Ärger {faction.Anger}, Furcht {faction.Fear}; Profil {faction.ReactionProfileId}; Zeichen {faction.SignatureProfileId}").ToList();
        lines.AddRange(game.World.FactionAwareness.Select(awareness => $"Beobachtung: {awareness.FactionId} in {awareness.RegionId} = {awareness.Level}"));
        return lines.Count == 0 ? new[] { "Keine Fraktionen oder Beobachtungszustände." } : lines;
    }

    private static IReadOnlyList<string> BuildWorldProcesses(GameState game)
    {
        var lines = game.World.WorldTriggers.Select(trigger =>
            $"Trigger {trigger.Id}: {trigger.TriggerId} | Tag {trigger.RaisedWorldDay} | Quelle {trigger.SourceLocationId ?? trigger.SourceCoord?.ToString() ?? "unbekannt"} | erledigt {trigger.IsResolved}").ToList();
        lines.AddRange(game.World.ScheduledConsequences.Select(consequence =>
            $"Prozess {consequence.Id}: {consequence.DefinitionId}/{consequence.ResolvedBranchId}; Quelle {consequence.SourceLocationId}; nächste Stufe {consequence.CurrentStage?.StageId ?? "abgeschlossen"} an Tag {consequence.DueWorldDay}; Kontexte {string.Join(", ", consequence.AffectedContextIds)}"));
        lines.AddRange(game.World.Situations.Select(situation =>
            $"Situation {situation.Id}: {situation.DefinitionId} = {situation.Status}; Frist {situation.DueWorldDay?.ToString() ?? "keine"}; Fraktion {situation.FactionId ?? "keine"}"));
        return lines.Count == 0 ? new[] { "Keine aktiven Trigger, Prozesse oder Situationen." } : lines;
    }

    private static IReadOnlyList<string> BuildCausality(GameState game)
    {
        var lines = game.World.Traces.Select(trace =>
        {
            var causes = trace.CausedByTraceIds.Count == 0 ? "Ausgang" : string.Join(", ", trace.CausedByTraceIds);
            var subjects = trace.SubjectIds.Count == 0 ? string.Empty : $" | betrifft: {string.Join(", ", trace.SubjectIds)}";
            return $"Tag {trace.WorldDay} | {trace.TraceId} | {trace.Kind} | verursacht durch: {causes} | {trace.Summary}{subjects}";
        }).ToList();
        return lines.Count == 0 ? new[] { "Noch keine Zustandsänderungen in der Kausalitätsgeschichte." } : lines;
    }

    private bool TryGetPlayback(out DevelopmentScenarioPlayback current)
    {
        if (playback != null)
        {
            current = playback;
            return true;
        }

        current = null!;
        SessionStatus.Text = "Zuerst ein Szenario oder einen Run-Record laden.";
        return false;
    }

    private void EnsureCatalog()
    {
        if (catalog == null) throw new InvalidOperationException("Der gemeinsame GameData-Katalog konnte nicht geladen werden.");
    }

    private static string FindRequiredDirectory(params string[] segments)
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var current = new DirectoryInfo(start);
            for (var depth = 0; depth < 10 && current != null; depth++, current = current.Parent)
            {
                var candidate = Path.Combine(new[] { current.FullName }.Concat(segments).ToArray());
                if (Directory.Exists(candidate)) return candidate;
            }
        }

        throw new DirectoryNotFoundException($"Repository directory '{Path.Combine(segments)}' was not found.");
    }

    private sealed class ScenarioEntry
    {
        public ScenarioEntry(string path)
        {
            Path = path;
            DisplayName = System.IO.Path.GetFileNameWithoutExtension(path);
        }

        public string Path { get; }
        public string DisplayName { get; }
    }

    private enum LocationCommandKind
    {
        Inspect,
        ScoutSurroundings,
        LocationAction,
        AdvanceProject
    }

    /// <summary>Read-only WPF projection of one command offered by the shared application layer.</summary>
    private sealed class LocationCommandEntry
    {
        public LocationCommandEntry(
            string id,
            string locationId,
            string locationName,
            HexCoord coord,
            LocationCommandKind kind,
            string? actionId,
            string label,
            string details,
            bool isAvailable,
            string? lockedReason)
        {
            Id = id;
            LocationId = locationId;
            LocationName = locationName;
            Coord = coord;
            Kind = kind;
            ActionId = actionId;
            Label = label;
            Details = details;
            IsAvailable = isAvailable;
            LockedReason = lockedReason;
        }

        public string Id { get; }
        public string LocationId { get; }
        public string LocationName { get; }
        public HexCoord Coord { get; }
        public LocationCommandKind Kind { get; }
        public string? ActionId { get; }
        public string Label { get; }
        public string Details { get; }
        public bool IsAvailable { get; }
        public string? LockedReason { get; }
        public string AvailabilityText => IsAvailable ? "Verfügbar" : $"Gesperrt: {LockedReason}";
        public string ExecuteLabel => Kind switch
        {
            LocationCommandKind.Inspect => "Betrachten",
            LocationCommandKind.ScoutSurroundings => "Umgebung absuchen",
            LocationCommandKind.AdvanceProject => "Projekt fortsetzen",
            _ => "Aktion ausführen"
        };
    }

    private sealed class AvailableScoutEntry
    {
        public AvailableScoutEntry(string id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        public string Id { get; }
        public string DisplayName { get; }
    }

    private sealed class TestTeamMemberEntry
    {
        public TestTeamMemberEntry(string id, string name, ExpeditionMemberRole role, ExpeditionMemberStatus status, int starLevel, string source)
        {
            Id = id;
            Name = name;
            Role = role;
            Status = status;
            StarLevel = starLevel;
            Source = source;
        }

        public string Id { get; }
        public string Name { get; }
        public ExpeditionMemberRole Role { get; }
        public ExpeditionMemberStatus Status { get; }
        public int StarLevel { get; }
        public string Source { get; }
        public bool IsAvailable => Status == ExpeditionMemberStatus.Available;
        public string DisplayName => $"{Name} ({Role})";
        public string Detail => $"{Source} | {Status} | {StarLevel} Sterne";
    }

    private sealed class TestTeamOptionEntry
    {
        public TestTeamOptionEntry(string locationName, string label, bool isAvailable, string? lockedReason)
        {
            LocationName = locationName;
            Label = label;
            IsAvailable = isAvailable;
            LockedReason = lockedReason;
        }

        public string LocationName { get; }
        public string Label { get; }
        public bool IsAvailable { get; }
        public string? LockedReason { get; }
        public string AvailabilityText => IsAvailable ? "Verfügbar" : $"Gesperrt: {LockedReason}";
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions JsonOptionsIndented = new() { WriteIndented = true };
}
