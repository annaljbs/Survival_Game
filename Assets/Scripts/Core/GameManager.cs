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
        EndDetail = c != null ? $"You reached the rescue point on day {c.DayCount} at {c.TimeText}." : "";
    }

    void OnDied()
    {
        if (State != GameState.Playing) return;
        State = GameState.Lost;
        var c = GameClock.Instance;
        EndTitle = stats.DeathCause;
        EndDetail = c != null ? $"Day {c.DayCount}, {c.TimeText}." : "";
    }
}
