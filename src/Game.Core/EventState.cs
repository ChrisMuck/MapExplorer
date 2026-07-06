#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class EventState
{
    private readonly List<EventOptionState> options;

    public EventState(
        string id,
        EventKind kind,
        string title,
        string source,
        string body,
        IEnumerable<EventOptionState> options,
        HexCoord? coord = null,
        string? factionId = null)
    {
        Id = RequireText(id, nameof(id));
        Kind = kind;
        Title = RequireText(title, nameof(title));
        Source = RequireText(source, nameof(source));
        Body = RequireText(body, nameof(body));
        this.options = new List<EventOptionState>(options ?? throw new ArgumentNullException(nameof(options)));
        if (this.options.Count == 0)
        {
            throw new ArgumentException("Event needs at least one option.", nameof(options));
        }

        Coord = coord;
        FactionId = string.IsNullOrWhiteSpace(factionId) ? null : factionId;
    }

    public string Id { get; }

    public EventKind Kind { get; }

    public string Title { get; }

    public string Source { get; }

    public string Body { get; }

    public HexCoord? Coord { get; }

    public string? FactionId { get; }

    public IReadOnlyList<EventOptionState> Options
    {
        get { return options; }
    }

    public bool IsResolved { get; private set; }

    public string? ResolvedOptionId { get; private set; }

    public void Resolve(string optionId)
    {
        if (IsResolved)
        {
            return;
        }

        if (FindOption(optionId) == null)
        {
            throw new ArgumentException("Unknown event option.", nameof(optionId));
        }

        IsResolved = true;
        ResolvedOptionId = optionId;
    }

    public EventOptionState? FindOption(string optionId)
    {
        foreach (var option in options)
        {
            if (option.Id == optionId)
            {
                return option;
            }
        }

        return null;
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
