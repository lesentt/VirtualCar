using UnityEditor;
using UnityEngine;

public static class VehicleCameraRigSetup
{
    const string PrefabSearchRoot = "Assets/_Project/Prefabs";

    [MenuItem("Tools/Virtual Vehicle/Setup Camera Rigs (All Vehicle Prefabs)")]
    public static void SetupAllVehiclePrefabs()
    {
        int baked = 0;
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabSearchRoot });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (BakePrefabAtPath(path))
                baked++;
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("摄像机 Rig 烘焙",
            $"已在 {baked} 个车辆 Prefab 中写入 CameraRig / CockpitAnchor。\n\n" +
            "可在 Prefab 模式下选中 CockpitAnchor 微调位置并保存。",
            "确定");
    }

    [MenuItem("Tools/Virtual Vehicle/Setup Camera Rigs (Current Scene)")]
    public static void SetupCurrentScene()
    {
        int baked = 0;
        foreach (CarController car in Object.FindObjectsOfType<CarController>())
        {
            if (BakeHierarchy(car.transform, markSceneDirty: true))
                baked++;
        }

        EditorUtility.DisplayDialog("摄像机 Rig 烘焙",
            $"已为场景中 {baked} 辆可驾驶车辆写入 CameraRig。\n\n请 Ctrl+S 保存场景。",
            "确定");
    }

    public static int BakeVehiclePrefabsSilent()
    {
        int baked = 0;
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabSearchRoot });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (BakePrefabAtPath(path))
                baked++;
        }

        return baked;
    }

    public static bool BakePrefabAtPath(string prefabPath)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null)
            return false;

        if (!ShouldBakeVehicle(prefabRoot))
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            return false;
        }

        bool baked = BakeHierarchy(prefabRoot.transform, markSceneDirty: false);
        if (baked)
        {
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            Debug.Log($"[VehicleCameraRigSetup] 已烘焙 {prefabPath}");
        }

        PrefabUtility.UnloadPrefabContents(prefabRoot);
        return baked;
    }

    public static bool BakeHierarchy(Transform vehicleRoot, bool markSceneDirty)
    {
        if (vehicleRoot == null || !ShouldBakeVehicle(vehicleRoot.gameObject))
            return false;

        VehicleCameraRig rig = vehicleRoot.GetComponent<VehicleCameraRig>();
        if (rig == null)
        {
            rig = markSceneDirty
                ? Undo.AddComponent<VehicleCameraRig>(vehicleRoot.gameObject)
                : vehicleRoot.gameObject.AddComponent<VehicleCameraRig>();
        }

        if (markSceneDirty)
            Undo.RegisterFullObjectHierarchyUndo(vehicleRoot.gameObject, "Setup Vehicle Camera Rig");

        rig.EnsureSetup();
        rig.ApplyViewMode(VehicleCameraRig.ViewMode.ThirdPerson);

        EditorUtility.SetDirty(rig);
        if (markSceneDirty && vehicleRoot.gameObject.scene.IsValid())
            EditorSceneManagerMarkDirty(vehicleRoot.gameObject);

        return true;
    }

    static bool ShouldBakeVehicle(GameObject root)
    {
        if (root.GetComponent<CarController>() != null)
            return true;
        if (root.GetComponentInChildren<Camera>(true) != null)
            return true;
        return false;
    }

    static void EditorSceneManagerMarkDirty(GameObject go)
    {
        if (!go.scene.IsValid())
            return;

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
    }
}

[CustomEditor(typeof(VehicleCameraRig))]
public class VehicleCameraRigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        VehicleCameraRig rig = (VehicleCameraRig)target;
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("编辑器微调", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "先在 Prefab 或场景中烘焙 Rig，再移动 CockpitAnchor 调整驾驶室视角。\n" +
            "调整完成后保存 Prefab / 场景；Play 时不会再丢失 CockpitAnchor。",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("烘焙 Camera Rig"))
            {
                Undo.RegisterFullObjectHierarchyUndo(rig.gameObject, "Bake Vehicle Camera Rig");
                rig.EnsureSetup();
                rig.ApplyViewMode(VehicleCameraRig.ViewMode.ThirdPerson);
                EditorUtility.SetDirty(rig);
            }

            if (GUILayout.Button("选中 CockpitAnchor"))
            {
                rig.EnsureSetup(false);
                if (rig.CockpitAnchor != null)
                    Selection.activeTransform = rig.CockpitAnchor;
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("预览第三人称"))
                Preview(rig, VehicleCameraRig.ViewMode.ThirdPerson);

            if (GUILayout.Button("预览驾驶室"))
                Preview(rig, VehicleCameraRig.ViewMode.FirstPerson);
        }

        if (GUILayout.Button("对齐 Scene 视图到驾驶室"))
            AlignSceneViewToCockpit(rig);
    }

    static void Preview(VehicleCameraRig rig, VehicleCameraRig.ViewMode mode)
    {
        Undo.RegisterFullObjectHierarchyUndo(rig.gameObject, "Preview Camera View");
        rig.EnsureSetup();
        rig.ApplyViewMode(mode);
        EditorUtility.SetDirty(rig);

        Camera cam = rig.VehicleCamera;
        if (cam != null)
            SceneView.lastActiveSceneView?.AlignViewToObject(cam.transform);
    }

    static void AlignSceneViewToCockpit(VehicleCameraRig rig)
    {
        rig.EnsureSetup(false);
        if (rig.CockpitAnchor == null)
            return;

        SceneView.lastActiveSceneView?.AlignViewToObject(rig.CockpitAnchor);
    }
}
