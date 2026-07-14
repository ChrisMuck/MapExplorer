using System.Collections.Generic;
using Game.App;
using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public sealed class UnityHexMapView : MonoBehaviour
{
    [Range(4, 80)] public int mapWidth = 40;
    [Range(4, 60)] public int mapHeight = 30;
    [Range(0.5f, 2f)] public float hexSize = 1f;
    public int mapSeed = 4711;
    [Range(3f, 30f)] public float zoomedInSize = 5f;
    [Range(8f, 60f)] public float zoomedOutSize = 24f;
    [Range(0.5f, 8f)] public float zoomSpeed = 3.5f;
    [Range(2f, 40f)] public float cameraPanSpeed = 14f;
    [Range(0.1f, 3f)] public float cameraDragPanSpeed = 1f;
    public bool centerCameraOnExpeditionAfterMove = true;
    public bool showDebugHexGrid = false;
    public bool showDebugFactionOwnership = false;
    public bool showKnowledgeFog = true;
    public bool showLegacyOnGui = false;
    public bool showSelectionPreview = true;
    public Vector2Int selectedPreviewHex = Vector2Int.zero;
    [Range(0, 8)] public int reachablePreviewRadius = 2;
    [SerializeField] private bool useCoreTutorialState = true;
    [SerializeField] private bool usePrefabOverrides = true;
    [SerializeField] private bool autoRebuildInEditMode = false;
    [SerializeField] private HexMapPrefabLibrary prefabLibrary;

    private const float Sqrt3 = 1.73205080757f;
    private const float VisualTileTopY = 0.08f;
    private const float VisualTileBottomY = -0.035f;
    private readonly Dictionary<Vector2Int, HexTileVisual> tiles = new();
    private readonly Dictionary<Vector2Int, List<GameObject>> featureObjectsByCoord = new();
    private readonly Dictionary<GameObject, List<Vector2Int>> featureObjectsByPath = new();
    private readonly Dictionary<string, Material> featureMaterials = new();
    private readonly Dictionary<string, Mesh> generatedMeshCache = new();
    private Transform cameraRig;
    private Camera strategyCamera;
    private float activeZoomedOutSize;
    private GameState coreGameState;
    private WorldGenerationRequest generatedCampaignRequest;
    // Game.Core bounds require non-negative Q values. WorldGen axial coordinates are therefore
    // shifted in the bridge; presentation restores that shift before placing hexes.
    private int generatedCoreQOffset;
    private readonly MovementCostService movementCostService = new MovementCostService();
    private readonly GameApplication gameApplication = CreateGameApplication();
    private Transform currentBuildRoot;
    private Material hexTerrainMaterial;
    private Material hexNatureMaterialTemplate;
    private Texture2D hexTerrainNoiseTexture;
    private HexDecorationLibrary hexDecorationLibrary;
    private HexMapChunkRenderer hexMapChunkRenderer;
    private Transform terrainRoot;
    private Transform featureRoot;
    private Transform systemsRoot;
    private Transform lightingRoot;
    private Transform hudRoot;
    private Transform overlayRoot;
    private Transform playerAnnotationRoot;
    private Transform expeditionMarker;
    private ExpeditionScreenController expeditionScreenController;
    private BaseCampScreenController baseCampScreenController;
    private bool warnedMissingPrefabLibrary;
    private Vector3 lastPanMousePosition;
    private bool isDraggingPan;
    private Vector3 rightInspectStartMousePosition;
    private Vector2Int rightInspectStartHex;
    private bool hasRightInspectStart;
    private readonly HashSet<string> autoOpenedLocationInteractionIds = new();
    private bool hasHoverPreview;
    private Vector2Int hoverPreviewHex;
    private bool hasInspectedHex;
    private Vector2Int inspectedHex;
    private string interactionMessage = "Ready.";
    private SpecialLocationState inspectedLocation;
    private PlayerMapMarkerKind selectedMarkerKind = PlayerMapMarkerKind.Question;
    private string markerLabelDraft = "";
    private string markerFactionIdDraft = "border-wardens";
    private string noteDraftText = "";
    private ScoutDirection scoutDirection = ScoutDirection.East;
    private int scoutDurationDays = 2;
    private ScoutMissionFocus scoutFocus = ScoutMissionFocus.Survey;
    private ScoutMissionBehavior scoutBehavior = ScoutMissionBehavior.Balanced;
    private bool sendTwoScouts = false;
    private HexMapPrefabLibrary Prefabs => prefabLibrary;
#if UNITY_EDITOR
    private bool editorRebuildQueued;
#endif

    private static readonly Vector2Int[] Directions =
    {
        new(1, 0),
        new(1, -1),
        new(0, -1),
        new(-1, 0),
        new(-1, 1),
        new(0, 1)
    };

    public GameState CurrentGameState => coreGameState;
    public bool HasStartedCampaign => generatedCampaignRequest != null;
    public WorldGenerationPresetCatalog? WorldGenerationCatalog => gameApplication.DataCatalog?.WorldGeneration;

    public string CurrentInteractionMessage => interactionMessage;

    public bool HasInspectedHex => hasInspectedHex;

    public HexCoord CurrentSelectedCoreCoord => ViewCoordToCoreCoord(hasInspectedHex ? inspectedHex : selectedPreviewHex);

    public KnowledgeLevel GetKnowledgeForUi(HexCoord coord)
    {
        return coreGameState == null ? KnowledgeLevel.Unknown : coreGameState.Knowledge.GetTileKnowledge(coord);
    }

    public bool TryGetTileForUi(HexCoord coord, out HexTileState tile)
    {
        tile = null;
        if (coreGameState == null)
        {
            return false;
        }

        return coreGameState.World.Map.TryGetTile(coord, out tile);
    }

    public SpecialLocationState GetLocationForUi(HexCoord coord)
    {
        return FindLocation(coord);
    }

    public IReadOnlyList<PlayerMapMarkerState> GetMarkersForUi(HexCoord coord)
    {
        var result = new List<PlayerMapMarkerState>();
        if (coreGameState == null)
        {
            return result;
        }

        foreach (var marker in coreGameState.PlayerNotes.Markers)
        {
            if (marker.Coord == coord)
            {
                result.Add(marker);
            }
        }

        return result;
    }

    public IReadOnlyList<PlayerMapNoteState> GetNotesForUi(HexCoord coord)
    {
        var result = new List<PlayerMapNoteState>();
        if (coreGameState == null)
        {
            return result;
        }

        foreach (var note in coreGameState.PlayerNotes.Notes)
        {
            if (note.Coord == coord)
            {
                result.Add(note);
            }
        }

        return result;
    }

    public LostExpeditionRecord GetLostExpeditionForUi(HexCoord coord)
    {
        if (coreGameState == null)
        {
            return null;
        }

        foreach (var record in coreGameState.Base.LostExpeditions)
        {
            if (record.LastKnownPosition == coord && record.Status != LostExpeditionStatus.FullyResolved)
            {
                return record;
            }
        }

        return null;
    }

    public bool IsExpeditionAtUi(HexCoord coord)
    {
        return coreGameState != null && coreGameState.Expedition.Position == coord;
    }

    public bool IsAtBaseForUi()
    {
        return coreGameState != null && coreGameState.Expedition.Position == coreGameState.Base.Location;
    }

    public IReadOnlyList<BaseMemberState> GetRosterForUi()
    {
        return coreGameState == null
            ? System.Array.Empty<BaseMemberState>()
            : coreGameState.Roster.Members;
    }

    public IReadOnlyList<BaseUpgradeState> GetUpgradesForUi()
    {
        return coreGameState == null
            ? System.Array.Empty<BaseUpgradeState>()
            : coreGameState.Base.Upgrades.Upgrades;
    }

    public IReadOnlyList<ArchiveEntryState> GetArchiveForUi()
    {
        return coreGameState == null
            ? System.Array.Empty<ArchiveEntryState>()
            : coreGameState.Base.Archive;
    }

    public void RequestStartUpgradeFromUi(string upgradeId)
    {
        if (coreGameState == null)
        {
            return;
        }

        var result = gameApplication.StartUpgrade(coreGameState, upgradeId);
        interactionMessage = result.Success
            ? result.ArchiveEntry ?? "Base upgrade built."
            : result.Error ?? "Base upgrade rejected.";
        RefreshHud();
        RefreshToolkitHud();
    }

    public EvaluationQueueState GetEvaluationQueueForUi()
    {
        return coreGameState == null ? new EvaluationQueueState() : coreGameState.Base.EvaluationQueue;
    }

    public void RequestEvaluateKnowledgeItemFromUi(string itemId)
    {
        if (coreGameState == null)
        {
            return;
        }

        var result = gameApplication.EvaluateKnowledgeItem(coreGameState, itemId);
        interactionMessage = result.Success
            ? result.ArchiveEntry ?? "Knowledge evaluated."
            : result.Error ?? "Evaluation rejected.";
        RefreshHud();
        RefreshToolkitHud();
    }

    public void RequestEndDayFromUi()
    {
        EndCurrentDay();
        RefreshToolkitHud();
    }

    public void RequestCompleteExpeditionFromUi()
    {
        CompleteCurrentExpedition();
        RefreshToolkitHud();
    }

    public void RequestStartBaseActionFromUi(BaseActionKind kind, string memberId = null)
    {
        if (coreGameState == null)
        {
            return;
        }

        var result = gameApplication.StartBaseAction(coreGameState, kind, memberId);
        interactionMessage = result.Success
            ? result.ArchiveEntry ?? "Base action applied."
            : result.Error ?? "Base action rejected.";
        RefreshKnowledgeOverlays();
        UpdateFeatureVisibility();
        RefreshHexOverlays();
        RefreshPlayerAnnotations();
        RefreshHud();
        RefreshToolkitHud();
    }

    public void RequestAdvanceBaseTimeFromUi()
    {
        AdvanceCurrentBaseTime();
        RefreshToolkitHud();
    }

    public void RequestOpenBaseCampFromUi()
    {
        if (baseCampScreenController == null)
        {
            baseCampScreenController = FindObjectOfType<BaseCampScreenController>(true);
        }

        if (baseCampScreenController == null)
        {
            Debug.LogWarning("Base camp screen is not in the scene. Add a GameObject with a UI Document (Source Asset = BaseCampScreen.uxml, its own Panel Settings with a HIGHER sort order than the expedition screen) and a BaseCampScreenController component. See Assets/UI/BaseCampScreen.README.md.");
            interactionMessage = "Basislager-Fenster fehlt in der Szene (siehe BaseCampScreen.README.md).";
            RefreshToolkitHud();
            return;
        }

        baseCampScreenController.Open();
    }

    public void SetExpeditionScreenVisible(bool visible)
    {
        if (expeditionScreenController == null)
        {
            expeditionScreenController = FindObjectOfType<ExpeditionScreenController>();
        }

        expeditionScreenController?.SetScreenVisible(visible);
    }

    public void RequestStartNewExpeditionFromUi(IReadOnlyList<string> memberIds)
    {
        if (coreGameState == null)
        {
            return;
        }

        var result = gameApplication.StartNewExpedition(coreGameState, memberIds);
        if (!result.Success)
        {
            interactionMessage = result.Error ?? "New expedition rejected.";
            RefreshHud();
            RefreshToolkitHud();
            return;
        }

        selectedPreviewHex = CoreCoordToViewCoord(coreGameState.Expedition.Position);
        hasInspectedHex = false;
        interactionMessage = $"Expedition {result.ExpeditionNumber} gestartet.";
        RefreshKnowledgeOverlays();
        UpdateFeatureVisibility();
        RefreshHexOverlays();
        RefreshPlayerAnnotations();
        UpdateExpeditionMarkerPosition();
        RefreshHud();
        RefreshToolkitHud();
    }

    public BaseUnitStockState GetUnitStockForUi()
    {
        return coreGameState == null ? new BaseUnitStockState() : coreGameState.Base.UnitStock;
    }

    public ExpeditionReadiness ComputeReadinessForUi(int memberCount, IReadOnlyList<string> unitIds, int rations, int medicine)
    {
        if (coreGameState == null)
        {
            return ExpeditionReadiness.Compute(memberCount, System.Array.Empty<BaseUnitState>(), rations, medicine);
        }

        return gameApplication.ComputeReadiness(coreGameState, memberCount, unitIds, rations, medicine);
    }

    public void RequestStartLoadoutExpeditionFromUi(IReadOnlyList<string> memberIds, IReadOnlyList<string> unitIds, int rations, int medicine)
    {
        if (coreGameState == null)
        {
            return;
        }

        var result = gameApplication.StartNewExpedition(coreGameState, memberIds, unitIds, rations, medicine);
        if (!result.Success)
        {
            interactionMessage = result.Error ?? "New expedition rejected.";
            RefreshHud();
            RefreshToolkitHud();
            return;
        }

        selectedPreviewHex = CoreCoordToViewCoord(coreGameState.Expedition.Position);
        hasInspectedHex = false;
        interactionMessage = $"Expedition {result.ExpeditionNumber} gestartet.";
        RefreshKnowledgeOverlays();
        UpdateFeatureVisibility();
        RefreshHexOverlays();
        RefreshPlayerAnnotations();
        UpdateExpeditionMarkerPosition();
        RefreshHud();
        RefreshToolkitHud();
    }

    public void RequestOpenScoutReportFromUi(int reportIndex)
    {
        if (coreGameState == null ||
            reportIndex < 0 ||
            reportIndex >= coreGameState.Knowledge.ScoutReports.Count)
        {
            return;
        }

        var report = coreGameState.Knowledge.ScoutReports[reportIndex];
        if (report.RelatedCoords.Count == 0)
        {
            interactionMessage = $"Scout report selected: {report.Title}. No map coordinates were reported.";
            RefreshHud();
            return;
        }

        var coord = report.RelatedCoords[0];
        var viewCoord = CoreCoordToViewCoord(coord);
        inspectedHex = viewCoord;
        selectedPreviewHex = viewCoord;
        hasInspectedHex = true;
        inspectedLocation = coreGameState.Knowledge.GetTileKnowledge(coord) == KnowledgeLevel.Confirmed ? FindLocation(coord) : null;
        interactionMessage = $"Scout report selected: {report.Title}. Reported field {coord}.";
        FocusCameraOnCoord(viewCoord);
        RefreshHexOverlays();
        RefreshHud();
    }

    public void RequestSendScoutMissionFromUi()
    {
        SendScoutMissionFromHud();
        RefreshToolkitHud();
    }

    public bool RequestSendScoutMissionFromUi(
        IReadOnlyList<string> scoutMemberIds,
        ScoutDirection direction,
        int durationDays,
        ScoutMissionFocus focus,
        ScoutMissionBehavior behavior)
    {
        if (coreGameState == null)
        {
            return false;
        }

        var result = gameApplication.SendScoutMission(coreGameState, scoutMemberIds, direction, durationDays, focus, behavior);
        if (!result.Success)
        {
            interactionMessage = result.Error ?? "Scout mission rejected.";
            RefreshToolkitHud();
            return false;
        }

        var scoutText = result.Mission!.ScoutMemberIds.Count == 1 ? "Scout" : "Scouts";
        interactionMessage = $"{scoutText} sent {result.Mission.Direction} for {result.Mission.DurationDays} day(s). Expected return day {result.Mission.ExpectedReturnWorldDay}.";
        RefreshHud();
        RefreshToolkitHud();
        return true;
    }

    public IReadOnlyList<ExpeditionMemberState> GetAvailableScoutsForUi()
    {
        var scouts = new List<ExpeditionMemberState>();
        if (coreGameState == null)
        {
            return scouts;
        }

        if (coreGameState.Expedition.Status != ExpeditionStatus.Active)
        {
            return scouts;
        }

        foreach (var member in coreGameState.Expedition.Members)
        {
            if (member.Role == ExpeditionMemberRole.Scout && member.Status == ExpeditionMemberStatus.Available)
            {
                scouts.Add(member);
            }
        }

        return scouts;
    }

    public void RequestAddMarkerFromUi()
    {
        EnsureUiSelectedHex();
        if (string.IsNullOrWhiteSpace(markerLabelDraft))
        {
            markerLabelDraft = DefaultMarkerLabel(selectedMarkerKind);
        }

        AddMarkerToInspectedHex();
        RefreshToolkitHud();
    }

    public void RequestMarkerFromLatestReportFromUi()
    {
        EnsureUiSelectedHex();
        if (coreGameState != null && coreGameState.Knowledge.ScoutReports.Count > 0)
        {
            var report = coreGameState.Knowledge.ScoutReports[coreGameState.Knowledge.ScoutReports.Count - 1];
            if (report.RelatedCoords.Count > 0)
            {
                inspectedHex = CoreCoordToViewCoord(report.RelatedCoords[0]);
                hasInspectedHex = true;
            }

            selectedMarkerKind = PlayerMapMarkerKind.Question;
            markerLabelDraft = report.Title;
        }

        AddMarkerToInspectedHex();
        RefreshToolkitHud();
    }

    public void RequestMarkerFromReportHintFromUi(int reportIndex, int hintIndex)
    {
        if (coreGameState == null || reportIndex < 0 || reportIndex >= coreGameState.Knowledge.ScoutReports.Count)
        {
            return;
        }

        var report = coreGameState.Knowledge.ScoutReports[reportIndex];
        if (report.RelatedCoords.Count > 0)
        {
            inspectedHex = CoreCoordToViewCoord(report.RelatedCoords[0]);
            hasInspectedHex = true;
        }
        else
        {
            EnsureUiSelectedHex();
        }

        var hint = hintIndex >= 0 && hintIndex < report.Hints.Count ? report.Hints[hintIndex] : report.Title;
        selectedMarkerKind = MarkerKindForReportHint(hint);
        markerLabelDraft = hint;
        AddMarkerToInspectedHex();
        RefreshToolkitHud();
    }

    public void RequestAddNoteFromUi()
    {
        EnsureUiSelectedHex();
        if (string.IsNullOrWhiteSpace(noteDraftText))
        {
            noteDraftText = "Notiz aus dem Expeditionsbuch.";
        }

        AddNoteToInspectedHex();
        RefreshToolkitHud();
    }

    public void RequestInspectSelectedLocationFromUi()
    {
        EnsureUiSelectedHex();
        if (coreGameState == null)
        {
            return;
        }

        var coord = ViewCoordToCoreCoord(inspectedHex);
        if (GetLostExpeditionForUi(coord) != null)
        {
            var recovery = gameApplication.RecoverLostExpedition(coreGameState, coord);
            if (recovery.Success)
            {
                var expeditionNumber = recovery.Record == null ? 0 : recovery.Record.ExpeditionNumber;
                interactionMessage = $"Recovered traces of Expedition {expeditionNumber}. Field knowledge +{recovery.RecoveredKnowledge}.";
                RefreshKnowledgeOverlays();
                RefreshHexOverlays();
                RefreshToolkitHud();
                return;
            }

            interactionMessage = recovery.Error ?? "Recovery rejected.";
            RefreshToolkitHud();
            return;
        }

        var result = gameApplication.InspectLocation(coreGameState, coord);
        if (result.Success)
        {
            inspectedLocation = result.Location;
            interactionMessage = result.Message;
            RefreshKnowledgeOverlays();
            RefreshHexOverlays();
            RefreshToolkitHud();
            return;
        }

        interactionMessage = result.Error ?? "Nothing to inspect here.";
        RefreshToolkitHud();
    }

    // Unity uses the same complete declared data catalog as tests and the future simulation runner.
    // Invalid declared content is a startup error; silently switching to fallback definitions would
    // make player behaviour differ from the content that was validated elsewhere.
    private static GameApplication CreateGameApplication()
    {
        var root = System.IO.Path.Combine(Application.streamingAssetsPath, "GameData");
        var catalog = GameDataCatalog.LoadFromDirectory(root)
            ?? throw new System.InvalidOperationException($"Game data folder '{root}' was not found.");
        return new GameApplication(catalog);
    }

    /// <summary>Starts one unseen generated campaign. This path intentionally offers no map preview or reroll.</summary>
    public void RequestStartGeneratedCampaignFromUi(WorldGenerationRequest request)
    {
        if (request == null)
        {
            return;
        }

        generatedCampaignRequest = request;
        generatedCoreQOffset = 0;
        useCoreTutorialState = true;
        Rebuild();
        interactionMessage = "Eine unbekannte Welt wurde vorbereitet. Die Expedition beginnt an der Küste.";
        RefreshToolkitHud();
    }

    public LocationInteractionQueryResult GetLocationInteractionForUi(string locationId)
    {
        if (coreGameState == null)
        {
            return LocationInteractionQueryResult.Rejected("No active game state.");
        }

        return gameApplication.GetLocationInteraction(coreGameState, locationId);
    }

    public LocationActionResult RequestResolveLocationActionFromUi(string locationId, string actionId)
    {
        if (coreGameState == null)
        {
            return LocationActionResult.Rejected("No active game state.");
        }

        var result = gameApplication.ResolveLocationAction(coreGameState, locationId, actionId);
        if (!result.Success)
        {
            interactionMessage = result.Error ?? "Location action rejected.";
            RefreshToolkitHud();
            return result;
        }

        if (result.ExpeditionMoved)
        {
            selectedPreviewHex = CoreCoordToViewCoord(coreGameState.Expedition.Position);
            hasInspectedHex = false;
            inspectedLocation = null;
            UpdateExpeditionMarkerPosition();
        }
        else if (result.Location != null)
        {
            inspectedHex = CoreCoordToViewCoord(result.Location.Coord);
            hasInspectedHex = true;
            inspectedLocation = result.Location;
        }

        interactionMessage = LocationActionMessage(result);
        RefreshKnowledgeOverlays();
        UpdateFeatureVisibility();
        RefreshHexOverlays();
        RefreshPlayerAnnotations();
        RefreshHud();
        RefreshToolkitHud();
        return result;
    }

    public LocationActionResult RequestAdvanceLocationProjectFromUi(string locationId)
    {
        if (coreGameState == null)
        {
            return LocationActionResult.Rejected("No active game state.");
        }

        var result = gameApplication.AdvanceLocationProject(coreGameState, locationId);
        if (!result.Success)
        {
            interactionMessage = result.Error ?? "Location project rejected.";
            RefreshToolkitHud();
            return result;
        }

        if (result.Location != null)
        {
            inspectedHex = CoreCoordToViewCoord(result.Location.Coord);
            hasInspectedHex = true;
            inspectedLocation = result.Location;
        }

        interactionMessage = LocationActionMessage(result);
        RefreshKnowledgeOverlays();
        UpdateFeatureVisibility();
        RefreshHexOverlays();
        RefreshPlayerAnnotations();
        RefreshHud();
        RefreshToolkitHud();
        return result;
    }

    public void RequestPrepareSuppliesWithKnowledgeFromUi()
    {
        if (coreGameState == null)
        {
            return;
        }

        var result = gameApplication.PrepareSuppliesWithKnowledge(coreGameState);
        if (!result.Success)
        {
            interactionMessage = result.Error ?? "Preparation rejected.";
            RefreshToolkitHud();
            return;
        }

        interactionMessage = $"Vorbereitung gekauft: +{result.SupplyBonus} Vorraete fuer die naechste Expedition. Wissen verbleibend: {result.RemainingKnowledgePoints}.";
        RefreshToolkitHud();
    }

    public void RequestResolveEventFromUi(string eventId, string optionId)
    {
        if (coreGameState == null)
        {
            return;
        }

        var result = gameApplication.ResolveEvent(coreGameState, eventId, optionId);
        if (!result.Success)
        {
            interactionMessage = result.Error ?? "Event option rejected.";
            RefreshToolkitHud();
            return;
        }

        interactionMessage = result.Message ?? "Event resolved.";
        RefreshPlayerAnnotations();
        RefreshToolkitHud();
    }

    public void RequestPurchaseFactionOfferFromUi(string offerId)
    {
        if (coreGameState == null)
        {
            return;
        }

        var result = gameApplication.PurchaseFactionOffer(coreGameState, offerId);
        if (!result.Success)
        {
            interactionMessage = result.Error ?? "Faction offer rejected.";
            RefreshToolkitHud();
            return;
        }

        interactionMessage = result.Message ?? "Faction offer accepted.";
        RefreshKnowledgeOverlays();
        RefreshPlayerAnnotations();
        RefreshToolkitHud();
    }

    public void RequestCloseFactionInteractionFromUi()
    {
        if (coreGameState == null)
        {
            return;
        }

        var result = gameApplication.CloseFactionInteraction(coreGameState);
        interactionMessage = result.Success
            ? result.Message ?? "Faction contact closed."
            : result.Error ?? "No faction contact open.";
        RefreshToolkitHud();
    }

    public void RefreshToolkitHud()
    {
        expeditionScreenController?.Refresh();
    }

    private void EnsureUiSelectedHex()
    {
        if (hasInspectedHex)
        {
            return;
        }

        inspectedHex = selectedPreviewHex;
        hasInspectedHex = true;
        inspectedLocation = coreGameState == null ? null : FindLocation(ViewCoordToCoreCoord(inspectedHex));
    }

    private void OnEnable()
    {
        if (Application.isPlaying || autoRebuildInEditMode)
        {
            RequestRebuild();
        }
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (Application.isPlaying || autoRebuildInEditMode)
        {
            RequestRebuild();
        }
    }

    private void Update()
    {
        // Decoration is manually GPU-instanced (not real MeshRenderers), so it must be resubmitted
        // every frame/repaint regardless of play mode, the same way terrain/feature GameObjects are
        // already rendered passively by Unity in Edit mode.
        hexMapChunkRenderer?.Present(strategyCamera);

        if (!Application.isPlaying)
        {
            return;
        }

        UpdateHoverPreview();
        HandleMapInput();
        UpdateCameraPan();
        UpdateCameraZoom();
    }

    private void OnGUI()
    {
        if (!showLegacyOnGui || !Application.isPlaying || !useCoreTutorialState || coreGameState == null)
        {
            return;
        }

        var oldColor = GUI.color;
        var oldBackground = GUI.backgroundColor;
        GUI.color = ColorFromHex("f1ead5");
        var panelColor = ColorFromHex("26302a");
        panelColor.a = 0.88f;
        GUI.backgroundColor = panelColor;

        GUILayout.BeginArea(new Rect(16f, 16f, 720f, 326f), GUI.skin.box);
        GUILayout.BeginHorizontal();
        GUILayout.Label($"World Day {coreGameState.World.WorldDay}", GUILayout.Width(105f));
        GUILayout.Label($"Expedition Day {coreGameState.Expedition.ExpeditionDay}", GUILayout.Width(135f));
        GUILayout.Label($"MP {coreGameState.Expedition.MovementPoints}/{coreGameState.Expedition.MaxMovementPoints}", GUILayout.Width(80f));
        GUILayout.Label($"Supplies {coreGameState.Expedition.Supplies}", GUILayout.Width(95f));
        GUILayout.Label($"Morale {coreGameState.Expedition.Morale}", GUILayout.Width(80f));
        GUILayout.EndHorizontal();

        GUILayout.Space(8f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("End Day", GUILayout.Width(110f), GUILayout.Height(34f)))
        {
            EndCurrentDay();
        }

        GUILayout.Label("Move: left-click reachable hex   Inspect/select: right-click or left-click non-reachable known hex   Pan: WASD/arrows or right/middle drag   Zoom: mouse wheel");
        GUILayout.EndHorizontal();
        GUILayout.Space(8f);
        GUILayout.Label(GetHudInteractionText());
        GUILayout.Space(8f);
        DrawAnnotationControls();
        GUILayout.Space(8f);
        DrawScoutControls();
        GUILayout.EndArea();

        GUI.color = oldColor;
        GUI.backgroundColor = oldBackground;
    }

    [ContextMenu("Rebuild Hex Map")]
    public void Rebuild()
    {
        // The renderer's root is a generated child and is cleaned up together with
        // the rest of the presentation below. Dropping this reference first keeps
        // a subsequent rebuild from trying to refresh an old world.
        hexMapChunkRenderer?.Dispose();
        hexMapChunkRenderer = null;
        ClearGeneratedChildren();
        ClearGeneratedRuntimeAssets();
        tiles.Clear();
        featureObjectsByCoord.Clear();
        featureObjectsByPath.Clear();
        strategyCamera = null;
        cameraRig = null;
        currentBuildRoot = null;
        terrainRoot = null;
        featureRoot = null;
        systemsRoot = null;
        lightingRoot = null;
        hudRoot = null;
        overlayRoot = null;
        playerAnnotationRoot = null;
        expeditionMarker = null;
        expeditionScreenController = null;
        baseCampScreenController = null;
        hasHoverPreview = false;
        hasInspectedHex = false;
        inspectedLocation = null;
        interactionMessage = "Ready.";
        EnsurePrefabLibrary();

        CreateMaterials();
        terrainRoot = NewRootGroup("Terrain");
        currentBuildRoot = terrainRoot;
        BuildMap();

        hexDecorationLibrary = new HexDecorationLibrary();
        hexMapChunkRenderer = new HexMapChunkRenderer(
            terrainRoot,
            prefabLibrary,
            usePrefabOverrides,
            hexDecorationLibrary,
            hexTerrainMaterial,
            featureMaterials["UnknownFog"],
            featureMaterials["ReportedFog"],
            hexNatureMaterialTemplate,
            hexSize,
            VisualTileTopY,
            VisualTileTopY + 0.115f,
            zoomedOutSize * 0.6f);
        hexMapChunkRenderer.Build(tiles, ActiveVisualSeed(), IsDecorationVisibleForCoord, FogStateForCoord);

        featureRoot = NewRootGroup("Features");
        currentBuildRoot = featureRoot;
        BuildFeatures();
        BuildCoastFlags();
        BuildExpeditionMarker();
        UpdateFeatureVisibility();

        overlayRoot = NewRootGroup("Hex Overlays");
        currentBuildRoot = overlayRoot;
        BuildHexOverlays();

        playerAnnotationRoot = NewRootGroup("Player Annotations");
        currentBuildRoot = playerAnnotationRoot;
        BuildPlayerAnnotations();

        systemsRoot = NewRootGroup("Systems");
        currentBuildRoot = systemsRoot;
        BuildWaterPlane();

        lightingRoot = NewRootGroup("Lighting");
        currentBuildRoot = lightingRoot;
        BuildLighting();

        currentBuildRoot = NewRootGroup("Camera");
        BuildCamera();

        hudRoot = NewRootGroup("HUD");
        currentBuildRoot = hudRoot;
        BuildHud();
        currentBuildRoot = null;
    }

    private void RequestRebuild()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            QueueEditorRebuild();
            return;
        }
