#nullable enable
using System;

namespace Game.Core
{

/// <summary>A carried item in a member's loadout (base-camp person sheet).</summary>
public sealed class MemberGear
{
    public MemberGear(string slot, string item, string? icon = null)
    {
        Slot = RequireText(slot, nameof(slot));
        Item = RequireText(item, nameof(item));
        Icon = icon;
    }

    public string Slot { get; }

    public string Item { get; }

    public string? Icon { get; }

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
