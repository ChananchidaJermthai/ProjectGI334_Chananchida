# Colonization asset integration

Open `Assets/Scenes/Map01.unity` in Unity 6000.6.6f1 and press Play. Map01 is the first enabled build scene. The original SampleScene is retained.

## Imported artwork

Original archive contents are retained under `Assets/Art/Colonization`, including source notes, SVG emblems, JSON data, and presentation images. No external asset downloads or additional packages are required.

| Artwork | PNG files | Integration |
| --- | ---: | --- |
| Terrain | 11 | Generated hex map and collection browser |
| Forest terrain | 7 | Forest variants on the map, clearing order, collection browser |
| Land units | 20 | Unit data, collection browser, colony recruitment |
| Ships | 6 | Ship data, collection browser, coastal colony shipbuilding |
| Native leaders | 8 | Native nation detail screens |
| Native emblems | 8 | Nation detail screens and village selection |
| Native villages | 8 | Eight settlements on the generated map |

The 8 nation profile JPGs are available from the nation detail screen. The overview JPG, HTML, CSV, SVGs, research notes, and combined JSON are retained as reference files. Individual JSON records populate the nation ScriptableObjects; their source URLs and contextual notes are preserved.

Textures import as Single sprites with alpha transparency, no mipmaps, and a maximum size of 2048. Terrain has substantial transparent padding. Visible alpha bounds are stored in `ArtSprite` and used to center/scale world artwork without modifying original pixels or sprite rectangles. Forest artwork already includes the ground, so it replaces the terrain image rather than stacking another full terrain image over it.

## Controls and playable flow

- WASD / arrow keys: move camera. Mouse wheel: zoom. Middle mouse drag: pan.
- Left-click a discovered hex: select. Click repeatedly to cycle stacked units.
- Right-click: route selected unit toward the hex, within its movement budget.
- Tab: next available unit. Backspace / Enter / End turn: complete work and refresh movement.
- B / Found colony: Free Colonist schedules a settlement for next turn; the colonist is consumed on completion.
- P / Clear forest: Pioneer schedules clearing for next turn.
- Right-click a neighboring ship with a land unit: board if there is capacity.
- Right-click adjacent land with a loaded ship, or press Disembark: unload the first passenger.
- Select a discovered Native village and choose Meet leader, or open Native nations to browse all 8.
- Recruit / build: browse all units; recruit on the selected colony (otherwise the first colony). A land unit costs 20 Food. A ship costs 20 Lumber and requires adjacent ocean. New units move next turn.

Initial units: Free Colonist, Hardy Pioneer, and Caravel at the eastern coastline. The map is deterministic (seed 334), 24 by 16 hexes. Land and ships use distinct traversable terrain. Routes use movement costs. Exploration reveals tiles within three hexes of units.

## Prototype rules and remaining manual work

This integration implements the available artwork in a playable foundation. It does not complete every feature in Weeks 1–13 of the production manual.

- Player settlements temporarily reuse the first Native village sprite, explicitly labelled in the welcome screen; dedicated European colony art is absent from this ZIP.
- Colony production is simplified: its tile and neighboring tiles contribute food, and forest tiles contribute lumber each turn. There is no worker assignment or 16-product production chain yet.
- Recruitment prices, movement, strength, and capacity are prototype balancing values, not historical research or a full transcription of the manual.
- Native nations are settlements and information screens; diplomacy, combat, and Native AI are not implemented.
- European nation selection, Europe trade/market, cargo products, resource icons, building artwork, opening illustrations, sound, save/load, and full town management are not supplied by this integration.
- The supplied Native notes distinguish interpretive portraits, original game emblems, different leader dates, and scenario balance settings. Those qualifications are preserved in data and displayed in the nation screens.

## Editing and regeneration

Runtime code is under `Assets/Scripts/Colonization`. Editable data is under `Assets/Data/Colonization`. Prefabs are under `Assets/Prefabs/Colonization`.

Use **Colonization > Import artwork and build Map01** to regenerate the scene/data from imported source artwork. This deliberately updates generated data, prefabs, Map01, and build-scene order; save or commit hand edits before rerunning it. It does not modify source image files or SampleScene. Normal Play does not regenerate these files.

## Validation

EditMode tests cover hex adjacency/distance, terrain restrictions, and all 68 distinct gameplay sprite references. PlayMode tests cover world initialization, boarding/unloading, turn completion, settlement production/recruitment, forest clearing, movement budgets, and nation UI.

See `Docs/Validation.md` for executed results.
