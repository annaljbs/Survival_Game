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
        if (moveInput.sqrMagnitude > 0.0001f)
        {
            float distance = moveSpeed * moveInput.magnitude * Time.deltaTime;
            stats.Apply(energy: -distance * energyPerUnit);
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = frozen ? Vector2.zero : moveInput * moveSpeed;
    }

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
