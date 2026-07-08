# UI_UX_CONCEPT.md

UI and UX concept for the Expedition Exploration Game.

Related documents:

- `exploration_game_concept.md`
- `technical_concept.md`
- `IMPLEMENTATION_PLAN.md`
- `AGENTS.md`

Status: **Initial UI Direction / Living Document**

---

## 1. Purpose

The current prototype UI is useful for development, but it should not accidentally become the final direction.

The UI must support the core fantasy:

> The player leads an expedition into an unknown world, reads uncertain information, marks the map, decides when to push forward and when to return.

The UI should make the player feel like they are managing an expedition map, field reports, warnings and decisions.

It should not feel like a debug dashboard.

---

## 2. Design Goals

The UI should be:

- readable
- calm
- map-first
- information-rich without being noisy
- good for long play sessions
- clear about uncertainty
- good for comparing reports, notes and confirmed knowledge
- visually grounded in exploration, cartography and expedition planning

The UI should not feel like:

- a mobile game
- a 4X empire-management UI
- a survival crafting inventory
- a generic fantasy RPG menu
- a spreadsheet with graphics
- a developer/debug tool

Core UI rule:

> The map is the main stage. Reports, notes and decisions orbit around it.

---

## 3. Current Prototype Assessment

The current prototype is good because it already shows:

- central hex map
- confirmed and unknown hexes
- expedition marker
- selected hex panel
- scout controls
- status panel
- report area
- end day button

This is enough for early development.

However, it currently feels like a developer prototype because:

- panels are isolated in screen corners
- the map is small compared to empty screen space
- typography is small and utilitarian
- information hierarchy is weak
- scout controls are squeezed into the selected-hex panel
- status information is separated from the action flow
- the right report panel is mostly empty space
- there is no clear expedition command area
- the UI does not yet express mystery, cartography or expedition planning

The next iteration should focus on structure, not beauty.

---

## 4. Overall Layout

Recommended desktop layout:

```text
+--------------------------------------------------------------------------------+
| Top Bar: Expedition Day | World Day | Supplies | Morale | MP | Alerts          |
+----------------------+-----------------------------------------+---------------+
| Left Context Panel   |                                         | Right Panel   |
| Selected Hex         |                                         | Expedition    |
| Location Info        |              Hex Map                    | Scouts        |
| Faction Signs        |                                         | Reports       |
| Notes/Markers        |                                         | Journal       |
|                      |                                         | Archive       |
+----------------------+-----------------------------------------+---------------+
| Bottom Action Bar: Move | Scout | Inspect | Note | Camp | Return | End Day     |
+--------------------------------------------------------------------------------+
```

The map should occupy most of the screen.

Panels should support the map, not dominate it.

---

## 5. Top Bar

The top bar shows global state at a glance.

Recommended contents:

- Expedition Day
- World Day
- current mode: Expedition / Base
- Supplies
- Medicine
- Morale
- Movement Points
- party size
- alerts

Example:

```text
Expedition Day 7    World Day 12    Supplies 42    Medicine 4    Morale Steady    MP 2/4
```

Alerts may include:

- Scout overdue
- Supplies low
- Member injured
- New report
- Faction warning
- Event pending

The top bar is status only. It should not contain detailed controls.

---

## 6. Left Context Panel

The left panel shows information about the selected hex, location or marker.

For a selected known hex, show:

- coordinate
- knowledge state
- biome if known
- movement cost if known
- reliability
- last confirmed/reported day
- visible markers
- player notes
- linked scout reports
- linked location or faction clues

Example:

```text
Hex 14 / 08

Knowledge: Reported
Reliability: Medium
Last report: Expedition 1, Day 5

Reported signs:
- Three carved posts
- Smoke farther east

Player note:
"Possible Border Warden boundary. Avoid with large group."
```

For unknown hexes, show very little:

```text
Unknown territory

No confirmed information.

Possible actions:
- Send scout in this direction
```

Do not show hidden biome, faction or location data.

For special locations, show:

- visible name
- location state
- known clues
- linked reports
- known faction relevance
- player notes
- available actions

Do not reveal hidden purpose unless discovered.

---

## 7. Right Panel

The right panel should become tab-based or section-based.

Recommended tabs:

- Expedition
- Scouts
- Reports
- Journal
- Archive

For MVP these can be simple stacked panels, but the target direction should be tabbed or collapsible.

