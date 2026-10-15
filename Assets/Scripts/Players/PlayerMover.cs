using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public sealed class PlayerMover : MonoBehaviour
{
    private float speed;
    [SerializeField, Min(0f)] private float groundAcceleration = 50f;
    [SerializeField, Min(0f)] private float airAcceleration = 15f;
    [SerializeField, Min(0f)]
    [FormerlySerializedAs("jumpHeight")]
    private float jumpVelocity = 6f;
    [SerializeField, Range(0f, 1f)] private float minimumGroundNormalY = 0.6f;
    [Header("신발 대시")]
    [SerializeField, Min(0f)] private float dashSpeed = 16f;
    [SerializeField, Min(0.01f)] private float dashDuration = 0.18f;
    [SerializeField, Min(0.01f)] private float dashRechargeTime = 2f;

    private Rigidbody rigid;
    private Transform cameraHolder;
    private InputManager inputManager;
    private UpgradeManager upgradeManager;
    private Vector2 inputDirection;
    private readonly HashSet<Collider> groundContacts = new();
    private bool jumpRequested;
    private int maxJumps = 1;
    private int jumpsUsed;
    private int maxDashCharges;
    private int dashCharges;
    private float dashRechargeRemaining;
    private float dashTimeRemaining;
    private Vector3 dashDirection;
    private bool dashRequested;
    private bool isDashing;
    private bool isInputSubscribed;

    public int MaxJumps => maxJumps;
    public int RemainingJumps => Mathf.Max(0, maxJumps - jumpsUsed);
    public int MaxDashCharges => maxDashCharges;
    public int DashCharges => dashCharges;
    public float DashRechargeProgress => dashCharges < maxDashCharges
        ? Mathf.Clamp01(1f - dashRechargeRemaining / dashRechargeTime) : 0f;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        rigid.interpolation = RigidbodyInterpolation.Interpolate;
        rigid.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void Start()
    {
        Camera mainCamera = Camera.main;
        cameraHolder = mainCamera != null ? mainCamera.transform.parent : null;
        SubscribeInputEvents();
    }

    private void OnEnable()
    {
        SubscribeInputEvents();
    }

    private void OnDisable()
    {
        UnsubscribeInputEvents();
        if (isDashing) EndDash();
        inputDirection = Vector2.zero;
        jumpRequested = false;
        dashRequested = false;
        groundContacts.Clear();
    }

    private void FixedUpdate()
    {
        groundContacts.RemoveWhere(collider => collider == null);

        Vector3 up = transform.up;
        bool grounded = groundContacts.Count > 0 && Vector3.Dot(rigid.linearVelocity, up) <= 0.1f;
        // Walking off a ledge consumes the ground jump; rising contacts cannot refill jumps.
        jumpsUsed = grounded ? 0 : Mathf.Max(jumpsUsed, 1);
        bool dashing = TickDash(up);
        Vector3 targetVelocity = GetMoveDirection(up) * speed;
        Vector3 currentVelocity = rigid.linearVelocity;
        Vector3 verticalVelocity = Vector3.Project(currentVelocity, up);
        Vector3 horizontalVelocity = currentVelocity - verticalVelocity;
        float acceleration = grounded ? groundAcceleration : airAcceleration;

        if (!dashing)
            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                targetVelocity,
                acceleration * Time.fixedDeltaTime);

        rigid.linearVelocity = horizontalVelocity + verticalVelocity;

        if (!jumpRequested)
        {
            return;
        }

        jumpRequested = false;
        if (jumpsUsed >= maxJumps)
        {
            return;
        }

        float currentVerticalSpeed = Vector3.Dot(rigid.linearVelocity, up);
        rigid.linearVelocity += up * (jumpVelocity - currentVerticalSpeed);
        jumpsUsed++;
        groundContacts.Clear();
    }

    private void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed) dashRequested = true;
    }

    private void TryStartDash(Vector3 up)
    {
        if (isDashing || dashCharges == 0) return;
        Vector3 direction = GetMoveDirection(up);
        if (direction == Vector3.zero)
            direction = Vector3.ProjectOnPlane(cameraHolder.forward, up).normalized;
        dashDirection = direction.normalized;
        if (dashCharges == maxDashCharges) dashRechargeRemaining = dashRechargeTime;
        dashCharges--;
        dashTimeRemaining = dashDuration;
        isDashing = true;
    }

    private bool TickDash(Vector3 up)
    {
        if (dashCharges < maxDashCharges)
        {
            dashRechargeRemaining -= Time.fixedDeltaTime;
            if (dashRechargeRemaining <= 0f)
            {
                dashCharges++;
                dashRechargeRemaining = dashCharges < maxDashCharges
                    ? dashRechargeRemaining + dashRechargeTime : 0f;
            }
        }

        if (isDashing && dashTimeRemaining <= 0f) EndDash();
        if (dashRequested)
        {
            dashRequested = false;
            TryStartDash(up);
        }
        if (!isDashing) return false;

        rigid.linearVelocity = dashDirection * dashSpeed + Vector3.Project(rigid.linearVelocity, up);
        dashTimeRemaining -= Time.fixedDeltaTime;
        return true;
    }

    private void EndDash()
    {
        isDashing = false;
        dashTimeRemaining = 0f;
        Vector3 vertical = Vector3.Project(rigid.linearVelocity, transform.up);
        rigid.linearVelocity = vertical + Vector3.ClampMagnitude(rigid.linearVelocity - vertical, speed);
    }

    private Vector3 GetMoveDirection(Vector3 up)
    {
        if (inputDirection == Vector2.zero || cameraHolder == null)
        {
            return Vector3.zero;
        }

        Vector3 front = Vector3.ProjectOnPlane(cameraHolder.forward, up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cameraHolder.right, up).normalized;
        Vector3 direction = front * inputDirection.y + right * inputDirection.x;
        return Vector3.ClampMagnitude(direction, 1f);
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        if (context.performed || context.started)
        {
            inputDirection = context.ReadValue<Vector2>();
        }
        else if (context.canceled)
        {
            inputDirection = Vector2.zero;
        }
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            jumpRequested = true;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        UpdateGroundContact(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        UpdateGroundContact(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        groundContacts.Remove(collision.collider);
    }

    private void UpdateGroundContact(Collision collision)
    {
        Vector3 up = transform.up;

        if (isDashing)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector3 normal = collision.GetContact(i).normal;
                if (Vector3.Dot(normal, up) < minimumGroundNormalY && Vector3.Dot(normal, dashDirection) < -0.1f)
                {
                    EndDash();
                    break;
                }
            }
        }

        for (int i = 0; i < collision.contactCount; i++)
        {
            if (Vector3.Dot(collision.GetContact(i).normal, up) >= minimumGroundNormalY)
            {
                groundContacts.Add(collision.collider);
                return;
            }
        }

        groundContacts.Remove(collision.collider);
    }

    private void SubscribeInputEvents()
    {
        if (isInputSubscribed || GameManager.Instance == null)
        {
            return;
        }

        inputManager = GameManager.Instance.InputManager;
        if (inputManager == null)
        {
            return;
        }

        inputManager.Subscribe(InputEvent.Move, OnMove);
        inputManager.Subscribe(InputEvent.Jump, OnJump);
        inputManager.Subscribe(InputEvent.Dash, OnDash);
        upgradeManager = GameManager.Instance.Upgrade;
        upgradeManager.SubscribeUpgradeChanged(RefreshMovementStats);
        RefreshMovementStats();
        isInputSubscribed = true;
    }

    private void UnsubscribeInputEvents()
    {
        if (!isInputSubscribed || inputManager == null)
        {
            return;
        }

        inputManager.Unsubscribe(InputEvent.Move, OnMove);
        inputManager.Unsubscribe(InputEvent.Jump, OnJump);
        inputManager.Unsubscribe(InputEvent.Dash, OnDash);
        upgradeManager.UnsubscribeUpgradeChanged(RefreshMovementStats);
        inputManager = null;
        isInputSubscribed = false;
    }

    private void RefreshMovementStats()
    {
        speed = upgradeManager.RuntimeStat.Equipment.Get(EquipmentStatType.MoveSpeed);
        int bootsUpgrades = upgradeManager.RuntimeLevel.GetEquipment((int)EquipmentType.Boots);
        maxJumps = Mathf.Min(bootsUpgrades + 1, 3);
        int previousCapacity = maxDashCharges;
        maxDashCharges = bootsUpgrades >= 4 ? 2 : bootsUpgrades >= 3 ? 1 : 0;
        // Unrelated upgrade notifications must not refill charges that were already spent.
        dashCharges = Mathf.Clamp(dashCharges + maxDashCharges - previousCapacity, 0, maxDashCharges);
        if (maxDashCharges == 0 && isDashing) EndDash();
    }
}
