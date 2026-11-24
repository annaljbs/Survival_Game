using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Optional helpers for the open Level.unity (all can be undone with Cmd+Z):
///   Survival > More Tree Variety              swaps about 40% of the trees for other kinds (run once).
///   Survival > Place Bushes at the Forest Edge replaces the automatic bushes by a few small clumps where the forest meets open ground.
///   Survival > Remove Automatic Bushes        removes those bushes again (your own painted tiles stay).
/// The bushes are tiles named "Deco_..." on the Details tilemap. Tiles you paint yourself are never touched.
/// </summary>
public static class ForestDecorator
{
    const string Nature = "Pixel/Tilesets/TilesetNature";

    // Bush settings: raise "SeedChance" for more clumps, "MinSpacing" (in cells of 1 unit) for them to be further apart.
    const double SeedChance = 0.05;
    const float MinSpacing = 6f;
    const double SecondBush = 0.6, ThirdBush = 0.25;

    [MenuItem("Survival/More Tree Variety")]
    public static void TreeVarietyMenu() => TreeVariety(true);

    [MenuItem("Survival/Place Bushes at the Forest Edge")]
    public static void BushesMenu() => Bushes(true);

    [MenuItem("Survival/Remove Automatic Bushes")]
    public static void RemoveBushesMenu()
    {
        var details = FindMap("Details");
        if (details == null) { Warn(true); return; }
        int n = RemoveDeco(details);
        Debug.Log($"Removed {n} automatic bushes and plants. Save the scene (Cmd+S).");
    }

    /// <summary>Both steps (used when the level is generated from scratch).</summary>
    public static void Run(bool ask)
    {
        TreeVariety(ask);
        Bushes(ask);
    }

    // ------------------------------------------------------------------ helpers

