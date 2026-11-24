# Tuning sheet – where to change speed, drain, weather, fire and costs

Everything below is editable in the Unity **Inspector**. No code needed.

> **Level design by hand:** see `docs/LEVEL_EDITING.md`. The sections below about the generator (`GreyboxSceneBuilder`) describe the old generated test level (`landscape.unity`). In the hand-made `Level.unity` you change objects and tiles directly in Unity; the Inspector values (costs, drain, rain, ...) are the same.

## Golden rules

1. **Change values while the game is stopped.** Edits made in Play mode are lost when you press Stop.
2. **Do not rerun `Survival > Build Greybox Scene` after tuning.** It recreates all objects and overwrites Inspector changes. If a value must survive a rebuild, also change the default in the code file listed in the last column.
3. After tuning: **File > Save** (Cmd+S) so the scene keeps the values.
4. All drains are **per game minute**. The clock turns game minutes into real seconds (see "Maths").

## 1. Clock and day/night

Object: `GREYBOX > Systems` → component **GameClock**

| Field | Now | Meaning | Code default in |
|---|---|---|---|
| Realtime Minutes Per Second | 2 | Game minutes per real second. Bigger = everything (drain, day, night, rain) happens faster | `Assets/Scripts/World/GameClock.cs` |
| Start Hour | 15 | Time of day when the game starts (15 = 15:00) | same |
| Night Intensity / Day Intensity | 0.15 / 1 | Darkness of the scene at night / day | same |

The clock runs continuously while playing. It pauses while an action card is open, while an action runs (the action moves the clock itself) and when the game is over. Daylight is between 06:00 and 20:00.

## 2. The four bars

Object: `GREYBOX > Player` → component **SurvivalStats**

| Field | Now | Meaning (per game minute) | Code default in |
|---|---|---|---|
| Water Loss | 0.10 | Water lost by time | `Assets/Scripts/Core/SurvivalStats.cs` |
| Food Loss | 0.04 | Food lost by time | same |
| Warmth Loss Day / Night | 0.02 / 0.10 | Warmth lost by time | same |
| Energy Loss Per Minute | 0.03 | Energy lost just by being awake | same |
| Warn Level / Critical Level | 30 / 15 | Bar turns warning colour / flashes red | same |
| **Gear Factor** | 1 (0.6 with jacket) | Multiplies warmth loss: clothing | same |
| **Shelter Factor** | 1 | Set automatically by the camp area you stand in | same |
| **Weather Factor** | 1 (1.5 in rain) | Set automatically by Weather | same |

Total warmth loss per minute = (day or night value) x Gear x Shelter x Weather, minus the fire bonus. A bar at 0 ends the game.

## 3. Walking

Object: `GREYBOX > Player` → component **PlayerController**

| Field | Now | Meaning | Code default in |
|---|---|---|---|
| Move Speed | 4 | Units per second | `Assets/Scripts/Player/PlayerController.cs` |
| Energy Per Unit | 0.05 | Extra energy per unit walked (on top of Energy Loss Per Minute) | same |
| Interact Radius | 1.6 | How close you must be to use an object | same |

## 4. Weather (rain)

Object: `GREYBOX > Systems` → component **Weather**

| Field | Now | Meaning | Code default in |
|---|---|---|---|
| Earliest / Latest Start Hour | 18.5 / 19.5 | Rain starts at a random time in this window each run (18.5 = 18:30) | `Assets/Scripts/World/Weather.cs` |
| Duration Minutes | 150 | How long it rains | same |
| Warning Minutes | 20 | Warning message this long before the rain | same |
| Rain Weather Factor | 1.5 | Warmth loss multiplier in rain | same |

While it rains, gather spots switch to their "soaked" version (gives wet items that do not burn).

## 5. Fire

Object: `GREYBOX > Fire pit: Camp A / B / C / D1 / D2` → component **Campfire**

