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

    /// <summary>
    /// 环境道具用冲量（优先 PhysX 接触冲量）。
    /// </summary>
    public static float ComputeImpulse(Collision collision)
    {
        if (collision == null || collision.contactCount == 0)
            return 0f;

        float contactImpulse = SumContactImpulse(collision);
        if (contactImpulse > 0.01f)
            return contactImpulse;

        return collision.relativeVelocity.magnitude * EstimateMass(collision.rigidbody);
    }

    /// <summary>
    /// 车辆用冲量（接触冲量与动量估算取较大者，低速衰减）。
    /// </summary>
    public static float ComputeVehicleImpulse(Collision collision, CollisionReporter reporter)
    {
        if (collision == null || collision.contactCount == 0)
            return 0f;

        float contactImpulse = SumContactImpulse(collision);

        Rigidbody rbSelf = null;
        if (reporter != null)
            rbSelf = reporter.GetComponent<Rigidbody>() ?? reporter.Root.GetComponent<Rigidbody>();
        if (rbSelf == null)
            rbSelf = collision.GetContact(0).thisCollider.attachedRigidbody;

        Rigidbody rbOther = collision.rigidbody;
        float relSpeed = collision.relativeVelocity.magnitude;
        float massSelf = rbSelf != null ? rbSelf.mass : 1000f;
        float effectiveMass = massSelf;

        if (rbOther != null && !rbOther.isKinematic && rbSelf != null)
            effectiveMass = (massSelf * rbOther.mass) / (massSelf + rbOther.mass);
        else if (rbSelf == null)
            effectiveMass = StaticEffectiveMass;

        float estimatedImpulse = relSpeed * effectiveMass;

        const float softSpeed = 8f;
        if (relSpeed < softSpeed)
            estimatedImpulse *= relSpeed / softSpeed;

        return Mathf.Max(contactImpulse, estimatedImpulse);
    }

    static float SumContactImpulse(Collision collision)
    {
        float impulse = 0f;
        for (int i = 0; i < collision.contactCount; i++)
            impulse += collision.GetContact(i).impulse.magnitude;
        return impulse;
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

    public static bool IsPartOfPlayerVehicle(GameObject go)
    {
        if (go == null)
            return false;

        if (go.GetComponent<CarController>() != null)
            return true;

        if (go.GetComponentInParent<CarController>(true) != null)
            return true;

        return go.GetComponentInParent<VehicleState>(true) != null;
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
