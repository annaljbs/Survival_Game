using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menu: Survival > Build Greybox Scene. Rebuilds the playable test map in the open scene
/// from coloured squares. Safe to run repeatedly (it replaces the "GREYBOX" root).
/// NOTE: this overwrites values you changed in the Inspector (see docs/TUNING.md).
/// </summary>
public static class GreyboxSceneBuilder
{
    const string ArtDir = "Assets/Art/Placeholder";
    const string RootName = "GREYBOX";
    const float HalfW = 120f, HalfH = 90f;

    static Sprite square;
    static Material litMaterial;

    [MenuItem("Survival/Build Greybox Scene")]
    public static void Build()
    {
        square = EnsureSprite();
        litMaterial = EnsureMaterial();

        var old = GameObject.Find(RootName);
        if (old != null) Object.DestroyImmediate(old);
        var root = new GameObject(RootName).transform;

        BuildTerrain(root);
        BuildCrashSite(root);
        BuildWater(root);
        BuildMaterials(root);
        BuildCamps(root);
        BuildPickups(root);

        // --- Rescue point ---
        BuildRescuePoint(root);
        BuildTerrainZones(root);

        BuildForest(root);

        // --- Player ---
        var player = Box("Player", root, Vector2.zero, new Vector2(0.8f, 0.8f), new Color(0.95f, 0.85f, 0.3f), 10);
        player.AddComponent<Rigidbody2D>();
        player.AddComponent<CircleCollider2D>().radius = 0.5f;
        player.AddComponent<SurvivalStats>();
        player.AddComponent<Inventory>();
        player.AddComponent<PlayerController>();

        // --- Systems ---
        var systems = new GameObject("Systems");
        systems.transform.SetParent(root, false);
        systems.AddComponent<WorldState>();
        systems.AddComponent<FindsScatter>();
        systems.AddComponent<GameClock>();
        systems.AddComponent<Weather>();
        systems.AddComponent<ObjectiveManager>();
        systems.AddComponent<ActionSystem>();
        systems.AddComponent<GameManager>();
        systems.AddComponent<SurvivalHUD>();

        SetupCameraAndLight(player.transform);
        SaveScene();
    }

    // ------------------------------------------------------------------ terrain

    static void BuildTerrain(Transform root)
    {
        // The map is large on purpose: key places are far apart and mostly off-screen
        // (camera sees about 21 x 12 units), so players have to explore and remember landmarks.
        Box("Ground", root, Vector2.zero, new Vector2(HalfW * 2, HalfH * 2), new Color(0.30f, 0.45f, 0.26f), -50);
        Wall(root, new Vector2(0, HalfH), new Vector2(HalfW * 2, 2));
        Wall(root, new Vector2(0, -HalfH), new Vector2(HalfW * 2, 2));
        Wall(root, new Vector2(HalfW, 0), new Vector2(2, HalfH * 2));
        Wall(root, new Vector2(-HalfW, 0), new Vector2(2, HalfH * 2));

        // Landmarks (solid, big, easy to recognise and remember)
        Solid("Landmark: big rock", root, new Vector2(30, -35), new Vector2(7, 7), new Color(0.40f, 0.40f, 0.44f));
        Solid("Landmark: lake", root, new Vector2(-70, -30), new Vector2(30, 20), new Color(0.15f, 0.35f, 0.70f));
        Solid("Landmark: hill", root, new Vector2(70, -20), new Vector2(18, 12), new Color(0.45f, 0.32f, 0.20f));

        // Rock plateau for the overhang camp, outside the normal map: reached by climbing.
        Box("Plateau ground", root, new Vector2(170, 0), new Vector2(30, 20), new Color(0.38f, 0.36f, 0.34f), -45);
        Wall(root, new Vector2(170, 10.5f), new Vector2(31, 1));
        Wall(root, new Vector2(170, -10.5f), new Vector2(31, 1));
        Wall(root, new Vector2(185.5f, 0), new Vector2(1, 21));
        Wall(root, new Vector2(154.5f, 0), new Vector2(1, 21));
    }

    static void BuildWater(Transform root)
    {
        // The stream is a long band you can walk through (decor only).
        Box("Stream (water)", root, new Vector2(57, 20), new Vector2(5, 70), new Color(0.15f, 0.40f, 0.85f), -20);
        Color waterBlue = new Color(0.35f, 0.65f, 1f);

        Interact(root, "Stream: fill bottle", new Vector2(52, 26), new Vector2(3f, 3f), waterBlue, new ActionData
        {
            name = "Fill the bottle", minutes = 20, energyDelta = -4,
            description = "A stream. The water looks clear, but you can't be sure it is safe.",
            reason = "A walk along the bank",
            requirements = new[] { Req(ItemType.Bottle, 1, false, "Needs a bottle. Search the wreck.") },
            blockedBy = new[] { ItemType.DirtyWater, ItemType.CleanWater },
            blockedHint = "Your bottle is already full.",
            grants = new[] { Give(ItemType.DirtyWater) },
            milestone = "water_filled", oneShot = false,
            resultText = "Bottle filled. Boil it before drinking.",
        });
        Interact(root, "Stream: drink", new Vector2(52, 14), new Vector2(3f, 3f), waterBlue, new ActionData
        {
            name = "Drink straight from the stream", minutes = 5, energyDelta = -15, waterDelta = 30, oneShot = false,
            description = "You are thirsty. But untreated water can make you sick.",
            reason = "Risk: stomach trouble costs energy",
            resultText = "That felt great... for a minute. Your stomach disagrees.",
        });
    }

    // ------------------------------------------------------------------ crash site

