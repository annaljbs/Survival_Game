using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Add new items only at the END: the numbers are stored in the scene.
/// Wet/green variants exist so that "not everything burns" can be taught.
/// </summary>
public enum ItemType
{
    None, Bottle, DirtyWater, CleanWater, Jacket, Lighter, Map, Snack,
    Tarp, Tinder, Kindling, Logs, WetTinder, WetKindling, WetLogs, GreenWood,
    Boughs,
    // Found along the paths. SpaceBlanket and Whistle are useful, the rest is junk (for the jokes).
    SpaceBlanket, Whistle, OddShoe, SelfieStick, DeadPowerBank, BrokenDrone, BeachBall, Guidebook,
    // Spare clothes from the wreck, scattered around: each one lowers warmth loss.
    WoolSweater, WoolHat, DrySocks, Gloves,
}

public static class ItemInfo
{
    /// <summary>Useless things you can pick up for fun (the HUD shows them together as "Junk").</summary>
    public static bool IsJunk(this ItemType type)
    {
        return type == ItemType.OddShoe || type == ItemType.SelfieStick || type == ItemType.DeadPowerBank ||
               type == ItemType.BrokenDrone || type == ItemType.BeachBall || type == ItemType.Guidebook;
    }

    /// <summary>"DirtyWater" -> "Dirty Water".</summary>
    public static string Pretty(this ItemType type)
    {
        string s = type.ToString();
        var sb = new StringBuilder();
        for (int i = 0; i < s.Length; i++)
        {
            if (i > 0 && char.IsUpper(s[i])) sb.Append(' ');
            sb.Append(s[i]);
        }
        return sb.ToString();
    }
}

/// <summary>Simple count-based inventory.</summary>
public class Inventory : MonoBehaviour
{
    readonly Dictionary<ItemType, int> items = new Dictionary<ItemType, int>();

    public event Action Changed;

    public int Count(ItemType type) => items.TryGetValue(type, out int n) ? n : 0;
    public bool Has(ItemType type) => Count(type) > 0;
    public IEnumerable<KeyValuePair<ItemType, int>> All => items;

    public void Add(ItemType type, int amount = 1)
    {
        if (type == ItemType.None || amount <= 0) return;
        items[type] = Count(type) + amount;
        Changed?.Invoke();
    }

    public bool Remove(ItemType type, int amount = 1)
    {
        if (Count(type) < amount) return false;
        items[type] = Count(type) - amount;
        if (items[type] == 0) items.Remove(type);
        Changed?.Invoke();
        return true;
    }
}
