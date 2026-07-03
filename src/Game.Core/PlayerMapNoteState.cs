using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class PlayerMapNoteState
{
    public PlayerMapNoteState(string id, HexCoord coord, string text)
    {
        Id = RequireText(id, nameof(id));
        Coord = coord;
        Text = RequireText(text, nameof(text));
    }

    public string Id { get; }

    public HexCoord Coord { get; }

    public string Text { get; }

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
