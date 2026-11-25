using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Prettier ground: soft dirt paths and a meadow with grass tufts.
///
/// DirtPath_* is a Rule Tile: you paint ONE tile and Unity picks the right edge, corner, end or fill piece by itself,
/// so paths get soft grassy borders. Meadow_* is a Rule Tile that puts plain grass and grass tufts at random.
/// Survival > Ground > ... converts the existing paths, changes the ground colour and paints the meadow.
/// The sprites come from TilesetFloor (two colour sets: olive and green, same layout).
/// </summary>
public static class GroundTools
{
    const string Dir = "Assets/Tiles/Ground";
    const string Sheet = "Pixel/Tilesets/TilesetFloor";

    struct Rule
    {
        public string Pattern;          // neighbours NW N NE W E SW S SE: 1 = dirt, 2 = no dirt, 0 = does not matter
        public List<Vector2Int> Sprites;
    }

    static Rule R(string pattern, params int[] colRow)
    {
        var list = new List<Vector2Int>();
        for (int i = 0; i + 1 < colRow.Length; i += 2) list.Add(new Vector2Int(colRow[i], colRow[i + 1]));
        return new Rule { Pattern = pattern, Sprites = list };
    }

    // column, row of the olive set in TilesetFloor (the green set is the same, 11 columns to the right)
    static readonly Rule[] DirtRules =
    {
        R("02022020", 3, 10),   // single block
        R("02022010", 3, 7),    // end, open at the top
        R("01022020", 3, 9),    // end, open at the bottom
        R("01022010", 3, 8),    // vertical path
        R("02021020", 0, 10),   // end, open on the left
        R("02012020", 2, 10),   // end, open on the right
        R("02011020", 1, 10),   // horizontal path
        R("02021010", 0, 7),    // corner top-left
        R("02012010", 2, 7),    // corner top-right
        R("01021020", 0, 9),    // corner bottom-left
        R("01012020", 2, 9),    // corner bottom-right
        R("02011010", 1, 7),    // edge top
        R("01011020", 1, 9),    // edge bottom
        R("01021010", 0, 8),    // edge left
        R("01012010", 2, 8),    // edge right
        R("21111111", 6, 9),    // inner corner top-left (a bit of grass pokes in)
        R("11211111", 5, 9),    // inner corner top-right
        R("11111211", 6, 8),    // inner corner bottom-left
        R("11111112", 5, 8),    // inner corner bottom-right
        R("01011010", 1, 8, 1, 8, 1, 8, 1, 8, 1, 8, 1, 8, 1, 8, 1, 8, 1, 8, 1, 8, 0, 11, 1, 11),   // fill (mostly plain, sometimes a twig or stone)
        R("00000000", 1, 8),    // anything else
    };

    // TilesetWater, set with the grass shore: same layout as the dirt set, one row higher (no single block)
    const string WaterSheet = "Pixel/Tilesets/TilesetWaterBank";   // TilesetWater with the grass around the bank made transparent
    static readonly Rule[] WaterRules =
    {
        R("02022020", 1, 9),    // single cell
        R("02022010", 3, 6),    // end, open at the top
        R("01022020", 3, 8),    // end, open at the bottom
        R("01022010", 3, 7),    // vertical stream
        R("02021020", 0, 9),    // end, open on the left
        R("02012020", 2, 9),    // end, open on the right
        R("02011020", 1, 9),    // horizontal stream
        R("02021010", 0, 6),    // corner top-left
        R("02012010", 2, 6),    // corner top-right
        R("01021020", 0, 8),    // corner bottom-left
        R("01012020", 2, 8),    // corner bottom-right
        R("02011010", 1, 6),    // shore top
        R("01011020", 1, 8),    // shore bottom
        R("01021010", 0, 7),    // shore left
        R("01012010", 2, 7),    // shore right
        R("21111111", 6, 8),    // inner corner top-left (a bit of land pokes in)
        R("11211111", 5, 8),    // inner corner top-right
        R("11111211", 6, 7),    // inner corner bottom-left
        R("11111112", 5, 7),    // inner corner bottom-right
        R("00000000", 1, 7),    // open water
    };

