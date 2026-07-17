using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Game.App;

namespace Game.Simulation.Wpf;

/// <summary>Renders the shared visual catalog as an information-safe WPF placeholder card.</summary>
internal static class SceneVisualPresenter
{
    public static void Apply(
        Border container,
        TextBlock title,
        TextBlock reference,
        VisualAssetCatalog? catalog,
        string? requestedVisualId)
    {
        var definition = catalog?.Resolve(requestedVisualId);
        var resolvedId = definition?.VisualAssetId ?? VisualAssetCatalog.UnknownVisualAssetId;

        container.Background = new SolidColorBrush(PlaceholderColor(resolvedId));
        container.ToolTip = definition?.Description ?? "Neutraler visueller Platzhalter ohne verborgene Information.";
        title.Text = definition?.DisplayName ?? "Unbekannte Szene";

        var typeLabel = definition == null ? "Platzhalter" : TypeLabel(definition.Type);
        reference.Text = !string.IsNullOrWhiteSpace(requestedVisualId) && requestedVisualId != resolvedId
            ? $"{typeLabel} · Fallback für {requestedVisualId}"
            : $"{typeLabel} · {resolvedId}";
    }

    public static void Clear(Border container, TextBlock title, TextBlock reference)
    {
        container.Background = new SolidColorBrush(Color.FromRgb(42, 47, 55));
        container.ToolTip = "Noch keine Szene ausgewählt.";
        title.Text = "Keine Szene";
        reference.Text = "Kein Visual ausgewählt";
    }

    private static string TypeLabel(VisualAssetType type) => type switch
    {
        VisualAssetType.Portrait => "Porträt",
        VisualAssetType.FactionSymbol => "Fraktionszeichen",
        VisualAssetType.LocationImage => "Ortsbild",
        VisualAssetType.EventImage => "Ereignisbild",
        VisualAssetType.ObjectImage => "Objektbild",
        VisualAssetType.SymbolImage => "Symbolbild",
        VisualAssetType.ReportImage => "Berichtsbild",
        _ => "Platzhalter"
    };

    private static Color PlaceholderColor(string value)
    {
        var hash = 2166136261u;
        foreach (var character in value)
        {
            hash ^= character;
            hash *= 16777619u;
        }

        return Color.FromRgb(
            (byte)(42 + hash % 45),
            (byte)(48 + (hash >> 8) % 42),
            (byte)(43 + (hash >> 16) % 48));
    }
}
