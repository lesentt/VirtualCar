using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Virtual Vehicle 编辑器入口（唯一菜单 + Play 前自动清理）。
/// </summary>
[InitializeOnLoad]
public static class VirtualVehicleEditor
{
    const string PrefabRoot = "Assets/download/SimplePoly City - Low Poly Assets/Prefab";

    static VirtualVehicleEditor()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                int removed = CleanupVehicleMeshColliders(silent: true);
                if (removed > 0)
                    Debug.Log($"[VirtualVehicleEditor] Play 前清理车辆误加组件 {removed} 个。");
            }
        };
    }

    [MenuItem("Tools/Virtual Vehicle/Setup Current Scene")]
    public static void SetupCurrentScene()
    {
        if (!EditorUtility.DisplayDialog(
                "配置当前场景",
                "将执行：\n" +
                "1. 清理车辆误加碰撞体\n" +
                "2. 删除装饰用静态车辆（Vehicle_*）\n" +
                "3. 为环境补 Collider 与碰撞分类\n" +
                "4. 配置 Car 1 / Police 1\n\n是否继续？",
                "继续", "取消"))
            return;

        int cleaned = CleanupVehicleMeshColliders(silent: true);
        int removedCars = RemoveDecorativeVehiclesInEditor();
        int colliders = AddEnvironmentColliders();
        int vehicles = SetupPlayerVehicles();
        int env = CollisionSceneSetup.ConfigureEnvironment();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("完成",
            $"请 Ctrl+S 保存后 Play。\n\n" +
            $"清理车辆碰撞：{cleaned}\n" +
            $"删除装饰车：{removedCars}\n" +
            $"补环境 Collider：{colliders}\n" +
            $"可驾驶车辆：{vehicles}\n" +
            $"环境碰撞分类：{env}",
            "确定");
    }

    static int SetupPlayerVehicles()
    {
        int count = 0;
        foreach (CarDamageSystem damage in Object.FindObjectsOfType<CarDamageSystem>())
        {
            GameObject go = damage.gameObject;
            if (go.GetComponent<VehicleCollisionHandler>() == null)
                go.AddComponent<VehicleCollisionHandler>();

            go.layer = CollisionTypes.GetLayerIndex(CollisionTypes.LayerVehicle);

            if (damage.damageMultiplier <= 0f)
                damage.damageMultiplier = 0.1f;

            VehicleCollisionHandler handler = go.GetComponent<VehicleCollisionHandler>();
            if (handler != null)
            {
                handler.damageScale = 0.0025f;
                handler.knockbackScale = 0.35f;
                handler.minImpulse = 300f;
            }

            count++;
        }

        return count;
    }

    static int RemoveDecorativeVehiclesInEditor()
    {
        int removed = 0;
        foreach (GameObject go in Object.FindObjectsOfType<GameObject>())
        {
            if (!CollisionSceneSetup.IsDecorativeVehicle(go))
                continue;

            Undo.DestroyObjectImmediate(go);
            removed++;
        }

        return removed;
    }

    static int AddEnvironmentColliders()
    {
        int added = 0;
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabRoot == null) continue;

            GameObject temp = PrefabUtility.InstantiatePrefab(prefabRoot) as GameObject;
            if (temp == null) continue;

            int n = AddCollidersRecursive(temp);
            if (n > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(temp, path);
                added += n;
            }

            Object.DestroyImmediate(temp);
        }

        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            added += AddCollidersRecursive(root);

        AssetDatabase.SaveAssets();
        return added;
    }

    static int AddCollidersRecursive(GameObject go)
    {
        int added = 0;
        if (ShouldSkipColliderSetup(go))
            return 0;

        foreach (MeshFilter mf in go.GetComponents<MeshFilter>())
        {
            if (mf.sharedMesh == null || go.GetComponent<Collider>() != null)
                continue;

            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            mc.convex = false;
            added++;
        }

        foreach (Transform child in go.transform)
            added += AddCollidersRecursive(child.gameObject);

        return added;
    }

    static bool ShouldSkipColliderSetup(GameObject go)
    {
        if (CollisionTypes.IsPartOfPlayerVehicle(go)) return true;
        if (CollisionSceneSetup.IsDecorativeVehicle(go)) return true;
        if (go.GetComponent<Camera>() != null) return true;
        if (go.GetComponent<Light>() != null) return true;
        if (go.GetComponent<AudioSource>() != null) return true;
        if (go.GetComponent<ParticleSystem>() != null) return true;
        return false;
    }

    static int CleanupVehicleMeshColliders(bool silent)
    {
        int removed = 0;

        foreach (MeshCollider mc in Resources.FindObjectsOfTypeAll<MeshCollider>())
        {
            if (mc == null || !IsInLoadedScene(mc.gameObject)) continue;
            if (!CollisionTypes.IsPartOfPlayerVehicle(mc.gameObject)) continue;
            Undo.DestroyObjectImmediate(mc);
            removed++;
        }

        foreach (Rigidbody rb in Resources.FindObjectsOfTypeAll<Rigidbody>())
        {
            if (rb == null || !IsInLoadedScene(rb.gameObject)) continue;
            if (!CollisionTypes.IsPartOfPlayerVehicle(rb.gameObject)) continue;
            if (rb.gameObject.GetComponent<CarController>() != null) continue;
            Undo.DestroyObjectImmediate(rb);
            removed++;
        }

        foreach (CollisionProfile profile in Resources.FindObjectsOfTypeAll<CollisionProfile>())
        {
            if (profile == null || !IsInLoadedScene(profile.gameObject)) continue;
            if (!CollisionTypes.IsPartOfPlayerVehicle(profile.gameObject)) continue;
            if (profile.gameObject.GetComponent<CarController>() != null) continue;
            Undo.DestroyObjectImmediate(profile);
            removed++;
        }

        foreach (DestructibleProp prop in Resources.FindObjectsOfTypeAll<DestructibleProp>())
        {
            if (prop == null || !IsInLoadedScene(prop.gameObject)) continue;
            if (!CollisionTypes.IsPartOfPlayerVehicle(prop.gameObject)) continue;
            Undo.DestroyObjectImmediate(prop);
            removed++;
        }

        if (!silent && removed > 0)
            Debug.Log($"[VirtualVehicleEditor] 共清理 {removed} 个误加组件。");

        return removed;
    }

    static bool IsInLoadedScene(GameObject go) =>
        go.scene.IsValid() && go.scene.isLoaded;
}
