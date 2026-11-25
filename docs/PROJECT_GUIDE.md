# Project guide: "Ohne Empfang / No Signal"

Status written on 25.11.2026 (after the session of 24.11.2026). Read this first when you come back.
Other documents: `docs/LEVEL_EDITING.md` (level design steps), `docs/TUNING.md` (all numbers and costs), `docs/CREDITS.md` (art, font, sound), `docs/GDD.md` (game design document, mostly still empty), `docs/survivalspiel_konzept.md` (concept, German).

---

## 1. The game in short

A plane crashes in the wilderness. You wake up with 3 percent phone battery and no signal. You must survive a day and a night and get rescued. Four bars (Energy, Water, Food, Warmth, 0 to 100) drain with game time; any bar at 0 ends the game. The game teaches basic survival: dry fire materials, safe water, shelter, layers and signalling. Dark humour thought bubbles comment on everything; the end screen gives a sarcastic line and one real survival tip.

**Style:** 2D top-down pixel art (16 pixels = 1 unit), Unity 6000.3.10f1, URP 2D, new Input System.

**Missions (10):** No signal, Salvage supplies, Find water, Gather fire materials, Build a fire, Make the water safe, Build a dry shelter, Get some sleep, Survive the night, Get rescued.

**Controls:** WASD or arrows move, **E** interact (confirm), **Esc** cancel, **R** restart after the end, **H** hints on/off (off by default), **M** mute.
**Debug keys (editor only, click into the Game view first):** 1 survival kit, 2 skip 1 hour, 3 jump to rain warning, 4 jump to 07:00, 5 refill bars, 6 teleport to the rescue spot, 7 teleport to the forest edge camp, 0 hide the key list.

---

## 2. What is made (and works in the code; see section 3 for what has NOT been seen in Unity yet)

### Core
- **Clock** (`GameClock`): runs continuously, 2 game minutes per real second, day and night light, starts at 15:00.
- **Bars** (`SurvivalStats`): energy, water, food, warmth with drain, warmth multipliers (jacket and clothes, shelter, rain, wet clothes), death causes.
- **Actions** (`ActionData`, `Interactable`, `ActionSystem`): every object has an action card with cost (time, energy, water, food, warmth), requirements, gives, flags. You see the cost before you confirm with E, then time runs forward, then effects apply.
- **Inventory** (`Inventory`): simple counts. Items are listed in `ItemType` (new ones only at the END of the list).
- **Missions** (`ObjectiveManager`): latched checkmarks driven by items, milestones and the clock.
- **Game states** (`GameManager`): Playing / Won / Lost, restart with R, sarcastic end quip plus real tip (`ChooseQuipAndTip`).

### Survival mechanics
- **Fire**: needs dry Tinder + Kindling + 2 Logs + Lighter. Wet or green material is refused. A fire burns 180 minutes, logs add 90, warms within 5.5 units, boils water. Flame and smoke are drawn at run time (`CampfireVisual`).
- **Water**: stream water is dirty; boil it on a lit fire (Dirty Water becomes Clean Water). Drinking from the stream is allowed but costs energy.
- **Rain** (`Weather`): random start 18:30 to 19:30, warning 20 minutes before, 150 minutes long. Gather spots switch to "soaked" items that do not burn. Standing in rain without a roof soaks your clothes.
- **Shelter** (`ShelterZone`): nothing is pre-built. At a camp site you build a **frame** (3 green wood), a **roof** (tarp or 6 spruce boughs, needs the frame) and a **bed** (3 boughs). Each part lowers warmth loss. Low ground (stream meadow) floods in rain even with a roof.
- **Clothes**: jacket plus sweater, socks, gloves (in trees) and hat. Their warmth factors multiply.
- **Terrain**: cold river (instant warmth loss and wet clothes), slow thick forest trail.
- **Rescue**: spot far in the north-east. Needs a signal (signal fire with 2 boughs for smoke, the tarp, or the whistle) and daylight 08:00 to 16:00; waiting takes 2 hours. The map item shows direction and distance.

