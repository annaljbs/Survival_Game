using UnityEngine;

/// <summary>
/// Gives a coloured placeholder square its pixel-art picture when the game starts.
/// The collider keeps its size in the world. Runs before other scripts (Interactable remembers the colour).
/// Size 0 = natural size of the picture (16 pixels = 1 unit). With "tiled" the picture repeats over the
/// original size of the square (ground, water).
/// </summary>
[DefaultExecutionOrder(-200)]
[RequireComponent(typeof(SpriteRenderer))]
public class PixelSkin : MonoBehaviour
{
    [Tooltip("Path inside Assets/Resources, without .png (e.g. Pixel/Items/Object/Bag)")]
    public string resource = "";
    [Tooltip("Region in pixels from the top-left. All 0 = whole picture.")]
    public int x, y, w, h;
    [Tooltip("World size. 0 = natural size.")]
    public Vector2 size;
    public bool tiled;

    void Awake()
    {
        var sprite = PixelArt.Get(resource, x, y, w, h);
        if (sprite == null) return;   // picture missing: keep the coloured square
        ApplySprite(sprite);
    }

    /// <summary>Put this sprite on the object (size, tiling, collider). Also used by the editor tool that makes the level editable.</summary>
    public void ApplySprite(Sprite sprite)
    {
        var sr = GetComponent<SpriteRenderer>();

        // Remember how big the collider is in the world, so the game feels the same as before.
        var box = GetComponent<BoxCollider2D>();
        Vector2 worldCollider = box != null ? Vector2.Scale(box.size, transform.localScale) : Vector2.zero;

        sr.sprite = sprite;
        sr.color = Color.white;

        if (tiled)
        {
            Vector2 area = size != Vector2.zero ? size : new Vector2(transform.localScale.x, transform.localScale.y);
            transform.localScale = Vector3.one;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = area;
            if (box != null) box.size = worldCollider;
        }
        else
        {
            Vector2 natural = sprite.rect.size / PixelArt.PixelsPerUnit;
            Vector2 target = size != Vector2.zero ? size : natural;
            transform.localScale = new Vector3(target.x / natural.x, target.y / natural.y, 1f);
            if (box != null)
            {
                box.size = new Vector2(worldCollider.x / transform.localScale.x, worldCollider.y / transform.localScale.y);
            }
        }
    }
}
