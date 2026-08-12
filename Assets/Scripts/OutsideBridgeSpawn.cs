using UnityEngine;

/// <summary>
/// Guarantees player starts outside on the bridge, facing the palace.
/// </summary>
[DefaultExecutionOrder(1000)]
public class OutsideBridgeSpawn : MonoBehaviour
{
    public float distanceFromGates = 194f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureOnPlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player == null) return;
        if (player.GetComponent<OutsideBridgeSpawn>() == null)
            player.AddComponent<OutsideBridgeSpawn>();
    }

    void Start()
    {
        Place();
        Invoke(nameof(Place), 0.05f);
        Invoke(nameof(Place), 0.25f);
    }

    void Place()
    {
        float doorZ = -20.5f;
        var cat = GameObject.Find("Cathedral");
        if (cat != null)
        {
            foreach (var t in cat.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant();
                if (n.Contains("fx_door_l") || n.Contains("door_l"))
                {
                    doorZ = t.position.z;
                    break;
                }
            }
        }

        float spawnZ = doorZ - distanceFromGates;
        Vector3 pos = new Vector3(0f, 0.2f, spawnZ);

        if (Physics.Raycast(new Vector3(0f, 8f, spawnZ), Vector3.down, out RaycastHit hit, 30f))
            pos.y = hit.point.y + 0.05f;

        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        transform.SetPositionAndRotation(pos, Quaternion.LookRotation(Vector3.forward, Vector3.up));
        if (cc != null)
        {
            cc.enabled = true;
            Physics.SyncTransforms();
        }
    }
}
