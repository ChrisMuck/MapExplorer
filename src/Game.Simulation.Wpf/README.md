# Game.Simulation.Wpf

Windows-only developer inspector for the shared expedition simulation. It is not a game client,
map renderer or Unity UI replacement.

Run it from the repository root:

```powershell
dotnet run --project src\Game.Simulation.Wpf\Game.Simulation.Wpf.csproj
```

The runner locates `UnityHexMapView/Assets/StreamingAssets/GameData` and
`tests/DevelopmentScenarios` from the repository root. It offers the three proof scenarios,
loading of exported scripted run records, individual scripted commands and normal end-day
fast-forwarding.

The tabs deliberately separate Player Knowledge, read-only World Truth and the causal trace
timeline. Manual fast-forwarding is visible in the session and disables scripted-record export;
make the same days explicit scenario commands before saving a reproducible record.

The project references only `Game.App` and `Game.Core`. Unity assemblies, map visuals, prefabs and
MonoBehaviours are intentionally absent.
