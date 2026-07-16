using System.Collections.Generic;
using System.Linq;
using Game.App;
using Game.Core;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controller for the full-screen base-camp window (mockup Turn 5a). It is data-driven from the
/// real simulation via <see cref="UnityHexMapView"/> (roster, knowledge, factions, archive) and
/// only sends commands through the existing Request*FromUi seam — it never mutates world state.
///
/// Wires: Team (compose), base actions (heal/recruit/engineer/supplies/time), Aufbruch (unit
/// stock + resource loadout with live readiness → start), Basis ausbauen (upgrade tree), Wissen
/// auswerten (evaluation queue), Fraktionen and Archiv (read-only).
///
/// Setup: own GameObject with a UI Document (Source = BaseCampScreen.uxml) + Panel Settings whose
/// sort order renders above the expedition screen. Starts hidden; opened via
/// UnityHexMapView.RequestOpenBaseCampFromUi().
/// </summary>
[RequireComponent(typeof(UIDocument))]
public sealed class BaseCampScreenController : MonoBehaviour
{
    private static readonly Color Gold = new Color32(240, 207, 122, 255);
    private static readonly Color Green = new Color32(143, 174, 116, 255);
    private static readonly Color Blue = new Color32(127, 160, 196, 255);
    private static readonly Color Danger = new Color32(217, 122, 95, 255);
    private static readonly Color Muted = new Color32(143, 137, 124, 255);
    private static readonly Color Neutral = new Color32(201, 195, 180, 255);

    private static readonly HashSet<string> NegativeTraits = new HashSet<string>
    {
        "Übermütig", "Langsam", "Wortkarg", "Sturköpfig", "Berechnend", "Zerstreut", "Narbig", "Kränklich"
    };

    private const int TeamCap = 6;

    // Mirror of StartNewExpeditionCommand's base budgets so the UI preview matches the command's clamping.
    private const int RationBudget = 40;
    private const int MedicineBudget = 4;

    private UnityHexMapView mapView;
    private VisualElement root;
    private string openTab = "team";
    private string selectedPersonId;
    private readonly HashSet<string> selectedTeam = new HashSet<string>();
    private readonly HashSet<string> selectedUnits = new HashSet<string>();
    private int rations = 20;
    private int medicine = 2;
    private ArchiveEntryKind? archiveKindFilter;
    private string archiveSearch = string.Empty;
    private bool isOpen;
    private bool bound;

    public void Initialize(UnityHexMapView view)
    {
        mapView = view;
        Bind();
    }

    private void OnEnable()
    {
        if (mapView == null)
        {
            mapView = FindObjectOfType<UnityHexMapView>();
        }

        Bind();
    }

    private void Bind()
    {
        var document = GetComponent<UIDocument>();
        if (document == null || document.rootVisualElement == null)
        {
            return;
        }

        root = document.rootVisualElement;

        if (bound)
        {
            return;
        }

        bound = true;

        RegisterTab("tab-team", "team");
        RegisterTab("tab-aufbruch", "aufbruch");
        RegisterTab("tab-basis", "basis");
        RegisterTab("tab-wissen", "wissen");
        RegisterTab("tab-frakt", "frakt");
        RegisterTab("tab-archiv", "archiv");

        Click("btn-close", Close);
        Click("base-return-continue", ContinueFromReturnScene);
        Click("detail-toggle", () => ToggleTeam(selectedPersonId));
        Click("detail-heal", HealSelected);
        Click("btn-recruit", () => SendBaseAction(BaseActionKind.RecruitMember));
        Click("btn-engineer", () => SendBaseAction(BaseActionKind.RequestEngineer));
        Click("btn-prepare-supplies", PrepareSupplies);
        Click("btn-advance-time", AdvanceTime);
        Click("btn-ready", StartExpedition);
        Click("rations-dec", () => StepRations(-1));
        Click("rations-inc", () => StepRations(1));
        Click("medicine-dec", () => StepMedicine(-1));
        Click("medicine-inc", () => StepMedicine(1));

        Click("filter-alle", () => SetArchiveFilter(null));
        Click("filter-bericht", () => SetArchiveFilter(ArchiveEntryKind.Bericht));
        Click("filter-brief", () => SetArchiveFilter(ArchiveEntryKind.Brief));
        Click("filter-erkenntnis", () => SetArchiveFilter(ArchiveEntryKind.Erkenntnis));
        Click("filter-vertrag", () => SetArchiveFilter(ArchiveEntryKind.Vertrag));
        Click("filter-notiz", () => SetArchiveFilter(ArchiveEntryKind.Notiz));

        var searchField = root.Q<TextField>("archive-search-field");
        if (searchField != null)
        {
            searchField.RegisterValueChangedCallback(evt =>
            {
                archiveSearch = evt.newValue ?? string.Empty;
                if (openTab == "archiv")
                {
                    BuildArchive(mapView.CurrentGameState);
                }
            });
        }

        if (!isOpen)
        {
            root.style.display = DisplayStyle.None;
        }
    }

    // ---------------------------------------------------------------- open / close

    public void Open()
    {
        if (root == null)
        {
            Bind();
        }

        if (root == null)
        {
            return;
        }

        isOpen = true;
        SetDisplay("base-return-overlay", false);
        root.style.display = DisplayStyle.Flex;
        mapView?.SetExpeditionScreenVisible(false);
        SeedTeamSelection();
        ShowTab(openTab);
        Refresh();
    }

