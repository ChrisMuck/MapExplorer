#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class FactionRepresentativeState
{
    public FactionRepresentativeState(
        string id,
        string factionId,
        string displayName,
        FactionRepresentativeRole role,
        string description)
    {
        Id = RequireText(id, nameof(id));
        FactionId = RequireText(factionId, nameof(factionId));
        DisplayName = RequireText(displayName, nameof(displayName));
        Role = role;
        Description = RequireText(description, nameof(description));
    }

    public string Id { get; }

    public string FactionId { get; }

    public string DisplayName { get; }

    public FactionRepresentativeRole Role { get; }

    public string Description { get; }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", name);
        }

        return value;
    }
}
}
