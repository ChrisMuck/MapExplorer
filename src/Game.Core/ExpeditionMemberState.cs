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
        ExpeditionMemberStatus status = ExpeditionMemberStatus.Available)
    {
        Id = RequireText(id, nameof(id));
        Name = RequireText(name, nameof(name));
        Role = role;
        Status = status;
    }

    public string Id { get; }

    public string Name { get; }

    public ExpeditionMemberRole Role { get; }

    public ExpeditionMemberStatus Status { get; private set; }

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
