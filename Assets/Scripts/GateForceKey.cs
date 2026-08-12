using UnityEngine;

/// <summary>
/// Press F near the palace to force-toggle doors (debug / backup).
/// </summary>
public class GateForceKey : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        var go = new GameObject("GateForceKey");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<GateForceKey>();
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.F)) return;
        GateRuntimeSetup.WireNow();
        var doors = Object.FindObjectsByType<GateDoor>(FindObjectsSortMode.None);
        bool open = true;
        foreach (var d in doors)
            if (d != null && d.WantOpen) { open = false; break; }
        foreach (var d in doors)
        {
            if (d == null) continue;
            if (open) d.Open(); else d.Close();
        }
        if (open) GateUniverseVoice.PlayWelcome();
        Debug.Log($"GateForceKey F -> {(open ? "OPEN" : "CLOSE")} doors={doors.Length}");
    }
}
