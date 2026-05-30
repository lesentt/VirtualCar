using Bitgem.VFX.StylisedWater;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// 使用 Bitgem Stylised Water 在 Highway Bridge 2 下方生成水面。
/// </summary>
public static class BridgeWaterSetup
{
    const string WaterObjectName = "Bridge Water";
    const string BridgeObjectName = "Highway Bridge 2";
    const string UrpWaterMaterialPath = "Assets/Bitgem/StylisedWater/URP/Materials/example-water-01.mat";
    const string BuiltInWaterMaterialPath = "Assets/Materials/BridgeWater.mat";

    [MenuItem("Tools/Virtual Vehicle/Add Bridge Water")]
    public static void AddBridgeWaterMenu()
    {
        if (CreateOrUpdateBridgeWater() != null)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("完成", "已在桥下生成 Bitgem 水面。\n请 Ctrl+S 保存场景。", "确定");
        }
    }

    public static GameObject CreateOrUpdateBridgeWater()
    {
        GameObject bridge = GameObject.Find(BridgeObjectName);
        if (bridge == null)
        {
            EditorUtility.DisplayDialog("未找到桥梁", $"场景中不存在 \"{BridgeObjectName}\"。", "确定");
            return null;
        }

        Bounds bridgeBounds = ComputeWorldBounds(bridge);
        if (bridgeBounds.size == Vector3.zero)
        {
            EditorUtility.DisplayDialog("错误", "无法读取桥梁网格范围。", "确定");
            return null;
        }

        float tileSize = 2f;
        float surfaceY = bridgeBounds.min.y - 1.8f;
        float widthX = Mathf.Ceil((bridgeBounds.size.x + 24f) / tileSize) * tileSize;
        float depthZ = Mathf.Ceil((bridgeBounds.size.z + 20f) / tileSize) * tileSize;

        Vector3 volumeOrigin = new Vector3(
            bridgeBounds.center.x - widthX * 0.5f + tileSize * 0.5f,
            surfaceY - tileSize,
            bridgeBounds.center.z - depthZ * 0.5f + tileSize * 0.5f);

        GameObject water = GameObject.Find(WaterObjectName);
        if (water == null)
        {
            water = new GameObject(WaterObjectName);
            Undo.RegisterCreatedObjectUndo(water, "Add Bridge Water");
        }

        water.transform.SetPositionAndRotation(volumeOrigin, Quaternion.identity);
        water.transform.localScale = Vector3.one;

        MeshFilter meshFilter = water.GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = Undo.AddComponent<MeshFilter>(water);

        MeshRenderer renderer = water.GetComponent<MeshRenderer>();
        if (renderer == null)
            renderer = Undo.AddComponent<MeshRenderer>(water);

        Material waterMaterial = ResolveWaterMaterial();
        if (waterMaterial != null)
            renderer.sharedMaterial = waterMaterial;
        else
            Debug.LogWarning("[BridgeWaterSetup] 未找到可用水面材质。");

        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = true;

        WaterVolumeBox volume = water.GetComponent<WaterVolumeBox>();
        if (volume == null)
            volume = Undo.AddComponent<WaterVolumeBox>(water);

        volume.IncludeFaces = 0;
        volume.IncludeFoam = WaterVolumeBase.TileFace.NegX | WaterVolumeBase.TileFace.PosX |
                             WaterVolumeBase.TileFace.NegZ | WaterVolumeBase.TileFace.PosZ;
        volume.TileSize = tileSize;
        volume.ShowDebug = false;
        volume.RealtimeUpdates = false;
        volume.Dimensions = new Vector3(widthX, tileSize, depthZ);

        WaterVolumeHelper helper = water.GetComponent<WaterVolumeHelper>();
        if (helper == null)
            helper = Undo.AddComponent<WaterVolumeHelper>(water);
        helper.WaterVolume = volume;

        volume.Rebuild();

        Debug.Log($"[BridgeWaterSetup] 水面已生成：尺寸 {widthX}x{depthZ}，水面高度约 {surfaceY:F1}");
        return water;
    }

    static Material ResolveWaterMaterial()
    {
        if (GraphicsSettings.currentRenderPipeline != null)
        {
            Material urpMaterial = AssetDatabase.LoadAssetAtPath<Material>(UrpWaterMaterialPath);
            if (urpMaterial != null)
                return urpMaterial;
        }

        return AssetDatabase.LoadAssetAtPath<Material>(BuiltInWaterMaterialPath);
    }

    static Bounds ComputeWorldBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(root.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }
}