    static void BuildCrashSite(Transform root)
    {
        Interact(root, "Phone", new Vector2(2.5f, 0), new Vector2(0.7f, 1.1f), new Color(0.1f, 0.1f, 0.12f), new ActionData
        {
            name = "Check your phone", minutes = 5, energyDelta = -1, oneShot = false,
            description = "3 percent battery. Maybe there is one bar of signal somewhere around here?",
            reason = "Holding it up to the sky",
            milestone = "phone_checked",
            resultText = "No signal. Of course. Maybe stop refreshing it.",
            thought = "\"Emergency calls only\" is a lie. Nobody is picking up that call either.",
        });
        Interact(root, "Luggage", new Vector2(-9, 5), new Vector2(1.4f, 1f), new Color(0.55f, 0.35f, 0.2f), new ActionData
        {
            name = "Search the luggage", minutes = 15, energyDelta = -3,
            description = "A pile of torn suitcases. Something useful might still be in there.",
            reason = "Digging through wreckage",
            grants = new[] { Give(ItemType.Bottle) },
            resultText = "A water bottle. A little dented, but it holds water.",
        });
        Interact(root, "Seats", new Vector2(-17, -6), new Vector2(1.6f, 1f), new Color(0.5f, 0.5f, 0.55f), new ActionData
        {
            name = "Search the seats", minutes = 15, energyDelta = -3,
            description = "Part of the cabin is still intact. Pockets, seat backs, folded tables.",
            reason = "Digging through wreckage",
            grants = new[] { Give(ItemType.Lighter) },
            resultText = "A lighter. Finally something that isn't a phone.",
        });
        Interact(root, "Backpack", new Vector2(-5, -13), new Vector2(1f, 1f), new Color(0.2f, 0.4f, 0.7f), new ActionData
        {
            name = "Take the jacket", minutes = 10, energyDelta = -2, exposureFactor = 0.6f,
            description = "A warm jacket sticks out of someone's backpack.",
            reason = "A short search",
            grants = new[] { Give(ItemType.Jacket) },
            resultText = "You put on the jacket. Nights will be less cold.",
        });
        Interact(root, "Tarp", new Vector2(-12, -3), new Vector2(1.8f, 1.2f), new Color(0.3f, 0.65f, 0.85f), new ActionData
        {
            name = "Cut loose the cabin tarp", minutes = 20, energyDelta = -4,
            description = "A big waterproof sheet from the cargo hold. Useful for shelter. Do not burn it.",
            reason = "Heavy and tangled",
            grants = new[] { Give(ItemType.Tarp) },
            resultText = "You got a tarp. Plastic never belongs in a fire.",
        });
        Interact(root, "Handbook", new Vector2(-20, 8), new Vector2(1f, 0.8f), new Color(0.85f, 0.8f, 0.6f), new ActionData
        {
            name = "Take the crew handbook", minutes = 10, energyDelta = -2,
            description = "A pilot's handbook with a folded paper map of the area.",
            reason = "A short search",
            grants = new[] { Give(ItemType.Map) },
            resultText = "A paper map. It needs no battery.",
        });
        Interact(root, "Magazines", new Vector2(-3, -7), new Vector2(1f, 0.8f), new Color(0.9f, 0.5f, 0.5f), new ActionData
        {
            name = "Tear out dry magazine pages", minutes = 5, energyDelta = -1,
            description = "In-flight magazines, dry inside the cabin. Paper catches fire easily.",
            reason = "Quick and easy",
            grants = new[] { Give(ItemType.Tinder) },
            resultText = "Dry paper: good tinder.",
        });
        Interact(root, "Snack trolley", new Vector2(16, -17), new Vector2(1.4f, 1f), new Color(0.85f, 0.75f, 0.3f), new ActionData
        {
            name = "Eat an airline snack", minutes = 10, energyDelta = -2, foodDelta = 25,
            description = "The trolley survived. Sealed snacks are safe to eat.",
            reason = "A quick break",
            resultText = "Pretzels. Never tasted so good.",
        });
    }

    // ------------------------------------------------------------------ fire materials

    static void BuildMaterials(Transform root)
    {
        Color greenBush = new Color(0.12f, 0.55f, 0.2f);
        Color grass = new Color(0.80f, 0.75f, 0.35f);
        Color twigs = new Color(0.55f, 0.45f, 0.35f);
        Color logCol = new Color(0.45f, 0.30f, 0.15f);
        Color birch = new Color(0.92f, 0.92f, 0.88f);

        // Green bushes: tempting for a fire, but green wood does not burn. Strong poles for a shelter frame, though.
        foreach (var p in new[] { new Vector2(8, 6), new Vector2(10, -4), new Vector2(-3, 10),
                                  new Vector2(-17, 36), new Vector2(-1, 52), new Vector2(-10, 26), new Vector2(46, 24) })
        {
            Interact(root, "Green bush", p, new Vector2(1.5f, 1.5f), greenBush, new ActionData
            {
                name = "Cut green branches", minutes = 10, energyDelta = -3, oneShot = false,
                description = "Fresh branches are easy to reach. But they are full of water: no good for a fire, strong for a shelter frame.",
                reason = "Cutting living wood",
                grants = new[] { Give(ItemType.GreenWood, 3) },
                resultText = "3 green poles: smoky and hard to burn. Good for a frame, not for a fire.",
            });
        }

        // Spruce trees: boughs for a thick roof, a bed and a smoky signal fire. They can be cut again and again.
        foreach (var p in new[] { new Vector2(14, -8), new Vector2(18, -3), new Vector2(2, -19),
                                  new Vector2(-18, 50), new Vector2(-4, 36), new Vector2(-8, 52),
                                  new Vector2(-10, 20), new Vector2(2, 28),
                                  new Vector2(36, -1), new Vector2(41, 26) })
        {
            Interact(root, "Spruce tree", p, new Vector2(1.2f, 2f), new Color(0.05f, 0.35f, 0.18f), new ActionData
            {
                name = "Cut spruce boughs", minutes = 12, energyDelta = -4, oneShot = false,
                description = "Spruce boughs are flexible and layer well. They make a roof, a soft bed and thick smoke.",
                reason = "Cutting and carrying boughs",
                grants = new[] { Give(ItemType.Boughs, 3) },
                resultText = "Three armfuls of spruce boughs.",
            });
        }

        // Dry grass (tinder) near the stream: wet once it rains.
        foreach (var p in new[] { new Vector2(38, 12), new Vector2(34, 2), new Vector2(40, 22) })
        {
            Interact(root, "Dry grass", p, new Vector2(1.6f, 1.2f), grass, new ActionData
            {
                name = "Gather dry grass", minutes = 10, energyDelta = -2,
                description = "Dry, fine grass is excellent tinder.",
                reason = "Pulling grass by hand",
                grants = new[] { Give(ItemType.Tinder) },
                resultText = "A bundle of dry tinder.",
            }, wet: new ActionData
            {
                name = "Gather grass (soaked)", minutes = 10, energyDelta = -2,
                description = "The rain has soaked the grass. It will not catch fire.",
                reason = "Pulling wet grass by hand",
                grants = new[] { Give(ItemType.WetTinder) },
                resultText = "Soaked grass. It will not light.",
            });
        }

        // Dead twigs (kindling) north of the start: wet once it rains.
        foreach (var p in new[] { new Vector2(6, 40), new Vector2(12, 46), new Vector2(2, 48) })
        {
            Interact(root, "Dead twigs", p, new Vector2(1.3f, 1.3f), twigs, new ActionData
            {
                name = "Snap dead twigs", minutes = 10, energyDelta = -3,
                description = "Thin dead twigs from standing trees. They snap cleanly when dry.",
                reason = "Walking between trees, breaking branches",
                grants = new[] { Give(ItemType.Kindling) },
                resultText = "Dry kindling.",
            }, wet: new ActionData
            {
                name = "Snap twigs (soaked)", minutes = 10, energyDelta = -3,
                description = "Rain has soaked the twigs. They bend instead of snapping.",
                reason = "Struggling with wet branches",
                grants = new[] { Give(ItemType.WetKindling) },
                resultText = "Soaked twigs. They will not light.",
            });
        }

        // Dry logs (fuel) far to the north-west: wet once it rains.
        foreach (var p in new[] { new Vector2(-24, 52), new Vector2(-30, 46), new Vector2(-18, 56), new Vector2(-28, 56), new Vector2(-20, 46) })
        {
            Interact(root, "Fallen log", p, new Vector2(2f, 0.9f), logCol, new ActionData
            {
                name = "Drag a dry log", minutes = 15, energyDelta = -5,
                description = "A dead log, dry inside. It will burn for a long time.",
                reason = "Heavy: dragging it over a long way",
                grants = new[] { Give(ItemType.Logs) },
                resultText = "A dry log for the fire.",
            }, wet: new ActionData
            {
                name = "Drag a log (soaked)", minutes = 15, energyDelta = -5,
                description = "Rain has soaked this log through. It will not light.",
                reason = "Heavy and wet",
                grants = new[] { Give(ItemType.WetLogs) },
                resultText = "A wet log. It will not light.",
            });
        }

        // Birch bark: far away and costly, but it burns even in the rain.
        foreach (var p in new[] { new Vector2(-80, 30), new Vector2(-84, 26) })
        {
            Interact(root, "Birch tree", p, new Vector2(0.9f, 1.8f), birch, new ActionData
            {
                name = "Peel birch bark", minutes = 20, energyDelta = -4,
                description = "Birch bark is full of oils. It lights even when damp.",
                reason = "A long way from camp",
                grants = new[] { Give(ItemType.Tinder) },
                resultText = "Birch bark: tinder that works even in the rain.",
            });
        }
    }