### World and art
- Map 240 x 180 units. Pixel-art from the free **Ninja Adventure** pack (CC0) and the font **Pixelify Sans** (OFL), see `docs/CREDITS.md`.
- **Level.unity** is the hand-editable level: Tilemaps (Paths, Details, Trees with collider), tiled ground pieces, objects with sprites, prefabs in `Assets/Prefabs/Objects`.
- Custom art: plane wreck (`Assets/Resources/Pixel/Custom/PlaneWreck.png`) drawn in the pack's palette.
- Run-time visuals (only visible while playing): player animation, camp flame and smoke, shelter parts, dialogue box, rain overlay.

### Feel
- **Thoughts** (`ThoughtLibrary`, `ThoughtDirector`): about 110 dark-humour lines in a typed dialogue box (bottom of the screen), triggered by actions, missions, time of day, weather, places, idling, walking and low bars.
- **Finds**: junk, useful items, decoy berries and clothes lie on the paths. Their places are **random every run** (`FindsScatter`, `RandomFind`, `FindSpot`).
- **Hints off** by default (`GameHints`); the end screen teaches instead.
- **Sound** (`AudioDirector`): wind, rain, river, day/night/tension music, short effects. The fire sound was removed on purpose (see section 4).
- **Pixel-perfect camera**, steady walking animation, merged tree colliders.

### Editor tools (menu "Survival")
| Command | What it does |
|---|---|
| Build Greybox Scene | the old generator for `landscape.unity` (refuses to run inside Level.unity) |
| Rebuild Level From Generator (REPLACES Level.unity) | throws your level away and rebuilds it. **Normally never use it** |
| Fix Ground (split into pieces) | repairs the "Cannot generate 9 slice" error |
| Repair Find Objects (missing scripts) | restores RandomFind/FindSpot after the file-name bug |
| Trim Hidden Forest (camera never sees it) | removes trees and ground the camera can never see |
| More Tree Variety | swaps about 40% of the trees for other kinds (run once) |
| Place Bushes at the Forest Edge | replaces the automatic bushes by small clumps at the forest edge |
| Remove Automatic Bushes | removes the automatic bushes again |
| Ground > Soft Paths + Olive Meadow / Green Meadow | turns the plain dirt squares into **soft Rule Tile paths** (`Assets/Tiles/Ground/DirtPath_*`), sets the ground to the matching grass and paints a **Meadow** tilemap with random grass tufts. Run again to switch colour. *Remove Meadow Tufts* deletes the tufts |
| Slice Ninja Pack Sheets (16x16) | cuts the tilesets and actor sprite sheets of the full pack (`Assets/Art/NinjaAdventure`) into 16x16 sprites for the Tile Palette. Run once after the pack is imported |

---

## 3. Important: what has NOT been checked in Unity yet

I (Claude) cannot run Unity, so everything was compiled with Unity's own compiler but only partly played by you. Please check in this order and tell me what is wrong:

1. **Console clean?** (Clear it, press Play.) The cloud/account warnings can be ignored. Red errors about `Assets/Scripts` or `Assets/Editor` are real.
2. **Repair Find Objects** was added but not confirmed. After it, "referenced script is missing" warnings should be gone and the finds should be in different places on every run.
3. **Level.unity plays like the old level?** Walk the whole route; the tree colliders (Trees tilemap) are new.
4. **Flame and smoke at a lit camp fire, shelter parts (frame, roof, bed), dialogue box, audio** (rain, river, music, short sounds) were never seen or heard by me.
5. **Walking diagonally and camera shaking** was fixed in code but not confirmed.
6. **Bushes**: the old automatic bushes and flowers are still in the scene until you run *Place Bushes at the Forest Edge* or *Remove Automatic Bushes*.
7. A full run to the rescue: use the debug keys (1, 6, 4) to test the ending quickly.

