#nullable enable
using System;
using Game.Core;

namespace Game.App
{

/// <summary>
/// A base-phase action the player takes while preparing the next expedition: heal an injured
/// roster member, recruit a new member, or request an engineer. Each spends Knowledge Points and
/// advances world time (docs rule: "Preparation costs time. Time changes the world.").
/// Costs and day counts are tunable placeholders pending game-feel balancing.
/// </summary>
public sealed class StartBaseActionCommand
{
    public const int HealCost = 5;
    public const int HealDays = 2;
    public const int RecruitCost = 8;
    public const int RecruitDays = 3;
    public const int EngineerCost = 12;
    public const int EngineerDays = 4;

    private static readonly ExpeditionMemberRole[] RecruitRoles =
    {
        ExpeditionMemberRole.Scout,
        ExpeditionMemberRole.Guard,
        ExpeditionMemberRole.Carrier,
        ExpeditionMemberRole.Medic,
        ExpeditionMemberRole.Scholar
    };

    private static readonly string[] RecruitNames =
    {
        "Kael", "Dara", "Piotr", "Lena", "Sven", "Mara", "Ivo", "Tessa"
    };

    private readonly AdvanceBaseTimeCommand advanceBaseTimeCommand = new AdvanceBaseTimeCommand();

    public StartBaseActionResult Execute(GameState game, BaseActionKind kind, string? memberId = null)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        if (game.Expedition.Status == ExpeditionStatus.Active)
        {
            return StartBaseActionResult.Rejected("Base actions are only available during base preparation.");
        }

        return kind switch
        {
            BaseActionKind.HealMember => Heal(game, memberId),
            BaseActionKind.RecruitMember => Recruit(game),
            BaseActionKind.RequestEngineer => RequestEngineer(game),
            _ => StartBaseActionResult.Rejected("Unknown base action.")
        };
    }

    private StartBaseActionResult Heal(GameState game, string? memberId)
    {
        if (string.IsNullOrWhiteSpace(memberId))
        {
            return StartBaseActionResult.Rejected("Select an injured member to heal.");
        }

        var member = game.Roster.FindMember(memberId!);
        if (member == null)
        {
            return StartBaseActionResult.Rejected("Unknown roster member.");
        }

        if (member.Status != ExpeditionMemberStatus.Injured)
        {
            return StartBaseActionResult.Rejected($"{member.Name} does not need healing.");
        }

        if (!game.Base.SpendKnowledgePoints(HealCost))
        {
            return StartBaseActionResult.Rejected($"Not enough Knowledge Points. Need {HealCost}.");
        }

        member.Heal();
        var entry = $"Base preparation: {member.Name} recovered from injury ({HealCost} Knowledge, {HealDays} day(s)).";
        game.Base.AddArchiveEntry(entry);
        advanceBaseTimeCommand.Execute(game, HealDays);
        return StartBaseActionResult.Applied(BaseActionKind.HealMember, HealCost, game.Base.KnowledgePoints, game.World.WorldDay, entry);
    }

    private StartBaseActionResult Recruit(GameState game)
    {
        if (!game.Base.SpendKnowledgePoints(RecruitCost))
        {
            return StartBaseActionResult.Rejected($"Not enough Knowledge Points. Need {RecruitCost}.");
        }

        var index = game.Roster.Members.Count;
        var name = RecruitNames[index % RecruitNames.Length];
        var role = RecruitRoles[index % RecruitRoles.Length];
        var member = game.Roster.Recruit(name, role);
        var entry = $"Base preparation: recruited {member.Name} ({role}) ({RecruitCost} Knowledge, {RecruitDays} day(s)).";
        game.Base.AddArchiveEntry(entry);
        advanceBaseTimeCommand.Execute(game, RecruitDays);
        return StartBaseActionResult.Applied(BaseActionKind.RecruitMember, RecruitCost, game.Base.KnowledgePoints, game.World.WorldDay, entry);
    }

    private StartBaseActionResult RequestEngineer(GameState game)
    {
        if (game.Base.HasRequestedEngineer)
        {
            return StartBaseActionResult.Rejected("An engineer has already been requested.");
        }

        if (!game.Base.SpendKnowledgePoints(EngineerCost))
        {
            return StartBaseActionResult.Rejected($"Not enough Knowledge Points. Need {EngineerCost}.");
        }

        game.Base.MarkEngineerRequested();
        var member = game.Roster.Recruit("Corin", ExpeditionMemberRole.Engineer);
        var entry = $"Base preparation: an engineer ({member.Name}) joined the roster ({EngineerCost} Knowledge, {EngineerDays} day(s)).";
        game.Base.AddArchiveEntry(entry);
        advanceBaseTimeCommand.Execute(game, EngineerDays);
        return StartBaseActionResult.Applied(BaseActionKind.RequestEngineer, EngineerCost, game.Base.KnowledgePoints, game.World.WorldDay, entry);
    }
}
}
