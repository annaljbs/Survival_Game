using UnityEngine;

/// <summary>
/// One rain shower per run. The start time is random inside a window, so it is a little different
/// each time. A warning comes first; while it rains things outside get wet and you lose warmth faster.
/// </summary>
public class Weather : MonoBehaviour
{
    public static Weather Instance { get; private set; }

    [Header("When (hour of day)")]
    public float earliestStartHour = 18.5f;
    public float latestStartHour = 19.5f;

    [Header("Rain")]
    public float durationMinutes = 150f;
    public float warningMinutes = 20f;
    [Tooltip("Warmth loss multiplier while it rains (outside).")]
    public float rainWeatherFactor = 1.5f;

    public bool IsRaining { get; private set; }
    public float RainStartMinute { get; private set; }

    bool warned;
    SurvivalStats stats;
    ActionSystem actions;

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        stats = FindFirstObjectByType<SurvivalStats>();
        actions = FindFirstObjectByType<ActionSystem>();

        var clock = GameClock.Instance;
        float dayStart = Mathf.Floor(clock.TotalMinutes / 1440f) * 1440f;
        RainStartMinute = dayStart + Random.Range(earliestStartHour, latestStartHour) * 60f;
    }

    void Update()
    {
        var clock = GameClock.Instance;
        if (clock == null) return;
        float t = clock.TotalMinutes;

        if (!warned && t >= RainStartMinute - warningMinutes)
        {
            warned = true;
            if (actions != null)
            {
                actions.Say($"Dark clouds are rolling in. Rain in about {warningMinutes:0} minutes!");
                actions.Think("Clouds. Of course. The universe has a great sense of timing.");
            }
        }

        bool shouldRain = t >= RainStartMinute && t < RainStartMinute + durationMinutes;
        if (shouldRain == IsRaining) return;

        IsRaining = shouldRain;
        if (WorldState.Instance != null) WorldState.Instance.Set(WorldFlag.Raining, IsRaining);
        if (stats != null) stats.weatherFactor = IsRaining ? rainWeatherFactor : 1f;
        if (actions != null)
        {
            actions.Say(IsRaining ? "It starts to rain. Everything outside gets wet." : "The rain stops.");
            actions.Think(IsRaining ? "Rain. On top of everything else. Wonderful." : "The rain stops. Now everything is only slightly damp and mostly hopeless.");
        }
    }
}
