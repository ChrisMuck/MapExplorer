#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;

namespace Game.App
{

/// <summary>
/// Records and presents only impressions earned by a normal location inspection. Objective context,
/// hidden modifiers and unidentified faction relations never pass through to presentation.
/// </summary>
public sealed class LocationInspectionPresentationResolver
{
    private const string ObservedModifierPrefix = "inspection-modifier:";
    private const string ObservedRelationPrefix = "inspection-relation:";
    private const string IdentifiedFactionPrefix = "inspection-faction:";
    private readonly LocationInteractionDefinitionSet definitions;
    private readonly CrossSystemAuthoringBundle? authoring;
    private readonly SceneDescriptionResolver? sceneResolver;

    public LocationInspectionPresentationResolver(LocationInteractionDefinitionSet definitions, CrossSystemAuthoringBundle? authoring = null,
        SceneDescriptionCatalog? scenes = null)
    {
        this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        this.authoring = authoring;
        sceneResolver = scenes == null ? null : new SceneDescriptionResolver(scenes);
    }

    public LocationInspectionPresentation? ObserveAndResolve(GameState game, SpecialLocationState location,
        LocationConditionKnowledgeState? previousKnownCondition = null, string? locale = null)
    {
        if (game == null) throw new ArgumentNullException(nameof(game));
        if (location == null) throw new ArgumentNullException(nameof(location));
        var profile = ResolveContentProfile(location);
        if (profile == null) return null;

        var impressions = new List<string>();
        var observableModifiers = new List<ObservableModifierScenePart>();
        var observableRelationKinds = new HashSet<LocationFactionRelationKind>();
        AddDistinct(impressions, profile.FlavorForState(location.OperationalStateId) ?? profile.Description ?? profile.ShortDescription);

        foreach (var modifierId in location.ModifierIds.OrderBy(id => id, StringComparer.Ordinal))
        {
            if (!definitions.Modifiers.TryGetValue(modifierId, out var modifier) ||
                !modifier.AppliesTo(location) || string.IsNullOrWhiteSpace(modifier.InspectionText)) continue;
            game.Knowledge.LearnLocationContextTag(location.Id, ObservedModifierPrefix + modifier.Id);
            AddDistinct(impressions, modifier.InspectionText);
            observableModifiers.Add(new ObservableModifierScenePart(modifier.Id, modifier.InspectionText));
            foreach (var relationKind in modifier.RevealedFactionRelationKinds) observableRelationKinds.Add(relationKind);
        }

        var relation = location.FactionRelations
            .Where(item => observableRelationKinds.Contains(item.Kind))
            .OrderBy(item => item.Kind)
            .ThenBy(item => item.FactionId, StringComparer.Ordinal)
            .FirstOrDefault();
        if (relation != null)
        {
            game.Knowledge.LearnLocationContextTag(location.Id, ObservedRelationPrefix + relation.Kind.ToString().ToLowerInvariant());
            var faction = game.FindFaction(relation.FactionId);
            var identified = faction != null && faction.ContactStatus is FactionContactStatus.Contacted or FactionContactStatus.Open or FactionContactStatus.Hostile;
            if (identified)
            {
                game.Knowledge.LearnLocationContextTag(location.Id, IdentifiedFactionPrefix + relation.FactionId);
            }
            AddDistinct(impressions, RelationImpression(relation.Kind, identified ? faction!.Name : null));
        }

        SceneDescriptionResult? scene = null;
        if (sceneResolver != null && !string.IsNullOrWhiteSpace(location.ArchetypeId) && !string.IsNullOrWhiteSpace(location.VariantId))
        {
            var identifiedFaction = relation == null ? null : game.FindFaction(relation.FactionId);
            var identityStage = identifiedFaction != null && identifiedFaction.ContactStatus is FactionContactStatus.Contacted or FactionContactStatus.Open or FactionContactStatus.Hostile
                ? "identified" : "anonymous";
            scene = sceneResolver.ResolveLocation(new LocationSceneView(
                location.Id, SceneTrigger.InspectionResult, location.ArchetypeId!, location.VariantId!, profile.Title,
                profile.Subtitle, profile.ImageId, location.InteractionStateId, location.OperationalStateId,
                location.PresenceStateId, game.World.WorldDay, previousKnownCondition,
                game.Knowledge.GetTileKnowledge(location.Coord), game.Knowledge.KnownLocationContextTags(location.Id),
                observableModifiers, observableRelationKinds.Select(kind => kind.ToString().ToLowerInvariant()), identityStage,
                identifiedFaction?.ContactStatus.ToString() ?? FactionContactStatus.Unknown.ToString(),
                identityStage == "identified" ? identifiedFaction!.Name : null, null, locale));
        }

        var message = scene?.Message ?? (impressions.Count == 0
            ? profile.ShortDescription ?? $"{profile.Title} wurde dokumentiert."
            : string.Join(" ", impressions));
        return new LocationInspectionPresentation(profile.Title, message, profile.JournalDiscovered ?? message, scene);
    }