    public void OpenReturnScene(SceneDescriptionResult scene, CompleteExpeditionResult result)
    {
        if (scene == null || result == null || !result.Success)
        {
            Open();
            return;
        }

        Open();
        SetText("base-return-title", scene.Title);
        SetText("base-return-subtitle", scene.Subtitle ?? "Basislager");
        SetText("base-return-body", scene.Message);
        SetText("base-return-summary",
            $"Gesichertes Wissen: {result.SecuredKnowledge} · Funde zur Auswertung: {result.ReturnedFindingsCount} · Expeditionstag {result.ExpeditionDay}");
        var image = root?.Q<VisualElement>("base-return-image");
        var visualId = scene.VisualId ?? "placeholder-base-return";
        var visual = mapView?.GetVisualAssetDefinitionForUi(visualId);
        if (image != null && visual != null) SceneVisualPresenter.Apply(image, visualId, visual);
        SetDisplay("base-return-overlay", true);
    }

    private void ContinueFromReturnScene()
    {
        SetDisplay("base-return-overlay", false);
        ShowTab(openTab);
        Refresh();
    }

    public void Close()
    {
        isOpen = false;
        if (root != null)
        {
            root.style.display = DisplayStyle.None;
        }

        mapView?.SetExpeditionScreenVisible(true);
        mapView?.RefreshToolkitHud();
    }

    private void SeedTeamSelection()
    {
        selectedTeam.Clear();
        selectedUnits.Clear();
        if (mapView == null)
        {
            return;
        }

        foreach (var member in mapView.GetRosterForUi())
        {
            if (member.IsAvailable && selectedTeam.Count < TeamCap)
            {
                selectedTeam.Add(member.Id);
            }
        }

        // Pre-select rested units so the first departure has support by default.
        var stock = mapView.GetUnitStockForUi();
        foreach (var unit in stock.Units)
        {
            if (!unit.IsExhausted)
            {
                selectedUnits.Add(unit.Id);
            }
        }

        var supplyBonus = mapView.CurrentGameState?.Base.PendingSupplyBonus ?? 0;
        rations = System.Math.Min(20 + supplyBonus, RationBudget);
        medicine = System.Math.Min(2, MedicineBudget);

        var first = mapView.GetRosterForUi().FirstOrDefault();
        selectedPersonId = first?.Id;
    }

    // ---------------------------------------------------------------- tabs

    private void RegisterTab(string elementName, string tab)
    {
        Click(elementName, () => { openTab = tab; ShowTab(tab); Refresh(); });
    }

    private void ShowTab(string tab)
    {
        openTab = tab;
        SetSection("section-team", tab == "team");
        SetSection("section-aufbruch", tab == "aufbruch");
        SetSection("section-basis", tab == "basis");
        SetSection("section-wissen", tab == "wissen");
        SetSection("section-frakt", tab == "frakt");
        SetSection("section-archiv", tab == "archiv");

        SetActive("tab-team", tab == "team");
        SetActive("tab-aufbruch", tab == "aufbruch");
        SetActive("tab-basis", tab == "basis");
        SetActive("tab-wissen", tab == "wissen");
        SetActive("tab-frakt", tab == "frakt");
        SetActive("tab-archiv", tab == "archiv");
    }

    // ---------------------------------------------------------------- refresh

    private void Refresh()
    {
        if (root == null || mapView == null || mapView.CurrentGameState == null)
        {
            return;
        }

        var state = mapView.CurrentGameState;

        SetText("value-world-day", $"Welt · Tag {state.World.WorldDay}");
        SetText("value-knowledge", state.Base.KnowledgePoints.ToString());
        SetText("value-supplies", (20 + state.Base.PendingSupplyBonus).ToString());
        SetText("value-party", mapView.GetRosterForUi().Count(m => m.IsAvailable).ToString());

        BuildTeam(state);
        BuildAufbruch(state);
        BuildBasis(state);
        BuildWissen(state);
        BuildFactions(state);
        BuildArchive(state);
    }

    // ---------------------------------------------------------------- team

    private void BuildTeam(GameState state)
    {
        var roster = mapView.GetRosterForUi();
        if (string.IsNullOrEmpty(selectedPersonId) && roster.Count > 0)
        {
            selectedPersonId = roster[0].Id;
        }

        var pool = root.Q<ScrollView>("pool-list");
        if (pool != null)
        {
            pool.Clear();
            foreach (var member in roster)
            {
                pool.Add(BuildPoolRow(member));
            }
        }

        SetText("pool-count", $"{roster.Count} Personen");
        BuildDetail(state, roster);
        BuildTeamRoster(roster);

        SetBaseActionButton("btn-recruit", state.Base.KnowledgePoints >= StartBaseActionCommand.RecruitCost);
        SetBaseActionButton("btn-engineer", !state.Base.HasRequestedEngineer && state.Base.KnowledgePoints >= StartBaseActionCommand.EngineerCost);
        SetBaseActionButton("btn-prepare-supplies", state.Base.KnowledgePoints >= PrepareSuppliesWithKnowledgeCommand.KnowledgeCost);
    }

    private VisualElement BuildPoolRow(BaseMemberState member)
    {
        var row = Div("person-row");
        if (member.Id == selectedPersonId) row.AddToClassList("sel");
        if (selectedTeam.Contains(member.Id)) row.AddToClassList("inteam");

        row.Add(Div("p-avatar"));
        var main = Div("p-main");
        main.Add(Lbl(member.Name, "p-name", "serif"));
        main.Add(Lbl(RoleText(member.Role), "p-role", "mono"));
        main.Add(RichLbl(Stars(member.Level), "p-stars"));
        row.Add(main);

        var side = Div("p-side");
        var dot = Div("p-status-dot");
        dot.style.backgroundColor = StatusColor(member.Status);
        side.Add(dot);
        if (selectedTeam.Contains(member.Id))
        {
            side.Add(Lbl("Team", "p-team-badge"));
        }

        row.Add(side);

        var id = member.Id;
        row.RegisterCallback<ClickEvent>(_ => { selectedPersonId = id; Refresh(); });
        return row;
    }

