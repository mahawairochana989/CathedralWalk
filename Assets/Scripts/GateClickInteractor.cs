using UnityEngine;

/// <summary>
/// E or Left Mouse opens/closes temple doors. Works near gates even without perfect aim.
/// </summary>
public class GateClickInteractor : MonoBehaviour
{
    public float maxDistance = 35f;
    public KeyCode altKey = KeyCode.E;

    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        GateRuntimeSetup.WireNow();
    }

    void Start()
    {
        // Re-wire after OutsideBridgeSpawn etc.
        GateRuntimeSetup.WireNow();
    }

    void Update()
    {
        bool near = IsNearGates(out float dist);

        bool press = Input.GetKeyDown(altKey) || Input.GetMouseButtonDown(0);
        if (!press) return;

        // When near gates, always toggle (don't require perfect ray hit)
        if (near)
        {
            ToggleAll();
            return;
        }

        // Raycast fallback (center or mouse)
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Cursor.lockState != CursorLockMode.Locked)
            ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, ~0, QueryTriggerInteraction.Collide))
        {
            if (hit.collider.GetComponentInParent<GateDoor>() != null
                || hit.collider.GetComponentInParent<GateClickTarget>() != null
                || hit.collider.name.ToLowerInvariant().Contains("door")
                || hit.collider.name.ToLowerInvariant().Contains("gate")
                || hit.collider.name.ToLowerInvariant().Contains("hinge"))
            {
                ToggleAll();
            }
        }
    }

    bool IsNearGates(out float dist)
    {
        dist = 999f;
        Vector3 p = cam != null ? cam.transform.position : transform.position;
        var doors = Object.FindObjectsByType<GateDoor>(FindObjectsSortMode.None);
        if (doors.Length == 0)
        {
            // Distance to known gate Z
            dist = Mathf.Abs(p.z - (-20.5f));
            return dist < 18f && Mathf.Abs(p.x) < 8f;
        }
        foreach (var d in doors)
        {
            if (d == null) continue;
            float dd = Vector3.Distance(p, d.transform.position);
            if (dd < dist) dist = dd;
        }
        return dist < 18f;
    }

    void ToggleAll()
    {
        GateRuntimeSetup.WireNow();
        var doors = Object.FindObjectsByType<GateDoor>(FindObjectsSortMode.None);
        if (doors.Length == 0)
        {
            Debug.LogWarning("GateClick: no GateDoor components");
            return;
        }

        bool shouldOpen = true;
        foreach (var d in doors)
            if (d != null && d.WantOpen) { shouldOpen = false; break; }

        foreach (var d in doors)
        {
            if (d == null) continue;
            if (shouldOpen) d.Open();
            else d.Close();
        }

        if (shouldOpen)
            GateUniverseVoice.PlayWelcome();

        Debug.Log($"GateClick: {(shouldOpen ? "OPEN" : "CLOSE")} x{doors.Length}");
    }

    void OnGUI() { }
}
