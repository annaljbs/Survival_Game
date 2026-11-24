using UnityEngine;

/// <summary>Reaching this zone wins the game (placeholder for the rescue point).</summary>
[RequireComponent(typeof(Collider2D))]
public class ExitZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() != null && GameManager.Instance != null)
            GameManager.Instance.Win();
    }
}
