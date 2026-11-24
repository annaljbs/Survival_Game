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

    SurvivalStats insideStats;

    void OnTriggerEnter2D(Collider2D other)
    {
        var stats = other.GetComponent<SurvivalStats>();
        if (stats == null) return;
        insideStats = stats;
        Apply(stats);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var stats = other.GetComponent<SurvivalStats>();
        if (stats == null) return;
        insideStats = null;
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
        switch (stage)
        {
            case ShelterStage.Frame:
                if (HasFrame) return;
                HasFrame = true;
                Part("Shelter frame", new Vector2(0, 0), new Vector2(4.4f, 0.4f), new Color(0.35f, 0.22f, 0.10f), 4);
                Part("Shelter post", new Vector2(-2f, -1.1f), new Vector2(0.4f, 2.4f), new Color(0.35f, 0.22f, 0.10f), 4);
                Part("Shelter post", new Vector2(2f, -1.1f), new Vector2(0.4f, 2.4f), new Color(0.35f, 0.22f, 0.10f), 4);
                break;
            case ShelterStage.Roof:
                if (HasRoof) return;
                HasRoof = true;
                var roofColor = tarpRoof ? new Color(0.30f, 0.65f, 0.85f, 0.9f) : new Color(0.10f, 0.40f, 0.15f, 0.95f);
                Part("Shelter roof", new Vector2(0, -0.9f), new Vector2(4.8f, 2.6f), roofColor, 6);
                break;
            case ShelterStage.Bedding:
                if (HasBedding) return;
                HasBedding = true;
                Part("Shelter bedding", new Vector2(0, -2.6f), new Vector2(3.6f, 1.4f), new Color(0.28f, 0.55f, 0.22f), 3);
                break;
        }
        if (insideStats != null) Apply(insideStats);
    }

    void Part(string partName, Vector2 offset, Vector2 size, Color color, int order)
    {
        // The shelter is drawn beside the fire pit (to the right), so it never covers the action objects.
        var go = new GameObject(partName);
        go.transform.SetParent(transform, false);
        go.transform.position = transform.position + new Vector3(5f + offset.x, 1.5f + offset.y, 0);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = partSprite;
        sr.color = color;
        sr.sortingOrder = order;
        if (partMaterial != null) sr.sharedMaterial = partMaterial;
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
