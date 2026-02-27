using UnityEngine;
using System.Collections.Generic;

public class WheelVisualAnimator : MonoBehaviour
{
    public enum Axis { X, Y, Z }

    [System.Serializable]
    public class Wheel
    {
        public Transform steerPivot;       // optional yaw
        public Transform spinTransform;    // spins
        public float radius = 0.33f;       // meters
        public Axis spinAxis = Axis.X;    // local axis that spins
        public bool multiplyByLossyScale = true;
    }

    [Header("References")]
    public Rigidbody rb;                 // car rigidbody (root)
    public WaypointCar waypointCar;        // for steer angle
    [Tooltip("Transform used to compute speed. Leave empty to auto-pick the car root.")]
    public Transform speedSource;        // <-- NEW

    [Header("Wheels (Front)")]
    public Wheel[] frontWheels;

    [Header("Wheels (Rear)")]
    public Wheel[] rearWheels;

    [Header("Tuning")]
    public float steerLerp = 10f;
    public float spinMultiplier = 1f;      // 1=physical, -1 flips direction
    public bool useUnscaledTime = false;

    [Header("Debug")]
    public bool debugInfo = false;         // logs speeds
    public bool drawAxes = false;

    // internal
    public float smoothSteer { get; private set; }
    Vector3 lastPos;
    bool hasLastPos;

    struct State { public float rAdj; public Vector3 axis; public float angle; }
    readonly List<State> frontState = new List<State>();
    readonly List<State> rearState = new List<State>();

    void Reset()
    {
        rb = GetComponentInParent<Rigidbody>();
        waypointCar = GetComponentInParent<WaypointCar>();
        AutoPickSpeedSource();
    }

    void OnValidate()
    {
        if (speedSource == null) AutoPickSpeedSource();
    }

    void AutoPickSpeedSource()
    {
        if (rb != null) { speedSource = rb.transform; return; }
        if (waypointCar != null) { speedSource = waypointCar.transform; return; }
        speedSource = transform.root != null ? transform.root : transform;
    }

    void Awake()
    {
        BuildState(frontWheels, frontState);
        BuildState(rearWheels, rearState);
        if (speedSource == null) AutoPickSpeedSource();
    }

    void BuildState(Wheel[] wheels, List<State> list)
    {
        list.Clear();
        if (wheels == null) return;
        foreach (var w in wheels)
        {
            State s = new State { rAdj = 0.33f, axis = Vector3.right, angle = 0f };
            if (w != null && w.spinTransform != null)
            {
                float r = Mathf.Max(0.001f, w.radius);
                if (w.multiplyByLossyScale)
                {
                    var sc = w.spinTransform.lossyScale;
                    if (w.spinAxis == Axis.X) r *= Mathf.Abs(sc.x);
                    if (w.spinAxis == Axis.Y) r *= Mathf.Abs(sc.y);
                    if (w.spinAxis == Axis.Z) r *= Mathf.Abs(sc.z);
                }
                s.rAdj = r;
                s.axis = (w.spinAxis == Axis.X) ? Vector3.right :
                         (w.spinAxis == Axis.Y) ? Vector3.up :
                                                  Vector3.forward;
            }
            list.Add(s);
        }
    }

    void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float targetSteer = (waypointCar != null) ? waypointCar.steerVisualDeg : 0f;
        smoothSteer = Mathf.Lerp(smoothSteer, targetSteer, steerLerp * Mathf.Max(0.000001f, dt));

        // steer pivots
        if (frontWheels != null)
            foreach (var w in frontWheels)
                if (w != null && w.steerPivot != null)
                    w.steerPivot.localRotation = Quaternion.Euler(0f, smoothSteer, 0f);
    }

    void LateUpdate()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (dt <= 0f) return;

        float speed = ComputeSpeed(dt); // m/s

        ApplySpin(frontWheels, frontState, speed, dt);
        ApplySpin(rearWheels, rearState, speed, dt);

        if (debugInfo)
            Debug.Log($"[WheelVisualAnimator] speed={speed:0.00} m/s, steer={smoothSteer:0.0}бу");
    }

    float ComputeSpeed(float dt)
    {
        // authoritative position: speedSource (usually the car root that moves)
        if (speedSource == null) AutoPickSpeedSource();

        Vector3 p = speedSource.position;
        if (!hasLastPos) { lastPos = p; hasLastPos = true; return 0f; }

        float s = (p - lastPos).magnitude / dt;
        lastPos = p;
        return s;
    }

    void ApplySpin(Wheel[] wheels, List<State> list, float speedMS, float dt)
    {
        if (wheels == null) return;

        for (int idx = 0; idx < wheels.Length; idx++)
        {
            var w = wheels[idx];
            if (w == null || w.spinTransform == null) continue;

            // recompute radius in case scale changed
            float r = Mathf.Max(0.001f, w.radius);
            if (w.multiplyByLossyScale)
            {
                var sc = w.spinTransform.lossyScale;
                if (w.spinAxis == Axis.X) r *= Mathf.Abs(sc.x);
                if (w.spinAxis == Axis.Y) r *= Mathf.Abs(sc.y);
                if (w.spinAxis == Axis.Z) r *= Mathf.Abs(sc.z);
            }

            float deltaDeg = (speedMS / r) * Mathf.Rad2Deg * dt * spinMultiplier;

            var s = (idx < list.Count) ? list[idx] : new State();
            s.rAdj = r;
            s.axis = (w.spinAxis == Axis.X) ? Vector3.right :
                     (w.spinAxis == Axis.Y) ? Vector3.up :
                                              Vector3.forward;
            s.angle += deltaDeg;
            if (idx < list.Count) list[idx] = s; else list.Add(s);

            w.spinTransform.localRotation = Quaternion.AngleAxis(s.angle, s.axis);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!drawAxes) return;

        void DrawFor(Wheel[] wheels)
        {
            if (wheels == null) return;
            foreach (var w in wheels)
            {
                if (w == null || w.spinTransform == null) continue;
                Gizmos.color = Color.cyan;
                Vector3 axis =
                    (w.spinAxis == Axis.X) ? w.spinTransform.right :
                    (w.spinAxis == Axis.Y) ? w.spinTransform.up :
                                             w.spinTransform.forward;
                Gizmos.DrawLine(w.spinTransform.position,
                                w.spinTransform.position + axis * 0.5f);
            }
        }
        DrawFor(frontWheels);
        DrawFor(rearWheels);
    }
}
