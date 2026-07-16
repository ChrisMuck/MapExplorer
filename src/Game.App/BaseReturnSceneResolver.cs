#nullable enable
using System;

namespace Game.App
{

/// <summary>Builds a base-arrival scene solely from the immutable completion result.</summary>
public sealed class BaseReturnSceneResolver
{
    private readonly SceneDescriptionCatalog scenes;
    private readonly SceneDescriptionResolver resolver;

    public BaseReturnSceneResolver(SceneDescriptionCatalog scenes)
    {
        this.scenes = scenes ?? throw new ArgumentNullException(nameof(scenes));
        resolver = new SceneDescriptionResolver(scenes);
    }

    public SceneDescriptionResult Resolve(CompleteExpeditionResult result, string? locale = null)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        if (!result.Success) throw new ArgumentException("A rejected completion has no return scene.", nameof(result));
        var title = scenes.Texts.Resolve("scene.title.base-return", locale)
            .Replace("{expeditionNumber}", result.ExpeditionNumber.ToString(), StringComparison.Ordinal);
        var subtitle = scenes.Texts.Resolve("scene.subtitle.base-return", locale)
            .Replace("{worldDay}", result.CompletionWorldDay.ToString(), StringComparison.Ordinal);
        return resolver.ResolveBaseReturn(new BaseReturnSceneView(
            "expedition-return:" + result.ExpeditionNumber,
            title,
            subtitle,
            "placeholder-base-return",
            result.TeamOutcome,
            result.ReturnedFindingsCount > 0,
            new[] { "base-return-delivered" },
            locale));
    }
}

}
