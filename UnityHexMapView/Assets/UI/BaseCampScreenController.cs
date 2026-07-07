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
/// First increment wires: Team (compose), base actions (heal/recruit/engineer/supplies/time),
/// Aufbruch (start), Fraktionen and Archiv (read-only). "Basis ausbauen" and "Wissen auswerten"
/// keep the mockup layout as non-functional placeholders (roadmap).
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

    private UnityHexMapView mapView;
    private VisualElement root;
    private string openTab = "team";
    private string selectedPersonId;
    private readonly HashSet<string> selectedTeam = new HashSet<string>();
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
        Click("detail-toggle", () => ToggleTeam(selectedPersonId));
        Click("detail-heal", HealSelected);
        Click("btn-recruit", () => SendBaseAction(BaseActionKind.RecruitMember));
        Click("btn-engineer", () => SendBaseAction(BaseActionKind.RequestEngineer));
        Click("btn-prepare-supplies", PrepareSupplies);
        Click("btn-advance-time", AdvanceTime);
        Click("btn-ready", StartExpedition);

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
        root.style.display = DisplayStyle.Flex;
        mapView?.SetExpeditionScreenVisible(false);
        SeedTeamSelection();
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

        // Träger/Soldaten stock is a roadmap system — show a hint instead of a live grid.
        ShowGridHint("porter-grid", "Bestand wächst mit Basisausbau (Roadmap).");
        ShowGridHint("soldier-grid", "Bestand wächst mit Basisausbau (Roadmap).");
        SetText("porter-count", "—");
        SetText("soldier-count", "—");

        SetText("rations-value", (20 + state.Base.PendingSupplyBonus).ToString());
        SetText("medicine-value", "3");

        var isEnded = state.Expedition.Status == ExpeditionStatus.Returned || state.Expedition.Status == ExpeditionStatus.Lost;
        var nextReady = isEnded && state.Base.CanStartNextExpedition(state.World.WorldDay);

        SetStat("stat-traglast", $"{teamMembers.Count} Personen", teamMembers.Count > 0 ? Green : Muted);
        SetStat("stat-verpflegung", $"≈ {(teamMembers.Count > 0 ? (20 + state.Base.PendingSupplyBonus) / teamMembers.Count : 0)} Tage", Gold);
        SetStat("stat-verteidigung", teamMembers.Count(m => m.Role == ExpeditionMemberRole.Guard).ToString(), Neutral);
        SetStat("stat-tempo", nextReady ? "Bereit" : "Wartet", nextReady ? Green : Gold);

        var advance = root.Q<Label>("btn-advance-time");
        if (advance != null)
        {
            advance.style.display = isEnded && !nextReady ? DisplayStyle.Flex : DisplayStyle.None;
        }

        var ready = root.Q<Label>("btn-ready");
        if (ready != null)
        {
            var canStart = nextReady && teamMembers.Count > 0;
            ready.RemoveFromClassList("ready-btn--go");
            ready.RemoveFromClassList("ready-btn--off");
            ready.AddToClassList(canStart ? "ready-btn--go" : "ready-btn--off");
            ready.text = !isEnded
                ? "Expedition ist unterwegs"
                : !nextReady
                    ? $"Bereit ab Tag {state.Base.NextExpeditionAvailableWorldDay}"
                    : teamMembers.Count == 0
                        ? "Kein Team gewählt"
                        : "Expedition aufbrechen →";
        }
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
        var factions = state.Factions;
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
        var scroll = root.Q<ScrollView>("archive-scroll");
        if (scroll == null)
        {
            return;
        }

        scroll.Clear();
        var entries = state.Base.ArchiveEntries;
        for (var i = entries.Count - 1; i >= 0; i--)
        {
            var row = Div("archive-row");
            row.Add(Lbl("❋", "archive-icon"));
            var main = Div("archive-main");
            main.Add(Lbl(entries[i], "archive-name", "serif"));
            row.Add(main);
            scroll.Add(row);
        }

        foreach (var report in state.Knowledge.ScoutReports)
        {
            var row = Div("archive-row");
            row.Add(Lbl("✎", "archive-icon"));
            var main = Div("archive-main");
            main.Add(Lbl(report.Title, "archive-name", "serif"));
            main.Add(Lbl("Späherbericht", "archive-src", "mono"));
            row.Add(main);
            scroll.Add(row);
        }

        if (entries.Count == 0 && state.Knowledge.ScoutReports.Count == 0)
        {
            scroll.Add(Lbl("Das Archiv ist noch leer.", "empty-slot"));
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

        mapView.RequestStartNewExpeditionFromUi(selectedTeam.ToList());
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

    private void ShowGridHint(string name, string hint)
    {
        var grid = root?.Q<VisualElement>(name);
        if (grid == null) return;
        grid.Clear();
        grid.Add(Lbl(hint, "hint", "mono"));
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
