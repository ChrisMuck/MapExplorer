#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

/// <summary>
/// The base's knowledge-evaluation queue. A limited number of evaluators (capacity) advance the
/// earliest unfinished items each base day; finished items become ready to evaluate into insights.
/// </summary>
public sealed class EvaluationQueueState
{
    private readonly List<EvaluationItemState> items = new();

    public EvaluationQueueState(int evaluatorCapacity = 2)
    {
        EvaluatorCapacity = evaluatorCapacity < 1 ? 1 : evaluatorCapacity;
    }

    public EvaluationQueueState(IEnumerable<EvaluationItemState> initialItems, int evaluatorCapacity = 2)
        : this(evaluatorCapacity)
    {
        items.AddRange(initialItems ?? throw new ArgumentNullException(nameof(initialItems)));
    }

    public int EvaluatorCapacity { get; }

    public IReadOnlyList<EvaluationItemState> Items => items;

    /// <summary>Items currently occupying an evaluator slot (not yet ready, not evaluated).</summary>
    public int ActiveCount => Math.Min(EvaluatorCapacity, items.Count(item => !item.IsEvaluated && !item.IsReady));

    public EvaluationItemState? FindItem(string id)
    {
        return items.FirstOrDefault(item => item.Id == id);
    }

    public void Add(EvaluationItemState item)
    {
        items.Add(item ?? throw new ArgumentNullException(nameof(item)));
    }

    /// <summary>
    /// Advances progress by the given number of base days. Each day the earliest unfinished items
    /// (up to the evaluator capacity) gain one day of progress.
    /// </summary>
    public void AdvanceDays(int days)
    {
        if (days < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(days), days, "Days must not be negative.");
        }

        for (var day = 0; day < days; day++)
        {
            var slots = EvaluatorCapacity;
            foreach (var item in items)
            {
                if (slots <= 0)
                {
                    break;
                }

                if (item.IsEvaluated || item.IsReady)
                {
                    continue;
                }

                item.AddProgress(1);
                slots--;
            }
        }
    }
}
}