**Git:** last pushed commit is `917850e` ("Trim hidden forest in Level.unity"). **Not committed yet:** the new `ForestDecorator.cs`, the renamed menu in `LevelExporter.cs`, the tile assets in `Assets/Tiles` (Deco_* and Tree_*), changes in `Level.unity`, `docs/LEVEL_EDITING.md` and this guide. Commit and push when it works (`git add -A`, `git commit -m "..."`, `git push`).

---

## 4. What still has to be made

### Soon (before the playtest, planned for 18.12; confirm the date with the lecturers, the slide says 19.12)
- [ ] Test everything in section 3 and fix what is wrong.
- [ ] **Your own fire sound**: put a looping file called `Fire` (.wav/.ogg/.mp3) into `Assets/Resources/Pixel/Audio/Ambient/`. It plays by itself near a lit fire.
- [ ] **Balance pass** without hints: a first run should be winnable but not trivial (energy costs, rain time, fire burn time, wait time; see `docs/TUNING.md`).
- [ ] **Soft edges** where dirt meets grass and around water (the Floor and Water sheets have edge tiles; or make a Rule Tile). Paths are plain squares right now.
- [ ] **Title screen** with a short intro (story, controls) and a **pause menu** with volume.
- [ ] **Playtest tools**: decision log (CSV), short pre/post quiz, GEQ questions.
- [ ] **A playable build** (macOS or Windows; the Windows build module may be needed on the Mac). Ask the lecturers which platform is accepted. Debug keys only exist in the editor and development builds.
- [ ] Paint the level the way you like it (see section 6): fewer or different trees, bushes, details, flowers if wanted.

### Polish (after the playtest, until the final on 28.01.2027)
- [ ] Skin the HUD and the action card in pixel style (the card and bars are still plain rectangles; the dialogue box is drawn in code).
- [ ] Inventory icons in the HUD (items are text only now).
- [ ] Better art for things that have none: clothes icons, phone, lighter, bottle, jacket; more custom sprites in the style of the pack. Optional extra packs were reviewed; the recommendation was to stay with Ninja Adventure (see the plan notes below).
- [ ] Animations and particles: rain drops from art, leaves, footsteps.
- [ ] Optional missions if testers finish too easily: twisted ankle, night noises, a second water source.
- [ ] Ideas from the Google input not built yet: sleeping in the metal fuselage as a freezing trap, burning plane fuel as a fireball trap, a rescue flyover that only sees thick smoke, reflecting sunlight with the phone as a signal.
- [ ] **GDD** (`docs/GDD.md`): frame story, learning goals, triadic design, evaluation results, sources.
- [ ] 4 to 5 screenshots (1920 x 1080), a 30 to 60 second trailer, submission on the Hagenberg Game Wiki with a PC executable.

---

## 5. How it was made (so you can find things)

### Folders
| Folder | Content |
|---|---|
| `Assets/Scripts/Core` | clock-independent systems: SurvivalStats, Inventory, GameManager, ThoughtDirector, AudioDirector, GameHints, DebugShortcuts |
| `Assets/Scripts/Actions` | ActionData (the data of one action), Interactable (an object you can use), ActionSystem |
| `Assets/Scripts/World` | GameClock, Weather, WorldState, Campfire, ShelterZone, TerrainZone, RescuePoint, FindsScatter / FindSpot / RandomFind, MapData, MapPainter (old level only) |
| `Assets/Scripts/Player` | PlayerController, CameraFollow (also adds the pixel-perfect camera) |
| `Assets/Scripts/Missions` | ObjectiveManager (the 10 missions) |
| `Assets/Scripts/UI` | SurvivalHUD (all UI built in code), ThoughtLibrary (all jokes), PixelUI (dialogue box frame) |
| `Assets/Scripts/Art` | PixelArt, PixelSkin, PixelCharacter (player animation), CampfireVisual, HideAtPlay |
| `Assets/Editor` | the Survival menu tools and `GreyboxSceneBuilder` (the generator) |
| `Assets/Resources/Pixel` | all pictures, fonts and sounds that are loaded by name (Tilesets, Characters, Items, Ui, Fx, Custom, Audio) |
| `Assets/Tiles`, `Assets/Prefabs/Objects` | Tile assets and object prefabs |
| `Assets/Scenes` | `Level.unity` (yours), `landscape.unity` (old generated), `SampleScene` (unused) |
| `Assets/Art/NinjaAdventure` | the **complete Ninja Adventure pack** (Actor, Backgrounds, FX, Items, Ui, Audio, Palette.png, license). Browse here for new art. It is outside `Resources`, so it is not packed into builds. Used pieces are copied into `Assets/Resources/Pixel` (the code loads those by name) |
| `Assets/Art/Fonts` | font license files |

