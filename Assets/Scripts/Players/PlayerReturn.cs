using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController), typeof(Rigidbody), typeof(CapsuleCollider))]
public sealed class PlayerReturn : MonoBehaviour
{
    [SerializeField] private LineRenderer cable;
    [SerializeField] private Transform cableAttachment;
    [FormerlySerializedAs("maximumSampleDistance")]
    [SerializeField, Min(0.05f)] private float sampleDistance = 0.35f;
    [SerializeField, Range(1f, 90f)] private float turnSampleAngle = 10f;
    [SerializeField, Min(0.1f)] private float returnSpeed = 16f;

    private readonly List<Vector3> path = new();
    private PlayerController player;
    private Rigidbody body;
    private CapsuleCollider capsule;
    private InputManager input;
    private Vector3 previousPosition;
    private Vector3 previousDirection;
    private Vector3 lastSamplePosition;
    private bool tracking;
    private bool returning;
    private bool stopRequested;
    private bool moverWasEnabled;
    private bool minerWasEnabled;
    private bool subscribed;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        body = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        cable.positionCount = 0;
    }

    private void Start()
    {
        input = GameManager.Instance.InputManager;
        input.Subscribe(InputEvent.ReturnToCamp, OnReturn);
        subscribed = true;
    }

    private void OnDisable()
    {
        if (returning)
            StopReturn();
    }

    private void OnDestroy()
    {
        if (subscribed)
            input.Unsubscribe(InputEvent.ReturnToCamp, OnReturn);
    }

    public void BeginTracking()
    {
        if (returning || tracking)
            return;

        tracking = true;
        path.Clear();
        path.Add(body.position);
        previousPosition = body.position;
        previousDirection = Vector3.zero;
        lastSamplePosition = body.position;
    }

    public void ResetAtCamp()
    {
        // Passing another part of the camp during a return must not skip the exit point.
        if (returning)
            return;

        tracking = false;
        path.Clear();
        cable.positionCount = 0;
    }

    private void OnReturn(InputAction.CallbackContext context)
    {
        if (context.performed && isActiveAndEnabled)
            BeginReturn();
    }

    private void BeginReturn()
    {
        if (!tracking || returning || path.Count == 0)
            return;

        returning = true;
        stopRequested = false;
        moverWasEnabled = player.PlayerMover.enabled;
        minerWasEnabled = player.PlayerMiner.enabled;
        player.PlayerMover.enabled = false;
        player.PlayerMiner.enabled = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.isKinematic = true;
    }

    private void FixedUpdate()
    {
        if (returning)
        {
            TickReturn();
            return;
        }

        if (!tracking)
            return;

        Vector3 position = body.position;
        Vector3 movement = position - previousPosition;
        if (movement.sqrMagnitude > 0.000001f)
        {
            // Keep the point before a turn, rather than cutting across the corner.
            if (previousDirection != Vector3.zero &&
                Vector3.Angle(previousDirection, movement) >= turnSampleAngle &&
                (previousPosition - path[path.Count - 1]).sqrMagnitude > 0.000001f)
            {
                PullPathTail(previousPosition);
                path.Add(previousPosition);
            }

            previousDirection = movement.normalized;
        }

        float distanceSquared = (position - lastSamplePosition).sqrMagnitude;
        if (distanceSquared >= sampleDistance * sampleDistance)
        {
            PullPathTail(position);
            path.Add(position);
            lastSamplePosition = position;
        }

        previousPosition = position;
    }

    private void PullPathTail(Vector3 position)
    {
        // The live endpoint replaces the last bend only when the player can pass directly.
        // Keep the first point: it is the camp boundary, not a removable bend.
        while (path.Count > 1 && CanShortcut(position, path[path.Count - 2]))
            path.RemoveAt(path.Count - 1);
    }

    private bool CanShortcut(Vector3 from, Vector3 to)
    {
        Vector3 scale = transform.lossyScale;
        float radius = capsule.radius * Mathf.Max(scale.x, scale.z);
        float halfSegment = Mathf.Max(0f, capsule.height * scale.y * 0.5f - radius);
        Vector3 center = from + transform.TransformVector(capsule.center);
        Vector3 endOffset = transform.up * halfSegment;
        const float skin = 0.02f;

        Vector3 delta = to - from;
        float distance = delta.magnitude;
        if (distance < 0.0001f)
            return true;

        foreach (RaycastHit hit in Physics.CapsuleCastAll(
            center + endOffset, center - endOffset, radius - skin, delta / distance,
            distance + skin, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.attachedRigidbody != body &&
                !Physics.GetIgnoreLayerCollision(gameObject.layer, hit.collider.gameObject.layer))
                return false;
        }

        return true;
    }

    private void TickReturn()
    {
        // Let the previous MovePosition finish before restoring dynamic movement.
        if (stopRequested || path.Count == 0)
        {
            bool arrived = path.Count == 0;
            StopReturn();
            if (arrived)
            {
                ResetAtCamp();
                // The exit point is just outside the trigger. Walking away can pay out again.
                BeginTracking();
            }
            previousPosition = body.position;
            previousDirection = Vector3.zero;
            lastSamplePosition = body.position;
            return;
        }

        Vector3 position = body.position;
        PullPathTail(position);
        float remainingDistance = returnSpeed * Time.fixedDeltaTime;
        while (remainingDistance > 0f && path.Count > 0)
        {
            Vector3 target = path[path.Count - 1];
            Vector3 delta = target - position;
            float distance = delta.magnitude;
            if (distance < 0.0001f)
            {
                path.RemoveAt(path.Count - 1);
                continue;
            }

            Vector3 direction = delta / distance;
            float step = Mathf.Min(remainingDistance, distance);
            float allowedStep = step;
            Vector3 scale = transform.lossyScale;
            float radius = capsule.radius * Mathf.Max(scale.x, scale.z);
            float halfSegment = Mathf.Max(0f, capsule.height * scale.y * 0.5f - radius);
            Vector3 center = position + transform.TransformVector(capsule.center);
            Vector3 endOffset = transform.up * halfSegment;
            const float skin = 0.02f;

            foreach (RaycastHit hit in Physics.CapsuleCastAll(
                center + endOffset, center - endOffset, radius - skin, direction,
                step + skin, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.attachedRigidbody == body ||
                    Physics.GetIgnoreLayerCollision(gameObject.layer, hit.collider.gameObject.layer))
                    continue;

                allowedStep = Mathf.Min(allowedStep, Mathf.Max(0f, hit.distance - skin));
            }

            position += direction * allowedStep;
            if (allowedStep < step)
            {
                stopRequested = true;
                break;
            }

            remainingDistance -= step;
            if (step >= distance)
                path.RemoveAt(path.Count - 1);
        }

        body.MovePosition(position);
    }

    private void StopReturn()
    {
        returning = false;
        stopRequested = false;
        body.isKinematic = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        player.PlayerMover.enabled = moverWasEnabled;
        player.PlayerMiner.enabled = minerWasEnabled;
    }

    private void LateUpdate()
    {
        RefreshCable();
    }

    private void RefreshCable()
    {
        if (path.Count == 0 ||
            (path.Count == 1 && (transform.position - path[0]).sqrMagnitude < 0.0025f))
        {
            cable.positionCount = 0;
            return;
        }

        // Store body positions for movement; draw the cable at the attachment's height.
        Vector3 offset = Vector3.Project(cableAttachment.position - transform.position, transform.up);
        cable.positionCount = path.Count + 1;
        for (int i = 0; i < path.Count; i++)
            cable.SetPosition(i, path[i] + offset);
        cable.SetPosition(path.Count, cableAttachment.position);
    }
}