    // ------------------------------------------------------------------ camps

    static void BuildCamps(Transform root)
    {
        // Every site starts with NO shelter. The player builds it (frame, roof, bed).
        // The sites differ in their natural conditions and in what is nearby.
        BuildCamp(root, "Camp A", new Vector2(45, 6), "STREAM MEADOW", "water next to you, but low open ground: floods in rain", 1.2f, lowGround: true);
        BuildCamp(root, "Camp B", new Vector2(-10, 42), "FOREST EDGE", "trees break the wind, fuel and boughs nearby", 0.9f);
        BuildCamp(root, "Camp C", new Vector2(170, 0), "ROCK LEDGE", "rock wall behind you, far up the hill, no water", 0.7f);
        BuildCamp(root, "Camp D1", new Vector2(8, -14), "CRASH SITE", "open ground, wreck supplies close by", 1f);
        BuildCamp(root, "Camp D2", new Vector2(-4, 23), "NORTH CLEARING", "open ground on the path north", 1f);

        var top = Anchor(root, "Plateau top", new Vector2(160, -5));
        var bottom = Anchor(root, "Plateau bottom", new Vector2(70, -10));
        Interact(root, "Climb up", new Vector2(70, -12), new Vector2(2f, 1.2f), new Color(0.6f, 0.5f, 0.4f), new ActionData
        {
            name = "Climb up to the rock ledge", minutes = 25, energyDelta = -20, oneShot = false,
            description = "A steep scramble up the hill to a ledge with a rock wall. There is no water and no spruce up there: carry what you need.",
            reason = "Climbing is exhausting",
            resultText = "You made it up. Out of the wind already.",
        }, moveTo: top);
        Interact(root, "Climb down", new Vector2(158, -5), new Vector2(2f, 1.2f), new Color(0.6f, 0.5f, 0.4f), new ActionData
        {
            name = "Climb down", minutes = 15, energyDelta = -10, oneShot = false,
            description = "Back down the rocks to the open land.",
            reason = "Climbing down is easier, but still tiring",
            resultText = "Back on the ground.",
        }, moveTo: bottom);
    }

