using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{

public sealed class EventOptionState
{
    public EventOptionState(
        string id,
        string label,
        string resultText,
        EventOptionEffectKind effectKind = EventOptionEffectKind.None)
    {
        Id = RequireText(id, nameof(id));
        Label = RequireText(label, nameof(label));
        ResultText = RequireText(resultText, nameof(resultText));
        EffectKind = effectKind;
    }

    public string Id { get; }

    public string Label { get; }

    public string ResultText { get; }

    public EventOptionEffectKind EffectKind { get; }

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