### Expedition Tab

Show:

- party members
- roles
- condition
- star level
- unavailable/on mission status
- carried resources
- active projects

Example:

```text
Mara      Scout      ★      On mission
Jonas     Scout      ★      Ready
Bren      Soldier    -      Healthy
Elin      Medic      -      Healthy
```

### Scouts Tab

Show:

- scouts ready
- active scout missions
- overdue scouts
- send scout controls

Scout mission example:

```text
Mara
Direction: Northeast
Focus: Faction traces
Behavior: Cautious
Expected back: Expedition Day 6
Status: On mission
```

Scout controls should eventually live here, not inside the selected-hex panel.

### Reports Tab

Show newest scout reports and important unread reports.

Each report card should show:

- title
- scout name
- day
- reliability
- short excerpt
- open button
- create markers from hints

### Journal Tab

Chronological expedition log.

### Archive Tab

Persistent knowledge.

For MVP, this can be simple. Later it should support filters for reports, locations, factions, warnings, symbols and old expeditions.

---

## 8. Bottom Action Bar

The bottom bar contains player actions relevant to the current context.

Possible actions:

- Move
- Inspect
- Send Scout
- Add Marker
- Add Note
- Camp
- Start Project
- Trade
- Ask About Warning
- Return to Base
- End Day

The bottom bar should be context-sensitive.

If an action is unavailable, explain why without revealing hidden truth.

Bad:

```text
Cannot enter: hidden faction lethal zone.
```

Good:

```text
Cannot enter safely: warning signs and poor visibility.
```

---

## 9. Map Presentation

The map is the core of the game.

Hexes should clearly distinguish:

- unknown
- reported
- confirmed
- old
- doubtful
- selected
- reachable this day
- current expedition position
- player marker
- known special location
- suspected warning area

Unknown hexes should not simply be pure black. They should feel like unexplored map space.

Possible treatments:

- darkened terrain if approximately known
- blank dark hex if unknown
- soft fog overlay
- sketched outline for reported but unconfirmed area
- faded/desaturated for old knowledge

Reachable hexes should be highlighted based on:

- remaining movement points
- terrain costs
- blocked areas
- known routes

Do not highlight hidden facts unless known.

Markers must be readable at map scale.

Marker categories should include:

- danger
- warning
- possible border
- scout missing
- ruin
- investigate later
- safe route
- sealed place
- faction contact
- custom note

---

## 10. Scout Report UI

Scout reports are central.

They should feel like expedition field reports, not system messages.

Report detail view should include:

- title
- scout name(s)
- day sent / day returned
- duration
- direction
- focus
- behavior
- reliability
- narrative report text
- structured hints
- related hexes
- create marker buttons
- add note button
- archive button

Example:

```text
Report: Smoke Beyond the Ridge
Scout: Mara
Returned: Expedition Day 5
Reliability: Medium

Mara followed the river northeast for two days. Beyond the second ridge she found three carved posts...
```

Hints:

```text
Hint 1: Possible border marker near Hex 14/08
Hint 2: Smoke seen farther east
Hint 3: Scout recommends caution
```

The player may turn hints into markers.

The UI should not automatically interpret all hints.

---

## 11. Event UI

Events should appear as focused decision panels.

They should not feel like pop-up spam.

Event panel contents:

- event title
- atmospheric description
- relevant location/hex if known
- options
- role-based availability
- consequences shown only when reasonably knowable

Example:

```text
A Sealed Box

Between the roots of a fallen tree lies a small wooden box. The ground is wet, but the box is dry. A carved symbol marks the lid.

Options:
- Open it
- Leave it
- Mark the location
- Ask scholar to inspect it
- Take it to base
```

If an option is unavailable, show why:

```text
Scholar required
```

Do not reveal hidden outcomes.

---

## 12. Base Screen

The base screen should feel different from the expedition map, but still connected to it.

Base screen priorities:

- expedition summary
- recovered reports
- injured members
- archived knowledge
- available recruits
- base actions
- time passing
- next expedition preparation

Suggested layout:

```text
+--------------------------------------------------------------------------------+
| Base: Expedition Center                                      World Day 18       |
+----------------------+---------------------------+-----------------------------+
| Expedition Summary   | Archive / Analysis        | Personnel                   |
| What happened        | Reports, clues, maps      | Ready / injured / recruit   |
+----------------------+---------------------------+-----------------------------+
| Base Actions         | Next Expedition Setup                                   |
+--------------------------------------------------------------------------------+
```

