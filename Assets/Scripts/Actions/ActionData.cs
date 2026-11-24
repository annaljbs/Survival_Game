using System;
using System.Collections.Generic;
using UnityEngine;

public enum FireRule { None, NeedsLit, NeedsUnlit }
public enum FireEffect { None, Light, AddFuel }
public enum ShelterStage { None, Frame, Roof, Bedding }

[Serializable]
public class ItemRequirement
{
    public ItemType item;
    public int count = 1;
    [Tooltip("Used up when the action completes. Off = only needed (like a lighter).")]
    public bool consume;
    [Tooltip("Shown when this is missing. Leave empty for 'Needs: <item>'.")]
    public string missingHint = "";
}

[Serializable]
public class ItemGrant
{
    public ItemType item;
    public int count = 1;
}

/// <summary>
/// Everything one action costs, needs and gives. Edited in the Inspector on an <see cref="Interactable"/>.
/// Deltas are added to the stats (negative = cost).
/// </summary>
[Serializable]
public class ActionData
{
    public string name = "Action";
    [TextArea] public string description = "";

    [Header("Cost")]
    public float minutes = 10f;
    public float energyDelta = -5f;
    public float waterDelta;
    public float foodDelta;
    public float warmthDelta;
    [Tooltip("Why does it cost this much? Shown in the preview.")]
    public string reason = "";

    [Header("Items")]
    public ItemRequirement[] requirements = new ItemRequirement[0];
    public ItemGrant[] grants = new ItemGrant[0];
    [Tooltip("Not possible while the player holds any of these (e.g. a bottle that is already full).")]
    public ItemType[] blockedBy = new ItemType[0];
    public string blockedHint = "";

    [Header("World conditions")]
    public WorldFlag requiresFlag = WorldFlag.None;
    public string requiresFlagHint = "";
    public WorldFlag forbiddenFlag = WorldFlag.None;
    public string forbiddenFlagHint = "";
    public WorldFlag setsFlag = WorldFlag.None;
    [Tooltip("Only possible between these hours of the day (e.g. 8 and 18). Leave both at 0 for 'any time'.")]
    public float onlyFromHour;
    public float onlyUntilHour;
    public string timeHint = "";

    [Header("Fire (needs a Campfire on the Interactable)")]
    public FireRule fireRule = FireRule.None;
    public FireEffect fireEffect = FireEffect.None;
    [Tooltip("Burn time added when fireEffect = AddFuel.")]
    public float fuelMinutes = 90f;

    [Header("Effects")]
    [Tooltip("Multiplies warmth loss from now on (0.6 = jacket). 1 = no change.")]
    public float exposureFactor = 1f;
    public bool oneShot = true;
    [Tooltip("Resting (sleep): no passive energy loss while it runs.")]
    public bool isRest;
    [Tooltip("Builds this part of the shelter on the ShelterZone linked on the Interactable.")]
    public ShelterStage shelterStage = ShelterStage.None;
    [Tooltip("With shelterStage = Roof: what the roof is made of (changes how it looks).")]
    public bool roofIsTarp;
    [Tooltip("Completing this action wins the game (rescue).")]
    public bool winsGame;
    [Tooltip("Name of a mission milestone that is reached when this action completes.")]
    public string milestone = "";

    [Header("Text")]
    [TextArea] public string resultText = "";
    [Tooltip("What the character thinks afterwards (dark humour). Shown as a thought bubble.")]
    [TextArea] public string thought = "";

    const string Good = "#8fe388";
    const string Bad = "#ff8f8f";

    /// <summary>One-line, colour-coded cost summary for the preview card (rich text).</summary>
    public string DescribeCost()
    {
        var s = $"~{minutes:0} min";
        s += Part("Energy", energyDelta);
        s += Part("Water", waterDelta);
        s += Part("Food", foodDelta);
        s += Part("Warmth", warmthDelta);
        return s;
    }

    /// <summary>"Needs: Lighter    Uses up: Tinder, Logs x2".</summary>
    public string DescribeNeeds()
    {
        if (requirements == null || requirements.Length == 0) return "";
        var keep = new List<string>();
        var use = new List<string>();
        foreach (var r in requirements)
        {
            string label = r.count > 1 ? $"{r.item.Pretty()} x{r.count}" : r.item.Pretty();
            (r.consume ? use : keep).Add(label);
        }
        string text = "";
        if (keep.Count > 0) text += "Needs: " + string.Join(", ", keep);
        if (use.Count > 0)
        {
            if (text.Length > 0) text += "     ";
            text += "Uses up: " + string.Join(", ", use);
        }
        return text;
    }

    static string Part(string label, float delta)
    {
        if (Mathf.Approximately(delta, 0f)) return "";
        string col = delta > 0f ? Good : Bad;
        string sign = delta > 0f ? "+" : "-";
        return $"    <color={col}>{label} {sign}{Mathf.Abs(delta):0}</color>";
    }
}
