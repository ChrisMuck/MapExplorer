using UnityEngine;

[CreateAssetMenu(menuName = "Hex Map/Prefab Library", fileName = "HexMapPrefabLibrary")]
public sealed class HexMapPrefabLibrary : ScriptableObject
{
    [Header("Forest")]
    public GameObject[] forestClusterPrefabs;
    public GameObject[] treePrefabs;

    [Header("Forest Types")]
    [Tooltip("Cluster prefabs for coniferous-only forests (pines).")]
    public GameObject[] coniferousForestClusterPrefabs;
    [Tooltip("Cluster prefabs for deciduous-only forests (broadleaf trees).")]
    public GameObject[] deciduousForestClusterPrefabs;
    [Tooltip("Cluster prefabs for mixed forests (pines + broadleaf).")]
    public GameObject[] mixedForestClusterPrefabs;
    [Tooltip("Individual pine (coniferous) tree prefabs.")]
    public GameObject[] pineTreePrefabs;
    [Tooltip("Individual broadleaf (deciduous) tree prefabs.")]
    public GameObject[] broadleafTreePrefabs;

    [Header("Mountains")]
    public GameObject[] mountainPeakPrefabs;
    public GameObject[] snowyMountainPeakPrefabs;
    public GameObject[] rockyRidgePrefabs;
    public GameObject[] snowyRockyRidgePrefabs;
    public GameObject[] foothillsPrefabs;
    public GameObject[] foothillRockPrefabs;
    public GameObject[] rockPrefabs;

    [Header("Settlements")]
    public GameObject[] settlementPrefabs;
    public GameObject[] settlementHousePrefabs;
    public GameObject[] settlementFencePrefabs;

    [Header("Special Places")]
    public GameObject[] towerPrefabs;
    public GameObject[] minePrefabs;
    public GameObject[] wallSegmentPrefabs;
    public GameObject[] wallTowerPrefabs;
    public GameObject[] coastMarkerPrefabs;
}