    static void BuildCamp(Transform root, string name, Vector2 c, string siteName, string siteNote, float siteFactor,
                          bool lowGround = false)
    {
        // The camp area (trigger). The shelter you build is drawn at its right side.
        var zoneGo = new GameObject("Zone: " + name);
        zoneGo.transform.SetParent(root, false);
        zoneGo.transform.position = c;
        var zoneCol = zoneGo.AddComponent<BoxCollider2D>();
        zoneCol.isTrigger = true;
        zoneCol.size = new Vector2(18, 14);
        var shelter = zoneGo.AddComponent<ShelterZone>();
        shelter.siteName = siteName;
        shelter.siteNote = siteNote;
        shelter.siteFactor = siteFactor;
        shelter.lowGround = lowGround;
        shelter.partSprite = square;
        shelter.partMaterial = litMaterial;

        Label(root, siteName, c + new Vector2(0, -5.6f));

        var pit = Box("Fire pit: " + name, root, c, new Vector2(1.6f, 1.6f), new Color(0.25f, 0.2f, 0.18f), 5);
        var fire = pit.AddComponent<Campfire>();

        Color tool = new Color(0.95f, 0.55f, 0.2f);
        Interact(root, name + ": build fire", c + new Vector2(-2.4f, 1.8f), new Vector2(1.2f, 1.2f), tool, new ActionData
        {
            name = "Build a fire", minutes = 20, energyDelta = -4, oneShot = false,
            description = "Lay tinder, add kindling, then logs, and light it. Everything must be dry.",
            reason = "Arranging a fire takes patience",
            fireRule = FireRule.NeedsUnlit, fireEffect = FireEffect.Light,
            requirements = new[]
            {
                Req(ItemType.Tinder, 1, true, "Needs dry tinder (grass, bark or paper). Wet or green material won't catch."),
                Req(ItemType.Kindling, 1, true, "Needs dry kindling: thin dead twigs. Green or wet twigs won't catch."),
                Req(ItemType.Logs, 2, true, "Needs 2 dry logs. Wet logs won't light."),
                Req(ItemType.Lighter, 1, false, "Needs a lighter."),
            },
            milestone = "fire_built",
            resultText = "The fire catches. Warmth, light and a way to boil water.",
        }, fire: fire);
        Interact(root, name + ": add logs", c + new Vector2(-2.4f, -1.8f), new Vector2(1.2f, 1.2f), tool, new ActionData
        {
            name = "Add a log to the fire", minutes = 10, energyDelta = -2, oneShot = false,
            description = "Feed the fire so it lasts longer.",
            reason = "Carefully placing the log",
            fireRule = FireRule.NeedsLit, fireEffect = FireEffect.AddFuel, fuelMinutes = 90,
            requirements = new[] { Req(ItemType.Logs, 1, true, "Needs a dry log.") },
            resultText = "The fire burns brighter.",
        }, fire: fire);
        Interact(root, name + ": boil water", c + new Vector2(0f, 2.8f), new Vector2(1.2f, 1.2f), tool, new ActionData
        {
            name = "Boil the water", minutes = 30, energyDelta = -5, oneShot = false,
            description = "Heat the water over the fire until it boils. Then let it cool.",
            reason = "Waiting for a rolling boil",
            fireRule = FireRule.NeedsLit,
            requirements = new[] { Req(ItemType.DirtyWater, 1, true, "Needs dirty water from the stream.") },
            grants = new[] { Give(ItemType.CleanWater) },
            milestone = "water_boiled",
            resultText = "Safe, boiled water.",
        }, fire: fire);
        Interact(root, name + ": drink", c + new Vector2(0f, -2.8f), new Vector2(1f, 1f), new Color(0.6f, 0.8f, 1f), new ActionData
        {
            name = "Drink the clean water", minutes = 5, energyDelta = 0, waterDelta = 40, oneShot = false,
            description = "Boiled water, cooled down.",
            reason = "A short rest",
            requirements = new[] { Req(ItemType.CleanWater, 1, true, "Needs clean water. Boil dirty water first.") },
            resultText = "Safe water. Pleasantly boring.",
        });

        // The bed spot is under the shelter (to the right of the fire pit).
        Interact(root, name + ": sleep", c + new Vector2(5f, -0.4f), new Vector2(2f, 1f), new Color(0.5f, 0.3f, 0.5f), new ActionData
        {
            name = "Sleep for a while", minutes = 180, energyDelta = 60, oneShot = false, isRest = true,
            description = "Resting restores energy, but the clock keeps running and the night is cold. A built shelter and a fire nearby keep you warm.",
            reason = "3 hours pass: you get thirsty and cold meanwhile",
            milestone = "slept",
            resultText = "You wake up stiff but a bit stronger.",
        });

        // --- Build the shelter: frame, roof, bedding ---
        Color build = new Color(0.30f, 0.65f, 0.55f);
        Interact(root, name + ": frame", c + new Vector2(2.4f, 4.8f), new Vector2(1.4f, 1.2f), build, new ActionData
        {
            name = "Build the shelter frame", minutes = 30, energyDelta = -6, oneShot = false,
            description = "A ridge pole resting on two forked posts. It breaks the wind, but it does not keep the rain out yet.",
            reason = "Cutting, trimming and lashing poles",
            requirements = new[] { Req(ItemType.GreenWood, 3, true, "Needs 3 strong poles. Green branches bend without breaking: cut them from a green bush.") },
            shelterStage = ShelterStage.Frame, milestone = "shelter_frame",
            resultText = "The frame stands. Wind will pass around it. Now it needs a roof.",
        }, zone: shelter);
        Interact(root, name + ": roof (tarp)", c + new Vector2(5f, 4.8f), new Vector2(1.4f, 1.2f), new Color(0.3f, 0.65f, 0.85f), new ActionData
        {
            name = "Hang the tarp as a roof", minutes = 15, energyDelta = -3, oneShot = false,
            description = "Stretch the tarp over the ridge pole and tilt it so rain runs off. Fast, but then you cannot use the tarp as a signal later.",
            reason = "Tying and weighing down the tarp",
            requirements = new[] { Req(ItemType.Tarp, 1, true, "Needs the tarp from the wreck.") },
            shelterStage = ShelterStage.Roof, roofIsTarp = true, milestone = "shelter_roof",
            resultText = "A waterproof roof. The rain will run off it.",
        }, zone: shelter);
        Interact(root, name + ": roof (boughs)", c + new Vector2(7.6f, 4.8f), new Vector2(1.4f, 1.2f), new Color(0.10f, 0.45f, 0.20f), new ActionData
        {
            name = "Thatch a roof with spruce boughs", minutes = 40, energyDelta = -6, oneShot = false,
            description = "Layer boughs like roof tiles, bottom to top, so water runs off. It must be thick: a thin roof leaks.",
            reason = "Many layers, placed one by one",
            requirements = new[] { Req(ItemType.Boughs, 6, true, "Needs 6 spruce boughs. A thin roof leaks, so layer it thick. Cut boughs at spruce trees.") },
            shelterStage = ShelterStage.Roof, milestone = "shelter_roof",
            resultText = "A thick, layered roof. The rain runs off it.",
        }, zone: shelter);
        Interact(root, name + ": bedding", c + new Vector2(8f, -1.4f), new Vector2(1.4f, 1.2f), new Color(0.35f, 0.6f, 0.25f), new ActionData
        {
            name = "Make a bed of spruce boughs", minutes = 20, energyDelta = -3, oneShot = false,
            description = "The cold ground steals more heat than the air does. A layer of boughs under you insulates.",
            reason = "Cutting and laying the boughs flat",
            requirements = new[] { Req(ItemType.Boughs, 3, true, "Needs 3 spruce boughs for a bed.") },
            shelterStage = ShelterStage.Bedding, milestone = "shelter_bed",
            resultText = "A soft, springy bed. The ground can't pull the heat out of you so fast.",
        }, zone: shelter);
    }