### How things came about
1. A generator script (`GreyboxSceneBuilder`) built a test level from coloured squares, because Unity could not be driven from the chat. It is why the first versions had "greybox" squares.
2. Pixel art from the Ninja Adventure pack was added at run time (`PixelSkin`, `MapPainter`).
3. **Survival > Rebuild Level From Generator** (first run called "Make Level Editable") baked the generated level into `Level.unity`: sprite sheets cut into 16x16 sprites (`PixelSheets`), real Tilemaps, tree tiles with colliders, prefabs. Since then `Level.unity` is yours and the generator must not touch it.
4. Pictures are cut by name: `TilesetNature_3_10` is column 3, row 10 of that sheet; `TilesetNature_r0_0_32x32` is a free rectangle (x, y, width x height in pixels from the top-left).
5. Everything that is code-built: HUD, dialogue box, thought texts, end screens, flame and smoke, shelter parts, camera setup, sound mixing.
6. Rules learned the hard way: **one component per file, file name = class name** (else Unity forgets the component when the scene is saved); never one huge tiled sprite (the ground is 36 pieces); never run the generator on `Level.unity`.

---

## 6. How to make changes

Always: **Cmd+S** after changes, commit often. Change things while the game is stopped.

### 6.1 Level design (tiles)
- Open `Assets/Scenes/Level.unity`. Hierarchy: `LEVEL` contains **Ground** (grass pieces), **Grid: Paths / Details / Trees** (Tilemaps), all objects, **Player**, **Systems**.
- **Window > 2D > Tile Palette**, create a palette, drag in `Assets/Tiles` and whole sheets from `Assets/Resources/Pixel/Tilesets`.
- Choose **Active Tilemap**: **Paths** (dirt roads), **Details** (decoration, never blocks), **Trees** (blocks the way).
- Tools: Paint **B**, Box fill **U**, Pick **I**, Erase **D** (or Shift+click with the brush), Select then **Delete** for an area.
- **A new path** = erase trees on `Trees` and paint dirt on `Paths`. **Closing a way** = paint trees.
- Trees block through their tile's **Collider Type** (Grid = the whole 2x2 cell, Sprite = follows the picture, None = no block). Only the `Trees` tilemap has a collider.
- Bigger Scene view: **Shift+Space** over the Scene view; drag panel borders; close the Game tab while painting; save the layout (Layout menu, top right).
- Fewer or other trees/bushes: see the commands in section 2 and `docs/LEVEL_EDITING.md`, chapter 10. Erase unwanted bushes with the **Erase** tool on `Details`, unwanted trees on `Trees`.
- Ground colour: select all `Ground piece` objects, change **Color** or **Sprite** on the Sprite Renderer.
- Sorting order of layers: ground -50, paths -44, details -43, water -20, trees 1, objects 5, player 10.

### 6.2 Water
There are three water objects in `LEVEL`:
| Object | What it is |
|---|---|
| `Stream (water)` | the stream at x = 57 (5 x 70 units). Walkable, no collider. Tiled water picture |
| `River (cold section)` | an 8 x 8 piece of the stream picture; the cold effect comes from the separate trigger **`Terrain: River`** (TerrainZone: warmth loss when entering, wet clothes) |
| `Landmark: lake` | the lake in the west (30 x 20). **Solid**: has a collider you cannot walk through |
And two things you can use at the stream: `Stream: fill bottle` (gives Dirty Water) and `Stream: drink` (drink untreated, costs energy).

