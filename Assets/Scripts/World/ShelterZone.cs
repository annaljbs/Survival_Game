using UnityEngine;

/// <summary>
/// An area (camp spot) that changes how fast the player loses warmth while standing in it.
/// Below 1 = sheltered, above 1 = exposed. Needs a trigger collider.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ShelterZone : MonoBehaviour
{
    public string label = "Camp";
    [Tooltip("Multiplies warmth loss while inside. 0.5 = half as cold, 1.5 = 50% colder.")]
    public float exposureFactor = 1f;

    void OnTriggerEnter2D(Collider2D other)
    {
        var stats = other.GetComponent<SurvivalStats>();
        if (stats == null) return;
        stats.shelterFactor = exposureFactor;
        stats.ZoneLabel = $"{label} (warmth loss x{exposureFactor:0.0#})";
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var stats = other.GetComponent<SurvivalStats>();
        if (stats == null) return;
        stats.shelterFactor = 1f;
        stats.ZoneLabel = "";
    }
}