    private LocationContentProfileDefinition? ResolveContentProfile(SpecialLocationState location)
    {
        var direct = definitions.FindContentProfile(location.ContentProfileId);
        if (direct != null) return direct;
        if (authoring == null || string.IsNullOrWhiteSpace(location.ArchetypeId) || string.IsNullOrWhiteSpace(location.VariantId)) return null;
        var profileId = authoring.ScenarioProfiles.Values
            .Where(candidate => candidate.ArchetypeId == location.ArchetypeId && candidate.VariantId == location.VariantId)
            .OrderBy(candidate => candidate.Id, StringComparer.Ordinal)
            .Select(candidate => candidate.ContentProfileId)
            .FirstOrDefault();
        return definitions.FindContentProfile(profileId);
    }

    private static string RelationImpression(LocationFactionRelationKind kind, string? factionName)
    {
        if (factionName == null)
        {
            return kind switch
            {
                LocationFactionRelationKind.Claimed => "Zeichen deuten darauf hin, dass eine unbekannte Gruppe Anspruch auf diesen Ort erhebt.",
                LocationFactionRelationKind.Watched => "Wer diesen Ort beobachtet, laesst sich noch nicht erkennen.",
                LocationFactionRelationKind.Sacred => "Die sichtbare Bedeutung des Ortes scheint mit einer noch unbekannten Gruppe verbunden zu sein.",
                LocationFactionRelationKind.Guarded => "Wer den Ort bewacht, laesst sich noch nicht erkennen.",
                _ => "Die erkennbare Verbindung zu einer Gruppe ist noch nicht geklaert."
            };
        }

        return kind switch
        {
            LocationFactionRelationKind.Claimed => $"Die sichtbaren Zeichen werden {factionName} zugeordnet; die Fraktion erhebt offenbar Anspruch auf diesen Ort.",
            LocationFactionRelationKind.Watched => $"Die sichtbaren Beobachtungsspuren werden {factionName} zugeordnet.",
            LocationFactionRelationKind.Sacred => $"Die sichtbare Bedeutung des Ortes wird mit {factionName} verbunden.",
            LocationFactionRelationKind.Guarded => $"Die sichtbaren Spuren der Bewachung werden {factionName} zugeordnet.",
            _ => $"Die sichtbare Verbindung wird {factionName} zugeordnet."
        };
    }

    private static void AddDistinct(ICollection<string> target, string? text)
    {
        if (!string.IsNullOrWhiteSpace(text) && !target.Contains(text.Trim())) target.Add(text.Trim());
    }
}

public sealed class LocationInspectionPresentation
{
    public LocationInspectionPresentation(string title, string message, string journalText, SceneDescriptionResult? scene = null)
    {
        Title = RequireText(title, nameof(title));
        Message = RequireText(message, nameof(message));
        JournalText = RequireText(journalText, nameof(journalText));
        Scene = scene;
    }

    public string Title { get; }
    public string Message { get; }
    public string JournalText { get; }
    public SceneDescriptionResult? Scene { get; }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value must not be empty.", name);
        return value.Trim();
    }
}
}