    private void BuildDetail(GameState state, IReadOnlyList<BaseMemberState> roster)
    {
        var member = roster.FirstOrDefault(m => m.Id == selectedPersonId) ?? roster.FirstOrDefault();
        if (member == null)
        {
            return;
        }

        SetText("detail-name", member.Name);
        SetText("detail-role", RoleText(member.Role));
        SetRichText("detail-stars", Stars(member.Level));
        SetText("detail-level", $"Stufe {member.Level}");
        SetText("detail-bio", string.IsNullOrWhiteSpace(member.Bio) ? "—" : "„" + member.Bio + "“");

        var status = root.Q<Label>("detail-status");
        if (status != null)
        {
            status.text = StatusText(member.Status);
            status.style.color = StatusColor(member.Status);
        }

        var skills = root.Q<VisualElement>("detail-skills");
        if (skills != null)
        {
            skills.Clear();
            foreach (var skill in member.Skills)
            {
                var sk = Div("skill");
                var top = Div("skill-top");
                top.Add(Lbl(skill.Name, "skill-name"));
                top.Add(Lbl($"{skill.Value}/5", "skill-val", "mono"));
                sk.Add(top);
                var bar = Div("skill-bar");
                var fill = Div("skill-fill");
                fill.style.width = Length.Percent(skill.Value / 5f * 100f);
                bar.Add(fill);
                sk.Add(bar);
                skills.Add(sk);
            }
        }

        var traits = root.Q<VisualElement>("detail-traits");
        if (traits != null)
        {
            traits.Clear();
            foreach (var trait in member.Traits)
            {
                traits.Add(Lbl(trait, "trait", NegativeTraits.Contains(trait) ? "trait--bad" : "trait--good"));
            }
        }

        var gear = root.Q<VisualElement>("detail-gear");
        if (gear != null)
        {
            gear.Clear();
            foreach (var item in member.Gear)
            {
                var gr = Div("gear-row");
                gr.Add(Lbl(string.IsNullOrEmpty(item.Icon) ? "◆" : item.Icon, "gear-ico"));
                gr.Add(Lbl(item.Item, "gear-item"));
                gr.Add(Lbl(item.Slot, "gear-slot", "mono"));
                gear.Add(gr);
            }
        }

        SetText("detail-avail", AvailNote(member.Status));

        var toggle = root.Q<Label>("detail-toggle");
        if (toggle != null)
        {
            var inTeam = selectedTeam.Contains(member.Id);
            toggle.RemoveFromClassList("toggle-btn--add");
            toggle.RemoveFromClassList("toggle-btn--rem");
            toggle.RemoveFromClassList("toggle-btn--off");
            if (inTeam)
            {
                toggle.text = "− Aus Team entfernen";
                toggle.AddToClassList("toggle-btn--rem");
            }
            else if (member.IsAvailable)
            {
                toggle.text = "+ Zum Team hinzufügen";
                toggle.AddToClassList("toggle-btn--add");
            }
            else
            {
                toggle.text = "Nicht verfügbar";
                toggle.AddToClassList("toggle-btn--off");
            }
        }

        var heal = root.Q<Label>("detail-heal");
        if (heal != null)
        {
            var injured = member.Status == ExpeditionMemberStatus.Injured || member.Status == ExpeditionMemberStatus.Exhausted;
            var canHeal = injured && state.Base.KnowledgePoints >= StartBaseActionCommand.HealCost;
            heal.style.display = injured ? DisplayStyle.Flex : DisplayStyle.None;
            heal.text = $"Heilen ({StartBaseActionCommand.HealCost} Wissen)";
            heal.EnableInClassList("toggle-btn--off", !canHeal);
        }
    }

    private void BuildTeamRoster(IReadOnlyList<BaseMemberState> roster)
    {
        var teamMembers = roster.Where(m => selectedTeam.Contains(m.Id)).ToList();
        var list = root.Q<ScrollView>("team-list");
        if (list != null)
        {
            list.Clear();
            foreach (var member in teamMembers)
            {
                var row = Div("team-row");
                row.Add(Div("team-avatar"));
                var main = new VisualElement { style = { flexGrow = 1 } };
                main.Add(Lbl(member.Name, "p-name", "serif"));
                main.Add(Lbl(RoleText(member.Role), "p-role", "mono"));
                row.Add(main);
                var rm = Lbl("−", "team-remove");
                var id = member.Id;
                rm.RegisterCallback<ClickEvent>(e => { ToggleTeam(id); e.StopPropagation(); });
                row.Add(rm);
                list.Add(row);
            }

            for (var i = teamMembers.Count; i < TeamCap; i++)
            {
                list.Add(Lbl("+ Freier Platz", "empty-slot"));
            }
        }

        SetText("team-count", $"{teamMembers.Count}/{TeamCap}");
        var capFill = root.Q<VisualElement>("team-cap-fill");
        if (capFill != null)
        {
            capFill.style.width = Length.Percent(teamMembers.Count / (float)TeamCap * 100f);
        }

        var agg = new Dictionary<string, int>();
        foreach (var member in teamMembers)
        {
            foreach (var skill in member.Skills)
            {
                agg[skill.Name] = (agg.TryGetValue(skill.Name, out var v) ? v : 0) + skill.Value;
            }
        }

        var aggEl = root.Q<VisualElement>("team-agg");
        if (aggEl != null)
        {
            aggEl.Clear();
            foreach (var kv in agg.OrderByDescending(k => k.Value).Take(4))
            {
                var r = Div("agg-row");
                r.Add(Lbl(kv.Key, "agg-name"));
                var val = Lbl(kv.Value.ToString(), "agg-val", "mono");
                val.style.color = kv.Value >= 8 ? Green : (kv.Value >= 5 ? Gold : Neutral);
                r.Add(val);
                aggEl.Add(r);
            }
        }
    }

