using UnityEngine;

/// <summary>
/// 环境物体碰撞分类。
/// </summary>
public enum CollisionCategory
{
    StaticImmovable,
    DynamicProp,
    DestructibleProp
}

/// <summary>
/// 碰撞层名称、冲量计算、车辆碰撞体识别与清理。
/// </summary>
public static class CollisionTypes
{
    public const float StaticEffectiveMass = 10000f;

    public const string LayerVehicle = "Vehicle";
    public const string LayerStaticEnv = "StaticEnv";
    public const string LayerDynamicProp = "DynamicProp";
    public const string LayerDestructible = "Destructible";
    public const string LayerDebris = "Debris";

    public static int GetLayerIndex(string layerName) => LayerMask.NameToLayer(layerName);

    public static bool IsDebris(GameObject go) =>
        go != null && go.layer == LayerMask.NameToLayer(LayerDebris);

    public static bool IsFallenEnvironmentObject(GameObject go)
    {
        if (go == null)
            return false;

        if (IsDebris(go))
            return true;

        DestructibleProp prop = go.GetComponentInParent<DestructibleProp>();
        return prop != null && prop.HasFallen;
    }

    public static float ComputeImpulse(Collision collision)
    {
        if (collision == null || collision.contactCount == 0)
            return 0f;

        float impulse = 0f;
        for (int i = 0; i < collision.contactCount; i++)
            impulse += collision.GetContact(i).impulse.magnitude;

        if (impulse > 0.01f)
            return impulse;

        return collision.relativeVelocity.magnitude * EstimateMass(collision.rigidbody);
    }

    public static float EstimateMass(Rigidbody rb)
    {
        if (rb == null)
            return StaticEffectiveMass;

        CollisionProfile profile = rb.GetComponent<CollisionProfile>();
        if (profile != null)
            return profile.GetEffectiveMass();

        DestructibleProp destructible = rb.GetComponent<DestructibleProp>();
        if (destructible != null)
            return destructible.GetEffectiveMass();

        return rb.mass;
    }

    public static float ComputeMassRatio(float carMass, float otherMass)
    {
        if (carMass <= 0f)
            return 1f;

        otherMass = Mathf.Max(otherMass, 0.01f);
        return otherMass / (carMass + otherMass);
    }

    public static float GetCategoryMultiplier(CollisionCategory category)
    {
        switch (category)
        {
            case CollisionCategory.StaticImmovable: return 1f;
            case CollisionCategory.DestructibleProp: return 0.4f;
            case CollisionCategory.DynamicProp: return 0.05f;
            default: return 1f;
        }
    }

    public static CollisionCategory ResolveCategory(Collision collision, GameObject otherObject)
    {
        if (otherObject == null)
            return CollisionCategory.StaticImmovable;

        CollisionProfile profile = otherObject.GetComponentInParent<CollisionProfile>();
        if (profile != null)
            return profile.category;

        if (otherObject.GetComponentInParent<DestructibleProp>() != null)
            return CollisionCategory.DestructibleProp;

        Rigidbody otherRb = collision.rigidbody;
        if (otherRb != null && !otherRb.isKinematic)
            return CollisionCategory.DynamicProp;

        return CollisionCategory.StaticImmovable;
    }

    public static bool IsPartOfPlayerVehicle(GameObject go)
    {
        if (go == null)
            return false;

        if (go.GetComponent<CarController>() != null || go.GetComponent<CarDamageSystem>() != null)
            return true;

        if (go.GetComponentInParent<CarController>(true) != null)
            return true;

        return go.GetComponentInParent<CarDamageSystem>(true) != null;
    }

    public static int CleanupVehicleColliders(GameObject vehicleRoot)
    {
        if (vehicleRoot == null)
            return 0;

        int removed = 0;

        foreach (CollisionProfile profile in vehicleRoot.GetComponentsInChildren<CollisionProfile>(true))
        {
            if (profile.gameObject == vehicleRoot) continue;
            Object.Destroy(profile);
            removed++;
        }

        foreach (DestructibleProp prop in vehicleRoot.GetComponentsInChildren<DestructibleProp>(true))
        {
            if (prop.gameObject == vehicleRoot) continue;
            Object.Destroy(prop);
            removed++;
        }

        foreach (Rigidbody rb in vehicleRoot.GetComponentsInChildren<Rigidbody>(true))
        {
            if (rb.gameObject == vehicleRoot) continue;
            Object.Destroy(rb);
            removed++;
        }

        foreach (Collider col in vehicleRoot.GetComponentsInChildren<Collider>(true))
        {
            if (col is WheelCollider || col is BoxCollider) continue;
            if (col is MeshCollider)
            {
                Object.Destroy(col);
                removed++;
            }
        }

        return removed;
    }

    public static int CleanupAllPlayerVehicles()
    {
        int total = 0;
        foreach (CarController car in Object.FindObjectsOfType<CarController>())
            total += CleanupVehicleColliders(car.gameObject);
        return total;
    }
}
