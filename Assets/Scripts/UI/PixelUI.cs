using UnityEngine;

/// <summary>
/// Small pixel-art UI pieces drawn in code (no image files needed): the dialogue box frame
/// in the style of classic 2D top-down RPGs (rounded corners, dark outline, light inner edge, cream fill).
/// Replace later with the box sprite of an art pack if you like.
/// </summary>
public static class PixelUI
{
    public static readonly Color Dark = new Color(0.10f, 0.10f, 0.18f);
    public static readonly Color Edge = new Color(0.45f, 0.50f, 0.70f);
    public static readonly Color Cream = new Color(0.97f, 0.95f, 0.88f);

    static Sprite box, arrow;

    /// <summary>A 16x16 framed box. Use it as a 9-sliced Image (border = 3 source pixels).</summary>
    public static Sprite Box()
    {
        if (box != null) return box;
        const int n = 16;
        var tex = NewTexture(n, n);
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                int d = Mathf.Min(Mathf.Min(x, n - 1 - x), Mathf.Min(y, n - 1 - y));
                Color c = d == 0 ? Dark : (d == 1 ? Edge : Cream);
                bool cornerX = x == 0 || x == n - 1;
                bool cornerY = y == 0 || y == n - 1;
                if (cornerX && cornerY) c = Color.clear;   // rounded corner
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        box = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(3, 3, 3, 3));
        return box;
    }

    /// <summary>A small triangle pointing down: the "more text" / "done" marker.</summary>
    public static Sprite Arrow()
    {
        if (arrow != null) return arrow;
        const int n = 8;
        var tex = NewTexture(n, n);
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                // Triangle with its tip at the bottom: row y has width (y + 1) * 2 - 1, centred.
                int half = y;                       // y = 0 is the bottom (the tip)
                bool inside = Mathf.Abs(x - 3.5f) <= half * 0.5f + 0.5f && y < 5;
                tex.SetPixel(x, y, inside ? Dark : Color.clear);
            }
        }
        tex.Apply();
        arrow = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        return arrow;
    }

    static Texture2D NewTexture(int w, int h)
    {
        return new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
    }
}
