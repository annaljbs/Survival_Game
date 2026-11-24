using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Sound for the whole game. Creates itself when the game starts (no scene setup needed).
/// Looping ambience: wind, rain (while it rains), river (near the stream), crackling fire (near a lit fire).
/// Music: calm by day, dark forest at night, tense while any bar is critical.
/// Short sounds: pickups, finished actions, refused actions, warnings.
/// Press M to mute. Sounds and music: Ninja Adventure pack (CC0).
/// </summary>
public class AudioDirector : MonoBehaviour
{
    [Range(0f, 1f)] public float ambienceVolume = 0.5f;
    [Range(0f, 1f)] public float musicVolume = 0.35f;
    [Range(0f, 1f)] public float sfxVolume = 0.7f;
    [Tooltip("Seconds to fade between music tracks and ambience.")]
    public float fadeSeconds = 2f;

    static AudioDirector instance;

    AudioSource wind, rain, river, fireLoop, musicDay, musicNight, musicTension, sfx;
    AudioClip coin, done, refused, warning;
    bool muted;

    ActionSystem actions;
    SurvivalStats stats;
    PlayerController player;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (instance != null) return;
        var go = new GameObject("AudioDirector");
        go.AddComponent<AudioDirector>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;

        wind = Loop("Pixel/Audio/Ambient/Wind2");
        rain = Loop("Pixel/Audio/Ambient/Rain");
        river = Loop("Pixel/Audio/Ambient/River");
        fireLoop = Loop("Pixel/Audio/Ambient/Fire");
        musicDay = Loop("Pixel/Audio/Music/Day");
        musicNight = Loop("Pixel/Audio/Music/Night");
        musicTension = Loop("Pixel/Audio/Music/Tension");
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;

        coin = Resources.Load<AudioClip>("Pixel/Audio/Sfx/Coin");
        done = Resources.Load<AudioClip>("Pixel/Audio/Sfx/Done");
        refused = Resources.Load<AudioClip>("Pixel/Audio/Sfx/Refused");
        warning = Resources.Load<AudioClip>("Pixel/Audio/Sfx/Warning");
    }

    AudioSource Loop(string path)
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.clip = Resources.Load<AudioClip>(path);
        source.loop = true;
        source.volume = 0f;
        source.playOnAwake = false;
        if (source.clip != null) source.Play();
        return source;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
        Unsubscribe();
    }

    // ------------------------------------------------------------------ link to the game objects (they change when the scene reloads)

    void Link()
    {
        if (actions == null)
        {
            Unsubscribe();
            actions = FindFirstObjectByType<ActionSystem>();
            if (actions != null)
            {
                actions.ActionCompleted += OnActionCompleted;
                actions.Refused += OnRefused;
            }
        }
        if (stats == null)
        {
            stats = FindFirstObjectByType<SurvivalStats>();
            if (stats != null) stats.LevelChanged += OnLevelChanged;
        }
        if (player == null) player = FindFirstObjectByType<PlayerController>();
    }

    void Unsubscribe()
    {
        if (actions != null)
        {
            actions.ActionCompleted -= OnActionCompleted;
            actions.Refused -= OnRefused;
        }
        if (stats != null) stats.LevelChanged -= OnLevelChanged;
    }

    void OnActionCompleted(Interactable target)
    {
        bool pickup = target.ActiveAction.grants != null && target.ActiveAction.grants.Length > 0 && target.ActiveAction.minutes <= 5f;
        Play(pickup ? coin : done);
    }

    void OnRefused() => Play(refused);

    void OnLevelChanged(StatType type, int level)
    {
        if (level > 0) Play(warning);
    }

    void Play(AudioClip clip)
    {
        if (clip != null && !muted) sfx.PlayOneShot(clip, sfxVolume);
    }

    // ------------------------------------------------------------------ mixing

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame) muted = !muted;
        Link();

        float k = muted ? 0f : 1f;
        float step = Time.unscaledDeltaTime / Mathf.Max(0.1f, fadeSeconds);

        var clock = GameClock.Instance;
        bool night = clock != null && clock.IsNight;
        bool raining = Weather.Instance != null && Weather.Instance.IsRaining;
        bool danger = stats != null && AnyCritical();
        bool playing = GameManager.Instance == null || GameManager.Instance.State == GameState.Playing;

        // Ambience
        Fade(wind, k * ambienceVolume * (night ? 0.6f : 0.35f), step);
        Fade(rain, k * ambienceVolume * (raining ? 0.9f : 0f), step);
        Fade(river, k * ambienceVolume * RiverLoudness(), step);
        Fade(fireLoop, k * ambienceVolume * FireLoudness(), step);

        // Music: one track at a time, tension wins
        float day = (!night && !danger && playing) ? 1f : 0f;
        float nightMusic = (night && !danger && playing) ? 1f : 0f;
        float tension = (danger && playing) ? 1f : 0f;
        Fade(musicDay, k * musicVolume * day, step);
        Fade(musicNight, k * musicVolume * nightMusic, step);
        Fade(musicTension, k * musicVolume * tension, step);
    }

    static void Fade(AudioSource source, float target, float step)
    {
        if (source == null || source.clip == null) return;
        source.volume = Mathf.MoveTowards(source.volume, target, step * 0.5f);
    }

    bool AnyCritical()
    {
        for (int i = 0; i < 4; i++) if (stats.GetLevel((StatType)i) == 2) return true;
        return false;
    }

    /// <summary>Louder the closer the player is to the stream (x = 57, from y = -15 to 55).</summary>
    float RiverLoudness()
    {
        if (player == null) return 0f;
        Vector2 p = player.transform.position;
        Vector2 nearest = new Vector2(57f, Mathf.Clamp(p.y, -15f, 55f));
        return Mathf.Clamp01(1f - Vector2.Distance(p, nearest) / 28f);
    }

    /// <summary>Louder the closer the player is to a lit fire.</summary>
    float FireLoudness()
    {
        if (player == null) return 0f;
        var fire = Campfire.NearestLit(player.transform.position, 18f);
        if (fire == null) return 0f;
        return Mathf.Clamp01(1f - Vector2.Distance(player.transform.position, fire.transform.position) / 18f);
    }
}
