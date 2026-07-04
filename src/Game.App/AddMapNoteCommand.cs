using System;
using Game.Core;

namespace Game.App
{

public sealed class AddMapNoteCommand
{
    public MapAnnotationResult Execute(GameState game, HexCoord coord, string text)
    {
        if (game == null)
        {
            throw new ArgumentNullException(nameof(game));
        }

        if (!game.World.Map.Contains(coord))
        {
            return MapAnnotationResult.Rejected("Note coordinate is outside the map.");
        }

        if (game.Knowledge.GetTileKnowledge(coord) == KnowledgeLevel.Unknown)
        {
            return MapAnnotationResult.Rejected("Cannot write a note for completely unknown territory yet.");
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return MapAnnotationResult.Rejected("Note text must not be empty.");
        }

        var id = $"note-{game.PlayerNotes.Notes.Count + 1}";
        game.PlayerNotes.AddNote(new PlayerMapNoteState(id, coord, text.Trim()));
        return MapAnnotationResult.Added(id);
    }
}
}
