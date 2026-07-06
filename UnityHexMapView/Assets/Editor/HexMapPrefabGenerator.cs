using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class HexMapPrefabGenerator
{
    private const string GeneratedRoot = "Assets/Prefabs/Generated";
    private const string MaterialRoot = "Assets/Materials/Generated";
    private const string LibraryPath = "Assets/Settings/DefaultHexMapPrefabLibrary.asset";
    private const string MeshLibraryPath = "Assets/Prefabs/Generated/GeneratedMeshes.asset";

    // SaveAsPrefabAsset does not persist procedurally generated meshes (the MeshFilter's
    // m_Mesh becomes null). We collect every generated mesh into this shared asset so the
    // prefabs reference real, saved meshes instead of empty shells.
    private static Mesh s_meshLibrary;

    [InitializeOnLoadMethod]
    private static void GenerateAfterImport()
    {
        EditorApplication.delayCall += () =>
        {
            if (!AssetDatabase.LoadAssetAtPath<GameObject>($"{GeneratedRoot}/Forest/ForestCluster_Mixed_Dense.prefab"))
            {
                GenerateDefaultPrefabs();
            }
        };
    }

    [MenuItem("Hex Map/Generate Default Prefabs")]
    public static void GenerateDefaultPrefabs()
    {
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets/Prefabs", "Generated");
        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets/Materials", "Generated");

        PrepareMeshLibrary();

        // Remove prefabs from the previous naming scheme so they do not linger as broken assets.
        DeleteObsoletePrefab($"{GeneratedRoot}/Forest/Tree_Round.prefab");
        DeleteObsoletePrefab($"{GeneratedRoot}/Forest/ForestCluster_Dense.prefab");
        DeleteObsoletePrefab($"{GeneratedRoot}/Forest/ForestCluster_Edge.prefab");
        DeleteObsoletePrefab($"{GeneratedRoot}/Mountains/MountainPeak.prefab");
        DeleteObsoletePrefab($"{GeneratedRoot}/Mountains/MountainPeak_Snowy.prefab");
        DeleteObsoletePrefab($"{GeneratedRoot}/Mountains/RockyRidge.prefab");
        DeleteObsoletePrefab($"{GeneratedRoot}/Mountains/RockyRidge_Snowy.prefab");
        DeleteObsoletePrefab($"{GeneratedRoot}/Settlements/SettlementCluster.prefab");

        var materials = CreateMaterials();

        var treePine = SavePrefab($"{GeneratedRoot}/Forest/Tree_Pine.prefab", CreatePineTree("Tree_Pine", materials, 1f));
        var treeBroadleaf = SavePrefab($"{GeneratedRoot}/Forest/Tree_Broadleaf.prefab", CreateBroadleafTree("Tree_Broadleaf", materials, 1f));
        var treeTall = SavePrefab($"{GeneratedRoot}/Forest/Tree_Tall.prefab", CreatePineTree("Tree_Tall", materials, 1.22f));

        var coniferDense = SavePrefab($"{GeneratedRoot}/Forest/ForestCluster_Conifer_Dense.prefab", CreateForestCluster("ForestCluster_Conifer_Dense", materials, 9, ForestMode.Conifer));
        var coniferEdge = SavePrefab($"{GeneratedRoot}/Forest/ForestCluster_Conifer_Edge.prefab", CreateForestCluster("ForestCluster_Conifer_Edge", materials, 6, ForestMode.Conifer));
        var deciduousDense = SavePrefab($"{GeneratedRoot}/Forest/ForestCluster_Deciduous_Dense.prefab", CreateForestCluster("ForestCluster_Deciduous_Dense", materials, 9, ForestMode.Deciduous));
        var deciduousEdge = SavePrefab($"{GeneratedRoot}/Forest/ForestCluster_Deciduous_Edge.prefab", CreateForestCluster("ForestCluster_Deciduous_Edge", materials, 6, ForestMode.Deciduous));
        var mixedDense = SavePrefab($"{GeneratedRoot}/Forest/ForestCluster_Mixed_Dense.prefab", CreateForestCluster("ForestCluster_Mixed_Dense", materials, 9, ForestMode.Mixed));
        var mixedEdge = SavePrefab($"{GeneratedRoot}/Forest/ForestCluster_Mixed_Edge.prefab", CreateForestCluster("ForestCluster_Mixed_Edge", materials, 6, ForestMode.Mixed));

        // Several faceted variants per mountain type so a range does not read as repeated cones;
        // the view picks among them per hex via its prefab hash.
        var peakVariants = new List<GameObject>();
        var snowyPeakVariants = new List<GameObject>();
        var ridgeVariants = new List<GameObject>();
        var snowyRidgeVariants = new List<GameObject>();
        for (var i = 0; i < 3; i++)
        {
            var suffix = (char)('A' + i);
            peakVariants.Add(SavePrefab($"{GeneratedRoot}/Mountains/MountainPeak_{suffix}.prefab", CreateMountainPeak($"MountainPeak_{suffix}", materials, false, 1f, 700 + i)));
            snowyPeakVariants.Add(SavePrefab($"{GeneratedRoot}/Mountains/MountainPeak_Snowy_{suffix}.prefab", CreateMountainPeak($"MountainPeak_Snowy_{suffix}", materials, true, 1.05f, 720 + i)));
            ridgeVariants.Add(SavePrefab($"{GeneratedRoot}/Mountains/RockyRidge_{suffix}.prefab", CreateRockyRidge($"RockyRidge_{suffix}", materials, false, 740 + i)));
            snowyRidgeVariants.Add(SavePrefab($"{GeneratedRoot}/Mountains/RockyRidge_Snowy_{suffix}.prefab", CreateRockyRidge($"RockyRidge_Snowy_{suffix}", materials, true, 760 + i)));
        }

        var foothills = SavePrefab($"{GeneratedRoot}/Mountains/Foothills.prefab", CreateFoothills("Foothills", materials, 780));
        var foothillRock = SavePrefab($"{GeneratedRoot}/Mountains/FoothillRock.prefab", CreateRock("FoothillRock", materials, 0.28f, 790));
        var rock = SavePrefab($"{GeneratedRoot}/Mountains/Rock.prefab", CreateRock("Rock", materials, 0.36f, 800));

        var hamlet = SavePrefab($"{GeneratedRoot}/Settlements/Village_Small.prefab", CreateSettlement("Village_Small", materials, 3, 0.32f, false, false, 810));
        var villageA = SavePrefab($"{GeneratedRoot}/Settlements/Village_A.prefab", CreateSettlement("Village_A", materials, 5, 0.4f, true, false, 811));
        var villageB = SavePrefab($"{GeneratedRoot}/Settlements/Village_B.prefab", CreateSettlement("Village_B", materials, 6, 0.42f, true, false, 812));
        var town = SavePrefab($"{GeneratedRoot}/Settlements/Town.prefab", CreateSettlement("Town", materials, 9, 0.5f, false, true, 813));
        var houseA = SavePrefab($"{GeneratedRoot}/Settlements/House_A.prefab", CreateHouse("House_A", materials, 1f, 820));
        var houseB = SavePrefab($"{GeneratedRoot}/Settlements/House_B.prefab", CreateHouse("House_B", materials, 0.78f, 821));
        var fence = SavePrefab($"{GeneratedRoot}/Settlements/Fence.prefab", CreateFence("Fence", materials));

        var tower = SavePrefab($"{GeneratedRoot}/Landmarks/Watchtower.prefab", CreateWatchtower("Watchtower", materials));
        var mine = SavePrefab($"{GeneratedRoot}/Landmarks/Mine.prefab", CreateMine("Mine", materials));
        var wallSegment = SavePrefab($"{GeneratedRoot}/Landmarks/WallSegment.prefab", CreateWallSegment("WallSegment", materials));
        var wallTower = SavePrefab($"{GeneratedRoot}/Landmarks/WallTower.prefab", CreateWallTower("WallTower", materials));
        var coastMarker = SavePrefab($"{GeneratedRoot}/Landmarks/CoastMarker.prefab", CreateCoastMarker("CoastMarker", materials));

        var grave = SavePrefab($"{GeneratedRoot}/Landmarks/MarkedGrave.prefab", CreateGrave("MarkedGrave", materials, 830));
        var abandonedCamp = SavePrefab($"{GeneratedRoot}/Landmarks/AbandonedCamp.prefab", CreateAbandonedCamp("AbandonedCamp", materials, 831));
        var ravine = SavePrefab($"{GeneratedRoot}/Landmarks/BrokenRavine.prefab", CreateBrokenRavine("BrokenRavine", materials, 832));
        var ruinA = SavePrefab($"{GeneratedRoot}/Landmarks/Ruin_A.prefab", CreateRuin("Ruin_A", materials, 833));
        var ruinB = SavePrefab($"{GeneratedRoot}/Landmarks/Ruin_B.prefab", CreateRuin("Ruin_B", materials, 834));
        var standingStonesA = SavePrefab($"{GeneratedRoot}/Landmarks/StandingStones_A.prefab", CreateLandmark("StandingStones_A", materials, 835));
        var standingStonesB = SavePrefab($"{GeneratedRoot}/Landmarks/StandingStones_B.prefab", CreateLandmark("StandingStones_B", materials, 836));

        var library = AssetDatabase.LoadAssetAtPath<HexMapPrefabLibrary>(LibraryPath);
        if (library == null)
        {
            EnsureFolder("Assets", "Settings");
            library = ScriptableObject.CreateInstance<HexMapPrefabLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }

        library.forestClusterPrefabs = new[] { mixedDense, mixedEdge };
        library.treePrefabs = new[] { treePine, treeBroadleaf, treeTall };
        library.coniferousForestClusterPrefabs = new[] { coniferDense, coniferEdge };
        library.deciduousForestClusterPrefabs = new[] { deciduousDense, deciduousEdge };
        library.mixedForestClusterPrefabs = new[] { mixedDense, mixedEdge };
        library.pineTreePrefabs = new[] { treePine, treeTall };
        library.broadleafTreePrefabs = new[] { treeBroadleaf };
        library.mountainPeakPrefabs = peakVariants.ToArray();
        library.snowyMountainPeakPrefabs = snowyPeakVariants.ToArray();
        library.rockyRidgePrefabs = ridgeVariants.ToArray();
        library.snowyRockyRidgePrefabs = snowyRidgeVariants.ToArray();
        library.foothillsPrefabs = new[] { foothills };
        library.foothillRockPrefabs = new[] { foothillRock };
        library.rockPrefabs = new[] { rock, foothillRock };
        library.settlementPrefabs = new[] { hamlet, villageA, villageB, town };
        library.settlementHousePrefabs = new[] { houseA, houseB };
        library.settlementFencePrefabs = new[] { fence };
        library.towerPrefabs = new[] { tower };
        library.minePrefabs = new[] { mine };
        library.wallSegmentPrefabs = new[] { wallSegment };
        library.wallTowerPrefabs = new[] { wallTower };
        library.coastMarkerPrefabs = new[] { coastMarker };
        library.gravePrefabs = new[] { grave };
        library.abandonedCampPrefabs = new[] { abandonedCamp };
        library.ravinePrefabs = new[] { ravine };
        library.ruinPrefabs = new[] { ruinA, ruinB };
        library.landmarkPrefabs = new[] { standingStonesA, standingStonesB };

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Generated default hex map prefabs and updated DefaultHexMapPrefabLibrary.");
    }

    private static MaterialSet CreateMaterials()
    {
        return new MaterialSet
        {
            Bark = Material("Bark", "4b3326", 0.72f),
            Leaf = Material("Leaf", "2f6a42", 0.82f),
            LeafDark = Material("LeafDark", "1e432f", 0.88f),
            Rock = Material("Rock", "606359", 0.88f),
            RockDark = Material("RockDark", "34362f", 0.9f),
            RockLight = Material("RockLight", "7c8076", 0.86f),
            RockWarm = Material("RockWarm", "635a4c", 0.88f),
            Snow = Material("Snow", "dbe1de", 0.6f),
            SnowShadow = Material("SnowShadow", "b3bec4", 0.66f),
            GroundDark = Material("ForestGround", "233f2d", 0.86f),
            Wall = Material("WallStone", "847e6b", 0.82f),
            Roof = Material("RoofRose", "86506a", 0.76f),
            RoofWarm = Material("RoofWarm", "9a6240", 0.76f),
            Wood = Material("Wood", "5a3d29", 0.82f),
            Flag = Material("FlagRed", "bb5148", 0.55f),
            Gold = Material("GoldOre", "c09a38", 0.45f),
            Cloth = Material("Cloth", "b3a37c", 0.6f),
            Charred = Material("Charred", "292420", 0.9f),
            Smoke = TransparentMaterial("Smoke", "c1b8aa", 0.24f)
        };
    }

    private enum ForestMode
    {
        Conifer,
        Deciduous,
        Mixed
    }

    // Pines now taper to real points (topRadius ~0.01) so no flat cap catches the light as a ring.
    private static GameObject CreatePineTree(string name, MaterialSet materials, float heightScale)
    {
        var root = NewRoot(name);
        AddCylinder(root.transform, "Trunk", materials.Bark, new Vector3(0f, 0.13f * heightScale, 0f), new Vector3(0.07f, 0.13f * heightScale, 0.07f), Quaternion.identity);
        AddCone(root.transform, "LowerCrown", materials.LeafDark, 0.32f, 0.012f, 0.40f * heightScale, new Vector3(0f, 0.40f * heightScale, 0f), Quaternion.Euler(-3f, 30f, 3f));
        AddCone(root.transform, "MidCrown", materials.Leaf, 0.22f, 0.01f, 0.34f * heightScale, new Vector3(0f, 0.62f * heightScale, 0f), Quaternion.Euler(2f, 10f, -2f));
        AddCone(root.transform, "TopCrown", materials.LeafDark, 0.14f, 0.008f, 0.26f * heightScale, new Vector3(0f, 0.82f * heightScale, 0f), Quaternion.Euler(-2f, -18f, 2f));
        return root;
    }

    // Broadleaf crown = faceted bipyramids (apex up + apex down), so it reads round with no flat top.
    private static GameObject CreateBroadleafTree(string name, MaterialSet materials, float heightScale)
    {
        var root = NewRoot(name);
        AddCylinder(root.transform, "Trunk", materials.Bark, new Vector3(0f, 0.12f, 0f), new Vector3(0.085f, 0.12f, 0.085f), Quaternion.identity);
        AddBipyramidCrown(root.transform, "Crown", materials.Leaf, 0.32f, 0.42f * heightScale, 0.30f * heightScale, new Vector3(0f, 0.42f, 0f));
        AddBipyramidCrown(root.transform, "CrownPuff", materials.LeafDark, 0.2f, 0.26f * heightScale, 0.2f * heightScale, new Vector3(0.1f, 0.56f, -0.05f));
        return root;
    }

    private static void AddBipyramidCrown(Transform parent, string name, Material material, float radius, float upperHeight, float lowerHeight, Vector3 center)
    {
        var holder = NewChild(parent, name, center, Quaternion.Euler(0f, 20f, 0f), Vector3.one);
        AddCone(holder.transform, "Upper", material, radius, 0.02f, upperHeight, new Vector3(0f, upperHeight * 0.5f, 0f), Quaternion.identity, 7);
        AddCone(holder.transform, "Lower", material, radius, 0.02f, lowerHeight, new Vector3(0f, -lowerHeight * 0.5f, 0f), Quaternion.Euler(180f, 0f, 0f), 7);
    }

    private static GameObject CreateForestCluster(string name, MaterialSet materials, int treeCount, ForestMode mode)
    {
        var root = NewRoot(name);
        // No ground hex here: the cluster prefab is placed with a random Y rotation for variety,
        // and a hex plate would visibly rotate against the grid. The forest terrain tile beneath
        // already provides the grid-aligned forest floor.
        for (var i = 0; i < treeCount; i++)
        {
            var angle = Mathf.PI * 2f * i / treeCount;
            var ring = i == 0 ? 0.05f : Mathf.Lerp(0.22f, 0.66f, (i % 5) / 4f);
            var pine = mode == ForestMode.Conifer || (mode == ForestMode.Mixed && i % 2 == 0);
            var tree = pine
                ? CreatePineTree("Tree", materials, 0.86f + (i % 5) * 0.07f)
                : CreateBroadleafTree("Tree", materials, 0.82f + (i % 4) * 0.08f);
            tree.transform.SetParent(root.transform, false);
            tree.transform.localPosition = new Vector3(Mathf.Cos(angle) * ring, 0.02f, Mathf.Sin(angle) * ring);
            tree.transform.localRotation = Quaternion.Euler(0f, i * 37f, 0f);
        }

        return root;
    }

    // Faceted rock massif: flared talus base, an irregular off-centre main peak with a lighter
    // sunlit face, lower secondary spurs for a ridge silhouette, snow that follows the summit
    // facets, and a few boulders spilling from the base.
    private static GameObject CreateMountainPeak(string name, MaterialSet materials, bool snowy, float scale, int seed)
    {
        var root = NewRoot(name);

        AddFacetedMound(root.transform, "Talus", materials.RockDark, 0.94f * scale, 0.62f * scale, 0.24f * scale, Vector3.zero, YRot(seed, 1), seed + 1);

        // Stockier proportions: wider base, gentler taper, less needle-like.
        var mainHeight = 1.0f * scale;
        var mainBaseY = 0.18f * scale;
        var mainPos = new Vector3(-0.04f * scale, mainBaseY, 0.02f * scale);
        var mainRot = YRot(seed, 2);
        AddFacetedPeak(root.transform, "MainPeak", materials.Rock, 0.72f * scale, 0.46f * scale, 0.06f * scale, mainHeight, mainPos, mainRot, seed + 2);
        // Small sunlit highlight, low on the front face (not a dominating light stripe).
        AddFacetedPeak(root.transform, "MainFace", materials.RockLight, 0.3f * scale, 0.2f * scale, 0.05f * scale, mainHeight * 0.5f, new Vector3(-0.2f * scale, mainBaseY + 0.04f * scale, 0.2f * scale), mainRot, seed + 3);

        for (var i = 0; i < 2; i++)
        {
            var a = Hash01(seed, i, 30) * Mathf.PI * 2f;
            var dist = Mathf.Lerp(0.34f, 0.48f, Hash01(seed, i, 31)) * scale;
            var h = mainHeight * Mathf.Lerp(0.46f, 0.7f, Hash01(seed, i, 32));
            AddFacetedPeak(root.transform, $"Spur_{i}", i == 0 ? materials.RockDark : materials.RockWarm, 0.4f * scale, 0.26f * scale, 0.05f * scale, h, new Vector3(Mathf.Cos(a) * dist, mainBaseY + 0.02f * scale, Mathf.Sin(a) * dist), Quaternion.Euler(0f, a * Mathf.Rad2Deg, 0f), seed + 40 + i);
        }

        // Snow only on snowy peaks; the view reserves those for the core of a range.
        if (snowy)
        {
            var snowHeight = mainHeight * 0.52f;
            var snowRadius = 0.44f * scale;
            var snowBaseY = mainBaseY + mainHeight * 0.5f;
            AddFacetedPeak(root.transform, "SnowShade", materials.SnowShadow, snowRadius * 1.06f, snowRadius * 0.62f, 0.05f * scale, snowHeight, new Vector3(-0.04f * scale, snowBaseY - 0.02f * scale, 0.02f * scale), mainRot, seed + 2);
            AddFacetedPeak(root.transform, "SnowCap", materials.Snow, snowRadius, snowRadius * 0.58f, 0.045f * scale, snowHeight, new Vector3(-0.05f * scale, snowBaseY, 0.03f * scale), mainRot, seed + 2);
        }

        for (var i = 0; i < 3; i++)
        {
            var a = Hash01(seed, i, 50) * Mathf.PI * 2f;
            var dist = Mathf.Lerp(0.55f, 0.8f, Hash01(seed, i, 51)) * scale;
            var s = Mathf.Lerp(0.1f, 0.18f, Hash01(seed, i, 52)) * scale;
            AddFacetedMound(root.transform, $"Boulder_{i}", i % 2 == 0 ? materials.Rock : materials.RockDark, s, s * 0.6f, s * 0.9f, new Vector3(Mathf.Cos(a) * dist, 0.03f * scale, Mathf.Sin(a) * dist), Quaternion.Euler(0f, a * 57f, 0f), seed + 60 + i);
        }

        return root;
    }

    // Lower faceted ridge with three summits along a wobbling line.
    private static GameObject CreateRockyRidge(string name, MaterialSet materials, bool snowy, int seed)
    {
        var root = NewRoot(name);
        AddFacetedMound(root.transform, "RidgeBase", materials.RockDark, 0.64f, 0.44f, 0.14f, Vector3.zero, YRot(seed, 1), seed + 1);

        var axis = Hash01(seed, 2, 8) * Mathf.PI;
        for (var i = 0; i < 3; i++)
        {
            var along = (i - 1) * 0.32f;
            var lateral = (Hash01(seed, i, 9) - 0.5f) * 0.18f;
            var px = Mathf.Cos(axis) * along + Mathf.Cos(axis + Mathf.PI * 0.5f) * lateral;
            var pz = Mathf.Sin(axis) * along + Mathf.Sin(axis + Mathf.PI * 0.5f) * lateral;
            var h = Mathf.Lerp(0.34f, 0.6f, Hash01(seed, i, 10));
            var mat = i == 1 ? materials.Rock : (i == 0 ? materials.RockDark : materials.RockWarm);
            var summitRot = YRot(seed, 11 + i);
            AddFacetedPeak(root.transform, $"Summit_{i}", mat, 0.3f, 0.19f, 0.04f, h, new Vector3(px, 0.06f, pz), summitRot, seed + 20 + i);
            if (snowy && i == 1)
            {
                AddFacetedPeak(root.transform, "RidgeSnow", materials.Snow, 0.22f, 0.13f, 0.03f, h * 0.46f, new Vector3(px, 0.06f + h * 0.5f, pz), summitRot, seed + 20 + i);
            }
        }

        return root;
    }

    private static GameObject CreateFoothills(string name, MaterialSet materials, int seed)
    {
        var root = NewRoot(name);
        for (var i = 0; i < 5; i++)
        {
            var angle = Mathf.PI * 2f * i / 5f + Hash01(seed, i, 3);
            var dist = Mathf.Lerp(0.2f, 0.42f, Hash01(seed, i, 4));
            var s = 0.18f + Hash01(seed, i, 5) * 0.12f;
            AddFacetedMound(root.transform, $"Rock_{i}", i % 2 == 0 ? materials.Rock : materials.RockDark, s, s * 0.5f, s * 0.8f, new Vector3(Mathf.Cos(angle) * dist, 0.04f, Mathf.Sin(angle) * dist), Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f), seed + 10 + i);
        }

        return root;
    }

    private static GameObject CreateRock(string name, MaterialSet materials, float size, int seed)
    {
        var root = NewRoot(name);
        AddFacetedMound(root.transform, "Rock", materials.Rock, size, size * 0.55f, size * 0.85f, Vector3.up * 0.02f, YRot(seed, 1), seed);
        AddFacetedMound(root.transform, "RockB", materials.RockDark, size * 0.55f, size * 0.3f, size * 0.5f, new Vector3(size * 0.5f, 0.01f, -size * 0.3f), YRot(seed, 2), seed + 1);
        return root;
    }

    private static Quaternion YRot(int seed, int salt)
    {
        return Quaternion.Euler(0f, Hash01(seed, salt, 999) * 360f, 0f);
    }

    // Parameterised settlement so hamlets, villages and towns can be generated from one builder:
    // a plaza, a ring of jittered houses (varied scale/rotation/roof), an optional well and fences.
    private static GameObject CreateSettlement(string name, MaterialSet materials, int houseCount, float radius, bool addFence, bool addWell, int seed)
    {
        var root = NewRoot(name);
        AddCylinder(root.transform, "Plaza", materials.Wall, Vector3.zero, new Vector3(radius * 1.05f, 0.025f, radius * 0.95f), Quaternion.Euler(0f, Hash01(seed, 0, 1) * 60f, 0f), 6);

        if (addWell)
        {
            AddCylinder(root.transform, "WellRim", materials.Wall, new Vector3(0f, 0.05f, 0f), new Vector3(0.11f, 0.05f, 0.11f), Quaternion.identity, 6);
            AddCylinder(root.transform, "WellShaft", materials.RockDark, new Vector3(0f, 0.055f, 0f), new Vector3(0.07f, 0.05f, 0.07f), Quaternion.identity, 6);
        }

        for (var i = 0; i < houseCount; i++)
        {
            var angle = Mathf.PI * 2f * i / houseCount + (Hash01(seed, i, 2) - 0.5f) * 0.6f;
            var ring = radius * Mathf.Lerp(addWell ? 0.42f : 0.5f, 0.98f, Hash01(seed, i, 3));
            var houseScale = 0.72f + Hash01(seed, i, 4) * 0.42f;
            var house = CreateHouse("House", materials, houseScale, seed * 31 + i);
            house.transform.SetParent(root.transform, false);
            house.transform.localPosition = new Vector3(Mathf.Cos(angle) * ring, 0.04f, Mathf.Sin(angle) * ring);
            house.transform.localRotation = Quaternion.Euler(0f, Hash01(seed, i, 5) * 360f, 0f);
        }

        if (addFence)
        {
            AddFence(root.transform, "FenceA", materials, new Vector3(-radius * 0.8f, 0.08f, -radius * 1.05f), Quaternion.Euler(0f, -8f, 0f));
            AddFence(root.transform, "FenceB", materials, new Vector3(radius * 0.5f, 0.08f, radius * 0.95f), Quaternion.Euler(0f, 168f, 0f));
        }

        AddCylinder(root.transform, "Smoke", materials.Smoke, new Vector3(radius * 0.2f, 0.48f, -0.06f), new Vector3(0.06f, 0.22f, 0.06f), Quaternion.identity);
        return root;
    }

    private static GameObject CreateHouse(string name, MaterialSet materials, float scale, int seed)
    {
        var root = NewRoot(name);
        var depth = 0.28f * scale * Mathf.Lerp(0.85f, 1.2f, Hash01(seed, 1, 3));
        AddCube(root.transform, "Body", materials.Wall, new Vector3(0f, 0.11f * scale, 0f), new Vector3(0.24f * scale, 0.22f * scale, depth), Quaternion.identity);
        var roofMaterial = Hash01(seed, 2, 4) < 0.5f ? materials.RoofWarm : materials.Roof;
        AddCone(root.transform, "Roof", roofMaterial, 0.22f * scale, 0.02f * scale, 0.2f * scale, new Vector3(0f, 0.32f * scale, 0f), Quaternion.Euler(0f, 45f, 0f), 4);
        return root;
    }

    private static GameObject CreateFence(string name, MaterialSet materials)
    {
        var root = NewRoot(name);
        AddFence(root.transform, "Fence", materials, Vector3.zero, Quaternion.identity);
        return root;
    }

    private static GameObject CreateWatchtower(string name, MaterialSet materials)
    {
        var root = NewRoot(name);
        AddCylinder(root.transform, "Base", materials.Wall, Vector3.zero, new Vector3(0.34f, 0.06f, 0.3f), Quaternion.identity, 6);
        AddCone(root.transform, "Body", materials.Wall, 0.22f, 0.18f, 1.05f, new Vector3(0f, 0.58f, 0f), Quaternion.identity, 8);
        AddCone(root.transform, "Roof", materials.Roof, 0.28f, 0.02f, 0.34f, new Vector3(0f, 1.28f, 0f), Quaternion.identity, 8);
        AddCube(root.transform, "Banner", materials.Flag, new Vector3(0.18f, 1.05f, -0.04f), new Vector3(0.28f, 0.18f, 0.035f), Quaternion.identity);
        return root;
    }

    private static GameObject CreateMine(string name, MaterialSet materials)
    {
        var root = NewRoot(name);
        AddCone(root.transform, "MineHill", materials.RockDark, 0.5f, 0.12f, 0.46f, new Vector3(0f, 0.23f, 0.03f), Quaternion.identity, 8);
        AddCube(root.transform, "Entrance", materials.Wood, new Vector3(0f, 0.18f, -0.28f), new Vector3(0.34f, 0.28f, 0.08f), Quaternion.identity);
        AddCube(root.transform, "RailA", materials.Wood, new Vector3(-0.07f, 0.035f, -0.48f), new Vector3(0.035f, 0.03f, 0.48f), Quaternion.identity);
        AddCube(root.transform, "RailB", materials.Wood, new Vector3(0.07f, 0.035f, -0.48f), new Vector3(0.035f, 0.03f, 0.48f), Quaternion.identity);
        AddRock(root.transform, "OreRockA", materials, 0.22f, new Vector3(-0.34f, 0.09f, 0.26f), Quaternion.Euler(8f, 22f, -4f));
        AddCube(root.transform, "OreHint", materials.Gold, new Vector3(0.32f, 0.12f, -0.18f), new Vector3(0.12f, 0.1f, 0.12f), Quaternion.Euler(15f, 30f, 8f));
        return root;
    }

    private static GameObject CreateWallSegment(string name, MaterialSet materials)
    {
        var root = NewRoot(name);
        AddCube(root.transform, "WallBody", materials.Wall, Vector3.up * 0.12f, new Vector3(1f, 1f, 1f), Quaternion.identity);
        AddCube(root.transform, "WallCap", materials.RockDark, Vector3.up * 0.31f, new Vector3(1.18f, 0.22f, 0.78f), Quaternion.identity);
        return root;
    }

    private static GameObject CreateWallTower(string name, MaterialSet materials)
    {
        var root = NewRoot(name);
        AddCone(root.transform, "Tower", materials.Wall, 0.22f, 0.18f, 0.46f, new Vector3(0f, 0.23f, 0f), Quaternion.identity, 8);
        return root;
    }

    private static GameObject CreateCoastMarker(string name, MaterialSet materials)
    {
        var root = NewRoot(name);
        AddCylinder(root.transform, "Pole", materials.Bark, new Vector3(0.12f, 0.36f, -0.18f), new Vector3(0.035f, 0.36f, 0.035f), Quaternion.identity);
        AddCube(root.transform, "Flag", materials.Flag, new Vector3(0.28f, 0.58f, -0.18f), new Vector3(0.34f, 0.2f, 0.035f), Quaternion.identity);
        return root;
    }

    private static GameObject CreateGrave(string name, MaterialSet materials, int seed)
    {
        var root = NewRoot(name);
        AddFacetedMound(root.transform, "Mound", materials.RockDark, 0.3f, 0.2f, 0.08f, Vector3.zero, YRot(seed, 1), seed);
        AddCube(root.transform, "Headstone", materials.Wall, new Vector3(0f, 0.13f, -0.22f), new Vector3(0.2f, 0.26f, 0.05f), Quaternion.Euler(-8f, Hash01(seed, 2, 3) * 20f - 10f, 0f));
        for (var i = 0; i < 4; i++)
        {
            var a = Mathf.PI * 2f * i / 4f + 0.4f;
            AddRock(root.transform, $"Border_{i}", materials, 0.08f, new Vector3(Mathf.Cos(a) * 0.26f, 0.03f, Mathf.Sin(a) * 0.2f), Quaternion.Euler(0f, a * 57f, 0f));
        }

        return root;
    }

    private static GameObject CreateAbandonedCamp(string name, MaterialSet materials, int seed)
    {
        var root = NewRoot(name);
        var tents = 2 + Mathf.FloorToInt(Hash01(seed, 0, 1) * 2f);
        for (var i = 0; i < tents; i++)
        {
            var a = Mathf.PI * 2f * i / tents + Hash01(seed, i, 2);
            AddCone(root.transform, $"Tent_{i}", materials.Cloth, 0.2f, 0.02f, 0.26f, new Vector3(Mathf.Cos(a) * 0.28f, 0.13f, Mathf.Sin(a) * 0.24f), Quaternion.Euler(0f, Hash01(seed, i, 3) * 360f, 0f), 4);
        }

        for (var i = 0; i < 5; i++)
        {
            var a = Mathf.PI * 2f * i / 5f;
            AddRock(root.transform, $"FireRing_{i}", materials, 0.05f, new Vector3(Mathf.Cos(a) * 0.1f, 0.02f, Mathf.Sin(a) * 0.1f), Quaternion.identity);
        }

        AddCube(root.transform, "LogA", materials.Charred, new Vector3(0f, 0.03f, 0f), new Vector3(0.14f, 0.03f, 0.03f), Quaternion.Euler(0f, 20f, 0f));
        AddCube(root.transform, "LogB", materials.Charred, new Vector3(0f, 0.03f, 0f), new Vector3(0.14f, 0.03f, 0.03f), Quaternion.Euler(0f, 110f, 0f));
        AddCube(root.transform, "Crate", materials.Wood, new Vector3(0.34f, 0.06f, 0.28f), new Vector3(0.12f, 0.12f, 0.12f), Quaternion.Euler(0f, 25f, 0f));
        return root;
    }

    private static GameObject CreateBrokenRavine(string name, MaterialSet materials, int seed)
    {
        var root = NewRoot(name);
        AddCube(root.transform, "Crack", materials.Charred, new Vector3(0f, -0.02f, 0f), new Vector3(0.92f, 0.08f, 0.28f), Quaternion.Euler(0f, Hash01(seed, 0, 1) * 40f - 20f, 0f));
        AddFacetedMound(root.transform, "EdgeA", materials.RockDark, 0.42f, 0.28f, 0.12f, new Vector3(0f, 0f, 0.28f), YRot(seed, 2), seed + 1);
        AddFacetedMound(root.transform, "EdgeB", materials.RockDark, 0.42f, 0.28f, 0.12f, new Vector3(0f, 0f, -0.28f), YRot(seed, 3), seed + 2);
        AddCube(root.transform, "PlankA", materials.Wood, new Vector3(-0.18f, 0.14f, 0.02f), new Vector3(0.34f, 0.03f, 0.12f), Quaternion.Euler(-6f, 8f, 0f));
        AddCube(root.transform, "PlankB", materials.Wood, new Vector3(0.2f, 0.13f, -0.02f), new Vector3(0.28f, 0.03f, 0.12f), Quaternion.Euler(7f, -6f, 0f));
        AddCylinder(root.transform, "PostA", materials.Wood, new Vector3(-0.32f, 0.1f, 0.14f), new Vector3(0.03f, 0.12f, 0.03f), Quaternion.identity);
        AddCylinder(root.transform, "PostB", materials.Wood, new Vector3(0.32f, 0.1f, -0.14f), new Vector3(0.03f, 0.12f, 0.03f), Quaternion.identity);
        return root;
    }

    private static GameObject CreateRuin(string name, MaterialSet materials, int seed)
    {
        var root = NewRoot(name);
        for (var i = 0; i < 5; i++)
        {
            var a = Mathf.PI * 0.5f + i * 0.5f;
            var h = Mathf.Lerp(0.12f, 0.34f, Hash01(seed, i, 1));
            AddCube(root.transform, $"Wall_{i}", materials.Wall, new Vector3(Mathf.Cos(a) * 0.3f, h * 0.5f, Mathf.Sin(a) * 0.3f), new Vector3(0.16f, h, 0.1f), Quaternion.Euler(Hash01(seed, i, 2) * 8f - 4f, a * 57f, Hash01(seed, i, 3) * 8f - 4f));
        }

        AddCone(root.transform, "Column", materials.Wall, 0.07f, 0.06f, 0.4f, new Vector3(-0.24f, 0.2f, 0.12f), Quaternion.Euler(12f, 0f, 8f), 8);
        for (var i = 0; i < 4; i++)
        {
            var a = Hash01(seed, i, 5) * Mathf.PI * 2f;
            AddRock(root.transform, $"Rubble_{i}", materials, 0.08f, new Vector3(Mathf.Cos(a) * 0.34f, 0.03f, Mathf.Sin(a) * 0.3f), Quaternion.Euler(0f, a * 57f, 0f));
        }

        return root;
    }

    private static GameObject CreateLandmark(string name, MaterialSet materials, int seed)
    {
        var root = NewRoot(name);
        const int stones = 5;
        for (var i = 0; i < stones; i++)
        {
            var a = Mathf.PI * 2f * i / stones;
            var h = Mathf.Lerp(0.3f, 0.5f, Hash01(seed, i, 1));
            var tilt = Hash01(seed, i, 2) * 10f - 5f;
            AddFacetedMound(root.transform, $"Stone_{i}", i % 2 == 0 ? materials.RockDark : materials.Rock, 0.09f, 0.07f, h, new Vector3(Mathf.Cos(a) * 0.3f, h * 0.5f, Mathf.Sin(a) * 0.3f), Quaternion.Euler(tilt, Hash01(seed, i, 3) * 360f, tilt * 0.5f), seed + i);
        }

        AddCube(root.transform, "Altar", materials.Rock, new Vector3(0f, 0.05f, 0f), new Vector3(0.18f, 0.08f, 0.14f), YRot(seed, 9));
        return root;
    }

    private static void AddFence(Transform parent, string name, MaterialSet materials, Vector3 localPosition, Quaternion localRotation)
    {
        var root = NewChild(parent, name, localPosition, localRotation, Vector3.one);
        for (var i = 0; i < 3; i++)
        {
            AddCube(root.transform, $"Plank_{i}", materials.Wood, new Vector3(-0.2f + i * 0.2f, 0f, 0f), new Vector3(0.16f, 0.08f, 0.035f), Quaternion.identity);
        }
    }

    private static void AddRock(Transform parent, string name, MaterialSet materials, float size, Vector3 localPosition, Quaternion localRotation)
    {
        AddCube(parent, name, materials.Rock, localPosition, new Vector3(size * 0.9f, size * 0.6f, size), localRotation);
    }

    private static void AddCube(Transform parent, string name, Material material, Vector3 localPosition, Vector3 localScale, Quaternion localRotation)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        SetupPrimitive(cube, parent, name, localPosition, localScale, localRotation, material);
    }

    private static void AddCylinder(Transform parent, string name, Material material, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, int sides = 0)
    {
        GameObject cylinder;
        if (sides > 0)
        {
            cylinder = NewChild(parent, name, localPosition, localRotation, localScale);
            var filter = cylinder.AddComponent<MeshFilter>();
            var renderer = cylinder.AddComponent<MeshRenderer>();
            filter.sharedMesh = PersistMesh(CylinderMesh(1f, 1f, 1f, sides));
            renderer.sharedMaterial = material;
            return;
        }

        cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        SetupPrimitive(cylinder, parent, name, localPosition, localScale, localRotation, material);
    }

    private static void AddCone(Transform parent, string name, Material material, float bottomRadius, float topRadius, float height, Vector3 localPosition, Quaternion localRotation, int sides = 6)
    {
        var cone = NewChild(parent, name, localPosition, localRotation, Vector3.one);
        var filter = cone.AddComponent<MeshFilter>();
        var renderer = cone.AddComponent<MeshRenderer>();
        filter.sharedMesh = PersistMesh(CylinderMesh(bottomRadius, topRadius, height, sides));
        renderer.sharedMaterial = material;
    }

    private static void AddFacetedPeak(Transform parent, string name, Material material, float baseRadius, float shoulderRadius, float tipRadius, float height, Vector3 localPosition, Quaternion localRotation, int seed)
    {
        var go = NewChild(parent, name, localPosition, localRotation, Vector3.one);
        var filter = go.AddComponent<MeshFilter>();
        var renderer = go.AddComponent<MeshRenderer>();
        filter.sharedMesh = PersistMesh(FacetedPeakMesh(baseRadius, shoulderRadius, tipRadius, height, seed));
        renderer.sharedMaterial = material;
    }

    private static void AddFacetedMound(Transform parent, string name, Material material, float baseRadius, float topRadius, float height, Vector3 localPosition, Quaternion localRotation, int seed)
    {
        var go = NewChild(parent, name, localPosition, localRotation, Vector3.one);
        var filter = go.AddComponent<MeshFilter>();
        var renderer = go.AddComponent<MeshRenderer>();
        filter.sharedMesh = PersistMesh(FacetedMoundMesh(baseRadius, topRadius, height, seed));
        renderer.sharedMaterial = material;
    }

    // Irregular faceted peak: wobbled base ring, offset shoulder ring, and an off-centre apex.
    // Built flat-shaded (unshared vertices) for crisp low-poly rock facets.
    private static Mesh FacetedPeakMesh(float baseRadius, float shoulderRadius, float tipRadius, float height, int seed)
    {
        const int segments = 7;
        var positions = new List<Vector3>();
        var indices = new List<int>();

        var shoulderY = height * 0.44f;
        var apexAngle = Hash01(seed, 17, 26097) * Mathf.PI * 2f;
        var apexOffset = tipRadius * Mathf.Lerp(0.15f, 0.55f, Hash01(seed, 19, 26099));
        positions.Add(new Vector3(Mathf.Cos(apexAngle) * apexOffset, height, Mathf.Sin(apexAngle) * apexOffset));

        for (var i = 0; i < segments; i++)
        {
            var angle = Mathf.PI * 2f * i / segments;
            var wobble = Mathf.Lerp(0.84f, 1.14f, Hash01(seed, i, 26000));
            var shoulderWobble = Mathf.Lerp(0.82f, 1.12f, Hash01(seed, i, 26031));
            positions.Add(new Vector3(Mathf.Cos(angle) * baseRadius * wobble, 0f, Mathf.Sin(angle) * baseRadius * wobble));
            positions.Add(new Vector3(Mathf.Cos(angle + 0.08f) * shoulderRadius * shoulderWobble, shoulderY, Mathf.Sin(angle + 0.08f) * shoulderRadius * shoulderWobble));
        }

        for (var i = 0; i < segments; i++)
        {
            var next = (i + 1) % segments;
            var baseA = 1 + i * 2;
            var shoulderA = baseA + 1;
            var baseB = 1 + next * 2;
            var shoulderB = baseB + 1;

            indices.Add(baseA); indices.Add(shoulderA); indices.Add(shoulderB);
            indices.Add(baseA); indices.Add(shoulderB); indices.Add(baseB);
            indices.Add(shoulderA); indices.Add(0); indices.Add(shoulderB);
        }

        return FlatMesh(positions, indices, "FacetedPeak");
    }

    // Faceted, flat-topped mound (truncated cone with wobble) for talus bases and boulders.
    private static Mesh FacetedMoundMesh(float baseRadius, float topRadius, float height, int seed)
    {
        const int segments = 8;
        var positions = new List<Vector3>();
        var indices = new List<int>();

        positions.Add(Vector3.up * height);

        for (var i = 0; i < segments; i++)
        {
            var angle = Mathf.PI * 2f * i / segments;
            var wobble = Mathf.Lerp(0.82f, 1.18f, Hash01(seed, i, 27000));
            var topWobble = Mathf.Lerp(0.8f, 1.13f, Hash01(seed, i, 27041));
            positions.Add(new Vector3(Mathf.Cos(angle) * baseRadius * wobble, 0f, Mathf.Sin(angle) * baseRadius * wobble));
            positions.Add(new Vector3(Mathf.Cos(angle + 0.12f) * topRadius * topWobble, height, Mathf.Sin(angle + 0.12f) * topRadius * topWobble));
        }

        for (var i = 0; i < segments; i++)
        {
            var next = (i + 1) % segments;
            var baseA = 1 + i * 2;
            var topA = baseA + 1;
            var baseB = 1 + next * 2;
            var topB = baseB + 1;

            indices.Add(baseA); indices.Add(topA); indices.Add(topB);
            indices.Add(baseA); indices.Add(topB); indices.Add(baseB);
            indices.Add(0); indices.Add(topB); indices.Add(topA);
        }

        return FlatMesh(positions, indices, "FacetedMound");
    }

    private static Mesh FlatMesh(List<Vector3> positions, List<int> indices, string meshName)
    {
        var vertices = new Vector3[indices.Count];
        var triangles = new int[indices.Count];
        for (var i = 0; i < indices.Count; i++)
        {
            vertices[i] = positions[indices[i]];
            triangles[i] = i;
        }

        var mesh = new Mesh { name = meshName };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static float Hash01(int a, int b, int c)
    {
        unchecked
        {
            var h = 2166136261u;
            h = (h ^ (uint)a) * 16777619u;
            h = (h ^ (uint)b) * 16777619u;
            h = (h ^ (uint)c) * 16777619u;
            return (h & 0x7fffffffu) / (float)0x7fffffff;
        }
    }

    private static void DeleteObsoletePrefab(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
        }
    }

    private static void PrepareMeshLibrary()
    {
        if (AssetDatabase.LoadAssetAtPath<Mesh>(MeshLibraryPath) != null)
        {
            AssetDatabase.DeleteAsset(MeshLibraryPath);
        }

        s_meshLibrary = new Mesh { name = "GeneratedMeshLibrary" };
        AssetDatabase.CreateAsset(s_meshLibrary, MeshLibraryPath);
    }

    // Registers a generated mesh as a sub-asset of the shared mesh library so prefabs that
    // reference it keep a valid, persisted mesh after SaveAsPrefabAsset.
    private static Mesh PersistMesh(Mesh mesh)
    {
        if (s_meshLibrary != null && !AssetDatabase.Contains(mesh))
        {
            AssetDatabase.AddObjectToAsset(mesh, s_meshLibrary);
        }

        return mesh;
    }

    private static GameObject NewRoot(string name)
    {
        return new GameObject(name);
    }

    private static GameObject NewChild(Transform parent, string name, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localPosition;
        child.transform.localRotation = localRotation;
        child.transform.localScale = localScale;
        return child;
    }

    private static void SetupPrimitive(GameObject primitive, Transform parent, string name, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material)
    {
        primitive.name = name;
        primitive.transform.SetParent(parent, false);
        primitive.transform.localPosition = localPosition;
        primitive.transform.localRotation = localRotation;
        primitive.transform.localScale = localScale;
        primitive.GetComponent<MeshRenderer>().sharedMaterial = material;
        Object.DestroyImmediate(primitive.GetComponent<Collider>());
    }

    private static GameObject SavePrefab(string path, GameObject root)
    {
        EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static Material Material(string name, string hex, float smoothness)
    {
        var path = $"{MaterialRoot}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader) { name = name, color = ColorFromHex(hex) };
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = ColorFromHex(hex);
        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", Mathf.Clamp01(1f - smoothness));
        }

        if (material.HasProperty("_Cull"))
        {
            material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material TransparentMaterial(string name, string hex, float alpha)
    {
        var material = Material(name, hex, 0.35f);
        var color = material.color;
        color.a = alpha;
        material.color = color;
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        return material;
    }

    private static Mesh CylinderMesh(float bottomRadius, float topRadius, float height, int segments)
    {
        var mesh = new Mesh { name = "GeneratedCylinder" };
        var vertices = new Vector3[segments * 4 + segments * 3 + segments * 3];
        var triangles = new int[segments * 12];
        var v = 0;
        var t = 0;
        var half = height * 0.5f;

        for (var i = 0; i < segments; i++)
        {
            var a0 = Mathf.PI * 2f * i / segments;
            var a1 = Mathf.PI * 2f * (i + 1) / segments;
            var b0 = new Vector3(Mathf.Cos(a0) * bottomRadius, -half, Mathf.Sin(a0) * bottomRadius);
            var b1 = new Vector3(Mathf.Cos(a1) * bottomRadius, -half, Mathf.Sin(a1) * bottomRadius);
            var p0 = new Vector3(Mathf.Cos(a0) * topRadius, half, Mathf.Sin(a0) * topRadius);
            var p1 = new Vector3(Mathf.Cos(a1) * topRadius, half, Mathf.Sin(a1) * topRadius);

            vertices[v] = p0;
            vertices[v + 1] = p1;
            vertices[v + 2] = b0;
            vertices[v + 3] = b1;
            triangles[t++] = v;
            triangles[t++] = v + 1;
            triangles[t++] = v + 2;
            triangles[t++] = v + 2;
            triangles[t++] = v + 1;
            triangles[t++] = v + 3;
            v += 4;

            vertices[v] = Vector3.up * half;
            vertices[v + 1] = p1;
            vertices[v + 2] = p0;
            triangles[t++] = v;
            triangles[t++] = v + 1;
            triangles[t++] = v + 2;
            v += 3;

            vertices[v] = Vector3.down * half;
            vertices[v + 1] = b0;
            vertices[v + 2] = b1;
            triangles[t++] = v;
            triangles[t++] = v + 1;
            triangles[t++] = v + 2;
            v += 3;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Color ColorFromHex(string hex)
    {
        if (ColorUtility.TryParseHtmlString("#" + hex, out var color))
        {
            return color;
        }

        return Color.magenta;
    }

    private static void EnsureFolder(string parent, string child)
    {
        var path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static void EnsureFolder(string path)
    {
        if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        var child = Path.GetFileName(path);
        EnsureFolder(parent);
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private sealed class MaterialSet
    {
        public Material Bark;
        public Material Leaf;
        public Material LeafDark;
        public Material Rock;
        public Material RockDark;
        public Material RockLight;
        public Material RockWarm;
        public Material Snow;
        public Material SnowShadow;
        public Material GroundDark;
        public Material Wall;
        public Material Roof;
        public Material RoofWarm;
        public Material Wood;
        public Material Flag;
        public Material Gold;
        public Material Cloth;
        public Material Charred;
        public Material Smoke;
    }
}
