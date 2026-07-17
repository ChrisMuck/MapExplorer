#nullable enable
using System;
using Game.Core;

namespace Game.App
{

/// <summary>Builds a memorial scene from the base's secured lost-expedition record only.</summary>
public sealed class ExpeditionMemorialSceneResolver
{
    private readonly SceneDescriptionCatalog scenes;
    private readonly SceneDescriptionResolver resolver;

    public ExpeditionMemorialSceneResolver(SceneDescriptionCatalog scenes)
    {
        this.scenes = scenes ?? throw new ArgumentNullException(nameof(scenes));
        resolver = new SceneDescriptionResolver(scenes);
    }

    public SceneDescriptionResult Resolve(LostExpeditionRecord record, string? locale = null)
    {
        if (record == null) throw new ArgumentNullException(nameof(record));
        var statusId = record.Status.ToString();
        var statusLabel = scenes.Texts.Resolve("scene.label.lost-status." + statusId, locale);
        var title = scenes.Texts.Resolve("scene.title.expedition-memorial", locale)
            .Replace("{expeditionNumber}", record.ExpeditionNumber.ToString(), StringComparison.Ordinal)
            .Replace("{statusLabel}", statusLabel, StringComparison.Ordinal);
        var subtitle = scenes.Texts.Resolve("scene.subtitle.expedition-memorial", locale)
            .Replace("{worldDay}", record.LastKnownWorldDay.ToString(), StringComparison.Ordinal);
        return resolver.ResolveExpeditionMemorial(new ExpeditionMemorialSceneView(
            record.ExpeditionId,
            title,
            subtitle,
            "placeholder-expedition-memorial",
            new[] { "memorial-recorded", "lost-status:" + statusId },
            locale));
    }
}

}
