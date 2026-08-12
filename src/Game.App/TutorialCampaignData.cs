#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Newtonsoft.Json.Linq;

namespace Game.App
{

/// <summary>
/// A deterministic campaign composition for the curated tutorial. It may bind generated-runtime
/// relationships to fixed tutorial instances, while reusable location profiles remain neutral.
/// </summary>
public sealed class TutorialCampaignDefinition
{
    public TutorialCampaignDefinition(string id, IEnumerable<TutorialLocationRelationDefinition>? locationRelations)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new LocationDataException("Tutorial campaign id must not be empty.");
        Id = id.Trim();
        LocationRelations = (locationRelations ?? Enumerable.Empty<TutorialLocationRelationDefinition>()).ToList();
    }

    public string Id { get; }
    public IReadOnlyList<TutorialLocationRelationDefinition> LocationRelations { get; }
}

/// <summary>One generated-runtime faction relationship applied only when the tutorial world is assembled.</summary>
public sealed class TutorialLocationRelationDefinition
{
    public TutorialLocationRelationDefinition(string locationId, string factionId, LocationFactionRelationKind kind, IEnumerable<string>? contextTags)
    {
        if (string.IsNullOrWhiteSpace(locationId)) throw new LocationDataException("Tutorial location relation needs a locationId.");
        if (string.IsNullOrWhiteSpace(factionId)) throw new LocationDataException("Tutorial location relation needs a factionId.");
        LocationId = locationId.Trim();
        FactionId = factionId.Trim();
        Kind = kind;
        ContextTags = (contextTags ?? Enumerable.Empty<string>())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public string LocationId { get; }
    public string FactionId { get; }
    public LocationFactionRelationKind Kind { get; }
    public IReadOnlyList<string> ContextTags { get; }
}

public static class TutorialCampaignDataLoader
{
    private const int SchemaVersion = 1;
    private const string DocumentType = "tutorial-campaign";

    public static TutorialCampaignDefinition LoadFromJson(IEnumerable<string> documents, LocationDataBundle locations)
    {
        if (documents == null) throw new ArgumentNullException(nameof(documents));
        if (locations == null) throw new ArgumentNullException(nameof(locations));

        var items = new List<JObject>();
        foreach (var json in documents)
        {
            var root = JObject.Parse(json);
            if (!string.Equals((string?)root["documentType"], DocumentType, StringComparison.Ordinal))
            {
                throw new LocationDataException($"Tutorial document must use documentType '{DocumentType}'.");
            }
            if ((int?)root["schemaVersion"] != SchemaVersion)
            {
                throw new LocationDataException($"Tutorial campaign must use schemaVersion {SchemaVersion}.");
            }
            items.AddRange((root["items"] as JArray ?? new JArray()).OfType<JObject>());
        }

        if (items.Count != 1)
        {
            throw new LocationDataException("Game-data catalog requires exactly one tutorial campaign definition.");
        }

        var item = items[0];
        var id = (string?)item["id"];
        var relations = new List<TutorialLocationRelationDefinition>();
        foreach (var relation in (item["locationRelations"] as JArray ?? new JArray()).OfType<JObject>())
        {
            var kindText = (string?)relation["kind"];
            if (!Enum.TryParse<LocationFactionRelationKind>(kindText, true, out var kind))
            {
                throw new LocationDataException($"Tutorial relation on '{id}' has invalid kind '{kindText}'.");
            }
            relations.Add(new TutorialLocationRelationDefinition(
                (string?)relation["locationId"] ?? string.Empty,
                (string?)relation["factionId"] ?? string.Empty,
                kind,
                relation["contextTags"]?.Values<string?>().Where(tag => !string.IsNullOrWhiteSpace(tag))!.Select(tag => tag!)));
        }

        var definition = new TutorialCampaignDefinition(id ?? string.Empty, relations);
        Validate(definition, locations);
        return definition;
    }

    private static void Validate(TutorialCampaignDefinition definition, LocationDataBundle locations)
    {
        var locationIds = locations.Instances.Select(location => location.Id).ToHashSet(StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var relation in definition.LocationRelations)
        {
            if (!locationIds.Contains(relation.LocationId))
            {
                throw new LocationDataException($"Tutorial campaign '{definition.Id}' references unknown location '{relation.LocationId}'.");
            }
            if (!seen.Add($"{relation.LocationId}:{relation.FactionId}:{relation.Kind}"))
            {
                throw new LocationDataException($"Tutorial campaign '{definition.Id}' repeats relation '{relation.LocationId}:{relation.FactionId}:{relation.Kind}'.");
            }
        }
    }
}
}
