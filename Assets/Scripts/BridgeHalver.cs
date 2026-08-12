using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Shortens bridge and ALWAYS spawns player outside, facing the temple gates.
/// Spawn is door-relative (not mesh-bounds), so it cannot land inside the palace.
/// </summary>
public class BridgeHalver : MonoBehaviour
{
    public float factor = 0.5f;
    public float bridgeHalfLength = 200f; // distance from gates to spawn after shorten

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoRun()
    {
        var cathedral = GameObject.Find("Cathedral");
        if (cathedral == null) return;

        var runner = cathedral.GetComponent<BridgeHalver>();
        if (runner == null) runner = cathedral.AddComponent<BridgeHalver>();

        // Run after other Awakes; place player outside for sure
        runner.StartCoroutine(runner.ApplyNextFrame());
    }

    IEnumerator ApplyNextFrame()
    {
        yield return null; // one frame — physics/colliders ready
        Apply();
        yield return null;
        ForceSpawnOutside();
    }

    [ContextMenu("Shorten Bridge + Spawn Outside")]
    public void Apply()
    {
        if (GameObject.Find("BridgeHalvedMarker") == null)
            ShortenBridgeMeshes();

        EnsureOutsideWalkProxy();
        ForceSpawnOutside();
    }

    void ShortenBridgeMeshes()
    {
        var pieces = CollectBridgePieces();
        if (pieces.Count == 0)
        {
            Debug.LogWarning("BridgeHalver: no bridge meshes to scale (proxy still used)");
            new GameObject("BridgeHalvedMarker").hideFlags = HideFlags.DontSave;
            return;
        }

        float doorZ = GetDoorZ();
        // Temple end of bridge ≈ door Z (slightly outside)
        Vector3 pivot = new Vector3(0f, 0f, doorZ);

        var root = new GameObject("BridgeScaleRoot_tmp");
        root.transform.position = pivot;

        var parents = new Dictionary<Transform, Transform>();
        foreach (var t in pieces)
        {
            parents[t] = t.parent;
            t.SetParent(root.transform, true);
        }
        root.transform.localScale = new Vector3(1f, 1f, factor);
        foreach (var t in pieces)
            t.SetParent(parents[t], true);
        DestroyImmediate(root);

        foreach (var t in pieces)
        {
            foreach (var mc in t.GetComponentsInChildren<MeshCollider>(true))
            {
                var mesh = mc.sharedMesh;
                mc.sharedMesh = null;
                mc.sharedMesh = mesh;
            }
        }

        var marker = new GameObject("BridgeHalvedMarker");
        marker.hideFlags = HideFlags.DontSave;
        Debug.Log("Bridge meshes scaled x" + factor);
    }

    void EnsureOutsideWalkProxy()
    {
        float doorZ = GetDoorZ();
        // Outside is more negative Z than the north gates
        float outerZ = doorZ - bridgeHalfLength;
        float midZ = (doorZ + outerZ) * 0.5f;
        float len = Mathf.Abs(doorZ - outerZ) + 8f;

        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go != null && go.name == "BridgeWalkProxy")
                DestroyImmediate(go);
        }

        var proxy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        proxy.name = "BridgeWalkProxy";
        var mr = proxy.GetComponent<MeshRenderer>();
        if (mr != null) DestroyImmediate(mr);

        proxy.transform.position = new Vector3(0f, -0.05f, midZ);
        proxy.transform.localScale = new Vector3(4f, 0.4f, len);
        proxy.layer = 0;
    }

    public void ForceSpawnOutside()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player == null) return;

        float doorZ = GetDoorZ();
        float spawnZ = doorZ - bridgeHalfLength + 6f; // near outer end, on the bridge
        Vector3 probe = new Vector3(0f, 10f, spawnZ);

        float groundY = 0.15f;
        if (Physics.Raycast(probe, Vector3.down, out RaycastHit hit, 40f, ~0, QueryTriggerInteraction.Ignore))
            groundY = hit.point.y;

        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // Face +Z toward the palace gates
        player.transform.SetPositionAndRotation(
            new Vector3(0f, groundY + 0.05f, spawnZ),
            Quaternion.LookRotation(Vector3.forward, Vector3.up));

        if (cc != null)
        {
            cc.enabled = true;
            Physics.SyncTransforms();
            cc.Move(Vector3.zero);
        }

        Debug.Log($"Spawn OUTSIDE on bridge at z={spawnZ:F1}, doorZ={doorZ:F1}, y={groundY:F2}");
    }

    float GetDoorZ()
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            string n = t.name.ToLowerInvariant();
            if (n.Contains("fx_door_l") || n.Contains("door_l") || n.Contains("gateframe_l"))
                return t.position.z;
        }
        // Known export: north gates ≈ -20.5
        return -20.5f;
    }

    List<Transform> CollectBridgePieces()
    {
        var pieces = new List<Transform>();
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            string n = t.name.ToLowerInvariant();
            if (!n.Contains("bridge")) continue;
            if (n.Contains("proxy") || n.Contains("marker") || n.Contains("scale")) continue;
            if (t.GetComponent<Renderer>() == null && t.GetComponent<MeshFilter>() == null) continue;
            pieces.Add(t);
        }
        return pieces;
    }
}