To change it:
- **Look**: select the object, **Sprite Renderer > Sprite**, pick another water sprite (`TilesetWater_1_1` is the plain water now; the sheet has many more, e.g. `TilesetWater_11_0`). Water edge pieces can be painted on `Details` from `TilesetWater`.
- **Size**: Sprite Renderer **Size** (Draw Mode is Tiled) and, for the lake, also the **Box Collider 2D** size. Move it with the Transform.
- **New pond**: duplicate `Landmark: lake` (blocks) or `Stream (water)` (walkable), then move and resize.
- **Cold or wet effect for new water**: duplicate `Terrain: River` and give it the same size and position; values are on the **Terrain Zone** component.
- **Where you can fill your bottle**: move the `Stream: fill bottle` object; it only needs to be near water.

### 6.3 Objects, pictures and assets
- **Move, rotate, scale, duplicate** objects in the Scene view (W, E, R, Cmd+D). Add objects by dragging a prefab from `Assets/Prefabs/Objects`.
- **Change the picture of any object** (chair, magazines, ...): select it, **Sprite Renderer > Sprite**, click the circle and search a sprite name:
  - Seats now use `tileset_camp_r0_112_48x16`, Magazines `Letter2`, Luggage `CrateEmpty`, Backpack `Bag`, Handbook `Book`, Snack trolley `Onigiri`, Tarp `tileset_camp_r128_80_32x32`.
  - Item icons are named like their file in `Assets/Resources/Pixel/Items/...`; tileset pieces are `<Sheet>_<column>_<row>`.
  - If you change a **prefab** (double-click it in `Assets/Prefabs/Objects`), all objects of that kind change.
- **Your own picture**: put a PNG into `Assets/Resources/Pixel/Custom/` (it becomes one sprite) and pick it as above. 16 pixels = 1 unit.
- **More from the Ninja Adventure pack**: the whole pack is in `Assets/Art/NinjaAdventure` (without the preview GIFs). Tileset sprites are named `<Sheet>_<column>_<row>` (for example `TilesetRelief_3_10`), actor sheets use the folder name (`Frog_0_0`). In the Tile Palette drag a sheet from `Assets/Art/NinjaAdventure/Backgrounds/Tilesets`. Single pictures (items, UI, FX) are one sprite each. To use something in code via `Resources.Load`, copy that one file into `Assets/Resources/Pixel/...`.
- **Player picture/animation**: component `Pixel Character` on `Player`, field **Resource** (`Pixel/Characters/Hunter`). Another sheet needs the same layout (columns down, up, left, right; rows = walking frames).
- **Font**: `Assets/Resources/Pixel/Ui/PixelifySans.ttf`, used in `SurvivalHUD.cs` and the camp labels.
- **Sounds and music**: `Assets/Resources/Pixel/Audio/...`. Replace a file with the same name to swap it. Volumes: component `Audio Director` while playing (ambience 0.5, music 0.35, effects 0.7).
- **Texts and jokes**: thoughts in `ThoughtLibrary.cs`; end quips and tips in `GameManager.ChooseQuipAndTip`; mission titles in `ObjectiveManager.cs`; texts of one object in its **Interactable** component.
- **Costs and numbers**: the **Interactable > Action** fields (minutes, energy, water, food, warmth, requirements, gives) and `docs/TUNING.md` for all other values (clock, drain, rain, fire, camps, walking).

### 6.4 What is and is not picked up
**How it works:** an object is "pickable" if it has an **Interactable** component with an action. If the action has **Gives**, the item goes into the inventory; if the action only has stat changes (food, water, warmth), it is used right away. **One Shot** (default) means it can be used once and then dims (it stays in the scene, but cannot be used again). Reusable objects have One Shot off.

