using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Menu: Survival > Rebuild Level From Generator (REPLACES Level.unity). Already done once: Level.unity is a normal scene now.
/// Turns the generated test level into a normal Unity scene that you edit by hand:
///   - the pixel-art sheets are cut into real Sprite assets (usable in the Tile Palette),
///   - the ground, paths and trees are real Tilemaps (trees have a collider: paint a tree = blocked, erase = open),
///   - every object has its sprite on it (move, rotate, duplicate in the Scene view) and common objects become prefabs,
///   - the result is saved as Assets/Scenes/Level.unity. From then on you work in that scene, not with the generator.
/// </summary>
public static class LevelExporter
{
    const string ScenePath = "Assets/Scenes/Level.unity";
    const string TilesDir = "Assets/Tiles";
    const string PrefabDir = "Assets/Prefabs/Objects";

    const string Floor = "Pixel/Tilesets/TilesetFloor";
    const string Nature = "Pixel/Tilesets/TilesetNature";
    const string Water = "Pixel/Tilesets/TilesetWater";
    const string Camp = "Pixel/Tilesets/tileset_camp";
    const string Hunter = "Pixel/Characters/Hunter";

    [MenuItem("Survival/Rebuild Level From Generator (REPLACES Level.unity)")]
    public static void Run()
    {
        if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Level.unity already exists",
                "This rebuilds the level from the generator and will REPLACE Assets/Scenes/Level.unity with everything you changed in it (tiles, objects, positions). You do NOT need this to edit the level. Continue only if you really want to start over.",
                "Replace my level", "Cancel"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GreyboxSceneBuilder.GenerateForEditing();
        var root = GameObject.Find("GREYBOX");
        if (root == null)
        {
            Debug.LogError("Make Level Editable: the generated level was not found.");
            return;
        }

        SliceSheetsAndBakeSprites(root);
        BuildTilemaps(root.transform);
        ForestDecorator.Run(false);
        TrimHiddenForest(false);
        MakePrefabs();
        root.name = "LEVEL";

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("Level.unity is ready. Open it and edit by hand (see docs/LEVEL_EDITING.md). It is now the scene in Build Settings.");
        EditorUtility.DisplayDialog("Level is editable now",
            "Saved as Assets/Scenes/Level.unity (and set as the build scene).\n\nTiles: Assets/Tiles\nObject prefabs: Assets/Prefabs/Objects\n\nRead docs/LEVEL_EDITING.md for the Tile Palette steps.", "OK");
    }

    // ------------------------------------------------------------------ remove what the camera never sees

    // How far the camera can see from a spot the player can stand on (world units). The pixel-perfect camera shows at least
    // 20 x 11 units; the extra margin covers wide windows and the size of one tree cell.
    const float SeeX = 16f, SeeY = 10f;

    /// <summary>
    /// Deletes every tree (and every piece of ground) that is too far from any place the player can reach to ever be on screen.
    /// The player's walkable area is found by flood fill from the player over the cells without a tree, so it also works
    /// after you changed the forest by hand. Can be undone with Cmd+Z.
    /// </summary>
    [MenuItem("Survival/Trim Hidden Forest (camera never sees it)")]
    public static void TrimHiddenForestMenu() => TrimHiddenForest(true);

