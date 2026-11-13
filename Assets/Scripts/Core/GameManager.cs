using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum GameState { Playing, Won, Lost }

/// <summary>Win = survive N days. Lose = die. R restarts.</summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int daysToSurvive = 7;
    public SurvivalStats stats;

    public GameState State { get; private set; } = GameState.Playing;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (State == GameState.Playing)
        {
            if (stats != null && stats.IsDead) State = GameState.Lost;
            else if (DayNightCycle.Instance != null && DayNightCycle.Instance.DayCount > daysToSurvive)
            {
                State = GameState.Won;
                Time.timeScale = 0f;
            }
        }
        else if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
