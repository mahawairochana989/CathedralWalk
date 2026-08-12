using UnityEngine;

/// <summary>
/// Auto-fixes "Particle Velocity curves must all be in the same mode" on gold pollen.
/// </summary>
[DefaultExecutionOrder(-100)]
public class GoldPollenVelocityFix : MonoBehaviour
{
    void Awake() => Fix();
    void OnEnable() => Fix();

    [ContextMenu("Fix Velocity Modes")]
    public void Fix()
    {
        var ps = GetComponent<ParticleSystem>();
        if (ps == null) ps = GetComponentInChildren<ParticleSystem>();
        if (ps == null) return;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.x = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);
        vel.y = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void FixInScene()
    {
        var go = GameObject.Find("FX_GoldPollenUnity");
        if (go == null) return;
        var fix = go.GetComponent<GoldPollenVelocityFix>();
        if (fix == null) fix = go.AddComponent<GoldPollenVelocityFix>();
        fix.Fix();
    }
}