    static void Label(Transform root, string text, Vector2 pos)
    {
        var go = new GameObject("Label: " + text);
        go.transform.SetParent(root, false);
        go.transform.position = pos;
        var tm = go.AddComponent<TextMesh>();
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.font = font;
        tm.text = text;
        tm.fontSize = 48;
        tm.characterSize = 0.12f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(1f, 1f, 1f, 0.85f);
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = font.material;
        mr.sortingOrder = 20;
    }

    // ------------------------------------------------------------------ things on the paths

    static void BuildPickups(Transform root)
    {
        // Litter along the paths and at the dead ends. Some of it helps (and teaches), most is useless.
        // They all look alike on purpose: you only find out by picking them up.
        Color litter = new Color(0.82f, 0.82f, 0.78f);
        Color berry = new Color(0.80f, 0.20f, 0.30f);

        // --- Useless junk: only the thought bubble is the reward ---
        Pickup(root, "Odd shoe", new Vector2(26, 1), litter, "Pick up the lone shoe",
               "A single hiking shoe, size 44. No sign of the other one. Or of the owner.",
               "Just one shoe. I won't wonder where the other one is. I won't.",
               item: ItemType.OddShoe);
        Pickup(root, "Guidebook", new Vector2(-35, 1), litter, "Pick up the guidebook",
               "\"Survive in 10 Minutes\", soaked and swollen.",
               "Page 1: \"Stay calm and don't get lost.\" A bit late for page 1.",
               item: ItemType.Guidebook);
        Pickup(root, "Selfie stick", new Vector2(-55, 13), litter, "Pick up the selfie stick",
               "A telescopic selfie stick, fully extended.",
               "Two meters of reach, zero bars of signal. A monument to vanity.",
               item: ItemType.SelfieStick);
        Pickup(root, "Power bank", new Vector2(54, -4), litter, "Pick up the power bank",
               "A power bank. The display says 0%.",
               "Empty. Even the objects out here have given up.",
               item: ItemType.DeadPowerBank);
        Pickup(root, "Beach ball", new Vector2(30, 50), litter, "Pick up the flat ball",
               "A deflated beach ball with a face drawn on it in marker.",
               "Someone drew a face on it. I won't talk to it. Not yet.",
               item: ItemType.BeachBall);
        Pickup(root, "Crashed drone", new Vector2(-40, -24), litter, "Pick up the crashed drone",
               "A drone with a \"SEARCH & RESCUE\" sticker. Its rotors are bent.",
               "A search-and-rescue drone, crashed. Who rescues the rescuers? Not important. Not important at all.",
               item: ItemType.BrokenDrone);

        // --- Useful finds (they teach something) ---
        Pickup(root, "Foil blanket", new Vector2(57, 60), litter, "Wrap up in the foil blanket",
               "A foil emergency blanket. It reflects your body heat and weighs almost nothing.",
               "I look like a baked potato. A baked potato that is trying very hard to stay alive.",
               item: ItemType.SpaceBlanket, warmth: 8, exposure: 0.85f,
               result: "Foil reflects body heat. Warmth loss x0.85 from now on (it does not insulate you from cold ground, though).");
        Pickup(root, "Whistle", new Vector2(-52, -27.5f), litter, "Pick up the whistle",
               "A plastic emergency whistle. Three blasts in a row are the international distress signal.",
               "A whistle. For once, a thing that works exactly as intended.",
               item: ItemType.Whistle,
               result: "A whistle: three blasts are the international distress signal. Keep it for the rescue spot.");
        Pickup(root, "Sealed water", new Vector2(23, -29), litter, "Drink the sealed water bottle",
               "A factory-sealed bottle. Unopened bottled water is safe. Check that the seal is intact.",
               "Sealed. Unlike the stream. I'm learning.",
               water: 30, result: "Safe, sealed water.");
        Pickup(root, "Chocolate", new Vector2(-3, 31), litter, "Eat the chocolate bar",
               "A chocolate bar, a bit crushed. The wrapper is intact.",
               "Expired in 2019. Calories don't expire. That's all I remember from school.",
               food: 20, result: "A small energy boost.");

        // --- Spare clothes from the crash: scattered around, a few hang in trees. Layers keep you warm. ---
        TreeFind(root, "Sweater in a tree", new Vector2(-24, 10), new Color(0.85f, 0.35f, 0.25f), "Climb the tree for the sweater",
                 "A sweater is caught in the branches, thrown there by the crash. Wool keeps warming even when it is damp.",
                 "Somebody's favourite sweater. It smells like a stranger's laundry soap and a little bit like a plane crash.",
                 ItemType.WoolSweater, 0.88f, "Wool sweater on. Layers trap warm air: warmth loss x0.88 more.");
        TreeFind(root, "Socks in a tree", new Vector2(-18, 43), new Color(0.9f, 0.9f, 0.4f), "Climb the tree for the socks",
                 "A pair of dry socks, tangled in a branch. Wet feet get cold fast; dry socks matter.",
                 "Dry socks. The third best invention in history, after fire and the wheel.",
                 ItemType.DrySocks, 0.94f, "Dry socks on. Warmth loss x0.94 more.");
        TreeFind(root, "Gloves in a tree", new Vector2(34, 10), new Color(0.35f, 0.6f, 0.9f), "Climb the tree for the gloves",
                 "One pair of gloves, high up. Hands and fingers get cold first.",
                 "Only the gloves are up here. I won't ask who was wearing them. I won't.",
                 ItemType.Gloves, 0.96f, "Gloves on. Warmth loss x0.96 more.");
        Pickup(root, "Wool hat", new Vector2(-46, 8), litter, "Pick up the wool hat",
               "A bobble hat lying on the path. Heat escapes quickly from an uncovered head.",
               "A bobble hat. A bold choice for a mountain trip. I am in no position to judge.",
               item: ItemType.WoolHat, exposure: 0.94f,
               result: "Wool hat on. Warmth loss x0.94 more.");

        // More places than finds, so every run is different (see FindsScatter).
        foreach (var p in new[] { new Vector2(29, 3), new Vector2(-62, 18), new Vector2(59, 30), new Vector2(62, -9),
                                  new Vector2(18, -25), new Vector2(-30, -19.6f), new Vector2(75, 70), new Vector2(49, 50) })
            Spot(root, p, false);
        foreach (var p in new[] { new Vector2(-25, -15), new Vector2(10, 52), new Vector2(46, 18), new Vector2(74, -8), new Vector2(-33, 49) })
            Spot(root, p, true);

        // --- Decoys: look good, are not ---
        Pickup(root, "Red berries", new Vector2(-69, 22), berry, "Eat the unknown berries",
               "You don't know what these are. Red and pretty: exactly how poisonous plants like to look.",
               "Pretty. Red. Pretty red is never a good sign. And I ate them anyway.",
               food: 5, water: -15, energy: -15,
               result: "Never eat plants you cannot identify. Your stomach disagrees violently.",
               reason: "Risk: stomach trouble");
        Pickup(root, "Red berries", new Vector2(42, 50), berry, "Eat the unknown berries",
               "You don't know what these are. Red and pretty: exactly how poisonous plants like to look.",
               "Pretty. Red. Pretty red is never a good sign. And I ate them anyway.",
               food: 5, water: -15, energy: -15,
               result: "Never eat plants you cannot identify. Your stomach disagrees violently.",
               reason: "Risk: stomach trouble");
    }

