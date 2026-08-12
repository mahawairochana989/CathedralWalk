using UnityEngine;

/// <summary>
/// Door hinge pivot — place this AT the column-side edge, door mesh as child.
/// Rotates around world up (Y) like a real door.
/// </summary>
public class GateDoor : MonoBehaviour
{
    public bool isLeftLeaf = true;
    public float openAngle = 110f;
    public float openDuration = 2.3f;

    Quaternion closedRot;
    float signedAngle;
    bool targetOpen;
    float t;
    bool inited;

    public bool IsOpen => targetOpen && t >= 0.99f;
    public bool WantOpen => targetOpen;

    public void InitIfNeeded()
    {
        if (inited) return;
        inited = true;
        closedRot = transform.rotation; // world rotation at rest
        // Blender FX_Hinge_L: +Z (~+110°), FX_Hinge_R: -Z (~-110°)
        // After glTF, swing around world Y.
        signedAngle = isLeftLeaf ? openAngle : -openAngle;
    }

    public void ResetInit()
    {
        inited = false;
        t = targetOpen ? 1f : 0f;
        InitIfNeeded();
    }

    public void Open()
    {
        InitIfNeeded();
        targetOpen = true;
    }

    public void Close()
    {
        InitIfNeeded();
        targetOpen = false;
    }

    void Update()
    {
        if (!inited) return;

        float target = targetOpen ? 1f : 0f;
        float speed = openDuration > 0.01f ? 1f / openDuration : 10f;
        t = Mathf.MoveTowards(t, target, speed * Time.deltaTime);
        float s = t * t * (3f - 2f * t);

        // Rotate around world Y through the hinge pivot position
        transform.rotation = closedRot * Quaternion.AngleAxis(signedAngle * s, Vector3.up);

        bool block = t < 0.65f;
        foreach (var c in GetComponentsInChildren<Collider>(true))
        {
            if (c == null || c.isTrigger) continue;
            c.enabled = block;
        }
    }
}
