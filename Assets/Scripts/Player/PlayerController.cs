using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Top-down movement and one-button interaction (E). Walking costs energy.
/// Inputs are defined in code so this works without extra setup.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(SurvivalStats), typeof(Inventory))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;

    [Header("Walking costs")]
    public float energyPerUnit = 0.05f;    // walking only costs energy; time passes by itself (GameClock)

    [Header("Interaction")]
    public float interactRadius = 1.6f;

    public Interactable Nearest { get; private set; }

    /// <summary>The terrain zone the player stands in (most recently entered), or null for normal ground.</summary>
    public TerrainZone CurrentTerrain => terrain.Count > 0 ? terrain[terrain.Count - 1] : null;

    readonly List<TerrainZone> terrain = new List<TerrainZone>();
    float speedMultiplier = 1f;

    Rigidbody2D rb;
    SurvivalStats stats;
    ActionSystem actions;
    InputAction move, interact, cancel;
    Vector2 moveInput;
    bool frozen;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<SurvivalStats>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        move = new InputAction("Move", InputActionType.Value);
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        move.AddBinding("<Gamepad>/leftStick");

        interact = new InputAction("Interact");
        interact.AddBinding("<Keyboard>/e");
        interact.AddBinding("<Gamepad>/buttonSouth");

        cancel = new InputAction("Cancel");
        cancel.AddBinding("<Keyboard>/escape");
        cancel.AddBinding("<Gamepad>/buttonEast");
    }

    void Start()
    {
        actions = FindFirstObjectByType<ActionSystem>();
    }

    void OnEnable() { move.Enable(); interact.Enable(); cancel.Enable(); }
    void OnDisable() { move.Disable(); interact.Disable(); cancel.Disable(); }

    void Update()
    {
        bool playing = !stats.IsDead &&
                       (GameManager.Instance == null || GameManager.Instance.State == GameState.Playing);
        bool busy = actions != null && actions.IsBusy;

        if (!playing)
        {
            moveInput = Vector2.zero;
            frozen = true;
            SetNearest(null);
            return;
        }

        if (actions != null)
        {
            if (interact.WasPressedThisFrame()) actions.OnInteractPressed(Nearest);
            if (cancel.WasPressedThisFrame()) actions.Cancel();
        }

        if (busy)
        {
            moveInput = Vector2.zero;
            frozen = true;
            return;
        }

        frozen = false;
        FindNearest();

        moveInput = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);

        var zone = CurrentTerrain;
        speedMultiplier = zone != null ? zone.speedMultiplier : 1f;
        if (moveInput.sqrMagnitude > 0.0001f)
        {
            float distance = moveSpeed * speedMultiplier * moveInput.magnitude * Time.deltaTime;
            float energyMultiplier = zone != null ? zone.energyMultiplier : 1f;
            float warmthLoss = zone != null ? zone.warmthPerUnit : 0f;
            stats.Apply(energy: -distance * energyPerUnit * energyMultiplier, warmth: -distance * warmthLoss);
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = frozen ? Vector2.zero : moveInput * moveSpeed * speedMultiplier;
    }

    public void EnterTerrain(TerrainZone zone)
    {
        if (terrain.Contains(zone)) return;
        terrain.Add(zone);
        if (zone.entryWarmthLoss > 0f) stats.Apply(warmth: -zone.entryWarmthLoss);
        if (zone.makesWet) stats.MakeWet(zone.wetMinutes);
        if (actions != null && !string.IsNullOrEmpty(zone.enterMessage)) actions.Say(zone.enterMessage);
    }

    public void ExitTerrain(TerrainZone zone) => terrain.Remove(zone);

    void FindNearest()
    {
        Interactable best = null;
        float bestDist = float.MaxValue;
        foreach (var hit in Physics2D.OverlapCircleAll(transform.position, interactRadius))
        {
            var item = hit.GetComponent<Interactable>();
            if (item == null || !item.Available) continue;
            float d = ((Vector2)hit.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = item; }
        }
        SetNearest(best);
    }

    void SetNearest(Interactable next)
    {
        if (next == Nearest) return;
        if (Nearest != null) Nearest.SetHighlight(false);
        Nearest = next;
        if (Nearest != null) Nearest.SetHighlight(true);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