    public static void TrimHiddenForest(bool askFirst)
    {
        Tilemap trees = null;
        foreach (var tm in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            if (tm.name == "Trees") trees = tm;
        var player = Object.FindFirstObjectByType<PlayerController>();
        if (trees == null || player == null)
        {
            if (askFirst) EditorUtility.DisplayDialog("Cannot trim", "Open Level.unity first: a Tilemap called Trees and the Player are needed.", "OK");
            return;
        }

        int cols = Mathf.RoundToInt(MapData.HalfW * 2 / MapData.Cell), rows = Mathf.RoundToInt(MapData.HalfH * 2 / MapData.Cell);
        int minX = -cols / 2, maxX = cols / 2 - 1, minY = -rows / 2, maxY = rows / 2 - 1;

        // 1) flood fill: which cells can the player reach?
        var start = trees.WorldToCell(player.transform.position);
        if (trees.HasTile(start))
        {
            if (askFirst) EditorUtility.DisplayDialog("Cannot trim", "The player stands inside a tree tile. Move the Player to open ground first.", "OK");
            return;
        }
        var reach = new HashSet<Vector3Int> { start };
        var queue = new Queue<Vector3Int>();
        queue.Enqueue(start);
        var steps = new[] { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down };
        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            foreach (var d in steps)
            {
                var n = c + d;
                if (n.x < minX || n.x > maxX || n.y < minY || n.y > maxY) continue;
                if (reach.Contains(n) || trees.HasTile(n)) continue;
                reach.Add(n);
                queue.Enqueue(n);
            }
        }

        // 2) every cell the camera could see from there
        int rx = Mathf.CeilToInt(SeeX / MapData.Cell), ry = Mathf.CeilToInt(SeeY / MapData.Cell);
        var visible = new HashSet<Vector3Int>(reach);
        foreach (var c in reach)
            for (int dx = -rx; dx <= rx; dx++)
                for (int dy = -ry; dy <= ry; dy++)
                    visible.Add(new Vector3Int(c.x + dx, c.y + dy, 0));

        // 3) what can go
        var removeTrees = new List<Vector3Int>();
        foreach (var pos in trees.cellBounds.allPositionsWithin)
            if (trees.HasTile(pos) && !visible.Contains(pos)) removeTrees.Add(pos);

        var ground = GameObject.Find("Ground");
        var removePieces = new List<GameObject>();
        if (ground != null)
        {
            foreach (Transform piece in ground.transform)
            {
                var sr = piece.GetComponent<SpriteRenderer>();
                if (sr == null) continue;
                var bounds = sr.bounds;
                bool seen = false;
                foreach (var c in visible)
                {
                    var p = trees.GetCellCenterWorld(c);
                    if (p.x >= bounds.min.x && p.x <= bounds.max.x && p.y >= bounds.min.y && p.y <= bounds.max.y) { seen = true; break; }
                }
                if (!seen) removePieces.Add(piece.gameObject);
            }
        }

        if (askFirst && !EditorUtility.DisplayDialog("Trim hidden forest",
                $"Remove {removeTrees.Count} trees and {removePieces.Count} pieces of ground that the camera never sees from any place the player can reach?\n\nUndo with Cmd+Z.",
                "Remove them", "Cancel"))
            return;

        Undo.RegisterCompleteObjectUndo(trees, "Trim hidden forest");
        trees.SetTiles(removeTrees.ToArray(), new TileBase[removeTrees.Count]);
        trees.CompressBounds();
        foreach (var piece in removePieces) Undo.DestroyObjectImmediate(piece);
        EditorSceneManager.MarkSceneDirty(trees.gameObject.scene);
        Debug.Log($"Trim hidden forest: removed {removeTrees.Count} trees and {removePieces.Count} ground pieces. Save the scene (Cmd+S).");
    }

    // ------------------------------------------------------------------ repair for an already made level

    /// <summary>
    /// A level made with the first version of the tool has ONE huge ground picture, which Unity cannot draw
    /// ("Cannot generate 9 slice ... size is too big"). This cuts it into 40 x 30 pieces. Safe to run again.
    /// </summary>
    [MenuItem("Survival/Fix Ground (split into pieces)")]
    public static void FixGround()
    {
        var ground = GameObject.Find("Ground");
        var sr = ground != null ? ground.GetComponent<SpriteRenderer>() : null;
        if (sr == null || sr.drawMode != SpriteDrawMode.Tiled)
        {
            EditorUtility.DisplayDialog("Nothing to fix", "The ground is already made of pieces (or there is no object called Ground in the open scene).", "OK");
            return;
        }

        Vector2 size = sr.size;
        Vector3 center = ground.transform.position;
        var sprite = sr.sprite;
        var material = sr.sharedMaterial;
        int order = sr.sortingOrder;
        Object.DestroyImmediate(sr);

        const float pieceW = 40f, pieceH = 30f;
        for (float gx = -size.x / 2f; gx < size.x / 2f - 0.01f; gx += pieceW)
        {
            for (float gy = -size.y / 2f; gy < size.y / 2f - 0.01f; gy += pieceH)
            {
                var piece = new GameObject("Ground piece");
                piece.transform.SetParent(ground.transform, false);
                piece.transform.position = center + new Vector3(gx + pieceW / 2f, gy + pieceH / 2f, 0f);
                var psr = piece.AddComponent<SpriteRenderer>();
                psr.sprite = sprite;
                psr.sharedMaterial = material;
                psr.sortingOrder = order;
                psr.drawMode = SpriteDrawMode.Tiled;
                psr.tileMode = SpriteTileMode.Continuous;
                psr.size = new Vector2(pieceW, pieceH);
            }
        }
        ground.transform.localScale = Vector3.one;
        EditorSceneManager.MarkSceneDirty(ground.scene);
        Debug.Log("Ground split into pieces. Save the scene (Cmd+S).");
    }

