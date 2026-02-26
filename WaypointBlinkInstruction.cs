using UnityEngine;

/// Attach to a waypoint Transform to tell cars what to blink when they arrive.
public class WaypointBlinkInstruction : MonoBehaviour
{
    public BlinkerLight.Mode mode = BlinkerLight.Mode.Left;
    [Tooltip("Seconds to keep this mode. <= 0 means keep until another instruction changes it.")]
    public float autoClearSeconds = 0f;
}
