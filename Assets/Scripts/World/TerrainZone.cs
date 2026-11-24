using UnityEngine;

/// <summary>
/// Ground that changes the cost of walking: thick forest is slow and tiring, cold water chills you.
/// Needs a trigger collider. The player (PlayerController) reads the zone it is standing in.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TerrainZone : MonoBehaviour
{
    public string label = "Rough ground";
    [Tooltip("Multiplies the energy cost of walking. 1.5 = 50% more tiring.")]
    public float energyMultiplier = 1f;
    [Tooltip("Multiplies walking speed. 0.75 = slower.")]
    public float speedMultiplier = 1f;
    [Tooltip("Warmth lost per unit walked here (cold water).")]
    public float warmthPerUnit;
    [Tooltip("Warmth lost once when stepping into the zone.")]
    public float entryWarmthLoss;
    [Tooltip("Entering soaks your clothes: warmth loss is higher until they dry.")]
    public bool makesWet;
    public float wetMinutes = 240f;
    public string enterMessage = "";

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player != null) player.EnterTerrain(this);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player != null) player.ExitTerrain(this);
    }
}
