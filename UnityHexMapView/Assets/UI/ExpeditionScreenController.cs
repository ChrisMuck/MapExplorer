using System.Collections.Generic;
using Game.App;
using Game.Core;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public sealed class ExpeditionScreenController : MonoBehaviour
{
    private const string Reports = "reports";
    private const string Scouts = "scouts";
    private const string Journal = "journal";
    private const string Archive = "archive";

    private UnityHexMapView mapView;
    private VisualElement root;
    private VisualElement sidePanel;
    private Label sidePanelTitle;
    private string openSection;
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

    public void Initialize(UnityHexMapView view)
    {
        mapView = view;
        Bind();
        Refresh();
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
        RegisterClick("action-inspect", () => mapView?.RequestInspectSelectedLocationFromUi());
        RegisterClick("action-camp", () => mapView?.RequestPrepareSuppliesWithKnowledgeFromUi());
        RegisterClick("action-return-base", () => mapView?.RequestCompleteExpeditionFromUi());
        RegisterClick("action-end-day", () => mapView?.RequestEndDayFromUi());
        RegisterClick("faction-modal-close", () => mapView?.RequestCloseFactionInteractionFromUi());
        RegisterClick("faction-response-leave", () => mapView?.RequestCloseFactionInteractionFromUi());
        RegisterClick("scout-modal-close", CloseScoutMissionPopup);
        RegisterClick("scout-mission-send", SendSelectedScoutMission);
        RegisterScoutMissionOptions();

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
        RefreshFactionInteractionPopup(state);
        RefreshScoutMissionPopup(state);
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
            var locationText = location == null ? "keine Landmarke" : $"{location.Name} · {LocationKindText(location.Kind)}";
            SetText("selected-meta", $"{tile.Terrain} · Höhe {tile.Elevation} · {locationText}");
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
        SetText("selected-location-status", location.IsInspected ? "Untersucht" : "Neu");
        SetText("selected-location-name", location.Name);
        SetText("selected-location-risk", LocationRiskText(location));
        SetText("selected-location-action", location.IsInspected ? "Archiviert. Weitere Hinweise im Journal prüfen." : "Aktion: Ort untersuchen und Ereignis auslösen.");

        if (inspectButton != null)
        {
            inspectButton.text = location.IsInspected ? "⌕ Ort ansehen" : "⌕ Ort untersuchen";
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
        SetText("report-title-main", report.Title);
        SetText("report-sub-main", $"Expedition · Späher · Tag {state.World.WorldDay}");
        SetText("report-reliability-main", $"Verlässlichkeit: {ReliabilityText(report.Reliability)}");
        SetText("report-direction-main", report.RelatedCoords.Count > 0 ? report.RelatedCoords[0].ToString() : "Unbekannt");
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

            var meta = new Label($"{ReliabilityText(report.Reliability)} | {report.Hints.Count} Hinweise");
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
