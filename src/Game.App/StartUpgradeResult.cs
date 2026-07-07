#nullable enable
using System;

namespace Game.App
{

public sealed class StartUpgradeResult
{
    private StartUpgradeResult(
        bool success,
        string? upgradeId,
        string? name,
        int knowledgeSpent,
        int remainingKnowledgePoints,
        string? archiveEntry,
        string? error)
    {
        Success = success;
        UpgradeId = upgradeId;
        Name = name;
        KnowledgeSpent = knowledgeSpent;
        RemainingKnowledgePoints = remainingKnowledgePoints;
        ArchiveEntry = archiveEntry;
        Error = error;
    }

    public bool Success { get; }

    public string? UpgradeId { get; }

    public string? Name { get; }

    public int KnowledgeSpent { get; }

    public int RemainingKnowledgePoints { get; }

    public string? ArchiveEntry { get; }

    public string? Error { get; }

    public static StartUpgradeResult Built(string upgradeId, string name, int knowledgeSpent, int remainingKnowledgePoints, string archiveEntry)
    {
        if (string.IsNullOrWhiteSpace(archiveEntry))
        {
            throw new ArgumentException("Built upgrade needs an archive entry.", nameof(archiveEntry));
        }

        return new StartUpgradeResult(true, upgradeId, name, knowledgeSpent, remainingKnowledgePoints, archiveEntry, null);
    }

    public static StartUpgradeResult Rejected(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Rejected upgrade needs an error.", nameof(error));
        }

        return new StartUpgradeResult(false, null, null, 0, 0, null, error);
    }
}
}
