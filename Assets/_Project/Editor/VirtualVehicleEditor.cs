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
    const string ProjectRoot = "Assets/_Project";
    const string PrefabRoot = "Assets/download/SimplePoly City - Low Poly Assets/Prefab";
    static readonly string[] VehiclePrefabPaths =
    {
        ProjectRoot + "/Prefabs/Car 1.prefab",
        ProjectRoot + "/Prefabs/Police 1.prefab",
        ProjectRoot + "/Prefabs/Taxi_stylized.prefab"
    };

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

    [MenuItem("Tools/Virtual Vehicle/Setup Taxi Prefab")]
    public static void SetupTaxiPrefab()
    {
        const string path = ProjectRoot + "/Prefabs/Taxi_stylized.prefab";
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
        if (prefabRoot == null)
        {
            EditorUtility.DisplayDialog("Taxi 配置失败", "未找到 Taxi_stylized.prefab。", "确定");
            return;
        }

        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(prefabRoot);
        DriveableVehicleBuilder.EnsureTaxi(prefabRoot);
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("完成", "Taxi 预制体已配置为可驾驶车辆。", "确定");
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
                "4. 配置 Car 1 / Police 1 / Taxi 碰撞与形变\n" +
                "5. 创建/更新碰撞配置资产\n" +
                "6. 启用车辆模型 Read/Write\n\n是否继续？",
                "继续", "取消"))
            return;

        int cleaned = CleanupVehicleMeshColliders(silent: true);
        int removedCars = RemoveDecorativeVehiclesInEditor();
        int colliders = AddEnvironmentColliders();
        int prefabs = UpgradeVehiclePrefabs();
        int vehicles = SetupPlayerVehicles();
        int placedTaxi = EnsureTaxiInScene();
        int env = CollisionSceneSetup.ConfigureEnvironment();
        CollisionSystemAssetSetup.CreateConfigAssetsSilent();
        int meshReadWrite = CollisionSystemAssetSetup.EnableVehicleMeshReadWriteSilent();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("完成",
            $"请 Ctrl+S 保存后 Play。\n\n" +
            $"清理车辆碰撞：{cleaned}\n" +
            $"删除装饰车：{removedCars}\n" +
            $"补环境 Collider：{colliders}\n" +
            $"升级车辆 Prefab：{prefabs}\n" +
            $"可驾驶车辆：{vehicles}\n" +
            $"场景出租车：{placedTaxi}\n" +
            $"环境碰撞分类：{env}\n" +
            $"启用 Mesh Read/Write：{meshReadWrite}",
            "确定");
    }

    static int SetupPlayerVehicles()
    {
        int count = 0;
        foreach (CarController car in Object.FindObjectsOfType<CarController>())
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(car.gameObject);
            VehicleDeformationSetup.SetupVehicle(car.gameObject);
            count++;
        }

        return count;
    }

    static int EnsureTaxiInScene()
    {
        foreach (CarController car in Object.FindObjectsOfType<CarController>())
        {
            if (car.gameObject.name.ToLowerInvariant().Contains("taxi_stylized"))
                return 0;
        }

        const string taxiPrefabPath = ProjectRoot + "/Prefabs/Taxi_stylized.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(taxiPrefabPath);
        if (prefab == null)
            return 0;

        Vector3 spawnPosition = new Vector3(-18.5f, 0.04f, -44.3f);
        foreach (CarController car in Object.FindObjectsOfType<CarController>())
        {
            if (!car.gameObject.name.Contains("Car 1"))
                continue;

            spawnPosition = car.transform.position + car.transform.right * 4f;
            spawnPosition.y = car.transform.position.y;
            break;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
            return 0;

        instance.transform.position = spawnPosition;
        instance.transform.rotation = Quaternion.Euler(0f, -170f, 0f);
        DriveableVehicleBuilder.EnsureTaxi(instance);
        VehicleDeformationSetup.SetupVehicle(instance);
        Undo.RegisterCreatedObjectUndo(instance, "Add Taxi");
        return 1;
    }

    static int UpgradeVehiclePrefabs()
    {
        int count = 0;

        foreach (string path in VehiclePrefabPaths)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
            if (prefabRoot == null)
                continue;

            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(prefabRoot);
            if (path.EndsWith("Taxi_stylized.prefab"))
                DriveableVehicleBuilder.EnsureTaxi(prefabRoot);
            else
                VehicleDeformationSetup.SetupVehicle(prefabRoot);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
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
