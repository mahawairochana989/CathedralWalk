using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public static class CathedralWalkSetup
{
    const string ScenePath = "Assets/Scenes/CathedralWalk.unity";

    [MenuItem("Cathedral/Setup Walking Scene")]
    public static void SetupFromMenu() => Setup();

    public static void SetupBatch()
    {
        Setup();
        EditorApplication.Exit(0);
    }

    public static void Setup()
    {
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Materials/Generated");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var camGo = GameObject.Find("Main Camera");
        if (camGo != null) Object.DestroyImmediate(camGo);

        // Prefer GLB (better materials), fallback FBX
        string[] modelCandidates =
        {
            "Assets/Models/Cathedral.glb",
            "Assets/Models/Cathedral.gltf",
            "Assets/Models/Cathedral.fbx"
        };

        GameObject model = null;
        string usedPath = null;
        foreach (var p in modelCandidates)
        {
            model = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (model != null) { usedPath = p; break; }
        }

        GameObject cathedral = null;
        if (model != null)
        {
            cathedral = (GameObject)PrefabUtility.InstantiatePrefab(model);
            if (cathedral == null)
                cathedral = Object.Instantiate(model);
            cathedral.name = "Cathedral";
            cathedral.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            cathedral.transform.localScale = Vector3.one;
            Debug.Log("Instanced cathedral from " + usedPath);

            FixMaterials(cathedral);
            AddColliders(cathedral);
            SetupBridgeWalkway(cathedral);
            ShortenBridge(cathedral, 0.5f);
            SetupGates(cathedral);
            AddSpinners(cathedral);
            AddGoldPollen(cathedral);
        }
        else
        {
            Debug.LogError("No Cathedral.glb/fbx found in Assets/Models");
        }

        SetupLighting();

        // Player — start at far end of bridge, facing the temple
        Vector3 spawnPos = new Vector3(0f, 1.8f, 460f);
        Quaternion spawnRot = Quaternion.LookRotation(Vector3.back, Vector3.up);
        if (cathedral != null)
            ComputeBridgeSpawn(cathedral, out spawnPos, out spawnRot);

        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.SetPositionAndRotation(spawnPos, spawnRot);
        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.stepOffset = 0.4f;
        cc.slopeLimit = 50f;

        var camObj = new GameObject("PlayerCamera");
        camObj.transform.SetParent(player.transform);
        camObj.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        var cam = camObj.AddComponent<Camera>();
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 1200f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.01f, 0.0f, 0.03f);
        camObj.AddComponent<AudioListener>();
        camObj.AddComponent<GateClickInteractor>();

        var fps = player.AddComponent<FirstPersonController>();
        fps.cameraPivot = camObj.transform;

        Debug.Log($"Player spawn at {spawnPos}, facing temple");

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("CathedralWalk scene ready: " + ScenePath);
        EditorSceneManager.OpenScene(ScenePath);
    }

    static void SetupLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.25f, 0.28f, 0.4f);
        RenderSettings.ambientEquatorColor = new Color(0.45f, 0.38f, 0.28f);
        RenderSettings.ambientGroundColor = new Color(0.12f, 0.1f, 0.08f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.08f, 0.06f, 0.1f);
        RenderSettings.fogDensity = 0.008f;

        var sun = GameObject.Find("Directional Light");
        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Euler(42f, -25f, 0f);
            var light = sun.GetComponent<Light>();
            light.color = new Color(1f, 0.92f, 0.78f);
            light.intensity = 0.85f;
            light.shadows = LightShadows.Soft;
        }

        CreatePoint("NaveFill", new Vector3(0f, 10f, 0f), new Color(1f, 0.88f, 0.6f), 3.2f, 55f);
        CreatePoint("AltarWarm", new Vector3(0f, 7f, -18f), new Color(1f, 0.75f, 0.45f), 2.4f, 30f);
        CreatePoint("EntranceFill", new Vector3(0f, 6f, 18f), new Color(0.7f, 0.8f, 1f), 1.8f, 28f);
        CreatePoint("AthleteGlow", new Vector3(0f, 4f, 0f), new Color(1f, 0.35f, 0.15f), 1.6f, 12f);
    }

    static void CreatePoint(string name, Vector3 pos, Color color, float intensity, float range)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
    }

    static Texture2D Tex(string file)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/" + file);
    }

    static Material MakeMat(string name, Color color, float metallic, float gloss, Texture2D albedo = null, bool cutout = false, float emission = 0f)
    {
        string path = "Assets/Materials/Generated/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = Shader.Find("Standard");
        mat.color = color;
        if (albedo != null)
        {
            mat.mainTexture = albedo;
            mat.SetTexture("_MainTex", albedo);
        }
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Glossiness", gloss);
        if (cutout)
        {
            mat.SetFloat("_Mode", 1f); // Cutout
            mat.SetOverrideTag("RenderType", "TransparentCutout");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            mat.SetInt("_ZWrite", 1);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 2450;
            mat.SetFloat("_Cutoff", 0.35f);
        }
        if (emission > 0f)
        {
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mat.SetColor("_EmissionColor", color * emission);
            if (albedo != null) mat.SetTexture("_EmissionMap", albedo);
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void FixMaterials(GameObject root)
    {
        var wall = MakeMat("WallMarble", Color.white, 0.35f, 0.55f, Tex("temple_wall_gold_marble_malachite.png"));
        var floor = MakeMat("FloorMarble", Color.white, 0.25f, 0.65f, Tex("temple_blue_gold_marble_floor.png"));
        var dome = MakeMat("DomeFresco", Color.white, 0.1f, 0.4f, Tex("cathedral_dome_interior_vivid.png"), false, 0.35f);
        var plafond = MakeMat("PlafondPaint", Color.white, 0.05f, 0.35f, Tex("classical_painted_ceiling.png"), false, 0.15f);
        var vault = MakeMat("VaultCoffer", Color.white, 0.2f, 0.45f, Tex("classical_vault_coffered.png"));
        var altar = MakeMat("AltarArch", Color.white, 0.55f, 0.7f, Tex("altar_golden_arch.png"), false, 0.2f);
        var fresco = MakeMat("ArchFresco", Color.white, 0.05f, 0.3f, Tex("arch_fresco_lunette_01.png"), false, 0.1f);
        var logo = MakeMat("SpinLogo", Color.white, 0f, 0.2f, Tex("logo_spin_clean.png"), true, 0.8f);
        var space = MakeMat("SpaceSky", Color.white, 0f, 0f, Tex("space_planet_bg.png"), false, 0.5f);
        var gold = MakeMat("GoldMetal", new Color(1f, 0.78f, 0.28f), 0.95f, 0.82f, null, false, 0.15f);
        var stone = MakeMat("ColumnStone", new Color(0.92f, 0.9f, 0.84f), 0.05f, 0.35f);
        var malachite = MakeMat("Malachite", new Color(0.1f, 0.45f, 0.28f), 0.4f, 0.55f);
        var lapis = MakeMat("Lapis", new Color(0.15f, 0.28f, 0.65f), 0.35f, 0.5f);
        var dark = MakeMat("DarkTrim", new Color(0.12f, 0.1f, 0.08f), 0.2f, 0.4f);

        int fixedCount = 0;
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            string n = r.gameObject.name.ToLowerInvariant();
            Material m = null;

            if (n.Contains("spinlogo") || n.Contains("logo2d")) m = logo;
            else if (n.Contains("spaceenv") || n.Contains("skydome") || n.Contains("planet")) m = space;
            else if (n.Contains("floor") || n.Contains("medallion") || n.Contains("step")) m = floor;
            else if (n.Contains("wall_")) m = wall;
            else if (n.Contains("dome_fresco") || n.Contains("drumfresco") || (n.StartsWith("dome") && !n.Contains("window") && !n.Contains("drum")))
                m = dome;
            else if (n.Contains("pendentive") && !n.Contains("frame"))
                m = dome; // sails under the cupola — same fresco family
            else if (n.Contains("pendentive") && n.Contains("frame"))
                m = gold;
            else if (n.Contains("dome_drum_inner"))
                m = stone;
            else if (n.Contains("dome_drum") || n.Contains("goldring") || n.Contains("drumstatue"))
                m = gold;
            else if (n.Contains("cornerplafond") && !n.Contains("frame")) m = plafond;
            else if (n.Contains("vault_") && !n.Contains("cofferframe")) m = vault;
            else if (n.Contains("archfresco")) m = fresco;
            else if (n.Contains("altararch") || n.Contains("altar_arch")) m = altar;
            else if (n.Contains("malachite")) m = malachite;
            else if (n.Contains("lapis")) m = lapis;
            else if (n.Contains("capital") || n.Contains("goldenathlete") || n.Contains("goldencircle")
                     || n.Contains("entrance_gold") || n.Contains("goldring") || n.Contains("gateframe"))
                m = gold;
            else if (n.Contains("pillar") || n.Contains("column") || n.Contains("cylinder")
                     || n.Contains("crossing") || n.Contains("drum") || n.Contains("plinth"))
                m = stone;
            else if (n.Contains("cofferframe") || n.Contains("frame"))
                m = gold;

            if (m != null)
            {
                var arr = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                for (int i = 0; i < arr.Length; i++) arr[i] = m;
                r.sharedMaterials = arr;
                fixedCount++;
            }
        }
        Debug.Log("Fixed materials on renderers: " + fixedCount);
        AssetDatabase.SaveAssets();
    }

    static void AddColliders(GameObject root)
    {
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            string n = mf.gameObject.name.ToLowerInvariant();
            bool forceKeep = n.Contains("bridge") || n.Contains("door") || n.Contains("gateframe")
                             || n.Contains("floor") || n.Contains("wall_");
            if (!forceKeep && (n.Contains("capital") || n.Contains("goldenathlete") || n.Contains("spaceenv")
                || n.Contains("spinlogo") || n.Contains("logo2d") || n.Contains("planet")
                || n.Contains("pollen") || n.Contains("cofferframe") || n.Contains("leaf")
                || n.Contains("neon") || n.Contains("stripe")))
                continue;

            // Skip extremely dense meshes (except forced)
            if (!forceKeep && mf.sharedMesh.triangles != null && mf.sharedMesh.triangles.Length / 3 > 200000)
                continue;

            if (mf.GetComponent<Collider>() != null) continue;
            var mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            mc.convex = false;
        }

        // Simple floor box safety collider (temple interior)
        if (GameObject.Find("FloorWalkProxy") == null)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "FloorWalkProxy";
            floor.transform.position = new Vector3(0f, -0.05f, 0f);
            floor.transform.localScale = new Vector3(80f, 0.1f, 80f);
            Object.DestroyImmediate(floor.GetComponent<MeshRenderer>());
        }
    }

    static Transform FindByNameContains(GameObject root, string token)
    {
        token = token.ToLowerInvariant();
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.ToLowerInvariant().Contains(token))
                return t;
        }
        return null;
    }

    static void ComputeBridgeSpawn(GameObject cathedral, out Vector3 spawnPos, out Quaternion spawnRot)
    {
        spawnPos = new Vector3(0f, 1.8f, 460f);
        spawnRot = Quaternion.LookRotation(Vector3.back, Vector3.up);

        var bridge = FindByNameContains(cathedral, "bridgeglass");
        if (bridge == null) bridge = FindByNameContains(cathedral, "bridge");
        var door = FindByNameContains(cathedral, "fx_door_l");
        if (door == null) door = FindByNameContains(cathedral, "door_l");

        Renderer bridgeRend = bridge != null ? bridge.GetComponentInChildren<Renderer>() : null;
        if (bridgeRend == null)
        {
            Debug.LogWarning("Bridge not found — using fallback spawn");
            return;
        }

        Bounds bb = bridgeRend.bounds;
        Vector3 templeAim = door != null ? door.position : Vector3.zero;
        Vector3 flatCenter = new Vector3(bb.center.x, 0f, bb.center.z);
        Vector3 flatTemple = new Vector3(templeAim.x, 0f, templeAim.z);
        Vector3 toTemple = (flatTemple - flatCenter);
        if (toTemple.sqrMagnitude < 0.01f)
            toTemple = Vector3.back;
        toTemple.Normalize();

        // Far end of bridge = opposite of temple direction
        float halfLen = Mathf.Max(bb.extents.x, bb.extents.z);
        // Prefer axis-aligned extent along toTemple
        Vector3 ext = bb.extents;
        halfLen = Mathf.Abs(toTemple.x) > Mathf.Abs(toTemple.z) ? ext.x : ext.z;

        Vector3 far = bb.center - toTemple * (halfLen - 2.5f);
        // Keep centered on bridge width (door aim can bias X)
        far.x = bb.center.x;
        if (Mathf.Abs(toTemple.x) > Mathf.Abs(toTemple.z))
            far.z = bb.center.z;
        spawnPos = new Vector3(far.x, bb.max.y + 1.75f, far.z);
        spawnRot = Quaternion.LookRotation(toTemple, Vector3.up);
        Debug.Log($"Bridge bounds {bb.center} size {bb.size}; spawn {spawnPos}; dir {toTemple}");
    }

    static void SetupBridgeWalkway(GameObject cathedral)
    {
        var bridge = FindByNameContains(cathedral, "bridgeglass");
        if (bridge == null) bridge = FindByNameContains(cathedral, "bridge");
        if (bridge == null) return;
        var rend = bridge.GetComponentInChildren<Renderer>();
        if (rend == null) return;
        Bounds bb = rend.bounds;

        RebuildBridgeWalkProxy(bb);

        // Bridge glass material — translucent purple-ish
        var glass = MakeMat("BridgeGlass", new Color(0.55f, 0.35f, 0.95f, 0.35f), 0.1f, 0.9f, null, false, 0.4f);
        // Approximate transparent mode
        glass.SetFloat("_Mode", 3f);
        glass.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        glass.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        glass.SetInt("_ZWrite", 0);
        glass.DisableKeyword("_ALPHATEST_ON");
        glass.EnableKeyword("_ALPHABLEND_ON");
        glass.renderQueue = 3000;
        glass.color = new Color(0.55f, 0.35f, 0.95f, 0.4f);
        foreach (var r in bridge.GetComponentsInChildren<Renderer>(true))
            r.sharedMaterial = glass;

        var neon = MakeMat("BridgeNeon", new Color(0.7f, 0.2f, 1f), 0.2f, 0.8f, null, false, 2.5f);
        foreach (var t in cathedral.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name.ToLowerInvariant();
            if (n.Contains("bridgepurple") || n.Contains("bridgeneon") || n.Contains("doorneon"))
            {
                var r = t.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = neon;
            }
        }
    }

    static void RebuildBridgeWalkProxy(Bounds bb)
    {
        var old = GameObject.Find("BridgeWalkProxy");
        if (old != null) Object.DestroyImmediate(old);

        var proxy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        proxy.name = "BridgeWalkProxy";
        Object.DestroyImmediate(proxy.GetComponent<MeshRenderer>());
        proxy.transform.position = new Vector3(bb.center.x, bb.min.y - 0.05f, bb.center.z);
        float len = Mathf.Max(bb.size.x, bb.size.z);
        float width = Mathf.Min(bb.size.x, bb.size.z);
        bool longZ = bb.size.z >= bb.size.x;
        proxy.transform.localScale = longZ
            ? new Vector3(Mathf.Max(width, 2.5f), 0.2f, len + 2f)
            : new Vector3(len + 2f, 0.2f, Mathf.Max(width, 2.5f));
    }

    /// <summary>
    /// Scale bridge length toward the temple end (keeps entrance attached).
    /// </summary>
    public static void ShortenBridge(GameObject cathedral, float factor)
    {
        if (cathedral == null) return;
        if (GameObject.Find("BridgeHalvedMarker") != null)
        {
            Debug.Log("Bridge already shortened — skip");
            return;
        }

        var door = FindByNameContains(cathedral, "fx_door_l");
        if (door == null) door = FindByNameContains(cathedral, "door_l");

        var pieces = new System.Collections.Generic.List<Transform>();
        foreach (var t in cathedral.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name.ToLowerInvariant();
            if (!n.Contains("bridge")) continue;
            if (t.GetComponent<Renderer>() == null && t.GetComponent<MeshFilter>() == null)
                continue;
            pieces.Add(t);
        }
        if (pieces.Count == 0)
        {
            Debug.LogWarning("No bridge pieces found to shorten");
            return;
        }

        Bounds bb = new Bounds(pieces[0].position, Vector3.zero);
        bool init = false;
        foreach (var t in pieces)
        {
            var r = t.GetComponentInChildren<Renderer>();
            if (r == null) continue;
            if (!init) { bb = r.bounds; init = true; }
            else bb.Encapsulate(r.bounds);
        }
        if (!init)
        {
            Debug.LogWarning("Bridge has no renderers");
            return;
        }

        Vector3 doorPos = door != null ? door.position : Vector3.zero;
        float templeZ = Mathf.Abs(bb.max.z - doorPos.z) <= Mathf.Abs(bb.min.z - doorPos.z)
            ? bb.max.z : bb.min.z;
        Vector3 pivot = new Vector3(bb.center.x, bb.center.y, templeZ);

        var root = new GameObject("BridgeScaleRoot_tmp");
        root.transform.position = pivot;
        root.transform.rotation = Quaternion.identity;

        var parents = new System.Collections.Generic.Dictionary<Transform, Transform>();
        foreach (var t in pieces)
        {
            parents[t] = t.parent;
            t.SetParent(root.transform, true);
        }

        // Length is along world Z for this cathedral export
        root.transform.localScale = new Vector3(1f, 1f, factor);

        foreach (var t in pieces)
            t.SetParent(parents[t], true);
        Object.DestroyImmediate(root);

        // New bounds + walk proxy
        init = false;
        foreach (var t in pieces)
        {
            var r = t.GetComponentInChildren<Renderer>();
            if (r == null) continue;
            if (!init) { bb = r.bounds; init = true; }
            else bb.Encapsulate(r.bounds);
        }
        if (init) RebuildBridgeWalkProxy(bb);

        var marker = new GameObject("BridgeHalvedMarker");
        marker.hideFlags = HideFlags.None;

        // Move player if present
        var player = GameObject.Find("Player");
        if (player != null && init)
        {
            ComputeBridgeSpawn(cathedral, out var spawnPos, out var spawnRot);
            player.transform.SetPositionAndRotation(spawnPos, spawnRot);
        }

        Debug.Log($"Bridge shortened x{factor}. New bounds center={bb.center} size={bb.size}");
    }

    [MenuItem("Cathedral/Shorten Bridge x0.5")]
    public static void ShortenBridgeMenu()
    {
        var cathedral = GameObject.Find("Cathedral");
        if (cathedral == null)
        {
            Debug.LogError("Cathedral not found in open scene");
            return;
        }
        // Allow re-run: remove marker if user wants — but menu uses one-shot. Remove marker first for force.
        var marker = GameObject.Find("BridgeHalvedMarker");
        if (marker != null) Object.DestroyImmediate(marker);

        ShortenBridge(cathedral, 0.5f);
        EditorUtility.SetDirty(cathedral);
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log("Bridge shortened and scene saved");
    }

    static void SetupGates(GameObject cathedral)
    {
        // Remove old hinge helpers if re-running
        var old = GameObject.Find("GateHinges");
        if (old != null) Object.DestroyImmediate(old);

        var hinges = new GameObject("GateHinges");
        Transform left = FindByNameContains(cathedral, "fx_door_l");
        if (left == null) left = FindByNameContains(cathedral, "door_l");
        Transform right = FindByNameContains(cathedral, "fx_door_r");
        if (right == null) right = FindByNameContains(cathedral, "door_r");
        Transform neonL = FindByNameContains(cathedral, "doorneon_l");
        Transform neonR = FindByNameContains(cathedral, "doorneon_r");

        if (left == null && right == null)
        {
            Debug.LogWarning("Temple doors not found in model");
            return;
        }

        // Hinge pivots at outer edges of door leaves (away from center)
        if (left != null)
            CreateHingedDoor(left, neonL, hinges.transform, isLeft: true);
        if (right != null)
            CreateHingedDoor(right, neonR, hinges.transform, isLeft: false);

        Debug.Log("Gates wired for click-to-open (LMB / E)");
    }

    static void CreateHingedDoor(Transform door, Transform neon, Transform hingesParent, bool isLeft)
    {
        var rend = door.GetComponentInChildren<Renderer>();
        Bounds b = rend != null ? rend.bounds : new Bounds(door.position, new Vector3(3.5f, 10f, 0.4f));

        // Outer hinge: left leaf (+X side), right leaf (-X side) in typical layout
        float hingeX = isLeft ? b.max.x : b.min.x;
        Vector3 hingePos = new Vector3(hingeX, b.min.y, b.center.z);

        var pivot = new GameObject(isLeft ? "Hinge_L" : "Hinge_R");
        pivot.transform.SetParent(hingesParent, false);
        pivot.transform.position = hingePos;
        pivot.transform.rotation = Quaternion.identity;

        // Reparent door (and neon) under pivot keeping world pose
        door.SetParent(pivot.transform, true);
        if (neon != null) neon.SetParent(pivot.transform, true);

        // Ensure clickable collider on door
        if (door.GetComponentInChildren<Collider>() == null)
        {
            var box = door.gameObject.AddComponent<BoxCollider>();
            if (rend != null)
            {
                box.center = door.InverseTransformPoint(b.center);
                box.size = b.size;
            }
            else box.size = new Vector3(3.5f, 10f, 0.5f);
        }

        var gate = pivot.AddComponent<GateDoor>();
        gate.isLeftLeaf = isLeft;
        gate.openAngle = 110f;
        gate.openDuration = 2.3f;

        // Gold-ish door material
        var doorMat = MakeMat(isLeft ? "DoorGold_L" : "DoorGold_R", new Color(0.85f, 0.65f, 0.2f), 0.9f, 0.75f, null, false, 0.25f);
        foreach (var r in door.GetComponentsInChildren<Renderer>(true))
            r.sharedMaterial = doorMat;
    }

    static void AddSpinners(GameObject root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name.ToLowerInvariant();
            if (n.Contains("goldenathlete") || n.Contains("athletepivot"))
            {
                if (t.GetComponent<SpinY>() == null) t.gameObject.AddComponent<SpinY>();
            }
            if (n.Contains("spinlogo") || n.Contains("logo2d"))
            {
                if (t.GetComponent<SpinLogo>() == null) t.gameObject.AddComponent<SpinLogo>();
            }
            if (n.Contains("planet"))
            {
                if (t.GetComponent<SpinY>() == null)
                {
                    var s = t.gameObject.AddComponent<SpinY>();
                    s.degreesPerSecond = 4f;
                }
            }
        }
    }

    static void AddGoldPollen(GameObject root)
    {
        if (GameObject.Find("FX_GoldPollenUnity") != null) return;
        var go = new GameObject("FX_GoldPollenUnity");
        go.transform.position = new Vector3(0f, 6f, 0f);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 12f;
        main.startSize = 0.04f;
        main.startColor = new Color(1f, 0.85f, 0.35f, 0.85f);
        main.maxParticles = 4000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission;
        emission.rateOverTime = 180f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(28f, 10f, 28f);
        ConfigurePollenVelocity(ps);
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = MakeMat("PollenGold", new Color(1f, 0.85f, 0.3f), 0.8f, 0.7f, null, false, 1.2f);
    }

    public static void ConfigurePollenVelocity(ParticleSystem ps)
    {
        if (ps == null) return;
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        // All axes must use the same MinMaxCurve mode
        vel.x = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);
        vel.y = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);
    }

    [MenuItem("Cathedral/Fix Gold Pollen Error")]
    public static void FixGoldPollenMenu()
    {
        var go = GameObject.Find("FX_GoldPollenUnity");
        if (go == null)
        {
            Debug.LogWarning("FX_GoldPollenUnity not found in open scene");
            return;
        }
        ConfigurePollenVelocity(go.GetComponent<ParticleSystem>());
        EditorUtility.SetDirty(go);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("Gold pollen velocity fixed");
    }
}
