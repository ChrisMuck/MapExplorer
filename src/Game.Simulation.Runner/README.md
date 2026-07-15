# Game.Simulation.Runner

Headless developer runner for reproducible individual scenarios and seed batches. It uses only
`Game.App` and `Game.Core`; it is suitable for CI, local regression checks and inspecting a failing
seed before opening the WPF inspector.

Run one scenario or exported scripted run record:

```powershell
dotnet run --project src\Game.Simulation.Runner\Game.Simulation.Runner.csproj -- `
  tests\DevelopmentScenarios\scenario-sealed-containment.json
```

Run a batch from a deterministic seed range. The runner automatically includes every
`tests/DevelopmentScenarios/scenario-*.json` proof scenario it finds:

```powershell
dotnet run --project src\Game.Simulation.Runner\Game.Simulation.Runner.csproj -- `
  --batch 41027 100 .\UnityHexMapView\Assets\StreamingAssets\GameData .\batch-report.json
```

The JSON report contains per-seed location/connection/process metrics and scenario metrics. Its
issues identify invalid cross-system content, generation failures, missing soft connections,
process-cap violations, unanswerable situations, missing warning paths, faction observations with
no reaction trace, dead-end visible options and failing scenarios. An empty `issues` list is the
technical batch pass condition; it does not replace owner review or Unity playtesting.