    // ------------------------------------------------------------------ sprites

    static void SliceSheetsAndBakeSprites(GameObject root)
    {
        var skins = root.GetComponentsInChildren<PixelSkin>(true);

        // Which rectangles are needed on which sheet (besides the 16x16 cells)?
        var extras = new Dictionary<string, List<RectInt>>
        {
            { Floor, new List<RectInt>() }, { Water, new List<RectInt>() }, { Camp, new List<RectInt>() },
            { Hunter, new List<RectInt>() },
            { Nature, new List<RectInt> {
                new RectInt(0, 0, 32, 32), new RectInt(32, 0, 32, 32), new RectInt(256, 0, 32, 32), new RectInt(288, 0, 32, 32),
                new RectInt(192, 0, 32, 32), new RectInt(272, 160, 64, 48) } },
        };
        foreach (var skin in skins)
        {
            if (skin.w <= 0 || skin.h <= 0) continue;
            if (!PixelImportFixer.IsSheet(PixelSheets.AssetPath(skin.resource))) continue;
            if (!extras.ContainsKey(skin.resource)) extras[skin.resource] = new List<RectInt>();
            extras[skin.resource].Add(new RectInt(skin.x, skin.y, skin.w, skin.h));
        }
        foreach (var kv in extras) PixelSheets.Slice(kv.Key, kv.Value);
        AssetDatabase.Refresh();

        foreach (var skin in skins)
        {
            var sprite = PixelSheets.Find(skin.resource, skin.x, skin.y, skin.w, skin.h);
            if (sprite == null)
            {
                Debug.LogWarning($"Make Level Editable: no sprite for {skin.name} ({skin.resource} {skin.x},{skin.y},{skin.w},{skin.h}); the coloured square stays.");
                continue;
            }
            skin.ApplySprite(sprite);
            Object.DestroyImmediate(skin);
        }

        // The player: show the first walking picture (the animation runs when you play)
        var player = root.GetComponentInChildren<PixelCharacter>();
        if (player != null)
        {
            var sr = player.GetComponent<SpriteRenderer>();
            var sprite = PixelSheets.Find(Hunter, 0, 0, 16, 16);
            if (sr != null && sprite != null)
            {
                sr.sprite = sprite;
                sr.color = Color.white;
            }
        }
    }

    // ------------------------------------------------------------------ tilemaps

    static void BuildTilemaps(Transform root)
    {
        Directory.CreateDirectory(TilesDir);

        // --- dirt paths (cell size 1) ---
        var dirt = new[] { MakeTile("Dirt_A", Floor, 128, 128, 16, 16, false, false), MakeTile("Dirt_B", Floor, 128, 144, 16, 16, false, false) };
        var paths = NewTilemap(root, "Paths", 1f, -44, false);
        int halfW = Mathf.RoundToInt(MapData.HalfW), halfH = Mathf.RoundToInt(MapData.HalfH);
        var rng = new System.Random(5);
        var pos = new List<Vector3Int>();
        var tiles = new List<TileBase>();
        for (int cy = -halfH; cy < halfH; cy++)
        {
            for (int cx = -halfW; cx < halfW; cx++)
            {
                var c = new Vector2(cx + 0.5f, cy + 0.5f);
                bool open = !MapData.IsBlocked(MapData.CellCenterOf(c));
                if (open && MapData.DistanceToPaths(c) <= MapData.PathHalfWidth + 1f)
                {
                    pos.Add(new Vector3Int(cx, cy, 0));
                    tiles.Add(dirt[rng.Next(dirt.Length)]);
                }
            }
        }
        paths.SetTiles(pos.ToArray(), tiles.ToArray());

        // an empty tilemap for your own details (flowers, rocks, ...)
        NewTilemap(root, "Details", 1f, -43, false);

        // --- trees (cell size 2: one 32x32 tree per cell, with a collider) ---
        var treeTiles = new List<TileBase>();
        var treeSprites = new[]
        {
            ("Tree_RoundA", 0, 0), ("Tree_RoundB", 256, 0), ("Tree_RoundC", 288, 0), ("Tree_Spruce", 32, 0),
        };
        foreach (var (name, x, y) in treeSprites)
        {
            treeTiles.Add(MakeTile(name, Nature, x, y, 32, 32, false, true));
            treeTiles.Add(MakeTile(name + "_Mirror", Nature, x, y, 32, 32, true, true));
            if (name == "Tree_Spruce") { treeTiles.Add(treeTiles[treeTiles.Count - 2]); treeTiles.Add(treeTiles[treeTiles.Count - 2]); }   // more spruces
        }
        var trees = NewTilemap(root, "Trees", MapData.Cell, 1, true);
        int cols = Mathf.RoundToInt(MapData.HalfW * 2 / MapData.Cell);
        int rows = Mathf.RoundToInt(MapData.HalfH * 2 / MapData.Cell);
        var tpos = new List<Vector3Int>();
        var ttiles = new List<TileBase>();
        for (int j = 0; j < rows; j++)
        {
            for (int i = 0; i < cols; i++)
            {
                if (!MapData.IsBlocked(MapData.CellCenter(i, j))) continue;
                tpos.Add(new Vector3Int(i - cols / 2, j - rows / 2, 0));
                ttiles.Add(treeTiles[rng.Next(treeTiles.Count)]);
            }
        }
        trees.SetTiles(tpos.ToArray(), ttiles.ToArray());
    }