    private void ToggleTeam(string memberId)
    {
        if (string.IsNullOrEmpty(memberId) || mapView == null)
        {
            return;
        }

        var member = mapView.GetRosterForUi().FirstOrDefault(m => m.Id == memberId);
        if (member == null)
        {
            return;
        }

        if (selectedTeam.Contains(memberId))
        {
            selectedTeam.Remove(memberId);
        }
        else if (member.IsAvailable && selectedTeam.Count < TeamCap)
        {
            selectedTeam.Add(memberId);
        }

        Refresh();
    }

    // ---------------------------------------------------------------- aufbruch

    private void BuildAufbruch(GameState state)
    {
        var roster = mapView.GetRosterForUi();
        var teamMembers = roster.Where(m => selectedTeam.Contains(m.Id)).ToList();

        var core = root.Q<ScrollView>("auf-core-list");
        if (core != null)
        {
            core.Clear();
            foreach (var member in teamMembers)
            {
                var row = Div("team-row");
                var avatar = Div("team-avatar");
                avatar.style.width = 32;
                avatar.style.height = 38;
                row.Add(avatar);
                var main = new VisualElement { style = { flexGrow = 1 } };
                main.Add(Lbl(member.Name, "p-name", "serif"));
                main.Add(Lbl(RoleText(member.Role), "p-role", "mono"));
                row.Add(main);
                core.Add(row);
            }

            if (teamMembers.Count == 0)
            {
                core.Add(Lbl("Im Reiter »Team« Mitglieder wählen.", "empty-slot"));
            }
        }

        SetText("auf-core-count", $"aus Reiter »Team« · {teamMembers.Count} Personen");

        // Live Träger/Soldaten stock drawn from the base; each unit is a selectable chip.
        var stock = mapView.GetUnitStockForUi();
        BuildUnitGrid("porter-grid", stock.Porters);
        BuildUnitGrid("soldier-grid", stock.Soldiers);
        SetText("porter-count", $"{stock.Porters.Count(p => selectedUnits.Contains(p.Id))} / {stock.Porters.Count}");
        SetText("soldier-count", $"{stock.Soldiers.Count(s => selectedUnits.Contains(s.Id))} / {stock.Soldiers.Count}");

        // Clamp resources to the base budgets (defensive; steppers already clamp).
        rations = Clamp(rations, 0, RationBudget + state.Base.PendingSupplyBonus);
        medicine = Clamp(medicine, 0, MedicineBudget);
        SetText("rations-value", rations.ToString());
        SetText("medicine-value", medicine.ToString());

        var isEnded = state.Expedition.Status == ExpeditionStatus.Returned || state.Expedition.Status == ExpeditionStatus.Lost;
        var nextReady = isEnded && state.Base.CanStartNextExpedition(state.World.WorldDay);

        var readiness = mapView.ComputeReadinessForUi(teamMembers.Count, selectedUnits.ToList(), rations, medicine);
        SetStat("stat-traglast", $"{readiness.Load} / {readiness.CarryCapacity}", readiness.Overload ? Danger : Green);
        SetStat("stat-verpflegung", $"≈ {readiness.FoodDays} Tage", readiness.FoodDays >= 3 ? Green : Gold);
        SetStat("stat-verteidigung", readiness.Defense.ToString(), readiness.Defense > 0 ? Neutral : Muted);
        SetStat("stat-tempo", readiness.SlowMarch ? "Langsam" : "Normal", readiness.SlowMarch ? Gold : Green);

        var advance = root.Q<Label>("btn-advance-time");
        if (advance != null)
        {
            advance.style.display = isEnded && !nextReady ? DisplayStyle.Flex : DisplayStyle.None;
        }

        var ready = root.Q<Label>("btn-ready");
        if (ready != null)
        {
            var canStart = nextReady && teamMembers.Count > 0 && !readiness.Overload;
            ready.RemoveFromClassList("ready-btn--go");
            ready.RemoveFromClassList("ready-btn--off");
            ready.AddToClassList(canStart ? "ready-btn--go" : "ready-btn--off");
            ready.text = !isEnded
                ? "Expedition ist unterwegs"
                : !nextReady
                    ? $"Bereit ab Tag {state.Base.NextExpeditionAvailableWorldDay}"
                    : teamMembers.Count == 0
                        ? "Kein Team gewählt"
                        : readiness.Overload
                            ? "Überladen – weniger mitnehmen"
                            : "Expedition aufbrechen →";
        }
    }

