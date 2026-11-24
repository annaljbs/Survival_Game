using UnityEngine;

/// <summary>Hides the placeholder picture of an object when the game starts (its collider keeps working).</summary>
[DefaultExecutionOrder(-300)]
public class HideAtPlay : MonoBehaviour
{
    void Awake()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;
    }
}
