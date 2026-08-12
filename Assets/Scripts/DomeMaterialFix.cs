using UnityEngine;

/// <summary>
/// Fixes black dome/pendentive areas and washed-out vaults at play time.
/// </summary>
public class DomeMaterialFix : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        var cat = GameObject.Find("Cathedral");
        if (cat == null) return;
        var fix = cat.GetComponent<DomeMaterialFix>();
        if (fix == null) fix = cat.AddComponent<DomeMaterialFix>();
        fix.Apply();
    }

    public void Apply()
    {
        var domeTex = LoadTex("cathedral_dome_interior_vivid");
        var plafondTex = LoadTex("classical_painted_ceiling");
        var vaultTex = LoadTex("classical_vault_coffered");
        var frescoTex = LoadTex("arch_fresco_lunette_01");

        var domeMat = Make("RT_DomeFresco", Color.white, 0.08f, 0.4f, domeTex, 0.45f);
        var pendentMat = Make("RT_Pendentive", new Color(1f, 0.95f, 0.85f), 0.05f, 0.35f,
            domeTex != null ? domeTex : plafondTex, 0.25f);
        var drumInner = Make("RT_DrumInner", new Color(0.93f, 0.9f, 0.82f), 0.05f, 0.35f, null, 0.05f);
        var vaultMat = Make("RT_Vault", Color.white, 0.15f, 0.45f, vaultTex, 0.08f);
        var goldMat = Make("RT_DomeGold", new Color(1f, 0.78f, 0.28f), 0.9f, 0.75f, null, 0.2f);
        var drumFresco = Make("RT_DrumFresco", Color.white, 0.05f, 0.35f,
            frescoTex != null ? frescoTex : domeTex, 0.2f);

        int n = 0;
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            string name = r.gameObject.name.ToLowerInvariant();
            Material m = null;

            if (name.Contains("pendentive") && !name.Contains("frame"))
                m = pendentMat;
            else if (name.Contains("pendentive") && name.Contains("frame"))
                m = goldMat;
            else if (name.Contains("dome_fresco") || name == "dome"
                     || (name.StartsWith("dome") && !name.Contains("window") && !name.Contains("drum")
                         && !name.Contains("goldring") && !name.Contains("fill")))
                m = domeMat;
            else if (name.Contains("dome_drum_inner") || name.Contains("drum_inner"))
                m = drumInner;
            else if (name.Contains("dome_drum") || name.Contains("goldring") || name.Contains("drumstatue"))
                m = goldMat;
            else if (name.Contains("drumfresco"))
                m = drumFresco;
            else if (name.StartsWith("vault_") && !name.Contains("cofferframe") && !name.Contains("panel")
                     && !name.Contains("rosette") && !name.Contains("rosering"))
                m = vaultMat;
            else if (name.Contains("cornerplafond") && !name.Contains("frame"))
                m = Make("RT_Plafond", Color.white, 0.05f, 0.35f, plafondTex, 0.2f);

            if (m != null)
            {
                var arr = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                for (int i = 0; i < arr.Length; i++) arr[i] = m;
                r.sharedMaterials = arr;
                n++;
            }
        }
        Debug.Log($"DomeMaterialFix applied to {n} renderers");
    }

    static Texture2D LoadTex(string name)
    {
        var t = Resources.Load<Texture2D>("CathedralTextures/" + name);
        if (t == null) t = Resources.Load<Texture2D>(name);
        return t;
    }

    static Material Make(string name, Color color, float metallic, float gloss, Texture2D albedo, float emission)
    {
        var mat = new Material(Shader.Find("Standard"));
        mat.name = name;
        mat.color = color;
        if (albedo != null)
        {
            mat.mainTexture = albedo;
            mat.SetTexture("_MainTex", albedo);
        }
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Glossiness", gloss);
        if (emission > 0f)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * emission);
            if (albedo != null) mat.SetTexture("_EmissionMap", albedo);
        }
        return mat;
    }
}
