#nullable enable
using System;

namespace Game.App
{

public sealed class PrepareSuppliesWithKnowledgeResult
{
    private PrepareSuppliesWithKnowledgeResult(bool success, int knowledgeSpent, int supplyBonus, int remainingKnowledgePoints, int pendingSupplyBonus, string? archiveEntry, string? error)
    {
        Success = success;
        KnowledgeSpent = knowledgeSpent;
        SupplyBonus = supplyBonus;
        RemainingKnowledgePoints = remainingKnowledgePoints;
        PendingSupplyBonus = pendingSupplyBonus;
        ArchiveEntry = archiveEntry;
        Error = error;
    }

    public bool Success { get; }

    public int KnowledgeSpent { get; }

    public int SupplyBonus { get; }

    public int RemainingKnowledgePoints { get; }

    public int PendingSupplyBonus { get; }

    public string? ArchiveEntry { get; }

    public string? Error { get; }

    public static PrepareSuppliesWithKnowledgeResult Prepared(int knowledgeSpent, int supplyBonus, int remainingKnowledgePoints, int pendingSupplyBonus, string archiveEntry)
    {
        if (string.IsNullOrWhiteSpace(archiveEntry))
        {
            throw new ArgumentException("Preparation needs an archive entry.", nameof(archiveEntry));
        }

        return new PrepareSuppliesWithKnowledgeResult(true, knowledgeSpent, supplyBonus, remainingKnowledgePoints, pendingSupplyBonus, archiveEntry, null);
    }

    public static PrepareSuppliesWithKnowledgeResult Rejected(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Rejected preparation needs an error.", nameof(error));
        }

        return new PrepareSuppliesWithKnowledgeResult(false, 0, 0, 0, 0, null, error);
    }
}
}
