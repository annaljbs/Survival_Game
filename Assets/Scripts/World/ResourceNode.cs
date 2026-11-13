using System.Collections;
using UnityEngine;

/// <summary>A harvestable node (tree, rock, berry bush, water). Respawns after a delay.</summary>
public class ResourceNode : MonoBehaviour
{
    public ItemType item = ItemType.Wood;
    public int yield = 1;
    public float respawnSeconds = 30f;

    public bool IsAvailable { get; private set; } = true;

    /// <summary>Raised whenever anything is harvested (missions listen to this).</summary>
    public static event System.Action<ItemType, int> Harvested;

    SpriteRenderer sr;
    Color fullColor;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) fullColor = sr.color;
    }

    public void Harvest(Inventory inventory)
    {
        if (!IsAvailable) return;
        inventory.Add(item, yield);
        Harvested?.Invoke(item, yield);
        StartCoroutine(Deplete());
    }

    IEnumerator Deplete()
    {
        IsAvailable = false;
        if (sr != null) sr.color = fullColor * 0.35f;
        yield return new WaitForSeconds(respawnSeconds);
        IsAvailable = true;
        if (sr != null) sr.color = fullColor;
    }
}
