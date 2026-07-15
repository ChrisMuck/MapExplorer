using System;
using System.Collections.Generic;
using System.Linq;
using Game.App;
using Game.Core;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public sealed class ExpeditionScreenController : MonoBehaviour
{
    private enum LocationInteractionView
    {
        Actions,
        Outcome,
        Project
    }

    private const string Reports = "reports";
    private const string Scouts = "scouts";
    private const string Journal = "journal";
    private const string Archive = "archive";

    private UnityHexMapView mapView;
    private VisualElement root;
    private VisualElement sidePanel;
    private Label sidePanelTitle;
    private string openSection;
    private string openLocationInteractionId;
    private string selectedLocationActionId;
    private string locationInteractionMessage;
    private LocationActionResult latestLocationActionResult;
    private LocationInteractionView locationInteractionView = LocationInteractionView.Actions;
    private bool isCompact;
    private bool isNarrow;
    private Vector2 lastResponsiveSize;
    private int selectedReportIndex = -1;
    private int selectedHintIndex = -1;
    private readonly HashSet<string> selectedScoutIds = new HashSet<string>();
    private ScoutDirection scoutDirection = ScoutDirection.East;
    private int scoutDurationDays = 2;
    private ScoutMissionFocus scoutFocus = ScoutMissionFocus.Survey;
    private ScoutMissionBehavior scoutBehavior = ScoutMissionBehavior.Balanced;
    private readonly List<WorldGenerationPreset> campaignPresets = new List<WorldGenerationPreset>();
    private readonly List<WorldGenerationOptionPreset> campaignOptionPresets = new List<WorldGenerationOptionPreset>();
    private string campaignPresetId = "medium";
    private int campaignFactionCount = 3;
    private string campaignFactionMood = "Gemischt";
    private string campaignCoastlineId = "balanced";
    private string campaignTerrainId = "hilly";
    private string campaignActivityId = "normal";
    private string campaignSettlementId = "balanced";

    public void Initialize(UnityHexMapView view)
    {
        mapView = view;
        Bind();
        Refresh();
    }

    /// <summary>Hides/shows the whole expedition overlay (used while the base-camp screen is open).</summary>
    public void SetScreenVisible(bool visible)
    {
        if (root == null)
        {
            var document = GetComponent<UIDocument>();
            root = document != null ? document.rootVisualElement : null;
        }

        if (root != null)
        {
            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private void OnEnable()
    {
        if (mapView == null)
        {
            mapView = FindObjectOfType<UnityHexMapView>();
        }

        Bind();
        Refresh();
    }

    private void Bind()
    {
        var document = GetComponent<UIDocument>();
        if (document == null || document.rootVisualElement == null)
        {
            return;
        }

        root = document.rootVisualElement;
        sidePanel = root.Q<VisualElement>("side-panel");
        sidePanelTitle = root.Q<Label>("side-panel-title");
        root.UnregisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
        root.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
        ApplyResponsiveClasses(root.resolvedStyle.width, root.resolvedStyle.height);

        MakeMapAreaClickThrough();
        SetDisplay("rail-expedition", false);
        RegisterRail("rail-scouts", Scouts);
        RegisterRail("rail-reports", Reports);
        RegisterRail("rail-journal", Journal);
        RegisterRail("rail-archive", Archive);

        RegisterClick("link-reports", () => Open(Reports));
        RegisterClick("side-panel-close", CloseSide);
        RegisterClick("action-open-report", OpenSelectedReport);
        RegisterClick("action-marker-from-report", () => mapView?.RequestMarkerFromReportHintFromUi(selectedReportIndex, selectedHintIndex));
        RegisterClick("action-send-scout", OpenScoutMissionPopup);
        RegisterClick("action-send-scout-panel", OpenScoutMissionPopup);
        RegisterClick("action-add-marker", () => mapView?.RequestAddMarkerFromUi());
        RegisterClick("action-add-note", () => mapView?.RequestAddNoteFromUi());
        RegisterClick("action-inspect", HandleInspectAction);
        RegisterClick("selected-location-action", HandleInspectAction);
        RegisterClick("btn-close", CloseLocationInteractionPopup);
        RegisterClick("btn-back-from-outcome", ShowLocationActions);
        RegisterClick("btn-back-from-project", ShowLocationActions);
        RegisterClick("btn-advance-project", AdvanceLocationProject);
        RegisterClick("action-camp", () => mapView?.RequestPrepareSuppliesWithKnowledgeFromUi());
        RegisterClick("action-return-base", () => mapView?.RequestCompleteExpeditionFromUi());
        RegisterClick("action-end-day", () => mapView?.RequestEndDayFromUi());
        RegisterClick("faction-modal-close", () => mapView?.RequestCloseFactionInteractionFromUi());
        RegisterClick("faction-response-leave", () => mapView?.RequestCloseFactionInteractionFromUi());
        RegisterClick("scout-modal-close", CloseScoutMissionPopup);
        RegisterClick("scout-mission-send", SendSelectedScoutMission);
        RegisterScoutMissionOptions();

        RegisterClick("action-open-base", () => mapView?.RequestOpenBaseCampFromUi());

        RegisterClick("campaign-size-small", () => SetCampaignPreset("small"));
        RegisterClick("campaign-size-medium", () => SetCampaignPreset("medium"));
        RegisterClick("campaign-size-large", () => SetCampaignPreset("large"));
        RegisterClick("campaign-size-huge", () => SetCampaignPreset("huge"));
        RegisterClick("campaign-size-gigantic", () => SetCampaignPreset("gigantic"));
        RegisterClick("campaign-factions-2", () => SetCampaignFactionCount(2));
        RegisterClick("campaign-factions-3", () => SetCampaignFactionCount(3));
        RegisterClick("campaign-factions-4", () => SetCampaignFactionCount(4));
        RegisterClick("campaign-mood-peaceful", () => SetCampaignFactionMood("Friedlich"));
        RegisterClick("campaign-mood-mixed", () => SetCampaignFactionMood("Gemischt"));
        RegisterClick("campaign-mood-hostile", () => SetCampaignFactionMood("Feindselig"));
        RegisterClick("campaign-coast-compact", () => SetCampaignOption("coastline", "compact"));
        RegisterClick("campaign-coast-balanced", () => SetCampaignOption("coastline", "balanced"));
        RegisterClick("campaign-coast-rugged", () => SetCampaignOption("coastline", "rugged"));
        RegisterClick("campaign-terrain-flat", () => SetCampaignOption("terrain", "flat"));
        RegisterClick("campaign-terrain-hilly", () => SetCampaignOption("terrain", "hilly"));
        RegisterClick("campaign-terrain-mountainous", () => SetCampaignOption("terrain", "mountainous"));
        RegisterClick("campaign-activity-quiet", () => SetCampaignOption("activity", "quiet"));
        RegisterClick("campaign-activity-normal", () => SetCampaignOption("activity", "normal"));
        RegisterClick("campaign-activity-active", () => SetCampaignOption("activity", "active"));
        RegisterClick("campaign-settlement-wilderness", () => SetCampaignOption("settlement", "wilderness"));
        RegisterClick("campaign-settlement-balanced", () => SetCampaignOption("settlement", "balanced"));
        RegisterClick("campaign-settlement-dense", () => SetCampaignOption("settlement", "dense"));
        RegisterClick("campaign-begin", BeginGeneratedCampaign);
        EnsureCampaignPresets();

        if (string.IsNullOrEmpty(openSection))
        {
            CloseSide();
        }
        else
        {
            Open(openSection);
        }
    }

    private void OnRootGeometryChanged(GeometryChangedEvent evt)
    {
        if (Mathf.Abs(evt.newRect.width - lastResponsiveSize.x) < 8f &&
            Mathf.Abs(evt.newRect.height - lastResponsiveSize.y) < 8f)
        {
            return;
        }

        ApplyResponsiveClasses(evt.newRect.width, evt.newRect.height);
    }

    private void ApplyResponsiveClasses(float width, float height)
    {
        if (root == null || width <= 0f || height <= 0f)
        {
            return;
        }

        lastResponsiveSize = new Vector2(width, height);

        var nextCompact = width < 1800f || height < 1000f;
        var nextNarrow = width < 1350f || height < 760f;
        if (nextCompact == isCompact && nextNarrow == isNarrow)
        {
            return;
        }

        isCompact = nextCompact;
        isNarrow = nextNarrow;
        root.EnableInClassList("ui-compact", isCompact);
        root.EnableInClassList("ui-narrow", isNarrow);
    }

    public void Refresh()
    {
        if (root == null || mapView == null || mapView.CurrentGameState == null)
        {
            return;
        }

        var state = mapView.CurrentGameState;
        SetText("value-expedition-day", $"Expedition · Tag {state.Expedition.ExpeditionDay}");
        SetText("value-world-day", $"Welt · Tag {state.World.WorldDay}");
        SetText("value-supplies", state.Expedition.Supplies.ToString());
        SetText("value-medicine", state.Expedition.Medicine.ToString());
        SetText("value-morale", MoraleText(state.Expedition.Morale));
        SetText("value-party", state.Expedition.Members.Count.ToString());
        SetText("value-mp", $"{state.Expedition.MovementPoints}/{state.Expedition.MaxMovementPoints}");
        RefreshKnowledgeStats(state);
        if (state.Expedition.Status == ExpeditionStatus.Returned)
        {
            SetText("value-expedition-day", "Expedition Â· Abgeschlossen");
        }

        RefreshSelectedField(state);
        RefreshAlert(state);
        RefreshReport(state);
        RefreshActionBar(state);
        BuildArchiveList(state);
        RefreshEventPopup(state);
        RefreshLocationInteractionPopup(state);
        RefreshFactionInteractionPopup(state);
        RefreshScoutMissionPopup(state);
        RefreshCampaignSetup();
    }

    private void EnsureCampaignPresets()
    {
        if (campaignPresets.Count > 0)
        {
            return;
        }

        var catalog = mapView?.WorldGenerationCatalog;
        if (catalog == null)
        {
            Debug.LogError("World generation presets are unavailable because the shared game-data catalog was not loaded.");
            return;
        }

        campaignPresets.AddRange(catalog.Sizes);
        campaignOptionPresets.AddRange(catalog.Options);
    }

    private void SetCampaignPreset(string presetId)
    {
        if (campaignPresets.Any(preset => preset.Id == presetId))
        {
            campaignPresetId = presetId;
            RefreshCampaignSetup();
        }
    }

    private void SetCampaignFactionCount(int count)
    {
        campaignFactionCount = count;
        RefreshCampaignSetup();
    }

    private void SetCampaignFactionMood(string mood)
    {
        campaignFactionMood = mood;
        RefreshCampaignSetup();
    }

    private void SetCampaignOption(string categoryId, string optionId)
    {
        if (!campaignOptionPresets.Any(option => option.CategoryId == categoryId && option.Id == optionId))
        {
            return;
        }

        switch (categoryId)
        {
            case "coastline": campaignCoastlineId = optionId; break;
            case "terrain": campaignTerrainId = optionId; break;
            case "activity": campaignActivityId = optionId; break;
            case "settlement": campaignSettlementId = optionId; break;
        }

        RefreshCampaignSetup();
    }

    private void BeginGeneratedCampaign()
    {
        if (mapView == null)
        {
            return;
        }

        var preset = campaignPresets.FirstOrDefault(item => item.Id == campaignPresetId);
        if (preset == null)
        {
            SetText("campaign-message", "Die Weltvoreinstellungen konnten nicht geladen werden.");
            return;
        }

        var seed = unchecked((uint)UnityEngine.Random.Range(1, int.MaxValue));
        var overrides = new Dictionary<string, double>();
        AddCampaignOverrides(overrides, "coastline", campaignCoastlineId);
        AddCampaignOverrides(overrides, "terrain", campaignTerrainId);
        AddCampaignOverrides(overrides, "activity", campaignActivityId);
        AddCampaignOverrides(overrides, "settlement", campaignSettlementId);
        mapView.RequestStartGeneratedCampaignFromUi(new WorldGenerationRequest
        {
            Seed = seed,
            Width = preset.Width,
            Height = preset.Height,
            FactionCount = campaignFactionCount,
            FactionMood = campaignFactionMood,
            GeneratorOverrides = overrides
        });
    }

    private void AddCampaignOverrides(IDictionary<string, double> target, string categoryId, string optionId)
    {
        var option = campaignOptionPresets.FirstOrDefault(item => item.CategoryId == categoryId && item.Id == optionId);
        if (option == null)
        {
            return;
        }

        foreach (var pair in option.GeneratorOverrides)
        {
            target[pair.Key] = pair.Value;
        }
    }

    private void RefreshCampaignSetup()
    {
        if (root == null || mapView == null)
        {
            return;
        }

        var visible = !mapView.HasStartedCampaign;
        SetDisplay("campaign-setup", visible);
        if (!visible)
        {
            return;
        }

        SetCampaignSelected("campaign-size-small", campaignPresetId == "small");
        SetCampaignSelected("campaign-size-medium", campaignPresetId == "medium");
        SetCampaignSelected("campaign-size-large", campaignPresetId == "large");
        SetCampaignSelected("campaign-size-huge", campaignPresetId == "huge");
        SetCampaignSelected("campaign-size-gigantic", campaignPresetId == "gigantic");
        SetCampaignSelected("campaign-factions-2", campaignFactionCount == 2);
        SetCampaignSelected("campaign-factions-3", campaignFactionCount == 3);
        SetCampaignSelected("campaign-factions-4", campaignFactionCount == 4);
        SetCampaignSelected("campaign-mood-peaceful", campaignFactionMood == "Friedlich");
        SetCampaignSelected("campaign-mood-mixed", campaignFactionMood == "Gemischt");
        SetCampaignSelected("campaign-mood-hostile", campaignFactionMood == "Feindselig");
        SetCampaignSelected("campaign-coast-compact", campaignCoastlineId == "compact");
        SetCampaignSelected("campaign-coast-balanced", campaignCoastlineId == "balanced");
        SetCampaignSelected("campaign-coast-rugged", campaignCoastlineId == "rugged");
        SetCampaignSelected("campaign-terrain-flat", campaignTerrainId == "flat");
        SetCampaignSelected("campaign-terrain-hilly", campaignTerrainId == "hilly");
        SetCampaignSelected("campaign-terrain-mountainous", campaignTerrainId == "mountainous");
        SetCampaignSelected("campaign-activity-quiet", campaignActivityId == "quiet");
        SetCampaignSelected("campaign-activity-normal", campaignActivityId == "normal");
        SetCampaignSelected("campaign-activity-active", campaignActivityId == "active");
        SetCampaignSelected("campaign-settlement-wilderness", campaignSettlementId == "wilderness");
        SetCampaignSelected("campaign-settlement-balanced", campaignSettlementId == "balanced");
        SetCampaignSelected("campaign-settlement-dense", campaignSettlementId == "dense");
        SetText("campaign-message", "Die Karte bleibt bis zum Aufbruch unbekannt.");
    }

    private void SetCampaignSelected(string elementName, bool selected)
    {
        root?.Q<Label>(elementName)?.EnableInClassList("campaign-choice--selected", selected);
    }

    public void OpenLocationInteraction(string locationId)
    {
        if (string.IsNullOrWhiteSpace(locationId))
        {
            return;
        }

        openLocationInteractionId = locationId;
        selectedLocationActionId = null;
        locationInteractionMessage = null;
        latestLocationActionResult = null;
        locationInteractionView = LocationInteractionView.Actions;
        SetDisplay("location-interaction-popup", true);
        RefreshLocationInteractionPopup(mapView?.CurrentGameState);
    }

    private void RefreshKnowledgeStats(GameState state)
    {
        SetText("value-field-knowledge", state.Expedition.UnsecuredKnowledge.ToString());
        SetText("value-base-knowledge", state.Base.KnowledgePoints.ToString());
    }

    private void OpenSelectedReport()
    {
        Open(Reports);
        mapView?.RequestOpenScoutReportFromUi(selectedReportIndex);
        Refresh();
    }

    private void OpenScoutMissionPopup()
    {
        if (mapView == null)
        {
            return;
        }

        selectedScoutIds.Clear();
        foreach (var scout in mapView.GetAvailableScoutsForUi())
        {
            selectedScoutIds.Add(scout.Id);
            break;
        }

        SetDisplay("scout-mission-popup", true);
        RefreshScoutMissionPopup(mapView.CurrentGameState);
    }

    private void CloseScoutMissionPopup()
    {
        SetDisplay("scout-mission-popup", false);
    }

    private void SendSelectedScoutMission()
    {
        if (mapView == null || selectedScoutIds.Count == 0)
        {
            SetText("scout-mission-preview", "Waehle mindestens einen verfuegbaren Spaeher aus der aktuellen Expedition.");
            return;
        }

        var sent = mapView.RequestSendScoutMissionFromUi(
            new List<string>(selectedScoutIds),
            scoutDirection,
            scoutDurationDays,
            scoutFocus,
            scoutBehavior);

        if (sent)
        {
            CloseScoutMissionPopup();
            Refresh();
            return;
        }

        SetText("scout-mission-preview", mapView.CurrentInteractionMessage);
    }

    private void HandleInspectAction()
    {
        if (mapView == null || mapView.CurrentGameState == null)
        {
            return;
        }

        var coord = mapView.CurrentSelectedCoreCoord;
        var location = mapView.GetLocationForUi(coord);
        if (location != null &&
            mapView.GetKnowledgeForUi(coord) == KnowledgeLevel.Confirmed &&
            !string.IsNullOrWhiteSpace(location.ArchetypeId))
        {
            OpenLocationInteraction(location.Id);
            return;
        }

        mapView.RequestInspectSelectedLocationFromUi();
        Refresh();
    }

    private void RegisterScoutMissionOptions()
    {
        RegisterScoutDirection("scout-direction-north", ScoutDirection.North);
        RegisterScoutDirection("scout-direction-north-east", ScoutDirection.NorthEast);
        RegisterScoutDirection("scout-direction-east", ScoutDirection.East);
        RegisterScoutDirection("scout-direction-south-east", ScoutDirection.SouthEast);
        RegisterScoutDirection("scout-direction-south", ScoutDirection.South);
        RegisterScoutDirection("scout-direction-south-west", ScoutDirection.SouthWest);
        RegisterScoutDirection("scout-direction-west", ScoutDirection.West);
        RegisterScoutDirection("scout-direction-north-west", ScoutDirection.NorthWest);

        for (var duration = 1; duration <= 5; duration++)
        {
            var capturedDuration = duration;
            RegisterClick($"scout-duration-{duration}", () =>
            {
                scoutDurationDays = capturedDuration;
                RefreshScoutMissionPopup(mapView?.CurrentGameState);
            });
        }

        RegisterScoutFocus("scout-focus-survey", ScoutMissionFocus.Survey);
        RegisterScoutFocus("scout-focus-route", ScoutMissionFocus.Route);
        RegisterScoutFocus("scout-focus-resources", ScoutMissionFocus.Resources);
        RegisterScoutFocus("scout-focus-faction", ScoutMissionFocus.FactionSigns);
        RegisterScoutFocus("scout-focus-ruins", ScoutMissionFocus.Ruins);

        RegisterScoutBehavior("scout-behavior-cautious", ScoutMissionBehavior.Cautious);
        RegisterScoutBehavior("scout-behavior-balanced", ScoutMissionBehavior.Balanced);
        RegisterScoutBehavior("scout-behavior-bold", ScoutMissionBehavior.Bold);
    }

    private void RegisterScoutDirection(string elementName, ScoutDirection direction)
    {
        RegisterClick(elementName, () =>
        {
            scoutDirection = direction;
            RefreshScoutMissionPopup(mapView?.CurrentGameState);
        });
    }

    private void RegisterScoutFocus(string elementName, ScoutMissionFocus focus)
    {
        RegisterClick(elementName, () =>
        {
            scoutFocus = focus;
            RefreshScoutMissionPopup(mapView?.CurrentGameState);
        });
    }

    private void RegisterScoutBehavior(string elementName, ScoutMissionBehavior behavior)
    {
        RegisterClick(elementName, () =>
        {
            scoutBehavior = behavior;
            RefreshScoutMissionPopup(mapView?.CurrentGameState);
        });
    }

    private void RefreshActionBar(GameState state)
    {
        var atBase = state.Expedition.Position == state.Base.Location;
        var isActive = state.Expedition.Status == ExpeditionStatus.Active;
        var isEnded = state.Expedition.Status == ExpeditionStatus.Returned || state.Expedition.Status == ExpeditionStatus.Lost;
        var nextReady = isEnded && state.Base.CanStartNextExpedition(state.World.WorldDay);
        var canComplete = isActive && atBase;
        var completeButton = root?.Q<Label>("action-return-base");
        if (completeButton != null)
        {
            completeButton.text = state.Expedition.Status == ExpeditionStatus.Returned
                ? "Expedition abgeschlossen"
                : "Expedition abschließen";
            completeButton.EnableInClassList("disabled", !canComplete);
            completeButton.tooltip = canComplete
                ? "Expedition in der Basis abschließen"
                : "Nur auf dem Basisfeld möglich";
        }

        var endDayButton = root?.Q<Label>("action-end-day");
        if (endDayButton != null)
        {
            endDayButton.EnableInClassList("disabled", !isActive);
        }

        if (completeButton != null && isEnded)
        {
            completeButton.text = nextReady
                ? "Neue Expedition starten"
                : $"Neue Expedition ab Tag {state.Base.NextExpeditionAvailableWorldDay}";
            completeButton.EnableInClassList("disabled", !nextReady);
            completeButton.tooltip = nextReady
                ? "Naechste Expedition starten"
                : "In der Basis muss noch Zeit vergehen";
        }

        if (completeButton != null && !isEnded)
        {
            completeButton.text = "Expedition abschliessen";
            completeButton.tooltip = canComplete
                ? "Expedition in der Basis abschliessen"
                : "Nur auf dem Basisfeld moeglich";
        }

        if (endDayButton != null)
        {
            endDayButton.text = isEnded && !nextReady ? "Base-Zeit +1" : "Tag beenden ->";
            endDayButton.EnableInClassList("disabled", !isActive && !isEnded);
        }

        var campButton = root?.Q<Label>("action-camp");
        if (campButton != null)
        {
            var canPrepareSupplies = isEnded && state.Base.KnowledgePoints >= PrepareSuppliesWithKnowledgeCommand.KnowledgeCost;
            campButton.text = isEnded
                ? $"+20 Vorraete ({PrepareSuppliesWithKnowledgeCommand.KnowledgeCost} Wissen)"
                : "Lager";
            campButton.EnableInClassList("disabled", isActive || !canPrepareSupplies);
            campButton.tooltip = isEnded
                ? canPrepareSupplies
                    ? "Wissen fuer Vorratsvorbereitung der naechsten Expedition ausgeben"
                    : $"Benoetigt {PrepareSuppliesWithKnowledgeCommand.KnowledgeCost} Wissen"
                : "Lager ist spaeter als Feldaktion geplant";
        }

        // The base window is only available once the expedition has returned to the base field.
        var openBaseButton = root?.Q<Label>("action-open-base");
        if (openBaseButton != null)
        {
            var canOpenBase = atBase && isEnded;
            openBaseButton.EnableInClassList("disabled", !canOpenBase);
            openBaseButton.tooltip = canOpenBase
                ? "Basislager-Verwaltung oeffnen"
                : "Nur in der Basis nach Abschluss der Expedition";
        }
    }

    private void RefreshSelectedField(GameState state)
    {
        var coord = mapView.CurrentSelectedCoreCoord;
        var knowledge = mapView.GetKnowledgeForUi(coord);
        SetText("coord", $"Feld {coord.Q:00} / {coord.R:00}");
        SetText("selected-knowledge", KnowledgeText(knowledge));
        SetText("selected-reliability", "Verlässlichkeit: Mittel");

        SpecialLocationState location = null;
        if (mapView.TryGetTileForUi(coord, out var tile))
        {
            location = mapView.GetLocationForUi(coord);
            var locationText = location == null || knowledge != KnowledgeLevel.Confirmed ? "keine bestaetigte Landmarke" : location.Name;
            SetText("selected-meta", knowledge == KnowledgeLevel.Unknown
                ? "Noch nicht erkundet"
                : $"{tile.Terrain} · {locationText}");
        }
        else
        {
            SetText("selected-meta", "Außerhalb der Karte");
        }

        var lostExpedition = mapView.GetLostExpeditionForUi(coord);
        RefreshSelectedLocation(location, knowledge, lostExpedition, coord);

        var markers = mapView.GetMarkersForUi(coord);
        var notes = mapView.GetNotesForUi(coord);
        SetText("selected-sign-1", markers.Count > 0 ? markers[markers.Count - 1].Label : "Keine Marker");
        SetText("selected-sign-2", mapView.CurrentInteractionMessage);
        SetText("selected-note", notes.Count > 0 ? $"„{notes[notes.Count - 1].Text}”" : "„Noch keine Notiz für dieses Feld.”");
    }

    private void RefreshSelectedLocation(SpecialLocationState location, KnowledgeLevel knowledge, LostExpeditionRecord lostExpedition, HexCoord coord)
    {
        var isConfirmedLocation = location != null && knowledge == KnowledgeLevel.Confirmed;
        SetDisplay("selected-location-card", isConfirmedLocation);

        var inspectButton = root?.Q<Label>("action-inspect");
        if (lostExpedition != null)
        {
            var expeditionIsHere = mapView.IsExpeditionAtUi(coord);
            if (inspectButton != null)
            {
                inspectButton.text = expeditionIsHere ? "Spur untersuchen" : "Spur erreichen";
                inspectButton.EnableInClassList("disabled", !expeditionIsHere);
                inspectButton.tooltip = expeditionIsHere
                    ? $"Spuren von Expedition {lostExpedition.ExpeditionNumber} untersuchen"
                    : "Die Expedition muss zuerst dieses Feld erreichen";
            }

            return;
        }

        if (!isConfirmedLocation)
        {
            if (inspectButton != null)
            {
                inspectButton.text = "⌕ Untersuchen";
                inspectButton.EnableInClassList("disabled", true);
            }

            return;
        }

        SetText("selected-location-kind", LocationKindText(location.Kind));
        var hasInteractionModel = !string.IsNullOrWhiteSpace(location.ArchetypeId);
        var interaction = hasInteractionModel ? mapView.GetLocationInteractionForUi(location.Id) : null;
        var presentation = interaction?.Presentation;
        SetText("selected-location-status", presentation?.KnowledgeLabel ?? "Bestaetigt");
        SetText("selected-location-name", presentation?.Title ?? location.Name);
        SetText("selected-location-risk", presentation?.Description ?? "Der Ort wurde noch nicht aus der Naehe aufgenommen.");
        SetText("selected-location-action", hasInteractionModel
            ? "Aktion: Entscheidungen oeffnen."
            : location.IsInspected ? "Archiviert. Weitere Hinweise im Journal pruefen." : "Aktion: Ort untersuchen und Ereignis ausloesen.");

        if (inspectButton != null)
        {
            inspectButton.text = hasInteractionModel
                ? "Ort oeffnen"
                : location.IsInspected ? "Ort ansehen" : "Ort untersuchen";
            inspectButton.EnableInClassList("disabled", false);
        }
    }

    private void RefreshAlert(GameState state)
    {
        var overdue = 0;
        foreach (var mission in state.Expedition.ScoutMissions)
        {
            if (mission.Status == ScoutMissionStatus.Active && mission.ExpectedReturnWorldDay < state.World.WorldDay)
            {
                overdue += 1;
            }
        }

        var reports = state.Knowledge.ScoutReports.Count;
        var alert = root.Q<VisualElement>("alert");
        if (alert == null)
        {
            return;
        }

        if (overdue > 0)
        {
            alert.style.display = DisplayStyle.Flex;
            SetText("alert-text", "Späher überfällig");
            SetText("alert-count", overdue.ToString());
            return;
        }

        if (reports > 0)
        {
            alert.style.display = DisplayStyle.Flex;
            SetText("alert-text", "Neue Berichte");
            SetText("alert-count", reports.ToString());
            return;
        }

        alert.style.display = DisplayStyle.None;
    }

    private void RefreshReport(GameState state)
    {
        var reportCount = state.Knowledge.ScoutReports.Count;
        SetText("report-count-label", reportCount.ToString());
        SetText("report-count-rail", reportCount.ToString());
        SetDisplay("report-card-main", reportCount > 0);
        SetDisplay("report-card-secondary", false);

        if (reportCount == 0)
        {
            selectedReportIndex = -1;
            selectedHintIndex = -1;
            SetText("report-title-main", "Noch keine Berichte");
            SetText("report-sub-main", "Späher · ausstehend");
            SetText("report-reliability-main", "Verlässlichkeit: --");
            SetText("report-direction-main", "Unbekannt");
            SetText("report-excerpt-main", "Sende Späher aus, um erste Hinweise aus dem Nebel zu erhalten.");
            BuildReportList(state);
            BuildHintList(null);
            return;
        }

        if (selectedReportIndex < 0 || selectedReportIndex >= reportCount)
        {
            selectedReportIndex = reportCount - 1;
        }

        BuildReportList(state);

        var report = state.Knowledge.ScoutReports[selectedReportIndex];
        var reportAvatar = root?.Q<VisualElement>("report-avatar-main");
        if (reportAvatar != null)
        {
            var mission = state.Expedition.ScoutMissions.FirstOrDefault(item => item.Id == report.MissionId);
            var names = mission == null
                ? "berichtender Scout"
                : string.Join(" und ", mission.ScoutMemberIds.Select(id => state.Expedition.FindMember(id)?.Name ?? id));
            reportAvatar.tooltip = $"Platzhalterporträt: {names}";
        }
        SetText("report-title-main", report.Title);
        SetText("report-sub-main", $"Expedition · Späher · Tag {state.World.WorldDay}");
        SetText("report-reliability-main", $"Verlässlichkeit: {ReliabilityText(report.Reliability)}");
        SetText("report-direction-main", ReportLeadHeader(report));
        SetText("report-excerpt-main", $"„{report.Body}”");
        BuildHintList(report);
    }

    private void BuildReportList(GameState state)
    {
        var list = root?.Q<VisualElement>("report-list");
        if (list == null)
        {
            return;
        }

        list.Clear();
        for (var i = state.Knowledge.ScoutReports.Count - 1; i >= 0; i--)
        {
            var report = state.Knowledge.ScoutReports[i];
            var row = new VisualElement();
            row.AddToClassList("report-list__row");
            if (i == selectedReportIndex)
            {
                row.AddToClassList("report-list__row--selected");
            }

            var title = new Label(report.Title);
            title.AddToClassList("report-list__title");
            row.Add(title);

            var meta = new Label($"{ReliabilityText(report.Reliability)} | {report.Leads.Count} Spuren | {report.Hints.Count} Hinweise");
            meta.AddToClassList("report-list__meta");
            row.Add(meta);

            var reportIndex = i;
            row.RegisterCallback<ClickEvent>(evt =>
            {
                selectedReportIndex = reportIndex;
                selectedHintIndex = 0;
                Refresh();
                evt.StopPropagation();
            });
            list.Add(row);
        }
    }

    private void BuildHintList(ScoutReportState report)
    {
        var list = root?.Q<VisualElement>("report-hint-list");
        if (list == null)
        {
            return;
        }

        list.Clear();
        if (report == null || report.Hints.Count == 0)
        {
            selectedHintIndex = -1;
            return;
        }

        if (selectedHintIndex < 0 || selectedHintIndex >= report.Hints.Count)
        {
            selectedHintIndex = 0;
        }

        var label = new Label("HINWEISE");
        label.AddToClassList("section-label");
        label.AddToClassList("hint-list__label");
        list.Add(label);

        foreach (var lead in report.Leads)
        {
            var row = new VisualElement();
            row.AddToClassList("hint-list__row");
            var scope = lead.Scope == ScoutLeadScope.Local ? "UMGEBUNG" : "RICHTUNG";
            var heading = new Label($"{scope} · {DirectionText(lead.Direction)} · {lead.Confidence}%");
            heading.AddToClassList("hint-list__label");
            row.Add(heading);
            var text = new Label(lead.Summary);
            text.AddToClassList("hint-list__text");
            row.Add(text);
            list.Add(row);
        }

        for (var i = 0; i < report.Hints.Count; i++)
        {
            var row = new VisualElement();
            row.AddToClassList("hint-list__row");
            if (i == selectedHintIndex)
            {
                row.AddToClassList("hint-list__row--selected");
            }

            var text = new Label(report.Hints[i]);
            text.AddToClassList("hint-list__text");
            row.Add(text);

            var hintIndex = i;
            row.RegisterCallback<ClickEvent>(evt =>
            {
                selectedHintIndex = hintIndex;
                Refresh();
                evt.StopPropagation();
            });
            list.Add(row);
        }
    }

    private static string ReportLeadHeader(ScoutReportState report)
    {
        var lead = report.Leads.FirstOrDefault();
        if (lead == null)
        {
            return "Keine Richtung bestaetigt";
        }

        var scope = lead.Scope == ScoutLeadScope.Local ? "Umgebung" : "Richtung";
        return $"{scope}: {DirectionText(lead.Direction)} · Hinweis {lead.Confidence}%";
    }

    private void RefreshScoutMissionPopup(GameState state)
    {
        var popup = root?.Q<VisualElement>("scout-mission-popup");
        if (popup == null)
        {
            return;
        }

        var availableScoutIds = new HashSet<string>();
        if (state != null && state.Expedition.Status == ExpeditionStatus.Active)
        {
            foreach (var member in state.Expedition.Members)
            {
                if (member.Role == ExpeditionMemberRole.Scout && member.Status == ExpeditionMemberStatus.Available)
                {
                    availableScoutIds.Add(member.Id);
                }
            }
        }

        selectedScoutIds.RemoveWhere(id => !availableScoutIds.Contains(id));

        var candidates = root?.Q<VisualElement>("scout-candidate-list");
        if (candidates != null)
        {
            candidates.Clear();
            if (state != null)
            {
                foreach (var member in state.Expedition.Members)
                {
                    if (member.Role != ExpeditionMemberRole.Scout)
                    {
                        continue;
                    }

                    var row = new VisualElement();
                    row.AddToClassList("scout-candidate");
                    var available = state.Expedition.Status == ExpeditionStatus.Active && member.Status == ExpeditionMemberStatus.Available;
                    row.EnableInClassList("disabled", !available);
                    row.EnableInClassList("selected", selectedScoutIds.Contains(member.Id));

                    var name = new Label(member.Name);
                    name.AddToClassList("scout-candidate__name");
                    row.Add(name);

                    var status = new Label(ScoutStatusText(member.Status));
                    status.AddToClassList("scout-candidate__status");
                    row.Add(status);

                    var memberId = member.Id;
                    row.RegisterCallback<ClickEvent>(evt =>
                    {
                        ToggleScoutSelection(memberId, available);
                        evt.StopPropagation();
                    });
                    candidates.Add(row);
                }
            }

            if (candidates.childCount == 0)
            {
                var empty = new Label("Keine verfuegbaren Spaeher in der aktuellen Expedition.");
                empty.AddToClassList("archive-empty");
                candidates.Add(empty);
            }
        }

        ToggleChoice("scout-direction-north", scoutDirection == ScoutDirection.North);
        ToggleChoice("scout-direction-north-east", scoutDirection == ScoutDirection.NorthEast);
        ToggleChoice("scout-direction-east", scoutDirection == ScoutDirection.East);
        ToggleChoice("scout-direction-south-east", scoutDirection == ScoutDirection.SouthEast);
        ToggleChoice("scout-direction-south", scoutDirection == ScoutDirection.South);
        ToggleChoice("scout-direction-south-west", scoutDirection == ScoutDirection.SouthWest);
        ToggleChoice("scout-direction-west", scoutDirection == ScoutDirection.West);
        ToggleChoice("scout-direction-north-west", scoutDirection == ScoutDirection.NorthWest);

        for (var duration = 1; duration <= 5; duration++)
        {
            ToggleChoice($"scout-duration-{duration}", scoutDurationDays == duration);
        }

        ToggleChoice("scout-focus-survey", scoutFocus == ScoutMissionFocus.Survey);
        ToggleChoice("scout-focus-route", scoutFocus == ScoutMissionFocus.Route);
        ToggleChoice("scout-focus-resources", scoutFocus == ScoutMissionFocus.Resources);
        ToggleChoice("scout-focus-faction", scoutFocus == ScoutMissionFocus.FactionSigns);
        ToggleChoice("scout-focus-ruins", scoutFocus == ScoutMissionFocus.Ruins);

        ToggleChoice("scout-behavior-cautious", scoutBehavior == ScoutMissionBehavior.Cautious);
        ToggleChoice("scout-behavior-balanced", scoutBehavior == ScoutMissionBehavior.Balanced);
        ToggleChoice("scout-behavior-bold", scoutBehavior == ScoutMissionBehavior.Bold);

        var expectedReturn = state == null ? 0 : state.World.WorldDay + scoutDurationDays;
        var selectedCount = selectedScoutIds.Count;
        var risk = ScoutRiskText(scoutBehavior, scoutFocus, scoutDurationDays);
        SetText("scout-modal-summary", $"{selectedCount}/2 Spaeher · Rueckkehr Tag {expectedReturn}");
        SetText("scout-mission-preview", state == null || state.Expedition.Status != ExpeditionStatus.Active
            ? "Es ist keine aktive Expedition unterwegs."
            : selectedCount == 0
            ? "Waehle mindestens einen verfuegbaren Spaeher aus der aktuellen Expedition."
            : $"{selectedCount} Spaeher nach {DirectionText(scoutDirection)} · {scoutDurationDays} Tag(e) · Fokus {FocusText(scoutFocus)} · {risk}");

        var sendButton = root?.Q<Label>("scout-mission-send");
        sendButton?.EnableInClassList("disabled", selectedCount == 0 || state == null || state.Expedition.Status != ExpeditionStatus.Active);
    }

    private void ToggleScoutSelection(string memberId, bool available)
    {
        if (!available)
        {
            return;
        }

        if (selectedScoutIds.Contains(memberId))
        {
            selectedScoutIds.Remove(memberId);
        }
        else if (selectedScoutIds.Count < 2)
        {
            selectedScoutIds.Add(memberId);
        }

        RefreshScoutMissionPopup(mapView?.CurrentGameState);
    }

    private void ToggleChoice(string elementName, bool selected)
    {
        var element = root?.Q<VisualElement>(elementName);
        element?.EnableInClassList("selected", selected);
    }

    private static string ScoutStatusText(ExpeditionMemberStatus status)
    {
        switch (status)
        {
            case ExpeditionMemberStatus.Available:
                return "bereit";
            case ExpeditionMemberStatus.Assigned:
                return "auf Mission";
            case ExpeditionMemberStatus.Injured:
                return "verletzt";
            case ExpeditionMemberStatus.Exhausted:
                return "erschöpft";
            case ExpeditionMemberStatus.Missing:
                return "vermisst";
            case ExpeditionMemberStatus.Dead:
                return "tot";
            default:
                return status.ToString();
        }
    }

    private static string DirectionText(ScoutDirection direction)
    {
        switch (direction)
        {
            case ScoutDirection.North:
                return "Nord";
            case ScoutDirection.NorthEast:
                return "Nordost";
            case ScoutDirection.East:
                return "Ost";
            case ScoutDirection.SouthEast:
                return "Suedost";
            case ScoutDirection.South:
                return "Sued";
            case ScoutDirection.SouthWest:
                return "Suedwest";
            case ScoutDirection.West:
                return "West";
            case ScoutDirection.NorthWest:
                return "Nordwest";
            default:
                return direction.ToString();
        }
    }

    private static string FocusText(ScoutMissionFocus focus)
    {
        switch (focus)
        {
            case ScoutMissionFocus.Survey:
                return "Erkunden";
            case ScoutMissionFocus.Route:
                return "Route";
            case ScoutMissionFocus.Resources:
                return "Ressourcen";
            case ScoutMissionFocus.FactionSigns:
                return "Zeichen";
            case ScoutMissionFocus.Ruins:
                return "Ruinen";
            default:
                return focus.ToString();
        }
    }

    private static string ScoutRiskText(ScoutMissionBehavior behavior, ScoutMissionFocus focus, int durationDays)
    {
        if (behavior == ScoutMissionBehavior.Bold || focus == ScoutMissionFocus.Ruins || durationDays >= 4)
        {
            return "Risiko hoch";
        }

        if (behavior == ScoutMissionBehavior.Cautious && durationDays <= 2)
        {
            return "Risiko niedrig";
        }

        return "Risiko mittel";
    }

    private void BuildArchiveList(GameState state)
    {
        var list = root?.Q<VisualElement>("archive-list");
        if (list == null)
        {
            return;
        }

        HideStaticArchiveCards(list);
        list.Clear();
        if (state.Base.ArchiveEntries.Count == 0 && state.Base.LostExpeditions.Count == 0)
        {
            var empty = new Label("Noch keine gesicherten Einträge.");
            empty.AddToClassList("archive-empty");
            list.Add(empty);
            return;
        }

        for (var i = state.Base.LostExpeditions.Count - 1; i >= 0; i--)
        {
            var lost = state.Base.LostExpeditions[i];
            var card = new VisualElement();
            card.AddToClassList("card");
            card.AddToClassList("card--plain");
            card.AddToClassList("archive-entry");

            var title = new Label($"Expedition {lost.ExpeditionNumber:00} vermisst");
            title.AddToClassList("card__title");
            title.AddToClassList("serif");
            card.Add(title);

            var body = new Label(
                $"Letzte bekannte Position: Feld {lost.LastKnownPosition.Q:00} / {lost.LastKnownPosition.R:00}\n" +
                $"Status: {LostExpeditionStatusText(lost.Status)}\n" +
                $"Geschaetztes verlorenes Wissen: {lost.EstimatedLostKnowledge}");
            body.AddToClassList("archive-entry__body");
            card.Add(body);

            list.Add(card);
        }

        for (var i = state.Base.ArchiveEntries.Count - 1; i >= 0; i--)
        {
            var card = new VisualElement();
            card.AddToClassList("card");
            card.AddToClassList("card--plain");
            card.AddToClassList("archive-entry");

            var title = new Label($"Archiv {i + 1:00}");
            title.AddToClassList("card__title");
            title.AddToClassList("serif");
            card.Add(title);

            var body = new Label(state.Base.ArchiveEntries[i]);
            body.AddToClassList("archive-entry__body");
            card.Add(body);

            list.Add(card);
        }
    }

    private static string LostExpeditionStatusText(LostExpeditionStatus status)
    {
        switch (status)
        {
            case LostExpeditionStatus.Missing:
                return "Vermisst";
            case LostExpeditionStatus.PresumedLost:
                return "Vermutlich verloren";
            case LostExpeditionStatus.PartiallyRecovered:
                return "Teilweise geborgen";
            case LostExpeditionStatus.SurvivorFound:
                return "Ueberlebende gefunden";
            case LostExpeditionStatus.RecordsRecovered:
                return "Aufzeichnungen geborgen";
            case LostExpeditionStatus.FullyResolved:
                return "Abgeschlossen";
            default:
                return status.ToString();
        }
    }

    private void HideStaticArchiveCards(VisualElement archiveList)
    {
        var archiveSection = root?.Q<VisualElement>("section-archive");
        if (archiveSection == null)
        {
            return;
        }

        foreach (var child in archiveSection.Children())
        {
            if (child != archiveList && child.ClassListContains("card"))
            {
                child.style.display = DisplayStyle.None;
            }
        }
    }

    private void RefreshEventPopup(GameState state)
    {
        var popup = root?.Q<VisualElement>("event-popup");
        var options = root?.Q<VisualElement>("event-options");
        if (popup == null || options == null)
        {
            return;
        }

        var eventState = state.Events.Current;
        if (eventState == null)
        {
            popup.style.display = DisplayStyle.None;
            options.Clear();
            return;
        }

        popup.style.display = DisplayStyle.Flex;
        SetText("event-source", eventState.Source);
        SetText("event-title", eventState.Title);
        SetText("event-body", eventState.Body);
        var eventImage = root?.Q<VisualElement>("event-image");
        if (eventImage != null)
        {
            eventImage.tooltip = $"Platzhalterbild: {eventState.Kind}";
        }

        options.Clear();
        for (var i = 0; i < eventState.Options.Count; i++)
        {
            var option = eventState.Options[i];
            var button = new Label(option.Label);
            button.AddToClassList(i == 0 ? "btn--primary" : "btn");
            button.AddToClassList("event-option");
            var eventId = eventState.Id;
            var optionId = option.Id;
            button.RegisterCallback<ClickEvent>(evt =>
            {
                mapView?.RequestResolveEventFromUi(eventId, optionId);
                evt.StopPropagation();
            });
            options.Add(button);
        }
    }

    private void RefreshLocationInteractionPopup(GameState state)
    {
        var popup = root?.Q<VisualElement>("location-interaction-popup");
        var options = root?.Q<VisualElement>("action-list");
        if (popup == null || options == null)
        {
            return;
        }

        if (state == null || string.IsNullOrWhiteSpace(openLocationInteractionId))
        {
            popup.style.display = DisplayStyle.None;
            options.Clear();
            return;
        }

        var result = mapView.GetLocationInteractionForUi(openLocationInteractionId);
        if (!result.Success || result.Interaction == null)
        {
            popup.style.display = DisplayStyle.None;
            options.Clear();
            locationInteractionMessage = result.Error;
            return;
        }

        var interaction = result.Interaction;
        var location = interaction.Location;
        popup.style.display = DisplayStyle.Flex;
        if (location.ActiveProject != null &&
            locationInteractionView == LocationInteractionView.Actions &&
            string.IsNullOrWhiteSpace(selectedLocationActionId))
        {
            locationInteractionView = LocationInteractionView.Project;
        }

        RenderLocationHeader(interaction, result.Presentation);
        RenderLocationViews(interaction);
    }

    private void CloseLocationInteractionPopup()
    {
        openLocationInteractionId = null;
        selectedLocationActionId = null;
        locationInteractionMessage = null;
        latestLocationActionResult = null;
        locationInteractionView = LocationInteractionView.Actions;
        SetDisplay("location-interaction-popup", false);
    }

    private void ShowLocationActions()
    {
        locationInteractionView = LocationInteractionView.Actions;
        RefreshLocationInteractionPopup(mapView?.CurrentGameState);
    }

    private void RenderLocationHeader(LocationInteractionModel interaction, LocationInteractionPresentation presentation)
    {
        var location = interaction.Location;

        SetText("location-icon-glyph", LocationIconText(location));
        SetText("location-eyebrow", "FUNDSTELLE");
        SetText("location-anchor", LocationAnchorText(location.Anchor));
        SetText("location-title", presentation?.Title ?? location.Name);
        SetText("location-subtitle", presentation?.Subtitle ?? "Bestaetigter besonderer Ort");
        SetText("state-knowledge", $"Wissensstand: {presentation?.KnowledgeLabel ?? "Unbekannt"}");
        SetText("state-operational", presentation?.OperationalStateText ?? "Der aktuelle Zustand ist unbekannt.");
        SetText("state-presence", presentation?.PresenceStateText ?? "Die aktuelle Anwesenheit ist unbekannt.");
        SetText("location-flavor", presentation?.Description ?? "Der Ort wurde noch nicht aus der Naehe aufgenommen.");

        var operational = root?.Q<Label>("state-operational");
        operational?.EnableInClassList("bi-state-pill--alert", false);

        var icon = root?.Q<Label>("location-icon-glyph");
        if (icon != null)
        {
            icon.tooltip = string.IsNullOrWhiteSpace(presentation?.ImageId)
                ? "Platzhalterbild: unbekannter Ort"
                : $"Bildreferenz: {presentation.ImageId}";
        }

        var modifierRow = root?.Q<VisualElement>("modifier-row");
        modifierRow?.Clear();
        if (modifierRow == null)
        {
            return;
        }

        foreach (var contextTag in presentation?.KnownContextTags ?? Array.Empty<string>())
        {
            var modifier = new Label($"Bekannt: {contextTag}");
            modifier.AddToClassList("bi-modifier");
            modifierRow.Add(modifier);
        }
    }

    private void RenderLocationViews(LocationInteractionModel interaction)
    {
        var showActions = locationInteractionView == LocationInteractionView.Actions;
        var showOutcome = locationInteractionView == LocationInteractionView.Outcome;
        var showProject = locationInteractionView == LocationInteractionView.Project;
        SetElementDisplay("view-actions", showActions);
        SetElementDisplay("view-outcome", showOutcome);
        SetElementDisplay("view-project", showProject);

        if (showOutcome)
        {
            RenderLocationOutcomeView();
            return;
        }

        if (showProject)
        {
            RenderLocationProjectView(interaction);
            return;
        }

        RenderLocationActionView(interaction);
    }

    private void RenderLocationActionView(LocationInteractionModel interaction)
    {
        var actionList = root?.Q<VisualElement>("action-list");
        var detailPanel = root?.Q<VisualElement>("detail-panel");
        if (actionList == null || detailPanel == null)
        {
            return;
        }

        EnsureSelectedLocationAction(interaction);
        var actionListContent = actionList is ScrollView scrollView ? scrollView.contentContainer : actionList;
        actionListContent.Clear();

        foreach (var option in interaction.Options)
        {
            actionListContent.Add(CreateLocationActionRow(option));
        }

        RenderLocationActionDetail(detailPanel, interaction, FindSelectedLocationOption(interaction));
    }

    private VisualElement CreateLocationActionRow(LocationInteractionOption option)
    {
        var row = new VisualElement();
        row.AddToClassList("bi-action-row");
        row.EnableInClassList("selected", option.Action.Id == selectedLocationActionId);
        row.EnableInClassList("locked", !option.IsAvailable);
        row.tooltip = option.IsAvailable ? option.Action.Description : option.LockedReason;

        var glyph = new Label(ActionGlyphText(option.Action));
        glyph.AddToClassList("bi-action-glyph");
        row.Add(glyph);

        var main = new VisualElement();
        main.AddToClassList("bi-action-main");
        var title = new Label(option.Action.Label);
        title.AddToClassList("bi-action-title");
        main.Add(title);
        var note = new Label(option.IsAvailable ? ConfidenceText(option.Confidence) : option.LockedReason ?? "Gesperrt");
        note.AddToClassList("bi-action-note");
        main.Add(note);
        row.Add(main);

        var risk = new Label(option.IsAvailable ? RiskPillText(option.RiskBand) : "LOCK");
        risk.AddToClassList("bi-risk-pill");
        risk.AddToClassList(RiskPillClass(option.RiskBand));
        risk.EnableInClassList("bi-risk-pill--locked", !option.IsAvailable);
        row.Add(risk);

        var actionId = option.Action.Id;
        row.RegisterCallback<ClickEvent>(evt =>
        {
            selectedLocationActionId = actionId;
            locationInteractionView = LocationInteractionView.Actions;
            RefreshLocationInteractionPopup(mapView?.CurrentGameState);
            evt.StopPropagation();
        });
        return row;
    }

    private void RenderLocationActionDetail(VisualElement detailPanel, LocationInteractionModel interaction, LocationInteractionOption option)
    {
        detailPanel.Clear();
        if (option == null)
        {
            var empty = new Label("Keine Aktion verfuegbar.");
            empty.AddToClassList("bi-detail-title");
            detailPanel.Add(empty);
            return;
        }

        var title = new Label(option.Action.Label);
        title.AddToClassList("bi-detail-title");
        detailPanel.Add(title);

        var description = new Label(option.Action.Description);
        description.AddToClassList("bi-detail-description");
        detailPanel.Add(description);

        var meta = new VisualElement();
        meta.AddToClassList("bi-detail-meta");
        var risk = new Label(RiskPillText(option.RiskBand));
        risk.AddToClassList("bi-risk-pill");
        risk.AddToClassList(RiskPillClass(option.RiskBand));
        meta.Add(risk);
        var cost = new Label(LocationActionCostText(option.Action));
        cost.AddToClassList("bi-detail-cost");
        meta.Add(cost);
        detailPanel.Add(meta);

        var note = new Label(option.IsAvailable
            ? $"Einschaetzung: {ConfidenceText(option.Confidence)}."
            : option.LockedReason ?? "Diese Aktion ist aktuell gesperrt.");
        note.AddToClassList("bi-detail-note");
        detailPanel.Add(note);

        if (option.Action.HardRequirements.Count > 0)
        {
            var requirements = new VisualElement();
            requirements.AddToClassList("bi-requirement-list");
            foreach (var requirement in option.Action.HardRequirements)
            {
                var line = new Label(RequirementText(requirement));
                line.AddToClassList("bi-requirement");
                requirements.Add(line);
            }

            detailPanel.Add(requirements);
        }

        var buttonText = option.Action.StartsProject ? "Projekt starten" : "Aktion durchfuehren";
        var button = new Label(option.IsAvailable ? buttonText : "Gesperrt");
        button.AddToClassList("bi-primary-button");
        button.EnableInClassList("disabled", !option.IsAvailable);
        if (option.IsAvailable)
        {
            button.RegisterCallback<ClickEvent>(evt =>
            {
                ResolveSelectedLocationAction(interaction.Location.Id);
                evt.StopPropagation();
            });
        }

        detailPanel.Add(button);
    }

    private void RenderLocationOutcomeView()
    {
        var outcome = latestLocationActionResult;
        SetText("outcome-tier", outcome?.OutcomeLabel ?? (outcome?.Success == false ? "Abgelehnt" : "Ergebnis"));
        SetText("outcome-caption", LocationActionResultText(outcome));

        var effects = root?.Q<VisualElement>("outcome-effects");
        effects?.Clear();
        if (effects == null)
        {
            return;
        }

        if (outcome == null || outcome.EffectTexts.Count == 0)
        {
            AddEffectCard(effects, outcome?.Error ?? "Keine direkten Effekte.");
            return;
        }

        foreach (var text in outcome.EffectTexts)
        {
            AddEffectCard(effects, text);
        }
    }

    private void RenderLocationProjectView(LocationInteractionModel interaction)
    {
        var project = interaction.Location.ActiveProject;
        var action = FindActionForProject(interaction, project);
        SetText("project-title", action?.Label ?? "Aktives Projekt");

        var effects = root?.Q<VisualElement>("project-effects");
        effects?.Clear();
        if (project == null)
        {
            SetText("project-day-label", "Kein aktives Projekt an dieser Fundstelle.");
            SetProgress("project-progress-fill", 0f);
            AddEffectCard(effects, "Starte eine Projektaktion, um hier Fortschritt aufzubauen.");
            return;
        }

        SetText("project-day-label", $"Fortschritt {project.Progress}/{project.RequiredProgress}");
        var progress = project.RequiredProgress <= 0 ? 0f : project.Progress * 100f / project.RequiredProgress;
        SetProgress("project-progress-fill", progress);

        if (action != null)
        {
            AddEffectCard(effects, action.Description);
            foreach (var effect in action.ProjectCompletionEffects)
            {
                AddEffectCard(effects, effect.Text);
            }
        }
        else
        {
            AddEffectCard(effects, "Dieses Projekt verweist auf eine nicht registrierte Aktion.");
        }
    }

    private void ResolveSelectedLocationAction(string locationId)
    {
        if (string.IsNullOrWhiteSpace(selectedLocationActionId))
        {
            return;
        }

        latestLocationActionResult = mapView?.RequestResolveLocationActionFromUi(locationId, selectedLocationActionId);
        locationInteractionMessage = LocationActionResultText(latestLocationActionResult);
        if (selectedLocationActionId == LocationInteractionContent.ActionLeave &&
            latestLocationActionResult != null &&
            latestLocationActionResult.Success)
        {
            CloseLocationInteractionPopup();
            return;
        }

        if (latestLocationActionResult != null &&
            latestLocationActionResult.Success &&
            latestLocationActionResult.ExpeditionMoved)
        {
            CloseLocationInteractionPopup();
            return;
        }

        if (latestLocationActionResult != null && latestLocationActionResult.Success)
        {
            locationInteractionView = latestLocationActionResult.Action != null && latestLocationActionResult.Action.StartsProject
                ? LocationInteractionView.Project
                : LocationInteractionView.Outcome;
        }
        else
        {
            locationInteractionView = LocationInteractionView.Actions;
        }

        Refresh();
    }

    private void AdvanceLocationProject()
    {
        if (string.IsNullOrWhiteSpace(openLocationInteractionId))
        {
            return;
        }

        latestLocationActionResult = mapView?.RequestAdvanceLocationProjectFromUi(openLocationInteractionId);
        locationInteractionMessage = LocationActionResultText(latestLocationActionResult);
        if (latestLocationActionResult != null && latestLocationActionResult.Success && latestLocationActionResult.Location?.ActiveProject == null)
        {
            locationInteractionView = LocationInteractionView.Outcome;
        }
        else
        {
            locationInteractionView = LocationInteractionView.Project;
        }

        Refresh();
    }

    private void EnsureSelectedLocationAction(LocationInteractionModel interaction)
    {
        if (FindSelectedLocationOption(interaction) != null)
        {
            return;
        }

        selectedLocationActionId = null;
        foreach (var option in interaction.Options)
        {
            if (option.IsAvailable)
            {
                selectedLocationActionId = option.Action.Id;
                return;
            }
        }

        if (interaction.Options.Count > 0)
        {
            selectedLocationActionId = interaction.Options[0].Action.Id;
        }
    }

    private LocationInteractionOption FindSelectedLocationOption(LocationInteractionModel interaction)
    {
        if (string.IsNullOrWhiteSpace(selectedLocationActionId))
        {
            return null;
        }

        return interaction.FindOption(selectedLocationActionId);
    }

    private static LocationActionDefinition FindActionForProject(LocationInteractionModel interaction, LocationProjectState project)
    {
        if (project == null)
        {
            return null;
        }

        foreach (var option in interaction.Options)
        {
            if (option.Action.Id == project.ActionId)
            {
                return option.Action;
            }
        }

        return null;
    }

    private void AddEffectCard(VisualElement parent, string text)
    {
        if (parent == null)
        {
            return;
        }

        var card = new Label(text);
        card.AddToClassList("bi-effect-card");
        parent.Add(card);
    }

    private void SetElementDisplay(string elementName, bool visible)
    {
        var element = root?.Q<VisualElement>(elementName);
        if (element != null)
        {
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private void SetProgress(string elementName, float percent)
    {
        var element = root?.Q<VisualElement>(elementName);
        if (element != null)
        {
            element.style.width = Length.Percent(Mathf.Clamp(percent, 0f, 100f));
        }
    }

    private void RefreshFactionInteractionPopup(GameState state)
    {
        var popup = root?.Q<VisualElement>("faction-contact-popup");
        var offerList = root?.Q<VisualElement>("faction-offer-list");
        if (popup == null || offerList == null)
        {
            return;
        }

        var interaction = state.ActiveFactionInteraction;
        if (interaction == null)
        {
            popup.style.display = DisplayStyle.None;
            offerList.Clear();
            return;
        }

        popup.style.display = DisplayStyle.Flex;
        SetText("faction-modal-title", $"KONTAKT · {interaction.FactionName}");
        SetText("faction-modal-coord", $"Feld {interaction.Coord.Q:00} / {interaction.Coord.R:00}");
        SetText("faction-representative-name", interaction.Representative.DisplayName);
        SetText("faction-representative-role", $"{RepresentativeRoleText(interaction.Representative.Role)} · {interaction.FactionName}");
        SetText("faction-attitude", interaction.AttitudeText);
        SetText("faction-representative-description", interaction.Representative.Description);
        SetText("faction-dialogue-label", $"DIESE BEGEGNUNG · TAG {state.World.WorldDay}");
        SetText("faction-dialogue", $"\"{interaction.DialogueText}\"");
        SetText("faction-knowledge-value", $"Wissen: {state.Base.KnowledgePoints}");

        offerList.Clear();
        for (var i = 0; i < interaction.Offers.Count; i++)
        {
            var offer = interaction.Offers[i];
            var card = new VisualElement();
            card.AddToClassList("faction-offer-card");
            if (!offer.IsAvailable)
            {
                card.AddToClassList("locked");
            }

            var title = new Label(offer.Title);
            title.AddToClassList("faction-offer-title");
            card.Add(title);

            var description = new Label(offer.Description);
            description.AddToClassList("faction-offer-description");
            card.Add(description);

            var footer = new VisualElement();
            footer.AddToClassList("faction-offer-footer");

            var cost = new Label(OfferCostText(offer));
            cost.AddToClassList("faction-offer-cost");
            footer.Add(cost);

            var button = new Label(offer.IsAvailable ? "Kaufen" : "Gesperrt");
            button.AddToClassList("faction-offer-buy");
            if (!offer.IsAvailable)
            {
                button.AddToClassList("disabled");
            }

            var offerId = offer.Id;
            button.RegisterCallback<ClickEvent>(evt =>
            {
                mapView?.RequestPurchaseFactionOfferFromUi(offerId);
                evt.StopPropagation();
            });
            footer.Add(button);

            card.Add(footer);
            offerList.Add(card);
        }
    }

    private static string RepresentativeRoleText(FactionRepresentativeRole role)
    {
        switch (role)
        {
            case FactionRepresentativeRole.Watcher:
                return "Beobachter";
            case FactionRepresentativeRole.Scout:
                return "Spaeher";
            case FactionRepresentativeRole.Guard:
                return "Waechter";
            case FactionRepresentativeRole.Messenger:
                return "Bote";
            case FactionRepresentativeRole.Trader:
                return "Haendler";
            case FactionRepresentativeRole.Guide:
                return "Fuehrer";
            case FactionRepresentativeRole.Leader:
                return "Anfuehrer";
            case FactionRepresentativeRole.MaskedSpeaker:
                return "Maskierte Stimme";
            default:
                return role.ToString();
        }
    }

    private static string OfferCostText(FactionOfferState offer)
    {
        if (!offer.IsAvailable)
        {
            return offer.LockedReason ?? "Nicht verfuegbar";
        }

        if (offer.KnowledgeCost > 0)
        {
            return $"{offer.KnowledgeCost} Wissen";
        }

        if (offer.MedicineCost > 0)
        {
            return $"{offer.MedicineCost} Medizin";
        }

        return "Kostenlos";
    }

    private static string LocationIconText(SpecialLocationState location)
    {
        if (location == null)
        {
            return "?";
        }

        switch (location.ArchetypeId)
        {
            case LocationInteractionContent.ArchetypeRouteObstacle:
                return "~";
            case LocationInteractionContent.ArchetypeInvestigationSite:
                return "?";
            default:
                return "+";
        }
    }

    private static string LocationAnchorText(LocationAnchor anchor)
    {
        if (anchor == null)
        {
            return "Ort unbekannt";
        }

        switch (anchor.Kind)
        {
            case LocationAnchorKind.Edge:
                return $"Kante {CoordText(anchor.Coords[0])} - {CoordText(anchor.Coords[1])}";
            case LocationAnchorKind.Area:
                return $"Gebiet {anchor.Coords.Count} Felder";
            case LocationAnchorKind.Path:
                return $"Pfad {anchor.Coords.Count} Felder";
            default:
                return $"Feld {CoordText(anchor.PrimaryCoord)}";
        }
    }

    private static string CoordText(HexCoord coord)
    {
        return $"{coord.Q:00}/{coord.R:00}";
    }

    private static string ActionGlyphText(LocationActionDefinition action)
    {
        if (action == null) return "-";
        if (action.ActionTags.Contains("inspect") || action.ActionTags.Contains("observe") || action.ActionTags.Contains("investigate")) return "?";
        if (action.ActionTags.Contains("repair") || action.ActionTags.Contains("contain")) return "#";
        if (action.ActionTags.Contains("route") || action.ActionTags.Contains("bypass") || action.ActionTags.Contains("cross")) return "->";
        if (action.ActionTags.Contains("document") || action.ActionTags.Contains("map") || action.ActionTags.Contains("mark")) return "+";
        if (action.ActionTags.Contains("leave") || action.ActionTags.Contains("withdraw")) return "X";
        return "-";
    }

    private static string RiskPillText(LocationRiskBand risk)
    {
        return risk == LocationRiskBand.None ? "Kein Risiko" : RiskBandText(risk);
    }

    private static string RiskPillClass(LocationRiskBand risk)
    {
        switch (risk)
        {
            case LocationRiskBand.Moderate:
                return "bi-risk-pill--moderate";
            case LocationRiskBand.High:
                return "bi-risk-pill--high";
            case LocationRiskBand.Extreme:
                return "bi-risk-pill--extreme";
            default:
                return string.Empty;
        }
    }

    private static string LocationActionCostText(LocationActionDefinition action)
    {
        if (action == null)
        {
            return "Kosten unbekannt";
        }

        if (action.StartsProject)
        {
            return $"{action.ProjectDurationDays} Projekttage";
        }

        if (action.Costs.Count > 0)
        {
            var parts = new List<string>();
            foreach (var cost in action.Costs)
            {
                parts.Add($"{cost.Amount} {CostKindText(cost.Kind)}");
            }

            return string.Join(", ", parts);
        }

        if (action.RepeatPolicy == LocationActionRepeatPolicy.RepeatableWithCost)
        {
            return "moegliche Kosten";
        }

        return "sofort";
    }

    private static string CostKindText(LocationCostKind kind)
    {
        switch (kind)
        {
            case LocationCostKind.MovementPoints: return "Bewegung";
            case LocationCostKind.Supplies: return "Vorraete";
            case LocationCostKind.Medicine: return "Medizin";
            case LocationCostKind.Morale: return "Moral";
            default: return kind.ToString();
        }
    }

    private static string RequirementText(LocationRequirementDefinition requirement)
    {
        if (requirement == null)
        {
            return "Voraussetzung unbekannt";
        }

        switch (requirement.Kind)
        {
            case LocationRequirementKind.OperationalStateAny:
                return $"Zustand: {string.Join(", ", requirement.Values)}";
            case LocationRequirementKind.ModifierActive:
                return $"Modifier aktiv: {string.Join(", ", requirement.Values)}";
            case LocationRequirementKind.RolePresent:
                return requirement.RequiredRole.HasValue
                    ? $"Rolle erforderlich: {requirement.RequiredRole.Value}"
                    : "Spezialrolle erforderlich";
            case LocationRequirementKind.PositionOnOrAdjacent:
                return "Expedition am Ort oder angrenzend";
            case LocationRequirementKind.AnchorKind:
                return requirement.RequiredAnchorKind.HasValue
                    ? $"Ortstyp: {requirement.RequiredAnchorKind.Value}"
                    : "Passender Ortstyp erforderlich";
            default:
                return requirement.UnmetReason;
        }
    }

    private static string LocationOptionText(LocationInteractionOption option)
    {
        var risk = option.RiskBand == LocationRiskBand.None ? "kein Risiko" : $"Risiko: {RiskBandText(option.RiskBand)}";
        if (!option.IsAvailable)
        {
            return $"{option.Action.Label} · gesperrt: {option.LockedReason ?? "nicht verfuegbar"}";
        }

        return $"{option.Action.Label} · {risk} · {ConfidenceText(option.Confidence)}";
    }

    private static string LocationActionResultText(LocationActionResult result)
    {
        if (result == null)
        {
            return "Keine Rueckmeldung.";
        }

        if (!result.Success)
        {
            return result.Error ?? "Aktion abgelehnt.";
        }

        var text = string.IsNullOrEmpty(result.OutcomeLabel)
            ? result.Action?.Label ?? "Aktion ausgefuehrt"
            : $"{result.Action?.Label}: {result.OutcomeLabel}";
        if (result.EffectTexts.Count == 0)
        {
            return text;
        }

        return $"{text}. {string.Join(" ", result.EffectTexts)}";
    }

    private static string LocationStateSummary(SpecialLocationState location)
    {
        var interaction = LocationInteractionStateText(location.InteractionStateId);
        var operation = LocationOperationalStateText(location.OperationalStateId);
        var presence = LocationPresenceStateText(location.PresenceStateId);
        return $"{interaction} · {operation} · {presence}";
    }

    private static string LocationInteractionStateText(string stateId)
    {
        switch (stateId)
        {
            case LocationStateIds.Interaction.Untouched:
                return "Neu";
            case LocationStateIds.Interaction.Observed:
                return "Beobachtet";
            case LocationStateIds.Interaction.Inspected:
                return "Eingeschaetzt";
            case LocationStateIds.Interaction.Investigated:
                return "Untersucht";
            case LocationStateIds.Interaction.Exhausted:
                return "Ausgeschoepft";
            default:
                return stateId;
        }
    }

    private static string LocationOperationalStateText(string stateId)
    {
        switch (stateId)
        {
            case LocationStateIds.Operational.Blocked:
                return "Blockiert";
            case LocationStateIds.Operational.RiskyPassage:
                return "Riskante Passage";
            case LocationStateIds.Operational.TemporarilyOpen:
                return "Provisorisch offen";
            case LocationStateIds.Operational.Open:
                return "Offen";
            case LocationStateIds.Operational.Repaired:
                return "Repariert";
            case LocationStateIds.Operational.Sealed:
                return "Versiegelt";
            case LocationStateIds.Operational.None:
                return "Kein Betriebszustand";
            default:
                return stateId;
        }
    }

    private static string LocationPresenceStateText(string stateId)
    {
        switch (stateId)
        {
            case LocationStateIds.Presence.Empty:
                return "Leer";
            case LocationStateIds.Presence.Watched:
                return "Beobachtet";
            case LocationStateIds.Presence.Occupied:
                return "Besetzt";
            case LocationStateIds.Presence.Unknown:
                return "Praesenz unklar";
            default:
                return stateId;
        }
    }

    private static string RiskBandText(LocationRiskBand risk)
    {
        switch (risk)
        {
            case LocationRiskBand.Low:
                return "niedrig";
            case LocationRiskBand.Moderate:
                return "moderat";
            case LocationRiskBand.High:
                return "hoch";
            case LocationRiskBand.Extreme:
                return "extrem";
            default:
                return "keins";
        }
    }

    private static string ConfidenceText(LocationEstimateConfidence confidence)
    {
        switch (confidence)
        {
            case LocationEstimateConfidence.Guess:
                return "Schaetzung";
            case LocationEstimateConfidence.Assessed:
                return "eingeschaetzt";
            case LocationEstimateConfidence.Confirmed:
                return "bestaetigt";
            default:
                return confidence.ToString();
        }
    }

    private void RegisterRail(string elementName, string section)
    {
        RegisterClick(elementName, () => Toggle(section));
    }

    private void RegisterClick(string elementName, System.Action action)
    {
        var element = root?.Q<VisualElement>(elementName);
        if (element == null)
        {
            return;
        }

        element.UnregisterCallback<ClickEvent>(OnElementClick);
        element.userData = action;
        element.RegisterCallback<ClickEvent>(OnElementClick);
    }

    private static void OnElementClick(ClickEvent evt)
    {
        if (evt.currentTarget is VisualElement element && element.userData is System.Action action)
        {
            action();
            evt.StopPropagation();
        }
    }

    private void Toggle(string section)
    {
        if (openSection == section)
        {
            CloseSide();
        }
        else
        {
            Open(section);
        }
    }

    private void Open(string section)
    {
        openSection = section;
        sidePanel?.AddToClassList("open");
        if (sidePanelTitle != null)
        {
            sidePanelTitle.text = TitleFor(section);
        }

        ShowOnly(section);
        SetActiveRail(section);
    }

    private void CloseSide()
    {
        openSection = null;
        sidePanel?.RemoveFromClassList("open");
        SetActiveRail(null);
    }

    private void ShowOnly(string section)
    {
        SetVisible("section-reports", section == Reports);
        SetVisible("section-expedition", false);
        SetVisible("section-scouts", section == Scouts);
        SetVisible("section-journal", section == Journal);
        SetVisible("section-archive", section == Archive);
    }

    private void SetVisible(string elementName, bool visible)
    {
        var element = root?.Q<VisualElement>(elementName);
        if (element == null)
        {
            return;
        }

        if (visible)
        {
            element.AddToClassList("visible");
        }
        else
        {
            element.RemoveFromClassList("visible");
        }
    }

    private void SetDisplay(string elementName, bool visible)
    {
        var element = root?.Q<VisualElement>(elementName);
        if (element != null)
        {
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private void SetActiveRail(string section)
    {
        ToggleActive("rail-scouts", section == Scouts);
        ToggleActive("rail-reports", section == Reports);
        ToggleActive("rail-journal", section == Journal);
        ToggleActive("rail-archive", section == Archive);
    }

    private void ToggleActive(string elementName, bool active)
    {
        var element = root?.Q<VisualElement>(elementName);
        if (element == null)
        {
            return;
        }

        if (active)
        {
            element.AddToClassList("active");
        }
        else
        {
            element.RemoveFromClassList("active");
        }
    }

    private void MakeMapAreaClickThrough()
    {
        var mapArea = root?.Q<VisualElement>(className: "map-area");
        if (mapArea != null)
        {
            mapArea.pickingMode = PickingMode.Ignore;
        }

        var rail = root?.Q<VisualElement>(className: "rail");
        if (rail != null)
        {
            rail.pickingMode = PickingMode.Position;
        }

        if (sidePanel != null)
        {
            sidePanel.pickingMode = PickingMode.Position;
        }
    }

    public bool IsPointerOverMapBlockingUi(Vector3 screenPosition)
    {
        if (root == null || root.panel == null || root.resolvedStyle.display == DisplayStyle.None)
        {
            return false;
        }

        var panelPosition = RuntimePanelUtils.ScreenToPanel(
            root.panel,
            new Vector2(screenPosition.x, screenPosition.y));

        if (IsVisibleElementAt("location-interaction-popup", panelPosition) ||
            IsVisibleElementAt("event-popup", panelPosition) ||
            IsVisibleElementAt("scout-mission-popup", panelPosition) ||
            IsVisibleElementAt("faction-contact-popup", panelPosition))
        {
            return true;
        }

        return IsMapBlockingElement(root.panel.Pick(panelPosition));
    }

    private bool IsVisibleElementAt(string elementName, Vector2 panelPosition)
    {
        var element = root?.Q<VisualElement>(elementName);
        return element != null &&
            element.resolvedStyle.display != DisplayStyle.None &&
            element.resolvedStyle.visibility == Visibility.Visible &&
            element.worldBound.Contains(panelPosition);
    }

    private static bool IsMapBlockingElement(VisualElement element)
    {
        for (var current = element; current != null; current = current.parent)
        {
            if (current.ClassListContains("topbar") ||
                current.ClassListContains("left-panel") ||
                current.ClassListContains("rail") ||
                current.ClassListContains("side-panel") ||
                current.ClassListContains("action-bar") ||
                current.ClassListContains("bi-root") ||
                current.ClassListContains("event-popup") ||
                current.ClassListContains("scout-popup") ||
                current.ClassListContains("faction-popup"))
            {
                return true;
            }

            if (current.ClassListContains("map-area"))
            {
                return false;
            }
        }

        return false;
    }

    private void SetText(string elementName, string text)
    {
        var label = root?.Q<Label>(elementName);
        if (label != null)
        {
            label.text = text;
        }
    }

    private static string TitleFor(string section)
    {
        switch (section)
        {
            case Reports:
                return "Berichte";
            case Scouts:
                return "Späher";
            case Journal:
                return "Journal";
            case Archive:
                return "Archiv";
            default:
                return "";
        }
    }

    private static string MoraleText(int morale)
    {
        if (morale >= 70)
        {
            return "Stabil";
        }

        if (morale >= 40)
        {
            return "Angespannt";
        }

        return "Kritisch";
    }

    private static string ReliabilityText(int reliability)
    {
        if (reliability >= 75)
        {
            return "Hoch";
        }

        if (reliability >= 45)
        {
            return "Mittel";
        }

        return "Niedrig";
    }

    private static string LocationKindText(LocationKind kind)
    {
        switch (kind)
        {
            case LocationKind.BaseCamp:
                return "BASIS";
            case LocationKind.Settlement:
                return "SIEDLUNG";
            case LocationKind.Watchtower:
                return "WACHTTURM";
            case LocationKind.Mine:
                return "MINE";
            case LocationKind.Ruin:
                return "RUINE";
            case LocationKind.WallSegment:
                return "MAUER";
            case LocationKind.BrokenRavine:
                return "HINDERNIS";
            case LocationKind.MarkedGrave:
                return "WARNZEICHEN";
            case LocationKind.AbandonedCamp:
                return "LAGER";
            default:
                return "ORT";
        }
    }

    private static string LocationRiskText(SpecialLocationState location)
    {
        switch (location.Kind)
        {
            case LocationKind.BrokenRavine:
                return location.IsInspected
                    ? "Instabile Schlucht. Ohne Engineer bleibt sie ein gefährliches Hindernis."
                    : "Route unklar. Sollte vor Bewegung oder Planung untersucht werden.";
            case LocationKind.MarkedGrave:
                return location.IsInspected
                    ? "Als mögliche Grenz- oder Drohmarkierung vermerkt."
                    : "Deutlich gesetztes Zeichen. Möglicher Hinweis auf fremdes Gebiet.";
            case LocationKind.AbandonedCamp:
                return location.IsInspected
                    ? "Spuren einer früheren Expedition wurden gesichert."
                    : "Verlassenes Lager. Kann alte, aber unzuverlässige Hinweise enthalten.";
            case LocationKind.Mine:
                return "Verlassene Mine. Mögliches Ressourcen- oder Gefahrenzeichen.";
            case LocationKind.Watchtower:
                return "Erhöhte Landmarke. Kann Blicklinien und fremde Kontrolle anzeigen.";
            case LocationKind.Settlement:
                return "Kontaktpunkt. Verhalten der Expedition kann spätere Begegnungen prägen.";
            case LocationKind.BaseCamp:
                return "Sicherer Ausgangspunkt und Archiv der Expedition.";
            default:
                return "Besonderer Ort. Untersuchung kann neue Informationen erzeugen.";
        }
    }

    private static string KnowledgeText(KnowledgeLevel knowledge)
    {
        switch (knowledge)
        {
            case KnowledgeLevel.Confirmed:
                return "Bestätigt";
            case KnowledgeLevel.Reported:
                return "Gemeldet";
            case KnowledgeLevel.OldOrDoubtful:
                return "Zweifelhaft";
            default:
                return "Unbekannt";
        }
    }
}
