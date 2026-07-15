using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class ExpeditionMemberState
{
    public ExpeditionMemberState(
        string id,
        string name,
        ExpeditionMemberRole role,
        ExpeditionMemberStatus status = ExpeditionMemberStatus.Available,
        int starLevel = 0)
    {
        Id = RequireText(id, nameof(id));
        Name = RequireText(name, nameof(name));
        Role = role;
        Status = status;
        if (starLevel < 0 || starLevel > 3) throw new ArgumentOutOfRangeException(nameof(starLevel), "Star level must be between 0 and 3.");
        StarLevel = starLevel;
    }

    public string Id { get; }

    public string Name { get; }

    public ExpeditionMemberRole Role { get; }

    public ExpeditionMemberStatus Status { get; private set; }

    /// <summary>Role experience used for qualitative resolution, never exposed as hidden world truth.</summary>
    public int StarLevel { get; }

    public void SetStatus(ExpeditionMemberStatus status)
    {
        Status = status;
    }

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