    [MenuItem("Survival/Ground/Soft Paths + Olive Meadow")]
    public static void ApplyOlive() => Apply("Olive", 0);

    [MenuItem("Survival/Ground/Soft Paths + Green Meadow")]
    public static void ApplyGreen() => Apply("Green", 11);

    [MenuItem("Survival/Ground/Remove Meadow Tufts")]
    public static void RemoveMeadow()
    {
        var go = GameObject.Find("Grid: Meadow (cell 1)");
        if (go == null) { Debug.Log("Ground: no meadow in this scene."); return; }
        Undo.DestroyObjectImmediate(go);
        EditorSceneManager.MarkSceneDirty(go.scene);
    }

    static Sprite S(int ox, int col, int row) => S(Sheet, ox, col, row);

    static Sprite S(string sheet, int ox, int col, int row)
    {
        var s = PixelSheets.Find(sheet, (ox + col) * 16, row * 16, 16, 16);
        if (s == null) throw new System.Exception("Ground: sprite " + sheet + " " + (ox + col) + "_" + row + " not found. Is the sheet cut? (Survival > Make Level Editable)");
        return s;
    }

    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        Directory.CreateDirectory(Dir);
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }

    static RuleTile.TilingRule MakeRule(string pattern, Sprite[] sprites, int id)
    {
        return new RuleTile.TilingRule
        {
            m_Id = id,
            m_Sprites = sprites,
            m_Output = sprites.Length > 1 ? RuleTile.TilingRuleOutput.OutputSprite.Random : RuleTile.TilingRuleOutput.OutputSprite.Single,
            m_ColliderType = Tile.ColliderType.None,
            m_Neighbors = pattern.Select(ch => (int)(ch - '0')).ToList(),
        };
    }

    public static RuleTile MakeDirtPath(string theme, int ox)
    {
        var tile = LoadOrCreate<RuleTile>($"{Dir}/DirtPath_{theme}.asset");
        tile.m_DefaultSprite = S(ox, 1, 8);
        tile.m_DefaultColliderType = Tile.ColliderType.None;
        tile.m_TilingRules = new List<RuleTile.TilingRule>();
        for (int i = 0; i < DirtRules.Length; i++)
            tile.m_TilingRules.Add(MakeRule(DirtRules[i].Pattern, DirtRules[i].Sprites.Select(v => S(ox, v.x, v.y)).ToArray(), i));
        EditorUtility.SetDirty(tile);
        return tile;
    }

    /// <summary>Cuts TilesetWaterBank into 16x16 sprites if that has not happened yet.</summary>
    static void EnsureBankSheet()
    {
        if (PixelSheets.Find(WaterSheet, 16, 96, 16, 16) != null) return;
        AssetDatabase.ImportAsset("Assets/Resources/Pixel/Tilesets/TilesetWaterBank.png", ImportAssetOptions.ForceSynchronousImport);
        PixelSheets.Slice(WaterSheet, null);
    }

    public static RuleTile MakeWater()
    {
        EnsureBankSheet();
        var tile = LoadOrCreate<RuleTile>($"{Dir}/Water_Pond.asset");
        tile.m_DefaultSprite = S(WaterSheet, 0, 1, 7);
        tile.m_DefaultColliderType = Tile.ColliderType.None;
        tile.m_TilingRules = new List<RuleTile.TilingRule>();
        for (int i = 0; i < WaterRules.Length; i++)
            tile.m_TilingRules.Add(MakeRule(WaterRules[i].Pattern, WaterRules[i].Sprites.Select(v => S(WaterSheet, 0, v.x, v.y)).ToArray(), i));
        EditorUtility.SetDirty(tile);
        return tile;
    }

    public static RuleTile MakeMeadow(string theme, int ox)
    {
        var tile = LoadOrCreate<RuleTile>($"{Dir}/Meadow_{theme}.asset");
        var plain = S(ox, 0, 12);
        var sprites = new List<Sprite>();
        for (int i = 0; i < 14; i++) sprites.Add(plain);                              // about 4 of 5 cells stay plain
        for (int c = 1; c <= 4; c++) sprites.Add(S(ox, c, 12));                       // four kinds of grass tufts
        tile.m_DefaultSprite = plain;
        tile.m_DefaultColliderType = Tile.ColliderType.None;
        tile.m_TilingRules = new List<RuleTile.TilingRule> { MakeRule("00000000", sprites.ToArray(), 0) };
        EditorUtility.SetDirty(tile);
        return tile;
    }

    static void Apply(string theme, int ox)
    {
        var scene = EditorSceneManager.GetActiveScene();
        var tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var paths = tilemaps.FirstOrDefault(t => t.name == "Paths");
        if (paths == null)
        {
            EditorUtility.DisplayDialog("Ground", "There is no tilemap called \"Paths\" in the open scene. Open Assets/Scenes/Level.unity first.", "OK");
            return;
        }

        RuleTile dirt = MakeDirtPath(theme, ox);
        RuleTile meadowTile = MakeMeadow(theme, ox);
        AssetDatabase.SaveAssets();

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Soft paths and meadow");

        // 1. ground pieces: plain grass of this colour set
        var pieces = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(r => r.gameObject.name == "Ground piece").ToArray();
        var plainGrass = S(ox, 0, 12);
        foreach (var r in pieces)
        {
            Undo.RecordObject(r, "Ground sprite");
            r.sprite = plainGrass;
            EditorUtility.SetDirty(r);
        }

        // 2. paths: every old dirt tile (or the other colour set) becomes the soft dirt path
        Undo.RegisterCompleteObjectUndo(paths, "Soft paths");
        paths.CompressBounds();
        var pos = new List<Vector3Int>();
        foreach (var p in paths.cellBounds.allPositionsWithin)
        {
            var t = paths.GetTile(p);
            if (t == null) continue;
            if (t.name.StartsWith("Dirt_") || t.name.StartsWith("DirtPath_")) pos.Add(p);
        }
        paths.SetTiles(pos.ToArray(), pos.Select(_ => (TileBase)dirt).ToArray());
        paths.RefreshAllTiles();

        // 3. meadow: a tilemap under the paths with random grass tufts over the whole ground
        var meadow = tilemaps.FirstOrDefault(t => t.name == "Meadow");
        if (meadow == null)
        {
            var gridGo = new GameObject("Grid: Meadow (cell 1)");
            var pathsGrid = paths.transform.parent;
            gridGo.transform.SetParent(pathsGrid != null ? pathsGrid.parent : null, false);
            if (pathsGrid != null) gridGo.transform.position = pathsGrid.position;
            gridGo.AddComponent<Grid>().cellSize = new Vector3(1f, 1f, 0f);
            var go = new GameObject("Meadow");
            go.transform.SetParent(gridGo.transform, false);
            meadow = go.AddComponent<Tilemap>();
            var rend = go.AddComponent<TilemapRenderer>();
            rend.sortingOrder = -49;   // above the ground (-50), below the paths (-44)
            var pathsRend = paths.GetComponent<TilemapRenderer>();
            if (pathsRend != null) rend.sharedMaterial = pathsRend.sharedMaterial;
            Undo.RegisterCreatedObjectUndo(gridGo, "Meadow");
        }
        meadow.ClearAllTiles();
        var cells = new HashSet<Vector3Int>();
        foreach (var r in pieces)
        {
            var b = r.bounds;
            var min = meadow.WorldToCell(new Vector3(b.min.x + 0.01f, b.min.y + 0.01f, 0f));
            var max = meadow.WorldToCell(new Vector3(b.max.x - 0.01f, b.max.y - 0.01f, 0f));
            for (int y = min.y; y <= max.y; y++)
                for (int x = min.x; x <= max.x; x++) cells.Add(new Vector3Int(x, y, 0));
        }
        var arr = cells.ToArray();
        meadow.SetTiles(arr, arr.Select(_ => (TileBase)meadowTile).ToArray());

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"Ground ({theme}): {pos.Count} path tiles made soft, {pieces.Length} ground pieces recoloured, {arr.Length} meadow cells painted. Save with Cmd+S.");
    }

    static Tilemap FindOrCreateTilemap(string name, string gridName, int sortingOrder, Tilemap like)
    {
        var existing = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == name);
        if (existing != null) return existing;
        var gridGo = new GameObject(gridName);
        var likeGrid = like != null ? like.transform.parent : null;
        gridGo.transform.SetParent(likeGrid != null ? likeGrid.parent : null, false);
        if (likeGrid != null) gridGo.transform.position = likeGrid.position;
        gridGo.AddComponent<Grid>().cellSize = new Vector3(1f, 1f, 0f);
        var go = new GameObject(name);
        go.transform.SetParent(gridGo.transform, false);
        var tm = go.AddComponent<Tilemap>();
        var rend = go.AddComponent<TilemapRenderer>();
        rend.sortingOrder = sortingOrder;
        var likeRend = like != null ? like.GetComponent<TilemapRenderer>() : null;
        if (likeRend != null) rend.sharedMaterial = likeRend.sharedMaterial;
        Undo.RegisterCreatedObjectUndo(gridGo, name);
        return tm;
    }

    /// <summary>Creates the water tile and an empty "Water" tilemap to paint on (nothing else in the scene changes).</summary>
    [MenuItem("Survival/Ground/Create Water Tile + Water Tilemap")]
    public static void CreateWater()
    {
        var paths = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == "Paths");
        MakeWater();
        AssetDatabase.SaveAssets();
        FindOrCreateTilemap("Water", "Grid: Water (cell 1)", -20, paths);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Ground: water tile is Assets/Tiles/Ground/Water_Pond.asset. Drag it into the Tile Palette and paint on the tilemap \"Water\". Save with Cmd+S.");
    }

    /// <summary>
    /// Paints the old blue water rectangles (stream, river, lake) as soft water tiles on the "Water" tilemap and hides the old
    /// rectangles. Their colliders and the cold-river zone stay, so the game works as before. Needs the olive grass.
    /// </summary>
    [MenuItem("Survival/Ground/Soft Water From Old Water Rectangles")]
    public static void ConvertWater()
    {
        var paths = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == "Paths");
        var water = MakeWater();
        AssetDatabase.SaveAssets();
        var tm = FindOrCreateTilemap("Water", "Grid: Water (cell 1)", -20, paths);
        Undo.RegisterCompleteObjectUndo(tm, "Soft water");

        string[] names = { "Stream (water)", "River (cold section)", "Landmark: lake" };
        var renderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(r => names.Contains(r.gameObject.name)).ToArray();
        var cells = new HashSet<Vector3Int>();
        foreach (var r in renderers)
        {
            var b = r.bounds;
            var min = tm.WorldToCell(new Vector3(b.min.x + 0.5f, b.min.y + 0.5f, 0f));
            var max = tm.WorldToCell(new Vector3(b.max.x - 0.5f, b.max.y - 0.5f, 0f));
            for (int y = min.y; y <= max.y; y++)
                for (int x = min.x; x <= max.x; x++) cells.Add(new Vector3Int(x, y, 0));
            Undo.RecordObject(r, "Hide old water");
            r.enabled = false;
        }
        var arr = cells.ToArray();
        tm.SetTiles(arr, arr.Select(_ => (TileBase)water).ToArray());
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Ground: {renderers.Length} old water rectangles hidden, {arr.Length} soft water cells painted. Save with Cmd+S.");
    }

    /// <summary>
    /// Random deco tiles: paint with ONE tile and Unity picks a different bush, fern or flower each time.
    /// They use the sprites of the Deco_* tiles in Assets/Tiles and are meant for the "Details" tilemap.
    /// </summary>
    [MenuItem("Survival/Ground/Create Random Deco Tiles (bushes, ferns, flowers)")]
    public static void CreateRandomDeco()
    {
        Sprite[] Sprites(params string[] names)
        {
            var list = new List<Sprite>();
            foreach (string n in names)
            {
                var t = AssetDatabase.LoadAssetAtPath<Tile>($"Assets/Tiles/{n}.asset");
                if (t != null && t.sprite != null) list.Add(t.sprite);
                else Debug.LogWarning("Ground: tile not found: " + n);
            }
            return list.ToArray();
        }

        void Make(string name, Sprite[] sprites)
        {
            if (sprites.Length == 0) return;
            var tile = LoadOrCreate<RuleTile>($"{Dir}/{name}.asset");
            tile.m_DefaultSprite = sprites[0];
            tile.m_DefaultColliderType = Tile.ColliderType.None;
            tile.m_TilingRules = new List<RuleTile.TilingRule> { MakeRule("00000000", sprites, 0) };
            EditorUtility.SetDirty(tile);
        }

        Make("Random_Bushes", Sprites("Deco_Bush_A", "Deco_Bush_B", "Deco_Bush_C", "Deco_Bush_D", "Deco_Bush_E"));
        Make("Random_Ferns", Sprites("Deco_Fern_A", "Deco_Fern_B", "Deco_Fern_C", "Deco_Tuft_A", "Deco_Tuft_B", "Deco_Tuft_C", "Deco_Tuft_D", "Deco_Tuft_E"));
        Make("Random_Flowers", Sprites("Deco_Flower_Daisy", "Deco_Flower_Red", "Deco_Flower_Sun_A", "Deco_Flower_Sun_B"));
        Make("Random_Forest_Floor", Sprites("Deco_Twigs", "Deco_Stump_Small", "Deco_Fern_A", "Deco_Tuft_A", "Deco_Tuft_B", "Deco_Fern_B"));
        AssetDatabase.SaveAssets();
        Debug.Log("Ground: random deco tiles are in Assets/Tiles/Ground (Random_Bushes, Random_Ferns, Random_Flowers, Random_Forest_Floor). Drag them into the Tile Palette and paint on \"Details\".");
    }

    /// <summary>
    /// Emergency repair: makes the grass ground again if the "Ground piece" objects were deleted (or are missing in parts).
    /// Then does the same as the Soft Paths + Meadow command: ground colour, soft paths, grass tufts.
    /// </summary>
    [MenuItem("Survival/Ground/Restore Ground (if deleted)")]
    public static void RestoreGround()
    {
        int choice = EditorUtility.DisplayDialogComplex("Restore ground",
            "Which grass do you want? The ground, the paths and the grass tufts are made again.", "Olive", "Cancel", "Green");
        if (choice == 1) return;
        string theme = choice == 0 ? "Olive" : "Green";
        int ox = choice == 0 ? 0 : 11;

        var level = GameObject.Find("LEVEL");
        var ground = GameObject.Find("Ground");
        if (ground == null)
        {
            ground = new GameObject("Ground");
            if (level != null) ground.transform.SetParent(level.transform, false);
            Undo.RegisterCreatedObjectUndo(ground, "Ground");
        }

        var existing = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(r => r.gameObject.name == "Ground piece").Select(r => r.transform.position).ToList();
        var plain = S(ox, 0, 12);
        // the same lit material the other level pieces use (so day and night light also work on the ground)
        Material material = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(r => r.gameObject.name == "Ground piece" && r.sharedMaterial != null).Select(r => r.sharedMaterial).FirstOrDefault();
        if (material == null)
        {
            var anyTilemap = Object.FindObjectsByType<TilemapRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.sharedMaterial != null);
            if (anyTilemap != null) material = anyTilemap.sharedMaterial;
        }
        if (material == null) material = GreyboxSceneBuilder.LitMaterial;
        int made = 0;
        const float w = 40f, h = 30f;
        for (int ix = 0; ix < 6; ix++)
        {
            for (int iy = 0; iy < 6; iy++)
            {
                var c = new Vector3(-120f + w / 2f + ix * w, -90f + h / 2f + iy * h, 0f);
                if (existing.Any(p => Mathf.Abs(p.x - c.x) < 1f && Mathf.Abs(p.y - c.y) < 1f)) continue;
                var piece = new GameObject("Ground piece");
                piece.transform.SetParent(ground.transform, false);
                piece.transform.position = c;
                var sr = piece.AddComponent<SpriteRenderer>();
                sr.sprite = plain;
                if (material != null) sr.sharedMaterial = material;
                sr.sortingOrder = -50;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                sr.size = new Vector2(w, h);
                Undo.RegisterCreatedObjectUndo(piece, "Ground piece");
                made++;
            }
        }
        Debug.Log($"Ground: {made} ground pieces made again.");
        Apply(theme, ox);
    }

    static readonly string[] OldWaterNames = { "Stream (water)", "River (cold section)", "Landmark: lake" };

    /// <summary>Cuts the transparent water sheet so you can drag it into a Tile Palette (same layout as TilesetWater).</summary>
    [MenuItem("Survival/Ground/Prepare Water Sheet for Hand Painting")]
    public static void PrepareWaterSheet()
    {
        EnsureBankSheet();
        MakeWater();
        AssetDatabase.SaveAssets();
        Debug.Log("Ground: TilesetWaterBank is cut. Drag Assets/Resources/Pixel/Tilesets/TilesetWaterBank into a Tile Palette. Same positions as in docs/img/wasser_bauplan.png.");
    }

    /// <summary>Clears all painted water on the "Water" tilemap and, if you want, shows the old blue rectangles again.</summary>
    [MenuItem("Survival/Ground/Reset Water (clear painted water)")]
    public static void ResetWater()
    {
        var tm = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == "Water");
        int choice = EditorUtility.DisplayDialogComplex("Reset water",
            "All painted water on the tilemap \"Water\" is removed. Show the old blue rectangles again?",
            "Show old rectangles", "Cancel", "Keep them hidden");
        if (choice == 1) return;
        if (tm != null)
        {
            Undo.RegisterCompleteObjectUndo(tm, "Reset water");
            tm.ClearAllTiles();
        }
        if (choice == 0)
        {
            foreach (var r in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!OldWaterNames.Contains(r.gameObject.name)) continue;
                Undo.RecordObject(r, "Show old water");
                r.enabled = true;
            }
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Ground: painted water cleared" + (choice == 0 ? ", old rectangles shown again." : "."));
    }

    /// <summary>
    /// Makes path and river flush: wherever a painted water cell touches a path cell, the path is continued underneath the water cell.
    /// The water bank is see-through, so the dirt shows right up to the shore line and the grassy path edge disappears.
    /// Run it after you painted or changed the river.
    /// </summary>
    [MenuItem("Survival/Ground/Make Paths Flush With Water")]
    public static void MakePathsFlushWithWater()
    {
        var all = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var paths = all.FirstOrDefault(t => t.name == "Paths");
        var water = all.FirstOrDefault(t => t.name == "Water");
        if (paths == null || water == null)
        {
            EditorUtility.DisplayDialog("Paths and water", "There must be tilemaps called \"Paths\" and \"Water\" in the open scene.", "OK");
            return;
        }
        TileBase dirt = null;
        paths.CompressBounds();
        foreach (var p in paths.cellBounds.allPositionsWithin)
        {
            var t = paths.GetTile(p);
            if (t != null && t.name.StartsWith("DirtPath_")) { dirt = t; break; }
        }
        if (dirt == null) dirt = AssetDatabase.LoadAssetAtPath<RuleTile>($"{Dir}/DirtPath_Olive.asset") ?? AssetDatabase.LoadAssetAtPath<RuleTile>($"{Dir}/DirtPath_Green.asset");
        if (dirt == null)
        {
            EditorUtility.DisplayDialog("Paths and water", "No soft path tile found. Run Survival > Ground > Soft Paths + ... first.", "OK");
            return;
        }

        water.CompressBounds();
        var add = new List<Vector3Int>();
        foreach (var wp in water.cellBounds.allPositionsWithin)
        {
            if (water.GetTile(wp) == null) continue;
            var pc = paths.WorldToCell(water.GetCellCenterWorld(wp));
            if (paths.GetTile(pc) != null) continue;
            bool touches = false;
            for (int dx = -1; dx <= 1 && !touches; dx++)
                for (int dy = -1; dy <= 1 && !touches; dy++)
                    if ((dx != 0 || dy != 0) && paths.GetTile(pc + new Vector3Int(dx, dy, 0)) != null) touches = true;
            if (touches) add.Add(pc);
        }
        Undo.RegisterCompleteObjectUndo(paths, "Paths flush with water");
        paths.SetTiles(add.ToArray(), add.Select(_ => dirt).ToArray());
        paths.RefreshAllTiles();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Ground: path continued under {add.Count} water cells.");
    }
}
