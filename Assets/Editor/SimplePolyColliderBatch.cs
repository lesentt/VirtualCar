using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 为 SimplePoly City 资源包中缺少 Collider 的 Prefab / Demo 场景物体添加 MeshCollider。
/// 菜单：Tools → Virtual Vehicle → Add Colliders to SimplePoly Demo
/// </summary>
public static class SimplePolyColliderBatch
{
    const string PrefabRoot = "Assets/download/SimplePoly City - Low Poly Assets/Prefab";
    const string DemoScenePath =
        "Assets/download/SimplePoly City - Low Poly Assets/Demo/SimplePoly City - Low Poly Assets_Demo Scene.unity";

    [MenuItem("Tools/Virtual Vehicle/Add Colliders to SimplePoly Demo")]
    public static void Run()
    {
        if (!EditorUtility.DisplayDialog(
                "添加碰撞体",
                "将为 SimplePoly 全部 Prefab 及 Demo 场景中缺 Collider 的网格物体添加 MeshCollider。\n是否继续？",
                "继续", "取消"))
            return;

        int prefabCount = ProcessAllPrefabs();
        int sceneCount = ProcessDemoScene();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "完成",
            $"Prefab 处理：{prefabCount} 个物体添加了 MeshCollider\n" +
            $"Demo 场景处理：{sceneCount} 个物体添加了 MeshCollider\n\n请保存场景（Ctrl+S）。",
            "确定");
    }

    static int ProcessAllPrefabs()
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

        return added;
    }

    static int ProcessDemoScene()
    {
        if (!System.IO.File.Exists(DemoScenePath))
            return 0;

        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(DemoScenePath);
        int added = 0;

        foreach (GameObject root in scene.GetRootGameObjects())
            added += AddCollidersRecursive(root);

        if (added > 0)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);

        return added;
    }

    static int AddCollidersRecursive(GameObject go)
    {
        int added = 0;

        if (ShouldSkip(go))
            return 0;

        foreach (MeshFilter mf in go.GetComponents<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            if (go.GetComponent<Collider>() != null) continue;

            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            mc.convex = false;
            added++;
        }

        foreach (Transform child in go.transform)
            added += AddCollidersRecursive(child.gameObject);

        return added;
    }

    static bool ShouldSkip(GameObject go)
    {
        if (go.GetComponent<Camera>() != null) return true;
        if (go.GetComponent<Light>() != null) return true;
        if (go.GetComponent<AudioSource>() != null) return true;
        if (go.GetComponent<ParticleSystem>() != null) return true;
        return false;
    }
}
