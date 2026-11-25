using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// Import settings for the complete Ninja Adventure pack in Assets/Art/NinjaAdventure:
/// every picture becomes a sharp pixel sprite (point filter, 16 pixels per unit, no compression).
/// The sheets (tilesets and character/animal sprite sheets) are cut into 16x16 pieces by
/// Survival > Slice Ninja Pack Sheets (16x16), so you can drag them into the Tile Palette.
/// Names are like the old ones: "TilesetRelief_3_10" = column 3, row 10 counted from the top-left.
/// </summary>
public class NinjaPackImportPostprocessor : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(NinjaPackTools.Folder)) return;
        NinjaPackTools.Apply((TextureImporter)assetImporter, assetPath);
    }
}

public static class NinjaPackTools
{
    public const string Folder = "Assets/Art/NinjaAdventure";

    /// <summary>Tileset sheets and the "SpriteSheet.png" of every actor (animals, characters, monsters, bosses).</summary>
    public static bool IsSheet(string assetPath)
    {
        if (assetPath.Contains("/Backgrounds/Tilesets/")) return true;
        if (assetPath.Contains("/Actor/") && Path.GetFileName(assetPath) == "SpriteSheet.png") return true;
        return false;
    }

    public static void Apply(TextureImporter importer, string assetPath)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = IsSheet(assetPath) ? SpriteImportMode.Multiple : SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 4096;
    }

    [MenuItem("Survival/Slice Ninja Pack Sheets (16x16)")]
    public static void SliceAll()
    {
        var paths = AssetDatabase.FindAssets("t:Texture2D", new[] { Folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(IsSheet)
            .ToArray();

        int done = 0;
        try
        {
            foreach (string path in paths)
            {
                EditorUtility.DisplayProgressBar("Slicing Ninja Pack", path, (float)done / paths.Length);
                SliceOne(path);
                done++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        Debug.Log("Ninja Pack: cut " + done + " sheets into 16x16 sprites.");
    }

    /// <summary>Width and height from the PNG header (bytes 16 to 23, big endian).</summary>
    static bool ReadPngSize(string assetPath, out int width, out int height)
    {
        width = height = 0;
        byte[] head = new byte[24];
        using (var f = File.OpenRead(assetPath))
            if (f.Read(head, 0, 24) < 24) return false;
        width = (head[16] << 24) | (head[17] << 16) | (head[18] << 8) | head[19];
        height = (head[20] << 24) | (head[21] << 16) | (head[22] << 8) | head[23];
        return true;
    }

    static void SliceOne(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;
        Apply(importer, assetPath);

        if (!ReadPngSize(assetPath, out int width, out int height) || width < 16 || height < 16) return;

        string texName = Path.GetFileNameWithoutExtension(assetPath);
        // actor sheets are all called "SpriteSheet": use the folder name instead, so every name is unique
        if (texName == "SpriteSheet") texName = Path.GetFileName(Path.GetDirectoryName(assetPath));

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        var all = new Dictionary<string, SpriteRect>();
        foreach (var r in provider.GetSpriteRects()) all[r.name] = r;   // keep what already exists

        for (int row = 0; row < height / 16; row++)
            for (int col = 0; col < width / 16; col++)
            {
                string name = texName + "_" + col + "_" + row;
                if (all.ContainsKey(name)) continue;
                all[name] = new SpriteRect
                {
                    name = name,
                    rect = new Rect(col * 16, height - (row + 1) * 16, 16, 16),   // Unity counts y from the bottom
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    spriteID = GUID.Generate(),
                };
            }

        var list = all.Values.ToArray();
        provider.SetSpriteRects(list);
        var ids = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        ids.SetNameFileIdPairs(list.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
        provider.Apply();
        importer.SaveAndReimport();
    }
}
