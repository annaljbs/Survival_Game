using UnityEngine;

/// <summary>Zero-setup immediate-mode HUD. Swap for uGUI/UI Toolkit later.</summary>
public class SurvivalHUD : MonoBehaviour
{
    public SurvivalStats stats;
    public Inventory inventory;
    public MissionManager missions;

    Texture2D px;
    GUIStyle label, big, small;

    void Awake()
    {
        px = new Texture2D(1, 1);
        px.SetPixel(0, 0, Color.white);
        px.Apply();
    }

    void InitStyles()
    {
        if (label != null) return;
        label = new GUIStyle(GUI.skin.label) { fontSize = 16 };
        small = new GUIStyle(GUI.skin.label) { fontSize = 13 };
        big = new GUIStyle(GUI.skin.label) { fontSize = 34, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
    }

    void Fill(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, px);
        GUI.color = Color.white;
    }

    void Bar(float y, string label_, float value, float max, Color c)
    {
        Fill(new Rect(10, y, 230, 22), new Color(0, 0, 0, 0.6f));
        Fill(new Rect(12, y + 2, 226f * Mathf.Clamp01(value / max), 18), c);
        GUI.Label(new Rect(16, y, 230, 22), $"{label_} {Mathf.CeilToInt(value)}", small);
    }

    void OnGUI()
    {
        if (stats == null) return;
        InitStyles();

        Bar(10, "Health", stats.Health, stats.maxHealth, new Color(0.85f, 0.2f, 0.2f));
        Bar(36, "Hunger", stats.Hunger, stats.maxHunger, new Color(0.9f, 0.6f, 0.15f));
        Bar(62, "Thirst", stats.Thirst, stats.maxThirst, new Color(0.25f, 0.55f, 0.95f));
        Bar(88, stats.IsExhausted ? "Energy (EXHAUSTED)" : "Energy", stats.Energy, stats.maxEnergy, new Color(0.9f, 0.85f, 0.2f));

        if (inventory != null)
        {
            GUI.Label(new Rect(10, 118, 600, 24),
                $"Wood {inventory.Count(ItemType.Wood)}   Stone {inventory.Count(ItemType.Stone)}   " +
                $"Berries {inventory.Count(ItemType.Berry)}   Water {inventory.Count(ItemType.Water)}", label);
        }

        var dn = DayNightCycle.Instance;
        var gm = GameManager.Instance;
        if (dn != null)
        {
            int total = gm != null ? gm.daysToSurvive : 0;
            int shown = Mathf.Min(dn.DayCount, total > 0 ? total : dn.DayCount);
            GUI.Label(new Rect(10, 144, 500, 24),
                $"Day {shown} / {total}  -  {(dn.IsNight ? "Night" : "Day")}", label);
        }

        DrawMissions();

        GUI.Label(new Rect(10, Screen.height - 26, 900, 22),
            "WASD move | Shift sprint (costs energy + health) | E gather | 1 eat berry | 2 drink water", small);

        DrawEndScreen(gm);
    }

    void DrawMissions()
    {
        if (missions == null) return;

        float w = 360f;
        float x = Screen.width - w - 10f;
        float y = 10f;

        GUI.Label(new Rect(x, y, w, 24), $"MISSIONS  (done {missions.Completed}, failed {missions.Failed})", label);
        y += 28f;

        if (missions.Active.Count == 0)
            GUI.Label(new Rect(x, y, w, 22), "No active missions...", small);

        var pos = stats.transform.position;
        foreach (var m in missions.Active)
        {
            Fill(new Rect(x, y, w, 62), new Color(0, 0, 0, 0.55f));
            GUI.Label(new Rect(x + 6, y + 2, w - 12, 20), m.Describe(pos), small);
            GUI.Label(new Rect(x + 6, y + 20, w - 12, 20), "Reward: " + m.RewardText(), small);

            Color tc = m.timeLeft < 15f ? new Color(1f, 0.4f, 0.4f) : Color.white;
            GUI.color = tc;
            GUI.Label(new Rect(x + 6, y + 38, w - 12, 20), $"Time left: {Mathf.CeilToInt(m.timeLeft)}s   Fail: -{m.penaltyEnergy:F0} energy", small);
            GUI.color = Color.white;
            y += 68f;
        }

        if (Time.unscaledTime - missions.MessageTime < 5f)
        {
            var center = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(0, 12, Screen.width, 28), missions.LastMessage, center);
        }
    }

    void DrawEndScreen(GameManager gm)
    {
        if (gm == null || gm.State == GameState.Playing) return;

        Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0, 0.6f));
        string title = gm.State == GameState.Won
            ? $"You survived {gm.daysToSurvive} days!"
            : stats.DeathCause;
        GUI.Label(new Rect(0, Screen.height / 2f - 60, Screen.width, 60), title, big);

        var center = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
        string detail = missions != null ? $"Missions completed: {missions.Completed}   failed: {missions.Failed}" : "";
        GUI.Label(new Rect(0, Screen.height / 2f, Screen.width, 28), detail, center);
        GUI.Label(new Rect(0, Screen.height / 2f + 30, Screen.width, 28), "Press R to play again", center);
    }
}