Base phase should clearly show that time passes.

Example:

```text
12 days pass while reports are archived and the wounded recover.

New information:
- Coastal People report smoke in the interior.
- Border markers near the ravine were renewed.
```

### 12.1 Current Base Camp Screen

The current prototype uses a separate full-screen UI Toolkit document for Base Camp. It opens when an
expedition has returned to the base and sits above the map UI.

Current tabs:

- Team: roster pool, selected team, person detail, healing and base recruiting actions.
- Aufbruch: selected core team, porter/soldier support units, rations, medicine, readiness preview
  and start-next-expedition action.
- Basis ausbauen: Knowledge Point upgrade cards with built, available and locked states.
- Wissen auswerten: evaluation queue, progress, ready insights and evaluation action.
- Fraktionen: current faction contact notes and attitude summary.
- Archiv: typed archive entries with filters and search.

UI rules for Base Camp:

- It is a planning and memory screen, not a city-management dashboard.
- It should make the next expedition easier to plan from what the previous expedition learned.
- Base time must remain visible whenever actions consume days.
- Important results should become archive/report entries instead of disappearing into transient UI.
- Start-next-expedition readiness must be readable before departure: team size, carry capacity,
  rations, medicine, defense and overload/slow-march warnings.
- The screen may be visually richer than the map HUD, but it must stay fast to scan and must not
  hide mandatory actions inside decorative panels.

---

## 13. UI Modes

The MVP should support these conceptual UI modes:

- Expedition Mode
- Report Reading Mode
- Map Annotation Mode
- Base Mode

The implementation may be simple, but the modes should not be mixed into one confusing screen.

---

## 14. Information Honesty

The UI must be honest about uncertainty.

Use terms such as:

- Unknown
- Reported
- Confirmed
- Old
- Doubtful
- Lost
- Reliability: Low / Medium / High / Confirmed

Do not show hidden truth in tooltips, debug panels or map colors.

If debug overlays are needed, they must be clearly developer-only.

---

## 15. Input Model

Initial MVP input should support:

- left click: select hex
- right click or button: context action
- mouse wheel: zoom
- drag / middle mouse: pan
- keyboard shortcut: end day if useful
- Esc: close modal/panel

Avoid requiring complex hotkeys for MVP.

Everything important should be clickable.

---

## 16. Accessibility and Readability

Minimum principles:

- readable font sizes
- high enough contrast
- color is not the only information channel
- icons should have tooltips or labels
- long report text should be comfortably spaced
- UI scaling should be possible later

For MVP, prioritize readability over style.

---

## 17. MVP UI Scope

MVP UI must include:

- main map
- top status bar
- selected hex/context panel
- expedition status panel
- scout mission controls
- scout report panel
- marker creation
- free text note input
- journal panel
- event panel
- end day button
- base phase screen
- second expedition setup

MVP UI does not need:

- final art
- animated panels
- complex map filters
- full archive search
- full symbol board
- faction relationship dashboard
- polished sound feedback
- controller support
- localization
- responsive mobile layout

---

## 18. UI Implementation Priorities

### UI-1: Clean Prototype Layout

Replace the corner-debug layout with:

- top status bar
- central map
- left context panel
- right tab/panel area
- bottom action bar

### UI-2: Map Readability

Improve:

- hex size
- fog state
- selected hex
- reachable hexes
- expedition marker
- marker icons

### UI-3: Scout Report Experience

Create readable report cards and a report detail view.

### UI-4: Notes and Markers

Make marker and note creation fast.

This must not feel like admin work.

### UI-5: Event and Base Screens

Create focused event modals and a readable Base Camp screen for return, archive, preparation and
next-expedition setup.

### UI-6: Visual Identity Pass

Only after the core loop works:

- color tuning
- typography
- panel styling
- map atmosphere
- icon consistency

---

## 19. Unity UI Rules

Unity UI scripts may:

- handle buttons
- display values
- open/close panels
- send commands
- refresh visuals

Unity UI scripts must not:

- calculate scout outcomes
- change faction trust directly
- consume supplies directly
- decide event consequences directly
- change hidden world truth directly
- reveal objective truth not present in KnowledgeState

UI reads state.

UI sends commands.

Application/Core changes state.

---

## 20. Screenshot Baseline Notes

