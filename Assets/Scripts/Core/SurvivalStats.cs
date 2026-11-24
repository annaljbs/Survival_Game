using System;
using UnityEngine;

public enum StatType { Energy, Water, Food, Warmth }

/// <summary>
/// The four survival values (all 0-100, full = good). All of them fall with game time;
/// Energy falls slowly with time, faster while walking, and through actions. Any value at 0 ends the game.
/// </summary>
public class SurvivalStats : MonoBehaviour
{
    public float max = 100f;

    [Header("Loss per game minute")]
    public float waterLoss = 0.10f;        // empty after ~17 game hours without drinking
    public float foodLoss = 0.04f;
    public float warmthLossDay = 0.02f;
    public float warmthLossNight = 0.10f;
    [Tooltip("Energy lost per game minute just from being awake. Walking costs extra on top (see PlayerController).")]
    public float energyLossPerMinute = 0.03f;

    [Header("Warning levels")]
    public float warnLevel = 30f;
    public float criticalLevel = 15f;

    [Header("Warmth loss multipliers (below 1 = protected, above 1 = exposed)")]
    [Tooltip("Clothing: the jacket sets this to 0.6.")]
    public float gearFactor = 1f;
    [Tooltip("Set by the ShelterZone the player stands in (meadow, forest edge, rock overhang).")]
    public float shelterFactor = 1f;
    [Tooltip("Set by Weather while it rains.")]
    public float weatherFactor = 1f;

    /// <summary>Total multiplier on warmth loss: gear x shelter x weather.</summary>
    public float ExposureMultiplier => gearFactor * shelterFactor * weatherFactor;

    /// <summary>Name of the place the player is in (set by ShelterZone), for the HUD.</summary>
    public string ZoneLabel { get; set; } = "";

    public float Energy { get; private set; }
    public float Water { get; private set; }
    public float Food { get; private set; }
    public float Warmth { get; private set; }

    /// <summary>True while a resting action (sleep) runs: no passive energy loss.</summary>
    public bool IsResting { get; set; }

    public bool IsDead { get; private set; }
    public string DeathCause { get; private set; } = "";
    public StatType? DeathStat { get; private set; }

    public event Action Changed;
    public event Action Died;
    /// <summary>Level 0 = fine, 1 = low, 2 = critical.</summary>
    public event Action<StatType, int> LevelChanged;

    readonly int[] levels = new int[4];

    void Awake()
    {
        Energy = Water = Food = Warmth = max;
    }

    void Start()
    {
        if (GameClock.Instance != null) GameClock.Instance.Advanced += OnAdvanced;
    }

    void OnDestroy()
    {
        if (GameClock.Instance != null) GameClock.Instance.Advanced -= OnAdvanced;
    }

    public float Get(StatType type)
    {
        switch (type)
        {
            case StatType.Energy: return Energy;
            case StatType.Water: return Water;
            case StatType.Food: return Food;
            default: return Warmth;
        }
    }

    public int GetLevel(StatType type) => levels[(int)type];

    void OnAdvanced(float minutes)
    {
        if (IsDead) return;

        bool night = GameClock.Instance != null && GameClock.Instance.IsNight;
        float warmthLoss = (night ? warmthLossNight : warmthLossDay) * ExposureMultiplier;
        float fireWarmth = Campfire.WarmthAt(transform.position);   // being close to a lit fire warms you

        if (!IsResting) Energy = Mathf.Max(0f, Energy - energyLossPerMinute * minutes);
        Water = Mathf.Max(0f, Water - waterLoss * minutes);
        Food = Mathf.Max(0f, Food - foodLoss * minutes);
        Warmth = Mathf.Clamp(Warmth + (fireWarmth - warmthLoss) * minutes, 0f, max);
        Evaluate();
    }

    /// <summary>Add (or subtract, if negative) to the stats.</summary>
    public void Apply(float energy = 0f, float water = 0f, float food = 0f, float warmth = 0f)
    {
        if (IsDead) return;
        Energy = Mathf.Clamp(Energy + energy, 0f, max);
        Water = Mathf.Clamp(Water + water, 0f, max);
        Food = Mathf.Clamp(Food + food, 0f, max);
        Warmth = Mathf.Clamp(Warmth + warmth, 0f, max);
        Evaluate();
    }

    void Evaluate()
    {
        Changed?.Invoke();

        for (int i = 0; i < levels.Length; i++)
        {
            float v = Get((StatType)i);
            int lvl = v <= criticalLevel ? 2 : (v <= warnLevel ? 1 : 0);
            if (lvl != levels[i])
            {
                levels[i] = lvl;
                LevelChanged?.Invoke((StatType)i, lvl);
            }
        }

        if (Energy <= 0f) Die(StatType.Energy, "You collapsed from exhaustion.");
        else if (Water <= 0f) Die(StatType.Water, "You died of thirst.");
        else if (Warmth <= 0f) Die(StatType.Warmth, "You froze in the night.");
        else if (Food <= 0f) Die(StatType.Food, "You starved.");
    }

    void Die(StatType stat, string cause)
    {
        if (IsDead) return;
        IsDead = true;
        DeathStat = stat;
        DeathCause = cause;
        Died?.Invoke();
    }
}
