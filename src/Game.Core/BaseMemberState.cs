#nullable enable
using System;
using System.Collections.Generic;

namespace Game.Core
{

/// <summary>
/// A member in the base roster (the persistent pool the player draws from when composing the
/// next expedition). Unlike <see cref="ExpeditionMemberState"/>, roster members persist between
/// expeditions so injuries, recruits and the requested engineer carry over. Roster members also
/// carry the richer profile shown on the base-camp person sheet (level, bio, skills, traits, gear).
/// </summary>
public sealed class BaseMemberState
{
    private readonly List<MemberSkill> skills = new();
    private readonly List<string> traits = new();
    private readonly List<MemberGear> gear = new();

    public BaseMemberState(
        string id,
        string name,
        ExpeditionMemberRole role,
        ExpeditionMemberStatus status = ExpeditionMemberStatus.Available,
        int level = 1,
        string? bio = null,
        IEnumerable<MemberSkill>? skills = null,
        IEnumerable<string>? traits = null,
        IEnumerable<MemberGear>? gear = null)
    {
        Id = RequireText(id, nameof(id));
        Name = RequireText(name, nameof(name));
        Role = role;
        Status = status;
        Level = level < 1 ? 1 : level;
        Bio = bio ?? string.Empty;
        if (skills != null)
        {
            this.skills.AddRange(skills);
        }

        if (traits != null)
        {
            this.traits.AddRange(traits);
        }

        if (gear != null)
        {
            this.gear.AddRange(gear);
        }
    }

    public string Id { get; }

    public string Name { get; }

    public ExpeditionMemberRole Role { get; }

    public ExpeditionMemberStatus Status { get; private set; }

    public int Level { get; }

    public string Bio { get; }

    public IReadOnlyList<MemberSkill> Skills => skills;

    public IReadOnlyList<string> Traits => traits;

    public IReadOnlyList<MemberGear> Gear => gear;

    public bool IsAvailable => Status == ExpeditionMemberStatus.Available;

    public void SetStatus(ExpeditionMemberStatus status)
    {
        Status = status;
    }

    public void Heal()
    {
        if (Status == ExpeditionMemberStatus.Injured || Status == ExpeditionMemberStatus.Exhausted)
        {
            Status = ExpeditionMemberStatus.Available;
        }
    }

    /// <summary>Creates a fresh active expedition member from this roster entry.</summary>
    public ExpeditionMemberState ToExpeditionMember()
    {
        return new ExpeditionMemberState(Id, Name, Role);
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
