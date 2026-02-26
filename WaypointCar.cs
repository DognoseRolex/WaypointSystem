using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WaypointCar : MonoBehaviour
{
    // Optional; will be auto-found if left null
    public BlinkerLight blinker;

    // Read by WheelVisualAnimator (degrees, left = -, right = +)
    public float steerVisualDeg { get; private set; }

    // ---------- Sensor Tuning ----------
    [Header("Sensor Tuning")]
    public bool includeTriggerColliders = false;   // true if NPC colliders are triggers
    public bool debugSensor = false;               // draw gizmos + logs
    float lastHitDist = -1f;
    Transform lastHitTransform = null;

    // ---------- Route ----------
    [Header("Route")]
    public WaypointPath path;          // assign a WaypointPath (array of Transforms)
    public bool loop = true;
    public int startIndex = 0;
    public bool autoPickStartIndex = true; // pick closest waypoint from current position

    // ---------- Driving ----------
    [Header("Driving")]
    public float maxSpeed = 9f;        // m/s (~32 km/h)
    public float accel = 5f;           // m/s^2
    public float brake = 12f;          // m/s^2
    public float steerLerp = 4f;       // steering responsiveness
    public float waypointRadius = 1.0f;

    // ---------- Spacing Sensor ----------
    [Header("Spacing Sensor")]
    public Transform sensorOrigin;     // front bumper empty
    public float sensorRange = 12f;
    public float sensorRadius = 1.0f;  // wider = more reliable lane sensing
    public LayerMask obstacleLayers;   // include NPC + Obstacle (barriers)
    public float minGap = 6.0f;        // desired following distance (m)
    public float crawlSpeed = 2.0f;    // m/s when close

    // ---------- Stop-line braking ----------
    [Header("Stop-line Braking")]
    public float stopBuffer = 1.5f;    // meters before the line to target 0 m/s
    public float holdSnapBack = 0.15f; // meters: if beyond line, pull back this much
    public float holdDeadzone = 0.08f; // meters: within this, freeze velocity

    // ---------- Runtime ----------
    [Header("Runtime (read-only)")]
    [SerializeField] bool stoplineBlocked = false;
    [SerializeField] Vector3 stoplinePoint;

    [HideInInspector] public NPCSpawner spawner;

    Rigidbody rb;
    int i;
    float curSpeed;

    /// Called by TrafficStopLine trigger
    public void SetStoplineBlocked(bool blocked, Vector3 point)
    {
        stoplineBlocked = blocked;
        stoplinePoint = point;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (blinker == null) blinker = GetComponentInChildren<BlinkerLight>();

        // Choose a sensible waypoint index from current placement
        if (path && path.Points != null && path.Points.Length > 0)
        {
            if (autoPickStartIndex)
                i = FindClosestWaypointIndex(transform.position);
            else
                i = Mathf.Clamp(startIndex, 0, path.Points.Length - 1);

            // If we spawned basically ON the chosen waypoint, advance one to avoid stalling
            if (Vector3.Distance(transform.position, path.Points[i].position) <= waypointRadius * 0.5f)
                i = (i + 1) % path.Points.Length;
        }
    }

    void FixedUpdate()
    {
        if (path == null || path.Points == null || path.Points.Length == 0) return;

        Transform targetT = path.Points[i];
        Vector3 toTarget = targetT.position - transform.position;
        Vector3 flatDir = Vector3.ProjectOnPlane(toTarget, Vector3.up).normalized;

        // Visual steering angle for wheel animation
        steerVisualDeg = Mathf.Clamp(
            Vector3.SignedAngle(transform.forward, flatDir, Vector3.up),
            -35f, 35f
        );

        // Rotate towards path direction
        if (flatDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(flatDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, steerLerp * Time.fixedDeltaTime);
        }

        // Decide desired speed from stop-line + spacing
        float desired = maxSpeed;

        if (stoplineBlocked)
        {
            // Horizontal distance from bumper to stop point (positive = in front)
            Vector3 a = transform.position;
            Vector3 b = stoplinePoint;
            a.y = b.y;
            float dist = Vector3.Dot((b - a), transform.forward);

            if (dist <= 0f)
            {
                // Already at/past line: hold just behind it and zero velocity
                Vector3 target = b - transform.forward * holdSnapBack;
                rb.MovePosition(Vector3.Lerp(rb.position, target, 0.6f));
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
#endif
                desired = 0f;
            }
            else
            {
                // Approach stop point, reduce desired speed as we get close
                float targetDist = Mathf.Max(0f, dist - stopBuffer);
                desired = Mathf.Clamp(targetDist, 0f, maxSpeed);
                if (dist < holdDeadzone) desired = 0f;
            }
        }
        else
        {
            // Spacing sensor in front
            if (TryGetFrontGap(out float gap))
            {
                if (gap < minGap)
                    desired = (gap < (minGap * 0.5f)) ? 0f : Mathf.Min(desired, crawlSpeed);
            }
        }

        // Accelerate / brake toward desired
        if (curSpeed < desired) curSpeed = Mathf.Min(desired, curSpeed + accel * Time.fixedDeltaTime);
        else curSpeed = Mathf.Max(desired, curSpeed - brake * Time.fixedDeltaTime);

        // Move forward
        Vector3 move = transform.forward * curSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);

        // Advance waypoint (and handle per-waypoint instructions)
        if (toTarget.magnitude <= waypointRadius)
        {
            // Apply blinker instruction on this waypoint (if present)
            var instr = targetT.GetComponent<WaypointBlinkInstruction>();
            if (instr != null && blinker != null)
                blinker.Set(instr.mode, instr.autoClearSeconds);

            // Despawn marker support
            if (targetT.GetComponent<WaypointDespawnHere>() != null)
            {
                Despawn();
                return;
            }

            i++;
            if (i >= path.Points.Length)
            {
                if (loop) i = 0;
                else { curSpeed = 0; Despawn(); }
            }
        }
    }

    void Despawn()
    {
        if (spawner != null) spawner.NotifyDespawn(this);
        Destroy(gameObject);
    }

    int FindClosestWaypointIndex(Vector3 pos)
    {
        int best = 0;
        float bestDist = float.MaxValue;
        for (int k = 0; k < path.Points.Length; k++)
        {
            var p = path.Points[k];
            if (!p) continue;
            float d = Vector3.SqrMagnitude(p.position - pos);
            if (d < bestDist) { bestDist = d; best = k; }
        }
        return best;
    }

    // ---------- Spacing Sensor (robust) ----------
    bool TryGetFrontGap(out float gap)
    {
        gap = 9999f;
        lastHitDist = -1f;
        lastHitTransform = null;

        if (sensorOrigin == null) return false;

        var qti = includeTriggerColliders ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;
        Ray ray = new Ray(sensorOrigin.position, transform.forward);
        RaycastHit[] hits = Physics.SphereCastAll(ray, sensorRadius, sensorRange, obstacleLayers, qti);

        if (hits == null || hits.Length == 0) return false;

        float nearest = float.MaxValue;
        Transform nearestT = null;

        foreach (var h in hits)
        {
            // Skip self
            if (h.rigidbody != null && h.rigidbody == rb) continue;
            if (h.transform == transform || h.transform.IsChildOf(transform)) continue;

            // Only consider objects in front
            Vector3 dirToHit = (h.point - sensorOrigin.position).normalized;
            if (Vector3.Dot(transform.forward, dirToHit) < 0.2f) continue;

            if (h.distance < nearest)
            {
                nearest = h.distance;
                nearestT = h.transform;
            }
        }

        if (nearest == float.MaxValue) return false;

        gap = Mathf.Max(0f, nearest - sensorRadius);
        lastHitDist = gap;
        lastHitTransform = nearestT;

        if (debugSensor)
        {
            Debug.DrawLine(sensorOrigin.position,
                           sensorOrigin.position + transform.forward * (gap + sensorRadius),
                           Color.yellow, 0.1f);
            if (nearestT) Debug.Log($"[WaypointCar] Front hit: {nearestT.name} gap={gap:0.00}m");
        }

        return true;
    }

    // ---------- Gizmos ----------
    void OnDrawGizmosSelected()
    {
        if (!debugSensor || sensorOrigin == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(sensorOrigin.position, sensorRadius);
        Gizmos.DrawLine(sensorOrigin.position, sensorOrigin.position + transform.forward * sensorRange);

        if (lastHitDist >= 0f)
        {
            Gizmos.color = Color.red;
            Vector3 p = sensorOrigin.position + transform.forward * (lastHitDist + sensorRadius);
            Gizmos.DrawWireSphere(p, 0.2f);
        }
    }
}
