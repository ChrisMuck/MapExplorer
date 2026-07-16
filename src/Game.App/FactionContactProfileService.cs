#nullable enable
using System;
using System.Linq;
using Game.Core;

namespace Game.App
{
public sealed class FactionContactProfileService
{
    private readonly SceneDescriptionCatalog scenes;
    private readonly FactionProfileDefinitionSet factionProfiles;

    public FactionContactProfileService(SceneDescriptionCatalog scenes, FactionProfileDefinitionSet factionProfiles)
    {
        this.scenes = scenes ?? throw new ArgumentNullException(nameof(scenes));
        this.factionProfiles = factionProfiles ?? throw new ArgumentNullException(nameof(factionProfiles));
    }

    public FactionContactAuthoredPresentation Resolve(FactionState faction, string? locale = null)
    {
        var contactStyle = factionProfiles.Find(faction.ReactionProfileId)?.ContactStyle;
        var profile = scenes.ContactProfiles.Values
            .Where(item => item.ContactStyle == contactStyle).OrderBy(item => item.Id, StringComparer.Ordinal).FirstOrDefault()
            ?? scenes.ContactProfiles.Values.Where(item => item.ContactStyle == null).OrderBy(item => item.Id, StringComparer.Ordinal).FirstOrDefault()
            ?? throw new LocationDataException("No fallback contact presentation profile is authored.");
        return new FactionContactAuthoredPresentation(profile.Role,
            scenes.Texts.Resolve(profile.RepresentativeNameTextId, locale),
            scenes.Texts.Resolve(profile.DescriptionTextId, locale), scenes.Texts.Resolve(profile.DialogueTextId, locale));
    }
}

public sealed record FactionContactAuthoredPresentation(FactionRepresentativeRole Role, string RepresentativeName,
    string Description, string Dialogue);
}
