#nullable enable
using System;
using Game.App;

internal sealed class SceneDescriptionDataTests
{
    public void RunAll()
    {
        GroupDefaultsExpandIntoEffectiveFragments();
        MissingDefaultLocaleTextIsRejected();
        UnknownConditionFieldIsRejected();
        TranslationsMustPreservePlaceholderContracts();
    }

    private static void GroupDefaultsExpandIntoEffectiveFragments()
    {
        var catalog = SceneDescriptionDataLoader.LoadFromJson(new[]
        {
            Fragments("""
              "defaultsByGroup": {
                "opening": { "subjectKind": "location", "source": { "kind": "direct-observation" }, "priority": 80 }
              },
              "items": [
                { "id": "frag-test-opening", "group": "opening", "textId": "scene.test.opening" }
              ]
            """),
            German("scene.test.opening", "Ein stiller Ort liegt vor der Expedition.")
        });

        var fragment = catalog.Fragments["frag-test-opening"];
        AssertEqual("location", fragment.SubjectKind, "subjectKind expands from group defaults");
        AssertEqual("direct-observation", fragment.Source.Kind, "source expands from group defaults");
        AssertEqual(80, fragment.Priority, "priority expands from group defaults");
        AssertEqual("always", fragment.Repetition, "universal repetition default is explicit after parsing");
        AssertEqual("Ein stiller Ort liegt vor der Expedition.", catalog.Texts.Resolve(fragment.TextId, "de-DE"), "regional German resolves through language fallback");
        AssertEqual("Ein stiller Ort liegt vor der Expedition.", catalog.Texts.Resolve(fragment.TextId, "en"), "missing language resolves through default fallback");
    }

    private static void MissingDefaultLocaleTextIsRejected()
    {
        AssertThrows(() => SceneDescriptionDataLoader.LoadFromJson(new[]
        {
            Fragments("""
              "items": [
                { "id": "frag-missing-text", "group": "opening", "subjectKind": "location", "source": { "kind": "direct-observation" }, "priority": 80, "textId": "scene.missing" }
              ]
            """),
            German("scene.other", "Ein anderer Text.")
        }), "fragments cannot reference absent German/default text");
    }

    private static void UnknownConditionFieldIsRejected()
    {
        AssertThrows(() => SceneDescriptionDataLoader.LoadFromJson(new[]
        {
            Fragments("""
              "items": [
                { "id": "frag-hidden-truth", "group": "opening", "subjectKind": "location", "source": { "kind": "direct-observation" }, "priority": 80, "when": { "hiddenFactionId": "faction-one" }, "textId": "scene.test" }
              ]
            """),
            German("scene.test", "Etwas ist zu sehen.")
        }), "unknown conditions cannot silently become hidden-truth predicates");
    }

    private static void TranslationsMustPreservePlaceholderContracts()
    {
        var fragments = Fragments("""
          "items": [
            { "id": "frag-scout-return", "group": "scout-return", "subjectKind": "scout-return", "source": { "kind": "delivered-outcome" }, "priority": 70, "textId": "scene.scout.return" }
          ]
        """);
        var german = German("scene.scout.return", "{memberName} kehrt zurück.");
        var english = """
        {
          "documentType": "scene-localization",
          "schemaVersion": 1,
          "contentVersion": 1,
          "locale": "en",
          "fallbackLocale": "de",
          "isDefault": false,
          "items": [{ "id": "scene.scout.return", "text": "The scout returns." }]
        }
        """;

        AssertThrows(() => SceneDescriptionDataLoader.LoadFromJson(new[] { fragments, german, english }),
            "translations cannot remove runtime placeholders");
    }

    private static string Fragments(string body) => $$"""
    {
      "documentType": "scene-fragments",
      "schemaVersion": 1,
      "contentVersion": 1,
      {{body}}
    }
    """;

    private static string German(string id, string text) => $$"""
    {
      "documentType": "scene-localization",
      "schemaVersion": 1,
      "contentVersion": 1,
      "locale": "de",
      "fallbackLocale": null,
      "isDefault": true,
      "items": [{ "id": "{{id}}", "text": "{{text}}" }]
    }
    """;

    private static void AssertThrows(Action action, string message)
    {
        try { action(); }
        catch (LocationDataException) { return; }
        throw new InvalidOperationException($"{message}: expected {nameof(LocationDataException)}.");
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!Equals(expected, actual)) throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
    }
}
