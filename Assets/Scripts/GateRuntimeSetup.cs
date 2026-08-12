using UnityEngine;

/// <summary>
/// Parents each door to a hinge on the COLUMN side (outer edge), not the center.
/// </summary>
public static class GateRuntimeSetup
{
    // Blender world → Unity glTF (X, Z, -Y): hinges at columns
    static readonly Vector3 BlenderHingeL = new Vector3(3.85f, 4.107f, -20.578f);
    static readonly Vector3 BlenderHingeR = new Vector3(-3.85f, 4.107f, -20.578f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void EnsureGatesWired() => WireNow();

    public static void WireNow()
    {
        var cathedral = GameObject.Find("Cathedral");
        if (cathedral == null) return;

        Transform doorL = FindDoor(cathedral, true);
        Transform doorR = FindDoor(cathedral, false);
        Transform neonL = FindNamed(cathedral, "doorneon_l", "fx_doorneon_l");
        Transform neonR = FindNamed(cathedral, "doorneon_r", "fx_doorneon_r");

        var hingesRoot = GameObject.Find("GateHinges");
        if (hingesRoot == null) hingesRoot = new GameObject("GateHinges");

        if (doorL != null)
            AttachToColumnHinge(doorL, neonL, hingesRoot.transform, true);
        if (doorR != null)
            AttachToColumnHinge(doorR, neonR, hingesRoot.transform, false);

        EnsureClickProxy(hingesRoot.transform, doorL, doorR);
        Debug.Log($"GateRuntimeSetup column-hinges L={doorL?.name} R={doorR?.name}");
    }

    static void AttachToColumnHinge(Transform door, Transform neon, Transform parent, bool isLeft)
    {
        // Strip GateDoor from the door mesh itself (must live on hinge only)
        foreach (var g in door.GetComponents<GateDoor>())
            Object.Destroy(g);

        // Unparent so we can place the hinge without dragging the door
        door.SetParent(null, true);
        if (neon != null) neon.SetParent(null, true);

        var rend = door.GetComponentInChildren<Renderer>();
        Bounds b = rend != null
            ? rend.bounds
            : new Bounds(door.position, new Vector3(3.5f, 10f, 0.5f));

        // Column-side edge: +X door → max.x, −X door → min.x
        bool plusX = b.center.x >= 0f;
        float hingeX = plusX ? b.max.x : b.min.x;
        // Prefer known Blender hinge X if close
        Vector3 preferred = plusX ? BlenderHingeL : BlenderHingeR;
        if (Mathf.Abs(preferred.x - hingeX) < 2.5f)
            hingeX = preferred.x;

        Vector3 hingePos = new Vector3(hingeX, Mathf.Min(b.min.y, preferred.y), b.center.z);
        // Keep Z at door plane
        hingePos.z = b.center.z;
        hingePos.y = b.min.y;

        string hingeName = plusX ? "Hinge_L" : "Hinge_R";
        var hingeGo = GameObject.Find(hingeName);
        if (hingeGo == null)
        {
            hingeGo = new GameObject(hingeName);
            hingeGo.transform.SetParent(parent, false);
        }

        // Clear previous children that aren't this door (avoid duplicates)
        var hinge = hingeGo.transform;
        for (int i = hinge.childCount - 1; i >= 0; i--)
        {
            var ch = hinge.GetChild(i);
            if (ch != door && ch != neon)
                ch.SetParent(null, true);
        }

        hinge.SetParent(parent, true);
        hinge.position = hingePos;
        hinge.rotation = Quaternion.identity;
        hinge.localScale = Vector3.one;

        // Parent door AFTER hinge is placed — keeps door world pose, pivot = column edge
        door.SetParent(hinge, true);
        if (neon != null) neon.SetParent(hinge, true);

        if (door.GetComponent<Collider>() == null && door.GetComponentInChildren<Collider>() == null)
            door.gameObject.AddComponent<BoxCollider>();

        var gate = hinge.GetComponent<GateDoor>();
        if (gate == null) gate = hinge.gameObject.AddComponent<GateDoor>();
        gate.isLeftLeaf = plusX; // +X leaf uses +angle like Blender L
        gate.openAngle = 110f;
        gate.openDuration = 2.3f;
        gate.ResetInit();

        // Sanity: door should be offset from hinge toward center (x→0)
        Vector3 local = door.localPosition;
        Debug.Log($"{hingeName} at {hingePos}, door local={local}, childCount={hinge.childCount}");
    }

    static Transform FindDoor(GameObject root, bool left)
    {
        Transform best = null;
        // Prefer exact names
        best = FindNamed(root, left ? "fx_door_l" : "fx_door_r");
        if (best != null && !best.name.ToLowerInvariant().Contains("neon"))
            return best;

        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name.ToLowerInvariant();
            if (!n.Contains("door") || n.Contains("neon") || n.Contains("light")) continue;
            var r = t.GetComponent<Renderer>() ?? t.GetComponentInChildren<Renderer>();
            if (r == null) continue;
            bool plus = r.bounds.center.x >= 0f;
            if (left && plus) return t;
            if (!left && !plus) return t;
        }
        return best;
    }

    static Transform FindNamed(GameObject root, params string[] tokens)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name.ToLowerInvariant();
            foreach (var token in tokens)
                if (n.Contains(token)) return t;
        }
        return null;
    }

    static void EnsureClickProxy(Transform parent, Transform doorL, Transform doorR)
    {
        const string name = "GateClickProxy";
        var existing = GameObject.Find(name);
        if (existing != null) Object.Destroy(existing);

        Vector3 center = new Vector3(0f, 4f, -20.5f);
        if (doorL != null && doorR != null)
            center = (doorL.position + doorR.position) * 0.5f;
        center.y = 4f;

        var proxy = new GameObject(name);
        proxy.transform.SetParent(parent, true);
        proxy.transform.position = center;
        var box = proxy.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(8f, 10f, 4f);
        proxy.AddComponent<GateClickTarget>();
    }
}

public class GateClickTarget : MonoBehaviour { }
