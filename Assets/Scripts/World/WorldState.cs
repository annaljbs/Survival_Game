using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Add new flags only at the END: the numbers are stored in the scene.</summary>
public enum WorldFlag { None, Raining, FireLit, Injured, SignalReady }

/// <summary>Global yes/no facts about the world (is it raining, is a fire burning, ...).</summary>
public class WorldState : MonoBehaviour
{
    public static WorldState Instance { get; private set; }

    readonly HashSet<WorldFlag> flags = new HashSet<WorldFlag>();

    public event Action<WorldFlag, bool> FlagChanged;

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool Has(WorldFlag flag) => flag != WorldFlag.None && flags.Contains(flag);

    public void Set(WorldFlag flag, bool on = true)
    {
        if (flag == WorldFlag.None) return;
        bool changed = on ? flags.Add(flag) : flags.Remove(flag);
        if (changed) FlagChanged?.Invoke(flag, on);
    }
}
