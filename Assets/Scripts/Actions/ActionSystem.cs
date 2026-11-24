using System;
using System.Collections;
using UnityEngine;

public enum ActionState { Idle, Preview, Running }

/// <summary>
/// Preview -> confirm -> time passes -> effects applied.
/// The player always sees what an action costs before committing to it.
/// </summary>
public class ActionSystem : MonoBehaviour
{
    public ActionState State { get; private set; } = ActionState.Idle;
    public bool IsBusy => State != ActionState.Idle;
    public Interactable Current { get; private set; }
    public string MissingHint { get; private set; }

    public event Action<Interactable, string> PreviewOpened;
    public event Action PreviewClosed;
    public event Action<Interactable> ActionStarted;
    public event Action<Interactable> ActionCompleted;
    public event Action<string> Message;

    SurvivalStats stats;
    GameClock clock;
    Inventory inventory;
    Transform playerTransform;

    void Start()
    {
        stats = FindFirstObjectByType<SurvivalStats>();
        inventory = FindFirstObjectByType<Inventory>();
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null) playerTransform = player.transform;
        clock = GameClock.Instance;
    }

    /// <summary>Show a message in the HUD (used by weather, fire, ...).</summary>
    public void Say(string message) => Message?.Invoke(message);

    public void OnInteractPressed(Interactable nearest)
    {
        switch (State)
        {
            case ActionState.Idle:
                if (nearest != null) Open(nearest);
                break;
            case ActionState.Preview:
                Confirm();
                break;
        }
    }

    public void Cancel()
    {
        if (State == ActionState.Preview) Close();
    }

    void Open(Interactable target)
    {
        Current = target;
        MissingHint = ComputeHint(target);
        State = ActionState.Preview;
        PreviewOpened?.Invoke(target, MissingHint);
    }

    string ComputeHint(Interactable target)
    {
        string hint = target.CheckRequirements(inventory);
        if (hint != null) return hint;
        // Fairness: never let an action silently kill the player through exhaustion.
        if (stats.Energy + target.ActiveAction.energyDelta <= 0f) return "Too exhausted - rest first";
        return null;
    }

    void Close()
    {
        if (stats != null) stats.IsResting = false;
        State = ActionState.Idle;
        Current = null;
        PreviewClosed?.Invoke();
    }

    void Confirm()
    {
        if (MissingHint != null)
        {
            Message?.Invoke(MissingHint);
            Close();
            return;
        }
        StartCoroutine(Run(Current));
    }

    IEnumerator Run(Interactable target)
    {
        State = ActionState.Running;
        ActionStarted?.Invoke(target);
        var a = target.ActiveAction;
        stats.IsResting = a.isRest;

        // Let the clock run forward visibly (0.6 - 2.2 seconds) instead of jumping.
        float duration = Mathf.Clamp(a.minutes / 30f, 0.6f, 2.2f);
        float elapsed = 0f;
        float advanced = 0f;
        while (elapsed < duration && !stats.IsDead)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            float step = Mathf.Min(a.minutes - advanced, a.minutes * dt / duration);
            clock.Advance(step);
            advanced += step;
            yield return null;
        }
        if (!stats.IsDead && advanced < a.minutes) clock.Advance(a.minutes - advanced);

        if (!stats.IsDead)
        {
            Complete(target, a);
        }

        Close();
    }

    void Complete(Interactable target, ActionData a)
    {
        if (a.requirements != null)
            foreach (var r in a.requirements)
                if (r.consume) inventory.Remove(r.item, r.count);
        if (a.grants != null)
            foreach (var g in a.grants) inventory.Add(g.item, g.count);

        if (target.fire != null)
        {
            if (a.fireEffect == FireEffect.Light) target.fire.Light();
            else if (a.fireEffect == FireEffect.AddFuel) target.fire.AddFuel(a.fuelMinutes);
        }
        if (a.setsFlag != WorldFlag.None && WorldState.Instance != null) WorldState.Instance.Set(a.setsFlag, true);
        if (!Mathf.Approximately(a.exposureFactor, 1f)) stats.gearFactor *= a.exposureFactor;

        stats.Apply(a.energyDelta, a.waterDelta, a.foodDelta, a.warmthDelta);
        if (a.oneShot) target.MarkUsed();

        if (!string.IsNullOrEmpty(a.milestone) && ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.Milestone(a.milestone);

        if (target.moveTo != null && playerTransform != null)
        {
            Vector3 dest = target.moveTo.position;
            playerTransform.position = new Vector3(dest.x, dest.y, playerTransform.position.z);
            var cam = Camera.main;
            if (cam != null) cam.transform.position = new Vector3(dest.x, dest.y, cam.transform.position.z);
        }

        if (!string.IsNullOrEmpty(a.resultText)) Message?.Invoke(a.resultText);
        ActionCompleted?.Invoke(target);
    }
}
