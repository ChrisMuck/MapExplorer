using System;

namespace Game.Core
{

/// <summary>A named member skill on a 0–5 scale (used by the base-camp person sheet).</summary>
public sealed class MemberSkill
{
    public MemberSkill(string name, int value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Skill name must not be empty.", nameof(name));
        }

        if (value < 0 || value > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Skill value must be between 0 and 5.");
        }

        Name = name;
        Value = value;
    }

    public string Name { get; }

    public int Value { get; }
}
}
