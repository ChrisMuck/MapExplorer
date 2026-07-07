#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

/// <summary>
/// Persistent pool of people at the base. The player composes each expedition from the
/// available members here; survivors are absorbed back after every return, and recruits
/// (including a requested engineer) are added during the base phase.
/// </summary>
public sealed class BaseRosterState
{
    private readonly List<BaseMemberState> members = new();
    private int recruitCounter;

    public BaseRosterState()
    {
    }

    public BaseRosterState(IEnumerable<BaseMemberState> initialMembers)
    {
        foreach (var member in initialMembers ?? throw new ArgumentNullException(nameof(initialMembers)))
        {
            Add(member);
        }
    }

    public IReadOnlyList<BaseMemberState> Members
    {
        get { return members; }
    }

    public void Add(BaseMemberState member)
    {
        if (member == null)
        {
            throw new ArgumentNullException(nameof(member));
        }

        if (members.Any(existing => existing.Id == member.Id))
        {
            throw new InvalidOperationException($"Roster already contains a member with id '{member.Id}'.");
        }

        members.Add(member);
    }

    public BaseMemberState? FindMember(string memberId)
    {
        return members.FirstOrDefault(member => member.Id == memberId);
    }

    public IReadOnlyList<BaseMemberState> Available()
    {
        return members.Where(member => member.IsAvailable).ToList();
    }

    public IReadOnlyList<BaseMemberState> Injured()
    {
        return members.Where(member => member.Status == ExpeditionMemberStatus.Injured).ToList();
    }

    /// <summary>
    /// Recruits a new member with a stable, unique id and returns it.
    /// </summary>
    public BaseMemberState Recruit(string name, ExpeditionMemberRole role)
    {
        recruitCounter += 1;
        var member = new BaseMemberState($"recruit-{recruitCounter}", name, role);
        members.Add(member);
        return member;
    }

    /// <summary>
    /// Reconciles the roster with an expedition that just returned: known members inherit the
    /// expedition status (injured/missing/dead), and any member not yet in the pool is added.
    /// </summary>
    public void AbsorbReturningMembers(ExpeditionState expedition)
    {
        if (expedition == null)
        {
            throw new ArgumentNullException(nameof(expedition));
        }

        foreach (var member in expedition.Members)
        {
            var existing = FindMember(member.Id);
            if (existing != null)
            {
                existing.SetStatus(member.Status);
            }
            else
            {
                members.Add(new BaseMemberState(member.Id, member.Name, member.Role, member.Status));
            }
        }
    }
}
}