    private void BuildUnitGrid(string gridName, IReadOnlyList<BaseUnitState> units)
    {
        var grid = root?.Q<VisualElement>(gridName);
        if (grid == null)
        {
            return;
        }

        grid.Clear();
        if (units.Count == 0)
        {
            grid.Add(Lbl("Kein Bestand", "hint", "mono"));
            return;
        }

        foreach (var unit in units)
        {
            var selected = selectedUnits.Contains(unit.Id);
            var chip = Div("unit");
            if (selected) chip.AddToClassList("sel");
            if (unit.IsExhausted) chip.AddToClassList("tired");

            var tile = Div("unit-tile", unit.Kind == BaseUnitKind.Porter ? "unit-tile--porter" : "unit-tile--soldier");
            var dot = Div("unit-dot");
            dot.style.backgroundColor = unit.IsExhausted ? Gold : Green;
            tile.Add(dot);
            chip.Add(tile);
            chip.Add(Lbl(unit.IsExhausted ? "Müde" : "Fit", "unit-cond", "mono"));

            var id = unit.Id;
            chip.RegisterCallback<ClickEvent>(_ => ToggleUnit(id));
            grid.Add(chip);
        }
    }

    private void ToggleUnit(string unitId)
    {
        if (selectedUnits.Contains(unitId))
        {
            selectedUnits.Remove(unitId);
        }
        else
        {
            selectedUnits.Add(unitId);
        }

        Refresh();
    }

    private void StepRations(int delta)
    {
        var max = RationBudget + (mapView?.CurrentGameState?.Base.PendingSupplyBonus ?? 0);
        rations = Clamp(rations + delta, 0, max);
        Refresh();
    }

    private void StepMedicine(int delta)
    {
        medicine = Clamp(medicine + delta, 0, MedicineBudget);
        Refresh();
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        return value > max ? max : value;
    }

    // ---------------------------------------------------------------- basis ausbauen

    private void BuildBasis(GameState state)
    {
        var scroll = root.Q<ScrollView>("basis-scroll");
        if (scroll == null)
        {
            return;
        }

        SetText("basis-knowledge", state.Base.KnowledgePoints.ToString());

        scroll.Clear();
        var upgrades = mapView.GetUpgradesForUi();
        var categories = new List<string>();
        foreach (var upgrade in upgrades)
        {
            if (!categories.Contains(upgrade.Category))
            {
                categories.Add(upgrade.Category);
            }
        }

        foreach (var category in categories)
        {
            var cat = Div("up-cat");
            var head = Div("up-cat-head");
            head.Add(Lbl(category, "up-cat-name", "sans"));
            head.Add(Div("up-cat-line"));
            cat.Add(head);

            var grid = Div("up-grid");
            foreach (var upgrade in upgrades.Where(u => u.Category == category))
            {
                grid.Add(BuildUpgradeCard(state, upgrade));
            }

            cat.Add(grid);
            scroll.Add(cat);
        }
    }

    private VisualElement BuildUpgradeCard(GameState state, BaseUpgradeState upgrade)
    {
        var card = Div("up-card");
        var prereqsMet = upgrade.PrerequisiteIds.All(id => mapView.GetUpgradesForUi().Any(u => u.Id == id && u.IsBuilt));
        var affordable = state.Base.KnowledgePoints >= upgrade.Cost;

        var top = Div("up-top");
        top.Add(Lbl(upgrade.Name, "up-name", "serif"));
        if (!upgrade.IsBuilt)
        {
            var cost = Lbl($"◆ {upgrade.Cost}", "up-cost", "mono");
            if (!prereqsMet || !affordable)
            {
                cost.AddToClassList("up-cost--locked");
            }

            top.Add(cost);
        }

        card.Add(top);
        card.Add(Lbl(upgrade.Description, "up-desc"));

        Label button;
        if (upgrade.IsBuilt)
        {
            card.AddToClassList("up-card--built");
            button = Lbl("✓ Gebaut", "up-btn", "up-btn--built");
        }
        else if (!prereqsMet)
        {
            card.AddToClassList("up-card--locked");
            var names = string.Join(", ", upgrade.PrerequisiteIds.Select(PrereqName));
            button = Lbl($"Benötigt: {names}", "up-btn", "up-btn--locked");
        }
        else if (!affordable)
        {
            button = Lbl($"Benötigt ◆ {upgrade.Cost}", "up-btn", "up-btn--locked");
        }
        else
        {
            button = Lbl("Ausbauen", "up-btn", "up-btn--go");
            var id = upgrade.Id;
            button.RegisterCallback<ClickEvent>(_ => { mapView.RequestStartUpgradeFromUi(id); Refresh(); });
        }

        card.Add(button);
        return card;
    }

    private string PrereqName(string id)
    {
        var upgrade = mapView.GetUpgradesForUi().FirstOrDefault(u => u.Id == id);
        return upgrade != null ? upgrade.Name : id;
    }

    // ---------------------------------------------------------------- wissen auswerten

    private void BuildWissen(GameState state)
    {
        var queue = mapView.GetEvaluationQueueForUi();
        var pending = queue.Items.Where(item => !item.IsEvaluated).ToList();
        var evaluated = queue.Items.Where(item => item.IsEvaluated).ToList();

        // The earliest still-maturing items occupy the evaluator slots; the rest wait.
        var inProgress = new HashSet<string>();
        var slots = queue.EvaluatorCapacity;
        foreach (var item in pending)
        {
            if (slots <= 0)
            {
                break;
            }

            if (item.IsReady)
            {
                continue;
            }

            inProgress.Add(item.Id);
            slots--;
        }

        SetText("auswerter-info", $"{inProgress.Count}/{queue.EvaluatorCapacity}");

        var queueScroll = root.Q<ScrollView>("queue-scroll");
        if (queueScroll != null)
        {
            queueScroll.Clear();
            foreach (var item in pending)
            {
                queueScroll.Add(BuildQueueCard(item, inProgress.Contains(item.Id)));
            }

            if (pending.Count == 0)
            {
                queueScroll.Add(Lbl("Keine offenen Funde zum Auswerten.", "empty-slot"));
            }
        }

        var insightScroll = root.Q<ScrollView>("insight-scroll");
        if (insightScroll != null)
        {
            insightScroll.Clear();
            foreach (var item in evaluated)
            {
                insightScroll.Add(BuildInsightCard(item));
            }

            if (evaluated.Count == 0)
            {
                insightScroll.Add(Lbl("Noch keine Erkenntnisse gesichert.", "empty-slot"));
            }
        }
    }

