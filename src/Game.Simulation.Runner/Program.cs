using System.Text.Json;
using Game.App;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: Game.Simulation.Runner <scenario-or-run-record.json> [game-data-root] [run-record.json]");
    return 2;
}

var scenarioPath = Path.GetFullPath(args[0]);
var dataRoot = args.Length > 1
    ? Path.GetFullPath(args[1])
    : FindGameDataRoot();
if (dataRoot == null)
{
    Console.Error.WriteLine("GameData root was not found. Supply it as the second argument.");
    return 2;
}

var catalog = GameDataCatalog.LoadFromDirectory(dataRoot);
if (catalog == null)
{
    Console.Error.WriteLine($"GameData could not be loaded from '{dataRoot}'.");
    return 2;
}

var inputJson = File.ReadAllText(scenarioPath);
var runRecord = TryLoadRunRecord(inputJson);
var result = runRecord == null
    ? new DevelopmentScenarioExecutor().Execute(catalog, DevelopmentScenarioLoader.LoadFile(scenarioPath))
    : runRecord.Replay(catalog);
var record = result.Session.CreateRunRecord();
Console.WriteLine($"Scenario: {result.ScenarioId}");
Console.WriteLine($"Seed: {record.Seed}; Content: {record.ContentVersion}; Commands: {record.Commands.Count}; Traces: {record.Traces.Count}");
foreach (var trace in record.Traces)
{
    var causes = trace.CausedByTraceIds.Count == 0 ? "root" : string.Join(", ", trace.CausedByTraceIds);
    Console.WriteLine($"Day {trace.WorldDay} | {trace.Kind} | {trace.TraceId} | caused by: {causes} | {trace.Summary}");
}
foreach (var failure in result.Failures) Console.Error.WriteLine(failure);

if (args.Length > 2)
{
    File.WriteAllText(Path.GetFullPath(args[2]), JsonSerializer.Serialize(result.CreateRunRecord(), new JsonSerializerOptions { WriteIndented = true }));
}

return result.Success ? 0 : 1;

static string? FindGameDataRoot()
{
    var current = Directory.GetCurrentDirectory();
    for (var index = 0; index < 8 && !string.IsNullOrWhiteSpace(current); index++)
    {
        var candidate = Path.Combine(current, "UnityHexMapView", "Assets", "StreamingAssets", "GameData");
        if (Directory.Exists(candidate)) return candidate;
        current = Directory.GetParent(current)?.FullName ?? string.Empty;
    }
    return null;
}

static DevelopmentScenarioRunRecord? TryLoadRunRecord(string json)
{
    using var document = JsonDocument.Parse(json);
    if (document.RootElement.ValueKind != JsonValueKind.Object
        || !document.RootElement.TryGetProperty("Scenario", out _)
        || !document.RootElement.TryGetProperty("Simulation", out _))
    {
        return null;
    }

    return JsonSerializer.Deserialize<DevelopmentScenarioRunRecord>(json, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    }) ?? throw new InvalidOperationException("Run record is empty.");
}
