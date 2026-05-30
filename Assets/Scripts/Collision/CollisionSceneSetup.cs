using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 场景碰撞自动配置：环境分类、删除装饰车、Play 时补全。
/// </summary>
public static class CollisionSceneSetup
{
    static readonly string[] StaticKeywords =
    {
        "building", "road", "street", "sideway", "sidewalk", "floor", "wall", "bridge",
        "shop", "house", "pillar", "ground", "asphalt", "pavement", "roof"
    };

    static readonly string[] ToppleKeywords =
    {
        "tree", "lamp", "traffic", "signal", "pole", "hydrant"
    };

    static readonly string[] DynamicKeywords =
    {
        "bush", "hedge", "trash", "bin", "cone", "chair", "bench", "cart", "sign",
        "barrier", "stone", "rock", "meter"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnSceneLoaded()
    {
        int cleaned = CollisionTypes.CleanupAllPlayerVehicles();
        int removedCars = RemoveDecorativeVehicles();
        EnsurePlayerVehicles();
        ConfigureEnvironment();

        if (cleaned > 0)
            Debug.Log($"[CollisionSceneSetup] 已清理车辆误加碰撞体 {cleaned} 个。");
        if (removedCars > 0)
            Debug.Log($"[CollisionSceneSetup] 已删除装饰车辆 {removedCars} 辆。");
    }

    public static bool IsDecorativeVehicle(GameObject go)
    {
        if (go == null)
            return false;

        if (go.GetComponent<CarController>() != null || go.GetComponentInParent<CarController>() != null)
            return false;

        return IsDecorativeVehicleName(go.name.ToLowerInvariant());
    }

    public static bool IsDecorativeVehicleName(string lowerName)
    {
        if (string.IsNullOrEmpty(lowerName))
            return false;

        if (lowerName.StartsWith("vehicle_"))
            return true;

        return lowerName.Contains("vehicle") && lowerName.Contains("color");
    }

    public static int RemoveDecorativeVehicles()
    {
        var toRemove = new List<GameObject>();
        foreach (GameObject go in Object.FindObjectsOfType<GameObject>())
        {
            if (IsDecorativeVehicle(go))
                toRemove.Add(go);
        }

        foreach (GameObject go in toRemove)
            Object.Destroy(go);

        return toRemove.Count;
    }

    public static void EnsurePlayerVehicles()
    {
        foreach (CarDamageSystem damage in Object.FindObjectsOfType<CarDamageSystem>())
        {
            GameObject go = damage.gameObject;
            if (go.GetComponent<VehicleCollisionHandler>() == null)
                go.AddComponent<VehicleCollisionHandler>();

            go.layer = CollisionTypes.GetLayerIndex(CollisionTypes.LayerVehicle);
        }
    }

    public static int ConfigureEnvironment()
    {
        int count = 0;
        foreach (GameObject go in Object.FindObjectsOfType<GameObject>())
        {
            if (!ShouldConfigure(go))
                continue;

            CollisionCategory? category = Classify(go.name);
            if (category == null)
                continue;

            ApplyCategory(go, category.Value);
            count++;
        }

        return count;
    }

    static bool ShouldConfigure(GameObject go)
    {
        if (go == null || !go.activeInHierarchy)
            return false;

        if (CollisionTypes.IsPartOfPlayerVehicle(go))
            return false;

        if (go.GetComponent<VehicleCollisionHandler>() != null)
            return false;

        if (go.GetComponent<Camera>() != null || go.GetComponent<Light>() != null)
            return false;

        if (go.GetComponent<WheelCollider>() != null)
            return false;

        if (IsDecorativeVehicleName(go.name.ToLowerInvariant()))
            return false;

        if (go.GetComponent<CollisionProfile>() != null)
            return true;

        return go.GetComponent<MeshFilter>() != null || go.GetComponent<Collider>() != null;
    }

    static CollisionCategory? Classify(string objectName)
    {
        string lower = objectName.ToLowerInvariant();

        foreach (string keyword in ToppleKeywords)
            if (lower.Contains(keyword)) return CollisionCategory.DestructibleProp;

        foreach (string keyword in DynamicKeywords)
            if (lower.Contains(keyword)) return CollisionCategory.DynamicProp;

        foreach (string keyword in StaticKeywords)
            if (lower.Contains(keyword)) return CollisionCategory.StaticImmovable;

        return null;
    }

    static void ApplyCategory(GameObject go, CollisionCategory category)
    {
        go.isStatic = false;

        CollisionProfile profile = go.GetComponent<CollisionProfile>();
        if (profile == null)
            profile = go.AddComponent<CollisionProfile>();

        profile.category = category;
        profile.mass = GetDefaultMass(category, go.name);
        profile.autoConfigurePhysics = true;

        if (category == CollisionCategory.DestructibleProp)
        {
            DestructibleProp prop = go.GetComponent<DestructibleProp>();
            if (prop == null)
                prop = go.AddComponent<DestructibleProp>();

            prop.kind = GuessKind(go.name);
            prop.mass = profile.mass;
            prop.toppleImpulseThreshold = DestructibleProp.GetDefaultToppleThreshold(prop.kind);
        }

        EnsureCollider(go, category);
        profile.ApplyPhysicsSetup();
    }

    static void EnsureCollider(GameObject go, CollisionCategory category)
    {
        if (go.GetComponent<Collider>() != null)
            return;

        MeshFilter meshFilter = go.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return;

        MeshCollider meshCollider = go.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = meshFilter.sharedMesh;
        meshCollider.convex = category != CollisionCategory.StaticImmovable;
    }

    static float GetDefaultMass(CollisionCategory category, string objectName)
    {
        if (category == CollisionCategory.StaticImmovable)
            return 0f;

        string lower = objectName.ToLowerInvariant();
        if (lower.Contains("stone") || lower.Contains("rock")) return 180f;
        if (lower.Contains("tree")) return 150f;
        if (lower.Contains("traffic") || lower.Contains("signal")) return 90f;
        if (lower.Contains("lamp") || lower.Contains("pole")) return 55f;
        if (lower.Contains("trash") || lower.Contains("bin")) return 22f;
        if (category == CollisionCategory.DynamicProp) return 12f;
        return 70f;
    }

    static DestructibleProp.PropKind GuessKind(string objectName)
    {
        string lower = objectName.ToLowerInvariant();
        if (lower.Contains("tree")) return DestructibleProp.PropKind.Tree;
        if (lower.Contains("traffic") || lower.Contains("signal")) return DestructibleProp.PropKind.TrafficSignal;
        if (lower.Contains("lamp") || lower.Contains("pole")) return DestructibleProp.PropKind.Lamp;
        if (lower.Contains("stone") || lower.Contains("rock")) return DestructibleProp.PropKind.Stone;
        return DestructibleProp.PropKind.Generic;
    }
}