| Field | Now | Meaning | Code default in |
|---|---|---|---|
| Burn Minutes | 180 | How long a new fire burns | `Assets/Scripts/World/Campfire.cs` |
| Max Burn Minutes | 360 | Upper limit when adding logs | same |
| Warm Radius | 5.5 | Distance in which the fire warms you | same |
| Warmth Per Minute | 0.20 | Warmth gained next to the fire (night loss is 0.10) | same |

Adding a log gives 90 more minutes (set on the "Add a log" action, field **Fuel Minutes**).

## 6. Camp sites and building your own shelter

Object: `GREYBOX > Zone: Camp A / B / C / D1 / D2` → component **ShelterZone**

No camp has a shelter at the start. You build it yourself in three parts, and the part multipliers stack with the site factor:

| Part | How | Multiplier | What it teaches |
|---|---|---|---|
| **Frame** | 3 Green Wood (poles = 1 bush cut), 30 min, -6 energy | x0.85 | Wind is a big heat thief. Green wood is bad fuel but good for building |
| **Roof** | **Tarp** (15 min, -3) **or** 6 Boughs (40 min, -6), needs the frame | x0.80, keeps the rain off | A roof must be waterproof or thick/layered. Using the tarp here means no tarp signal later |
| **Bedding** | 3 Boughs (1 spruce cut), 20 min, -3 energy | x0.75 | The cold ground steals heat: insulate from below |

Fully built: x0.85 x 0.80 x 0.75 = **0.51** of the site factor. Fields on ShelterZone: `Site Factor`, `Low Ground`, `Frame / Roof / Bedding Factor`.
Boughs come from **Spruce trees** (3 per cut, 12 min, -4 energy, can be cut again and again). Green Wood comes from **Green bushes** (reusable).
Without a roof, standing in rain **soaks your clothes** (wet for 180 min after leaving the rain, x1.4 warmth loss on top of the rain x1.5).

| Site | Where | Site Factor | Notes |
|---|---|---|---|
| STREAM MEADOW (A) | (45, 6) | 1.2 | Water next to you, but **low ground**: it floods in rain, so even a good roof does not keep you dry |
| FOREST EDGE (B) | (-10, 42) | 0.9 | Trees break the wind; twigs, logs and spruce close by; far from water |
| ROCK LEDGE (C) | plateau (170, 0), climb up from the hill | 0.7 | Rock wall behind you. Far, costly climb, no water and no spruce: carry your materials |
| CRASH SITE (D1) | (8, -14) | 1.0 | Open ground, wreck supplies and 3 spruce close by |
| NORTH CLEARING (D2) | (-4, 23) | 1.0 | Open ground on the path north |