**Goes into the inventory (shown in the items line):**
| From | Item | Used once? |
|---|---|---|
| Luggage | Bottle | once |
| Seats | Lighter | once |
| Backpack | Jacket (also warmth factor x0.6) | once |
| Tarp | Tarp (roof or signal, not both) | once |
| Handbook | Map (shows direction to the rescue spot) | once |
| Magazines | Tinder | once |
| Dry grass x3, Birch tree x2 | Tinder (soaked grass gives Wet Tinder while it rains) | once each |
| Dead twigs x3 | Kindling (Wet Kindling in rain) | once each |
| Fallen log x5 | Logs (Wet Logs in rain) | once each |
| Green bush x7 | 3 Green Wood | reusable |
| Spruce tree x10 | 3 Boughs | reusable |
| Stream: fill bottle | Dirty Water (only if you hold the bottle and it is not full) | reusable |
| Boil water at a lit fire | Clean Water | reusable |
| Whistle (random spot) | Whistle (a signal at the rescue spot) | once |
| Foil blanket (random) | item plus warmth x0.85 and +8 warmth | once |
| Wool hat (random), Sweater, Socks, Gloves (in trees, random) | item plus warmth factor (hat 0.94, sweater 0.88, socks 0.94, gloves 0.96) | once |
| Junk: Odd shoe, Guidebook, Selfie stick, Power bank, Beach ball, Crashed drone (random) | only a joke; the items line shows them together as "Junk xN" | once |

**Used immediately, not carried:** Snack trolley (food +25), Chocolate (food +20), Sealed water (water +30), Stream: drink, drinking Clean Water at a camp, Red berries (a trap: costs energy and water), sleeping, building a shelter part, climbing, all signals.

**Not pickable (scenery):** trees, bushes and other tiles, the ground, paths, the plane wreck, the lake, the hill, the big rock, camp name labels, terrain zones. They have no Interactable. To make a decoration pickable, give it a collider and an **Interactable**; to make something not pickable, remove the Interactable (or turn One Shot on and use it).

**Random:** all junk, the whistle, foil blanket, chocolate, sealed water, hat and berries, plus the three clothes in trees, move to random **Find spot** objects at the start of every run (21 ground spots and 8 tree spots). Add or move spots by duplicating a `Find spot (ground)` or `Find spot (tree)` object. Fire materials, spruce, bushes and camps are fixed.

### 6.5 Camps, shelter and fire
- Five camp sites: STREAM MEADOW (A, around 45, 6, low ground floods), FOREST EDGE (B, -10, 42), ROCK LEDGE (C, plateau at 170, 0, reached by "Climb up"), CRASH SITE (D1, 8, -14), NORTH CLEARING (D2, -4, 23). A camp = objects `Camp X: ...`, `Zone: Camp X` (component **Shelter Zone**: site name, note, site factor, low ground) and `Fire pit: Camp X` (component **Campfire**).
- New camp: select all objects of one camp together, duplicate together (Cmd+D), move them and give the zone a new Site Name.
- The numbers (part factors 0.85 / 0.80 / 0.75) are on **Shelter Zone**.

### 6.6 Git
```bash
git add -A
git commit -m "describe the change"
git push
```
Check with `git status` first. The repo is on GitHub (annaljbs/Survival_Game). Only CC0 art and an OFL font are in it, so it is safe to be public. Do not add other itch.io packs to git without checking their license ("no redistribution" packs must stay out of the repo).

---

## 7. Decisions and notes worth remembering
- Wilderness setting (not an island), 10 missions, English in-game text.
- Hints are off on purpose ("you have to think while playing"); H turns them on for testing or the playtest.
- Dark humour: jokes aim at bad luck, nature and technology, never at real victims.
- Art decision: one base style (Ninja Adventure). Of the 24 itch.io packs reviewed, only two optional add-ons were recommended: *aamatniekss Topdown Fantasy Forest* (free) and *shubibubi 100 Nature Things* ($3.99, foraging items). Others differ in size or style, have unclear licenses, are AI-made (judgemon21: do not use) or withdrawn (pixivan).
- The Unity MCP bridge is unreliable (it hung once); work is verified by compiling with Unity's compiler and by you playing.
- Grading rubric (course): Technical Implementation, Purpose & Creativity, Functionality & Polish. Pass target: a solid, playable, fun game with a clear purpose.
