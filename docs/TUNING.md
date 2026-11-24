# Tuning sheet – where to change speed, drain, weather, fire and costs

Everything below is editable in the Unity **Inspector**. No code needed.

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

Object: `GREYBOX > Fire pit: Camp A / B / C` → component **Campfire**

| Field | Now | Meaning | Code default in |
|---|---|---|---|
| Burn Minutes | 180 | How long a new fire burns | `Assets/Scripts/World/Campfire.cs` |
| Max Burn Minutes | 360 | Upper limit when adding logs | same |
| Warm Radius | 4.5 | Distance in which the fire warms you | same |
| Warmth Per Minute | 0.20 | Warmth gained next to the fire (night loss is 0.10) | same |

Adding a log gives 90 more minutes (set on the "Add a log" action, field **Fuel Minutes**).

## 6. Camp areas (shelter factors)

Object: `GREYBOX > Zone: Camp A / B / C` → component **ShelterZone** (Exposure Factor)

| Camp | Where | Exposure Factor | Notes |
|---|---|---|---|
| A | Meadow by the stream (45, 6) | 1.5 | Close to water, open and cold |
| B | Forest edge (-10, 42) | 0.9 | Some cover, dry fuel nearby |
| C | Rock overhang on the plateau (170, 0) | 0.5 | Dry and warm, but a climb away and no water |

## 7. Actions (what each object costs and gives)

Object: select the object under `GREYBOX` → component **Interactable** → **Action** (and **Wet Action** for things that change in rain).
Main fields: **Minutes**, **Energy / Water / Food / Warmth Delta** (negative = cost), **Requirements** (item, count, consume), **Grants**, **Blocked By**, **Fire Rule/Effect**, **Exposure Factor**, **One Shot**, **Is Rest**, **Milestone**, texts.

| Object | Action | Min | Energy | Other |
|---|---|---|---|---|
| Phone | Check your phone | 5 | -1 | mission 1, repeatable |
| Luggage | Search the luggage | 15 | -4 | gives Bottle |
| Seats | Search the seats | 15 | -4 | gives Lighter |
| Backpack | Take the jacket | 10 | -2 | gives Jacket, Gear Factor x0.6 |
| Tarp | Cut loose the cabin tarp | 20 | -6 | gives Tarp (for the lean-to later) |
| Handbook | Take the crew handbook | 10 | -2 | gives Map |
| Magazines | Tear out dry magazine pages | 5 | -1 | gives Tinder (dry in rain too) |
| Snack trolley | Eat an airline snack | 10 | -2 | Food +25 |
| Stream: fill | Fill the bottle | 20 | -6 | needs Bottle, blocked if bottle full, gives Dirty Water |
| Stream: drink | Drink straight from the stream | 5 | -15 | Water +30 (risky on purpose) |
| Green bush x3 | Cut green branches | 10 | -4 | gives Green Wood (does not burn) |
| Dry grass x3 | Gather dry grass | 10 | -3 | Tinder; wet version gives Wet Tinder |
| Dead twigs x3 | Snap dead twigs | 10 | -4 | Kindling; wet version gives Wet Kindling |
| Fallen log x3 | Drag a dry log | 15 | -8 | Logs; wet version gives Wet Logs |
| Birch tree x2 | Peel birch bark | 20 | -6 | Tinder, works in rain, far away |
| Camp: build fire | Build a fire | 20 | -4 | uses Tinder, Kindling, 2 Logs; needs Lighter |
| Camp: add logs | Add a log to the fire | 10 | -2 | uses 1 Log, +90 min burn |
| Camp: boil water | Boil the water | 30 | -5 | needs lit fire; Dirty Water → Clean Water |
| Camp: drink | Drink the clean water | 5 | 0 | Water +40, uses Clean Water |
| Camp: sleep | Sleep for a while | 180 | +50 | Is Rest, mission "Get some sleep" |
| Climb up (base of hill) | Climb up to the rock overhang | 25 | -20 | moves you to Camp C |
| Climb down (plateau) | Climb down | 15 | -10 | moves you back |

## 8. Map: forest, paths and the edge (code only)

The map is a dense solid forest. You can only walk on **brown dirt paths**, in **clearings**, and along the **stream**. Everything else is blocked by solid forest, and a 9-unit forest wall closes the edge of the map (outside it is dark void).

These are set in `Assets/Editor/GreyboxSceneBuilder.cs` (then rebuild the scene):

| What | Where in the file | Meaning |
|---|---|---|
| `Clearings` | list near `BuildForest` | Open rectangles (x, y, width, height): crash site, meadow, forest edge, log field, birch grove, hill foot, rescue point |
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