The first prototype screenshot is a useful baseline.

It proves that:

- hex map rendering works
- state panels can display values
- scout command controls exist
- reports area exists
- end day exists

Immediate next improvements:

1. Add top status bar instead of isolated right status block.
2. Move scout controls into a dedicated right-side scout panel.
3. Keep selected hex information in the left panel.
4. Add bottom action bar for context actions.
5. Make the map larger and more central.
6. Add a clear report card area instead of an empty black box.
7. Increase font size and spacing.
8. Use visual grouping and panel titles.
9. Add clear selected/reachable/unknown states.
10. Keep current prototype layout only as dev/debug mode if useful.

---

## 21. UI Success Criteria

The UI direction is successful if testers say:

- I know what day it is and what my expedition status is.
- I can tell what is known, reported and unknown.
- I understand what I can do next.
- Scout reports are readable.
- I want to place notes.
- The map invites exploration.
- Returning to old notes feels useful.
- The interface supports mystery instead of solving it for me.

The UI direction fails if testers say:

- It feels like a debug tool.
- I cannot find important actions.
- I do not know what information is confirmed.
- Scout reports are annoying to read.
- Notes feel like extra work.
- The map is too small.
- Panels hide the interesting part of the game.
- I feel like I am managing menus instead of exploring.

---

## 22. Current UI Direction Summary

The game should use a map-first UI with:

- top global status bar
- large central hex map
- left context panel
- right expedition/report panel
- bottom context action bar
- readable scout reports
- clear uncertainty states
- fast player notes and markers
- focused event panels
- simple but meaningful base screen

The first priority is not beauty.

The first priority is:

> Make exploration, uncertainty and note-taking readable and satisfying.

---

## 23. Portraits, Scene Images and Visual Communication

Important communication should feel personal.

Whenever the player receives meaningful information from a person, faction, scout, stranger or expedition member, the UI should show a portrait or character image.

Whenever the player discovers an important place, object, warning, ruin or event, the UI should show an image or scene illustration.

Initial implementation may use placeholders and dummy images.

Final art can be added later.

Core rule:

> Important information should have a face, a place or an image.

This makes the world feel more personal and less like a text database.

### 23.1 Communication Popups

Faction interactions, scout reports, stranger events and important character messages should appear in focused communication popups or panels.

Suggested layout:

```text
+--------------------------------------------------------------------+
| Portrait / Image          | Speaker / Source                       |
|                           | Faction / Role / Context               |
|                           |----------------------------------------|
|                           | Text / Report / Message                |
|                           |                                        |
|                           |                                        |
|                           |----------------------------------------|
|                           | Options / Actions                      |
+--------------------------------------------------------------------+
```

Examples:

- Scout returns with report: show scout portrait.
- Faction elder speaks: show elder portrait.
- Border Warden warning: show messenger or masked contact.
- Stranger offers a map: show stranger portrait.
- Medic reports injury: show medic portrait.
- Hidden faction sends a message: show obscured or symbolic portrait.
- Discovery of sealed gate: show location image instead of portrait.
- Found box event: show object image.

### 23.2 Scout Reports

Scout reports should show the scout’s portrait.

If two scouts were sent together, show both portraits or the lead scout portrait plus a small secondary portrait.

The report should feel like a person came back and told the player what happened.

Example report header:

```text
Mara returned at dusk.

Portrait: Mara
Role: Scout
Status: Tired, but unharmed
Reliability: Medium
```

The portrait helps the player care when a scout is overdue, injured or missing.

### 23.3 Faction Interactions

Faction interactions should show a representative image.

This may be:

- faction leader portrait
- messenger portrait
- guard portrait
- trader portrait
- masked speaker
- symbolic image if the faction does not communicate normally

The player should not interact with a faceless diplomacy panel.

The interaction UI should show who is speaking, not only which faction they belong to.

Minimum MVP fields:

- representative name or role
- faction name
- access level: watcher, guard, messenger, trader, leader or unknown
- current attitude in readable language
- whether this person can offer trade, passage, warnings or only observation

The first contact with a faction should usually show guards, scouts or messengers. Leaders should appear only after trust, location progress, a successful offer, or a faction-specific access requirement.

For unknown factions, the image may be partial, shadowed or symbolic.

Example:

```text
A woman wearing river-shell ornaments waits near the boat landing.
She does not step closer, but she raises both hands to show she carries no weapon.
```

Image:

- Coastal People messenger placeholder

Options:

- greet peacefully
- offer trade
- ask about smoke
- withdraw

MVP offer panel:

- show 5 to 10 authored offers across the whole slice, not a large shop
- when trade is available, always include a simple Knowledge for Supplies offer
- show cost, reward and whether the offer is one-time or repeatable
- keep unavailable offers visible only when useful as a hint; otherwise hide them
- tie offers to map discoveries such as safe camps, springs, routes, passes, warning signs or map fragments
- support offers or dialogue options that require a leverage object, proof, favor or faction-request item
- when useful, show why an option is locked, e.g. "requires grave token" or "requires proof from the sealed ruin"

Example offer cards:

- 10 Supplies: costs Knowledge
- River route hint: reveals reported hexes
- Safe spring location: costs Medicine or Knowledge
- Warning sign interpretation: costs Knowledge
- Rough map fragment: costs Knowledge
- Border passage: requires returned grave token

### 23.4 Event Images

Not all events have speakers.

Events without speakers should show a scene or object image.

Examples:

- sealed box
- marked grave
- warning posts
- abandoned camp
- ravine / broken bridge
- sealed gate
- strange lights near camp
- found journal
- old map fragment

This image should appear in the event popup and later in the archive entry.

### 23.5 Discovery Cards

Important discoveries should trigger a discovery card.

Discovery cards should show:

- image
- discovered name or visible description
- location
- discovery day
- known clues
- available actions
- archive button
- marker/note button

Example:

```text
Discovered: Marked Grave

You find a low stone grave under a bent tree. Fresh cloth bands hang from branches nearby.

Known clues:
- fresh offerings
- carved stone
- no tracks beyond the marker
```

Image:

- marked_grave_placeholder.png

### 23.6 Archive Images

Archive entries should include thumbnails or images.

The archive should not be a plain text list.

Archive images may represent:

- scout portrait
- faction representative
- special location
- object
- warning sign
- map fragment
- recovered journal
- symbol

Example archive card:

```text
Marked Grave
Image: grave thumbnail
Type: Special Location
Status: Discovered
Linked reports: 2
Player notes: 1
```

This helps players remember discoveries visually.

### 23.7 Placeholder Art Strategy

The MVP should use placeholders first.

Placeholder images should be intentionally simple but consistent.

They may be:

- colored silhouette portraits
- simple generated dummy portraits
- icon-like drawings
- grayscale sketches
- labeled placeholder panels
- abstract faction symbols
- rough location thumbnails

Do not block implementation on final art.

Required placeholder categories:

```text
assets/placeholders/portraits/
assets/placeholders/factions/
assets/placeholders/locations/
assets/placeholders/events/
assets/placeholders/objects/
assets/placeholders/symbols/
```

Example placeholder filenames:

```text
portrait_scout_mara_placeholder.png
portrait_scout_jonas_placeholder.png
portrait_coastal_messenger_placeholder.png
portrait_border_warden_placeholder.png
location_marked_grave_placeholder.png
location_ravine_bridge_placeholder.png
location_abandoned_camp_placeholder.png
object_sealed_box_placeholder.png
symbol_warning_posts_placeholder.png
```

### 23.8 Visual Asset Rules

Every important runtime entity should be able to reference a visual asset.

Entities that should support visuals:

- expedition members
- scout reports
- faction representatives
- faction contacts
- special locations
- events
- archive entries
- discovered objects
- warning signs
- journals and map fragments

The UI should fall back to a generic placeholder if no specific image exists.

Fallback examples:

- unknown person silhouette
- unknown location sketch
- unknown object icon
- faction symbol placeholder
- generic report parchment

### 23.9 Visuals Must Not Reveal Hidden Truth

Images must follow the same information honesty rules as text.

Do not show the true hidden nature of a location before the player has discovered it.

Bad:

- showing a monster image for a sealed gate before the player knows what is inside
- showing a faction leader portrait before the player has met the faction
- showing a clear map of a hidden route before it is known

Good:

- show closed gate exterior
- show shadowed messenger
- show partial symbol
- show unknown silhouette
- show object without explaining its purpose

Visuals are part of the information system.

They must not spoil hidden world truth.

### 23.10 UI Priority

For the MVP, the goal is not beautiful final art.

The goal is:

> Every important communication and discovery has a visual anchor.

This will make the game more personal, memorable and emotionally readable.