    /// <summary>A tree with a piece of clothing caught in it (a coloured scrap shows at the top). Climbing costs energy.</summary>
    static void TreeFind(Transform root, string name, Vector2 pos, Color cloth, string actionName, string description,
                         string thought, ItemType item, float exposure, string result)
    {
        var scrap = Box(name + " (cloth)", root, pos + new Vector2(0.3f, 1.1f), new Vector2(0.7f, 0.7f), cloth, 6);
        var action = new ActionData
        {
            name = actionName, minutes = 20, energyDelta = -7,
            description = description, reason = "Climbing, then shimmying along a branch",
            grants = new[] { Give(item) }, exposureFactor = exposure, resultText = result, thought = thought,
        };
        var tree = Interact(root, name, pos, new Vector2(1.4f, 2.6f), new Color(0.06f, 0.30f, 0.15f), action);
        var find = tree.gameObject.AddComponent<RandomFind>();
        find.inTree = true;
        find.attached = scrap.transform;
        find.attachedOffset = new Vector3(0.3f, 1.1f, 0f);
        Spot(root, pos, true);
    }

    /// <summary>A possible place for a find. Every run, the finds are shuffled onto these spots (see FindsScatter).</summary>
    static void Spot(Transform root, Vector2 pos, bool inTree)
    {
        var go = new GameObject(inTree ? "Find spot (tree)" : "Find spot (ground)");
        go.transform.SetParent(root, false);
        go.transform.position = pos;
        go.AddComponent<FindSpot>().inTree = inTree;
    }

    static void Pickup(Transform root, string name, Vector2 pos, Color color, string actionName, string description, string thought,
                       ItemType item = ItemType.None, float food = 0f, float water = 0f, float warmth = 0f,
                       float energy = -1f, float exposure = 1f, string result = "", string reason = "A short stop")
    {
        var action = new ActionData
        {
            name = actionName, minutes = 3, energyDelta = energy, foodDelta = food, waterDelta = water, warmthDelta = warmth,
            description = description, reason = reason, exposureFactor = exposure,
            resultText = result, thought = thought,
        };
        if (item != ItemType.None) action.grants = new[] { Give(item) };
        var pickup = Interact(root, name, pos, new Vector2(0.9f, 0.9f), color, action);
        pickup.gameObject.AddComponent<RandomFind>().inTree = false;
        Spot(root, pos, false);
    }

    // ------------------------------------------------------------------ rescue point and terrain

    static void BuildRescuePoint(Transform root)
    {
        // Reaching the marker is not enough: you must make a signal and wait to be found.
        Vector2 c = new Vector2(98, 70);
        var marker = Box("Rescue point marker", root, c, new Vector2(4, 4), new Color(0.2f, 1f, 1f), -1);
        marker.AddComponent<RescuePoint>();
        Label(root, "CLEARING (rescue spot)", c + new Vector2(0, -6.5f));

        Interact(root, "Signal fire", c + new Vector2(-4.5f, 3.5f), new Vector2(1.4f, 1.4f), new Color(1f, 0.5f, 0.15f), new ActionData
        {
            name = "Build a signal fire", minutes = 25, energyDelta = -5,
            description = "A big fire in the open, with green boughs on top for thick white smoke. It can be seen from far away.",
            reason = "Stacking fuel in the open",
            requirements = new[]
            {
                Req(ItemType.Tinder, 1, true, "Needs dry tinder."),
                Req(ItemType.Kindling, 1, true, "Needs dry kindling."),
                Req(ItemType.Logs, 2, true, "Needs 2 dry logs."),
                Req(ItemType.Boughs, 2, true, "Needs 2 spruce boughs: green boughs on the fire make thick white smoke."),
                Req(ItemType.Lighter, 1, false, "Needs a lighter."),
            },
            setsFlag = WorldFlag.SignalReady,
            resultText = "Thick smoke rises. Someone may see it.",
        });
        Interact(root, "Signal tarp", c + new Vector2(4.5f, 3.5f), new Vector2(1.8f, 1.2f), new Color(0.3f, 0.65f, 0.85f), new ActionData
        {
            name = "Spread the bright tarp", minutes = 20, energyDelta = -4,
            description = "A big bright shape on open ground is easy to spot from the air.",
            reason = "Smoothing it out and weighing it down with stones",
            requirements = new[] { Req(ItemType.Tarp, 1, false, "Needs the tarp from the wreck. Did you use it for a shelter?") },
            setsFlag = WorldFlag.SignalReady,
            resultText = "The tarp lies flat and bright in the open.",
        });
        Interact(root, "Whistle signal", c + new Vector2(0, 3.5f), new Vector2(1.4f, 1.2f), new Color(0.9f, 0.9f, 0.5f), new ActionData
        {
            name = "Blow three blasts on the whistle", minutes = 10, energyDelta = -2,
            description = "Three sharp blasts, repeated: the international distress signal. It carries far, but only helps if someone is within earshot.",
            reason = "Lungs and patience",
            requirements = new[] { Req(ItemType.Whistle, 1, false, "Needs a whistle.") },
            setsFlag = WorldFlag.SignalReady,
            resultText = "Three blasts, again and again. Somebody may hear it.",
            thought = "Three blasts. Somewhere, a deer is deeply offended.",
        });
        Interact(root, "Wait for rescue", c + new Vector2(0, -4f), new Vector2(2f, 1.2f), new Color(0.5f, 0.9f, 1f), new ActionData
        {
            name = "Wait for rescue", minutes = 120, energyDelta = -6, oneShot = false,
            description = "Stay in the open and keep your signal visible. Walking on makes you harder to find.",
            reason = "Waiting is tiring too",
            requiresFlag = WorldFlag.SignalReady,
            requiresFlagHint = "Make a signal first (fire or tarp), or nobody will see you.",
            onlyFromHour = 8f, onlyUntilHour = 16f,
            timeHint = "Search teams only fly in daylight (08:00 to 16:00). Wait for the morning, or come back earlier.",
            winsGame = true,
            resultText = "You hear rotors. Help has arrived!",
        });
    }

