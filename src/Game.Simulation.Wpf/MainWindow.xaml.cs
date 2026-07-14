using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using Game.App;
using Game.Core;
using Microsoft.Win32;

namespace Game.Simulation.Wpf;

public partial class MainWindow : Window
{
    private GameDataCatalog? catalog;
    private DevelopmentScenarioPlayback? playback;

    public MainWindow()
    {
        InitializeComponent();
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
        if (current.HasManualTimeAdvance)
        {
            SessionStatus.Text = "Der Lauf enthält manuelles Fortschreiben. Für einen reproduzierbaren Record zuerst die Tage als Skriptbefehle im Szenario festhalten.";
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
        PlayerOptionsList.ItemsSource = BuildPlayerOptions(playback.Session, game);
        WorldLocationsList.ItemsSource = BuildWorldLocations(game);
        WorldFactionsList.ItemsSource = BuildWorldFactions(game);
        WorldProcessesList.ItemsSource = BuildWorldProcesses(game);
        CausalityList.ItemsSource = BuildCausality(game);
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

    private static IReadOnlyList<string> BuildPlayerOptions(SimulationSession session, GameState game)
    {
        var lines = new List<string>();
        foreach (var location in game.World.Locations.Where(location => location.Anchor.Coords.Any(coord => game.Knowledge.GetTileKnowledge(coord) == KnowledgeLevel.Confirmed)))
        {
            var query = session.GetLocationInteraction(location.Id);
            if (!query.Success || query.Interaction == null) continue;
            lines.Add($"{location.Name}");
            lines.AddRange(query.Interaction.Options.Select(option =>
                $"  {(option.IsAvailable ? "verfügbar" : "gesperrt")}: {option.Action.Label} | Risiko {option.RiskBand} ({option.Confidence}) | {option.Commitment}{(option.IsAvailable ? string.Empty : $" | {option.LockedReason}")}"));
        }

        return lines.Count == 0 ? new[] { "Keine bestätigte Location mit sichtbaren Interaktionsoptionen." } : lines;
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

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions JsonOptionsIndented = new() { WriteIndented = true };
}
