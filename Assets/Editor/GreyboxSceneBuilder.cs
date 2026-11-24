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

        // --- Rescue point ---
        var exit = Box("Rescue point", root, new Vector2(92, 56), new Vector2(4, 4), new Color(0.2f, 1f, 1f), -1);
        exit.AddComponent<BoxCollider2D>().isTrigger = true;
        exit.AddComponent<ExitZone>();

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
            name = "Fill the bottle", minutes = 20, energyDelta = -6,
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
        });
        Interact(root, "Luggage", new Vector2(-9, 5), new Vector2(1.4f, 1f), new Color(0.55f, 0.35f, 0.2f), new ActionData
        {
            name = "Search the luggage", minutes = 15, energyDelta = -4,
            description = "A pile of torn suitcases. Something useful might still be in there.",
            reason = "Digging through wreckage",
            grants = new[] { Give(ItemType.Bottle) },
            resultText = "A water bottle. A little dented, but it holds water.",
        });
        Interact(root, "Seats", new Vector2(-17, -6), new Vector2(1.6f, 1f), new Color(0.5f, 0.5f, 0.55f), new ActionData
        {
            name = "Search the seats", minutes = 15, energyDelta = -4,
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
            name = "Cut loose the cabin tarp", minutes = 20, energyDelta = -6,
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

        // Green bushes close to the start: tempting, but green wood does not burn.
        foreach (var p in new[] { new Vector2(8, 6), new Vector2(10, -4), new Vector2(-3, 10) })
        {
            Interact(root, "Green bush", p, new Vector2(1.5f, 1.5f), greenBush, new ActionData
            {
                name = "Cut green branches", minutes = 10, energyDelta = -4,
                description = "Fresh branches are easy to reach. But they are full of water.",
                reason = "Cutting living wood",
                grants = new[] { Give(ItemType.GreenWood) },
                resultText = "Green wood: smoky and hard to burn. Good for a frame, not for a fire.",
            });
        }

        // Dry grass (tinder) near the stream: wet once it rains.
        foreach (var p in new[] { new Vector2(38, 12), new Vector2(34, 2), new Vector2(40, 22) })
        {
            Interact(root, "Dry grass", p, new Vector2(1.6f, 1.2f), grass, new ActionData
            {
                name = "Gather dry grass", minutes = 10, energyDelta = -3,
                description = "Dry, fine grass is excellent tinder.",
                reason = "Pulling grass by hand",
                grants = new[] { Give(ItemType.Tinder) },
                resultText = "A bundle of dry tinder.",
            }, wet: new ActionData
            {
                name = "Gather grass (soaked)", minutes = 10, energyDelta = -3,
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
                name = "Snap dead twigs", minutes = 10, energyDelta = -4,
                description = "Thin dead twigs from standing trees. They snap cleanly when dry.",
                reason = "Walking between trees, breaking branches",
                grants = new[] { Give(ItemType.Kindling) },
                resultText = "Dry kindling.",
            }, wet: new ActionData
            {
                name = "Snap twigs (soaked)", minutes = 10, energyDelta = -4,
                description = "Rain has soaked the twigs. They bend instead of snapping.",
                reason = "Struggling with wet branches",
                grants = new[] { Give(ItemType.WetKindling) },
                resultText = "Soaked twigs. They will not light.",
            });
        }

        // Dry logs (fuel) far to the north-west: wet once it rains.
        foreach (var p in new[] { new Vector2(-24, 52), new Vector2(-30, 46), new Vector2(-18, 56) })
        {
            Interact(root, "Fallen log", p, new Vector2(2f, 0.9f), logCol, new ActionData
            {
                name = "Drag a dry log", minutes = 15, energyDelta = -8,
                description = "A dead log, dry inside. It will burn for a long time.",
                reason = "Heavy: dragging it over a long way",
                grants = new[] { Give(ItemType.Logs) },
                resultText = "A dry log for the fire.",
            }, wet: new ActionData
            {
                name = "Drag a log (soaked)", minutes = 15, energyDelta = -8,
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
                name = "Peel birch bark", minutes = 20, energyDelta = -6,
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
        // A: close to water, but open and windy.  B: forest edge, some cover, fuel nearby.
        // C: dry rock overhang on a plateau: warmest, but far, a climb away and no water.
        BuildCamp(root, "Camp A", new Vector2(45, 6), 1.5f, "Meadow by the stream (exposed)");
        BuildCamp(root, "Camp B", new Vector2(-10, 42), 0.9f, "Forest edge (some cover)");
        BuildCamp(root, "Camp C", new Vector2(170, 0), 0.5f, "Rock overhang (dry, windproof)");

        var top = Anchor(root, "Plateau top", new Vector2(160, -5));
        var bottom = Anchor(root, "Plateau bottom", new Vector2(70, -10));
        Interact(root, "Climb up", new Vector2(70, -12), new Vector2(2f, 1.2f), new Color(0.6f, 0.5f, 0.4f), new ActionData
        {
            name = "Climb up to the rock overhang", minutes = 25, energyDelta = -20, oneShot = false,
            description = "A steep scramble up the hill to a dry overhang. There is no water up there.",
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

    static void BuildCamp(Transform root, string name, Vector2 c, float exposure, string zoneLabel)
    {
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
        Interact(root, name + ": add logs", c + new Vector2(2.4f, 1.8f), new Vector2(1.2f, 1.2f), tool, new ActionData
        {
            name = "Add a log to the fire", minutes = 10, energyDelta = -2, oneShot = false,
            description = "Feed the fire so it lasts longer.",
            reason = "Carefully placing the log",
            fireRule = FireRule.NeedsLit, fireEffect = FireEffect.AddFuel, fuelMinutes = 90,
            requirements = new[] { Req(ItemType.Logs, 1, true, "Needs a dry log.") },
            resultText = "The fire burns brighter.",
        }, fire: fire);
        Interact(root, name + ": boil water", c + new Vector2(-2.4f, -1.8f), new Vector2(1.2f, 1.2f), tool, new ActionData
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
        Interact(root, name + ": drink", c + new Vector2(2.4f, -1.8f), new Vector2(1f, 1f), new Color(0.6f, 0.8f, 1f), new ActionData
        {
            name = "Drink the clean water", minutes = 5, energyDelta = 0, waterDelta = 40, oneShot = false,
            description = "Boiled water, cooled down.",
            reason = "A short rest",
            requirements = new[] { Req(ItemType.CleanWater, 1, true, "Needs clean water. Boil dirty water first.") },
            resultText = "Safe water. Pleasantly boring.",
        });
        Interact(root, name + ": sleep", c + new Vector2(0, -3.4f), new Vector2(1.8f, 1f), new Color(0.5f, 0.3f, 0.5f), new ActionData
        {
            name = "Sleep for a while", minutes = 180, energyDelta = 50, oneShot = false, isRest = true,
            description = "Resting restores energy, but the clock keeps running and the night is cold.",
            reason = "3 hours pass: you get thirsty and cold meanwhile",
            milestone = "slept",
            resultText = "You wake up stiff but a bit stronger.",
        });

        var zone = new GameObject("Zone: " + name);
        zone.transform.SetParent(root, false);
        zone.transform.position = c;
        var col = zone.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(14, 12);
        var shelter = zone.AddComponent<ShelterZone>();
        shelter.label = zoneLabel;
        shelter.exposureFactor = exposure;
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
        new Rect(84, 48, 16, 16),       // rescue point
    };

    // Paths: polylines of waypoints. They are drawn as brown dirt roads and kept free of trees.
    static readonly Vector2[][] Paths =
    {
        new[] { new Vector2(20, -2), new Vector2(32, 4) },                                              // crash site <-> meadow
        new[] { new Vector2(0, 10), new Vector2(-2, 24), new Vector2(-4, 34) },                         // crash site -> north
        new[] { new Vector2(-24, -4), new Vector2(-45, 6), new Vector2(-62, 18), new Vector2(-76, 25) },// crash site -> birch grove
        new[] { new Vector2(50, 0), new Vector2(58, -8), new Vector2(64, -9) },                         // meadow -> hill
        new[] { new Vector2(57, -14), new Vector2(57, 56) },                                            // along the stream (north-south)
        new[] { new Vector2(57, 50), new Vector2(75, 52), new Vector2(86, 56) },                        // stream -> rescue point
        new[] { new Vector2(12, 46), new Vector2(30, 50), new Vector2(50, 50), new Vector2(57, 50) },   // forest edge -> stream (long way round)
        new[] { new Vector2(-22, -16), new Vector2(-40, -24), new Vector2(-54, -28) },                  // dead end to the lake
        new[] { new Vector2(12, -20), new Vector2(24, -30) },                                           // dead end to the big rock
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
                                 ActionData wet = null, Campfire fire = null, Transform moveTo = null)
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
