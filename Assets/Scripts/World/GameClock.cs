using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// The game clock, in game minutes. It runs continuously in real time while playing. It is paused
/// while an action card is open (thinking about a choice is free) and while an action is running
/// (the action moves the clock forward itself). Also drives the day/night lighting.
/// </summary>
public class GameClock : MonoBehaviour
{
    public static GameClock Instance { get; private set; }

    [Header("Time")]
    public float startHour = 15f;
    [Tooltip("Game minutes that pass per real second. 2 = a game hour every 30 seconds.")]
    public float realtimeMinutesPerSecond = 2f;

    [Header("Lighting")]
    public Light2D globalLight;
    public float nightIntensity = 0.15f;
    public float dayIntensity = 1f;
    public Color dayTint = Color.white;
    public Color nightTint = new Color(0.45f, 0.5f, 1f);
    [Tooltip("Colour outside the map. Dark, so the edge of the world is clearly visible.")]
    public Color cameraDay = new Color(0.03f, 0.05f, 0.04f);
    public Color cameraNight = new Color(0.01f, 0.02f, 0.03f);

    public float TotalMinutes { get; private set; }
    public float Brightness { get; private set; }

    /// <summary>Raised after time moved forward; argument = minutes advanced.</summary>
    public event Action<float> Advanced;

    public float HourOfDay => (TotalMinutes / 60f) % 24f;
    public int DayCount => 1 + (int)(TotalMinutes / 1440f);
    public bool IsNight => Brightness < 0.35f;

    public string TimeText
    {
        get
        {
            int total = Mathf.FloorToInt(TotalMinutes) % 1440;
            return $"{total / 60:00}:{total % 60:00}";
        }
    }

    Camera cam;
    ActionSystem actions;

    void Start()
    {
        actions = FindFirstObjectByType<ActionSystem>();
    }

    void Update()
    {
        var gm = GameManager.Instance;
        bool playing = gm == null || gm.State == GameState.Playing;
        bool busy = actions != null && actions.IsBusy;
        if (playing && !busy) Advance(realtimeMinutesPerSecond * Time.deltaTime);
    }

    void Awake()
    {
        Instance = this;
        TotalMinutes = startHour * 60f;
        cam = Camera.main;

        if (globalLight == null)
        {
            foreach (var l in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
            {
                if (l.lightType == Light2D.LightType.Global) { globalLight = l; break; }
            }
        }
        UpdateVisuals();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Advance(float minutes)
    {
        if (minutes <= 0f) return;
        TotalMinutes += minutes;
        Advanced?.Invoke(minutes);
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        float h = HourOfDay;
        // Daylight between 06:00 and 20:00, peaking at 13:00.
        float sun = (h >= 6f && h <= 20f) ? Mathf.Sin((h - 6f) / 14f * Mathf.PI) : 0f;
        Brightness = Mathf.Clamp01(sun * 1.4f);

        if (globalLight != null)
        {
            globalLight.intensity = Mathf.Lerp(nightIntensity, dayIntensity, Brightness);
            globalLight.color = Color.Lerp(nightTint, dayTint, Brightness);
        }
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.Lerp(cameraNight, cameraDay, Brightness);
        }
    }
}
