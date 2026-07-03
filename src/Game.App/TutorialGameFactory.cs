using System;
using System.Collections.Generic;
using Game.Core;

namespace Game.App
{

public static class TutorialGameFactory
{
    public static GameState Create()
    {
        var bounds = new HexMapBounds(40, 30);
        var map = GenerateTutorialMap(bounds);
        var baseCoord = new HexCoord(3, 15);

        map.SetTile(new HexTileState(baseCoord, TerrainType.Coast, locationId: "base-camp"));
        map.SetTile(new HexTileState(new HexCoord(4, 15), TerrainType.Coast, roadId: "old-coast-road"));
        map.SetTile(new HexTileState(new HexCoord(5, 15), TerrainType.Forest, roadId: "old-coast-road"));
        map.SetTile(new HexTileState(new HexCoord(7, 14), TerrainType.Hills));
        map.SetTile(new HexTileState(new HexCoord(8, 14), TerrainType.Mountain, elevation: 3));
        map.SetTile(new HexTileState(new HexCoord(9, 14), TerrainType.Mountain, elevation: 4, isBlocked: true));
        map.SetTile(new HexTileState(new HexCoord(6, 16), TerrainType.Swamp, riverId: "gray-river"));
        map.SetTile(new HexTileState(new HexCoord(12, 15), TerrainType.Hills, locationId: "broken-ravine"));

        var world = new WorldState(map, CreateTutorialPaths(), CreateTutorialLocations());
        var knowledge = new KnowledgeState();
        ConfirmStartArea(knowledge, baseCoord);

        var notes = new PlayerNotesState();
        var expedition = new ExpeditionState(
            expeditionNumber: 1,
            position: baseCoord,
            members: CreateTutorialMembers(),
            movementPoints: 4,
            supplies: 20,
            medicine: 3,
            morale: 70,
            capacity: 20);
        var baseState = new BaseState(baseCoord);
        baseState.AddArchiveEntry("First expedition prepared at the coastal base.");

        return new GameState(world, knowledge, notes, expedition, baseState);
    }

    private static HexMapState GenerateTutorialMap(HexMapBounds bounds)
    {
        var tiles = new List<HexTileState>();

        foreach (var coord in bounds.AllCoords())
        {
            var view = ToCenteredViewCoord(coord, bounds);
            var terrain = PickTerrain(view);
            var elevation = terrain == TerrainType.Snow ? 4 :
                terrain == TerrainType.Mountain ? 3 :
                terrain == TerrainType.Hills ? 2 :
                terrain == TerrainType.Water ? 0 :
                1;

            tiles.Add(new HexTileState(coord, terrain, elevation, terrain == TerrainType.Water));
        }

        return new HexMapState(bounds, tiles);
    }

    private static IEnumerable<WorldPathState> CreateTutorialPaths()
    {
        return new[]
        {
            new WorldPathState("river-gray", WorldPathKind.River, ViewPath(
                (-8, 1), (-7, 1), (-6, 0), (-5, 0), (-4, 1), (-3, 1), (-2, 2), (-1, 2),
                (0, 1), (1, 1), (2, 0), (3, 0), (4, -1), (5, -1), (6, -2), (7, -2))),
            new WorldPathState("river-north", WorldPathKind.River, ViewPath(
                (-2, -6), (-1, -6), (0, -6), (0, -5), (1, -5), (1, -4), (2, -4), (2, -3),
                (3, -3), (4, -4))),
            new WorldPathState("road-main", WorldPathKind.Road, ViewPath(
                (-5, 2), (-4, 2), (-3, 2), (-2, 1), (-1, 0), (0, 0), (1, -1), (2, -1), (3, -2))),
            new WorldPathState("road-east-branch", WorldPathKind.Road, ViewPath(
                (-1, 0), (-1, 1), (0, 2), (1, 2), (2, 2), (3, 2), (4, 3))),
            new WorldPathState("road-south-branch", WorldPathKind.Road, ViewPath(
                (-5, 2), (-5, 3), (-4, 4), (-3, 5))),
            new WorldPathState("border-wardens", WorldPathKind.TerritoryBorder, ViewPath(
                (-6, 3), (-5, 2), (-4, 2), (-3, 1), (-2, 1), (-1, 0), (0, 0), (1, -1),
                (2, -1), (3, -2), (4, -2))),
            new WorldPathState("ancient-wall", WorldPathKind.Wall, ViewPath(
                (-12, 10), (-11, 9), (-10, 9), (-9, 8), (-8, 8), (-7, 7), (-6, 7), (-5, 6)))
        };
    }