    private VisualElement BuildQueueCard(EvaluationItemState item, bool inProgress)
    {
        var card = Div("queue-card");
        if (item.IsReady)
        {
            card.AddToClassList("queue-card--ready");
        }

        var top = Div("queue-top");
        top.Add(Lbl(item.Name, "queue-name", "serif"));
        var remaining = Mathf.Max(0, item.RequiredDays - item.ProgressDays);
        var eta = item.IsReady ? "Bereit" : (inProgress ? $"≈ {remaining} Tag(e)" : "Wartet");
        var etaLabel = Lbl(eta, "queue-eta", "mono");
        if (item.IsReady)
        {
            etaLabel.AddToClassList("queue-eta--ready");
        }

        top.Add(etaLabel);
        card.Add(top);
        card.Add(Lbl(item.Source, "queue-from", "mono"));

        if (item.IsReady)
        {
            var button = Lbl("✓ Auswerten", "queue-btn");
            var id = item.Id;
            button.RegisterCallback<ClickEvent>(_ => { mapView.RequestEvaluateKnowledgeItemFromUi(id); Refresh(); });
            card.Add(button);
        }
        else
        {
            var progress = Div("progress");
            var fill = Div("progress-fill");
            fill.style.width = Length.Percent(item.RequiredDays > 0 ? item.ProgressDays / (float)item.RequiredDays * 100f : 0f);
            progress.Add(fill);
            card.Add(progress);
        }

        return card;
    }

    private VisualElement BuildInsightCard(EvaluationItemState item)
    {
        var card = Div("insight-card");
        card.Add(Lbl("✦", "insight-ico"));
        var main = Div("insight-main");
        var head = Div("insight-head");
        head.Add(Lbl(item.Name, "insight-name", "serif"));
        main.Add(head);
        main.Add(Lbl(item.InsightText, "insight-text"));
        card.Add(main);
        return card;
    }

    // ---------------------------------------------------------------- factions

    private void BuildFactions(GameState state)
    {
        var grid = root.Q<VisualElement>("frakt-grid");
        if (grid == null)
        {
            return;
        }

        grid.Clear();
        var factions = mapView.GetKnownFactionsForUi();
        for (var i = 0; i < factions.Count; i++)
        {
            var faction = factions[i];
            var card = Div("faction-card");
            if (i % 2 == 1) card.AddToClassList("faction-card--r");
            card.Add(Div("faction-portrait"));

            var main = Div("faction-main");
            var top = Div("faction-top");
            top.Add(Lbl(faction.Name, "faction-name", "serif"));
            var standing = Lbl(ContactText(faction.ContactStatus), "stand-pill");
            standing.style.color = Gold;
            top.Add(standing);
            main.Add(top);
            main.Add(Lbl($"Vertrauen {faction.Trust} · Zorn {faction.Anger} · Furcht {faction.Fear}", "faction-type", "mono"));

            var attLabel = Div("haltung-label");
            attLabel.Add(Lbl("Haltung", "haltung-name"));
            var attColor = faction.Anger > faction.Trust ? Danger : (faction.Trust >= 30 ? Green : Gold);
            var attText = Lbl(AttitudeText(faction));
            attText.style.fontSize = 11;
            attText.style.color = attColor;
            attLabel.Add(attText);
            main.Add(attLabel);

            var bar = Div("haltung-bar");
            var fill = Div("haltung-fill");
            fill.style.width = Length.Percent(Mathf.Clamp(faction.Trust + 20, 8, 95));
            fill.style.backgroundColor = attColor;
            bar.Add(fill);
            main.Add(bar);

            main.Add(Lbl(faction.Memories.Count > 0 ? "„" + faction.Memories[faction.Memories.Count - 1] + "“" : "Noch wenig über sie bekannt.", "faction-info", "serif"));
            card.Add(main);
            grid.Add(card);
        }

        if (factions.Count == 0)
        {
            grid.Add(Lbl("Noch keine Fraktionen entdeckt.", "empty-slot"));
        }
    }

    // ---------------------------------------------------------------- archive

