using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private InputActionReference inputAction;

    private Rigidbody2D rb;
    private PlayerHealth playerHealth;
    private PlayerStats playerStats;

    public bool IsMoving => enabled && input.sqrMagnitude > 0.01f;

    public Vector2 FacingDirection { get; private set; }

    private Vector2 input;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerHealth = GetComponent<PlayerHealth>();
        playerStats = GetComponent<PlayerStats>();
        FacingDirection = Vector2.right;
    }

    private void OnEnable()
    {
        inputAction.action.Enable();
        playerHealth.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        inputAction.action.Disable();
        playerHealth.OnDied -= HandleDied;
    }

    private void Update()
    {
        input = inputAction.action.ReadValue<Vector2>();

        if (input.sqrMagnitude > 0.01f)
            FacingDirection = input.normalized;
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = input * playerStats.Speed;
    }

    private void HandleDied()
    {
        rb.linearVelocity = Vector2.zero;
        enabled = false;
        Debug.Log("이동 중단");
    }

    // For Debug (바라보는 방향 표시)
    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.red;
    //    Gizmos.DrawRay(transform.position, FacingDirection * 3);
    //}
}