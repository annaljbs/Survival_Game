using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads pixel-art pictures from Assets/Resources/Pixel and cuts sprites out of them at run time.
/// 16 pixels = 1 world unit. Regions are given in pixels from the TOP-left corner (like in an image editor).
/// Free art: "Ninja Adventure Asset Pack" by pixel-boy (CC0), see docs/CREDITS.md.
/// </summary>
public static class PixelArt
{
    public const float PixelsPerUnit = 16f;

    static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
    static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

    public static Texture2D Texture(string resourcePath)
    {
        if (textures.TryGetValue(resourcePath, out var cached) && cached != null) return cached;
        var tex = Resources.Load<Texture2D>(resourcePath);
        if (tex == null)
        {
            Debug.LogWarning("PixelArt: picture not found: Resources/" + resourcePath);
            return null;
        }
        tex.filterMode = FilterMode.Point;   // in case the importer settings did not apply yet
        textures[resourcePath] = tex;
        return tex;
    }

    /// <summary>A sprite from a rectangle of a picture. w or h = 0 means the whole picture.</summary>
    public static Sprite Get(string resourcePath, int x, int yFromTop, int w, int h)
    {
        string key = $"{resourcePath}:{x},{yFromTop},{w},{h}";
        if (sprites.TryGetValue(key, out var cached) && cached != null) return cached;

        var tex = Texture(resourcePath);
        if (tex == null) return null;
        if (w <= 0 || h <= 0)
        {
            x = 0; yFromTop = 0; w = tex.width; h = tex.height;
        }
        var rect = new Rect(x, tex.height - yFromTop - h, w, h);
        var sprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
        sprite.name = key;
        sprites[key] = sprite;
        return sprite;
    }

    public static Sprite Whole(string resourcePath) => Get(resourcePath, 0, 0, 0, 0);
}
