using UnityEngine;

/// <summary>
/// 环境物体碰撞配置：分类、质量、物理层初始化。
/// </summary>
[DisallowMultipleComponent]
public class CollisionProfile : MonoBehaviour
{
    [LabelText("碰撞分类")]
    public CollisionCategory category = CollisionCategory.StaticImmovable;

    [LabelText("有效质量(kg)")]
    [Tooltip("用于损伤计算；StaticImmovable 视为极大质量")]
    public float mass = 100f;

    [LabelText("自动配置物理")]
    [Tooltip("Awake 时自动设置 Layer 与 Rigidbody")]
    public bool autoConfigurePhysics = true;

    Rigidbody cachedRigidbody;

    void Awake()
    {
        if (autoConfigurePhysics)
            ApplyPhysicsSetup();
    }

    public void ApplyPhysicsSetup()
    {
        gameObject.isStatic = false;
        gameObject.layer = GetLayerForCategory(category);

        switch (category)
        {
            case CollisionCategory.StaticImmovable:
                RemoveRigidbodyIfPresent();
                break;

            case CollisionCategory.DynamicProp:
                ConfigureDynamicRigidbody(false);
                EnsureCollidersExist();
                EnsureConvexColliders();
                break;

            case CollisionCategory.DestructibleProp:
                ConfigureDynamicRigidbody(true);
                EnsureCollidersExist();
                EnsureConvexColliders();
                EnsureDestructibleComponent();
                break;
        }
    }

    public float GetEffectiveMass()
    {
        if (category == CollisionCategory.StaticImmovable)
            return CollisionTypes.StaticEffectiveMass;

        if (cachedRigidbody != null && cachedRigidbody.mass > 0f)
            return cachedRigidbody.mass;

        return mass;
    }

    public static int GetLayerForCategory(CollisionCategory cat)
    {
        switch (cat)
        {
            case CollisionCategory.StaticImmovable:
                return CollisionTypes.GetLayerIndex(CollisionTypes.LayerStaticEnv);
            case CollisionCategory.DynamicProp:
                return CollisionTypes.GetLayerIndex(CollisionTypes.LayerDynamicProp);
            case CollisionCategory.DestructibleProp:
                return CollisionTypes.GetLayerIndex(CollisionTypes.LayerDestructible);
            default:
                return 0;
        }
    }

    void ConfigureDynamicRigidbody(bool kinematic)
    {
        cachedRigidbody = GetComponent<Rigidbody>();
        if (cachedRigidbody == null)
            cachedRigidbody = gameObject.AddComponent<Rigidbody>();

        cachedRigidbody.isKinematic = kinematic;
        cachedRigidbody.mass = mass;
        cachedRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        cachedRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void RemoveRigidbodyIfPresent()
    {
        cachedRigidbody = GetComponent<Rigidbody>();
        if (cachedRigidbody != null)
            Destroy(cachedRigidbody);
    }

    void EnsureDestructibleComponent()
    {
        DestructibleProp destructible = GetComponent<DestructibleProp>();
        if (destructible == null)
            destructible = gameObject.AddComponent<DestructibleProp>();

        destructible.mass = mass;
    }

    public static void EnsureConvexColliders(GameObject root)
    {
        foreach (Collider col in root.GetComponentsInChildren<Collider>())
        {
            if (col is MeshCollider meshCollider)
                meshCollider.convex = true;
        }
    }

    void EnsureConvexColliders()
    {
        EnsureConvexColliders(gameObject);
    }

    void EnsureCollidersExist()
    {
        if (GetComponentsInChildren<Collider>().Length > 0)
            return;

        foreach (MeshFilter meshFilter in GetComponentsInChildren<MeshFilter>())
        {
            if (meshFilter.GetComponent<Collider>() != null)
                continue;

            MeshCollider meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
            meshCollider.convex = true;
        }
    }
}
