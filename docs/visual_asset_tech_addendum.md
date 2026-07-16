# VISUAL_ASSET_TECH_ADDENDUM.md

Technical addendum for portraits, placeholder images and discovery visuals.

Related documents:

- `technical_concept.md`
- `UI_UX_CONCEPT.md`
- `IMPLEMENTATION_PLAN.md`

---

## 1. Purpose

The game should show a portrait, scene image or object image whenever the player receives important communication or discovers something meaningful.

This document defines the technical support needed for that system.

Initial implementation should use placeholders and dummy images.

Final art can be added later without changing game logic.

Core rule:

> Important information should have a face, a place or an image.

---

## 2. Visual Asset Principles

Visual assets are presentation data.

They should not control simulation logic.

They may be referenced by game state, reports, events and archive entries, but they must not determine outcomes.

Visual assets must not reveal hidden `WorldState` truth.

Example:

- a sealed gate event may show an exterior gate image
- it must not show the dangerous thing behind the gate unless discovered

---

## 3. Asset Folder Structure

Recommended folder structure:

```text
UnityProject/Assets/ExpeditionGame/Art/
  placeholders/
    portraits/
    factions/
    locations/
    events/
    objects/
    symbols/
  portraits/
  factions/
  locations/
  events/
  objects/
  symbols/
```

Content definitions may reference assets by stable ID, not by hardcoded UI path whenever possible.

---

## 4. VisualAssetDefinition

Add a visual asset definition model.

```csharp
public sealed class VisualAssetDefinition
{
    public string VisualAssetId { get; set; }
    public VisualAssetType Type { get; set; }

    public string DisplayName { get; set; }
    public string AssetPath { get; set; }

    public bool IsPlaceholder { get; set; }
    public string? Description { get; set; }

    public List<string> Tags { get; set; }
}
```

```csharp
public enum VisualAssetType
{
    Portrait,
    FactionSymbol,
    LocationImage,
    EventImage,
    ObjectImage,
    SymbolImage,
    ReportImage,
    UnknownPlaceholder
}
```

---

## 5. Visual Asset References

Runtime objects should reference visuals by ID.

Add optional fields where appropriate:

```csharp
public string? VisualAssetId { get; set; }
```

For multiple visuals:

```csharp
public List<string> VisualAssetIds { get; set; }
```

---

## 6. Recommended Model Additions

### ExpeditionMemberState

```csharp
public string? PortraitAssetId { get; set; }
```

Used for scout reports, member lists, injury reports and base screens.

### ScoutReportState

```csharp
public List<string> SpeakerMemberIds { get; set; }
public List<string> VisualAssetIds { get; set; }
```

Usually references the returning scout portrait.

### FactionState or FactionDefinition

```csharp
public string? SymbolAssetId { get; set; }
public string? DefaultRepresentativeAssetId { get; set; }
```

### Faction Contact / Interaction Event

For specific faction conversations, the event or interaction should reference the actual speaker image:

```csharp
public string? SpeakerName { get; set; }
public string? SpeakerRole { get; set; }
public string? SpeakerPortraitAssetId { get; set; }
```

### SpecialLocationState / Definition

```csharp
public string? LocationImageAssetId { get; set; }
```

### EventDefinition / EventState

```csharp
public string? PrimaryVisualAssetId { get; set; }
public List<string> AdditionalVisualAssetIds { get; set; }
```

### ArchiveEntryState

```csharp
public string? ThumbnailAssetId { get; set; }
public List<string> VisualAssetIds { get; set; }
```

---

## 7. Fallback Rules

The UI should always have a fallback visual.

Fallbacks:

```text
unknown_person_placeholder
unknown_location_placeholder
unknown_object_placeholder
unknown_faction_placeholder
unknown_report_placeholder
unknown_symbol_placeholder
```

If a specific asset is missing, UI should show a fallback and log a warning.

It should not crash.

---

## 8. Placeholder Requirements for MVP

Minimum placeholder set:

```text
portrait_scout_mara_placeholder
portrait_scout_jonas_placeholder
portrait_scout_generic_placeholder
portrait_coastal_messenger_placeholder
portrait_border_warden_placeholder
portrait_unknown_person_placeholder

location_marked_grave_placeholder
location_ravine_bridge_placeholder
location_abandoned_camp_placeholder
location_sealed_gate_placeholder
location_unknown_placeholder

object_sealed_box_placeholder
object_old_map_placeholder
object_journal_placeholder
object_unknown_placeholder

symbol_warning_posts_placeholder
symbol_unknown_placeholder
```

These can be simple dummy images.

They should be visually consistent enough that the UI feels intentional.

---

## 9. Interaction Popup Data

A communication popup should receive a view model such as:

```csharp
public sealed class CommunicationViewModel
{
    public string Title { get; set; }

    public string? SpeakerName { get; set; }
    public string? SpeakerRole { get; set; }
    public string? FactionName { get; set; }

    public string PrimaryText { get; set; }

    public string? PrimaryVisualAssetId { get; set; }

    public List<CommunicationOptionViewModel> Options { get; set; }
}
```

```csharp
public sealed class CommunicationOptionViewModel
{
    public string OptionId { get; set; }
    public string Text { get; set; }

    public bool IsAvailable { get; set; }
    public string? UnavailableReason { get; set; }
}
```

This view model is presentation-facing.

It should be created from game state and event state.

It should not own simulation logic.

---

## 10. Archive Card Visuals

Archive cards should support thumbnails.

Example view model:

```csharp
public sealed class ArchiveCardViewModel
{
    public string ArchiveEntryId { get; set; }

    public string Title { get; set; }
    public string TypeLabel { get; set; }
    public string Summary { get; set; }

    public string? ThumbnailAssetId { get; set; }

    public int CreatedWorldDay { get; set; }
    public ReliabilityLevel Reliability { get; set; }
}
```

---

## 11. Implementation Notes

The first version should not over-engineer art loading.

Suggested MVP approach:

1. Add `VisualAssetDefinition`.
2. Create a small JSON file listing placeholder assets.
3. Add `PortraitAssetId` to expedition members.
4. Add `PrimaryVisualAssetId` to events.
5. Add `LocationImageAssetId` to special locations.
6. Add `ThumbnailAssetId` to archive entries.
7. Build one reusable communication popup scene.
8. Build one reusable discovery card scene.
9. Build one reusable archive card component.

Do not generate final images as part of core implementation.

Use placeholders first.

---

## 12. Testing

Core tests should verify:

- missing visual asset falls back safely
- event state can reference visual asset ID
- scout report can reference scout portrait
- archive entry can reference thumbnail
- visual asset references serialize/deserialize correctly

Visual correctness is manual/UI review.

---

## 13. Current Decision

The MVP should show:

- scout portrait in scout reports
- faction/person portrait in faction interactions
- scene/object image in event popups
- location image in discovery cards
- thumbnail image in archive cards

This makes the game more personal and more memorable.

### Implementation status

The MVP foundation is implemented as presentation-only data:

- `VisualAssetDefinition` and its catalog are loaded from the manifested
  `Visuals/visual-assets.json` game-data document.
- Stable IDs resolve centrally. Unknown IDs resolve to `unknown_visual_placeholder`; a missing
  Unity `Resources` sprite keeps the generated placeholder and emits one warning.
- The reusable Unity presenter is used by location, event, contact, scout-return, base-return and
  memorial scenes. Controllers do not select paths or branch on concrete faction/location IDs.
- Current entries intentionally have no `assetPath`. Final or interim sprites can be introduced by
  adding a Resources-relative path to the JSON entry, without changing simulation or scene code.
- WPF resolves the same definitions into deterministic placeholder cards for location commands,
  scout returns, active contacts, events, base returns and memorials. It remains a development
  client and does not depend on Unity assemblies or Unity resource paths.
