using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class SpecialLocationState
{
    private readonly List<string> modifierIds = new();

    public SpecialLocationState(string id, LocationKind kind, HexCoord coord, string name)
        : this(
            id,
            kind,
            coord,
            name,
            LocationAnchor.Point(coord),
            archetypeId: null,
            variantId: null,
            modifierIds: null,
            contentProfileId: null,
            interactionStateId: LocationStateIds.Interaction.Untouched,
            operationalStateId: LocationStateIds.Operational.None,
            presenceStateId: LocationStateIds.Presence.Unknown)
    {
    }

    public SpecialLocationState(
        string id,
        LocationKind kind,
        HexCoord coord,
        string name,
        LocationAnchor anchor,
        string? archetypeId,
        string? variantId,
        IEnumerable<string>? modifierIds = null,
        string? contentProfileId = null,
        string interactionStateId = LocationStateIds.Interaction.Untouched,
        string operationalStateId = LocationStateIds.Operational.None,
        string presenceStateId = LocationStateIds.Presence.Unknown)
    {
        Id = RequireText(id, nameof(id));
        Kind = kind;
        Coord = coord;
        Name = RequireText(name, nameof(name));
        Anchor = anchor ?? throw new ArgumentNullException(nameof(anchor));
        ArchetypeId = NormalizeOptionalText(archetypeId);
        VariantId = NormalizeOptionalText(variantId);
        ContentProfileId = NormalizeOptionalText(contentProfileId);
        InteractionStateId = RequireText(interactionStateId, nameof(interactionStateId));
        OperationalStateId = RequireText(operationalStateId, nameof(operationalStateId));
        PresenceStateId = RequireText(presenceStateId, nameof(presenceStateId));

        if (modifierIds != null)
        {
            foreach (var modifierId in modifierIds)
            {
                this.modifierIds.Add(RequireText(modifierId, nameof(modifierIds)));
            }
        }
    }

    public string Id { get; }

    public LocationKind Kind { get; }

    public HexCoord Coord { get; }

    public string Name { get; }

    public LocationAnchor Anchor { get; }

    public string? ArchetypeId { get; }

    public string? VariantId { get; }

    public IReadOnlyList<string> ModifierIds
    {
        get { return modifierIds; }
    }

    public string? ContentProfileId { get; }

    public bool IsDiscovered { get; private set; }

    public bool IsInspected { get; private set; }

    public int? DiscoveredWorldDay { get; private set; }

    public int? InspectedWorldDay { get; private set; }

    public string InteractionStateId { get; private set; }

    public string OperationalStateId { get; private set; }

    public string PresenceStateId { get; private set; }

    public LocationProjectState? ActiveProject { get; private set; }

    public void Discover(int worldDay)
    {
        if (worldDay < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(worldDay), worldDay, "World day must be at least 1.");
        }

        if (IsDiscovered)
        {
            return;
        }

        IsDiscovered = true;
        DiscoveredWorldDay = worldDay;
    }

    public void Inspect(int worldDay)
    {
        Discover(worldDay);
        if (IsInspected)
        {
            return;
        }

        IsInspected = true;
        InspectedWorldDay = worldDay;
    }

    public void ForgetDiscovery()
    {
        if (Kind == LocationKind.BaseCamp)
        {
            return;
        }

        IsDiscovered = false;
        IsInspected = false;
        DiscoveredWorldDay = null;
        InspectedWorldDay = null;
    }

    public void SetState(string channel, string stateId)
    {
        channel = RequireText(channel, nameof(channel));
        stateId = RequireText(stateId, nameof(stateId));

        switch (channel)
        {
            case LocationStateChannels.Interaction:
                InteractionStateId = stateId;
                break;
            case LocationStateChannels.Operational:
                OperationalStateId = stateId;
                break;
            case LocationStateChannels.Presence:
                PresenceStateId = stateId;
                break;
            default:
                throw new ArgumentException($"Unknown location state channel '{channel}'.", nameof(channel));
        }
    }

    public void StartProject(string projectId, int requiredProgress)
    {
        if (requiredProgress < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredProgress), requiredProgress, "Project progress must be at least one.");
        }

        ActiveProject = new LocationProjectState(RequireText(projectId, nameof(projectId)), requiredProgress);
    }

    public void AdvanceProject()
    {
        if (ActiveProject == null)
        {
            throw new InvalidOperationException("No project is active at this location.");
        }

        ActiveProject.Advance();
    }

    public void ClearProject()
    {
        ActiveProject = null;
    }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", name);
        }

        return value;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
}
