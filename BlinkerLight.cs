using System.Collections;
using UnityEngine;

/// Controls left/right/hazard blinkers for an NPC car.
/// Works with GameObjects, Light components, and/or emissive Renderers.
public class BlinkerLight : MonoBehaviour
{
    public enum Mode { Off, Left, Right, Hazard }

    [Header("Left side")]
    public GameObject[] leftObjects;
    public Light[] leftLights;
    public Renderer[] leftEmitters;

    [Header("Right side")]
    public GameObject[] rightObjects;
    public Light[] rightLights;
    public Renderer[] rightEmitters;

    [Header("Blinking")]
    public float onTime = 0.5f;
    public float offTime = 0.5f;
    public Color emissionColor = new Color(1f, 0.6f, 0f); // amber-ish
    public float emissionIntensity = 2.5f;

    Coroutine loop;
    Mode current = Mode.Off;
    MaterialPropertyBlock mpb;

    void OnEnable()
    {
        if (mpb == null) mpb = new MaterialPropertyBlock();
        ApplyState(false, false); // all off
    }
    void OnDisable()
    {
        StopBlink();
        ApplyState(false, false);
    }

    public Mode Current => current;

    /// Set a mode, optionally clear automatically after seconds (<=0 means keep).
    public void Set(Mode mode, float autoClearSeconds = 0f)
    {
        if (mode == current && loop != null) return;

        StopBlink();
        current = mode;

        if (current == Mode.Off)
        {
            ApplyState(false, false);
            return;
        }

        loop = StartCoroutine(BlinkLoop(autoClearSeconds));
    }

    public void Clear() => Set(Mode.Off);

    IEnumerator BlinkLoop(float autoClearSeconds)
    {
        float timer = 0f;
        while (true)
        {
            ApplyState(true, true);
            yield return new WaitForSeconds(onTime);

            ApplyState(false, false);
            yield return new WaitForSeconds(offTime);

            if (autoClearSeconds > 0f)
            {
                timer += onTime + offTime;
                if (timer >= autoClearSeconds)
                {
                    Set(Mode.Off, 0f);
                    yield break;
                }
            }
        }
    }

    void StopBlink()
    {
        if (loop != null) StopCoroutine(loop);
        loop = null;
    }

    void ApplyState(bool leftOn, bool rightOn)
    {
        // Only the side(s) matching current mode should blink
        leftOn = (current == Mode.Left || current == Mode.Hazard) && leftOn;
        rightOn = (current == Mode.Right || current == Mode.Hazard) && rightOn;

        ToggleSide(leftObjects, leftLights, leftEmitters, leftOn);
        ToggleSide(rightObjects, rightLights, rightEmitters, rightOn);
    }

    void ToggleSide(GameObject[] objs, Light[] lights, Renderer[] rend, bool on)
    {
        if (objs != null)
            foreach (var go in objs) if (go) go.SetActive(on);

        if (lights != null)
            foreach (var l in lights) if (l) l.enabled = on;

        if (rend != null)
        {
            foreach (var r in rend)
            {
                if (!r) continue;
                mpb.Clear();
                r.GetPropertyBlock(mpb);
                Color c = on ? emissionColor * Mathf.LinearToGammaSpace(emissionIntensity) : Color.black;
                mpb.SetColor("_EmissionColor", c);
                r.SetPropertyBlock(mpb);
                foreach (var m in r.sharedMaterials) if (m) m.EnableKeyword("_EMISSION");
            }
        }
    }
}
