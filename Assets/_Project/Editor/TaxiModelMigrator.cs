using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 将可驾驶出租车从 Taxi_stylized 迁移到 High Matters 的 Taxi 模型。
/// </summary>
public static class TaxiModelMigrator
{
    const string ProjectRoot = "Assets/_Project";
    const string SourceTaxiPath = "Assets/download/High Matters/Free American Sedans/Prefabs/Taxi.prefab";
    const string ProjectTaxiPath = ProjectRoot + "/Prefabs/Taxi.prefab";

    [MenuItem("Tools/Virtual Vehicle/Migrate Taxi Model (Scene + Prefab)")]
    public static void MigrateTaxiModel()
    {
        int replaced = MigrateTaxiModelSilent(showDialogs: true);
        if (replaced < 0)
            return;

        EditorUtility.DisplayDialog("迁移完成",
            $"已生成 {ProjectTaxiPath}，并在场景中替换 {replaced} 个出租车实例。\n\n" +
            "请保存场景（Ctrl+S）。可在 Taxi Prefab 中微调 CockpitAnchor。",
            "确定");
    }

    public static int MigrateTaxiModelSilent(bool showDialogs = false)
    {
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(SourceTaxiPath))
        {
            if (showDialogs)
                EditorUtility.DisplayDialog("迁移失败", $"未找到源模型：\n{SourceTaxiPath}", "确定");
            return -1;
        }

        if (!BuildProjectTaxiPrefab())
        {
            if (showDialogs)
                EditorUtility.DisplayDialog("迁移失败", "无法生成 Taxi.prefab。", "确定");
            return -1;
        }

        int replaced = ReplaceSceneTaxi();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        return replaced;
    }

    public static bool BuildProjectTaxiPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(SourceTaxiPath);
        if (root == null)
            return false;

        root.name = "Taxi";
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
        DriveableVehicleBuilder.EnsureTaxi(root);
        VehicleDeformationSetup.SetupVehicle(root);
        VehicleCameraRigSetup.BakeHierarchy(root.transform, markSceneDirty: false);

        PrefabUtility.SaveAsPrefabAsset(root, ProjectTaxiPath);
        PrefabUtility.UnloadPrefabContents(root);
        return true;
    }

    static int ReplaceSceneTaxi()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectTaxiPath);
        if (prefab == null)
            return 0;

        CarController legacy = FindLegacyTaxiController();
        GameObject raw = FindRawTaxiModel();
        Pose spawn = ResolveSpawnPose();

        int removed = 0;
        if (legacy != null)
        {
            spawn = new Pose(legacy.transform.position, legacy.transform.rotation);
            Undo.DestroyObjectImmediate(legacy.gameObject);
            removed++;
        }

        if (raw != null)
        {
            if (removed == 0)
                spawn = new Pose(raw.transform.position, raw.transform.rotation);
            Undo.DestroyObjectImmediate(raw);
            removed++;
        }

        if (legacy == null && raw == null)
            return 0;

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
            return 0;

        instance.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        instance.name = "Taxi";
        Undo.RegisterCreatedObjectUndo(instance, "Migrate Taxi Model");
        return 1;
    }

    static Pose ResolveSpawnPose()
    {
        CarController legacy = FindLegacyTaxiController();
        if (legacy != null)
            return new Pose(legacy.transform.position, legacy.transform.rotation);

        GameObject raw = FindRawTaxiModel();
        if (raw != null)
            return new Pose(raw.transform.position, raw.transform.rotation);

        foreach (CarController car in Object.FindObjectsOfType<CarController>())
        {
            if (car.gameObject.name.Contains("Car 1"))
                return new Pose(car.transform.position + car.transform.right * 4f, car.transform.rotation);
        }

        return new Pose(new Vector3(-18.5f, 0.04f, -44.3f), Quaternion.Euler(0f, -170f, 0f));
    }

    static CarController FindLegacyTaxiController()
    {
        foreach (CarController car in Object.FindObjectsOfType<CarController>())
        {
            if (car == null)
                continue;

            string lower = car.gameObject.name.ToLowerInvariant();
            if (lower.Contains("taxi"))
                return car;
        }

        return null;
    }

    static GameObject FindRawTaxiModel()
    {
        foreach (GameObject go in Object.FindObjectsOfType<GameObject>())
        {
            if (go == null || !go.scene.IsValid() || !go.scene.isLoaded)
                continue;
            if (go.name == "Taxi" && go.GetComponent<CarController>() == null)
                return go;
        }

        return null;
    }
}
