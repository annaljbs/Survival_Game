using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Forest tools.
///  * Remove Trees On Water: deletes every tree whose cell touches painted water (tilemap "Water") or one of the old water rectangles.
///  * Thin Out Forest: keeps the forest as an INVISIBLE WALL (tilemap "Forest Wall", red marks in the Scene view, hidden in the game),
///    so nobody can walk where the trees were, but replaces the dense mixed forest by a few fir trees (two sizes) along the open ground.
///    You place more trees by hand wherever you like; the wall stays until you erase it on "Forest Wall".
/// </summary>
public static class ForestTools
{
    const string Nature = "Pixel/Tilesets/TilesetNature";
    const string MarkerResource = "Pixel/Custom/ForestWallMarker";
    static readonly string[] OldWater = { "Stream (water)", "River (cold section)", "Landmark: lake" };

    // How many fir trees are kept along the open ground
    const float KeepChance = 0.60f;      // of the edge cells, 0 = none, 1 = as many as fit
    // How the kept fir trees are spread over the four sizes S, M, L, XL (relative weights)
    static readonly float[] SizeWeights = { 4f, 3f, 2f, 1f };
    static readonly int[] SizeSpacing = { 2, 2, 3, 3 };   // free cells needed around a tree of that size (Chebyshev distance)

    static readonly string[] FirNames = { "Fir_S", "Fir_M", "Fir_L", "Fir_XL" };
    static readonly int[] FirHeights = { 32, 39, 46, 53 };

