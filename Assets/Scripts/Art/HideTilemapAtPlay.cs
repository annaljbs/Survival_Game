using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Hides the picture of a tilemap when the game starts (its collider keeps working). Used for the invisible forest wall.</summary>
[DefaultExecutionOrder(-300)]
public class HideTilemapAtPlay : MonoBehaviour
{
    void Awake()
    {
        var r = GetComponent<TilemapRenderer>();
        if (r != null) r.enabled = false;
    }
}
