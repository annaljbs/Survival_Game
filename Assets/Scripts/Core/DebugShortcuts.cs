using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Test helpers, active only in the Unity Editor and development builds (Debug.isDebugBuild).
/// It creates itself when the game starts, so no scene changes are needed.
/// Keys (number row):  1 kit   2 +1 hour   3 rain   4 morning   5 refill bars   6 rescue spot   7 forest edge   0 hide this panel
/// </summary>
public class DebugShortcuts : MonoBehaviour
{
    bool showPanel = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (!Debug.isDebugBuild || FindFirstObjectByType<DebugShortcuts>() != null) return;
        var go = new GameObject("DebugShortcuts");
        go.AddComponent<DebugShortcuts>();
        DontDestroyOnLoad(go);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame) GiveKit();
        if (kb.digit2Key.wasPressedThisFrame) SkipTime(60f, "+1 hour");
        if (kb.digit3Key.wasPressedThisFrame) JumpToRain();
        if (kb.digit4Key.wasPressedThisFrame) JumpToMorning();
        if (kb.digit5Key.wasPressedThisFrame) Refill();
        if (kb.digit6Key.wasPressedThisFrame) TeleportToRescue();
        if (kb.digit7Key.wasPressedThisFrame) TeleportToObject("Fire pit: Camp B", new Vector2(0, -3.5f), "Forest edge camp");
        if (kb.digit0Key.wasPressedThisFrame) showPanel = !showPanel;
    }

    // ------------------------------------------------------------------ actions

    void GiveKit()
    {
        var inv = FindFirstObjectByType<Inventory>();
        var stats = FindFirstObjectByType<SurvivalStats>();
        if (inv == null) return;
        inv.Add(ItemType.Bottle);
        inv.Add(ItemType.Lighter);
        inv.Add(ItemType.Jacket);
        inv.Add(ItemType.Map);
        inv.Add(ItemType.Tarp);
        inv.Add(ItemType.Tinder, 2);
        inv.Add(ItemType.Kindling, 2);
        inv.Add(ItemType.Logs, 4);
        inv.Add(ItemType.GreenWood, 6);
        inv.Add(ItemType.Boughs, 12);
        inv.Add(ItemType.DirtyWater);
        if (stats != null) stats.gearFactor = Mathf.Min(stats.gearFactor, 0.6f);
        Say("DEBUG: survival kit added");
    }

    void SkipTime(float minutes, string label)
    {
        var clock = GameClock.Instance;
        if (clock == null || !IsPlaying()) return;
        clock.Advance(minutes);
        Say($"DEBUG: {label} ({clock.TimeText})");
    }

    void JumpToRain()
    {
        var clock = GameClock.Instance;
        var weather = Weather.Instance;
        if (clock == null || weather == null || !IsPlaying()) return;
        float minutes = weather.RainStartMinute - weather.warningMinutes - clock.TotalMinutes;
        if (minutes <= 0f)
        {
            Say("DEBUG: rain is already here or over");
            return;
        }
        clock.Advance(minutes);
        Say($"DEBUG: rain warning now ({clock.TimeText})");
    }

    void JumpToMorning()
    {
        var clock = GameClock.Instance;
        if (clock == null || !IsPlaying()) return;
        float now = clock.TotalMinutes;
        float day = Mathf.Floor(now / 1440f);
        float target = day * 1440f + 7f * 60f;
        if (target <= now) target += 1440f;
        clock.Advance(target - now);
        Say($"DEBUG: morning ({clock.TimeText}). The bars kept draining.");
    }

    void Refill()
    {
        var stats = FindFirstObjectByType<SurvivalStats>();
        if (stats == null) return;
        stats.Apply(stats.max, stats.max, stats.max, stats.max);
        Say("DEBUG: bars refilled");
    }

    void TeleportToRescue()
    {
        if (RescuePoint.Instance == null) return;
        Teleport((Vector2)RescuePoint.Instance.transform.position + new Vector2(0, -2.5f), "Rescue spot");
    }

    void TeleportToObject(string objectName, Vector2 offset, string label)
    {
        var go = GameObject.Find(objectName);
        if (go == null) return;
        Teleport((Vector2)go.transform.position + offset, label);
    }

    void Teleport(Vector2 position, string label)
    {
        var player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;
        player.transform.position = new Vector3(position.x, position.y, player.transform.position.z);
        var body = player.GetComponent<Rigidbody2D>();
        if (body != null) body.linearVelocity = Vector2.zero;
        var cam = Camera.main;
        if (cam != null) cam.transform.position = new Vector3(position.x, position.y, cam.transform.position.z);
        Say("DEBUG: moved to " + label);
    }

    // ------------------------------------------------------------------ helpers

    static bool IsPlaying() => GameManager.Instance == null || GameManager.Instance.State == GameState.Playing;

    static void Say(string text)
    {
        var actions = FindFirstObjectByType<ActionSystem>();
        if (actions != null) actions.Say(text);
        Debug.Log(text);
    }

    void OnGUI()
    {
        if (!showPanel) return;
        GUI.Label(new Rect(10, Screen.height - 54, 1400, 24),
                  "DEBUG   1 kit   2 +1 hour   3 rain warning   4 morning   5 refill bars   6 rescue spot   7 forest edge camp   0 hide");
    }
}