    private void BuildArchive(GameState state)
    {
        if (state == null)
        {
            return;
        }

        var scroll = root.Q<ScrollView>("archive-scroll");
        if (scroll == null)
        {
            return;
        }

        var all = CollectArchiveEntries(state);

        // Filter counts drive the left rail; recompute each refresh so they stay live.
        SetText("filter-count-alle", all.Count.ToString());
        SetText("filter-count-bericht", ArchiveFilter.CountOfKind(all, ArchiveEntryKind.Bericht).ToString());
        SetText("filter-count-brief", ArchiveFilter.CountOfKind(all, ArchiveEntryKind.Brief).ToString());
        SetText("filter-count-erkenntnis", ArchiveFilter.CountOfKind(all, ArchiveEntryKind.Erkenntnis).ToString());
        SetText("filter-count-vertrag", ArchiveFilter.CountOfKind(all, ArchiveEntryKind.Vertrag).ToString());
        SetText("filter-count-notiz", ArchiveFilter.CountOfKind(all, ArchiveEntryKind.Notiz).ToString());

        SetActive("filter-alle", !archiveKindFilter.HasValue);
        SetActive("filter-bericht", archiveKindFilter == ArchiveEntryKind.Bericht);
        SetActive("filter-brief", archiveKindFilter == ArchiveEntryKind.Brief);
        SetActive("filter-erkenntnis", archiveKindFilter == ArchiveEntryKind.Erkenntnis);
        SetActive("filter-vertrag", archiveKindFilter == ArchiveEntryKind.Vertrag);
        SetActive("filter-notiz", archiveKindFilter == ArchiveEntryKind.Notiz);

        var filtered = ArchiveFilter.Filter(all, archiveKindFilter, archiveSearch);

        scroll.Clear();
        if (filtered.Count == 0)
        {
            scroll.Add(Lbl(all.Count == 0 ? "Das Archiv ist noch leer." : "Keine Treffer für diesen Filter.", "empty-slot"));
            return;
        }

        // Newest first.
        for (var i = filtered.Count - 1; i >= 0; i--)
        {
            scroll.Add(BuildArchiveRow(filtered[i]));
        }
    }

    private static List<ArchiveEntryState> CollectArchiveEntries(GameState state)
    {
        var all = new List<ArchiveEntryState>(state.Base.Archive);

        // Scout reports live in KnowledgeState; surface them as Bericht rows in the same view.
        foreach (var report in state.Knowledge.ScoutReports)
        {
            all.Add(new ArchiveEntryState(report.Title, ArchiveEntryKind.Bericht, "Späher", 0, ArchiveReliability.Mittel));
        }

        return all;
    }

    private VisualElement BuildArchiveRow(ArchiveEntryState entry)
    {
        var row = Div("archive-row");
        var icon = Lbl(ArchiveIcon(entry.Kind), "archive-icon");
        icon.style.color = ArchiveKindColor(entry.Kind);
        row.Add(icon);

        var main = Div("archive-main");
        main.Add(Lbl(entry.Title, "archive-name", "serif"));
        main.Add(Lbl(ArchiveSourceLine(entry), "archive-src", "mono"));
        row.Add(main);

        row.Add(Lbl(entry.Kind.ToString(), "type-tag"));

        var rel = Lbl(ReliabilityText(entry.Reliability), "archive-rel", "mono");
        rel.style.color = ReliabilityColor(entry.Reliability);
        row.Add(rel);

        row.Add(Lbl(entry.WorldDay > 0 ? $"Tag {entry.WorldDay}" : "—", "archive-date", "mono"));
        return row;
    }

    private void SetArchiveFilter(ArchiveEntryKind? kind)
    {
        archiveKindFilter = kind;
        BuildArchive(mapView.CurrentGameState);
    }

    private static string ArchiveSourceLine(ArchiveEntryState entry)
    {
        return entry.WorldDay > 0 ? $"{entry.Source} · Tag {entry.WorldDay}" : entry.Source;
    }

    private static string ArchiveIcon(ArchiveEntryKind kind)
    {
        switch (kind)
        {
            case ArchiveEntryKind.Bericht: return "✎";
            case ArchiveEntryKind.Brief: return "✉";
            case ArchiveEntryKind.Erkenntnis: return "◆";
            case ArchiveEntryKind.Vertrag: return "⇄";
            default: return "✦";
        }
    }

    private Color ArchiveKindColor(ArchiveEntryKind kind)
    {
        switch (kind)
        {
            case ArchiveEntryKind.Bericht: return Gold;
            case ArchiveEntryKind.Brief: return Blue;
            case ArchiveEntryKind.Erkenntnis: return Green;
            case ArchiveEntryKind.Vertrag: return Neutral;
            default: return Muted;
        }
    }

    private static string ReliabilityText(ArchiveReliability reliability)
    {
        switch (reliability)
        {
            case ArchiveReliability.Niedrig: return "Verläss. Niedrig";
            case ArchiveReliability.Mittel: return "Verläss. Mittel";
            case ArchiveReliability.Hoch: return "Verläss. Hoch";
            case ArchiveReliability.Bestaetigt: return "Bestätigt";
            default: return "—";
        }
    }

    private Color ReliabilityColor(ArchiveReliability reliability)
    {
        switch (reliability)
        {
            case ArchiveReliability.Bestaetigt:
            case ArchiveReliability.Hoch: return Green;
            case ArchiveReliability.Mittel: return Gold;
            case ArchiveReliability.Niedrig: return Danger;
            default: return Muted;
        }
    }

    // ---------------------------------------------------------------- base actions / commands

    private void SendBaseAction(BaseActionKind kind)
    {
        mapView?.RequestStartBaseActionFromUi(kind);
        Refresh();
    }

    private void HealSelected()
    {
        if (mapView == null || string.IsNullOrEmpty(selectedPersonId))
        {
            return;
        }

        var member = mapView.GetRosterForUi().FirstOrDefault(m => m.Id == selectedPersonId);
        if (member != null && (member.Status == ExpeditionMemberStatus.Injured || member.Status == ExpeditionMemberStatus.Exhausted))
        {
            mapView.RequestStartBaseActionFromUi(BaseActionKind.HealMember, member.Id);
            Refresh();
        }
    }

    private void PrepareSupplies()
    {
        mapView?.RequestPrepareSuppliesWithKnowledgeFromUi();
        Refresh();
    }

    private void AdvanceTime()
    {
        mapView?.RequestAdvanceBaseTimeFromUi();
        Refresh();
    }