    private static IEnumerable<SpecialLocationState> CreateTutorialLocations()
    {
        return new[]
        {
            new SpecialLocationState("base-camp", LocationKind.BaseCamp, new HexCoord(3, 15), "Coastal Base"),
            new SpecialLocationState("settlement-west", LocationKind.Settlement, ViewCoord(-5, 2), "Western Camp"),
            new SpecialLocationState("settlement-crossing", LocationKind.Settlement, ViewCoord(-1, 0), "River Crossing"),
            new SpecialLocationState("settlement-east", LocationKind.Settlement, ViewCoord(3, -2), "Eastern Hamlet"),
            new SpecialLocationState("settlement-north", LocationKind.Settlement, ViewCoord(4, 3), "Northern Village"),
            new SpecialLocationState("settlement-south", LocationKind.Settlement, ViewCoord(-3, 5), "Foothill Camp"),
            new SpecialLocationState("watchtower", LocationKind.Watchtower, ViewCoord(9, 2), "Old Watchtower"),
            new SpecialLocationState("mine", LocationKind.Mine, ViewCoord(-10, -2), "Abandoned Mine"),
            new SpecialLocationState("broken-ravine", LocationKind.BrokenRavine, new HexCoord(12, 15), "Broken Ravine")
        };
    }

    private static IEnumerable<HexCoord> ViewPath(params (int Q, int R)[] coords)
    {
        foreach (var coord in coords)
        {
            yield return ViewCoord(coord.Q, coord.R);
        }
    }

    private static HexCoord ViewCoord(int q, int r)
    {
        var row = r + 15;
        var centeredColumn = q + (r - (r & 1)) / 2;
        var column = centeredColumn + 20;
        return new HexCoord(column, row);
    }

    private static HexCoord ToCenteredViewCoord(HexCoord coord, HexMapBounds bounds)
    {
        var centeredRow = coord.R - bounds.Height / 2;
        var centeredColumn = coord.Q - bounds.Width / 2;
        var q = centeredColumn - (centeredRow - (centeredRow & 1)) / 2;
        return new HexCoord(q, centeredRow);
    }

    private static TerrainType PickTerrain(HexCoord view)
    {
        var water = WaterStrength(view);
        var mountain = MountainStrength(view);
        var forest = ForestStrength(view);
        var dry = DryPlainsStrength(view);

        if (water > 0.74)
        {
            return TerrainType.Water;
        }

        if (water > 0.52)
        {
            return TerrainType.Coast;
        }

        if (mountain > 0.88)
        {
            return Hash01(view.Q, view.R, 701) > 0.72 ? TerrainType.Snow : TerrainType.Mountain;
        }

        if (mountain > 0.64)
        {
            return TerrainType.Mountain;
        }

        if (mountain > 0.42)
        {
            return TerrainType.Hills;
        }

        if (forest > 0.58)
        {
            return TerrainType.Forest;
        }

        if (forest > 0.43)
        {
            return TerrainType.Hills;
        }

        if (dry > 0.68)
        {
            return TerrainType.DryPlains;
        }

        return TerrainType.Grassland;
    }

    private static double WaterStrength(HexCoord view)
    {
        var strength = 0.0;
        strength = Math.Max(strength, Blob(view, 13.5, 4.0, 5.2, 3.6));
        strength = Math.Max(strength, Blob(view, -14.0, -5.0, 4.2, 3.4));
        strength = Math.Max(strength, Blob(view, 16.0, -8.0, 5.0, 4.2));
        strength = Math.Max(strength, Blob(view, -6.0, -11.5, 4.4, 2.8));
        strength = Math.Max(strength, Blob(view, 6.0, 10.5, 3.8, 2.6));
        return strength + (Hash01(view.Q, view.R, 91) - 0.5) * 0.18;
    }