    static Tilemap NewTilemap(Transform root, string name, float cellSize, int sortingOrder, bool withCollider)
    {
        var gridGo = new GameObject($"Grid: {name} (cell {cellSize:0})");
        gridGo.transform.SetParent(root, false);
        gridGo.AddComponent<Grid>().cellSize = new Vector3(cellSize, cellSize, 0f);

        var go = new GameObject(name);
        go.transform.SetParent(gridGo.transform, false);
        var tilemap = go.AddComponent<Tilemap>();
        var renderer = go.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;
        if (GreyboxSceneBuilder.LitMaterial != null) renderer.sharedMaterial = GreyboxSceneBuilder.LitMaterial;

        if (withCollider)
        {
            // Painted tiles block the way. The colliders of all tiles are merged into one shape (no seams to catch on).
            go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            var composite = go.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            composite.generationType = CompositeCollider2D.GenerationType.Synchronous;
            go.AddComponent<TilemapCollider2D>().compositeOperation = Collider2D.CompositeOperation.Merge;
        }
        return tilemap;
    }

    internal static Tile MakeTile(string name, string resource, int x, int y, int w, int h, bool mirror, bool collider)
    {
        string path = $"{TilesDir}/{name}.asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }
        tile.sprite = PixelSheets.Find(resource, x, y, w, h);
        tile.colliderType = collider ? Tile.ColliderType.Grid : Tile.ColliderType.None;
        tile.transform = mirror ? Matrix4x4.Scale(new Vector3(-1f, 1f, 1f)) : Matrix4x4.identity;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    // ------------------------------------------------------------------ prefabs

    static void MakePrefabs()
    {
        try
        {
            Directory.CreateDirectory(PrefabDir);
            var groups = new Dictionary<string, List<Interactable>>();
            foreach (var item in Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None))
            {
                // Objects that point to other objects of the scene (fire pit, camp zone, climbing target) stay normal scene objects.
                if (item.fire != null || item.zone != null || item.moveTo != null) continue;
                var find = item.GetComponent<RandomFind>();
                if (find != null && find.attached != null) continue;
                if (item.name.StartsWith("Camp")) continue;
                if (!groups.ContainsKey(item.name)) groups[item.name] = new List<Interactable>();
                groups[item.name].Add(item);
            }

            foreach (var kv in groups)
            {
                string file = kv.Key.Replace(":", "").Replace("/", "-").Replace("  ", " ").Trim();
                string path = $"{PrefabDir}/{file}.prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(kv.Value[0].gameObject, path);
                if (prefab == null) continue;
                foreach (var item in kv.Value)
                {
                    var parent = item.transform.parent;
                    var position = item.transform.position;
                    var rotation = item.transform.rotation;
                    Object.DestroyImmediate(item.gameObject);
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                    instance.transform.SetPositionAndRotation(position, rotation);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Make Level Editable: prefabs could not be created (the level itself is fine): " + e.Message);
        }
    }
}
