using System;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType { Wood, Stone, Berry, Water }

/// <summary>Simple count-based inventory.</summary>
public class Inventory : MonoBehaviour
{
    readonly Dictionary<ItemType, int> items = new Dictionary<ItemType, int>();

    public event Action Changed;

    public int Count(ItemType type) => items.TryGetValue(type, out int n) ? n : 0;

    public void Add(ItemType type, int amount = 1)
    {
        items[type] = Count(type) + amount;
        Changed?.Invoke();
    }

    public bool Remove(ItemType type, int amount = 1)
    {
        if (Count(type) < amount) return false;
        items[type] = Count(type) - amount;
        Changed?.Invoke();
        return true;
    }
}
