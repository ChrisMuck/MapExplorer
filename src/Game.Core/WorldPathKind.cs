using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public enum WorldPathKind
{
    River,
    Road,
    TerritoryBorder,
    Wall,
    /// <summary>Visible ravine or ditch line. Blocking stays on the edge location; the path is only the line a map draws.</summary>
    Chasm
}
}
