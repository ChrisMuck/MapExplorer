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
fast-forwarding. For each confirmed location it separately offers the voluntary direct commands
**Betrachten**, **Umgebung absuchen** (with a selected free scout), all player-visible location
actions and, where applicable, project progress. Local surroundings searches resolve immediately
and spend the JSON-defined movement cost (currently two movement points); directional scouting
remains a separate multi-day mission.

The tabs deliberately separate Player Knowledge, read-only World Truth and the causal trace
timeline. Every direct decision and manual fast-forward is visible in the session and disables
scripted-record export; make the same decisions explicit scenario commands before saving a
reproducible record.

The **Testteam zusammenstellen** tab is a non-mutating specialist-gate preview. It combines the
base roster (when present) with the active expedition, lets developers select an available team,
and evaluates confirmed-location options through the same `Game.App` query contract. It never
changes the current expedition, world state, knowledge state or scenario command path.

The project references only `Game.App` and `Game.Core`. Unity assemblies, map visuals, prefabs and
MonoBehaviours are intentionally absent.