Each site has a name label on the ground, and the info line (top right) shows the site, its note, the shelter progress `[x]` and the current warmth loss factor.
The **sleep spot** is under the shelter, to the right of the fire pit (the fire's `Warm Radius` is 5.5, so it still warms you there).

### Terrain and wet clothes

Object: `GREYBOX > Terrain: River` and `Terrain: Thick trail` (several pieces) → component **TerrainZone**

| Zone | Where | Energy x | Speed x | Warmth | Notes |
|---|---|---|---|---|---|
| River (cold water) | north stretch of the stream road, y 38 to 46 | 1.2 | 0.8 | -12 once when entering, then 0.3 per unit | Soaks your clothes |
| Thick forest trail | shortcut from the meadow to the rescue point | 1.5 | 0.75 | none | Shorter but slow |

**Wet clothes:** while wet, warmth loss is multiplied by **Wet Factor** (1.4, on SurvivalStats). They dry in 240 game minutes, 4x faster next to a lit fire. The HUD shows "WET CLOTHES".

### Finds along the paths (edit in `BuildPickups` in the builder)

**Random every run:** the finds below are shuffled onto 21 ground spots and 8 tree spots when the game starts (`FindsScatter`), so the places in the table are only the editor positions. Clothes in trees go to tree spots, everything else to ground spots. The fire materials, spruce, bushes and camps are fixed.

All look alike (grey, small) so you only learn what they are by picking them up. Every pickup shows a dark-humour **thought bubble** (field `Thought` on the action; bubble lasts 6 s).

| Find | Where | Effect | Learning |
|---|---|---|---|
| Foil blanket | final stretch to the rescue spot (57, 60) | Warmth +8, warmth loss x0.85 | Foil reflects body heat, but doesn't insulate from cold ground |
| Whistle | end of the lake dead end (-52, -27) | Item; at the rescue spot "Blow three blasts" makes a signal | Three blasts = international distress signal |
| Sealed water bottle | end of the big-rock dead end (23, -29) | Water +30 | Sealed water is safe, stream water is not |
| **Wool sweater** (in a tree) | crash site edge (-24, 10) | Warmth loss x0.88; climbing costs 20 min, -7 energy | Layers trap warm air; wool works even when damp |
| **Dry socks** (in a tree) | forest edge (-18, 43) | x0.94 | Wet feet get cold fast |
| **Gloves** (in a tree) | meadow (34, 10) | x0.96 | Hands get cold first |
| **Wool hat** | birch path (-46, 8) | x0.94 | An uncovered head loses heat quickly |
| Chocolate bar | path north (-3, 31) | Food +20 | |
| Red berries x2 | birch path (-69, 22), long road (42, 50) | Food +5, Water -15, Energy -15 | Never eat unknown plants |
| Junk: shoe, guidebook, selfie stick, power bank, beach ball, crashed drone | along the paths | Only a joke; the HUD shows them as "Junk xN" | |

Clothes stack with the jacket (x0.6): all four together make x0.6 x 0.88 x 0.94 x 0.94 x 0.96 = about 0.45. The info line shows "Clothing: warmth loss x...". Trees with clothes show a coloured scrap at the top.

Thought bubbles also appear for: the phone, the rain warning, rain start/stop, and any bar that gets critical (`CriticalThoughts` in `SurvivalHUD.cs`).

## Debug shortcuts (testing only)

Work in the Unity Editor and development builds only (`Assets/Scripts/Core/DebugShortcuts.cs`). They need no scene changes. Click into the Game view first. A small panel at the bottom-left shows the keys.

| Key | What it does |
|---|---|
| **1** | Survival kit: bottle, lighter, jacket, map, tarp, 2 tinder, 2 kindling, 4 logs, 6 green wood, 12 boughs, dirty water |
| **2** | Skip 1 hour (the bars drain as normal) |
| **3** | Jump to the rain warning (if the rain has not started yet) |
| **4** | Jump to 07:00 the next morning (the bars drain meanwhile: build shelter and fire first, or refill with 5) |
| **5** | Refill all four bars |
| **6** | Teleport to the rescue spot |
| **7** | Teleport to the forest edge camp |
| **0** | Hide or show the key panel |

Quick test of the ending: 1, then 6, build a signal fire, press 4 (or skip hours with 2) until 08:00 to 16:00, then "Wait for rescue".

## Sound, fire effects and custom art

| What | Where |
|---|---|
| Sound and music | `Assets/Scripts/Core/AudioDirector.cs` (creates itself, no scene setup). Inspector fields appear on the `AudioDirector` object while playing; defaults: ambience 0.5, music 0.35, sound effects 0.7. **M** mutes everything |
| What plays when | Wind always (louder at night); rain while it rains; river near the stream (x = 57); **fire crackling near a lit fire: currently no sound file** (the pack's one was removed, see below); music "Clearing" by day, "Dark Forest" at night, "Tension" while any bar is critical; short sounds for pickups (coin), finished actions, refused actions and bar warnings |
| Files | `Assets/Resources/Pixel/Audio/` (14 MB). To swap a sound, replace the file with the same name. **Fire sound:** put your own looping file at `Assets/Resources/Pixel/Audio/Ambient/Fire.wav` (or .ogg/.mp3, name `Fire`): it plays by itself, louder the closer you are to a lit fire. Without the file the fire is silent |
| Flame and smoke at the fire pit | `CampfireVisual` on each fire pit (flame flickers, shrinks when under 30 minutes are left, smoke puffs rise) |
| Shelter parts | Drawn by `ShelterZone.Build` from camp tiles: ridge pole and posts (frame), tarp cloth or layered bush (roof), grass (bed) |
| Plane wreck | `Assets/Resources/Pixel/Custom/PlaneWreck.png`, placed in `BuildCrashSite` of the builder |

## Art (pixel art from the free Ninja Adventure pack)

The pictures are in `Assets/Resources/Pixel/` (CC0, see `docs/CREDITS.md`). They are put onto the coloured squares **when the game starts** (Play mode), so the Scene view in the editor still shows plain squares and dark forest blocks.

| What | Where it is decided |
|---|---|
| Ground (grass, dirt paths) and the 32x32 trees | `Assets/Scripts/World/MapPainter.cs`, using the shape of the map in `Assets/Scripts/World/MapData.cs` (clearings, paths). Changing `MapData` and rebuilding the scene changes walls and pictures together |
| Picture for each object (bag, book, tarp, camp pit, ...) | `ApplyIcon` in `Assets/Editor/GreyboxSceneBuilder.cs`. Item icons: `Icon(go, "Object/Bag")`, parts of a tileset: `Region(go, path, x, y, w, h)` (pixels from the top-left, 16 pixels = 1 unit) |
| Player (walking animation, 4 directions) | `PixelCharacter` on the Player, sheet `Pixel/Characters/Hunter`. To use another character, change `Resource` (all sheets in the pack have the same layout) |
| Font | `Resources/Pixel/Ui/PixelifySans` (OFL) is used by the HUD and the camp name labels. The pack's own font had almost no space between words. Sizes are scaled x1.12 in `MakeText` of `SurvivalHUD.cs` |
| Camera | `CameraFollow` adds a **pixel-perfect camera** (16 pixels per unit, 320x180 reference = about 20 x 11 tiles): art pixels always map to whole screen pixels, so nothing shimmers. To see more or less of the map, change the reference size in `CameraFollow.Awake` |
| Trees | `MapPainter.PaintTrees`: one tree or bush per forest cell, slightly shifted and mirrored at random, on the same grass as the ground (the dark placeholder blocks are hidden by `HideAtPlay`) |
| Dialogue box | Drawn in code (`PixelUI.cs`) in the style of classic RPGs |

Textures are kept sharp automatically (`Assets/Editor/PixelImportFixer.cs`: no compression, point filter). If a picture is missing, the coloured square stays.

## Thought bubbles and end screens

Object: `GREYBOX > Systems` → component **ThoughtDirector** (it decides *when*; the words are in `Assets/Scripts/UI/ThoughtLibrary.cs`, about 110 lines in pools).

| Field | Now | Meaning |
|---|---|---|
| Min Seconds Between Random | 14 | Cooldown for flavour bubbles (gathering, walking, idling, ...). Raise it for fewer bubbles |
| Idle Seconds | 25 | Standing still this long (real seconds) triggers a comment |
| Walk Units Per Comment | 90 | Distance walked before a walking comment |
| Chance After Gather | 0.35 | Chance of a comment after picking up material |
| Chance When Refused | 0.6 | Chance of a comment when an action is not possible |

**Always shown (ignore the cooldown):** phone checks (4 fixed lines in order, then random), mission completed, a bar getting low or critical, fire, shelter parts, sleeping, climbing, signals, rain without a roof, flooded or cosy shelter, wet clothes, sunset / midnight / 3 a.m. / sunrise / day-2 noon / day-2 sunset, river, fire dying.
**Flavour (cooldown):** gathering, searching, walking, idling, entering a camp site for the first time, thick trail, junk x3 and x6, refused actions.
An action's own `Thought` (set on the action) wins over the generic pool. A line is never repeated before its pool is used up. To add jokes: add lines to a pool in `ThoughtLibrary.cs` (no scene rebuild needed).

**End screens:** a sarcastic line and one **real survival tip** under the result, chosen in `GameManager.ChooseQuipAndTip` from the cause and what you did: ate unknown berries, drank straight from the stream (`drank_stream`), never built a fire, soaked by rain without a roof (`rain_exposed`), or the plain cause. The win screen has its own line and tip. These replace the old in-game hints as the place where the game teaches.

## Hints (help texts)

By default there are **no hints**: no mission explanations under the current mission, no tips in the low-bar warnings and no explanation what is missing ("Needs: Logs x2" instead of "Needs 2 dry logs. Wet logs won't light"). Press **H** in the game to switch them on (for onboarding or testing). The default is `GameHints.Enabled` in `Assets/Scripts/Core/GameHints.cs`.

## 7. Actions (what each object costs and gives)

Object: select the object under `GREYBOX` → component **Interactable** → **Action** (and **Wet Action** for things that change in rain).
Main fields: **Minutes**, **Energy / Water / Food / Warmth Delta** (negative = cost), **Requirements** (item, count, consume), **Grants**, **Blocked By**, **Fire Rule/Effect**, **Exposure Factor**, **One Shot**, **Is Rest**, **Milestone**, texts.

| Object | Action | Min | Energy | Other |
|---|---|---|---|---|
| Phone | Check your phone | 5 | -1 | mission 1, repeatable |
| Luggage | Search the luggage | 15 | -3 | gives Bottle |
| Seats | Search the seats | 15 | -3 | gives Lighter |
| Backpack | Take the jacket | 10 | -2 | gives Jacket, Gear Factor x0.6 |
| Tarp | Cut loose the cabin tarp | 20 | -4 | gives Tarp (roof OR signal, not both) |
| Handbook | Take the crew handbook | 10 | -2 | gives Map: the info line then shows the direction and distance to the rescue spot |
| Magazines | Tear out dry magazine pages | 5 | -1 | gives Tinder (dry in rain too) |
| Snack trolley | Eat an airline snack | 10 | -2 | Food +25 |
| Stream: fill | Fill the bottle | 20 | -4 | needs Bottle, blocked if bottle full, gives Dirty Water |
| Stream: drink | Drink straight from the stream | 5 | -15 | Water +30 (risky on purpose) |
| Green bush x7 | Cut green branches | 10 | -3 | gives 3 Green Wood (does not burn; poles for the frame), reusable |
| Dry grass x3 | Gather dry grass | 10 | -2 | Tinder; wet version gives Wet Tinder |
| Dead twigs x3 | Snap dead twigs | 10 | -3 | Kindling; wet version gives Wet Kindling |
| Fallen log x5 | Drag a dry log | 15 | -5 | Logs; wet version gives Wet Logs |
| Birch tree x2 | Peel birch bark | 20 | -4 | Tinder, works in rain, far away |
| Camp: build fire | Build a fire | 20 | -4 | uses Tinder, Kindling, 2 Logs; needs Lighter |
| Camp: add logs | Add a log to the fire | 10 | -2 | uses 1 Log, +90 min burn |
| Camp: boil water | Boil the water | 30 | -5 | needs lit fire; Dirty Water → Clean Water |
| Camp: drink | Drink the clean water | 5 | 0 | Water +40, uses Clean Water |
| Camp: sleep | Sleep for a while | 180 | +60 | Is Rest, mission "Get some sleep" |
| Camp: frame | Build the shelter frame | 30 | -6 | uses 3 Green Wood |
| Camp: roof (tarp) | Hang the tarp as a roof | 15 | -3 | uses Tarp, needs frame |
| Camp: roof (boughs) | Thatch a roof with spruce boughs | 40 | -6 | uses 6 Boughs, needs frame |
| Camp: bedding | Make a bed of spruce boughs | 20 | -3 | uses 3 Boughs |
| Spruce tree | Cut spruce boughs | 12 | -4 | gives 3 Boughs, reusable |
| Rescue point: signal fire | Build a signal fire | 25 | -5 | uses Tinder, Kindling, 2 Logs, 2 Boughs (smoke); needs Lighter; sets signal |
| Rescue point: tarp | Spread the bright tarp | 20 | -4 | needs Tarp (kept); sets signal |
| Rescue point: wait | Wait for rescue | 120 | -6 | needs a signal and daylight (**08:00 to 16:00**); **wins the game** |
| Climb up (base of hill) | Climb up to the rock ledge | 25 | -20 | moves you to Camp C |
| Climb down (plateau) | Climb down | 15 | -10 | moves you back |

## 8. Map: forest, paths and the edge (code only)

The map is a dense solid forest. You can only walk on **brown dirt paths**, in **clearings**, and along the **stream**. Everything else is blocked by solid forest, and a 9-unit forest wall closes the edge of the map (outside it is dark void).

These are set in `Assets/Editor/GreyboxSceneBuilder.cs` (then rebuild the scene):

| What | Where in the file | Meaning |
|---|---|---|
| `Clearings` | list near `BuildForest` | Open rectangles (x, y, width, height): crash site, meadow, forest edge, log field, birch grove, hill foot, rescue point (far north-east, 98/70) |
| `Paths` | list near `BuildForest` | Polylines of waypoints; each becomes a dirt road about 7 units wide |
| `PathHalfWidth` | constant | Smaller = narrower paths (3.5 = 7 wide) |
| `Cell` | constant | Size of one forest block (3) |
| `EdgeBand` | constant | Thickness of the forest wall at the edge (9) |

To add a new place or road: add a rectangle to `Clearings` or a polyline to `Paths`, put the objects inside it, rebuild.

## Maths: how long until a bar is empty?

```
real seconds until empty = 100 / (drain per game minute x Realtime Minutes Per Second)
```

With today's values (clock = 2):

| Bar | Calculation | Empty after |
|---|---|---|
| Water | 100 / (0.10 x 2) | about 8 min |
| Warmth at night (no jacket, open ground) | 100 / (0.10 x 2) | about 8 min |
| Warmth at night with jacket and Camp B | 100 / (0.10 x 0.6 x 0.9 x 2) | about 15 min |
| Warmth at night next to a lit fire | gains 0.20, loses 0.10 x factors | stays stable |
| Food | 100 / (0.04 x 2) | about 21 min |
| Energy, standing | 100 / (0.03 x 2) | about 28 min |
| Energy, walking constantly | 100 / (0.03 x 2 + 0.05 x 4) | about 6.5 min |

Other numbers: 24 game hours = 12 real minutes at clock 2. Rain starts about 3.5 to 4.5 game hours after the start (roughly 105-135 real seconds of idle time, less if you do actions, because actions jump the clock).

## Quick recipes

| I want... | Change |
|---|---|
| Everything slower / calmer | Lower **Realtime Minutes Per Second** (e.g. 1) |
| More time to gather before the rain | Raise **Earliest/Latest Start Hour** (e.g. 20 / 21) or lower the clock speed |
| Rain more dangerous | Raise **Rain Weather Factor** |
| Fire lasts longer / shorter | Change **Burn Minutes** and **Fuel Minutes** |
| Make camp choice matter more | Spread the **Exposure Factors** further apart |
| Thirst less urgent | Lower **Water Loss** |
| Walking should hurt more | Raise **Energy Per Unit** |
| An action cheaper | Lower **Minutes** and/or make **Energy Delta** less negative |

## Testing a change

1. Stop Play mode, change the value, press Cmd+S.
2. Press Play, watch the bar for the time you expect (see the table above).
3. Change only one value at a time and note it in the log.

## Tuning log (write your changes here)

| Date | Value | Old | New | Why |
|---|---|---|---|---|
| 24.11.2026 | (initial values from this sheet) | | | |

| 24.11.2026 | Energy costs of gathering, searching, shelter parts; walking 0.08 back to 0.05; sleep +50 to +60; spruce/bush give 3 | | | After two test deaths: energy ran out before the shelter was done (total cost of a good first day was above 100) |