    private void StartExpedition()
    {
        if (mapView == null || selectedTeam.Count == 0)
        {
            return;
        }

        mapView.RequestStartLoadoutExpeditionFromUi(selectedTeam.ToList(), selectedUnits.ToList(), rations, medicine);
        if (mapView.CurrentGameState != null && mapView.CurrentGameState.Expedition.Status == ExpeditionStatus.Active)
        {
            Close();
            return;
        }

        Refresh();
    }

    // ---------------------------------------------------------------- small helpers

    private void Click(string elementName, System.Action action)
    {
        var element = root?.Q<VisualElement>(elementName);
        if (element == null)
        {
            return;
        }

        element.RegisterCallback<ClickEvent>(_ => action());
    }

    private void SetSection(string name, bool visible)
    {
        var element = root?.Q<VisualElement>(name);
        if (element == null) return;
        if (visible) element.AddToClassList("visible"); else element.RemoveFromClassList("visible");
    }

    private void SetActive(string name, bool active)
    {
        var element = root?.Q<VisualElement>(name);
        if (element == null) return;
        if (active) element.AddToClassList("active"); else element.RemoveFromClassList("active");
    }

    private void SetText(string name, string text)
    {
        var label = root?.Q<Label>(name);
        if (label != null) label.text = text;
    }

    private void SetDisplay(string name, bool visible)
    {
        var element = root?.Q<VisualElement>(name);
        if (element != null) element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void SetRichText(string name, string richText)
    {
        var label = root?.Q<Label>(name);
        if (label != null) { label.enableRichText = true; label.text = richText; }
    }

    private void SetStat(string name, string text, Color color)
    {
        var label = root?.Q<Label>(name);
        if (label == null) return;
        label.text = text;
        label.style.color = color;
    }

    private void SetBaseActionButton(string name, bool enabled)
    {
        var button = root?.Q<Label>(name);
        if (button == null) return;
        button.EnableInClassList("up-btn--locked", !enabled);
    }

    private static VisualElement Div(params string[] classes)
    {
        var element = new VisualElement();
        foreach (var c in classes)
        {
            if (!string.IsNullOrEmpty(c)) element.AddToClassList(c);
        }

        return element;
    }

    private static Label Lbl(string text, params string[] classes)
    {
        var label = new Label(text);
        foreach (var c in classes)
        {
            if (!string.IsNullOrEmpty(c)) label.AddToClassList(c);
        }

        return label;
    }

    private static Label RichLbl(string richText, string cssClass)
    {
        var label = Lbl(richText, cssClass);
        label.enableRichText = true;
        return label;
    }

    private static string Stars(int level)
    {
        var clamped = Mathf.Clamp(level, 0, 5);
        var full = new string('★', clamped);
        var empty = new string('☆', 5 - clamped);
        return "<color=#F0CF7A>" + full + "</color><color=#43403A>" + empty + "</color>";
    }

    private static string RoleText(ExpeditionMemberRole role)
    {
        switch (role)
        {
            case ExpeditionMemberRole.Scout: return "Späher";
            case ExpeditionMemberRole.Guard: return "Wache";
            case ExpeditionMemberRole.Carrier: return "Träger";
            case ExpeditionMemberRole.Medic: return "Medic";
            case ExpeditionMemberRole.Scholar: return "Gelehrter";
            case ExpeditionMemberRole.Engineer: return "Ingenieur";
            default: return role.ToString();
        }
    }

    private static string StatusText(ExpeditionMemberStatus status)
    {
        switch (status)
        {
            case ExpeditionMemberStatus.Available: return "Verfügbar";
            case ExpeditionMemberStatus.Assigned: return "Im Einsatz";
            case ExpeditionMemberStatus.Injured: return "Verletzt";
            case ExpeditionMemberStatus.Exhausted: return "Erschöpft";
            case ExpeditionMemberStatus.Missing: return "Vermisst";
            case ExpeditionMemberStatus.Dead: return "Gefallen";
            default: return status.ToString();
        }
    }

    private static Color StatusColor(ExpeditionMemberStatus status)
    {
        switch (status)
        {
            case ExpeditionMemberStatus.Available: return Green;
            case ExpeditionMemberStatus.Injured: return Danger;
            case ExpeditionMemberStatus.Exhausted: return Gold;
            case ExpeditionMemberStatus.Assigned: return Blue;
            default: return Muted;
        }
    }

    private static string AvailNote(ExpeditionMemberStatus status)
    {
        switch (status)
        {
            case ExpeditionMemberStatus.Available: return "Bereit für die nächste Expedition";
            case ExpeditionMemberStatus.Injured: return "Verletzt — heilen kostet Wissen und Zeit";
            case ExpeditionMemberStatus.Exhausted: return "Erschöpft — braucht Ruhe oder Pflege";
            case ExpeditionMemberStatus.Assigned: return "Derzeit mit einer Expedition unterwegs";
            default: return "";
        }
    }

    private static string ContactText(FactionContactStatus status)
    {
        switch (status)
        {
            case FactionContactStatus.Contacted: return "Kontakt";
            case FactionContactStatus.Rumored: return "Gerücht";
            default: return "Unbekannt";
        }
    }

    private static string AttitudeText(FactionState faction)
    {
        if (faction.Anger >= 30) return "Feindselig";
        if (faction.Fear >= 30) return "Nervös";
        if (faction.Trust >= 40) return "Freundlich";
        if (faction.Trust >= 15) return "Wachsam";
        return "Unbekannt";
    }
}