    private static double MountainStrength(HexCoord view)
    {
        if (view.Q < -16 || view.Q > 15)
        {
            return 0.0;
        }

        var ridge = 7.2 - view.Q * 0.18 + Math.Sin((view.Q + 4.0) * 0.55) * 1.0;
        var distance = Math.Abs(view.R - ridge);
        var pass = Math.Abs(view.Q + 4) < 1.25 || Math.Abs(view.Q - 7) < 1.1 ? 0.24 : 0.0;
        return Math.Max(0.0, 1.0 - distance / 3.1 + (Hash01(view.Q, view.R, 311) - 0.5) * 0.24 - pass);
    }

    private static double ForestStrength(HexCoord view)
    {
        var strength = 0.0;
        strength = Math.Max(strength, Blob(view, -9.0, 2.5, 4.8, 3.0));
        strength = Math.Max(strength, Blob(view, -15.0, 9.0, 3.4, 2.8));
        strength = Math.Max(strength, Blob(view, 8.0, -3.5, 4.0, 3.2));
        strength = Math.Max(strength, Blob(view, 12.0, 9.0, 3.8, 2.8));
        strength = Math.Max(strength, Blob(view, -2.0, -8.0, 3.4, 2.6));
        return strength + (Hash01(view.Q, view.R, 173) - 0.5) * 0.16;
    }

    private static double DryPlainsStrength(HexCoord view)
    {
        var strength = 0.0;
        strength = Math.Max(strength, Blob(view, -7.0, -2.0, 4.5, 3.4));
        strength = Math.Max(strength, Blob(view, 4.0, -6.5, 5.0, 3.6));
        strength = Math.Max(strength, Blob(view, 12.0, 0.0, 3.6, 2.8));
        return strength + (Hash01(view.Q, view.R, 419) - 0.5) * 0.12;
    }

    private static double Blob(HexCoord view, double centerQ, double centerR, double radiusQ, double radiusR)
    {
        var q = (view.Q - centerQ) / radiusQ;
        var r = (view.R - centerR) / radiusR;
        return Math.Max(0.0, 1.0 - Math.Sqrt(q * q + r * r));
    }

    private static double Hash01(int q, int r, int seed)
    {
        unchecked
        {
            var n = q * 374761393 + r * 668265263 + seed * 2147483647;
            n = (n ^ (n >> 13)) * 1274126177;
            return ((n ^ (n >> 16)) & 0x7fffffff) / 2147483647.0;
        }
    }

    private static void ConfirmStartArea(KnowledgeState knowledge, HexCoord baseCoord)
    {
        knowledge.SetTileKnowledge(baseCoord, KnowledgeLevel.Confirmed);

        foreach (var neighbor in baseCoord.Neighbors())
        {
            knowledge.SetTileKnowledge(neighbor, KnowledgeLevel.Confirmed);
        }
    }

    private static IEnumerable<ExpeditionMemberState> CreateTutorialMembers()
    {
        return new[]
        {
            new ExpeditionMemberState("scout-1", "Mira", ExpeditionMemberRole.Scout),
            new ExpeditionMemberState("scout-2", "Tovin", ExpeditionMemberRole.Scout),
            new ExpeditionMemberState("guard-1", "Bram", ExpeditionMemberRole.Guard),
            new ExpeditionMemberState("guard-2", "Ilyra", ExpeditionMemberRole.Guard),
            new ExpeditionMemberState("carrier-1", "Nessa", ExpeditionMemberRole.Carrier),
            new ExpeditionMemberState("carrier-2", "Oren", ExpeditionMemberRole.Carrier),
            new ExpeditionMemberState("medic-1", "Sela", ExpeditionMemberRole.Medic),
            new ExpeditionMemberState("scholar-1", "Rook", ExpeditionMemberRole.Scholar)
        };
    }
}
}
