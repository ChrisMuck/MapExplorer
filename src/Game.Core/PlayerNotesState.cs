using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class PlayerNotesState
{
    private readonly List<PlayerMapMarkerState> markers = new();
    private readonly List<PlayerMapNoteState> notes = new();

    public IReadOnlyList<PlayerMapMarkerState> Markers
    {
        get { return markers; }
    }

    public IReadOnlyList<PlayerMapNoteState> Notes
    {
        get { return notes; }
    }

    public void AddMarker(PlayerMapMarkerState marker)
    {
        markers.Add(marker ?? throw new ArgumentNullException(nameof(marker)));
    }

    public void AddNote(PlayerMapNoteState note)
    {
        notes.Add(note ?? throw new ArgumentNullException(nameof(note)));
    }
}
}
