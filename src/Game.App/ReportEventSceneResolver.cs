#nullable enable
using System;
using System.Collections.Generic;
using Game.Core;

namespace Game.App
{

/// <summary>Projects only already-delivered event information into the shared scene resolver.</summary>
public sealed class ReportEventSceneResolver
{
    private readonly SceneDescriptionResolver resolver;

    public ReportEventSceneResolver(SceneDescriptionCatalog scenes)
    {
        resolver = new SceneDescriptionResolver(scenes ?? throw new ArgumentNullException(nameof(scenes)));
    }

    public SceneDescriptionResult Resolve(EventState eventState, string? locale = null)
    {
        if (eventState == null) throw new ArgumentNullException(nameof(eventState));
        var tags = new List<string>
        {
            "event-kind:" + eventState.Kind,
            "event-has-options"
        };
        if (eventState.Coord != null) tags.Add("event-location-known");
        return resolver.ResolveReportEvent(new ReportEventSceneView(
            eventState.Id,
            eventState.Title,
            eventState.Source,
            "placeholder-event-" + eventState.Kind,
            eventState.Body,
            tags,
            locale));
    }
}

}
