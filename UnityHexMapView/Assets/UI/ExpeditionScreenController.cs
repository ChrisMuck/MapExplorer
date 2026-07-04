using System.Collections.Generic;
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
        RegisterClick("action-open-report", () => Open(Reports));
        RegisterClick("action-marker-from-report", () => mapView?.RequestMarkerFromReportHintFromUi(selectedReportIndex, selectedHintIndex));
        RegisterClick("action-send-scout", () => mapView?.RequestSendScoutMissionFromUi());
        RegisterClick("action-send-scout-panel", () => mapView?.RequestSendScoutMissionFromUi());
        RegisterClick("action-add-marker", () => mapView?.RequestAddMarkerFromUi());
        RegisterClick("action-add-note", () => mapView?.RequestAddNoteFromUi());
        RegisterClick("action-end-day", () => mapView?.RequestEndDayFromUi());

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

        RefreshSelectedField(state);
        RefreshAlert(state);
        RefreshReport(state);
    }

    private void RefreshSelectedField(GameState state)
    {
        var coord = mapView.CurrentSelectedCoreCoord;
        SetText("coord", $"Feld {coord.Q:00} / {coord.R:00}");
        SetText("selected-knowledge", KnowledgeText(mapView.GetKnowledgeForUi(coord)));
        SetText("selected-reliability", "Verlässlichkeit: Mittel");

        if (mapView.TryGetTileForUi(coord, out var tile))
        {
            var location = mapView.GetLocationForUi(coord);
            var locationText = location == null ? "keine Landmarke" : $"{location.Name} · {location.Kind}";
            SetText("selected-meta", $"{tile.Terrain} · Höhe {tile.Elevation} · {locationText}");
        }
        else
        {
            SetText("selected-meta", "Außerhalb der Karte");
        }

        var markers = mapView.GetMarkersForUi(coord);
        var notes = mapView.GetNotesForUi(coord);
        SetText("selected-sign-1", markers.Count > 0 ? markers[markers.Count - 1].Label : "Keine Marker");
        SetText("selected-sign-2", mapView.CurrentInteractionMessage);
        SetText("selected-note", notes.Count > 0 ? $"„{notes[notes.Count - 1].Text}”" : "„Noch keine Notiz für dieses Feld.”");
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