    static Tilemap FindMap(string name)
    {
        foreach (var tm in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            if (tm.name == name) return tm;
        return null;
    }

    static void Warn(bool ask)
    {
        if (ask) EditorUtility.DisplayDialog("Cannot do that", "Open Level.unity first (the tilemaps Trees, Paths and Details are needed).", "OK");
    }

    class Pick
    {
        readonly List<TileBase> tiles = new List<TileBase>();
        readonly List<float> weights = new List<float>();
        float total;
        public void Add(TileBase tile, float weight) { tiles.Add(tile); weights.Add(weight); total += weight; }
        public TileBase Roll(System.Random rng)
        {
            double r = rng.NextDouble() * total;
            for (int i = 0; i < tiles.Count; i++) { r -= weights[i]; if (r <= 0) return tiles[i]; }
            return tiles[tiles.Count - 1];
        }
    }

    /// <summary>Removes every tile on the tilemap whose asset is called Deco_... Returns how many.</summary>
    static int RemoveDeco(Tilemap details)
    {
        Undo.RegisterCompleteObjectUndo(details, "Remove automatic bushes");
        var positions = new List<Vector3Int>();
        foreach (var pos in details.cellBounds.allPositionsWithin)
        {
            var tile = details.GetTile(pos);
            if (tile != null && tile.name.StartsWith("Deco_")) positions.Add(pos);
        }
        details.SetTiles(positions.ToArray(), new TileBase[positions.Count]);
        details.CompressBounds();
        EditorSceneManager.MarkSceneDirty(details.gameObject.scene);
        return positions.Count;
    }

    // ------------------------------------------------------------------ trees

    public static void TreeVariety(bool ask)
    {
        var trees = FindMap("Trees");
        if (trees == null) { Warn(ask); return; }

        PixelSheets.Slice(Nature, new[]
        {
            new RectInt(64, 0, 32, 32),      // dead tree
            new RectInt(96, 0, 32, 32),      // round tree with roots
            new RectInt(48, 288, 48, 48),    // big green oak
            new RectInt(144, 288, 48, 48),   // big autumn tree
        });
        AssetDatabase.Refresh();

        var rare = new Pick();
        void AddTree(string name, int x, int y, int w, int h, float weight)
        {
            rare.Add(LevelExporter.MakeTile(name, Nature, x, y, w, h, false, true), weight);
            rare.Add(LevelExporter.MakeTile(name + "_Mirror", Nature, x, y, w, h, true, true), weight);
        }
        AddTree("Tree_Roots", 96, 0, 32, 32, 3f);
        AddTree("Tree_BigOak", 48, 288, 48, 48, 3f);
        AddTree("Tree_Dead", 64, 0, 32, 32, 1.2f);
        AddTree("Tree_Autumn", 144, 288, 48, 48, 1f);

        var rng = new System.Random();
        Undo.RegisterCompleteObjectUndo(trees, "More tree variety");
        var positions = new List<Vector3Int>();
        var tiles = new List<TileBase>();
        foreach (var pos in trees.cellBounds.allPositionsWithin)
        {
            if (!trees.HasTile(pos) || rng.NextDouble() > 0.40) continue;
            positions.Add(pos);
            tiles.Add(rare.Roll(rng));
        }
        trees.SetTiles(positions.ToArray(), tiles.ToArray());
        EditorSceneManager.MarkSceneDirty(trees.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"More tree variety: {positions.Count} trees swapped. Save the scene (Cmd+S).");
    }

    // ------------------------------------------------------------------ bushes

    public static void Bushes(bool ask)
    {
        var trees = FindMap("Trees");
        var paths = FindMap("Paths");
        var details = FindMap("Details");
        if (trees == null || paths == null || details == null) { Warn(ask); return; }

        Tile Deco(string name, int col, int row) => LevelExporter.MakeTile("Deco_" + name, Nature, col * 16, row * 16, 16, 16, false, false);
        var bushes = new Pick();
        bushes.Add(Deco("Bush_A", 0, 10), 1f);
        bushes.Add(Deco("Bush_B", 1, 10), 1f);
        bushes.Add(Deco("Bush_C", 2, 10), 1f);
        bushes.Add(Deco("Bush_D", 6, 10), 1f);
        bushes.Add(Deco("Bush_E", 10, 10), 1f);

        int removed = RemoveDeco(details);   // start from a clean slate: running this again does not pile up

        // things a bush must not be put on or next to
        var obstacles = new List<Vector2>();
        foreach (var item in Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None)) obstacles.Add(item.transform.position);
        foreach (var spot in Object.FindObjectsByType<FindSpot>(FindObjectsSortMode.None)) obstacles.Add(spot.transform.position);
        var player = Object.FindFirstObjectByType<PlayerController>();
        if (player != null) obstacles.Add(player.transform.position);

        bool Free(Vector3Int cell)
        {
            Vector3 c = details.GetCellCenterWorld(cell);
            if (Mathf.Abs(c.x) > MapData.HalfW - 2f || Mathf.Abs(c.y) > MapData.HalfH - 2f) return false;
            if (paths.HasTile(cell) || details.HasTile(cell)) return false;
            if (trees.HasTile(trees.WorldToCell(c))) return false;
            foreach (var o in obstacles) if ((o - (Vector2)c).sqrMagnitude < 4f) return false;   // 2 units
            return true;
        }

        // candidate cells: open ground that touches a tree
        var dirs = new[] { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down };
        var candidates = new List<Vector3Int>();
        var seen = new HashSet<Vector3Int>();
        foreach (var tpos in trees.cellBounds.allPositionsWithin)
        {
            if (!trees.HasTile(tpos)) continue;
            Vector3 center = trees.GetCellCenterWorld(tpos);
            foreach (var d in dirs)
            {
                Vector3 outside = center + new Vector3(d.x * 1.5f, d.y * 1.5f, 0f);
                Vector3 along = new Vector3(d.y != 0 ? 0.5f : 0f, d.x != 0 ? 0.5f : 0f, 0f);
                foreach (var sign in new[] { -1f, 1f })
                {
                    var cell = details.WorldToCell(outside + along * sign);
                    if (seen.Add(cell) && Free(cell)) candidates.Add(cell);
                }
            }
        }

        // pick a few seeds, far apart, then grow each into a small clump of 1 to 3 bushes
        var rng = new System.Random();
        for (int i = candidates.Count - 1; i > 0; i--)   // shuffle
        {
            int j = rng.Next(i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }
        var seeds = new List<Vector3Int>();
        var placed = new Dictionary<Vector3Int, TileBase>();
        foreach (var cell in candidates)
        {
            if (rng.NextDouble() > SeedChance) continue;
            bool tooClose = false;
            foreach (var s in seeds)
                if (Vector3Int.Distance(s, cell) < MinSpacing) { tooClose = true; break; }
            if (tooClose) continue;
            seeds.Add(cell);
            placed[cell] = bushes.Roll(rng);

            double[] more = { SecondBush, ThirdBush };
            var current = cell;
            foreach (double chance in more)
            {
                if (rng.NextDouble() > chance) break;
                var next = current + dirs[rng.Next(dirs.Length)];
                if (placed.ContainsKey(next) || !Free(next)) continue;
                placed[next] = bushes.Roll(rng);
                current = next;
            }
        }

        Undo.RegisterCompleteObjectUndo(details, "Place bushes");
        var keys = new List<Vector3Int>(placed.Keys);
        var tiles = new List<TileBase>();
        foreach (var k in keys) tiles.Add(placed[k]);
        details.SetTiles(keys.ToArray(), tiles.ToArray());
        EditorSceneManager.MarkSceneDirty(details.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"Bushes: {removed} old automatic ones removed, {keys.Count} new bushes in {seeds.Count} clumps. Save the scene (Cmd+S).");
    }
}
