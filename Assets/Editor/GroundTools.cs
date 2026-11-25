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

    static Sprite S(int ox, int col, int row)
    {
        var s = PixelSheets.Find(Sheet, (ox + col) * 16, row * 16, 16, 16);
        if (s == null) throw new System.Exception("Ground: sprite TilesetFloor_" + (ox + col) + "_" + row + " not found. Is the sheet cut? (Survival > Make Level Editable)");
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
}
