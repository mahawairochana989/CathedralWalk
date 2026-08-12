using UnityEngine;

/// <summary>
/// Transparent glass bridge + scrolling purple neon stripes.
/// </summary>
public class BridgeNeonFX : MonoBehaviour
{
    public float neonSpeed = 2.8f;
    public float stripeCount = 22f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        var cat = GameObject.Find("Cathedral");
        if (cat == null) return;
        var fx = cat.GetComponent<BridgeNeonFX>();
        if (fx == null) fx = cat.AddComponent<BridgeNeonFX>();
        // After BridgeHalver shortens meshes
        fx.Invoke(nameof(Apply), 0.35f);
        fx.Invoke(nameof(Apply), 1.0f);
    }

    public void Apply()
    {
        var glassShader = Shader.Find("Cathedral/BridgeGlass");
        var neonShader = Shader.Find("Cathedral/BridgeNeonScroll");
        if (glassShader == null || neonShader == null)
        {
            Debug.LogWarning("BridgeNeonFX: shaders not found yet, retry next frame");
            Invoke(nameof(Apply), 0.2f);
            return;
        }

        var glassMat = new Material(glassShader);
        glassMat.name = "RT_BridgeGlass";
        glassMat.SetColor("_Color", new Color(0.45f, 0.25f, 0.85f, 0.18f));
        glassMat.SetColor("_Glow", new Color(0.75f, 0.25f, 1f, 1f));
        glassMat.SetFloat("_GlowStrength", 0.55f);

        var neonMat = new Material(neonShader);
        neonMat.name = "RT_BridgeNeon";
        neonMat.SetColor("_Color", new Color(0.75f, 0.15f, 1f, 1f));
        neonMat.SetColor("_CoreColor", new Color(1f, 0.75f, 1f, 1f));
        neonMat.SetFloat("_Speed", neonSpeed);
        neonMat.SetFloat("_StripeCount", 0.09f);
        neonMat.SetFloat("_StripeWidth", 0.2f);
        neonMat.SetFloat("_Emission", 5.5f);
        neonMat.SetFloat("_UseWorld", 1f);
        neonMat.SetFloat("_WorldAxis", 1f); // scroll along world Z (bridge length)

        var railMat = new Material(Shader.Find("Standard"));
        railMat.name = "RT_BridgeRail";
        railMat.color = new Color(0.55f, 0.55f, 0.65f, 0.55f);
        railMat.SetFloat("_Metallic", 0.85f);
        railMat.SetFloat("_Glossiness", 0.9f);
        SetTransparentStandard(railMat, new Color(0.6f, 0.6f, 0.75f, 0.45f));

        int glassN = 0, neonN = 0;
        // Remove old fallback ribbons
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name.StartsWith("BridgeNeonRibbon_"))
                Destroy(t.gameObject);
        }

        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            string n = r.gameObject.name.ToLowerInvariant();
            if (!n.Contains("bridge")) continue;
            if (n.Contains("proxy") || n.Contains("ribbon")) continue;

            if (n.Contains("glass") && !n.Contains("rail"))
            {
                r.sharedMaterial = glassMat;
                glassN++;
            }
            else if (n.Contains("purple") || n.Contains("neon") || n.Contains("stripe"))
            {
                r.sharedMaterial = neonMat;
                neonN++;
            }
            else if (n.Contains("rail"))
            {
                r.sharedMaterial = railMat;
            }
            else
            {
                r.sharedMaterial = glassMat;
                glassN++;
            }
        }

        // Always add bright scrolling ribbons on top of glass for clear neon motion
        SpawnFallbackNeonRibbons(neonMat);
        // Center stripe too
        SpawnCenterRibbon(neonMat);

        Debug.Log($"BridgeNeonFX: glass={glassN} neonMeshes={neonN}");
    }

    void SpawnCenterRibbon(Material neonMat)
    {
        Renderer glass = FindGlass();
        if (glass == null) return;
        Bounds b = glass.bounds;
        bool longZ = b.size.z >= b.size.x;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "BridgeNeonRibbon_C";
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(transform, true);
        if (longZ)
        {
            go.transform.position = new Vector3(b.center.x, b.max.y + 0.03f, b.center.z);
            go.transform.localScale = new Vector3(0.28f, 0.05f, b.size.z * 0.98f);
        }
        else
        {
            go.transform.position = new Vector3(b.center.x, b.max.y + 0.03f, b.center.z);
            go.transform.localScale = new Vector3(b.size.x * 0.98f, 0.05f, 0.28f);
        }
        var mat = new Material(neonMat);
        mat.SetFloat("_StripeCount", 0.07f);
        mat.SetFloat("_Speed", neonSpeed * 1.15f);
        mat.SetFloat("_UseWorld", 1f);
        mat.SetFloat("_WorldAxis", longZ ? 1f : 0f);
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    Renderer FindGlass()
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            string n = r.gameObject.name.ToLowerInvariant();
            if (n.Contains("bridgeglass") || (n.Contains("bridge") && n.Contains("glass")))
                return r;
        }
        foreach (var r in GetComponentsInChildren<Renderer>(true))
            if (r.gameObject.name.ToLowerInvariant().Contains("bridge") && !r.gameObject.name.ToLowerInvariant().Contains("ribbon"))
                return r;
        return null;
    }

    void SpawnFallbackNeonRibbons(Material neonMat)
    {
        Renderer glass = FindGlass();
        if (glass == null) return;

        Bounds b = glass.bounds;
        bool longZ = b.size.z >= b.size.x;
        for (int i = -1; i <= 1; i += 2)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "BridgeNeonRibbon_" + i;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, true);
            if (longZ)
            {
                go.transform.position = new Vector3(b.center.x + i * Mathf.Max(b.extents.x, 1.2f) * 0.55f, b.max.y + 0.025f, b.center.z);
                go.transform.localScale = new Vector3(0.1f, 0.035f, b.size.z * 0.98f);
            }
            else
            {
                go.transform.position = new Vector3(b.center.x, b.max.y + 0.025f, b.center.z + i * Mathf.Max(b.extents.z, 1.2f) * 0.55f);
                go.transform.localScale = new Vector3(b.size.x * 0.98f, 0.035f, 0.1f);
            }
            var mat = new Material(neonMat);
            mat.SetFloat("_UseWorld", 1f);
            mat.SetFloat("_WorldAxis", longZ ? 1f : 0f);
            mat.SetFloat("_Speed", neonSpeed * (i < 0 ? 0.85f : 1.2f));
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }

    static void SetTransparentStandard(Material mat, Color c)
    {
        mat.SetFloat("_Mode", 3f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        mat.color = c;
    }
}
