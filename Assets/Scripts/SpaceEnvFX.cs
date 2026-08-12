using UnityEngine;

/// <summary>
/// Full-scene space backdrop like Blender SpaceEnv (skydome + planet + loop anim).
/// </summary>
public class SpaceEnvFX : MonoBehaviour
{
    public float skydomeSpinDegPerSec = 2.1f;   // ~25° / 5s like Blender
    public float planetSpinDegPerSec = 72f;     // full turn ~5s
    public float planetBobAmp = 2f;
    public float planetBobSpeed = 1.25f;
    public float planetPulseAmp = 0.04f;

    Transform skydome;
    Transform planet;
    Vector3 planetBasePos;
    Vector3 planetBaseScale;
    Material skyMat;
    Material planetMat;
    float t;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        var go = GameObject.Find("SpaceEnvFX");
        if (go == null) go = new GameObject("SpaceEnvFX");
        var fx = go.GetComponent<SpaceEnvFX>();
        if (fx == null) fx = go.AddComponent<SpaceEnvFX>();
        fx.Invoke(nameof(Setup), 0.2f);
        fx.Invoke(nameof(Setup), 0.8f);
    }

    public void Setup()
    {
        EnsureCameraDrawsSpace();
        EnsureSkydome();
        EnsurePlanet();
        ApplyMaterials();
        Debug.Log($"SpaceEnvFX ready sky={skydome} planet={planet}");
    }

    void EnsureCameraDrawsSpace()
    {
        foreach (var cam in Camera.allCameras)
        {
            if (cam == null) continue;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.0f, 0.04f);
            cam.farClipPlane = Mathf.Max(cam.farClipPlane, 2000f);
        }
        var main = Camera.main;
        if (main != null)
        {
            main.clearFlags = CameraClearFlags.SolidColor;
            main.backgroundColor = new Color(0.01f, 0.0f, 0.04f);
            main.farClipPlane = Mathf.Max(main.farClipPlane, 2000f);
        }
    }

    void EnsureSkydome()
    {
        skydome = FindByName("spaceenv_skydome", "skydome");
        if (skydome == null)
        {
            // Higher-res sphere reduces faceting; shader also uses direction UVs (seamless)
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "SpaceEnv_Skydome";
            Object.Destroy(sphere.GetComponent<Collider>());
            sphere.transform.position = new Vector3(0f, 8f, 0f);
            sphere.transform.localScale = new Vector3(-920f, -620f, -920f); // negative = normals inward
            skydome = sphere.transform;
        }
        else
        {
            var r = skydome.GetComponent<Renderer>();
            if (r != null)
            {
                Bounds b = r.bounds;
                if (b.size.x < 200f)
                    skydome.localScale = skydome.localScale * (900f / Mathf.Max(b.size.x, 1f));
            }
            // Prefer viewing inner surface
            var sc = skydome.localScale;
            if (sc.x > 0f) skydome.localScale = new Vector3(-Mathf.Abs(sc.x), -Mathf.Abs(sc.y), -Mathf.Abs(sc.z));
        }
    }

    void EnsurePlanet()
    {
        planet = FindByName("spaceenv_planet", "planetbillboard", "planet");
        // Blender (0, 320, 40) → Unity ~ (0, 40, -320)
        Vector3 wantPos = new Vector3(0f, 45f, -340f);

        if (planet == null)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "SpaceEnv_PlanetBillboard";
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.position = wantPos;
            quad.transform.localScale = Vector3.one * 160f;
            // Face toward temple / bridge (toward +Z / origin)
            quad.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            planet = quad.transform;
        }

        planetBasePos = planet.position;
        // If planet is near origin (import fail), move to backdrop
        if (planetBasePos.magnitude < 50f || Mathf.Abs(planetBasePos.z) < 80f)
        {
            planet.position = wantPos;
            planetBasePos = wantPos;
            if (planet.localScale.magnitude < 10f)
                planet.localScale = Vector3.one * 160f;
        }
        planetBaseScale = planet.localScale;
    }

    void ApplyMaterials()
    {
        var tex = Resources.Load<Texture2D>("CathedralTextures/space_planet_bg");
        var skyShader = Shader.Find("Cathedral/SpaceBackdrop");
        var planetShader = Shader.Find("Cathedral/SpacePlanet");
        if (skyShader == null || planetShader == null)
        {
            Debug.LogWarning("SpaceEnvFX: shaders missing, retry");
            return;
        }

        skyMat = new Material(skyShader);
        skyMat.name = "RT_SpaceSky";
        if (tex != null) skyMat.mainTexture = tex;
        skyMat.SetColor("_Tint", new Color(0.4f, 0.18f, 0.65f, 1f));
        skyMat.SetFloat("_Emission", 1.5f);
        skyMat.SetFloat("_Scroll", 0.015f);
        skyMat.SetFloat("_StarAmount", 0.8f);
        skyMat.SetFloat("_Pulse", 0.22f);

        planetMat = new Material(planetShader);
        planetMat.name = "RT_SpacePlanet";
        if (tex != null) planetMat.mainTexture = tex;
        planetMat.SetColor("_Tint", new Color(1f, 0.8f, 1f, 1f));
        planetMat.SetFloat("_Emission", 2.0f);
        planetMat.SetFloat("_Pulse", 0.3f);

        if (skydome != null)
        {
            foreach (var r in skydome.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterial = skyMat;
        }
        if (planet != null)
        {
            foreach (var spin in planet.GetComponentsInChildren<SpinY>(true))
                Object.Destroy(spin);
            foreach (var r in planet.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterial = planetMat;
        }

        // Also fix any other spaceenv renderers under Cathedral
        var cat = GameObject.Find("Cathedral");
        if (cat != null)
        {
            foreach (var r in cat.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.gameObject.name.ToLowerInvariant();
                if (n.Contains("skydome") || (n.Contains("spaceenv") && n.Contains("dome")))
                    r.sharedMaterial = skyMat;
                else if (n.Contains("planet"))
                    r.sharedMaterial = planetMat;
            }
        }
    }

    void Update()
    {
        t += Time.deltaTime;

        if (skydome != null)
            skydome.Rotate(0f, skydomeSpinDegPerSec * Time.deltaTime, 0f, Space.World);

        if (planet != null)
        {
            planet.Rotate(0f, planetSpinDegPerSec * Time.deltaTime, 0f, Space.World);
            float bob = Mathf.Sin(t * planetBobSpeed) * planetBobAmp;
            planet.position = planetBasePos + Vector3.up * bob;
            float pulse = 1f + Mathf.Sin(t * 2.2f) * planetPulseAmp;
            planet.localScale = planetBaseScale * pulse;

            // Keep facing the temple area roughly
            Vector3 toCenter = (Vector3.zero - planet.position);
            toCenter.y = 0f;
            if (toCenter.sqrMagnitude > 0.01f)
            {
                // Billboard: face camera if available
                var cam = Camera.main;
                if (cam != null)
                {
                    Vector3 look = planet.position - cam.transform.position;
                    if (look.sqrMagnitude > 0.01f)
                        planet.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
                }
            }
        }

        if (skyMat != null)
            skyMat.SetFloat("_Emission", 1.35f + Mathf.Sin(t * 1.7f) * 0.35f);
        if (planetMat != null)
            planetMat.SetFloat("_Emission", 1.7f + Mathf.Sin(t * 2.2f) * 0.5f);
    }

    static Transform FindByName(params string[] tokens)
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            string n = t.name.ToLowerInvariant();
            foreach (var token in tokens)
            {
                if (n.Contains(token))
                    return t;
            }
        }
        return null;
    }
}
