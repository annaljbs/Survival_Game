using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Decides WHEN the character thinks out loud (the dark-humour bubbles). The words are in ThoughtLibrary.
/// "Important" thoughts (missions, rain, bars in danger, the phone) always show.
/// Flavour thoughts (gathering, walking, idling) wait for a cooldown so the bubbles never become spam.
/// An action's own "Thought" text always wins over the generic pool for that action.
/// </summary>
public class ThoughtDirector : MonoBehaviour
{
    [Header("Flavour bubbles")]
    [Tooltip("Minimum real seconds between two flavour bubbles (gathering, walking, idling, ...).")]
    public float minSecondsBetweenRandom = 14f;
    [Tooltip("Real seconds of standing still before the character comments.")]
    public float idleSeconds = 25f;
    [Tooltip("Units walked before the character comments on the walk.")]
    public float walkUnitsPerComment = 90f;
    [Tooltip("Chance of a comment after gathering something (0 to 1).")]
    [Range(0f, 1f)] public float chanceAfterGather = 0.35f;
    [Tooltip("Chance of a comment when an action is refused (0 to 1).")]
    [Range(0f, 1f)] public float chanceWhenRefused = 0.6f;

    ActionSystem actions;
    SurvivalStats stats;
    PlayerController player;
    Inventory inventory;

    float nextFlavourTime;
    int phoneChecks;
    int lastJunkCount;
    bool wasWet;
    TerrainZone lastTerrain;
    ShelterZone lastZone;
    readonly HashSet<ShelterZone> visitedSites = new HashSet<ShelterZone>();
    readonly HashSet<Campfire> dyingFires = new HashSet<Campfire>();
    readonly HashSet<int> firedTimes = new HashSet<int>();

    Vector2 lastPos;
    float idleTimer;
    float walked;
    float nextRainThought, nextCosyThought;

    // Game times (in minutes since day 1, 00:00) with a thought each.
    static readonly (int minute, string pool)[] TimeThoughts =
    {
        (20 * 60, "sunset"),
        (24 * 60, "midnight"),
        (27 * 60, "witching"),
        (30 * 60, "sunrise"),
        (36 * 60, "noon2"),
        (44 * 60, "sunset2"),
    };

    void Start()
    {
        ThoughtLibrary.Reset();
        actions = FindFirstObjectByType<ActionSystem>();
        stats = FindFirstObjectByType<SurvivalStats>();
        player = FindFirstObjectByType<PlayerController>();
        inventory = FindFirstObjectByType<Inventory>();

        if (actions != null)
        {
            actions.ActionCompleted += OnActionCompleted;
            actions.Refused += OnRefused;
        }
        if (stats != null) stats.LevelChanged += OnLevelChanged;
        if (ObjectiveManager.Instance != null) ObjectiveManager.Instance.Completed += OnMissionCompleted;
        if (player != null) lastPos = player.transform.position;
    }

    void OnDestroy()
    {
        if (actions != null)
        {
            actions.ActionCompleted -= OnActionCompleted;
            actions.Refused -= OnRefused;
        }
        if (stats != null) stats.LevelChanged -= OnLevelChanged;
        if (ObjectiveManager.Instance != null) ObjectiveManager.Instance.Completed -= OnMissionCompleted;
    }

    // ------------------------------------------------------------------ showing

    void Show(string text, bool important)
    {
        if (string.IsNullOrEmpty(text) || actions == null) return;
        if (!important && Time.time < nextFlavourTime) return;
        actions.Think(text);
        nextFlavourTime = Time.time + minSecondsBetweenRandom;
    }

    void ShowPool(string pool, bool important, float chance = 1f)
    {
        if (chance < 1f && Random.value > chance) return;
        Show(ThoughtLibrary.Pick(pool), important);
    }

    // ------------------------------------------------------------------ events

    void OnActionCompleted(Interactable target)
    {
        var a = target.ActiveAction;

        if (a.milestone == "phone_checked")
        {
            phoneChecks++;
            Show(ThoughtLibrary.Phone(phoneChecks), true);
            return;
        }
        CountJunk();

        // An action with its own thought already showed it. Just keep the flavour bubbles from piling up.
        if (!string.IsNullOrEmpty(a.thought))
        {
            nextFlavourTime = Time.time + minSecondsBetweenRandom;
            return;
        }

        string pool = null;
        float chance = 1f;
        if (a.shelterStage == ShelterStage.Frame) pool = "frame";
        else if (a.shelterStage == ShelterStage.Roof) pool = "roof";
        else if (a.shelterStage == ShelterStage.Bedding) pool = "bed";
        else if (a.fireEffect == FireEffect.Light) pool = "fire_light";
        else if (a.fireEffect == FireEffect.AddFuel) { pool = "fire_add"; chance = 0.5f; }
        else if (a.milestone == "water_boiled") pool = "boil";
        else if (a.milestone == "water_filled" || a.name.Contains("Drink")) { pool = "water"; chance = 0.6f; }
        else if (a.isRest) pool = "sleep_wake";
        else if (target.moveTo != null) pool = "climb";
        else if (a.setsFlag == WorldFlag.SignalReady) pool = "signal";
        else if (a.name.StartsWith("Search") || a.name.StartsWith("Take") || a.name.StartsWith("Cut loose")) { pool = "search"; chance = 0.5f; }
        else if (a.grants != null && a.grants.Length > 0) { pool = "gather"; chance = chanceAfterGather; }

        if (pool != null) ShowPool(pool, pool != "gather" && pool != "search" && pool != "water" && pool != "fire_add", chance);
    }

