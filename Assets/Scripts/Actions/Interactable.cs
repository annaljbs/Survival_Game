using UnityEngine;

/// <summary>
/// A world object the player can use. Holds one <see cref="ActionData"/>, plus an optional
/// "wet" version that replaces it while it rains (e.g. dry grass becomes soaked grass).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Interactable : MonoBehaviour
{
    public ActionData action = new ActionData();

    [Header("While it rains")]
    public bool hasWetAction;
    public ActionData wetAction = new ActionData();

    [Header("Optional links")]
    [Tooltip("The fire pit this object belongs to (needed for FireRule / FireEffect actions).")]
    public Campfire fire;
    [Tooltip("If set, the player is moved here after the action (e.g. climbing).")]
    public Transform moveTo;
    [Tooltip("The camp area this object improves (e.g. building a lean-to).")]
    public ShelterZone zone;

    public bool Used { get; private set; }

    /// <summary>The action that applies right now (wet version while raining, if there is one).</summary>
    public ActionData ActiveAction =>
        hasWetAction && WorldState.Instance != null && WorldState.Instance.Has(WorldFlag.Raining) ? wetAction : action;

    public bool Available => !(ActiveAction.oneShot && Used);

    SpriteRenderer sr;
    Color baseColor = Color.white;
    bool highlighted;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
    }

    public void MarkUsed()
    {
        Used = true;
        if (ActiveAction.oneShot && sr != null) sr.color = baseColor * 0.4f;
    }

    public void SetHighlight(bool on)
    {
        if (highlighted == on || sr == null || !Available) return;
        highlighted = on;
        sr.color = on ? Color.Lerp(baseColor, Color.white, 0.45f) : baseColor;
    }

    /// <summary>Null if the action is possible, otherwise a short hint that says why not.</summary>
    public string CheckRequirements(Inventory inventory)
    {
        var a = ActiveAction;
        var world = WorldState.Instance;

        if (a.requiresFlag != WorldFlag.None && (world == null || !world.Has(a.requiresFlag)))
            return string.IsNullOrEmpty(a.requiresFlagHint) ? $"Not possible right now ({a.requiresFlag})" : a.requiresFlagHint;
        if (a.forbiddenFlag != WorldFlag.None && world != null && world.Has(a.forbiddenFlag))
            return string.IsNullOrEmpty(a.forbiddenFlagHint) ? $"Not possible right now ({a.forbiddenFlag})" : a.forbiddenFlagHint;

        if (a.onlyUntilHour > a.onlyFromHour && GameClock.Instance != null)
        {
            float h = GameClock.Instance.HourOfDay;
            if (h < a.onlyFromHour || h >= a.onlyUntilHour)
                return string.IsNullOrEmpty(a.timeHint) ? $"Only possible between {a.onlyFromHour:0}:00 and {a.onlyUntilHour:0}:00" : a.timeHint;
        }

        if (a.shelterStage != ShelterStage.None)
        {
            if (zone == null) return "There is no camp site here";
            string why = zone.CanBuild(a.shelterStage);
            if (why != null) return why;
        }

        if (a.fireRule != FireRule.None)
        {
            if (fire == null) return "There is no fire pit here";
            if (a.fireRule == FireRule.NeedsLit && !fire.IsLit) return "The fire is not lit";
            if (a.fireRule == FireRule.NeedsUnlit && fire.IsLit) return "The fire is already burning";
        }

        if (a.blockedBy != null)
        {
            foreach (var item in a.blockedBy)
                if (inventory.Has(item)) return string.IsNullOrEmpty(a.blockedHint) ? $"Not possible while you carry: {item.Pretty()}" : a.blockedHint;
        }

        if (a.requirements != null)
        {
            foreach (var r in a.requirements)
            {
                if (inventory.Count(r.item) >= r.count) continue;
                if (GameHints.Enabled && !string.IsNullOrEmpty(r.missingHint)) return r.missingHint;
                return r.count > 1 ? $"Needs: {r.item.Pretty()} x{r.count}" : $"Needs: {r.item.Pretty()}";
            }
        }
        return null;
    }
}
