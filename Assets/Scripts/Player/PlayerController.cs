using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Top-down movement, gathering (E), eat (1), drink (2).
/// Inputs are defined in code so this works without extra setup.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(SurvivalStats), typeof(Inventory))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float sprintMultiplier = 1.6f;

    [Header("Actions")]
    public float interactRadius = 1.3f;
    public float berryHunger = 25f;
    public float waterThirst = 35f;

    Rigidbody2D rb;
    SurvivalStats stats;
    Inventory inventory;

    InputAction move, sprint, interact, eat, drink;
    Vector2 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<SurvivalStats>();
        inventory = GetComponent<Inventory>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        move = new InputAction("Move", InputActionType.Value);
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        move.AddBinding("<Gamepad>/leftStick");

        sprint = new InputAction("Sprint", binding: "<Keyboard>/leftShift");
        interact = new InputAction("Interact", binding: "<Keyboard>/e");
        eat = new InputAction("Eat", binding: "<Keyboard>/1");
        drink = new InputAction("Drink", binding: "<Keyboard>/2");
    }

    void OnEnable()
    {
        move.Enable(); sprint.Enable(); interact.Enable();
        eat.Enable(); drink.Enable();
    }

    void OnDisable()
    {
        move.Disable(); sprint.Disable(); interact.Disable();
        eat.Disable(); drink.Disable();
    }

    void Update()
    {
        if (stats.IsDead) { moveInput = Vector2.zero; stats.IsSprinting = false; return; }

        moveInput = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
        stats.IsSprinting = sprint.IsPressed() && moveInput.sqrMagnitude > 0.01f && !stats.IsExhausted;

        if (interact.WasPressedThisFrame()) TryGather();
        if (eat.WasPressedThisFrame() && inventory.Remove(ItemType.Berry)) stats.Eat(berryHunger);
        if (drink.WasPressedThisFrame() && inventory.Remove(ItemType.Water)) stats.Drink(waterThirst);
    }

    void FixedUpdate()
    {
        float speed = moveSpeed * (stats.IsSprinting ? sprintMultiplier : 1f);
        if (stats.IsExhausted) speed *= 0.7f; // exhausted: slower
        rb.linearVelocity = moveInput * speed;
    }

    void TryGather()
    {
        ResourceNode best = null;
        float bestDist = float.MaxValue;
        foreach (var hit in Physics2D.OverlapCircleAll(transform.position, interactRadius))
        {
            var node = hit.GetComponent<ResourceNode>();
            if (node == null || !node.IsAvailable) continue;
            float d = ((Vector2)hit.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = node; }
        }
        if (best != null) best.Harvest(inventory);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green; Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
