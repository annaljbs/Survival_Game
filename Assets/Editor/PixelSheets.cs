using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// Cuts the pixel-art sheets into real Sprite assets: every 16x16 cell, plus any bigger rectangles that were asked for
/// (trees, tents, ...). Afterwards you can see and use the pieces in the Project window, the Sprite Editor and the Tile Palette.
/// Names: "TilesetNature_3_10" = column 3, row 10 (counted from the top-left, 16x16 each);
///        "TilesetNature_r0_0_32x32" = a free rectangle x, y, width x height in pixels from the top-left.
/// </summary>
public static class PixelSheets
{
    public static string AssetPath(string resourcePath) => "Assets/Resources/" + resourcePath + ".png";

    public static string SpriteName(string textureName, int x, int y, int w, int h)
    {
        if (w == 16 && h == 16 && x % 16 == 0 && y % 16 == 0) return $"{textureName}_{x / 16}_{y / 16}";
        return $"{textureName}_r{x}_{y}_{w}x{h}";
    }

    /// <summary>Cut a sheet: all 16x16 cells plus the extra rectangles (x, y from the top-left, in pixels).</summary>
    public static void Slice(string resourcePath, IEnumerable<RectInt> extraRects)
    {
        string assetPath = AssetPath(resourcePath);
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning("PixelSheets: not found: " + assetPath);
            return;
        }
        PixelImportFixer.Apply(importer, assetPath);
        importer.SaveAndReimport();

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        int width = tex.width, height = tex.height;
        string texName = Path.GetFileNameWithoutExtension(assetPath);

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        // Keep what is already cut (same names keep the same ids, so nothing that uses a sprite breaks)
        var all = new Dictionary<string, SpriteRect>();
        foreach (var r in provider.GetSpriteRects()) all[r.name] = r;

        void Add(string name, int x, int yTop, int w, int h)
        {
            if (all.ContainsKey(name)) return;
            all[name] = new SpriteRect
            {
                name = name,
                rect = new Rect(x, height - yTop - h, w, h),   // Unity counts y from the bottom
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                spriteID = GUID.Generate(),
            };
        }

        for (int row = 0; row < height / 16; row++)
            for (int col = 0; col < width / 16; col++)
                Add(SpriteName(texName, col * 16, row * 16, 16, 16), col * 16, row * 16, 16, 16);
        if (extraRects != null)
            foreach (var r in extraRects)
                if (r.width > 0 && r.height > 0 && r.xMax <= width && r.yMax <= height)
                    Add(SpriteName(texName, r.x, r.y, r.width, r.height), r.x, r.y, r.width, r.height);

        var list = all.Values.ToArray();
        provider.SetSpriteRects(list);
        var ids = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        ids.SetNameFileIdPairs(list.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
        provider.Apply();
        importer.SaveAndReimport();
    }

    /// <summary>The Sprite asset for a region of a picture (null if it is not cut yet). w = h = 0: the whole single picture.</summary>
    public static Sprite Find(string resourcePath, int x, int y, int w, int h)
    {
        string assetPath = AssetPath(resourcePath);
        var sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().ToArray();
        if (sprites.Length == 0) return null;
        if (!PixelImportFixer.IsSheet(assetPath)) return sprites[0];
        string name = SpriteName(Path.GetFileNameWithoutExtension(assetPath), x, y, w, h);
        return sprites.FirstOrDefault(s => s.name == name);
    }
}
