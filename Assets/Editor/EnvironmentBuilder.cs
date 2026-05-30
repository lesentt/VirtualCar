using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// 按 Prefab 实际尺寸搭建测试环境，保证与车辆（约 5m×2m）比例协调。
/// 菜单：Tools → Virtual Vehicle → Build Test Environment
/// </summary>
public static class EnvironmentBuilder
{
    const string EnvironmentRootName = "Environment";
    const string PolygonRoot = "Assets/POLYGON city pack/Prefabs";
    const string EnvMaterialPath = "Assets/Materials/EnvironmentPhysicMaterial.physicMaterial";

    // 以真实道路为参考的布局参数（米）
    const float SidewalkWidth = 4f;
    const float BuildingSetback = 3f;
    const float TargetTreeHeight = 7f;
    const float TargetBushHeight = 1.6f;

    [MenuItem("Tools/Virtual Vehicle/Build Test Environment")]
    public static void Build()
    {
        if (!EditorUtility.DisplayDialog(
                "搭建测试环境",
                "将按车辆比例重新搭建 Environment（宽马路、建筑退线、植物缩放）。\n是否继续？",
                "搭建", "取消"))
            return;

        ClearExisting();
        CleanupTestObjects();

        PhysicMaterial envMat = AssetDatabase.LoadAssetAtPath<PhysicMaterial>(EnvMaterialPath);

        // 测量关键 Prefab 尺寸（米）
        Bounds streetTile = MeasurePrefabBounds($"{PolygonRoot}/Floor/Street 1 Prefab.prefab");
        Bounds wideStreet = MeasurePrefabBounds($"{PolygonRoot}/Floor/Street 4 16M prefab.prefab");
        Bounds buildingLarge = MeasurePrefabBounds($"{PolygonRoot}/Buildings/Building_A1_prefab.prefab");
        Bounds buildingShop = MeasurePrefabBounds($"{PolygonRoot}/Buildings/Shop_A_prefab.prefab");
        Bounds tree = MeasurePrefabBounds($"{PolygonRoot}/Props/Tree prefab.prefab");
        Bounds bush = MeasurePrefabBounds($"{PolygonRoot}/Props/bush prefab.prefab");
        Bounds wall = MeasurePrefabBounds($"{PolygonRoot}/Props/Wall 5 prefab.prefab");
        Bounds stone = MeasurePrefabBounds($"{PolygonRoot}/Props/Stone 1 Prefab.prefab");

        float tileZ = Mathf.Max(streetTile.size.x, streetTile.size.z);
        float tileX = Mathf.Min(streetTile.size.x, streetTile.size.z);
        if (tileZ < 1f) tileZ = 10f;
        if (tileX < 1f) tileX = 10f;

        float roadHalfWidth = Mathf.Max(wideStreet.size.x, wideStreet.size.z) * 0.5f;
        if (roadHalfWidth < 4f) roadHalfWidth = 8f;

        Vector3 center = GetCourseCenter();
        float cx = center.x;
        float cz = center.z;

        Debug.Log($"[EnvironmentBuilder] 中心=({cx:F1}, {cz:F1}) | 街道块≈{tileX:F1}×{tileZ:F1}m | 宽路≈{roadHalfWidth * 2f:F1}m | 大楼≈{buildingLarge.size.x:F1}×{buildingLarge.size.z:F1}m");

        GameObject root = new GameObject(EnvironmentRootName);
        Undo.RegisterCreatedObjectUndo(root, "Build Environment");

        Transform roads = CreateChild(root.transform, "Roads");
        Transform buildings = CreateChild(root.transform, "Buildings");
        Transform bridge = CreateChild(root.transform, "Bridge");
        Transform props = CreateChild(root.transform, "Props");

        // —— 道路：宽马路（16m）+ 十字延伸 ——
        PlaceWideRoad(roads, new Vector3(cx, 0f, cz), 0f);
        for (int i = 1; i <= 3; i++)
        {
            PlaceWideRoad(roads, new Vector3(cx, 0f, cz + i * tileZ), 0f);
            PlaceWideRoad(roads, new Vector3(cx, 0f, cz - i * tileZ), 0f);
            PlaceWideRoad(roads, new Vector3(cx + i * tileZ, 0f, cz), 90f);
            PlaceWideRoad(roads, new Vector3(cx - i * tileZ, 0f, cz), 90f);
        }

        // 人行道（Sideway 沿 Z 方向贴路缘）
        float sidewalkOffset = roadHalfWidth + SidewalkWidth * 0.5f;
        Place($"{PolygonRoot}/Floor/Sideway 1 prefab.prefab", roads,
            new Vector3(cx - sidewalkOffset, 0f, cz), Vector3.zero);
        Place($"{PolygonRoot}/Floor/Sideway 1 prefab.prefab", roads,
            new Vector3(cx + sidewalkOffset, 0f, cz), Vector3.zero);
        Place($"{PolygonRoot}/Floor/Sideway 1 prefab.prefab", roads,
            new Vector3(cx, 0f, cz - sidewalkOffset), new Vector3(0f, 90f, 0f));
        Place($"{PolygonRoot}/Floor/Sideway 1 prefab.prefab", roads,
            new Vector3(cx, 0f, cz + sidewalkOffset), new Vector3(0f, 90f, 0f));

        // —— 建筑：大搂退线，商铺临街 ——
        float largeSetback = roadHalfWidth + SidewalkWidth + BuildingSetback + buildingLarge.extents.x;
        float shopSetback = roadHalfWidth + SidewalkWidth * 0.5f + buildingShop.extents.z;

        PlaceBuilding($"{PolygonRoot}/Buildings/Building_A1_prefab.prefab", buildings,
            new Vector3(cx - largeSetback, 0f, cz + 15f), 90f, envMat);
        PlaceBuilding($"{PolygonRoot}/Buildings/Building_C1_prefab.prefab", buildings,
            new Vector3(cx + largeSetback, 0f, cz + 15f), -90f, envMat);
        PlaceBuilding($"{PolygonRoot}/Buildings/Building_A1_prefab.prefab", buildings,
            new Vector3(cx - largeSetback, 0f, cz - 25f), 90f, envMat);

        PlaceBuilding($"{PolygonRoot}/Buildings/Shop_A_prefab.prefab", buildings,
            new Vector3(cx - shopSetback, 0f, cz - 5f), 90f, envMat);
        PlaceBuilding($"{PolygonRoot}/Buildings/Shop_B_prefab.prefab", buildings,
            new Vector3(cx + shopSetback, 0f, cz + 8f), -90f, envMat);

        // —— 桥：跨度覆盖宽路，净高约 5m ——
        float bridgeZ = cz + tileZ * 2.5f;
        float bridgeDeckY = 5.5f;
        float bridgeClearHalf = roadHalfWidth + 1f;

        PlaceWideRoad(bridge, new Vector3(cx, bridgeDeckY, bridgeZ), 0f);
        PlaceWideRoad(bridge, new Vector3(cx, bridgeDeckY, bridgeZ + tileZ * 0.9f), 0f);

        PlacePillar(bridge, wall, envMat, new Vector3(cx - bridgeClearHalf, 0f, bridgeZ - 2f));
        PlacePillar(bridge, wall, envMat, new Vector3(cx + bridgeClearHalf, 0f, bridgeZ - 2f));
        PlacePillar(bridge, wall, envMat, new Vector3(cx - bridgeClearHalf, 0f, bridgeZ + tileZ * 0.9f + 2f));
        PlacePillar(bridge, wall, envMat, new Vector3(cx + bridgeClearHalf, 0f, bridgeZ + tileZ * 0.9f + 2f));

        // —— 道具：按目标高度缩放 ——
        float treeScale = tree.size.y > 0.1f ? TargetTreeHeight / tree.size.y : 1f;
        float bushScale = bush.size.y > 0.1f ? TargetBushHeight / bush.size.y : 1f;

        float propOffset = roadHalfWidth + SidewalkWidth + 1.5f;
        PlaceScaled($"{PolygonRoot}/Props/Tree prefab.prefab", props,
            new Vector3(cx - propOffset, 0f, cz + 10f), Vector3.zero, treeScale);
        PlaceScaled($"{PolygonRoot}/Props/Tree prefab.prefab", props,
            new Vector3(cx + propOffset, 0f, cz + 10f), Vector3.zero, treeScale);
        PlaceScaled($"{PolygonRoot}/Props/Tree prefab.prefab", props,
            new Vector3(cx - propOffset, 0f, cz - 15f), Vector3.zero, treeScale);
        PlaceScaled($"{PolygonRoot}/Props/Tree prefab.prefab", props,
            new Vector3(cx + propOffset, 0f, cz - 15f), Vector3.zero, treeScale);

        PlaceScaled($"{PolygonRoot}/Props/bush prefab.prefab", props,
            new Vector3(cx - roadHalfWidth - 1f, 0f, cz + 3f), Vector3.zero, bushScale);
        PlaceScaled($"{PolygonRoot}/Props/bush prefab.prefab", props,
            new Vector3(cx + roadHalfWidth + 1f, 0f, cz - 3f), Vector3.zero, bushScale);
        Place($"{PolygonRoot}/Props/hedge_curve prefab.prefab", props,
            new Vector3(cx - roadHalfWidth - 0.5f, 0f, cz - 8f), new Vector3(0f, 90f, 0f));

        Place($"{PolygonRoot}/Props/Stone 1 Prefab.prefab", props,
            new Vector3(cx + roadHalfWidth * 0.5f, 0f, cz + 4f), Vector3.zero, envMat);
        Place($"{PolygonRoot}/Lamps/street_lamp 1 prefab.prefab", props,
            new Vector3(cx - roadHalfWidth - 0.8f, 0f, cz), Vector3.zero);
        Place($"{PolygonRoot}/Lamps/street_lamp 1 prefab.prefab", props,
            new Vector3(cx + roadHalfWidth + 0.8f, 0f, cz), Vector3.zero);

        MarkStaticRecursive(root);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = root;

        Debug.Log("[EnvironmentBuilder] 完成。若仍有偏差，查看 Console 中的尺寸日志。");
    }

