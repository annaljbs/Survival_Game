using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum GameState { Playing, Won, Lost }

/// <summary>Playing / Won / Lost. R restarts the scene.</summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; } = GameState.Playing;
    public string EndTitle { get; private set; } = "";
    public string EndDetail { get; private set; } = "";
    /// <summary>A sarcastic line about how it ended, and one real survival fact.</summary>
    public string EndQuip { get; private set; } = "";
    public string EndTip { get; private set; } = "";

    SurvivalStats stats;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    void Start()
    {
        stats = FindFirstObjectByType<SurvivalStats>();
        if (stats != null) stats.Died += OnDied;
    }

    void OnDestroy()
    {
        if (stats != null) stats.Died -= OnDied;
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (State != GameState.Playing && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void Win()
    {
        if (State != GameState.Playing) return;
        State = GameState.Won;
        if (ObjectiveManager.Instance != null) ObjectiveManager.Instance.Milestone("rescued");
        var c = GameClock.Instance;
        EndTitle = "Rescued!";
        EndDetail = c != null ? $"You were found on day {c.DayCount} at {c.TimeText}." : "";
        var missions = ObjectiveManager.Instance;
        if (missions != null) EndDetail += $"   Missions: {missions.DoneCount}/{missions.Objectives.Count}";
        EndQuip = "The pilot spotted you. Possibly because of the thick smoke, possibly because of your enthusiastic arm waving.";
        EndTip = "Rescuers find people who stay visible: thick smoke (green boughs on a fire), bright colours in the open, three blasts of a whistle. Stay where you can be found.";
    }

    void OnDied()
    {
        if (State != GameState.Playing) return;
        State = GameState.Lost;
        var c = GameClock.Instance;
        EndTitle = stats.DeathCause;
        EndDetail = c != null ? $"Day {c.DayCount}, {c.TimeText}." : "";
        ChooseQuipAndTip();
    }

    /// <summary>Picks the sarcastic line and the real tip from the cause and what the player did (or did not do).</summary>
    void ChooseQuipAndTip()
    {
        var m = ObjectiveManager.Instance;
        bool ate = m != null && m.Has("ate_berries");
        bool drankStream = m != null && m.Has("drank_stream");
        bool fire = m != null && m.Has("fire_built");
        bool roof = m != null && m.Has("shelter_roof");
        bool soaked = m != null && m.Has("rain_exposed");

        if (ate)
        {
            EndQuip = "You trusted your gut. Your gut had terrible judgment. \"Bright red and shiny\" is not an approval badge.";
            EndTip = "Never eat wild berries or plants you cannot identify with 100% certainty. Some look harmless and still shut down your organs.";
            return;
        }
        switch (stats.DeathStat)
        {
            case StatType.Energy:
                EndQuip = "You gave everything you had. It turned out to be less than the task required.";
                EndTip = "Energy is a budget: every step and every task costs it. Rest early, before the tank is empty. Exhaustion makes every other mistake worse.";
                break;
            case StatType.Water:
                if (drankStream)
                {
                    EndQuip = "Stream water: free, wild, organic and full of surprise guests.";
                    EndTip = "Untreated water can carry bacteria and parasites. Boil it for at least one minute, and drink it only when it has cooled down.";
                }
                else
                {
                    EndQuip = "Surrounded by streams, killed by thirst. The universe loves irony.";
                    EndTip = "Water comes first: a person lasts only about three days without it. Boil stream water before you drink it.";
                }
                break;
            case StatType.Warmth:
                if (!fire)
                {
                    EndQuip = "No fire, no warmth, no plan. At least the night was consistent.";
                    EndTip = "A fire gives warmth, light and boiled water. Collect dry tinder, kindling and logs before dark, because wet material will not burn.";
                }
                else if (soaked && !roof)
                {
                    EndQuip = "Rain, no roof, wet clothes. Nature's cheapest freezer.";
                    EndTip = "Wet clothes lose most of their insulation. Staying dry is worth more than a thicker coat: a simple roof helps a lot.";
                }
                else
                {
                    EndQuip = "You froze. A bad way to learn that nights are cold.";
                    EndTip = "At night the body loses heat fast. Layers, a windbreak, a bed off the cold ground and a fire work best together. Sleeping directly on the ground drains heat.";
                }
                break;
            default:
                EndQuip = "You starved. Not an ideal weight-loss programme.";
                EndTip = "Over a few days food matters less than water and warmth, but hunger drains your energy. Eat safe food, never unknown plants.";
                break;
        }
    }
}
