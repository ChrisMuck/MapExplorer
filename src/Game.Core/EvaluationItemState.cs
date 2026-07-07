#nullable enable
using System;

namespace Game.Core
{

/// <summary>
/// A field discovery being analysed at the base. It matures over base days (up to the evaluator
/// capacity), becomes ready, and is then manually evaluated into an archived insight plus bonus
/// Knowledge Points.
/// </summary>
public sealed class EvaluationItemState
{
    public EvaluationItemState(
        string id,
        string name,
        string source,
        int requiredDays,
        int knowledgeReward,
        string insightText,
        int progressDays = 0,
        bool isEvaluated = false)
    {
        Id = RequireText(id, nameof(id));
        Name = RequireText(name, nameof(name));
        Source = source ?? string.Empty;
        if (requiredDays < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredDays), requiredDays, "Required days must not be negative.");
        }

        if (knowledgeReward < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(knowledgeReward), knowledgeReward, "Knowledge reward must not be negative.");
        }

        RequiredDays = requiredDays;
        KnowledgeReward = knowledgeReward;
        InsightText = insightText ?? string.Empty;
        ProgressDays = Math.Max(0, Math.Min(progressDays, requiredDays));
        IsEvaluated = isEvaluated;
    }

    public string Id { get; }

    public string Name { get; }

    public string Source { get; }

    public int RequiredDays { get; }

    public int KnowledgeReward { get; }

    public string InsightText { get; }

    public int ProgressDays { get; private set; }

    public bool IsEvaluated { get; private set; }

    public bool IsReady => !IsEvaluated && ProgressDays >= RequiredDays;

    public void AddProgress(int days)
    {
        if (days < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(days), days, "Progress must not be negative.");
        }

        ProgressDays = Math.Min(RequiredDays, ProgressDays + days);
    }

    public void MarkEvaluated()
    {
        IsEvaluated = true;
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