#endif
        Rebuild();
    }

#if UNITY_EDITOR
    private void QueueEditorRebuild()
    {
        if (editorRebuildQueued)
        {
            return;
        }

        editorRebuildQueued = true;
        UnityEditor.EditorApplication.delayCall += RebuildFromEditorDelay;
    }

    private void RebuildFromEditorDelay()
    {
        editorRebuildQueued = false;
        if (this == null || !isActiveAndEnabled)
        {
            return;
        }

        Rebuild();
    }
#endif

    private void ClearGeneratedChildren()
    {
        var children = new List<GameObject>();
        for (var i = 0; i < transform.childCount; i++)
        {
            children.Add(transform.GetChild(i).gameObject);
        }

        var generatedMeshes = new HashSet<Mesh>();
        foreach (var child in children)
        {
            CollectGeneratedMeshes(child, generatedMeshes);
        }

        foreach (var mesh in generatedMeshCache.Values)
        {
            if (mesh != null && IsGeneratedRuntimeAsset(mesh))
            {
                generatedMeshes.Add(mesh);
            }
        }

        foreach (var child in children)
        {
            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }

        foreach (var mesh in generatedMeshes)
        {
            if (mesh != null)
            {
                DestroyGeneratedObject(mesh);
            }
        }
    }

    private void ClearGeneratedRuntimeAssets()
    {
        var materials = new HashSet<Material>();
        CollectMaterials(featureMaterials.Values, materials);

        var textures = new HashSet<Texture>();
        foreach (var material in materials)
        {
            // Custom shaders (Hex/HexTerrain, Hex/HexNatureInstanced) don't have a "_MainTex"-named
            // property, so accessing .mainTexture on them logs a Unity warning even though it's
            // harmless; guard with HasProperty so only shaders that actually have one are queried.
            if (material == null || !material.HasProperty("_MainTex"))
            {
                continue;
            }

            var texture = material.mainTexture;
            if (texture != null && IsGeneratedRuntimeAsset(texture))
            {
                textures.Add(texture);
            }
        }

        foreach (var material in materials)
        {
            if (material != null && IsGeneratedRuntimeAsset(material))
            {
                DestroyGeneratedObject(material);
            }
        }

        foreach (var texture in textures)
        {
            if (texture != null)
            {
                DestroyGeneratedObject(texture);
            }
        }

        featureMaterials.Clear();
        generatedMeshCache.Clear();
        hexTerrainMaterial = null;
        hexNatureMaterialTemplate = null;
        if (hexTerrainNoiseTexture != null)
        {
            DestroyGeneratedObject(hexTerrainNoiseTexture);
        }

        hexTerrainNoiseTexture = null;
    }

    private static void CollectMaterials(IEnumerable<Material> source, HashSet<Material> target)
    {
        foreach (var material in source)
        {
            if (material != null)
            {
                target.Add(material);
            }
        }
    }

    private static void CollectGeneratedMeshes(GameObject root, HashSet<Mesh> meshes)
    {
        var meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
        foreach (var meshFilter in meshFilters)
        {
            var mesh = meshFilter.sharedMesh;
            if (mesh != null && IsGeneratedRuntimeAsset(mesh))
            {
                meshFilter.sharedMesh = null;
                meshes.Add(mesh);
            }
        }
    }

    private static bool IsGeneratedRuntimeAsset(Object obj)
    {
        if ((obj.hideFlags & HideFlags.DontSave) == 0)
        {
            return false;
        }

#if UNITY_EDITOR
        if (UnityEditor.EditorUtility.IsPersistent(obj))
        {
            return false;
        }
#endif

        return true;
    }

    private static void DestroyGeneratedObject(Object obj)
    {
        if (Application.isPlaying)
        {
            Destroy(obj);
        }
        else
        {
            DestroyImmediate(obj);
        }
    }

    private void CreateMaterials()
    {
        featureMaterials["DebugHex"] = TransparentMaterial("Debug Hex", "101916", 0.32f);
        featureMaterials["FactionOwnerCoastal"] = TransparentMaterial("Debug Faction Coastal", "4f94a8", 0.34f);
        featureMaterials["FactionOwnerWardens"] = TransparentMaterial("Debug Faction Wardens", "b76857", 0.34f);
        featureMaterials["FactionOwnerHidden"] = TransparentMaterial("Debug Faction Hidden", "6f5a9d", 0.36f);
        CreateFactionOwnerPalette();
        featureMaterials["UnknownFog"] = TransparentMaterial("Unknown Fog", "151817", 0.74f);
        featureMaterials["ReportedFog"] = TransparentMaterial("Reported Fog", "303432", 0.52f);
        featureMaterials["ReachableHex"] = TransparentMaterial("Reachable Hex", "a8c884", 0.24f);
        featureMaterials["HoverHex"] = EmissiveMaterial("Hover Hex", "d6d0a0", "f0dfa4", 0.18f);
        featureMaterials["BlockedHoverHex"] = TransparentMaterial("Blocked Hover Hex", "b96b62", 0.34f);
        featureMaterials["InspectedHex"] = EmissiveMaterial("Inspected Hex", "8fb8d3", "a9d8ef", 0.12f);
        featureMaterials["SelectedHex"] = EmissiveMaterial("Selected Hex", "ded69a", "f2e5a7", 0.22f);
        featureMaterials["PlayerMarker"] = EmissiveMaterial("Player Marker", "d3b35d", "f3d67a", 0.14f);
        featureMaterials["PlayerNote"] = EmissiveMaterial("Player Note", "7ea5c5", "a8d8f0", 0.12f);
        featureMaterials["FactionMarker"] = EmissiveMaterial("Faction Marker", "b86458", "ef9a83", 0.16f);
        featureMaterials["DangerMarker"] = EmissiveMaterial("Danger Marker", "9b3f35", "ee6a58", 0.18f);
        featureMaterials["RiverBank"] = Material("River Bank", "355653", 0.82f);
        featureMaterials["River"] = EmissiveMaterial("River", "2f7391", "5aa7c2", 0.06f);
        featureMaterials["RiverFoam"] = TransparentMaterial("River Foam", "b4dbe2", 0.08f);
        featureMaterials["WornGround"] = TransparentMaterial("Worn Ground", "5a4a30", 0.42f);
        featureMaterials["RoadShadow"] = TransparentMaterial("Road Bed", "463526", 0.22f);
        featureMaterials["Road"] = Material("Road", "6f5638", "574230", "7d6144", 41, 0.12f);
        featureMaterials["Border"] = EmissiveMaterial("Territory Border", "4b9aaa", "88c8d1", 0.08f);
        featureMaterials["SettlementWall"] = Material("Settlement Wall", "766f5e", 0.9f);
        featureMaterials["SettlementRoof"] = Material("Settlement Roof", "79425f", 0.82f);
        featureMaterials["SettlementRoofWarm"] = Material("Settlement Roof Warm", "9b6240", 0.82f);
        featureMaterials["Smoke"] = TransparentMaterial("Smoke", "c2b9a8", 0.18f);
        featureMaterials["GrassTuftLight"] = Material("Grass Tuft Light", "8ba95a", 0.86f);
        featureMaterials["GrassTuftDark"] = Material("Grass Tuft Dark", "5f7c42", 0.88f);
        featureMaterials["FlowerWhite"] = Material("Flower White", "e9e7d6", 0.7f);
        featureMaterials["FlowerYellow"] = Material("Flower Yellow", "e6c64f", 0.66f);
        featureMaterials["FlowerRed"] = Material("Flower Red", "c25b4c", 0.66f);
        featureMaterials["Pebble"] = Material("Pebble", "8b8a7c", 0.9f);
        featureMaterials["TreeTrunk"] = Material("Tree Trunk", "4c3327", 0.82f);
        featureMaterials["TreeCrown"] = Material("Tree Crown", "2a613f", 0.86f);
        featureMaterials["TreeCrownDark"] = Material("Tree Crown Dark", "1b3f2d", 0.9f);
        featureMaterials["TreeCrownMid"] = Material("Tree Crown Mid", "356e46", 0.86f);
        featureMaterials["TreeCrownLight"] = Material("Tree Crown Light", "4c7f4a", 0.84f);
        featureMaterials["TreeCrownGold"] = Material("Tree Crown Gold", "8f8a3a", 0.8f);
        featureMaterials["TreeCrownAutumn"] = Material("Tree Crown Autumn", "a76a2f", 0.78f);
        featureMaterials["ForestCover"] = Material("Forest Cover", "4a6a40", 0.88f);
        featureMaterials["Rock"] = DoubleSidedMaterial(Material("Rock", "777b71", 0.9f));
        featureMaterials["DarkRock"] = DoubleSidedMaterial(Material("Dark Rock", "383c36", 0.92f));
        featureMaterials["MountainBase"] = DoubleSidedMaterial(Material("Mountain Base", "666356", 0.92f));
        featureMaterials["Foothill"] = DoubleSidedMaterial(Material("Foothill", "6e6c55", 0.9f));
        featureMaterials["SnowCap"] = DoubleSidedMaterial(Material("Snow Cap", "d8dedb", 0.72f));
        featureMaterials["Flag"] = Material("Flag", "c4574d", 0.62f);
        featureMaterials["Gold"] = Material("Ore Gold", "c89d3b", 0.58f);
        featureMaterials["MineWood"] = Material("Mine Wood", "5d422d", 0.86f);
        featureMaterials["WallStone"] = Material("Ancient Wall Stone", "8d8772", 0.9f);
        featureMaterials["TowerRoof"] = Material("Tower Roof", "4e5267", 0.78f);
        featureMaterials["WaterPlane"] = Material("Distant Water", "36585c", 0.44f);
        featureMaterials["DeepWater"] = Material("Deep Water", "22444a", 0.4f);
        featureMaterials["BoardShelf"] = Material("Island Shelf", "46502e", 0.92f);
        featureMaterials["ShallowWater"] = TransparentMaterial("Coastal Shallows", "4d838d", 0.5f);
        featureMaterials["CoastFoam"] = TransparentMaterial("Coast Foam", "cfe6e0", 0.2f);
        featureMaterials["ExpeditionBase"] = Material("Expedition Base", "443627", 0.82f);
        featureMaterials["ExpeditionCloth"] = Material("Expedition Cloth", "d0a44c", 0.7f);
        featureMaterials["ExpeditionFlag"] = EmissiveMaterial("Expedition Flag", "d95b4f", "e8a15d", 0.18f);
        featureMaterials["ExpeditionRing"] = TransparentMaterial("Expedition Ring", "f0d889", 0.28f);

        // Give water a subtle tiling ripple so the atmosphere manager can scroll it into flow.
        var ripple = WaterRippleTexture();
        AssignWaterRipple("River", ripple, new Vector2(3f, 1f));
        AssignWaterRipple("ShallowWater", ripple, new Vector2(6f, 6f));
        AssignWaterRipple("WaterPlane", ripple, new Vector2(8f, 8f));
        AssignWaterRipple("DeepWater", ripple, new Vector2(10f, 10f));

        CreateHexTerrainMaterial();
        CreateHexNatureMaterialTemplate();
    }

    // Distinct, well-separated hues for the faction-ownership debug overlay. The three tutorial-map
    // factions keep their own tuned colors (FactionOwnerCoastal/Wardens/Hidden); anything else
    // (e.g. a generated campaign's dynamic "faction-0", "faction-1", ... ids) is deterministically
    // hashed into this broader palette instead of collapsing into one shared fallback color.
    private static readonly string[] FactionOwnerPaletteHex =
    {
        "4f94a8", "b76857", "6f5a9d", "6f9d5a", "c2984a", "9d5a7a", "5a7a9d", "9d9d5a"
    };

    private void CreateFactionOwnerPalette()
    {
        for (var i = 0; i < FactionOwnerPaletteHex.Length; i++)
        {
            featureMaterials[$"FactionOwnerPalette{i}"] = TransparentMaterial($"Debug Faction Palette {i}", FactionOwnerPaletteHex[i], 0.34f);
        }
    }

    // One shared material for the entire chunked terrain layer (HexMapChunkRenderer); per-tile
    // color variety is baked into vertex color instead of picking among per-terrain materials.
    private void CreateHexTerrainMaterial()
    {
        var shader = Shader.Find("Hex/HexTerrain");
        if (shader == null)
        {
            Debug.LogWarning("Hex/HexTerrain shader not found; terrain will fall back to vertex-color-less URP Lit.");
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }

        hexTerrainNoiseTexture = TileableNoiseTexture();
        hexTerrainMaterial = new Material(shader)
        {
            name = "Hex Terrain",
            hideFlags = HideFlags.DontSave
        };
        if (hexTerrainMaterial.HasProperty("_NoiseTex"))
        {
            hexTerrainMaterial.SetTexture("_NoiseTex", hexTerrainNoiseTexture);
        }

        featureMaterials["HexTerrain"] = hexTerrainMaterial;
    }

    // Template for HexMapChunkRenderer's ground-scatter materials (grass/flowers/pebbles); the
    // renderer clones this per color category and enables GPU instancing.
    private void CreateHexNatureMaterialTemplate()
    {
        var shader = Shader.Find("Hex/HexNatureInstanced");
        if (shader == null)
        {
            Debug.LogWarning("Hex/HexNatureInstanced shader not found; ground scatter will fall back to URP Lit (no wind sway).");
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }

        hexNatureMaterialTemplate = new Material(shader)
        {
            name = "Hex Nature Template",
            hideFlags = HideFlags.DontSave,
            enableInstancing = true
        };
        featureMaterials["HexNatureTemplate"] = hexNatureMaterialTemplate;
    }

    // Seamless grayscale value noise for the terrain shader's micro-variation, reusing the same
    // periodic-noise technique as the cloud cookie texture.
    private Texture2D TileableNoiseTexture()
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave
        };

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var v = TileableNoise(x, y, size, 3.4f, 12.9f);
                texture.SetPixel(x, y, new Color(v, v, v, 1f));
            }
        }

        texture.Apply(true, false);
        return texture;
    }

    private Material Material(string name, string hex, float smoothness)
    {
        return Material(name, ColorFromHex(hex), smoothness);
    }

    private Material Material(string name, string hex, string baseHex, string accentHex, int seedOffset, float strength)
    {
        return Material(name, ColorFromHex(hex), 0.88f, NoiseTexture(baseHex, accentHex, seedOffset, strength));
    }

    private Material DoubleSidedMaterial(Material material)
    {
        if (material.HasProperty("_Cull"))
        {
            material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        }

        material.doubleSidedGI = true;
        return material;
    }

    private Material Material(string name, Color color, float smoothness, Texture2D texture = null)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        var material = new Material(shader)
        {
            name = name,
            color = color,
            hideFlags = HideFlags.DontSave
        };

        if (texture != null)
        {
            material.mainTexture = texture;
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", Mathf.Clamp01(1f - smoothness));
        }

        if (material.HasProperty("_Roughness"))
        {
            material.SetFloat("_Roughness", smoothness);
        }

        return material;
    }

    private Material TransparentMaterial(string name, string hex, float alpha)
    {
        var color = ColorFromHex(hex);
        color.a = alpha;
        var material = Material(name, color, 0.9f);
        material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = 3000;
        return material;
    }

    private Material EmissiveMaterial(string name, string hex, string emissionHex, float energy)
    {
        var material = Material(name, ColorFromHex(hex), 0.62f);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", ColorFromHex(emissionHex) * energy);
        }

        return material;
    }

    private Texture2D NoiseTexture(string baseHex, string accentHex, int seedOffset, float strength)
    {
        const int size = 96;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Trilinear,
            hideFlags = HideFlags.DontSave
        };

        var baseColor = ColorFromHex(baseHex);
        var accentColor = ColorFromHex(accentHex);
        var offset = (mapSeed + seedOffset * 997) * 0.017f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var large = Mathf.PerlinNoise(x * 0.065f + offset, y * 0.065f - offset);
                var fine = Mathf.Sin(x * 0.73f + y * 0.31f + seedOffset) * 0.045f;
                var amount = Mathf.Clamp01(large * strength + fine + 0.08f);
                texture.SetPixel(x, y, Color.Lerp(baseColor, accentColor, amount));
            }
        }

        texture.Apply(true, false);
        return texture;
    }

    // Tileable subtle ripple used as the base map on water so scrolling its UVs reads as flow.
    private Texture2D WaterRippleTexture()
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave
        };

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var n = Mathf.PerlinNoise(x * 0.09f, y * 0.09f);
                var streak = Mathf.Sin(x * 0.28f + n * 4f) * 0.5f + 0.5f;
                var v = Mathf.Lerp(0.86f, 1f, streak * 0.6f + n * 0.4f);
                texture.SetPixel(x, y, new Color(v, v, v, 1f));
            }
        }

        texture.Apply(true, false);
        return texture;
    }

    // Soft, seamlessly tiling cloud mask for the sun's cookie: open sky = 1, cloud = darker.
    // Low base frequency = big blobs; a detail octave breaks up the edges; a high threshold
    // keeps the sky mostly open with occasional large, irregular clouds.
    private Texture2D CloudCookieTexture()
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave
        };

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var big = TileableNoise(x, y, size, 2.2f, 31.7f);
                var detail = TileableNoise(x, y, size, 5.5f, 88.3f);
                var raw = big * 0.76f + detail * 0.24f;
                var cloud = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.52f, 0.8f, raw));
                var v = Mathf.Lerp(1f, 0.55f, cloud);
                texture.SetPixel(x, y, new Color(v, v, v, v));
            }
        }

        texture.Apply(true, false);
        return texture;
    }

    // Periodic value noise: blends Perlin with its wrapped copies so the result tiles seamlessly.
    private static float TileableNoise(int x, int y, int size, float frequency, float seed)
    {
        var fx = x / (float)size;
        var fy = y / (float)size;

        float Sample(float px, float py)
        {
            return Mathf.PerlinNoise(px * frequency / size + seed, py * frequency / size + seed);
        }

        return Sample(x, y) * (1f - fx) * (1f - fy)
             + Sample(x - size, y) * fx * (1f - fy)
             + Sample(x, y - size) * (1f - fx) * fy
             + Sample(x - size, y - size) * fx * fy;
    }

    private void AssignWaterRipple(string key, Texture2D texture, Vector2 scale)
    {
        if (featureMaterials.TryGetValue(key, out var material))
        {
            material.mainTexture = texture;
            material.mainTextureScale = scale;
        }
    }

    private void BuildMap()
    {
        if (useCoreTutorialState)
        {
            coreGameState = generatedCampaignRequest == null
                ? gameApplication.CreateTutorialGame()
                : gameApplication.CreateGeneratedGame(generatedCampaignRequest);
            generatedCoreQOffset = generatedCampaignRequest == null
                ? 0
                : (coreGameState.World.Map.Bounds.Height - 1) >> 1;
            BuildMapFromCoreState(coreGameState.World.Map);
            selectedPreviewHex = CoreCoordToViewCoord(coreGameState.Expedition.Position);
            return;
        }

        coreGameState = null;
        BuildProceduralMap();
    }

    private void BuildProceduralMap()
    {
        for (var row = 0; row < mapHeight; row++)
        {
            for (var column = 0; column < mapWidth; column++)
            {
                var coord = OffsetToCenteredAxial(column, row);
                var world = AxialToWorld(coord);
                var normalizedColumn = mapWidth > 1 ? (column - (mapWidth - 1) * 0.5f) / ((mapWidth - 1) * 0.5f) : 0f;
                var normalizedRow = mapHeight > 1 ? (row - (mapHeight - 1) * 0.5f) / ((mapHeight - 1) * 0.5f) : 0f;
                var edgeFade = Mathf.Clamp01(new Vector2(normalizedColumn, normalizedRow).magnitude * 0.58f);
                var heightNoise = Noise(coord.x * 0.18f, coord.y * 0.18f, 0) - edgeFade * 0.08f;
                var terrain = PickTerrain(heightNoise, coord);
                var mountainRange = MountainRangeStrength(coord);
                if (mountainRange > 0.82f)
                {
                    terrain = HexTerrainKind.Snow;
                }
                else if (mountainRange > 0.48f)
                {
                    terrain = HexTerrainKind.Mountain;
                }
                else if (mountainRange > 0.26f && terrain != HexTerrainKind.Water)
                {
                    terrain = HexTerrainKind.Hills;
                }

                var elevation = TerrainElevation(terrain, heightNoise);
                tiles[coord] = new HexTileVisual { Terrain = terrain, Elevation = elevation, World = world };
            }
        }
    }

    private void BuildMapFromCoreState(HexMapState map)
    {
        mapWidth = map.Bounds.Width;
        mapHeight = map.Bounds.Height;

        foreach (var coreTile in map.Tiles)
        {
            var coord = CoreCoordToViewCoord(coreTile.Coord);
            var world = AxialToWorld(coord);
            var terrain = TerrainFromCore(coreTile.Terrain);
            var elevation = coreTile.Elevation > 0
                ? coreTile.Elevation * 0.18f
                : TerrainElevation(terrain, Noise(coord.x * 0.18f, coord.y * 0.18f, 0));

            tiles[coord] = new HexTileVisual { Terrain = terrain, Elevation = elevation, World = world };
        }
    }

    private Vector2Int CoreCoordToViewCoord(HexCoord coord)
    {
        if (generatedCampaignRequest != null)
        {
            return new Vector2Int(coord.Q - generatedCoreQOffset, coord.R);
        }

        return OffsetToCenteredAxial(coord.Q, coord.R);
    }

    private Vector3 CoreCoordToWorld(HexCoord coord)
    {
        return AxialToWorld(CoreCoordToViewCoord(coord));
    }

    private static HexTerrainKind TerrainFromCore(TerrainType terrain)
    {
        switch (terrain)
        {
            case TerrainType.Water:
                return HexTerrainKind.Water;
            case TerrainType.Coast:
                return HexTerrainKind.Coast;
            case TerrainType.Forest:
                return HexTerrainKind.Forest;
            case TerrainType.Hills:
            case TerrainType.Swamp:
                return HexTerrainKind.Hills;
            case TerrainType.Mountain:
                return HexTerrainKind.Mountain;
            case TerrainType.Snow:
                return HexTerrainKind.Snow;
            case TerrainType.Grassland:
            case TerrainType.DryPlains:
            case TerrainType.Desert:
                return HexTerrainKind.Grass;
            default:
                return HexTerrainKind.Grass;
        }
    }

    private HexTerrainKind PickTerrain(float value, Vector2Int coord)
    {
        var h = value + Mathf.Sin(coord.x * 0.53f) * 0.04f + Mathf.Cos(coord.y * 0.71f) * 0.03f;
        if (h < -0.38f) return HexTerrainKind.Water;
        if (h < -0.24f) return HexTerrainKind.Coast;
        if (h < 0.12f) return HexTerrainKind.Grass;
        if (h < 0.29f) return HexTerrainKind.Forest;
        if (h < 0.46f) return HexTerrainKind.Hills;
        if (h < 0.62f) return HexTerrainKind.Mountain;
        return HexTerrainKind.Snow;
    }

    private float MountainRangeStrength(Vector2Int coord)
    {
        var q = coord.x;
        if (q < -14 || q > 13)
        {
            return 0f;
        }

        var ridgeR = -0.45f * q + 4.1f + Mathf.Sin((q + mapSeed * 0.01f) * 0.62f) * 1.25f;
        var distance = Mathf.Abs(coord.y - ridgeR);
        var ruggedness = Hash01(coord.x, coord.y, 8123) * 0.24f - 0.08f;
        var pass = Mathf.Abs(q + 4) < 1.2f || Mathf.Abs(q - 6) < 1.1f ? 0.22f : 0f;
        return Mathf.Clamp01(1f - distance / 3.1f + ruggedness - pass);
    }

    private static float TerrainElevation(HexTerrainKind terrain, float value)
    {
        if (terrain == HexTerrainKind.Water) return 0.06f;
        if (terrain == HexTerrainKind.Coast) return 0.18f;

        var raw = terrain switch
        {
            HexTerrainKind.Grass => 0.34f + Mathf.Max(value, 0f) * 0.28f,
            HexTerrainKind.Forest => 0.48f + Mathf.Max(value, 0f) * 0.34f,
            HexTerrainKind.Hills => 0.76f + value * 0.36f,
            HexTerrainKind.Mountain => 1.08f + value * 0.48f,
            HexTerrainKind.Snow => 1.36f + value * 0.48f,
            _ => 0.3f
        };
        return Mathf.Round(raw / 0.18f) * 0.18f;
    }

    // Occasional coast-marker flag; low count, so it stays a plain GameObject like settlements
    // rather than going through the chunk-instanced decoration path.
    private void BuildCoastFlags()
    {
        foreach (var pair in tiles)
        {
            if (pair.Value.Terrain != HexTerrainKind.Coast || Mathf.Abs(pair.Key.x * 3 + pair.Key.y * 5) % 7 != 0)
            {
                continue;
            }

            var anchor = NewChild($"CoastMarkerAnchor_{pair.Key.x}_{pair.Key.y}", featureRoot);
            anchor.transform.localPosition = pair.Value.World;
            var flag = AddFlag(anchor.transform, VisualTileTopY);
            RegisterFeatureObject(pair.Key, flag);
        }
    }

    private GameObject AddFlag(Transform parent, float elevation)
    {
        if (TryPlacePrefab(Prefabs.coastMarkerPrefabs, parent, "CoastMarker", Vector3.up * elevation, Quaternion.identity, Vector3.one * hexSize, 0, Mathf.RoundToInt(elevation * 1000f), 8401, out var instance))
        {
            return instance;
        }

        var root = NewChild("CoastMarker", parent);
        var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "Pole";
        pole.transform.SetParent(root.transform, false);
        pole.transform.localPosition = new Vector3(0.12f, elevation + 0.36f, -0.18f);
        pole.transform.localScale = new Vector3(0.035f, 0.36f, 0.035f);
        pole.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["TreeTrunk"];

        var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flag.name = "Flag";
        flag.transform.SetParent(root.transform, false);
        flag.transform.localPosition = new Vector3(0.28f, elevation + 0.58f, -0.18f);
        flag.transform.localScale = new Vector3(0.34f, 0.2f, 0.035f);
        flag.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["Flag"];
        return root;
    }

    // Flat, slightly irregular worn-earth patch that grounds a structure to the land
    // instead of leaving it stamped on top of clean grass.
    private GameObject AddGroundApron(Vector2Int coord, float radius, int seed)
    {
        if (!tiles.TryGetValue(coord, out var tile))
        {
            return null;
        }

        var apron = NewChild($"GroundApron_{coord.x}_{coord.y}");
        RegisterFeatureObject(coord, apron);
        apron.transform.localPosition = tile.World + Vector3.up * (VisualTileTopY + 0.006f);
        apron.transform.localRotation = Quaternion.Euler(0f, Hash01(coord.x, coord.y, seed) * 360f, 0f);
        AddMesh(apron, "Apron", DiscMesh(radius, 12, seed), featureMaterials["WornGround"]);
        return apron;
    }

    private Mesh DiscMesh(float radius, int segments, int seed)
    {
        var vertices = new List<Vector3> { Vector3.zero };
        var uvs = new List<Vector2> { new(0.5f, 0.5f) };
        var triangles = new List<int>();

        for (var i = 0; i < segments; i++)
        {
            var angle = Mathf.PI * 2f * i / segments;
            var wobble = Mathf.Lerp(0.82f, 1.08f, Hash01(i, seed, 40501));
            var r = radius * wobble;
            vertices.Add(new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r));
            uvs.Add(new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f));
        }

        for (var i = 0; i < segments; i++)
        {
            var current = i + 1;
            var next = (i + 1) % segments + 1;
            triangles.Add(0);
            triangles.Add(next);
            triangles.Add(current);
        }

        return MeshFrom(vertices, uvs, triangles, "GroundApron");
    }

    private void BuildFeatures()
    {
        if (useCoreTutorialState && coreGameState != null)
        {
            BuildCoreFeatures(coreGameState.World);
            return;
        }

        BuildRiver(new[]
        {
            new Vector2Int(-8, 1), new Vector2Int(-7, 1), new Vector2Int(-6, 0), new Vector2Int(-5, 0),
            new Vector2Int(-4, 1), new Vector2Int(-3, 1), new Vector2Int(-2, 2), new Vector2Int(-1, 2),
            new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 0), new Vector2Int(3, 0),
            new Vector2Int(4, -1), new Vector2Int(5, -1), new Vector2Int(6, -2), new Vector2Int(7, -2)
        });
        BuildRiver(new[]
        {
            new Vector2Int(-2, -6), new Vector2Int(-1, -6), new Vector2Int(0, -6), new Vector2Int(0, -5),
            new Vector2Int(1, -5), new Vector2Int(1, -4), new Vector2Int(2, -4), new Vector2Int(2, -3),
            new Vector2Int(3, -3), new Vector2Int(4, -4)
        });

        var settlements = new[]
        {
            new Vector2Int(-5, 2), new Vector2Int(-1, 0), new Vector2Int(3, -2), new Vector2Int(4, 3), new Vector2Int(-3, 5)
        };
        foreach (var settlement in settlements)
        {
            BuildSettlement(settlement);
        }

        BuildRoad(new[]
        {
            new Vector2Int(-5, 2), new Vector2Int(-4, 2), new Vector2Int(-3, 2), new Vector2Int(-2, 1),
            new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, -1), new Vector2Int(2, -1), new Vector2Int(3, -2)
        });
        BuildRoad(new[]
        {
            new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 2), new Vector2Int(1, 2),
            new Vector2Int(2, 2), new Vector2Int(3, 2), new Vector2Int(4, 3)
        });
        BuildRoad(new[] { new Vector2Int(-5, 2), new Vector2Int(-5, 3), new Vector2Int(-4, 4), new Vector2Int(-3, 5) });
        BuildTerritoryBorder(new[]
        {
            new Vector2Int(-6, 3), new Vector2Int(-5, 2), new Vector2Int(-4, 2), new Vector2Int(-3, 1),
            new Vector2Int(-2, 1), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, -1),
            new Vector2Int(2, -1), new Vector2Int(3, -2), new Vector2Int(4, -2)
        });

        BuildTower(new Vector2Int(9, 2));
        BuildMine(new Vector2Int(-10, -2));
        BuildGreatWall(new[]
        {
            new Vector2Int(-12, 10), new Vector2Int(-11, 9), new Vector2Int(-10, 9), new Vector2Int(-9, 8),
            new Vector2Int(-8, 8), new Vector2Int(-7, 7), new Vector2Int(-6, 7), new Vector2Int(-5, 6)
        });
    }

    private void BuildCoreFeatures(WorldState world)
    {
        foreach (var path in world.Paths)
        {
            var coords = CorePathToViewPath(path.Coords);
            switch (path.Kind)
            {
                case WorldPathKind.River:
                    BuildRiver(coords);
                    break;
                case WorldPathKind.Road:
                    BuildRoad(coords);
                    break;
                case WorldPathKind.TerritoryBorder:
                    BuildTerritoryBorder(coords);
                    break;
                case WorldPathKind.Wall:
                    BuildGreatWall(coords);
                    break;
            }
        }

        foreach (var location in world.Locations)
        {
            var coord = CoreCoordToViewCoord(location.Coord);
            switch (location.Kind)
            {
                case LocationKind.Settlement:
                case LocationKind.BaseCamp:
                    BuildSettlement(coord);
                    break;
                case LocationKind.Watchtower:
                    BuildTower(coord);
                    break;
                case LocationKind.Mine:
                    BuildMine(coord);
                    break;
                case LocationKind.MarkedGrave:
                    BuildLandmarkObject(coord, Prefabs.gravePrefabs, "MarkedGrave", 9301, featureMaterials["WallStone"]);
                    break;
                case LocationKind.AbandonedCamp:
                    BuildLandmarkObject(coord, Prefabs.abandonedCampPrefabs, "AbandonedCamp", 9302, featureMaterials["ExpeditionCloth"]);
                    break;
                case LocationKind.BrokenRavine:
                    BuildLandmarkObject(coord, Prefabs.ravinePrefabs, "BrokenRavine", 9303, featureMaterials["DarkRock"]);
                    break;
                case LocationKind.Ruin:
                    BuildLandmarkObject(coord, Prefabs.ruinPrefabs, "Ruin", 9304, featureMaterials["WallStone"]);
                    break;
                case LocationKind.Landmark:
                    BuildLandmarkObject(coord, Prefabs.landmarkPrefabs, "Landmark", 9305, featureMaterials["Rock"]);
                    break;
                case LocationKind.WallSegment:
                    BuildTower(coord);
                    break;
            }
        }
    }

    // Places a discovery/landmark prefab (grave, abandoned camp, ravine, ruin, standing stones)
    // with a simple fallback marker if no prefab is assigned. Registered as a feature so it
    // obeys fog/knowledge like every other placed object.
    private void BuildLandmarkObject(Vector2Int coord, GameObject[] prefabs, string label, int seed, Material fallbackMaterial)
    {
        if (!tiles.TryGetValue(coord, out var tile))
        {
            return;
        }

        var root = NewChild($"{label}_{coord.x}_{coord.y}");
        RegisterFeatureObject(coord, root);
        root.transform.localPosition = tile.World + Vector3.up * (VisualTileTopY + 0.02f);
        root.transform.localRotation = Quaternion.Euler(0f, Hash01(coord.x, coord.y, seed) * 360f, 0f);
        if (TryPlacePrefab(prefabs, root.transform, $"{label}Prefab", Vector3.zero, Quaternion.identity, Vector3.one * hexSize, coord.x, coord.y, seed, out _))
        {
            return;
        }

        var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = label;
        marker.transform.SetParent(root.transform, false);
        marker.transform.localPosition = new Vector3(0f, 0.12f, 0f);
        marker.transform.localScale = new Vector3(0.24f, 0.24f, 0.24f);
        marker.GetComponent<MeshRenderer>().sharedMaterial = fallbackMaterial;
    }

    private IReadOnlyList<Vector2Int> CorePathToViewPath(IReadOnlyList<HexCoord> coords)
    {
        var viewCoords = new List<Vector2Int>(coords.Count);
        foreach (var coord in coords)
        {
            viewCoords.Add(CoreCoordToViewCoord(coord));
        }

        return viewCoords;
    }

    private void BuildKnowledgeOverlays()
    {
        UpdateFeatureVisibility();
        RefreshKnowledgeOverlays();
    }

    private void RefreshKnowledgeOverlays()
    {
        hexMapChunkRenderer?.RefreshKnowledge(IsDecorationVisibleForCoord, FogStateForCoord);
    }

    // Whether decoration (forest/mountain/foothill/ground-scatter instances) should render for this
    // hex right now. Mirrors the pre-rewrite IsFeatureVisible gate used for settlements etc.
    private bool IsDecorationVisibleForCoord(Vector2Int coord)
    {
        if (!useCoreTutorialState || coreGameState == null || !showKnowledgeFog || showDebugHexGrid)
        {
            return true;
        }

        return coreGameState.Knowledge.GetTileKnowledge(ViewCoordToCoreCoord(coord)) == KnowledgeLevel.Confirmed;
    }

    private HexFogState FogStateForCoord(Vector2Int coord)
    {
        if (!useCoreTutorialState || coreGameState == null || !showKnowledgeFog || showDebugHexGrid)
        {
            return HexFogState.None;
        }

        var knowledge = coreGameState.Knowledge.GetTileKnowledge(ViewCoordToCoreCoord(coord));
        if (knowledge == KnowledgeLevel.Confirmed)
        {
            return HexFogState.None;
        }

        return knowledge == KnowledgeLevel.Reported ? HexFogState.Reported : HexFogState.Unknown;
    }

    private void RegisterFeatureObject(Vector2Int coord, GameObject obj)
    {
        if (!featureObjectsByCoord.TryGetValue(coord, out var objects))
        {
            objects = new List<GameObject>();
            featureObjectsByCoord[coord] = objects;
        }

        objects.Add(obj);
        obj.SetActive(IsFeatureVisible(coord));
    }

    private void RegisterFeatureObject(IReadOnlyList<Vector2Int> coords, GameObject obj)
    {
        featureObjectsByPath[obj] = new List<Vector2Int>(coords);
        obj.SetActive(IsFeatureVisible(coords));
    }

    private bool IsFeatureVisible(Vector2Int coord)
    {
        if (!useCoreTutorialState || coreGameState == null || !showKnowledgeFog || showDebugHexGrid)
        {
            return true;
        }

        var coreCoord = ViewCoordToCoreCoord(coord);
        return coreGameState.Knowledge.GetTileKnowledge(coreCoord) == KnowledgeLevel.Confirmed;
    }

    private bool IsFeatureVisible(IReadOnlyList<Vector2Int> coords)
    {
        if (!useCoreTutorialState || coreGameState == null || !showKnowledgeFog || showDebugHexGrid)
        {
            return true;
        }

        foreach (var coord in coords)
        {
            if (coreGameState.Knowledge.GetTileKnowledge(ViewCoordToCoreCoord(coord)) == KnowledgeLevel.Confirmed)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateFeatureVisibility()
    {
        foreach (var pair in featureObjectsByCoord)
        {
            var visible = IsFeatureVisible(pair.Key);
            foreach (var obj in pair.Value)
            {
                if (obj != null)
                {
                    obj.SetActive(visible);
                }
            }
        }

        foreach (var pair in featureObjectsByPath)
        {
            if (pair.Key != null)
            {
                pair.Key.SetActive(IsFeatureVisible(pair.Value));
            }
        }
    }

    private void BuildHexOverlays()
    {
        if (showDebugFactionOwnership)
        {
            BuildDebugFactionOwnershipOverlays();
        }

        if (showDebugHexGrid)
        {
            foreach (var coord in tiles.Keys)
            {
                AddHexOverlay(coord, "DebugHex", 0.988f, 0.94f, 0.055f, featureMaterials["DebugHex"]);
            }
        }

        if (!showSelectionPreview)
        {
            return;
        }

        foreach (var coord in tiles.Keys)
        {
            if (coord == selectedPreviewHex)
            {
                continue;
            }

            if (IsReachablePreviewCoord(coord))
            {
                AddHexOverlay(coord, "ReachableHex", 0.972f, 0.91f, 0.07f, featureMaterials["ReachableHex"]);
            }
        }

        if (hasInspectedHex && inspectedHex != selectedPreviewHex)
        {
            AddHexOverlay(inspectedHex, "InspectedHex", 1.0f, 0.89f, 0.082f, featureMaterials["InspectedHex"]);
        }

        AddHexOverlay(selectedPreviewHex, "SelectedHex", 1.005f, 0.88f, 0.09f, featureMaterials["SelectedHex"]);

        if (hasHoverPreview && tiles.ContainsKey(hoverPreviewHex))
        {
            var material = IsReachablePreviewCoord(hoverPreviewHex)
                ? featureMaterials["HoverHex"]
                : featureMaterials["BlockedHoverHex"];
            AddHexOverlay(hoverPreviewHex, "HoverHex", 1.018f, 0.86f, 0.105f, material);
        }
    }

    private void BuildDebugFactionOwnershipOverlays()
    {
        if (!useCoreTutorialState || coreGameState == null)
        {
            return;
        }

        foreach (var pair in tiles)
        {
            var coreCoord = ViewCoordToCoreCoord(pair.Key);
            if (!coreGameState.World.Map.TryGetTile(coreCoord, out var coreTile) || coreTile == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(coreTile.OwnerId))
            {
                continue;
            }

            AddDebugFactionOwnershipOverlay(pair.Key, coreTile.OwnerId);
        }
    }

    private void AddDebugFactionOwnershipOverlay(Vector2Int coord, string ownerId)
    {
        if (!tiles.TryGetValue(coord, out var tile))
        {
            return;
        }

        var overlay = NewChild($"FactionOwner_{ownerId}_{coord.x}_{coord.y}", overlayRoot);
        overlay.transform.localPosition = tile.World;
        AddMesh(overlay, "FactionOwner", HexTopMesh(hexSize * 0.965f, VisualTileTopY + 0.062f), DebugFactionOwnershipMaterial(ownerId));
    }

    private Material DebugFactionOwnershipMaterial(string ownerId)
    {
        if (ownerId == "coastal-people")
        {
            return featureMaterials["FactionOwnerCoastal"];
        }

        if (ownerId == "border-wardens")
        {
            return featureMaterials["FactionOwnerWardens"];
        }

        if (ownerId == "hidden-ones")
        {
            return featureMaterials["FactionOwnerHidden"];
        }

        // Generated campaigns mint their own faction ids (e.g. "faction-0", "faction-1", ...) that
        // don't match the tutorial map's fixed three above; give each a stable, distinct color from
        // the broader palette instead of collapsing every non-tutorial faction into one fallback hue.
        // (Modulo, not Mathf.Abs, so int.MinValue can never produce a negative index.)
        var length = FactionOwnerPaletteHex.Length;
        var index = ((StableStringHash(ownerId) % length) + length) % length;
        return featureMaterials[$"FactionOwnerPalette{index}"];
    }

    // string.GetHashCode() is randomized per-process in modern .NET, so it can't be used here (the
    // same faction would get a different color every time the game runs). FNV-1a is a simple,
    // stable, order-sensitive hash that gives every distinct faction id its own consistent color.
    private static int StableStringHash(string value)
    {
        unchecked
        {
            var hash = (int)2166136261;
            foreach (var c in value)
            {
                hash = (hash ^ c) * 16777619;
            }

            return hash;
        }
    }

    private void RefreshHexOverlays()
    {
        if (overlayRoot != null)
        {
            DestroyGeneratedObject(overlayRoot.gameObject);
        }

        overlayRoot = NewRootGroup("Hex Overlays");
        currentBuildRoot = overlayRoot;
        BuildHexOverlays();
        currentBuildRoot = null;
    }

    private void BuildPlayerAnnotations()
    {
        if (!useCoreTutorialState || coreGameState == null)
        {
            return;
        }

        foreach (var marker in coreGameState.PlayerNotes.Markers)
        {
            if (!IsAnnotationVisible(marker.Coord))
            {
                continue;
            }

            AddPlayerMarker(marker);
        }

        foreach (var note in coreGameState.PlayerNotes.Notes)
        {
            if (!IsAnnotationVisible(note.Coord))
            {
                continue;
            }

            AddPlayerNoteIcon(note);
        }
    }

    private void RefreshPlayerAnnotations()
    {
        if (playerAnnotationRoot != null)
        {
            DestroyGeneratedObject(playerAnnotationRoot.gameObject);
        }

        playerAnnotationRoot = NewRootGroup("Player Annotations");
        currentBuildRoot = playerAnnotationRoot;
        BuildPlayerAnnotations();
        currentBuildRoot = null;
    }

    private void AddPlayerMarker(PlayerMapMarkerState marker)
    {
        var coord = CoreCoordToViewCoord(marker.Coord);
        if (!tiles.TryGetValue(coord, out var tile))
        {
            return;
        }

        var root = NewChild($"Marker_{marker.Kind}_{marker.Coord.Q}_{marker.Coord.R}", playerAnnotationRoot);
        root.transform.localPosition = tile.World + Vector3.up * (VisualTileTopY + 0.16f);
        var material = MarkerMaterial(marker.Kind);
        AddMesh(root, "MarkerRing", HexRingMesh(hexSize * 0.25f, hexSize * 0.16f, 0f), material);

        var pin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pin.name = "MarkerPin";
        pin.transform.SetParent(root.transform, false);
        pin.transform.localPosition = new Vector3(0f, 0.16f * hexSize, 0f);
        pin.transform.localScale = new Vector3(0.1f * hexSize, 0.28f * hexSize, 0.1f * hexSize);
        pin.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        pin.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private void AddPlayerNoteIcon(PlayerMapNoteState note)
    {
        var coord = CoreCoordToViewCoord(note.Coord);
        if (!tiles.TryGetValue(coord, out var tile))
        {
            return;
        }

        var root = NewChild($"Note_{note.Coord.Q}_{note.Coord.R}", playerAnnotationRoot);
        root.transform.localPosition = tile.World + new Vector3(0.22f * hexSize, VisualTileTopY + 0.17f, -0.16f * hexSize);
        root.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);

        var noteCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        noteCube.name = "NoteCard";
        noteCube.transform.SetParent(root.transform, false);
        noteCube.transform.localScale = new Vector3(0.2f * hexSize, 0.035f * hexSize, 0.14f * hexSize);
        noteCube.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["PlayerNote"];
    }

    private Material MarkerMaterial(PlayerMapMarkerKind kind)
    {
        switch (kind)
        {
            case PlayerMapMarkerKind.Danger:
            case PlayerMapMarkerKind.Warning:
                return featureMaterials["DangerMarker"];
            case PlayerMapMarkerKind.FactionContact:
            case PlayerMapMarkerKind.FactionWarning:
            case PlayerMapMarkerKind.FactionTerritory:
            case PlayerMapMarkerKind.FactionRumor:
                return featureMaterials["FactionMarker"];
            default:
                return featureMaterials["PlayerMarker"];
        }
    }

    private void HandleMapInput()
    {
        if (!useCoreTutorialState || coreGameState == null || strategyCamera == null)
        {
            return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            hasRightInspectStart = TryGetPointerHex(out rightInspectStartHex);
            rightInspectStartMousePosition = Input.mousePosition;
            return;
        }

        if (Input.GetMouseButtonUp(1))
        {
            if (hasRightInspectStart &&
                (Input.mousePosition - rightInspectStartMousePosition).sqrMagnitude < 36f &&
                TryGetPointerHex(out var rightClickHex) &&
                rightClickHex == rightInspectStartHex)
            {
                InspectHex(rightClickHex);
                RefreshHexOverlays();
                RefreshHud();
            }

            hasRightInspectStart = false;
            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (!TryGetPointerHex(out var viewCoord))
        {
            return;
        }

        if (!IsReachablePreviewCoord(viewCoord))
        {
            InspectHex(viewCoord);
            RefreshHexOverlays();
            RefreshHud();
            return;
        }

        var destination = ViewCoordToCoreCoord(viewCoord);
        var result = gameApplication.MoveExpedition(coreGameState, destination);
        if (!result.Success)
        {
            interactionMessage = result.Error ?? "Move rejected.";
            RefreshHud();
            return;
        }

        selectedPreviewHex = CoreCoordToViewCoord(coreGameState.Expedition.Position);
        hasInspectedHex = false;
        inspectedLocation = null;
        interactionMessage = $"Moved to {destination}. Cost {result.Cost}.";
        UpdateExpeditionMarkerPosition();
        RefreshKnowledgeOverlays();
        UpdateFeatureVisibility();
        RefreshHexOverlays();
        RefreshPlayerAnnotations();
        RefreshHud();

        if (centerCameraOnExpeditionAfterMove)
        {
            FocusCameraOnCoord(selectedPreviewHex);
        }

        TryAutoOpenLocationInteractionOnArrival();
    }

    private void TryAutoOpenLocationInteractionOnArrival()
    {
        if (coreGameState == null)
        {
            return;
        }

        var location = FindLocation(coreGameState.Expedition.Position);
        if (!ShouldAutoOpenLocationInteraction(location))
        {
            return;
        }

        if (!ShouldRepeatAutoOpenLocationInteraction(location) &&
            !autoOpenedLocationInteractionIds.Add(location.Id))
        {
            return;
        }

        inspectedHex = CoreCoordToViewCoord(location.Coord);
        hasInspectedHex = true;
        inspectedLocation = location;

        if (expeditionScreenController == null)
        {
            expeditionScreenController = FindObjectOfType<ExpeditionScreenController>();
        }

        expeditionScreenController?.OpenLocationInteraction(location.Id);
    }

    private bool ShouldAutoOpenLocationInteraction(SpecialLocationState location)
    {
        if (location == null || string.IsNullOrWhiteSpace(location.ArchetypeId))
        {
            return false;
        }

        if (coreGameState.Knowledge.GetTileKnowledge(location.Coord) != KnowledgeLevel.Confirmed)
        {
            return false;
        }

        if (ShouldRepeatAutoOpenLocationInteraction(location))
        {
            return true;
        }

        if (location.InteractionStateId != LocationStateIds.Interaction.Untouched)
        {
            return false;
        }

        // Any archetype-driven location auto-opens its interaction screen on first arrival.
        return true;
    }

    private static bool ShouldRepeatAutoOpenLocationInteraction(SpecialLocationState location)
    {
        return location != null &&
            location.ArchetypeId == LocationInteractionContent.ArchetypeRouteObstacle &&
            IsUnresolvedRouteObstacleState(location.OperationalStateId);
    }

    private static bool IsUnresolvedRouteObstacleState(string operationalStateId)
    {
        return operationalStateId == LocationStateIds.Operational.Blocked ||
            operationalStateId == LocationStateIds.Operational.RiskyPassage ||
            operationalStateId == LocationStateIds.Operational.Destroyed;
    }

    private void EndCurrentDay()
    {
        if (coreGameState == null)
        {
            return;
        }

        if (coreGameState.Expedition.Status == ExpeditionStatus.Returned ||
            coreGameState.Expedition.Status == ExpeditionStatus.Lost)
        {
            AdvanceCurrentBaseTime();
            return;
        }

        var result = gameApplication.EndDay(coreGameState);
        if (!result.Success)
        {
            interactionMessage = result.Error ?? "End day rejected.";
            RefreshHud();
            return;
        }

        interactionMessage = BuildEndDayMessage(result);
        RefreshKnowledgeOverlays();
        UpdateFeatureVisibility();
        RefreshHexOverlays();
        RefreshPlayerAnnotations();
        RefreshHud();
    }

    private void CompleteCurrentExpedition()
    {
        if (coreGameState == null)
        {
            return;
        }

        if (coreGameState.Expedition.Status == ExpeditionStatus.Returned ||
            coreGameState.Expedition.Status == ExpeditionStatus.Lost)
        {
            StartNextExpedition();
            return;
        }

        var result = gameApplication.CompleteExpedition(coreGameState);
        if (!result.Success)
        {
            interactionMessage = result.Error ?? "Expedition completion rejected.";
            RefreshHud();
            return;
        }

        interactionMessage = $"Expedition {result.ExpeditionNumber} abgeschlossen. Wissen gesichert: +{result.SecuredKnowledge}. Basiswissen: {result.BaseKnowledgePoints}. Naechste Expedition ab Welttag {result.NextExpeditionAvailableWorldDay}.";
        RefreshKnowledgeOverlays();
        UpdateFeatureVisibility();
        RefreshHexOverlays();
        RefreshPlayerAnnotations();
        RefreshHud();
    }

    private void AdvanceCurrentBaseTime()
    {
        var result = gameApplication.AdvanceBaseTime(coreGameState);
        if (!result.Success)
        {
            interactionMessage = result.Error ?? "Base time rejected.";
            RefreshHud();
            return;
        }

        interactionMessage = result.NextExpeditionReady
            ? $"Base-Zeit vergangen. Neue Expedition ist an Welttag {result.WorldDay} bereit."
            : $"Base-Zeit vergangen. Welttag {result.WorldDay}.";
        RefreshKnowledgeOverlays();
        UpdateFeatureVisibility();
        RefreshHexOverlays();
        RefreshPlayerAnnotations();
        RefreshHud();
    }

    private void StartNextExpedition()
    {
        var result = gameApplication.StartNewExpedition(coreGameState);
        if (!result.Success)
        {
            interactionMessage = result.Error ?? "New expedition rejected.";
            RefreshHud();
            return;
        }

        selectedPreviewHex = CoreCoordToViewCoord(coreGameState.Expedition.Position);
        hasInspectedHex = false;
        interactionMessage = $"Expedition {result.ExpeditionNumber} gestartet.";
        RefreshKnowledgeOverlays();
        UpdateFeatureVisibility();
        RefreshHexOverlays();
        RefreshPlayerAnnotations();
        UpdateExpeditionMarkerPosition();
        RefreshHud();
    }

    private static string BuildEndDayMessage(EndDayResult result)
    {
        if (result.ExpeditionLost)
        {
            return $"Expedition lost on world day {result.WorldDay}. No reports returned. Recovery time required.";
        }

        var message = $"Day advanced. Expedition day {result.ExpeditionDay}. Supplies consumed {result.SuppliesConsumed}.";
        if (result.ScoutResolutions.Count == 0)
        {
            return message;
        }

        var reports = 0;
        foreach (var resolution in result.ScoutResolutions)
        {
            if (resolution.Report != null)
            {
                reports += 1;
            }
        }

        return $"{message} Scout updates: {result.ScoutResolutions.Count}, reports: {reports}.";
    }

    private static string LocationActionMessage(LocationActionResult result)
    {
        if (!result.Success)
        {
            return result.Error ?? "Location action rejected.";
        }

        var prefix = string.IsNullOrEmpty(result.OutcomeLabel)
            ? result.Action?.Label ?? "Location action"
            : $"{result.Action?.Label}: {result.OutcomeLabel}";

        if (result.EffectTexts.Count == 0)
        {
            return prefix;
        }

        return $"{prefix}. {string.Join(" ", result.EffectTexts)}";
    }

    private void DrawAnnotationControls()
    {
        if (!hasInspectedHex)
        {
            GUILayout.Label("Inspect a known hex to add markers or notes.");
            return;
        }

        var coord = ViewCoordToCoreCoord(inspectedHex);
        GUILayout.Label(GetSelectedAnnotationSummary(coord));

        GUILayout.BeginHorizontal();
        if (GUILayout.Button($"Marker: {selectedMarkerKind}", GUILayout.Width(190f), GUILayout.Height(26f)))
        {
            CycleMarkerKind();
        }

        markerLabelDraft = GUILayout.TextField(markerLabelDraft, GUILayout.Width(220f));
        if (IsFactionMarkerKind(selectedMarkerKind))
        {
            markerFactionIdDraft = GUILayout.TextField(markerFactionIdDraft, GUILayout.Width(140f));
        }

        if (GUILayout.Button("Add Marker", GUILayout.Width(100f), GUILayout.Height(26f)))
        {
            AddMarkerToInspectedHex();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        noteDraftText = GUILayout.TextField(noteDraftText, GUILayout.Width(558f));
        if (GUILayout.Button("Add Note", GUILayout.Width(100f), GUILayout.Height(26f)))
        {
            AddNoteToInspectedHex();
        }

        GUILayout.EndHorizontal();
    }

    private void AddMarkerToInspectedHex()
    {
        if (!hasInspectedHex || coreGameState == null)
        {
            return;
        }

        var coord = ViewCoordToCoreCoord(inspectedHex);
        var label = string.IsNullOrWhiteSpace(markerLabelDraft) ? DefaultMarkerLabel(selectedMarkerKind) : markerLabelDraft;
        var factionId = IsFactionMarkerKind(selectedMarkerKind) ? markerFactionIdDraft : null;
        var result = gameApplication.AddMapMarker(coreGameState, coord, selectedMarkerKind, label, factionId);
        if (!result.Success)
        {
            interactionMessage = result.Error ?? "Marker rejected.";
            return;
        }

        markerLabelDraft = "";
        interactionMessage = $"Marker added at {coord}: {label}.";
        RefreshPlayerAnnotations();
        RefreshHud();
    }

    private void AddNoteToInspectedHex()
    {
        if (!hasInspectedHex || coreGameState == null)
        {
            return;
        }

        var coord = ViewCoordToCoreCoord(inspectedHex);
        var result = gameApplication.AddMapNote(coreGameState, coord, noteDraftText);
        if (!result.Success)
        {
            interactionMessage = result.Error ?? "Note rejected.";
            return;
        }

        noteDraftText = "";
        interactionMessage = $"Note added at {coord}.";
        RefreshPlayerAnnotations();
        RefreshHud();
    }

    private void DrawScoutControls()
    {
        GUILayout.Label($"Scouts: {CountAvailableScouts()} available, {CountActiveScoutMissions()} active mission(s), {CountScoutReports()} report(s).");
        GUILayout.BeginHorizontal();

        if (GUILayout.Button($"Direction: {scoutDirection}", GUILayout.Width(150f), GUILayout.Height(26f)))
        {
            CycleScoutDirection();
        }

        if (GUILayout.Button($"Focus: {scoutFocus}", GUILayout.Width(160f), GUILayout.Height(26f)))
        {
            CycleScoutFocus();
        }

        if (GUILayout.Button($"Behavior: {scoutBehavior}", GUILayout.Width(160f), GUILayout.Height(26f)))
        {
            CycleScoutBehavior();
        }

        if (GUILayout.Button($"Days: {scoutDurationDays}", GUILayout.Width(86f), GUILayout.Height(26f)))
        {
            scoutDurationDays = scoutDurationDays >= 5 ? 1 : scoutDurationDays + 1;
        }

        sendTwoScouts = GUILayout.Toggle(sendTwoScouts, "2 scouts", GUILayout.Width(82f));
        if (GUILayout.Button("Send", GUILayout.Width(76f), GUILayout.Height(26f)))
        {
            SendScoutMissionFromHud();
        }

        GUILayout.EndHorizontal();
    }

    private void SendScoutMissionFromHud()
    {
        if (coreGameState == null)
        {
            return;
        }

        var selectedScouts = SelectAvailableScoutIds(sendTwoScouts ? 2 : 1);
        var result = gameApplication.SendScoutMission(coreGameState, selectedScouts, scoutDirection, scoutDurationDays, scoutFocus, scoutBehavior);
        if (!result.Success)
        {
            interactionMessage = result.Error ?? "Scout mission rejected.";
            return;
        }

        var scoutText = result.Mission!.ScoutMemberIds.Count == 1 ? "Scout" : "Scouts";
        interactionMessage = $"{scoutText} sent {result.Mission.Direction} for {result.Mission.DurationDays} day(s). Expected return day {result.Mission.ExpectedReturnWorldDay}.";
        RefreshHud();
    }

    private List<string> SelectAvailableScoutIds(int count)
    {
        var ids = new List<string>(count);
        if (coreGameState == null)
        {
            return ids;
        }

        if (coreGameState.Expedition.Status != ExpeditionStatus.Active)
        {
            return ids;
        }

        foreach (var member in coreGameState.Expedition.Members)
        {
            if (member.Role != ExpeditionMemberRole.Scout || member.Status != ExpeditionMemberStatus.Available)
            {
                continue;
            }

            ids.Add(member.Id);
            if (ids.Count >= count)
            {
                break;
            }
        }

        return ids;
    }

    private int CountAvailableScouts()
    {
        if (coreGameState == null)
        {
            return 0;
        }

        if (coreGameState.Expedition.Status != ExpeditionStatus.Active)
        {
            return 0;
        }

        var count = 0;
        foreach (var member in coreGameState.Expedition.Members)
        {
            if (member.Role == ExpeditionMemberRole.Scout && member.Status == ExpeditionMemberStatus.Available)
            {
                count += 1;
            }
        }

        return count;
    }

    private int CountActiveScoutMissions()
    {
        if (coreGameState == null)
        {
            return 0;
        }

        var count = 0;
        foreach (var mission in coreGameState.Expedition.ScoutMissions)
        {
            if (mission.Status == ScoutMissionStatus.Active || mission.Status == ScoutMissionStatus.Overdue)
            {
                count += 1;
            }
        }

        return count;
    }

    private int CountScoutReports()
    {
        return coreGameState?.Knowledge.ScoutReports.Count ?? 0;
    }

    private void CycleScoutDirection()
    {
        scoutDirection = scoutDirection switch
        {
            ScoutDirection.North => ScoutDirection.NorthEast,
            ScoutDirection.NorthEast => ScoutDirection.East,
            ScoutDirection.East => ScoutDirection.SouthEast,
            ScoutDirection.SouthEast => ScoutDirection.South,
            ScoutDirection.South => ScoutDirection.SouthWest,
            ScoutDirection.SouthWest => ScoutDirection.West,
            ScoutDirection.West => ScoutDirection.NorthWest,
            _ => ScoutDirection.North
        };
    }

    private void CycleScoutFocus()
    {
        scoutFocus = scoutFocus switch
        {
            ScoutMissionFocus.Survey => ScoutMissionFocus.Route,
            ScoutMissionFocus.Route => ScoutMissionFocus.Resources,
            ScoutMissionFocus.Resources => ScoutMissionFocus.FactionSigns,
            ScoutMissionFocus.FactionSigns => ScoutMissionFocus.Ruins,
            _ => ScoutMissionFocus.Survey
        };
    }

    private void CycleScoutBehavior()
    {
        scoutBehavior = scoutBehavior switch
        {
            ScoutMissionBehavior.Cautious => ScoutMissionBehavior.Balanced,
            ScoutMissionBehavior.Balanced => ScoutMissionBehavior.Bold,
            _ => ScoutMissionBehavior.Cautious
        };
    }

    private void CycleMarkerKind()
    {
        selectedMarkerKind = selectedMarkerKind switch
        {
            PlayerMapMarkerKind.Question => PlayerMapMarkerKind.Warning,
            PlayerMapMarkerKind.Warning => PlayerMapMarkerKind.Danger,
            PlayerMapMarkerKind.Danger => PlayerMapMarkerKind.Destination,
            PlayerMapMarkerKind.Destination => PlayerMapMarkerKind.Resource,
            PlayerMapMarkerKind.Resource => PlayerMapMarkerKind.FactionContact,
            PlayerMapMarkerKind.FactionContact => PlayerMapMarkerKind.FactionWarning,
            PlayerMapMarkerKind.FactionWarning => PlayerMapMarkerKind.FactionTerritory,
            PlayerMapMarkerKind.FactionTerritory => PlayerMapMarkerKind.FactionRumor,
            PlayerMapMarkerKind.FactionRumor => PlayerMapMarkerKind.Note,
            _ => PlayerMapMarkerKind.Question
        };
    }

    private static bool IsFactionMarkerKind(PlayerMapMarkerKind kind)
    {
        return kind == PlayerMapMarkerKind.FactionContact ||
            kind == PlayerMapMarkerKind.FactionWarning ||
            kind == PlayerMapMarkerKind.FactionTerritory ||
            kind == PlayerMapMarkerKind.FactionRumor;
    }

    private static string DefaultMarkerLabel(PlayerMapMarkerKind kind)
    {
        return kind switch
        {
            PlayerMapMarkerKind.FactionContact => "Faction contact",
            PlayerMapMarkerKind.FactionWarning => "Faction warning",
            PlayerMapMarkerKind.FactionTerritory => "Suspected territory",
            PlayerMapMarkerKind.FactionRumor => "Faction rumor",
            PlayerMapMarkerKind.Danger => "Danger",
            PlayerMapMarkerKind.Warning => "Warning",
            PlayerMapMarkerKind.Destination => "Destination",
            PlayerMapMarkerKind.Resource => "Resource",
            PlayerMapMarkerKind.Note => "Note",
            _ => "Question"
        };
    }

    private static PlayerMapMarkerKind MarkerKindForReportHint(string hint)
    {
        if (string.IsNullOrWhiteSpace(hint))
        {
            return PlayerMapMarkerKind.Question;
        }

        var normalized = hint.ToLowerInvariant();
        if (normalized.Contains("faction") || normalized.Contains("territor") || normalized.Contains("warning"))
        {
            return PlayerMapMarkerKind.FactionRumor;
        }

        if (normalized.Contains("resource"))
        {
            return PlayerMapMarkerKind.Resource;
        }

        if (normalized.Contains("route") || normalized.Contains("path"))
        {
            return PlayerMapMarkerKind.Destination;
        }

        if (normalized.Contains("ruin") || normalized.Contains("stone") || normalized.Contains("sealed"))
        {
            return PlayerMapMarkerKind.Question;
        }

        return PlayerMapMarkerKind.Warning;
    }

    private void UpdateHoverPreview()
    {
        if (strategyCamera == null)
        {
            return;
        }

        var nextHasHover = TryGetPointerHex(out var nextHover);
        if (nextHasHover == hasHoverPreview && (!nextHasHover || nextHover == hoverPreviewHex))
        {
            return;
        }

        hasHoverPreview = nextHasHover;
        hoverPreviewHex = nextHover;
        RefreshHexOverlays();
    }

    private void InspectHex(Vector2Int viewCoord)
    {
        hasInspectedHex = true;
        inspectedHex = viewCoord;
        inspectedLocation = null;

        var coreCoord = ViewCoordToCoreCoord(viewCoord);
        var knowledge = GetKnowledgeLevel(viewCoord);
        if (knowledge == KnowledgeLevel.Unknown)
        {
            interactionMessage = $"Unknown territory {coreCoord}.";
            return;
        }

        if (!coreGameState.World.Map.TryGetTile(coreCoord, out var tile) || tile == null)
        {
            interactionMessage = $"Outside map {coreCoord}.";
            return;
        }

        inspectedLocation = knowledge == KnowledgeLevel.Confirmed ? FindLocation(coreCoord) : null;
        var movement = movementCostService.GetEntryCost(tile);
        var movementText = movement.CanEnter ? $"Move cost {movement.Cost}" : movement.Reason ?? "Blocked";
        if (inspectedLocation != null)
        {
            interactionMessage = $"{inspectedLocation.Name} ({inspectedLocation.Kind}) at {coreCoord}. {tile.Terrain}. {movementText}.";
            return;
        }

        interactionMessage = $"{knowledge} {tile.Terrain} at {coreCoord}. {movementText}.";
    }

    private bool TryGetPointerHex(out Vector2Int coord)
    {
        coord = default;
        if (strategyCamera == null || IsPointerBlockedByExpeditionUi())
        {
            return false;
        }

        var ray = strategyCamera.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(transform.up, transform.TransformPoint(new Vector3(0f, VisualTileTopY, 0f)));
        if (!plane.Raycast(ray, out var distance))
        {
            return false;
        }

        var hit = ray.GetPoint(distance);
        var local = transform.InverseTransformPoint(hit);
        coord = WorldToAxial(local);
        return tiles.ContainsKey(coord);
    }

    private bool IsPointerBlockedByExpeditionUi()
    {
        if (expeditionScreenController == null)
        {
            expeditionScreenController = FindObjectOfType<ExpeditionScreenController>();
        }

        return expeditionScreenController != null &&
            expeditionScreenController.IsPointerOverMapBlockingUi(Input.mousePosition);
    }

    private void UpdateCameraPan()
    {
        if (cameraRig == null || strategyCamera == null)
        {
            return;
        }

        var right = strategyCamera.transform.right;
        right.y = 0f;
        right.Normalize();

        var forward = strategyCamera.transform.forward;
        forward.y = 0f;
        forward.Normalize();

        var keyboard = Vector3.zero;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            keyboard -= right;
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            keyboard += right;
        }
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            keyboard += forward;
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            keyboard -= forward;
        }

        if (keyboard.sqrMagnitude > 0.001f)
        {
            cameraRig.position += keyboard.normalized * (cameraPanSpeed * Time.deltaTime);
        }

        if (IsPointerBlockedByExpeditionUi() &&
            (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2) || Input.GetMouseButton(1) || Input.GetMouseButton(2)))
        {
            isDraggingPan = false;
            return;
        }

        var panButtonDown = Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
        var panButtonHeld = Input.GetMouseButton(1) || Input.GetMouseButton(2);
        if (panButtonDown)
        {
            isDraggingPan = true;
            lastPanMousePosition = Input.mousePosition;
        }

        if (!panButtonHeld)
        {
            isDraggingPan = false;
            return;
        }

        if (!isDraggingPan)
        {
            return;
        }

        var delta = Input.mousePosition - lastPanMousePosition;
        lastPanMousePosition = Input.mousePosition;
        var scale = strategyCamera.orthographicSize * 0.0022f * cameraDragPanSpeed;
        cameraRig.position += (-right * delta.x - forward * delta.y) * scale;
    }

    private bool IsReachablePreviewCoord(Vector2Int coord)
    {
        if (useCoreTutorialState && coreGameState != null)
        {
            if (HexDistance(coord, selectedPreviewHex) != 1)
            {
                return false;
            }

            var coreCoord = ViewCoordToCoreCoord(coord);
            if (!coreGameState.World.Map.TryGetTile(coreCoord, out var tile) || tile == null)
            {
                return false;
            }

            if (!IsKnownForMovement(coord))
            {
                return false;
            }

            var cost = movementCostService.GetEntryCost(tile);
            return cost.CanEnter && cost.Cost <= coreGameState.Expedition.MovementPoints;
        }

        return HexDistance(coord, selectedPreviewHex) <= reachablePreviewRadius;
    }

    private bool IsKnownForMovement(Vector2Int coord)
    {
        if (!useCoreTutorialState || coreGameState == null || !showKnowledgeFog || showDebugHexGrid)
        {
            return true;
        }

        return GetKnowledgeLevel(coord) != KnowledgeLevel.Unknown;
    }

    private bool IsAnnotationVisible(HexCoord coord)
    {
        if (!useCoreTutorialState || coreGameState == null || !showKnowledgeFog || showDebugHexGrid)
        {
            return true;
        }

        foreach (var marker in coreGameState.PlayerNotes.Markers)
        {
            if (marker.Coord == coord)
            {
                return true;
            }
        }

        foreach (var note in coreGameState.PlayerNotes.Notes)
        {
            if (note.Coord == coord)
            {
                return true;
            }
        }

        return coreGameState.Knowledge.GetTileKnowledge(coord) != KnowledgeLevel.Unknown;
    }

    private KnowledgeLevel GetKnowledgeLevel(Vector2Int coord)
    {
        if (!useCoreTutorialState || coreGameState == null)
        {
            return KnowledgeLevel.Confirmed;
        }

        return coreGameState.Knowledge.GetTileKnowledge(ViewCoordToCoreCoord(coord));
    }

    private SpecialLocationState FindLocation(HexCoord coord)
    {
        if (coreGameState == null)
        {
            return null;
        }

        foreach (var location in coreGameState.World.Locations)
        {
            if (location.Coord == coord)
            {
                return location;
            }
        }

        return null;
    }

    private string GetHudInteractionText()
    {
        var hoverText = hasHoverPreview
            ? $"Hover {ViewCoordToCoreCoord(hoverPreviewHex)}: {(IsReachablePreviewCoord(hoverPreviewHex) ? "reachable" : "not reachable")}"
            : "Hover none";

        if (inspectedLocation != null)
        {
            return $"{hoverText} | Selected {inspectedLocation.Name}: {interactionMessage}";
        }

        return $"{hoverText} | {interactionMessage}";
    }

    private string GetSelectedAnnotationSummary(HexCoord coord)
    {
        if (coreGameState == null)
        {
            return "No game state.";
        }

        var markerCount = 0;
        PlayerMapMarkerState lastMarker = null;
        foreach (var marker in coreGameState.PlayerNotes.Markers)
        {
            if (marker.Coord == coord)
            {
                markerCount += 1;
                lastMarker = marker;
            }
        }

        var noteCount = 0;
        PlayerMapNoteState lastNote = null;
        foreach (var note in coreGameState.PlayerNotes.Notes)
        {
            if (note.Coord == coord)
            {
                noteCount += 1;
                lastNote = note;
            }
        }

        var details = $"Selected {coord}: {markerCount} marker(s), {noteCount} note(s).";
        if (lastMarker != null)
        {
            details += $" Last marker: {lastMarker.Kind} \"{lastMarker.Label}\".";
        }

        if (lastNote != null)
        {
            details += $" Last note: \"{lastNote.Text}\".";
        }

        return details;
    }

    private HexCoord ViewCoordToCoreCoord(Vector2Int coord)
    {
        if (generatedCampaignRequest != null)
        {
            return new HexCoord(coord.x + generatedCoreQOffset, coord.y);
        }

        var row = coord.y + mapHeight / 2;
        var centeredColumn = coord.x + (coord.y - (coord.y & 1)) / 2;
        var column = centeredColumn + mapWidth / 2;
        return new HexCoord(column, row);
    }

    private Vector2Int WorldToAxial(Vector3 local)
    {
        var q = (Sqrt3 / 3f * local.x - 1f / 3f * local.z) / hexSize;
        var r = (2f / 3f * local.z) / hexSize;
        return RoundAxial(q, r);
    }

    private static Vector2Int RoundAxial(float q, float r)
    {
        var s = -q - r;
        var rq = Mathf.Round(q);
        var rr = Mathf.Round(r);
        var rs = Mathf.Round(s);

        var qDiff = Mathf.Abs(rq - q);
        var rDiff = Mathf.Abs(rr - r);
        var sDiff = Mathf.Abs(rs - s);

        if (qDiff > rDiff && qDiff > sDiff)
        {
            rq = -rr - rs;
        }
        else if (rDiff > sDiff)
        {
            rr = -rq - rs;
        }

        return new Vector2Int(Mathf.RoundToInt(rq), Mathf.RoundToInt(rr));
    }

    private void BuildExpeditionMarker()
    {
        if (!useCoreTutorialState || coreGameState == null)
        {
            return;
        }

        var coord = CoreCoordToViewCoord(coreGameState.Expedition.Position);
        if (!tiles.TryGetValue(coord, out var tile))
        {
            return;
        }

        var marker = NewChild("ExpeditionMarker");
        expeditionMarker = marker.transform;
        expeditionMarker.localPosition = tile.World + Vector3.up * (VisualTileTopY + 0.12f);

        AddMesh(marker, "ExpeditionRing", HexRingMesh(hexSize * 0.42f, hexSize * 0.32f, 0f), featureMaterials["ExpeditionRing"]);

        var baseBody = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseBody.name = "ExpeditionBase";
        baseBody.transform.SetParent(marker.transform, false);
        baseBody.transform.localPosition = Vector3.up * 0.08f;
        baseBody.transform.localScale = new Vector3(0.18f * hexSize, 0.08f * hexSize, 0.18f * hexSize);
        baseBody.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["ExpeditionBase"];

        var canopy = CreateCone("ExpeditionCanopy", 0.26f * hexSize, 0.04f * hexSize, 0.28f * hexSize, featureMaterials["ExpeditionCloth"]);
        canopy.transform.SetParent(marker.transform, false);
        canopy.transform.localPosition = Vector3.up * (0.28f * hexSize);
        canopy.transform.localRotation = Quaternion.Euler(0f, 30f, 0f);

        var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "ExpeditionFlagPole";
        pole.transform.SetParent(marker.transform, false);
        pole.transform.localPosition = new Vector3(0.17f * hexSize, 0.36f * hexSize, -0.1f * hexSize);
        pole.transform.localScale = new Vector3(0.025f * hexSize, 0.32f * hexSize, 0.025f * hexSize);
        pole.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["ExpeditionBase"];

        var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flag.name = "ExpeditionFlag";
        flag.transform.SetParent(marker.transform, false);
        flag.transform.localPosition = new Vector3(0.31f * hexSize, 0.55f * hexSize, -0.1f * hexSize);
        flag.transform.localScale = new Vector3(0.22f * hexSize, 0.13f * hexSize, 0.025f * hexSize);
        flag.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["ExpeditionFlag"];
    }

    private void UpdateExpeditionMarkerPosition()
    {
        if (expeditionMarker == null || coreGameState == null)
        {
            return;
        }

        var coord = CoreCoordToViewCoord(coreGameState.Expedition.Position);
        if (tiles.TryGetValue(coord, out var tile))
        {
            expeditionMarker.localPosition = tile.World + Vector3.up * (VisualTileTopY + 0.12f);
        }
    }

    private void AddHexOverlay(Vector2Int coord, string name, float outerScale, float innerScale, float yOffset, Material material)
    {
        if (!tiles.TryGetValue(coord, out var tile))
        {
            return;
        }

        var overlay = NewChild($"{name}_{coord.x}_{coord.y}", overlayRoot);
        overlay.transform.localPosition = tile.World;
        AddMesh(overlay, name, HexRingMesh(hexSize * outerScale, hexSize * innerScale, VisualTileTopY + yOffset), material);
    }

    private static int HexDistance(Vector2Int a, Vector2Int b)
    {
        var dq = a.x - b.x;
        var dr = a.y - b.y;
        return (Mathf.Abs(dq) + Mathf.Abs(dr) + Mathf.Abs(dq + dr)) / 2;
    }

    private void BuildRiver(IReadOnlyList<Vector2Int> coords)
    {
        var points = CoordsToPathPoints(coords, 0.06f, 0.14f, 2101);
        if (points.Count < 2) return;

        var river = NewChild("RiverPath");
        RegisterFeatureObject(coords, river);
        AddMesh(river, "RiverBank", RibbonMesh(points, 0.32f * hexSize), featureMaterials["RiverBank"]);
        AddMesh(river, "River", RibbonMesh(RaisePoints(points, 0.018f), 0.2f * hexSize), featureMaterials["River"]);
        AddMesh(river, "RiverFoam", RibbonMesh(RaisePoints(points, 0.034f), 0.035f * hexSize), featureMaterials["RiverFoam"]);
    }

    private void BuildRoad(IReadOnlyList<Vector2Int> coords)
    {
        var points = CoordsToPathPoints(coords, 0.08f, 0.16f, 3307);
        if (points.Count < 2) return;

        var road = NewChild("RoadPath");
        RegisterFeatureObject(coords, road);
        AddMesh(road, "RoadWear", RibbonMesh(RaisePoints(points, -0.058f), 0.4f * hexSize), featureMaterials["WornGround"]);
        AddMesh(road, "RoadBed", RibbonMesh(points, 0.26f * hexSize), featureMaterials["RoadShadow"]);
        AddMesh(road, "Road", RibbonMesh(RaisePoints(points, 0.012f), 0.15f * hexSize), featureMaterials["Road"]);
    }

    private void BuildTerritoryBorder(IReadOnlyList<Vector2Int> coords)
    {
        var points = CoordsToPathPoints(coords, 0.12f, 0.13f, 5297);
        if (points.Count < 2) return;

        var border = NewChild("TerritoryBorderPath");
        RegisterFeatureObject(coords, border);
        AddMesh(border, "TerritoryBorder", DashedPathMesh(points, 0.07f * hexSize, 0.74f), featureMaterials["Border"]);
    }

    private void BuildSettlement(Vector2Int coord)
    {
        if (!tiles.TryGetValue(coord, out var tile))
        {
            return;
        }

        AddGroundApron(coord, 0.6f * hexSize, 7250);

        var settlement = NewChild($"Settlement_{coord.x}_{coord.y}");
        RegisterFeatureObject(coord, settlement);
        settlement.transform.localPosition = tile.World + Vector3.up * (VisualTileTopY + 0.045f);
        var rotation = Hash01(coord.x, coord.y, 7301) * 360f;
        settlement.transform.localRotation = Quaternion.Euler(0f, rotation, 0f);
        if (TryPlacePrefab(Prefabs.settlementPrefabs, settlement.transform, "SettlementPrefab", Vector3.zero, Quaternion.identity, Vector3.one * hexSize, coord.x, coord.y, 7301, out _))
        {
            return;
        }

        AddMesh(settlement, "Plaza", CylinderMesh(0.48f * hexSize, 0.42f * hexSize, 0.05f, 6), featureMaterials["SettlementWall"]);

        var houseCount = 4 + Mathf.Abs(coord.x * 3 + coord.y * 5) % 3;
        for (var i = 0; i < houseCount; i++)
        {
            var angle = i * Mathf.PI * 2f / houseCount + Mathf.Lerp(-0.18f, 0.18f, Hash01(coord.x, coord.y, 7400 + i));
            var radius = Mathf.Lerp(0.2f, 0.38f, Hash01(coord.y, coord.x, 7500 + i)) * hexSize;
            var height = Mathf.Lerp(0.14f, 0.28f, Hash01(coord.x, coord.y, 7600 + i)) * hexSize;
            var width = Mathf.Lerp(0.16f, 0.26f, Hash01(coord.y, coord.x, 7700 + i)) * hexSize;
            var housePosition = new Vector3(Mathf.Cos(angle) * radius, height * 0.5f + 0.03f, Mathf.Sin(angle) * radius);
            var houseRotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg + 28f, 0f);
            var houseDepth = width * Mathf.Lerp(0.82f, 1.25f, Hash01(coord.x, coord.y, 7800 + i));
            if (TryPlacePrefab(Prefabs.settlementHousePrefabs, settlement.transform, "HousePrefab", housePosition, houseRotation, new Vector3(width, height, houseDepth), coord.x, coord.y, 7600 + i, out _))
            {
                continue;
            }

            var house = GameObject.CreatePrimitive(PrimitiveType.Cube);
            house.name = "House";
            house.transform.SetParent(settlement.transform, false);
            house.transform.localPosition = housePosition;
            house.transform.localScale = new Vector3(width, height, houseDepth);
            house.transform.localRotation = houseRotation;
            house.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["SettlementWall"];

            var roofMaterial = i % 3 == 0 ? featureMaterials["SettlementRoofWarm"] : featureMaterials["SettlementRoof"];
            var roof = CreateCone("Roof", width * 0.82f, 0.01f, height * 0.82f, roofMaterial);
            roof.transform.SetParent(settlement.transform, false);
            roof.transform.localPosition = house.transform.localPosition + Vector3.up * (height * 0.5f + height * 0.36f);
            roof.transform.localRotation = Quaternion.Euler(0f, house.transform.localEulerAngles.y + 45f, 0f);
        }

        var chimney = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        chimney.name = "ChimneySmoke";
        chimney.transform.SetParent(settlement.transform, false);
        chimney.transform.localPosition = new Vector3(0.1f, 0.45f, -0.08f);
        chimney.transform.localScale = new Vector3(0.06f, 0.2f, 0.06f);
        chimney.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["Smoke"];

        for (var i = 0; i < 3; i++)
        {
            var fencePosition = new Vector3(-0.34f + i * 0.2f, 0.065f, -0.42f);
            var fenceRotation = Quaternion.Euler(0f, -8f, 0f);
            if (TryPlacePrefab(Prefabs.settlementFencePrefabs, settlement.transform, "FencePrefab", fencePosition, fenceRotation, Vector3.one * hexSize, coord.x, coord.y, 7900 + i, out _))
            {
                continue;
            }

            var fence = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fence.name = "Fence";
            fence.transform.SetParent(settlement.transform, false);
            fence.transform.localPosition = fencePosition;
            fence.transform.localScale = new Vector3(0.16f, 0.08f, 0.035f);
            fence.transform.localRotation = fenceRotation;
            fence.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["MineWood"];
        }
    }

    private void BuildTower(Vector2Int coord)
    {
        if (!tiles.TryGetValue(coord, out var tile))
        {
            return;
        }

        AddGroundApron(coord, 0.42f * hexSize, 9050);

        var tower = NewChild($"Watchtower_{coord.x}_{coord.y}");
        RegisterFeatureObject(coord, tower);
        tower.transform.localPosition = tile.World + Vector3.up * (VisualTileTopY + 0.045f);
        tower.transform.localRotation = Quaternion.Euler(0f, Hash01(coord.x, coord.y, 9001) * 360f, 0f);
        if (TryPlacePrefab(Prefabs.towerPrefabs, tower.transform, "WatchtowerPrefab", Vector3.zero, Quaternion.identity, Vector3.one * hexSize, coord.x, coord.y, 9001, out _))
        {
            return;
        }

        AddMesh(tower, "TowerBase", CylinderMesh(0.34f * hexSize, 0.3f * hexSize, 0.12f, 6), featureMaterials["WallStone"]);

        var body = CreateCone("TowerBody", 0.22f * hexSize, 0.18f * hexSize, 1.05f * hexSize, featureMaterials["WallStone"]);
        body.transform.SetParent(tower.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.58f * hexSize, 0f);

        var roof = CreateCone("TowerRoof", 0.28f * hexSize, 0.02f * hexSize, 0.34f * hexSize, featureMaterials["TowerRoof"]);
        roof.transform.SetParent(tower.transform, false);
        roof.transform.localPosition = new Vector3(0f, 1.28f * hexSize, 0f);

        var banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
        banner.name = "TowerBanner";
        banner.transform.SetParent(tower.transform, false);
        banner.transform.localPosition = new Vector3(0.18f * hexSize, 1.05f * hexSize, -0.04f * hexSize);
        banner.transform.localScale = new Vector3(0.28f * hexSize, 0.18f * hexSize, 0.035f * hexSize);
        banner.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["Flag"];
    }

    private void BuildMine(Vector2Int coord)
    {
        if (!tiles.TryGetValue(coord, out var tile))
        {
            return;
        }

        AddGroundApron(coord, 0.52f * hexSize, 9250);

        var mine = NewChild($"Mine_{coord.x}_{coord.y}");
        RegisterFeatureObject(coord, mine);
        mine.transform.localPosition = tile.World + Vector3.up * (VisualTileTopY + 0.045f);
        mine.transform.localRotation = Quaternion.Euler(0f, -25f, 0f);
        if (TryPlacePrefab(Prefabs.minePrefabs, mine.transform, "MinePrefab", Vector3.zero, Quaternion.identity, Vector3.one * hexSize, coord.x, coord.y, 9201, out _))
        {
            return;
        }

        var hill = CreateCone("MineHill", 0.5f * hexSize, 0.12f * hexSize, 0.46f * hexSize, featureMaterials["DarkRock"]);
        hill.transform.SetParent(mine.transform, false);
        hill.transform.localPosition = new Vector3(0f, 0.23f * hexSize, 0.03f * hexSize);

        var entrance = GameObject.CreatePrimitive(PrimitiveType.Cube);
        entrance.name = "MineEntrance";
        entrance.transform.SetParent(mine.transform, false);
        entrance.transform.localPosition = new Vector3(0f, 0.18f * hexSize, -0.28f * hexSize);
        entrance.transform.localScale = new Vector3(0.34f * hexSize, 0.28f * hexSize, 0.08f * hexSize);
        entrance.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["MineWood"];

        for (var i = 0; i < 2; i++)
        {
            var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = "MineRail";
            rail.transform.SetParent(mine.transform, false);
            rail.transform.localPosition = new Vector3((i == 0 ? -0.07f : 0.07f) * hexSize, 0.035f * hexSize, -0.48f * hexSize);
            rail.transform.localScale = new Vector3(0.035f * hexSize, 0.03f * hexSize, 0.48f * hexSize);
            rail.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["MineWood"];
        }

        for (var i = 0; i < 3; i++)
        {
            AddRock(mine.transform, 0f, -0.34f + i * 0.18f, 0.26f - i * 0.08f, i);
        }

        var ore = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ore.name = "OreHint";
        ore.transform.SetParent(mine.transform, false);
        ore.transform.localPosition = new Vector3(0.32f * hexSize, 0.12f * hexSize, -0.18f * hexSize);
        ore.transform.localScale = new Vector3(0.12f * hexSize, 0.1f * hexSize, 0.12f * hexSize);
        ore.transform.localRotation = Quaternion.Euler(15f, 30f, 8f);
        ore.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["Gold"];
    }

    private void BuildGreatWall(IReadOnlyList<Vector2Int> coords)
    {
        var points = new List<Vector3>();
        foreach (var coord in coords)
        {
            if (tiles.TryGetValue(coord, out var tile))
            {
                points.Add(tile.World + Vector3.up * (VisualTileTopY + 0.08f));
            }
        }

        if (points.Count < 2)
        {
            return;
        }

        var wall = NewChild("AncientWall");
        RegisterFeatureObject(coords, wall);
        for (var i = 0; i < points.Count - 1; i++)
        {
            AddWallSegment(wall.transform, points[i], points[i + 1], i);
        }

        for (var i = 0; i < points.Count; i += 2)
        {
            if (TryPlacePrefab(Prefabs.wallTowerPrefabs, wall.transform, "WallTowerPrefab", points[i] + Vector3.up * (0.18f * hexSize), Quaternion.identity, Vector3.one * hexSize, i, points.Count, 9501, out _))
            {
                continue;
            }

            var tower = CreateCone("WallTower", 0.22f * hexSize, 0.18f * hexSize, 0.46f * hexSize, featureMaterials["WallStone"]);
            tower.transform.SetParent(wall.transform, false);
            tower.transform.localPosition = points[i] + Vector3.up * (0.18f * hexSize);
        }
    }

    private void AddWallSegment(Transform parent, Vector3 a, Vector3 b, int index)
    {
        var delta = b - a;
        var length = new Vector2(delta.x, delta.z).magnitude;
        if (length <= 0.001f)
        {
            return;
        }

        var rotation = Quaternion.LookRotation(new Vector3(delta.x, 0f, delta.z), Vector3.up);
        if (TryPlacePrefab(Prefabs.wallSegmentPrefabs, parent, $"WallSegmentPrefab_{index}", Vector3.Lerp(a, b, 0.5f) + Vector3.up * (0.12f * hexSize), rotation, new Vector3(0.22f * hexSize, 0.28f * hexSize, length), index, Mathf.RoundToInt(length * 100f), 9601, out _))
        {
            return;
        }

        var segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        segment.name = $"WallSegment_{index}";
        segment.transform.SetParent(parent, false);
        segment.transform.localPosition = Vector3.Lerp(a, b, 0.5f) + Vector3.up * (0.12f * hexSize);
        segment.transform.localRotation = rotation;
        segment.transform.localScale = new Vector3(0.22f * hexSize, 0.28f * hexSize, length);
        segment.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["WallStone"];

        if (index % 2 == 0)
        {
            var cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cap.name = "WallCap";
            cap.transform.SetParent(parent, false);
            cap.transform.localPosition = segment.transform.localPosition + Vector3.up * (0.17f * hexSize);
            cap.transform.localRotation = segment.transform.localRotation;
            cap.transform.localScale = new Vector3(0.3f * hexSize, 0.08f * hexSize, length * 0.74f);
            cap.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["DarkRock"];
        }
    }

    private List<Vector3> CoordsToPathPoints(IReadOnlyList<Vector2Int> coords, float yOffset, float jitter, int seedOffset)
    {
        var points = new List<Vector3>();
        for (var i = 0; i < coords.Count; i++)
        {
            var coord = coords[i];
            if (!tiles.TryGetValue(coord, out var tile))
            {
                continue;
            }

            var jx = Mathf.Lerp(-jitter, jitter, Hash01(coord.x, coord.y, seedOffset + i * 37)) * hexSize;
            var jz = Mathf.Lerp(-jitter, jitter, Hash01(coord.y, coord.x, seedOffset + i * 73)) * hexSize;
            points.Add(tile.World + new Vector3(jx, VisualTileTopY + yOffset, jz));
        }

        return SmoothPath(points, 5);
    }

    private static List<Vector3> SmoothPath(IReadOnlyList<Vector3> points, int subdivisions)
    {
        if (points.Count < 3)
        {
            return new List<Vector3>(points);
        }

        var smooth = new List<Vector3>();
        for (var i = 0; i < points.Count - 1; i++)
        {
            var p0 = points[Mathf.Max(i - 1, 0)];
            var p1 = points[i];
            var p2 = points[i + 1];
            var p3 = points[Mathf.Min(i + 2, points.Count - 1)];
            for (var step = 0; step < subdivisions; step++)
            {
                var t = step / (float)subdivisions;
                smooth.Add(CatmullRom(p0, p1, p2, p3, t));
            }
        }

        smooth.Add(points[^1]);
        return smooth;
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        var t2 = t * t;
        var t3 = t2 * t;
        return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    private static List<Vector3> RaisePoints(IReadOnlyList<Vector3> points, float amount)
    {
        var raised = new List<Vector3>(points.Count);
        foreach (var point in points)
        {
            raised.Add(point + Vector3.up * amount);
        }

        return raised;
    }

    private Mesh RibbonMesh(IReadOnlyList<Vector3> points, float width)
    {
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        if (points.Count < 2)
        {
            return MeshFrom(vertices, uvs, triangles, "EmptyRibbon");
        }

        var left = new List<Vector3>(points.Count);
        var right = new List<Vector3>(points.Count);
        for (var i = 0; i < points.Count; i++)
        {
            var previous = points[Mathf.Max(i - 1, 0)];
            var next = points[Mathf.Min(i + 1, points.Count - 1)];
            var tangent = new Vector3(next.x - previous.x, 0f, next.z - previous.z).normalized;
            if (tangent.sqrMagnitude <= 0.0001f) tangent = Vector3.forward;
            var side = new Vector3(-tangent.z, 0f, tangent.x) * width * 0.5f;
            left.Add(points[i] + side);
            right.Add(points[i] - side);
        }

        for (var i = 0; i < points.Count - 1; i++)
        {
            AddRibbonQuad(vertices, uvs, triangles, left[i], left[i + 1], right[i], right[i + 1], i / (float)(points.Count - 1), (i + 1) / (float)(points.Count - 1));
        }

        return MeshFrom(vertices, uvs, triangles, "Ribbon");
    }

    private Mesh DashedPathMesh(IReadOnlyList<Vector3> points, float width, float dashFraction)
    {
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        for (var i = 0; i < points.Count - 1; i++)
        {
            if (i % 2 == 1) continue;

            var start = points[i];
            var end = Vector3.Lerp(points[i], points[i + 1], dashFraction);
            var tangent = new Vector3(end.x - start.x, 0f, end.z - start.z).normalized;
            if (tangent.sqrMagnitude <= 0.0001f) continue;
            var side = new Vector3(-tangent.z, 0f, tangent.x) * width * 0.5f;
            AddRibbonQuad(vertices, uvs, triangles, start + side, end + side, start - side, end - side, 0f, 1f);
        }

        return MeshFrom(vertices, uvs, triangles, "DashedPath");
    }

    private static void AddRibbonQuad(List<Vector3> vertices, List<Vector2> uvs, List<int> triangles, Vector3 leftA, Vector3 leftB, Vector3 rightA, Vector3 rightB, float u0, float u1)
    {
        var start = vertices.Count;
        vertices.Add(leftA); uvs.Add(new Vector2(u0, 0f));
        vertices.Add(leftB); uvs.Add(new Vector2(u1, 0f));
        vertices.Add(rightA); uvs.Add(new Vector2(u0, 1f));
        vertices.Add(rightB); uvs.Add(new Vector2(u1, 1f));
        triangles.Add(start);
        triangles.Add(start + 1);
        triangles.Add(start + 2);
        triangles.Add(start + 2);
        triangles.Add(start + 1);
        triangles.Add(start + 3);
    }

    private Mesh HexTopMesh(float radius, float y)
    {
        var key = MeshCacheKey("hex-top", radius, y);
        if (generatedMeshCache.TryGetValue(key, out var cached) && cached != null)
        {
            return cached;
        }

        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();
        for (var i = 0; i < 6; i++)
        {
            var a = HexCorner(radius, i);
            var b = HexCorner(radius, (i + 1) % 6);
            var start = vertices.Count;
            vertices.Add(new Vector3(0f, y, 0f)); uvs.Add(new Vector2(0.5f, 0.5f));
            vertices.Add(new Vector3(b.x, y, b.y)); uvs.Add(TopUv(b, radius));
            vertices.Add(new Vector3(a.x, y, a.y)); uvs.Add(TopUv(a, radius));
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }

        var mesh = MeshFrom(vertices, uvs, triangles, "HexTop");
        generatedMeshCache[key] = mesh;
        return mesh;
    }

    private Mesh HexSideMesh(float radius, float y)
    {
        var key = MeshCacheKey("hex-side", radius, y, VisualTileBottomY);
        if (generatedMeshCache.TryGetValue(key, out var cached) && cached != null)
        {
            return cached;
        }

        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();
        for (var i = 0; i < 6; i++)
        {
            var a = HexCorner(radius, i);
            var b = HexCorner(radius, (i + 1) % 6);
            var start = vertices.Count;
            vertices.Add(new Vector3(a.x, y, a.y)); uvs.Add(new Vector2(i / 6f, 0f));
            vertices.Add(new Vector3(b.x, y, b.y)); uvs.Add(new Vector2((i + 1) / 6f, 0f));
            vertices.Add(new Vector3(a.x, VisualTileBottomY, a.y)); uvs.Add(new Vector2(i / 6f, 1f));
            vertices.Add(new Vector3(b.x, VisualTileBottomY, b.y)); uvs.Add(new Vector2((i + 1) / 6f, 1f));
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start + 2);
            triangles.Add(start + 1);
            triangles.Add(start + 3);
        }

        var mesh = MeshFrom(vertices, uvs, triangles, "HexSide");
        generatedMeshCache[key] = mesh;
        return mesh;
    }

    private Mesh HexRingMesh(float outerRadius, float innerRadius, float y)
    {
        var key = MeshCacheKey("hex-ring", outerRadius, innerRadius, y);
        if (generatedMeshCache.TryGetValue(key, out var cached) && cached != null)
        {
            return cached;
        }

        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();
        for (var i = 0; i < 6; i++)
        {
            var outerA = HexCorner(outerRadius, i);
            var outerB = HexCorner(outerRadius, (i + 1) % 6);
            var innerA = HexCorner(innerRadius, i);
            var innerB = HexCorner(innerRadius, (i + 1) % 6);
            var start = vertices.Count;
            vertices.Add(new Vector3(outerA.x, y, outerA.y)); uvs.Add(TopUv(outerA, outerRadius));
            vertices.Add(new Vector3(outerB.x, y, outerB.y)); uvs.Add(TopUv(outerB, outerRadius));
            vertices.Add(new Vector3(innerA.x, y, innerA.y)); uvs.Add(TopUv(innerA, outerRadius));
            vertices.Add(new Vector3(innerB.x, y, innerB.y)); uvs.Add(TopUv(innerB, outerRadius));
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start + 2);
            triangles.Add(start + 1);
            triangles.Add(start + 3);
        }

        var mesh = MeshFrom(vertices, uvs, triangles, "HexRing");
        generatedMeshCache[key] = mesh;
        return mesh;
    }

    private Mesh CylinderMesh(float bottomRadius, float topRadius, float height, int segments)
    {
        var key = MeshCacheKey("cylinder", bottomRadius, topRadius, height, segments);
        if (generatedMeshCache.TryGetValue(key, out var cached) && cached != null)
        {
            return cached;
        }

        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();
        var half = height * 0.5f;

        for (var i = 0; i < segments; i++)
        {
            var a0 = Mathf.PI * 2f * i / segments;
            var a1 = Mathf.PI * 2f * (i + 1) / segments;
            var b0 = new Vector3(Mathf.Cos(a0) * bottomRadius, -half, Mathf.Sin(a0) * bottomRadius);
            var b1 = new Vector3(Mathf.Cos(a1) * bottomRadius, -half, Mathf.Sin(a1) * bottomRadius);
            var t0 = new Vector3(Mathf.Cos(a0) * topRadius, half, Mathf.Sin(a0) * topRadius);
            var t1 = new Vector3(Mathf.Cos(a1) * topRadius, half, Mathf.Sin(a1) * topRadius);
            var start = vertices.Count;
            vertices.Add(t0); uvs.Add(new Vector2(i / (float)segments, 0f));
            vertices.Add(t1); uvs.Add(new Vector2((i + 1) / (float)segments, 0f));
            vertices.Add(b0); uvs.Add(new Vector2(i / (float)segments, 1f));
            vertices.Add(b1); uvs.Add(new Vector2((i + 1) / (float)segments, 1f));
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start + 2);
            triangles.Add(start + 1);
            triangles.Add(start + 3);

            start = vertices.Count;
            vertices.Add(Vector3.up * half); uvs.Add(new Vector2(0.5f, 0.5f));
            vertices.Add(t1); uvs.Add(new Vector2(1f, 0f));
            vertices.Add(t0); uvs.Add(new Vector2(0f, 0f));
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }

        var mesh = MeshFrom(vertices, uvs, triangles, "Cylinder");
        generatedMeshCache[key] = mesh;
        return mesh;
    }

    private GameObject CreateCone(string name, float bottomRadius, float topRadius, float height, Material material)
    {
        var cone = new GameObject(name);
        var filter = cone.AddComponent<MeshFilter>();
        var renderer = cone.AddComponent<MeshRenderer>();
        filter.sharedMesh = CylinderMesh(bottomRadius, topRadius, height, 6);
        renderer.sharedMaterial = material;
        return cone;
    }

    private void AddRock(Transform parent, float elevation, float x, float z, int index)
    {
        if (TryPlacePrefab(Prefabs.rockPrefabs, parent, "RockPrefab", new Vector3(x, elevation + 0.06f + index * 0.02f, z), Quaternion.Euler(7f, 25f + index * 40f, 4f), new Vector3(0.22f, 0.14f + index * 0.06f, 0.28f), index, Mathf.RoundToInt(x * 100f), 8101, out _))
        {
            return;
        }

        var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rock.name = "Rock";
        rock.transform.SetParent(parent, false);
        rock.transform.localPosition = new Vector3(x, elevation + 0.09f + index * 0.03f, z);
        rock.transform.localScale = new Vector3(0.22f, 0.14f + index * 0.06f, 0.28f);
        rock.transform.localRotation = Quaternion.Euler(7f, 25f + index * 40f, 4f);
        rock.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["Rock"];
    }

    private GameObject CreateFacetedPeak(string name, float baseRadius, float shoulderRadius, float tipRadius, float height, Material material, int seedOffset)
    {
        var peak = new GameObject(name);
        var filter = peak.AddComponent<MeshFilter>();
        var renderer = peak.AddComponent<MeshRenderer>();
        filter.sharedMesh = FacetedPeakMesh(baseRadius, shoulderRadius, tipRadius, height, seedOffset);
        renderer.sharedMaterial = material;
        return peak;
    }

    private GameObject CreateFacetedMound(string name, float baseRadius, float topRadius, float height, Material material, int seedOffset)
    {
        var mound = new GameObject(name);
        var filter = mound.AddComponent<MeshFilter>();
        var renderer = mound.AddComponent<MeshRenderer>();
        filter.sharedMesh = FacetedMoundMesh(baseRadius, topRadius, height, seedOffset);
        renderer.sharedMaterial = material;
        return mound;
    }

    private Mesh FacetedPeakMesh(float baseRadius, float shoulderRadius, float tipRadius, float height, int seedOffset)
    {
        const int segments = 7;
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        var shoulderY = height * 0.44f;
        var apexAngle = Hash01(seedOffset, 17, 26097) * Mathf.PI * 2f;
        var apexOffset = tipRadius * Mathf.Lerp(0.15f, 0.55f, Hash01(seedOffset, 19, 26099));
        var apexIndex = vertices.Count;
        vertices.Add(new Vector3(Mathf.Cos(apexAngle) * apexOffset, height, Mathf.Sin(apexAngle) * apexOffset));
        uvs.Add(new Vector2(0.5f, 1f));

        for (var i = 0; i < segments; i++)
        {
            var angle = Mathf.PI * 2f * i / segments;
            var wobble = Mathf.Lerp(0.84f, 1.14f, Hash01(seedOffset, i, 26000));
            var shoulderWobble = Mathf.Lerp(0.82f, 1.12f, Hash01(seedOffset, i, 26031));
            vertices.Add(new Vector3(Mathf.Cos(angle) * baseRadius * wobble, 0f, Mathf.Sin(angle) * baseRadius * wobble));
            uvs.Add(new Vector2(i / (float)segments, 0f));
            vertices.Add(new Vector3(Mathf.Cos(angle + 0.08f) * shoulderRadius * shoulderWobble, shoulderY, Mathf.Sin(angle + 0.08f) * shoulderRadius * shoulderWobble));
            uvs.Add(new Vector2(i / (float)segments, 0.55f));
        }

        for (var i = 0; i < segments; i++)
        {
            var next = (i + 1) % segments;
            var baseA = 1 + i * 2;
            var shoulderA = baseA + 1;
            var baseB = 1 + next * 2;
            var shoulderB = baseB + 1;

            triangles.Add(baseA);
            triangles.Add(shoulderA);
            triangles.Add(shoulderB);
            triangles.Add(baseA);
            triangles.Add(shoulderB);
            triangles.Add(baseB);

            triangles.Add(shoulderA);
            triangles.Add(apexIndex);
            triangles.Add(shoulderB);
        }

        return MeshFrom(vertices, uvs, triangles, "FacetedPeak");
    }

    private Mesh FacetedMoundMesh(float baseRadius, float topRadius, float height, int seedOffset)
    {
        const int segments = 8;
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        vertices.Add(Vector3.up * height);
        uvs.Add(new Vector2(0.5f, 0.5f));

        for (var i = 0; i < segments; i++)
        {
            var angle = Mathf.PI * 2f * i / segments;
            var wobble = Mathf.Lerp(0.82f, 1.18f, Hash01(seedOffset, i, 27000));
            var topWobble = Mathf.Lerp(0.8f, 1.13f, Hash01(seedOffset, i, 27041));
            vertices.Add(new Vector3(Mathf.Cos(angle) * baseRadius * wobble, 0f, Mathf.Sin(angle) * baseRadius * wobble));
            uvs.Add(new Vector2(i / (float)segments, 1f));
            vertices.Add(new Vector3(Mathf.Cos(angle + 0.12f) * topRadius * topWobble, height, Mathf.Sin(angle + 0.12f) * topRadius * topWobble));
            uvs.Add(new Vector2(i / (float)segments, 0f));
        }

        for (var i = 0; i < segments; i++)
        {
            var next = (i + 1) % segments;
            var baseA = 1 + i * 2;
            var topA = baseA + 1;
            var baseB = 1 + next * 2;
            var topB = baseB + 1;

            triangles.Add(baseA);
            triangles.Add(topA);
            triangles.Add(topB);
            triangles.Add(baseA);
            triangles.Add(topB);
            triangles.Add(baseB);

            triangles.Add(0);
            triangles.Add(topB);
            triangles.Add(topA);
        }

        return MeshFrom(vertices, uvs, triangles, "FacetedMound");
    }

    private void BuildWaterPlane()
    {
        var boardSize = BoardWorldSize();
        var center = BoardWorldCenter();
        var parent = currentBuildRoot != null ? currentBuildRoot : transform;

        // Earthy shelf just under the tiles: the board reads as a landmass instead of
        // floating hexes over a hard cliff, and it hides the jagged outer silhouette.
        var shelf = GameObject.CreatePrimitive(PrimitiveType.Plane);
        shelf.name = "IslandShelf";
        shelf.transform.SetParent(parent, false);
        shelf.transform.localPosition = new Vector3(center.x, VisualTileBottomY - 0.012f, center.z);
        shelf.transform.localScale = new Vector3(boardSize.x * 0.108f, 1f, boardSize.y * 0.108f);
        shelf.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["BoardShelf"];

        // Pale surf ring hugging the shoreline, just outside the shelf.
        var foam = GameObject.CreatePrimitive(PrimitiveType.Plane);
        foam.name = "CoastFoam";
        foam.transform.SetParent(parent, false);
        foam.transform.localPosition = new Vector3(center.x, VisualTileBottomY - 0.055f, center.z);
        foam.transform.localScale = new Vector3(boardSize.x * 0.114f, 1f, boardSize.y * 0.114f);
        foam.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["CoastFoam"];

        // Translucent shallows ring fades the coastline from land into deeper water.
        var shallows = GameObject.CreatePrimitive(PrimitiveType.Plane);
        shallows.name = "CoastalShallows";
        shallows.transform.SetParent(parent, false);
        shallows.transform.localPosition = new Vector3(center.x, VisualTileBottomY - 0.14f, center.z);
        shallows.transform.localScale = new Vector3(boardSize.x * 0.123f, 1f, boardSize.y * 0.123f);
        shallows.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["ShallowWater"];

        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "DistantWaterPlane";
        plane.transform.SetParent(parent, false);
        plane.transform.localPosition = new Vector3(center.x, -0.42f, center.z);
        plane.transform.localScale = new Vector3(boardSize.x * 0.16f, 1f, boardSize.y * 0.16f);
        plane.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["WaterPlane"];

        // Larger, darker layer underneath for a sense of depth toward the horizon.
        var deep = GameObject.CreatePrimitive(PrimitiveType.Plane);
        deep.name = "DeepWaterPlane";
        deep.transform.SetParent(parent, false);
        deep.transform.localPosition = new Vector3(center.x, -0.72f, center.z);
        deep.transform.localScale = new Vector3(boardSize.x * 0.3f, 1f, boardSize.y * 0.3f);
        deep.GetComponent<MeshRenderer>().sharedMaterial = featureMaterials["DeepWater"];
    }

    private Vector3 BoardWorldCenter()
    {
        if (tiles.Count == 0)
        {
            return Vector3.zero;
        }

        var minX = float.MaxValue;
        var minZ = float.MaxValue;
        var maxX = float.MinValue;
        var maxZ = float.MinValue;
        foreach (var tile in tiles.Values)
        {
            minX = Mathf.Min(minX, tile.World.x);
            minZ = Mathf.Min(minZ, tile.World.z);
            maxX = Mathf.Max(maxX, tile.World.x);
            maxZ = Mathf.Max(maxZ, tile.World.z);
        }

        return new Vector3((minX + maxX) * 0.5f, 0f, (minZ + maxZ) * 0.5f);
    }

    private void BuildLighting()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = ColorFromHex("738083");
        RenderSettings.fogDensity = 0.006f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ColorFromHex("9aa091");
        RenderSettings.ambientEquatorColor = ColorFromHex("5c6356");
        RenderSettings.ambientGroundColor = ColorFromHex("2a3128");
        RenderSettings.ambientIntensity = 0.55f;

        // Warm key light at a lower angle for longer, more sculpted shadows.
        var sun = NewChild("LateAfternoonSun");
        var light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = ColorFromHex("ffd98f");
        light.intensity = 1.25f;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.72f;
        // Cloud-shadow cookie disabled for now (CloudCookieTexture / SetClouds kept for a later rework).
        sun.transform.localRotation = Quaternion.Euler(42f, -46f, 0f);

        // Cool sky fill from the opposite side keeps shadows from going flat-black.
        var fill = NewChild("SoftBlueFill");
        var fillLight = fill.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.color = ColorFromHex("94b0c0");
        fillLight.intensity = 0.32f;
        fill.transform.localRotation = Quaternion.Euler(38f, 150f, 0f);

        // Subtle cool rim/back light to separate raised features from the ground.
        var rim = NewChild("CoolRimLight");
        var rimLight = rim.AddComponent<Light>();
        rimLight.type = LightType.Directional;
        rimLight.color = ColorFromHex("bcd0d6");
        rimLight.intensity = 0.22f;
        rim.transform.localRotation = Quaternion.Euler(18f, 96f, 0f);

        // Ambient animation: flowing water + drifting cloud shadows (play mode only).
        var atmosphere = NewChild("Atmosphere").AddComponent<HexMapAtmosphere>();
        atmosphere.AddWater(featureMaterials["River"], new Vector2(0.14f, 0f));
        atmosphere.AddWater(featureMaterials["ShallowWater"], new Vector2(0.015f, 0.01f));
        atmosphere.AddWater(featureMaterials["WaterPlane"], new Vector2(0.01f, 0.008f));
        atmosphere.AddWater(featureMaterials["DeepWater"], new Vector2(0.008f, 0.006f));

        BuildPostProcessing();
    }

    private void BuildPostProcessing()
    {
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.hideFlags = HideFlags.DontSave;

        var tonemapping = profile.Add<Tonemapping>(true);
        tonemapping.mode.Override(TonemappingMode.Neutral);

        var colorAdjustments = profile.Add<ColorAdjustments>(true);
        colorAdjustments.postExposure.Override(0.04f);
        colorAdjustments.contrast.Override(7f);
        colorAdjustments.saturation.Override(5f);
        colorAdjustments.colorFilter.Override(ColorFromHex("fff2df"));

        var whiteBalance = profile.Add<WhiteBalance>(true);
        whiteBalance.temperature.Override(6f);

        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.92f);
        bloom.intensity.Override(0.4f);
        bloom.scatter.Override(0.62f);
        bloom.tint.Override(ColorFromHex("f4ead2"));

        var vignette = profile.Add<Vignette>(true);
        vignette.color.Override(ColorFromHex("161b18"));
        vignette.intensity.Override(0.3f);
        vignette.smoothness.Override(0.45f);

        var volumeObject = NewChild("Global Post Processing");
        var volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.sharedProfile = profile;
    }

    private void BuildCamera()
    {
        var rig = NewChild("StrategyCameraRig");
        cameraRig = rig.transform;
        if (useCoreTutorialState && coreGameState != null)
        {
            var coord = CoreCoordToViewCoord(coreGameState.Expedition.Position);
            if (tiles.TryGetValue(coord, out var tile))
            {
                cameraRig.localPosition = new Vector3(tile.World.x, 0f, tile.World.z);
            }
        }

        var cameraObject = NewChild("StrategyCamera", rig.transform);
        var camera = cameraObject.AddComponent<Camera>();
        strategyCamera = camera;
        camera.orthographic = true;
        var boardSize = BoardWorldSize();
        var fittedSize = Mathf.Max(boardSize.x * 0.31f, boardSize.y * 0.5f);
        activeZoomedOutSize = Mathf.Max(zoomedOutSize, fittedSize);
        camera.orthographicSize = Mathf.Clamp(fittedSize, zoomedInSize, activeZoomedOutSize);
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 180f;
        camera.backgroundColor = ColorFromHex("30383a");
        camera.clearFlags = CameraClearFlags.SolidColor;

        var cameraData = camera.GetUniversalAdditionalCameraData();
        if (cameraData != null)
        {
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cameraData.antialiasingQuality = AntialiasingQuality.High;
        }

        cameraObject.transform.localPosition = new Vector3(10.6f, 13.8f, 13.2f);
        cameraObject.transform.LookAt(cameraRig.position + Vector3.up * 0.35f, Vector3.up);
    }

    private void BuildHud()
    {
        if (coreGameState == null)
        {
            return;
        }

        if (expeditionScreenController == null)
        {
            expeditionScreenController = FindObjectOfType<ExpeditionScreenController>();
        }

        if (expeditionScreenController == null)
        {
            Debug.LogWarning("ExpeditionScreenController is missing. Add an Expedition HUD UIDocument scene object and assign the project UI assets there.");
            return;
        }

        expeditionScreenController.Initialize(this);
    }

    private void RefreshHud()
    {
        RefreshToolkitHud();
    }

    private void FocusCameraOnCoord(Vector2Int coord)
    {
        if (cameraRig == null || !tiles.TryGetValue(coord, out var tile))
        {
            return;
        }

        cameraRig.localPosition = new Vector3(tile.World.x, cameraRig.localPosition.y, tile.World.z);
    }

    private void UpdateCameraZoom()
    {
        if (strategyCamera == null)
        {
            return;
        }

        var input = Input.mouseScrollDelta.y;
        if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus))
        {
            input += 1f;
        }

        if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus))
        {
            input -= 1f;
        }

        if (Mathf.Abs(input) <= 0.001f)
        {
            return;
        }

        var targetSize = strategyCamera.orthographicSize - input * zoomSpeed * Time.deltaTime * 12f;
        strategyCamera.orthographicSize = Mathf.Clamp(targetSize, zoomedInSize, activeZoomedOutSize);
    }

    private GameObject AddMesh(GameObject parent, string name, Mesh mesh, Material material)
    {
        var go = NewChild(name, parent.transform);
        var filter = go.AddComponent<MeshFilter>();
        var renderer = go.AddComponent<MeshRenderer>();
        filter.sharedMesh = mesh;
        renderer.sharedMaterial = material;
        return go;
    }

    private GameObject NewChild(string childName)
    {
        return NewChild(childName, currentBuildRoot != null ? currentBuildRoot : transform);
    }

    private Transform NewRootGroup(string groupName)
    {
        return NewChild(groupName, transform).transform;
    }

    private static GameObject NewChild(string childName, Transform parent)
    {
        var go = new GameObject(childName);
        go.transform.SetParent(parent, false);
        return go;
    }

    private bool TryPlacePrefab(GameObject[] prefabs, Transform parent, string objectName, Vector3 localPosition, Quaternion localRotation, Vector3 scaleMultiplier, int seedA, int seedB, int seedOffset, out GameObject instance)
    {
        instance = null;
        if (!usePrefabOverrides || prefabs == null || prefabs.Length == 0)
        {
            return false;
        }

        var prefab = PickPrefab(prefabs, seedA, seedB, seedOffset);
        if (prefab == null)
        {
            return false;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            instance = UnityEditor.PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        }
#endif
        if (instance == null)
        {
            instance = Instantiate(prefab, parent);
        }
        else
        {
            instance.transform.SetParent(parent, false);
        }

        instance.name = objectName;
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = localRotation;
        var prefabScale = instance.transform.localScale;
        instance.transform.localScale = new Vector3(prefabScale.x * scaleMultiplier.x, prefabScale.y * scaleMultiplier.y, prefabScale.z * scaleMultiplier.z);
        return true;
    }

    private void EnsurePrefabLibrary()
    {
        if (prefabLibrary != null || warnedMissingPrefabLibrary)
        {
            return;
        }

        warnedMissingPrefabLibrary = true;
        Debug.LogWarning("HexMapPrefabLibrary is not assigned. Assign Assets/Settings/DefaultHexMapPrefabLibrary.asset on UnityHexMapView.");
    }

    private static bool HasPrefab(GameObject[] prefabs)
    {
        if (prefabs == null)
        {
            return false;
        }

        for (var i = 0; i < prefabs.Length; i++)
        {
            if (prefabs[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private GameObject PickPrefab(GameObject[] prefabs, int seedA, int seedB, int seedOffset)
    {
        var validCount = 0;
        for (var i = 0; i < prefabs.Length; i++)
        {
            if (prefabs[i] != null)
            {
                validCount++;
            }
        }

        if (validCount == 0)
        {
            return null;
        }

        var target = Mathf.FloorToInt(Hash01(seedA, seedB, seedOffset) * validCount);
        var validIndex = 0;
        for (var i = 0; i < prefabs.Length; i++)
        {
            if (prefabs[i] == null)
            {
                continue;
            }

            if (validIndex == target)
            {
                return prefabs[i];
            }

            validIndex++;
        }

        for (var i = 0; i < prefabs.Length; i++)
        {
            if (prefabs[i] != null)
            {
                return prefabs[i];
            }
        }

        return null;
    }

    private Mesh MeshFrom(List<Vector3> vertices, List<Vector2> uvs, List<int> triangles, string meshName)
    {
        var mesh = new Mesh { name = meshName, hideFlags = HideFlags.DontSave };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static string MeshCacheKey(string prefix, params float[] values)
    {
        var key = prefix;
        foreach (var value in values)
        {
            key += ":" + Mathf.RoundToInt(value * 10000f);
        }

        return key;
    }

    private static string MeshCacheKey(string prefix, float valueA, float valueB, float valueC, int valueD)
    {
        return prefix + ":" +
            Mathf.RoundToInt(valueA * 10000f) + ":" +
            Mathf.RoundToInt(valueB * 10000f) + ":" +
            Mathf.RoundToInt(valueC * 10000f) + ":" +
            valueD;
    }

    private Vector3 AxialToWorld(Vector2Int coord)
    {
        var x = hexSize * Sqrt3 * (coord.x + coord.y * 0.5f);
        var z = hexSize * 1.5f * coord.y;
        return new Vector3(x, 0f, z);
    }

    private Vector2Int OffsetToCenteredAxial(int column, int row)
    {
        var centeredRow = row - mapHeight / 2;
        var centeredColumn = column - mapWidth / 2;
        var q = centeredColumn - (centeredRow - (centeredRow & 1)) / 2;
        return new Vector2Int(q, centeredRow);
    }

    private Vector2 BoardWorldSize()
    {
        if (tiles.Count > 0)
        {
            var minX = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var minZ = float.PositiveInfinity;
            var maxZ = float.NegativeInfinity;
            foreach (var tile in tiles.Values)
            {
                minX = Mathf.Min(minX, tile.World.x);
                maxX = Mathf.Max(maxX, tile.World.x);
                minZ = Mathf.Min(minZ, tile.World.z);
                maxZ = Mathf.Max(maxZ, tile.World.z);
            }

            return new Vector2(
                maxX - minX + Sqrt3 * hexSize,
                maxZ - minZ + 2f * hexSize);
        }

        var width = Sqrt3 * hexSize * (mapWidth + 0.5f);
        var height = 1.5f * hexSize * (mapHeight + 0.5f);
        return new Vector2(width, height);
    }

    private static Vector2 HexCorner(float radius, int index)
    {
        var angle = Mathf.Deg2Rad * (60f * index + 30f);
        return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
    }

    private static Vector2 TopUv(Vector2 point, float radius)
    {
        return new Vector2(point.x / (radius * 2f) + 0.5f, point.y / (radius * 2f) + 0.5f);
    }

    private float Noise(float x, float y, int offset)
    {
        var seed = (mapSeed + offset * 131) * 0.013f;
        var a = Mathf.PerlinNoise(x + seed, y - seed) * 2f - 1f;
        var b = Mathf.PerlinNoise(x * 2.1f - seed, y * 2.1f + seed) * 2f - 1f;
        return a * 0.72f + b * 0.28f;
    }

    private float Hash01(int a, int b, int c)
    {
        return HexVisualHash.Value01(ActiveVisualSeed(), a, b, c);
    }

    // Generated campaigns use their own world-gen seed for visual variety; the tutorial map (and
    // any other non-generated game) falls back to the inspector-exposed mapSeed.
    private int ActiveVisualSeed()
    {
        return generatedCampaignRequest != null ? unchecked((int)generatedCampaignRequest.Seed) : mapSeed;
    }

    private static Color ColorFromHex(string hex)
    {
        if (ColorUtility.TryParseHtmlString("#" + hex, out var color))
        {
            return color;
        }

        return Color.white;
    }
}