    static void BuildTerrainZones(Transform root)
    {
        // Cold river: the northern stretch of the stream road. Wading chills you and soaks your clothes.
        Box("River (cold section)", root, new Vector2(57, 42f), new Vector2(8, 8), new Color(0.10f, 0.30f, 0.75f), -19);
        var river = new GameObject("Terrain: River");
        river.transform.SetParent(root, false);
        river.transform.position = new Vector2(57, 42f);
        var riverCol = river.AddComponent<BoxCollider2D>();
        riverCol.isTrigger = true;
        riverCol.size = new Vector2(8, 8);
        var riverZone = river.AddComponent<TerrainZone>();
        riverZone.label = "River (cold water)";
        riverZone.energyMultiplier = 1.2f;
        riverZone.speedMultiplier = 0.8f;
        riverZone.warmthPerUnit = 0.3f;
        riverZone.entryWarmthLoss = 12f;
        riverZone.makesWet = true;
        riverZone.enterMessage = "You wade into the icy river. Your clothes are soaked!";

        // Thick forest trail: the shortcut to the rescue point. Shorter, but slow and tiring.
        for (int i = 0; i < TrailThick.Length - 1; i++)
        {
            Vector2 a = TrailThick[i], b = TrailThick[i + 1];
            Vector2 mid = (a + b) / 2f;
            float len = Vector2.Distance(a, b) + 2f;
            float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;

            var look = Box("Thick trail", root, mid, new Vector2(len, 4.5f), new Color(0.22f, 0.30f, 0.14f), -39);
            look.transform.rotation = Quaternion.Euler(0, 0, angle);

            var zone = new GameObject("Terrain: Thick trail");
            zone.transform.SetParent(root, false);
            zone.transform.position = mid;
            zone.transform.rotation = Quaternion.Euler(0, 0, angle);
            var col = zone.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(len, 5f);
            var tz = zone.AddComponent<TerrainZone>();
            tz.label = "Thick forest trail (slow, tiring)";
            tz.energyMultiplier = 1.5f;
            tz.speedMultiplier = 0.75f;
        }
    }

    // ------------------------------------------------------------------ forest, paths, map edge

    const float Cell = 3f;            // size of one forest block
    const float PathHalfWidth = 3.5f; // paths are about 7 units wide (the camera shows 21 x 12)
    const float EdgeBand = 9f;        // forest wall thickness at the map edge (always solid)

    // Open areas (center, half size). Everything outside these and the paths is solid forest.
    static readonly Rect[] Clearings =
    {
        new Rect(-27, -21, 50, 34),     // crash site
        new Rect(30, -4, 24, 34),       // meadow by the stream (camp A, dry grass, stream)
        new Rect(-21, 33, 34, 22),      // forest edge (camp B, dead twigs)
        new Rect(-34, 43, 20, 16),      // log field
        new Rect(-90, 21, 16, 14),      // birch grove
        new Rect(61, -16, 18, 12),      // foot of the hill (climb up)
        new Rect(90, 62, 16, 16),       // rescue point (far north-east)
        new Rect(-14, 16, 20, 14),      // camp D2 (open ground on the way north)
    };

    // The forest shortcut to the rescue point: shorter than the other routes, but slow and tiring (TerrainZone).
    static readonly Vector2[] TrailThick = { new Vector2(50, 28), new Vector2(72, 40), new Vector2(88, 56), new Vector2(96, 66) };

    // Paths: polylines of waypoints. They are drawn as brown dirt roads and kept free of trees.
    static readonly Vector2[][] Paths =
    {
        new[] { new Vector2(20, -2), new Vector2(32, 4) },                                              // crash site <-> meadow
        new[] { new Vector2(0, 10), new Vector2(-2, 24), new Vector2(-4, 34) },                         // crash site -> north
        new[] { new Vector2(-24, -4), new Vector2(-45, 6), new Vector2(-62, 18), new Vector2(-76, 25) },// crash site -> birch grove
        new[] { new Vector2(50, 0), new Vector2(58, -8), new Vector2(64, -9) },                         // meadow -> hill
        new[] { new Vector2(57, -14), new Vector2(57, 56) },                                            // along the stream (north-south)
        new[] { new Vector2(57, 50), new Vector2(57, 70), new Vector2(90, 70) },                        // stream -> rescue point
        new[] { new Vector2(12, 46), new Vector2(30, 50), new Vector2(50, 50), new Vector2(57, 50) },   // forest edge -> stream (long way round)
        new[] { new Vector2(-22, -16), new Vector2(-40, -24), new Vector2(-54, -28) },                  // dead end to the lake
        new[] { new Vector2(12, -20), new Vector2(24, -30) },                                           // dead end to the big rock
        TrailThick,                                                                                     // forest shortcut to the rescue point
    };

