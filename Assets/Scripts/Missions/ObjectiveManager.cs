using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One mission. Done is latched: once reached it stays done.</summary>
public class Objective
{
    public string id;
    public string title;
    public string hint;
    public Func<bool> condition;
    public bool Done;
}

/// <summary>
/// The mission list. Missions complete from items the player holds, from "milestones"
/// that actions report (see ActionData.milestone), or from the clock.
/// </summary>
public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }

    public readonly List<Objective> Objectives = new List<Objective>();

    public event Action<Objective> Completed;
    public event Action Changed;

    readonly HashSet<string> milestones = new HashSet<string>();
    Inventory inventory;

    /// <summary>The first mission that is not done yet.</summary>
    public Objective Current
    {
        get
        {
            foreach (var o in Objectives) if (!o.Done) return o;
            return null;
        }
    }

    public int DoneCount
    {
        get
        {
            int n = 0;
            foreach (var o in Objectives) if (o.Done) n++;
            return n;
        }
    }

    void Awake()
    {
        Instance = this;
        Build();
    }

    void Start() => inventory = FindFirstObjectByType<Inventory>();

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool Has(string milestone) => milestones.Contains(milestone);

    public void Milestone(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (milestones.Add(key)) Evaluate();
    }

    void Update() => Evaluate();

    void Evaluate()
    {
        bool changed = false;
        foreach (var o in Objectives)
        {
            if (o.Done) continue;
            if (!o.condition()) continue;
            o.Done = true;
            changed = true;
            Completed?.Invoke(o);
        }
        if (changed) Changed?.Invoke();
    }

    bool Holds(ItemType item, int count = 1) => inventory != null && inventory.Count(item) >= count;

    void Build()
    {
        Add("phone", "No signal",
            "Check your phone. Spoiler: no bars.",
            () => Has("phone_checked"));
        Add("salvage", "Salvage supplies",
            "Search the wreck. You need at least a bottle and a lighter; a jacket, tarp and map help too. More clothes are scattered around: look in the trees.",
            () => Holds(ItemType.Bottle) && Holds(ItemType.Lighter));
        Add("water", "Find water",
            "Fill your bottle at the stream. Do not drink it yet.",
            () => Has("water_filled"));
        Add("materials", "Gather fire materials",
            "Dry grass: meadow by the stream (east). Dead twigs: forest edge (north). 2 dry logs: log field (far north-west). Before it rains!",
            () => Holds(ItemType.Tinder) && Holds(ItemType.Kindling) && Holds(ItemType.Logs, 2));
        Add("fire", "Build a fire",
            "Use a fire pit at a camp spot. Pick the spot where you want to spend the night.",
            () => Has("fire_built"));
        Add("boil", "Make the water safe",
            "Boil your dirty water on the lit fire.",
            () => Has("water_boiled"));
        Add("shelter", "Build a dry shelter",
            "At a camp: frame (1 green bush cut = 3 poles), then a roof (tarp, or 6 spruce boughs). Rain must not get in. Avoid low ground!",
            () => Has("shelter_frame") && Has("shelter_roof"));
        Add("sleep", "Get some sleep",
            "Sleep inside your shelter (the spot to the right of the fire pit). A bed of boughs keeps the cold ground out.",
            () => Has("slept"));
        Add("night", "Survive the night",
            "Keep warm, keep the fire fed and make it to the morning.",
            () => Has("slept") && GameClock.Instance != null && GameClock.Instance.TotalMinutes >= 1440f + 6f * 60f);
        Add("rescue", "Get rescued",
            "Find the rescue spot (the crew map shows the direction), make a signal and wait there in daylight (08:00 to 16:00).",
            () => Has("rescued"));
    }

    void Add(string id, string title, string hint, Func<bool> condition)
    {
        Objectives.Add(new Objective { id = id, title = title, hint = hint, condition = condition });
    }
}