    static Tilemap Find(string name) =>
        Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == name);

    /// <summary>The cells of the tree tilemap that touch water.</summary>
    static HashSet<Vector3Int> TreeCellsOnWater(Tilemap trees)
    {
        var set = new HashSet<Vector3Int>();
        var water = Find("Water");
        if (water != null)
        {
            water.CompressBounds();
            foreach (var p in water.cellBounds.allPositionsWithin)
                if (water.GetTile(p) != null) set.Add(trees.WorldToCell(water.GetCellCenterWorld(p)));
        }
        foreach (var r in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!OldWater.Contains(r.gameObject.name)) continue;
            Vector2 size = r.drawMode == SpriteDrawMode.Simple && r.sprite != null ? (Vector2)r.sprite.bounds.size : r.size;
            var s = r.transform.lossyScale;
            size = new Vector2(size.x * Mathf.Abs(s.x), size.y * Mathf.Abs(s.y));
            Vector3 c = r.transform.position;
            var min = trees.WorldToCell(new Vector3(c.x - size.x / 2f + 0.01f, c.y - size.y / 2f + 0.01f, 0f));
            var max = trees.WorldToCell(new Vector3(c.x + size.x / 2f - 0.01f, c.y + size.y / 2f - 0.01f, 0f));
            for (int y = min.y; y <= max.y; y++)
                for (int x = min.x; x <= max.x; x++) set.Add(new Vector3Int(x, y, 0));
        }
        return set;
    }

    static int RemoveWaterTrees(Tilemap trees)
    {
        int n = 0;
        foreach (var c in TreeCellsOnWater(trees))
        {
            if (!trees.HasTile(c)) continue;
            trees.SetTile(c, null);
            n++;
        }
        return n;
    }

    [MenuItem("Survival/Forest/Remove Trees On Water")]
    public static void RemoveTreesOnWater()
    {
        var trees = Find("Trees");
        if (trees == null) { EditorUtility.DisplayDialog("Forest", "There is no tilemap called \"Trees\" in the open scene.", "OK"); return; }
        Undo.RegisterCompleteObjectUndo(trees, "Remove trees on water");
        int n = RemoveWaterTrees(trees);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Forest: {n} trees on water removed. Save with Cmd+S.");
    }


    /// <summary>
    /// Makes the fir tiles (S = the fir of the tileset, M, L, XL = taller and wider firs built from it), each also mirrored.
    /// The foot of every fir sits at the lower edge of its cell, so a tall fir grows upwards from the cell where you paint it.
    /// Returns [size][0 = normal, 1 = mirrored].
    /// </summary>
    static Tile[][] EnsureFirTiles()
    {
        var result = new Tile[4][];
        // S: the fir of the tileset itself
        result[0] = new[]
        {
            LevelExporter.MakeTile("Tree_Spruce", Nature, 32, 0, 32, 32, false, true),
            LevelExporter.MakeTile("Tree_Spruce_Mirror", Nature, 32, 0, 32, 32, true, true),
        };
        for (int i = 1; i < 4; i++)
        {
            string path = $"Assets/Resources/Pixel/Custom/{FirNames[i]}.png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new System.Exception("Forest: picture not found: " + path);
            var ts = new TextureImporterSettings();
            importer.ReadTextureSettings(ts);
            ts.spriteAlignment = (int)SpriteAlignment.Custom;
            ts.spritePivot = new Vector2(0.5f, 16f / FirHeights[i]);   // 16 pixels = half a cell: foot at the lower edge of the cell
            importer.SetTextureSettings(ts);
            importer.SaveAndReimport();
            string res = "Pixel/Custom/" + FirNames[i];
            result[i] = new[]
            {
                LevelExporter.MakeTile(FirNames[i], res, 0, 0, 0, 0, false, true),
                LevelExporter.MakeTile(FirNames[i] + "_Mirror", res, 0, 0, 0, 0, true, true),
            };
        }
        return result;
    }

    /// <summary>Fir tiles for hand painting, one tile that picks a random size, and the right draw order for tall trees.</summary>
    [MenuItem("Survival/Forest/Create Fir Tiles (4 sizes) for Hand Painting")]
    public static void CreateFirTiles()
    {
        var firs = EnsureFirTiles();

        const string dir = "Assets/Tiles/Ground";
        System.IO.Directory.CreateDirectory(dir);
        const string path = dir + "/Random_Firs.asset";
        var rule = AssetDatabase.LoadAssetAtPath<RuleTile>(path);
        if (rule == null)
        {
            rule = ScriptableObject.CreateInstance<RuleTile>();
            AssetDatabase.CreateAsset(rule, path);
        }
        var sprites = new List<Sprite>();
        for (int i = 0; i < 4; i++)
            for (int n = 0; n < Mathf.RoundToInt(SizeWeights[i]); n++) sprites.Add(firs[i][0].sprite);
        rule.m_DefaultSprite = firs[0][0].sprite;
        rule.m_DefaultColliderType = Tile.ColliderType.Grid;
        rule.m_TilingRules = new List<RuleTile.TilingRule>
        {
            new RuleTile.TilingRule
            {
                m_Id = 0,
                m_Sprites = sprites.ToArray(),
                m_Output = RuleTile.TilingRuleOutput.OutputSprite.Random,
                m_RandomTransform = RuleTile.TilingRuleOutput.Transform.MirrorX,
                m_ColliderType = Tile.ColliderType.Grid,
                m_Neighbors = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0 },
            }
        };
        EditorUtility.SetDirty(rule);
        AssetDatabase.SaveAssets();

        // tall trees overlap the cell above: draw the rows from top to bottom so the lower tree is in front
        var trees = Find("Trees");
        if (trees != null)
        {
            var rend = trees.GetComponent<TilemapRenderer>();
            if (rend != null)
            {
                Undo.RecordObject(rend, "Tree draw order");
                rend.sortOrder = TilemapRenderer.SortOrder.TopLeft;
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        Debug.Log("Forest: tiles Fir_S, Fir_M, Fir_L, Fir_XL (+ _Mirror) and Random_Firs are in Assets/Tiles and Assets/Tiles/Ground. Drag them into a Tile Palette and paint on \"Trees\".");
    }


    // ---------------------------------------------------------------- scatter fir trees over the whole forest

    const int MaxDepth = 9;   // trees are placed up to this many cells away from the open ground (the camera never sees further)

    static float[] DepthWeights(int depth)
    {
        if (depth <= 1) return new[] { 4f, 3f, 1f, 0.3f };   // at the path: small and medium firs
        if (depth == 2) return new[] { 2f, 3f, 2f, 1f };
        return new[] { 1f, 2f, 3f, 2f };                     // deeper in the forest: the tall ones
    }

    [MenuItem("Survival/Forest/Scatter Fir Trees (sparse)")]
    public static void ScatterSparse() => ScatterFirs(0.30f);

    [MenuItem("Survival/Forest/Scatter Fir Trees (medium)")]
    public static void ScatterMedium() => ScatterFirs(0.50f);

    [MenuItem("Survival/Forest/Scatter Fir Trees (dense)")]
    public static void ScatterDense() => ScatterFirs(0.80f);

    /// <summary>
    /// Finds out what is forest and what is open ground.
    /// blocked = cells of the forest wall and of trees, reachable = open ground the Player can walk to,
    /// depth = distance of every forest cell to the open ground (1 = touches it), occupied = cells with a tree (size index).
    /// </summary>
    static bool Analyze(Tilemap trees, Tilemap wall, out HashSet<Vector3Int> blocked, out HashSet<Vector3Int> reachable,
                        out Dictionary<Vector3Int, int> depth, out Dictionary<Vector3Int, int> occupied)
    {
        blocked = new HashSet<Vector3Int>();
        occupied = new Dictionary<Vector3Int, int>();
        reachable = new HashSet<Vector3Int>();
        depth = new Dictionary<Vector3Int, int>();
        if (wall != null)
        {
            wall.CompressBounds();
            foreach (var c in wall.cellBounds.allPositionsWithin) if (wall.HasTile(c)) blocked.Add(c);
        }
        trees.CompressBounds();
        foreach (var c in trees.cellBounds.allPositionsWithin)
            if (trees.HasTile(c)) { blocked.Add(c); occupied[c] = 1; }

        int hx = Mathf.CeilToInt(MapData.HalfW / MapData.Cell), hy = Mathf.CeilToInt(MapData.HalfH / MapData.Cell);
        var player = GameObject.Find("Player");
        Vector3Int start = player != null ? trees.WorldToCell(player.transform.position) : Vector3Int.zero;
        var queue = new Queue<Vector3Int>();
        if (!blocked.Contains(start)) { reachable.Add(start); queue.Enqueue(start); }
        Vector3Int[] dirs4 = { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down };
        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            foreach (var d in dirs4)
            {
                var n = c + d;
                if (Mathf.Abs(n.x) > hx || Mathf.Abs(n.y) > hy) continue;
                if (blocked.Contains(n) || !reachable.Add(n)) continue;
                queue.Enqueue(n);
            }
        }
        if (reachable.Count < 5) return false;

        var q2 = new Queue<Vector3Int>();
        foreach (var r in reachable)
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    var n = r + new Vector3Int(dx, dy, 0);
                    if (blocked.Contains(n) && !depth.ContainsKey(n)) { depth[n] = 1; q2.Enqueue(n); }
                }
        while (q2.Count > 0)
        {
            var c = q2.Dequeue();
            int dc = depth[c];
            if (dc >= MaxDepth) continue;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    var n = c + new Vector3Int(dx, dy, 0);
                    if (blocked.Contains(n) && !depth.ContainsKey(n)) { depth[n] = dc + 1; q2.Enqueue(n); }
                }
        }
        return true;
    }

    const string NeedPlayer = "The Player must stand on open ground (not inside the forest wall) so the open ground can be found.";

    static int RollSize(System.Random rng, float[] w)
    {
        float roll = (float)rng.NextDouble() * w.Sum();
        for (int i = 0; i < w.Length; i++) { if (roll < w[i]) return i; roll -= w[i]; }
        return 0;
    }

    /// <summary>
    /// Plants fir trees of four sizes on the whole forest area (everywhere the invisible Forest Wall is), in groves with clearings.
    /// Small firs stand at the path, tall ones deeper in. Trees that are there already stay. Running it again adds more.
    /// </summary>
    static void ScatterFirs(float baseChance)
    {
        var trees = Find("Trees");
        var wall = Find("Forest Wall");
        if (trees == null || wall == null)
        {
            EditorUtility.DisplayDialog("Forest", "There must be tilemaps called \"Trees\" and \"Forest Wall\". Run Survival > Forest > Thin Out Forest first.", "OK");
            return;
        }
        var firs = EnsureFirTiles();
        Undo.RegisterCompleteObjectUndo(trees, "Scatter fir trees");
        if (!Analyze(trees, wall, out var blocked, out var reachable, out var depth, out var occupied))
        {
            EditorUtility.DisplayDialog("Forest", NeedPlayer, "OK");
            return;
        }
        var onWater = TreeCellsOnWater(trees);

        var rng = new System.Random();
        float seed = (float)rng.NextDouble() * 1000f;
        var candidates = depth.Keys.Where(c => !occupied.ContainsKey(c) && !onWater.Contains(c)).OrderBy(_ => rng.Next()).ToList();

        int placed = 0;
        var perSize = new int[4];
        foreach (var c in candidates)
        {
            // groves and clearings: a slow noise changes the chance from place to place
            float noise = Mathf.PerlinNoise(c.x * 0.16f + seed, c.y * 0.16f + seed);
            float chance = Mathf.Clamp01(baseChance * Mathf.Lerp(0.25f, 1.4f, noise));
            if (rng.NextDouble() > chance) continue;

            int size = RollSize(rng, DepthWeights(depth[c]));
            bool ok = true;
            for (int dx = -3; dx <= 3 && ok; dx++)
                for (int dy = -3; dy <= 3 && ok; dy++)
                {
                    if (!occupied.TryGetValue(c + new Vector3Int(dx, dy, 0), out int other)) continue;
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) < Mathf.Max(SizeSpacing[size], SizeSpacing[other])) ok = false;
                }
            if (!ok) continue;

            occupied[c] = size;
            trees.SetTile(c, firs[size][rng.Next(2)]);
            perSize[size]++;
            placed++;
        }

        var rend = trees.GetComponent<TilemapRenderer>();
        if (rend != null) rend.sortOrder = TilemapRenderer.SortOrder.TopLeft;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Forest: {placed} fir trees placed over the forest (sizes S/M/L/XL: {perSize[0]}/{perSize[1]}/{perSize[2]}/{perSize[3]}). Save with Cmd+S.");
    }

    // sizes S, M, L, XL for the tree line at the edge and for the row behind it
    static readonly float[] EdgeWeights = { 3f, 3f, 2f, 1f };
    static readonly float[] BackRowWeights = { 0.5f, 2f, 3f, 2f };
    const float BackRowChance = 0.75f;

    /// <summary>
    /// A closed line of fir trees along the whole edge of the walkable ground, so you can see where you cannot go:
    /// every forest cell that touches open ground gets a fir (mixed sizes, mirrored at random), and a second row behind it
    /// with taller firs peeks out over the first. Trees that are there already stay. Cmd+Z undoes it.
    /// </summary>
    [MenuItem("Survival/Forest/Border Trees Along the Edge of the Walkable Ground")]
    public static void BorderTrees()
    {
        var trees = Find("Trees");
        var wall = Find("Forest Wall");
        if (trees == null)
        {
            EditorUtility.DisplayDialog("Forest", "There is no tilemap called \"Trees\" in the open scene.", "OK");
            return;
        }
        var firs = EnsureFirTiles();
        Undo.RegisterCompleteObjectUndo(trees, "Border trees");
        if (!Analyze(trees, wall, out var blocked, out var reachable, out var depth, out var occupied))
        {
            EditorUtility.DisplayDialog("Forest", NeedPlayer, "OK");
            return;
        }
        var onWater = TreeCellsOnWater(trees);
        var rng = new System.Random();
        var perSize = new int[4];
        int placed = 0;

        void Plant(Vector3Int c, float[] weights)
        {
            if (occupied.ContainsKey(c) || onWater.Contains(c)) return;
            int size = RollSize(rng, weights);
            occupied[c] = size;
            trees.SetTile(c, firs[size][rng.Next(2)]);
            perSize[size]++;
            placed++;
        }

        // the map edge: open ground that touches the border of the map has no forest wall behind it, so make the cells beyond it trees
        int hx = Mathf.CeilToInt(MapData.HalfW / MapData.Cell), hy = Mathf.CeilToInt(MapData.HalfH / MapData.Cell);
        var outside = new List<Vector3Int>();
        foreach (var r in reachable)
        {
            if (r.x >= hx) outside.Add(r + Vector3Int.right);
            if (r.x <= -hx) outside.Add(r + Vector3Int.left);
            if (r.y >= hy) outside.Add(r + Vector3Int.up);
            if (r.y <= -hy) outside.Add(r + Vector3Int.down);
        }

        // row 1: every forest cell that touches the open ground (and the cells beyond the map border)
        foreach (var c in depth.Where(kv => kv.Value == 1).Select(kv => kv.Key).Concat(outside).Distinct().OrderBy(_ => rng.Next()).ToList())
            Plant(c, EdgeWeights);

        // row 2: behind it, tall firs that peek out over the first row
        foreach (var c in depth.Where(kv => kv.Value == 2).Select(kv => kv.Key).OrderBy(_ => rng.Next()).ToList())
            if (rng.NextDouble() < BackRowChance) Plant(c, BackRowWeights);

        var rend = trees.GetComponent<TilemapRenderer>();
        if (rend != null) rend.sortOrder = TilemapRenderer.SortOrder.TopLeft;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Forest: {placed} border trees placed (sizes S/M/L/XL: {perSize[0]}/{perSize[1]}/{perSize[2]}/{perSize[3]}). Save with Cmd+S.");
    }

    static Tile MakeWallTile()
    {
        const string path = "Assets/Tiles/Forest_Wall.asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }
        tile.sprite = PixelSheets.Find(MarkerResource, 0, 0, 0, 0);
        tile.colliderType = Tile.ColliderType.Grid;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    static Tilemap FindOrCreateWall(Tilemap trees)
    {
        var wall = Find("Forest Wall");
        if (wall != null) return wall;
        var treesGrid = trees.transform.parent;
        var gridGo = new GameObject("Grid: Forest Wall (cell 2)");
        gridGo.transform.SetParent(treesGrid != null ? treesGrid.parent : null, false);
        if (treesGrid != null) gridGo.transform.position = treesGrid.position;
        gridGo.AddComponent<Grid>().cellSize = trees.layoutGrid.cellSize;

        var go = new GameObject("Forest Wall");
        go.transform.SetParent(gridGo.transform, false);
        wall = go.AddComponent<Tilemap>();
        var rend = go.AddComponent<TilemapRenderer>();
        rend.sortingOrder = 50;   // above everything in the Scene view, so you see the red marks
        var treesRend = trees.GetComponent<TilemapRenderer>();
        if (treesRend != null) rend.sharedMaterial = treesRend.sharedMaterial;
        go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        var composite = go.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        composite.generationType = CompositeCollider2D.GenerationType.Synchronous;
        go.AddComponent<TilemapCollider2D>().compositeOperation = Collider2D.CompositeOperation.Merge;
        go.AddComponent<HideTilemapAtPlay>();
        Undo.RegisterCreatedObjectUndo(gridGo, "Forest Wall");
        return wall;
    }

    [MenuItem("Survival/Forest/Thin Out Forest (invisible wall + fir trees along the paths)")]
    public static void ThinOutForest()
    {
        var trees = Find("Trees");
        if (trees == null) { EditorUtility.DisplayDialog("Forest", "There is no tilemap called \"Trees\" in the open scene.", "OK"); return; }
        if (!EditorUtility.DisplayDialog("Thin out the forest?",
                "1. Trees on water are removed.\n" +
                "2. Everywhere there is a tree now, an invisible wall is made (you cannot walk there).\n" +
                "3. The trees are replaced by a few fir trees in four sizes along the open ground.\n\n" +
                "Commit or save a copy first. Cmd+Z undoes it.", "Do it", "Cancel")) return;

        // fir tiles (four sizes, each also mirrored)
        var firs = EnsureFirTiles();
        var wallTile = MakeWallTile();

        Undo.RegisterCompleteObjectUndo(trees, "Thin out forest");

        // 1. trees on water go
        int onWater = RemoveWaterTrees(trees);

        // 2. the invisible wall where trees stand now
        trees.CompressBounds();
        var blocked = new HashSet<Vector3Int>();
        foreach (var p in trees.cellBounds.allPositionsWithin)
            if (trees.HasTile(p)) blocked.Add(p);
        var wall = FindOrCreateWall(trees);
        Undo.RegisterCompleteObjectUndo(wall, "Forest wall");
        wall.SetTiles(blocked.ToArray(), blocked.Select(_ => (TileBase)wallTile).ToArray());

        // 3. open ground the player can reach (flood fill from the Player)
        int hx = Mathf.CeilToInt(MapData.HalfW / MapData.Cell), hy = Mathf.CeilToInt(MapData.HalfH / MapData.Cell);
        var player = GameObject.Find("Player");
        Vector3Int start = player != null ? trees.WorldToCell(player.transform.position) : Vector3Int.zero;
        var reachable = new HashSet<Vector3Int>();
        var queue = new Queue<Vector3Int>();
        if (!blocked.Contains(start)) { reachable.Add(start); queue.Enqueue(start); }
        Vector3Int[] dirs = { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down };
        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            foreach (var d in dirs)
            {
                var n = c + d;
                if (Mathf.Abs(n.x) > hx || Mathf.Abs(n.y) > hy) continue;
                if (blocked.Contains(n) || !reachable.Add(n)) continue;
                queue.Enqueue(n);
            }
        }

        // 4. the edge: wall cells next to reachable open ground
        var edge = new List<Vector3Int>();
        foreach (var c in blocked)
        {
            bool next = false;
            for (int dx = -1; dx <= 1 && !next; dx++)
                for (int dy = -1; dy <= 1 && !next; dy++)
                    if ((dx != 0 || dy != 0) && reachable.Contains(c + new Vector3Int(dx, dy, 0))) next = true;
            if (next) edge.Add(c);
        }
        var rng = new System.Random();
        edge = edge.OrderBy(_ => rng.Next()).ToList();

        // 5. choose the fir trees: not too close together (more room around the big ones)
        var kept = new List<(Vector3Int cell, int size)>();
        float totalWeight = SizeWeights.Sum();
        int RollSize()
        {
            float r = (float)rng.NextDouble() * totalWeight;
            for (int i = 0; i < SizeWeights.Length; i++) { if (r < SizeWeights[i]) return i; r -= SizeWeights[i]; }
            return 0;
        }
        foreach (var c in edge)
        {
            if (rng.NextDouble() > KeepChance) continue;
            int size = RollSize();
            bool ok = true;
            foreach (var k in kept)
            {
                int dist = Mathf.Max(Mathf.Abs(k.cell.x - c.x), Mathf.Abs(k.cell.y - c.y));
                if (dist < Mathf.Max(SizeSpacing[size], SizeSpacing[k.size])) { ok = false; break; }
            }
            if (ok) kept.Add((c, size));
        }

        trees.ClearAllTiles();
        var treesRend = trees.GetComponent<TilemapRenderer>();
        if (treesRend != null) treesRend.sortOrder = TilemapRenderer.SortOrder.TopLeft;
        foreach (var k in kept) trees.SetTile(k.cell, firs[k.size][rng.Next(2)]);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Forest: {onWater} trees on water removed, wall of {blocked.Count} cells made, {kept.Count} fir trees placed (sizes S/M/L/XL: {kept.Count(k => k.size == 0)}/{kept.Count(k => k.size == 1)}/{kept.Count(k => k.size == 2)}/{kept.Count(k => k.size == 3)}). Save with Cmd+S.");
    }
}
