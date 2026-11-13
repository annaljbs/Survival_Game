using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>Cycles a global 2D light (and camera background) between day and night.</summary>
public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance { get; private set; }

    public float dayLengthSeconds = 120f;
    [Range(0f, 1f)] public float startTime = 0.25f; // 0 = midnight, 0.5 = noon
    public float nightIntensity = 0.12f;
    public float dayIntensity = 1f;
    public Color dayTint = Color.white;
    public Color nightTint = new Color(0.45f, 0.5f, 1f);
    public Light2D globalLight;

    public float Time01 { get; private set; }
    public float Brightness { get; private set; }
    public bool IsNight => Brightness < 0.4f;
    public int DayCount { get; private set; } = 1;

    Camera cam;
    Color camDay = new Color(0.35f, 0.55f, 0.3f);
    Color camNight = new Color(0.02f, 0.04f, 0.08f);

    void Awake()
    {
        Instance = this;
        Time01 = startTime;
        cam = Camera.main;
        if (globalLight == null)
        {
            globalLight = gameObject.AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
        }
    }

    void Update()
    {
        float prev = Time01;
        Time01 = (Time01 + Time.deltaTime / dayLengthSeconds) % 1f;
        if (Time01 < prev) DayCount++;

        // cos: midnight (0) -> 0, noon (0.5) -> 1
        Brightness = 0.5f - 0.5f * Mathf.Cos(Time01 * Mathf.PI * 2f);
        globalLight.intensity = Mathf.Lerp(nightIntensity, dayIntensity, Brightness);
        globalLight.color = Color.Lerp(nightTint, dayTint, Brightness);
        if (cam != null) cam.backgroundColor = Color.Lerp(camNight, camDay, Brightness);
    }
}
