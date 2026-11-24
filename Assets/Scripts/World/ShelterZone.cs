using UnityEngine;

/// <summary>
/// A camp site. Nothing is sheltered at first: the player builds the shelter in three parts.
///   Frame   (poles)           - blocks the wind                    x0.85
///   Roof    (tarp or boughs)  - keeps the rain off (needs a frame) x0.80
///   Bedding (boughs on the ground) - insulates from the cold earth x0.75
/// The site itself also matters (windy meadow, rock wall, low ground that floods).
/// Needs a trigger collider.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ShelterZone : MonoBehaviour
{
    public string siteName = "Camp";
    [Tooltip("Short text about the place, shown in the HUD.")]
    public string siteNote = "";
    [Tooltip("Natural warmth loss multiplier of the site. 1.2 = windy and open, 0.8 = rock wall behind you.")]
    public float siteFactor = 1f;
    [Tooltip("Low ground: water runs in when it rains, so even a good roof does not keep you dry.")]
    public bool lowGround;

    [Header("Shelter parts (multipliers when built)")]
    public float frameFactor = 0.85f;
    public float roofFactor = 0.80f;
    public float beddingFactor = 0.75f;

    [Header("Look of the built parts (set by the scene builder)")]
    public Sprite partSprite;
    public Material partMaterial;

    public bool HasFrame { get; private set; }
    public bool HasRoof { get; private set; }
    public bool HasBedding { get; private set; }

    /// <summary>Warmth loss multiplier of this spot: site x built parts.</summary>
    public float Factor =>
        siteFactor * (HasFrame ? frameFactor : 1f) * (HasRoof ? roofFactor : 1f) * (HasBedding ? beddingFactor : 1f);

    /// <summary>The rain is kept off only by a roof, and not on low ground.</summary>
    public bool KeepsRainOff => HasRoof && !lowGround;

    /// <summary>The camp site the player stands in right now (null outside).</summary>
    public static ShelterZone Current { get; private set; }

    SurvivalStats insideStats;

    void OnTriggerEnter2D(Collider2D other)
    {
        var stats = other.GetComponent<SurvivalStats>();
        if (stats == null) return;
        insideStats = stats;
        Current = this;
        Apply(stats);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var stats = other.GetComponent<SurvivalStats>();
        if (stats == null) return;
        insideStats = null;
        if (Current == this) Current = null;
        stats.shelterFactor = 1f;
        stats.RainProtected = false;
        stats.ZoneLabel = "";
    }

    /// <summary>Null if this part can be built now, otherwise a short hint why not.</summary>
    public string CanBuild(ShelterStage stage)
    {
        switch (stage)
        {
            case ShelterStage.Frame:
                return HasFrame ? "The frame is already built." : null;
            case ShelterStage.Roof:
                if (HasRoof) return "This shelter already has a roof.";
                return HasFrame ? null : "A roof needs a frame to rest on. Build the frame first.";
            case ShelterStage.Bedding:
                return HasBedding ? "You already made a bed here." : null;
        }
        return null;
    }

    /// <summary>Build one part of the shelter (also takes effect right away if the player is inside).</summary>
    public void Build(ShelterStage stage, bool tarpRoof)
    {
        const string Camp = "Pixel/Tilesets/tileset_camp";
        const string Nature = "Pixel/Tilesets/TilesetNature";
        switch (stage)
        {
            case ShelterStage.Frame:
                if (HasFrame) return;
                HasFrame = true;
                // ridge pole on two posts
                Part("Shelter ridge pole", new Vector2(0, 0.2f), PixelArt.Get(Camp, 0, 112, 48, 16), Vector2.zero, 3, false);
                Part("Shelter post", new Vector2(-1.3f, -0.5f), PixelArt.Get(Camp, 0, 80, 16, 32), Vector2.zero, 3, false);
                Part("Shelter post", new Vector2(1.3f, -0.5f), PixelArt.Get(Camp, 0, 80, 16, 32), Vector2.zero, 3, false);
                break;
            case ShelterStage.Roof:
                if (HasRoof) return;
                HasRoof = true;
                // tarp: the cloth repeated over the frame; boughs: green bushes layered thickly
                var roof = tarpRoof ? PixelArt.Get(Camp, 128, 80, 32, 32) : PixelArt.Get(Nature, 0, 160, 16, 16);
                Part("Shelter roof", new Vector2(0, -0.6f), roof, new Vector2(4f, 2f), 4, true);
                break;
            case ShelterStage.Bedding:
                if (HasBedding) return;
                HasBedding = true;
                Part("Shelter bedding", new Vector2(0, -2.3f), PixelArt.Get("Pixel/Items/Resource/Grass", 0, 0, 0, 0), new Vector2(3f, 1f), 2, true);
                break;
        }
        if (insideStats != null) Apply(insideStats);
    }

    void Part(string partName, Vector2 offset, Sprite sprite, Vector2 size, int order, bool tiled)
    {
        // The shelter is drawn beside the fire pit (to the right), so it never covers the action objects.
        var go = new GameObject(partName);
        go.transform.SetParent(transform, false);
        go.transform.position = transform.position + new Vector3(5f + offset.x, 1.5f + offset.y, 0);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        if (partMaterial != null) sr.sharedMaterial = partMaterial;
        if (tiled && sprite != null)
        {
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = size;
        }
    }

    string Progress()
    {
        return $"frame {(HasFrame ? "[x]" : "[ ]")}  roof {(HasRoof ? "[x]" : "[ ]")}  bed {(HasBedding ? "[x]" : "[ ]")}";
    }

    void Apply(SurvivalStats stats)
    {
        stats.shelterFactor = Factor;
        stats.RainProtected = KeepsRainOff;
        string note = string.IsNullOrEmpty(siteNote) ? "" : $" - {siteNote}";
        string wetFloor = lowGround && HasRoof ? " (roof is not enough: water runs in!)" : "";
        stats.ZoneLabel = $"{siteName}{note}   |   Shelter: {Progress()}{wetFloor}   warmth loss x{Factor:0.0#}";
    }
}