    [MenuItem("Tools/Virtual Vehicle/Clear Test Environment")]
    public static void ClearMenu()
    {
        if (!EditorUtility.DisplayDialog("清除测试环境", "删除场景中的 Environment 物体？", "清除", "取消"))
            return;

        ClearExisting();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    static Vector3 GetCourseCenter()
    {
        GameObject car = GameObject.Find("Car");
        GameObject police = GameObject.Find("Police");

        if (car != null && police != null)
        {
            Vector3 a = car.transform.position;
            Vector3 b = police.transform.position;
            return new Vector3((a.x + b.x) * 0.5f, 0f, (a.z + b.z) * 0.5f);
        }

        if (car != null)
            return new Vector3(car.transform.position.x, 0f, car.transform.position.z);

        return new Vector3(-21f, 0f, -40f);
    }

    static Bounds MeasurePrefabBounds(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
            return new Bounds(Vector3.zero, Vector3.one * 10f);

        GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        temp.transform.position = Vector3.zero;
        temp.transform.rotation = Quaternion.identity;
        temp.transform.localScale = Vector3.one;

        Bounds bounds = GetCombinedBounds(temp);
        Object.DestroyImmediate(temp);
        return bounds;
    }

    static Bounds GetCombinedBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(go.transform.position, Vector3.one);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    static void PlaceWideRoad(Transform parent, Vector3 pos, float yaw)
    {
        Place($"{PolygonRoot}/Floor/Street 4 16M prefab.prefab", parent, pos, new Vector3(0f, yaw, 0f));
    }

    static void PlaceBuilding(string path, Transform parent, Vector3 pos, float yaw, PhysicMaterial mat)
    {
        Place(path, parent, pos, new Vector3(0f, yaw, 0f), mat);
    }

    static void PlacePillar(Transform parent, Bounds wallBounds, PhysicMaterial mat, Vector3 pos)
    {
        float pillarScale = 5f / Mathf.Max(wallBounds.size.y, 1f);
        PlaceScaled($"{PolygonRoot}/Props/Wall 5 prefab.prefab", parent, pos,
            new Vector3(0f, 90f, 0f), pillarScale, mat);
    }

    static Transform CreateChild(Transform parent, string name)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Build Environment");
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static GameObject Place(string prefabPath, Transform parent, Vector3 pos, Vector3 euler,
        PhysicMaterial physicMaterial = null)
    {
        return PlaceScaled(prefabPath, parent, pos, euler, 1f, physicMaterial);
    }

    static GameObject PlaceScaled(string prefabPath, Transform parent, Vector3 pos, Vector3 euler,
        float uniformScale, PhysicMaterial physicMaterial = null)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[EnvironmentBuilder] 找不到 Prefab：{prefabPath}");
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        Undo.RegisterCreatedObjectUndo(instance, "Build Environment");
        instance.transform.localPosition = pos;
        instance.transform.localRotation = Quaternion.Euler(euler);
        instance.transform.localScale = Vector3.one * uniformScale;

        if (physicMaterial != null)
            ApplyMaterial(instance, physicMaterial);

        return instance;
    }

    static void ApplyMaterial(GameObject root, PhysicMaterial material)
    {
        foreach (Collider col in root.GetComponentsInChildren<Collider>())
            col.sharedMaterial = material;
    }

    static void MarkStaticRecursive(GameObject root)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            GameObjectUtility.SetStaticEditorFlags(t.gameObject,
                StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        }
    }

    static void ClearExisting()
    {
        GameObject existing = GameObject.Find(EnvironmentRootName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);
    }

    static void CleanupTestObjects()
    {
        GameObject cube = GameObject.Find("Cube");
        if (cube != null)
            Undo.DestroyObjectImmediate(cube);
    }
}