    void OnRefused()
    {
        ShowPool("refused", false, chanceWhenRefused);
    }

    void OnMissionCompleted(Objective o)
    {
        string pool = "m_" + o.id;
        if (ThoughtLibrary.Has(pool)) ShowPool(pool, true);
    }

    void OnLevelChanged(StatType type, int level)
    {
        if (level == 0) return;
        ShowPool((level == 2 ? "crit_" : "low_") + type, true);
    }

    void CountJunk()
    {
        if (inventory == null) return;
        int junk = 0;
        foreach (var kv in inventory.All) if (kv.Key.IsJunk()) junk += kv.Value;
        if (junk >= 6 && lastJunkCount < 6) ShowPool("junk6", false);
        else if (junk >= 3 && lastJunkCount < 3) ShowPool("junk3", false);
        lastJunkCount = junk;
    }

    // ------------------------------------------------------------------ per-frame checks

    void Update()
    {
        if (stats == null || actions == null || player == null) return;
        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return;

        CheckTime();
        CheckMovement();
        CheckPlace();
        CheckWeather();
        CheckFire();

        if (stats.IsWet && !wasWet) ShowPool("wet", true);
        wasWet = stats.IsWet;
    }

    void CheckTime()
    {
        var clock = GameClock.Instance;
        if (clock == null) return;
        // If several times were skipped at once (debug), only the latest one is said.
        string latest = null;
        foreach (var t in TimeThoughts)
        {
            if (clock.TotalMinutes < t.minute || firedTimes.Contains(t.minute)) continue;
            firedTimes.Add(t.minute);
            latest = t.pool;
        }
        if (latest != null) ShowPool(latest, true);
    }

    void CheckMovement()
    {
        Vector2 pos = player.transform.position;
        float moved = Vector2.Distance(pos, lastPos);
        lastPos = pos;

        if (moved > 0.002f && moved < 5f)
        {
            idleTimer = 0f;
            walked += moved;
            if (walked >= walkUnitsPerComment)
            {
                walked = 0f;
                ShowPool("walk", false);
            }
        }
        else if (!actions.IsBusy)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleSeconds)
            {
                idleTimer = 0f;
                ShowPool("idle", false);
            }
        }
        else
        {
            idleTimer = 0f;
        }
    }

    void CheckPlace()
    {
        var terrain = player.CurrentTerrain;
        if (terrain != lastTerrain)
        {
            lastTerrain = terrain;
            if (terrain != null)
            {
                if (terrain.label.Contains("River")) ShowPool("river", true);
                else if (terrain.label.Contains("Thick")) ShowPool("thick", false);
            }
        }

        var zone = ShelterZone.Current;
        if (zone != lastZone)
        {
            lastZone = zone;
            if (zone != null && visitedSites.Add(zone))
            {
                string pool = "site_" + zone.siteName;
                if (ThoughtLibrary.Has(pool)) ShowPool(pool, false);
            }
        }
    }

    void CheckWeather()
    {
        var weather = Weather.Instance;
        if (weather == null || !weather.IsRaining) return;

        var zone = ShelterZone.Current;
        bool underRoof = zone != null && zone.HasRoof;

        if (!stats.RainProtected && ObjectiveManager.Instance != null) ObjectiveManager.Instance.Milestone("rain_exposed");

        if (underRoof && zone.lowGround)
        {
            if (Time.time >= nextCosyThought)
            {
                nextCosyThought = Time.time + 60f;
                ShowPool("flood", true);
            }
        }
        else if (underRoof)
        {
            if (Time.time >= nextCosyThought)
            {
                nextCosyThought = Time.time + 75f;
                ShowPool("cosy", false);
            }
        }
        else if (!stats.RainProtected && Time.time >= nextRainThought)
        {
            nextRainThought = Time.time + 45f;
            ShowPool("rain_no_roof", false);
        }
    }

    void CheckFire()
    {
        dyingFires.RemoveWhere(f => f == null || f.TimeLeft > 60f);
        var fire = Campfire.NearestLit(player.transform.position, 12f);
        if (fire == null || fire.TimeLeft > 25f || dyingFires.Contains(fire)) return;
        dyingFires.Add(fire);
        ShowPool("fire_dying", true);
    }
}
