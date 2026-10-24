# Vorschlag G3: Katalogvarianten und Zustände für die Generator-Orte

Status: **vom Owner freigegeben (Zusammenführungen, Betriebswerte, Platzhaltertexte); Daten angelegt, siehe `world_generator_location_data_concept.md` (Stand G3)**

Zugehörig: `world_generator_location_data_concept.md` (Stufe G3), `low_poly_map_presentation_implementation_plan.md` (V3).

## 1. Worum es geht

Die 30 Orte des Generators sollen Katalogvarianten bekommen. Jede Variante braucht im Katalog:

| Datei (`GameData/Locations/…`) | Inhalt je Variante |
| --- | --- |
| `Variants/variants.json` | Id, Archetyp, Name (Textschlüssel), Modifikatoren |
| `StateProfiles/state-profiles.json` | die Zustände (Betrieb, Anwesenheit, Erkundung) |
| `ScenarioProfiles/scenario-profiles.json` | Platzierungshinweise, Aktionen, Zustandsregeln |
| `ContentProfiles/content-profiles.json` + `Scenes/Locales/de.json` | Texte |

Die Erkundung (interaction) und Anwesenheit (presence) übernehme ich vom Archetyp, so wie sie die bestehenden Profile schon haben. Neu und
ortsspezifisch ist der **Betrieb** (operational): das ist der Zustand, den man im Bild sieht (verfallen, gepflegt, entweiht, erloschen,
brennend …). Dazu bitte deine Freigabe.

## 2. Zusammenführungen mit bestehenden Varianten (bitte entscheiden)

- **Giftiger Sumpf = `restless-marsh`.** Diese Variante ist schon vollständig angelegt (Sumpf, Überflutung, Verseuchung) und wurde vom Generator nie benutzt.
- **Warnzeichen = `border-warning-sign`** (die Tutorial-Grenzwarnung, mit Bild und Zuständen).
- **Gipfelmal = `wind-carved-peak`?** Diese Variante ist ebenfalls angelegt (Gipfel, Grat, signalfähig). Alternative: eine eigene Variante `summit-marker`.

Damit blieben 24 neue Varianten. Bei den drei Zusammenführungen müsstest du nur zustimmen; Texte und Aktionen sind schon da.

## 3. Zustände je Variante

Betrieb (der erste Wert ist der Anfangszustand), je Archetyp die übernommene Erkundung (E) und Anwesenheit (A):

### Untersuchungsorte (E: untouched, inspected, searched, investigated, documented; A: unknown, empty, watched, abandoned)

| Generator-Name | Varianten-Id | Betrieb |
| --- | --- | --- |
| Alte Ruine | `old-ruin` | crumbling, shored-up, collapsed |
| Steinkreis | `stone-circle` | dormant, awakened, broken |
| Verfallener Schrein | `ruined-shrine` | decayed, tended, desecrated |
| Vergessenes Grab | schon `marked-grave` | sealed, open, disturbed |

### Spurenorte (E: wie oben; A: abandoned, recently-used, watched)

| Generator-Name | Varianten-Id | Betrieb |
| --- | --- | --- |
| Kalte Feuerstelle | `cold-fireplace` | cold, smoldering, cleared |
| Zerbrochener Karren | `broken-cart` | wrecked, looted, salvaged |
| Altes Bootslager | `old-boat-camp` | derelict, repaired, in-use |
| Verlassenes Lager | schon `abandoned-camp` | none, cleared, unsafe |

### Verwahrungsorte (E: untouched, inspected, investigated, secured, documented; A: unknown, watched, guarded, abandoned)

| Generator-Name | Varianten-Id | Betrieb |
| --- | --- | --- |
| Verschlossene Höhle | `sealed-cave` | sealed, opened, collapsed |
| Gebannte Schwelle | `warded-threshold` | warded, weakened, broken |
| Versiegeltes Tor | schon `sealed-gate` | sealed, opened, breached |

### Wegehindernis (E: untouched, inspected; A: unknown, watched, guarded, abandoned)

