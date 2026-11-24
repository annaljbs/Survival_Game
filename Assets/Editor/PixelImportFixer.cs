using UnityEditor;
using UnityEngine;

/// <summary>
/// Import settings for all pixel art under Assets/Resources/Pixel: sharp (point filter, no compression, no mipmaps),
/// 16 pixels per unit, and real Sprites. Big pictures (tilesets, character sheets, effects) are "Multiple" sprites
/// that get cut into 16x16 pieces by Survival > Make Level Editable; single pictures (item icons, UI, custom art) are one sprite.
/// </summary>
public class PixelImportPostprocessor : AssetPostprocessor
{
    public const string Folder = "Assets/Resources/Pixel";

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Folder)) return;
        PixelImportFixer.Apply((TextureImporter)assetImporter, assetPath);
    }
}

[InitializeOnLoad]
public static class PixelImportFixer
{
    static PixelImportFixer()
    {
        EditorApplication.delayCall += FixAll;
    }

    /// <summary>Tilesets, character sheets and effect sheets are cut into many sprites.</summary>
    public static bool IsSheet(string assetPath)
    {
        return assetPath.Contains("/Pixel/Tilesets/") || assetPath.Contains("/Pixel/Characters/") || assetPath.Contains("/Pixel/Fx/");
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

    static bool NeedsFix(TextureImporter t, string path)
    {
        var mode = IsSheet(path) ? SpriteImportMode.Multiple : SpriteImportMode.Single;
        return t.textureType != TextureImporterType.Sprite || t.spriteImportMode != mode ||
               !Mathf.Approximately(t.spritePixelsPerUnit, 16f) || t.filterMode != FilterMode.Point ||
               t.textureCompression != TextureImporterCompression.Uncompressed || t.mipmapEnabled;
    }

    static void FixAll()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { PixelImportPostprocessor.Folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || !NeedsFix(importer, path)) continue;
            Apply(importer, path);
            importer.SaveAndReimport();
        }
    }
}
