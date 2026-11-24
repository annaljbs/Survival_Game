using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// A fire pit. Lit by an action (needs dry tinder, kindling, logs and a lighter), burns for a limited
/// time, can be refuelled, warms the player nearby and allows boiling water.
/// </summary>
public class Campfire : MonoBehaviour
{
    static readonly List<Campfire> All = new List<Campfire>();

    public float burnMinutes = 180f;
    public float maxBurnMinutes = 360f;
    [Tooltip("How close you must be to feel the warmth.")]
    public float warmRadius = 4.5f;
    [Tooltip("Warmth gained per game minute next to the fire (night loss is 0.10).")]
    public float warmthPerMinute = 0.20f;

    public float TimeLeft { get; private set; }
    public bool IsLit => TimeLeft > 0f;

    SpriteRenderer sr;
    Color unlitColor = Color.gray;
    Light2D glow;
    ActionSystem actions;
    float seed;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) unlitColor = sr.color;
        seed = Random.value * 100f;

        var go = new GameObject("Fire glow");
        go.transform.SetParent(transform, false);
        glow = go.AddComponent<Light2D>();
        glow.lightType = Light2D.LightType.Point;
        glow.pointLightOuterRadius = 8f;
        glow.pointLightInnerRadius = 1f;
        glow.color = new Color(1f, 0.6f, 0.25f);
        glow.intensity = 0f;
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Start()
    {
        actions = FindFirstObjectByType<ActionSystem>();
        if (GameClock.Instance != null) GameClock.Instance.Advanced += OnAdvanced;
    }

    void OnDestroy()
    {
        if (GameClock.Instance != null) GameClock.Instance.Advanced -= OnAdvanced;
    }

    public void Light()
    {
        TimeLeft = burnMinutes;
        Refresh();
        UpdateWorldFlag();
    }

    public void AddFuel(float minutes)
    {
        TimeLeft = Mathf.Min(maxBurnMinutes, TimeLeft + minutes);
        Refresh();
    }

    void OnAdvanced(float minutes)
    {
        if (!IsLit) return;
        TimeLeft -= minutes;
        if (TimeLeft > 0f) return;

        TimeLeft = 0f;
        Refresh();
        UpdateWorldFlag();
        if (actions != null) actions.Say("The fire has gone out.");
    }

    void Update()
    {
        // Flickering glow.
        glow.intensity = IsLit ? 1.1f + 0.4f * Mathf.PerlinNoise(Time.time * 7f, seed) : 0f;
    }

    void Refresh()
    {
        if (sr != null) sr.color = IsLit ? new Color(1f, 0.55f, 0.15f) : unlitColor;
    }

    static void UpdateWorldFlag()
    {
        if (WorldState.Instance == null) return;
        bool any = false;
        foreach (var f in All) if (f.IsLit) { any = true; break; }
        WorldState.Instance.Set(WorldFlag.FireLit, any);
    }

    /// <summary>Warmth per game minute that lit fires give at this position (0 if none is close).</summary>
    public static float WarmthAt(Vector2 position)
    {
        float best = 0f;
        foreach (var f in All)
        {
            if (!f.IsLit) continue;
            if (Vector2.Distance(position, f.transform.position) <= f.warmRadius)
                best = Mathf.Max(best, f.warmthPerMinute);
        }
        return best;
    }

    /// <summary>The closest lit fire within maxDistance, or null.</summary>
    public static Campfire NearestLit(Vector2 position, float maxDistance)
    {
        Campfire best = null;
        float bestDist = maxDistance;
        foreach (var f in All)
        {
            if (!f.IsLit) continue;
            float d = Vector2.Distance(position, f.transform.position);
            if (d <= bestDist) { bestDist = d; best = f; }
        }
        return best;
    }
}