    static void BuildForest(Transform root)
    {
        // 1) Brown dirt roads along the paths (decor).
        foreach (var path in Paths)
        {
            for (int i = 0; i < path.Length - 1; i++)
            {
                Vector2 a = path[i], b = path[i + 1];
                Vector2 mid = (a + b) / 2f;
                float len = Vector2.Distance(a, b) + PathHalfWidth;
                var road = Box("Path", root, mid, new Vector2(len, PathHalfWidth * 1.6f), new Color(0.52f, 0.42f, 0.27f), -40);
                road.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
            }
        }

        // 2) Solid forest: walk a grid, block every cell that is not open, merge neighbours in a row.
        int cols = Mathf.RoundToInt(HalfW * 2 / Cell);
        int rows = Mathf.RoundToInt(HalfH * 2 / Cell);
        var rng = new System.Random(7);   // fixed seed: the map is always the same
        int blocks = 0, treeSprites = 0;

        for (int j = 0; j < rows; j++)
        {
            int runStart = -1;
            for (int i = 0; i <= cols; i++)
            {
                bool blocked = i < cols && IsBlocked(CellCenter(i, j));
                if (blocked && runStart < 0) runStart = i;
                if (!blocked && runStart >= 0)
                {
                    int len = i - runStart;
                    float x = -HalfW + Cell * (runStart + len / 2f);
                    float y = -HalfH + Cell * (j + 0.5f);
                    var block = Box("Forest", root, new Vector2(x, y), new Vector2(Cell * len, Cell), new Color(0.07f, 0.18f, 0.09f), 0);
                    block.AddComponent<BoxCollider2D>();
                    blocks++;
                    runStart = -1;
                }
            }
        }

        // 3) Tree sprites on top of the forest (no collision). Denser along the path edges so the way is easy to read.
        for (int j = 0; j < rows; j++)
        {
            for (int i = 0; i < cols; i++)
            {
                Vector2 c = CellCenter(i, j);
                if (!IsBlocked(c)) continue;
                bool nearOpen = !IsBlocked(c + Vector2.right * Cell) || !IsBlocked(c + Vector2.left * Cell) ||
                                !IsBlocked(c + Vector2.up * Cell) || !IsBlocked(c + Vector2.down * Cell);
                if (rng.NextDouble() > (nearOpen ? 0.95 : 0.30)) continue;
                float size = 2.2f + (float)rng.NextDouble() * 1.0f;
                float shade = (float)rng.NextDouble();
                var color = new Color(0.10f + 0.06f * shade, 0.28f + 0.14f * shade, 0.12f + 0.06f * shade);
                var offset = new Vector2((float)(rng.NextDouble() - 0.5) * 1.2f, (float)(rng.NextDouble() - 0.5) * 1.2f);
                Box("Tree", root, c + offset, new Vector2(size, size), color, 2);
                treeSprites++;
            }
        }
        Debug.Log($"Forest built: {blocks} solid blocks, {treeSprites} tree sprites.");
    }

    static Vector2 CellCenter(int i, int j)
    {
        return new Vector2(-HalfW + Cell * (i + 0.5f), -HalfH + Cell * (j + 0.5f));
    }

    /// <summary>True if this spot is solid forest (not a clearing, not a path, or part of the map-edge wall).</summary>
    static bool IsBlocked(Vector2 p)
    {
        if (Mathf.Abs(p.x) > HalfW - EdgeBand || Mathf.Abs(p.y) > HalfH - EdgeBand) return true;
        foreach (var r in Clearings) if (r.Contains(p)) return false;
        foreach (var path in Paths)
            for (int i = 0; i < path.Length - 1; i++)
                if (DistanceToSegment(p, path[i], path[i + 1]) <= PathHalfWidth) return false;
        return true;
    }

    static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    // ------------------------------------------------------------------ small helpers

    static ItemRequirement Req(ItemType item, int count, bool consume, string hint)
    {
        return new ItemRequirement { item = item, count = count, consume = consume, missingHint = hint };
    }

    static ItemGrant Give(ItemType item, int count = 1)
    {
        return new ItemGrant { item = item, count = count };
    }

    static GameObject Box(string name, Transform parent, Vector2 pos, Vector2 size, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = color;
        sr.sortingOrder = order;
        if (litMaterial != null) sr.sharedMaterial = litMaterial;
        return go;
    }

    static void Solid(string name, Transform parent, Vector2 pos, Vector2 size, Color color)
    {
        var go = Box(name, parent, pos, size, color, 3);   // drawn above the forest so landmarks stay visible
        go.AddComponent<BoxCollider2D>();
    }

    static void Wall(Transform parent, Vector2 pos, Vector2 size)
    {
        var go = new GameObject("Wall");
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.AddComponent<BoxCollider2D>().size = size;
    }

    static Transform Anchor(Transform parent, string name, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        return go.transform;
    }

    static Interactable Interact(Transform parent, string name, Vector2 pos, Vector2 size, Color color, ActionData action,
                                 ActionData wet = null, Campfire fire = null, Transform moveTo = null, ShelterZone zone = null)
    {
        var go = Box(name, parent, pos, size, color, 5);   // above the tree sprites
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = Vector2.one * 1.3f;
        var item = go.AddComponent<Interactable>();
        item.action = action;
        if (wet != null)
        {
            item.hasWetAction = true;
            item.wetAction = wet;
        }
        item.fire = fire;
        item.moveTo = moveTo;
        item.zone = zone;
        return item;
    }

    static void SetupCameraAndLight(Transform player)
    {
        // Camera (create one if the scene has none)
        var cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0, 0, -10);
        }
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        var follow = cam.GetComponent<CameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.target = player;

        // Global light (create one if the scene has none)
        bool hasGlobal = false;
        foreach (var l in Object.FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>(FindObjectsSortMode.None))
            if (l.lightType == UnityEngine.Rendering.Universal.Light2D.LightType.Global) hasGlobal = true;
        if (!hasGlobal)
        {
            var lightGo = new GameObject("Global Light 2D");
            var light = lightGo.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Global;
        }
    }

    static void SaveScene()
    {
        // Save without a dialog: an untitled scene gets a real path.
        var scene = EditorSceneManager.GetActiveScene();
        const string scenePath = "Assets/Scenes/Game.unity";
        if (string.IsNullOrEmpty(scene.path))
        {
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, scenePath);
        }
        else
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(EditorSceneManager.GetActiveScene().path, true) };
        Debug.Log("Greybox scene built and saved: " + EditorSceneManager.GetActiveScene().path);
    }

    // ------------------------------------------------------------------ placeholder art

    static Sprite EnsureSprite()
    {
        Directory.CreateDirectory(ArtDir);
        string path = ArtDir + "/white16.png";
        if (!File.Exists(path))
        {
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            var px = new Color[16 * 16];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Material EnsureMaterial()
    {
        string path = ArtDir + "/SpriteLit.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        if (shader == null) return null;
        mat = new Material(shader);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
