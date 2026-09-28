using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController), typeof(Rigidbody), typeof(SphereCollider))]
public sealed class PlayerReturn : MonoBehaviour
{
    [SerializeField] private LineRenderer cable;
    [SerializeField] private Transform cableAttachment;
    [FormerlySerializedAs("maximumSampleDistance")]
    [SerializeField, Min(0.05f)] private float sampleDistance = 0.35f;
    [SerializeField, Range(1f, 90f)] private float turnSampleAngle = 10f;
    [SerializeField, Min(0.1f)] private float returnSpeed = 16f;
    [SerializeField, Min(0.05f)] private float loosePointSpacing = 0.25f;
    [SerializeField, Min(0f)] private float groundClearance = 0.01f;
    [SerializeField, Min(0.01f)] private float cableDropDuration = 0.25f;

    private readonly List<Vector3> path = new();
    private readonly List<Vector3> raisedCablePoints = new();
    private readonly List<Vector3> looseCablePoints = new();
    private PlayerController player;
    private Rigidbody body;
    private SphereCollider sphere;
    private InputManager input;
    private Vector3 previousPosition;
    private Vector3 previousDirection;
    private Vector3 lastSamplePosition;
    private float nextGroundUpdateTime;
    private float cableReleaseTime;
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
        sphere = GetComponent<SphereCollider>();
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
        nextGroundUpdateTime = 0f;
        cableReleaseTime = Time.time - cableDropDuration;
    }

    public void ResetAtCamp()
    {
        // Passing another part of the camp during a return must not skip the exit point.
        if (returning)
            return;

        tracking = false;
        path.Clear();
        raisedCablePoints.Clear();
        looseCablePoints.Clear();
        nextGroundUpdateTime = 0f;
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
        float radius = sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        Vector3 center = from + transform.TransformVector(sphere.center);
        const float skin = 0.02f;

        Vector3 delta = to - from;
        float distance = delta.magnitude;
        if (distance < 0.0001f)
            return true;

        foreach (RaycastHit hit in Physics.SphereCastAll(
            center, radius - skin, delta / distance,
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
            float radius = sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            Vector3 center = position + transform.TransformVector(sphere.center);
            const float skin = 0.02f;

            foreach (RaycastHit hit in Physics.SphereCastAll(
                center, radius - skin, direction,
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
        cableReleaseTime = Time.time;
        nextGroundUpdateTime = 0f;
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
            nextGroundUpdateTime = 0f;
            return;
        }

        if (returning)
        {
            Vector3 offset = Vector3.Project(cableAttachment.position - transform.position, transform.up);
            cable.positionCount = path.Count + 1;
            for (int i = 0; i < path.Count; i++)
                cable.SetPosition(i, path[i] + offset);
            cable.SetPosition(path.Count, cableAttachment.position);
            return;
        }

        if (Time.time >= nextGroundUpdateTime)
        {
            BuildLooseCablePoints();
            nextGroundUpdateTime = Time.time + 0.1f;
        }

        float drop = Mathf.SmoothStep(0f, 1f, (Time.time - cableReleaseTime) / cableDropDuration);
        cable.positionCount = looseCablePoints.Count + 1;
        for (int i = 0; i < looseCablePoints.Count; i++)
            cable.SetPosition(i, Vector3.Lerp(raisedCablePoints[i], looseCablePoints[i], drop));
        cable.SetPosition(looseCablePoints.Count, cableAttachment.position);
    }

    private void BuildLooseCablePoints()
    {
        // These samples only change the rendered cable, never the player's return path.
        raisedCablePoints.Clear();
        looseCablePoints.Clear();
        Vector3 up = transform.up;
        Vector3 offset = Vector3.Project(cableAttachment.position - transform.position, up);
        float clearance = cable.widthMultiplier * 0.5f + groundClearance;

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 from = path[i] + offset;
            Vector3 to = i + 1 < path.Count ? path[i + 1] + offset : cableAttachment.position;
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(from, to) / loosePointSpacing));
            for (int j = 0; j < steps; j++)
            {
                Vector3 point = Vector3.Lerp(from, to, (float)j / steps);
                Vector3 groundPoint = point;
                float nearestDistance = float.PositiveInfinity;
                foreach (RaycastHit hit in Physics.RaycastAll(
                    point, -up, 8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.attachedRigidbody == body ||
                        Physics.GetIgnoreLayerCollision(gameObject.layer, hit.collider.gameObject.layer) ||
                        hit.distance >= nearestDistance)
                        continue;

                    nearestDistance = hit.distance;
                    groundPoint = hit.point + hit.normal * clearance;
                }

                raisedCablePoints.Add(point);
                looseCablePoints.Add(groundPoint);
            }
        }
    }
}
