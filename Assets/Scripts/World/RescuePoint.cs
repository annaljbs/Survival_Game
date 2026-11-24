using UnityEngine;

/// <summary>Marks the rescue spot. The HUD points to it only while the player carries the map.</summary>
public class RescuePoint : MonoBehaviour
{
    public static RescuePoint Instance { get; private set; }

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>"north-east, about 120 m" from a position (1 unit = 1 m).</summary>
    public string DirectionFrom(Vector2 from)
    {
        Vector2 d = (Vector2)transform.position - from;
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;       // 0 = east, 90 = north
        string[] names = { "east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east" };
        int index = Mathf.RoundToInt(((angle % 360f) + 360f) % 360f / 45f) % 8;
        return $"{names[index]}, about {Mathf.RoundToInt(d.magnitude / 5f) * 5} m";
    }
}