| Generator-Name | Varianten-Id | Betrieb |
| --- | --- | --- |
| Verschütteter Pass | `blocked-pass` | blocked, narrow-path, open, cleared |
| Zerstörte Brücke | schon `broken-bridge` | (bestehend) |

### Ressourcenorte (natürliches Phänomen; E: untouched, observed, approached, surveyed, documented; A: unknown, watched, abandoned)

| Generator-Name | Varianten-Id | Betrieb |
| --- | --- | --- |
| Süßwasserquelle | `freshwater-spring` | clear, muddied, dried-up |
| Fischgrund | `fishing-grounds` | rich, thin, fished-out |
| Heilkräuter | `healing-herbs` | growing, picked, withered |
| Holzbestand | `timber-stand` | standing, partly-felled, cleared |
| Salzvorkommen | `salt-deposit` | exposed, mined, exhausted |
| Beerenhain | `berry-grove` | ripe, picked-over, blighted |

### Gefahrenzonen (E: untouched, assessed, contained, documented; A: unknown, spreading, watched)

| Generator-Name | Varianten-Id | Betrieb |
| --- | --- | --- |
| Giftiger Sumpf | schon `restless-marsh` | (bestehend) |
| Vulkanschlund | `volcanic-vent` | smoldering, erupting, dormant, avoided |
| Tote Zone | `dead-zone` | lifeless, avoided, traversed, contained |

### Landmarken (natürliches Phänomen; E: untouched, observed, approached, surveyed, documented; A: unknown, watched, abandoned)

| Generator-Name | Varianten-Id | Betrieb |
| --- | --- | --- |
| Signalturm | `signal-tower` | dark, lit, ruined |
| Gipfelmal | `wind-carved-peak` oder `summit-marker` | (siehe Abschnitt 2) |
| Kolossstatue | `colossus-statue` | standing, cracked, toppled |
| Alter Leuchtturm | `old-lighthouse` | dark, lit, ruined |

### Grenzzeichen (E: untouched, observed, interpreted, documented; A: unknown, watched, guarded)

| Generator-Name | Varianten-Id | Betrieb |
| --- | --- | --- |
| Grenzsteine | `border-stones` | standing, respected, violated |
| Warnzeichen | schon `border-warning-sign` | standing, respected, violated |
| Schädelmarken | `skull-marks` | standing, respected, violated |
| Bänder im Wind | `wind-ribbons` | standing, respected, violated |

Zustandsnamen sind englische Ids; die deutschen Texte stehen in `de.json` (Textschlüssel).

## 4. Umfang und Platzhalter

Ein Ort braucht für ein sinnvolles Spiel Aktionen, Funde und Texte. Ich schlage vor:

1. In G3 bekommt jede neue Variante **Zustände, Archetyp-Aktionen, einen kurzen Namen und eine Beschreibung** (ein bis zwei Sätze). Findings,
   Ergebnistabellen und Folgen übernehmen die Archetyp-Vorgaben; ortsspezifische kommen später.
2. Die Texte sind als **Platzhalter** erkennbar (Entwurf zur Überarbeitung) und gehen mit der Pull-Request-Beschreibung zur Durchsicht an dich.
3. Das Zustandsprofil gilt je Variante einmal; ein gemeinsames Profil für mehrere Varianten gibt es nicht (jede hat andere Betriebswerte).
4. Alte Spielstände mit `worldgen-<name>`-Ids werden verworfen (deine Entscheidung); das Speicherformat bekommt eine neue Version.
5. Der Generator liefert danach Varianten-Ids statt Namen, und `WorldGenBridge.VariantIdFor` entfällt.

## 5. Fragen

1. Zusammenführungen aus Abschnitt 2 (Giftiger Sumpf, Warnzeichen, Gipfelmal): einverstanden?
2. Die Betriebswerte aus Abschnitt 3: einverstanden oder Änderungswünsche?
3. Texte als Platzhalter durch mich schreiben lassen und von dir durchsehen, oder willst du sie selbst liefern?
