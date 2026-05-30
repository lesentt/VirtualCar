using UnityEngine;

/// <summary>
/// 车辆碰撞处理：冲量 + 质量比 → 损伤，并对车辆施加反冲。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class VehicleCollisionHandler : MonoBehaviour
{
    [LabelText("损伤倍率")]
    public float damageScale = 0.0025f;

    [LabelText("最低有效冲量")]
    public float minImpulse = 300f;

    [LabelText("反冲倍率")]
    [Range(0f, 1.5f)]
    public float knockbackScale = 0.35f;

    Rigidbody rb;
    CarDamageSystem damageSystem;
    float lastImpulse;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        damageSystem = GetComponent<CarDamageSystem>();
        gameObject.layer = CollisionTypes.GetLayerIndex(CollisionTypes.LayerVehicle);
        rb.maxDepenetrationVelocity = 2f;
    }

    void OnCollisionEnter(Collision collision)
    {
        GameObject other = collision.gameObject;
        if (other == null) return;

        if (other.GetComponentInParent<VehicleCollisionHandler>() != null)
            return;

        if (CollisionTypes.IsFallenEnvironmentObject(other))
        {
            ClampVerticalVelocity();
            return;
        }

        float impulse = CollisionTypes.ComputeImpulse(collision);
        if (impulse < minImpulse) return;

        CollisionCategory category = CollisionTypes.ResolveCategory(collision, other);
        float otherMass = CollisionTypes.EstimateMass(collision.rigidbody);
        float massRatio = CollisionTypes.ComputeMassRatio(rb.mass, otherMass);
        float categoryMultiplier = CollisionTypes.GetCategoryMultiplier(category);

        float damageAmount = impulse * damageScale * massRatio * categoryMultiplier;
        lastImpulse = impulse;

        damageSystem?.ReceiveImpact(damageAmount, impulse);
        ApplyKnockback(collision, impulse, massRatio, category);
        ClampVerticalVelocity();
    }

    void ApplyKnockback(Collision collision, float impulse, float massRatio, CollisionCategory category)
    {
        if (collision.contactCount == 0 || knockbackScale <= 0f)
            return;

        if (category == CollisionCategory.DynamicProp)
            return;

        float categoryKnockback = category == CollisionCategory.StaticImmovable ? knockbackScale : knockbackScale * 0.25f;

        ContactPoint contact = collision.GetContact(0);
        Vector3 knockDir = contact.normal;
        knockDir.y = 0f;
        if (knockDir.sqrMagnitude < 0.01f)
            return;

        knockDir.Normalize();
        if (Vector3.Dot(knockDir, GetHorizontalVelocity()) > 0f)
            knockDir = -knockDir;

        float speedChange = (impulse * massRatio * categoryKnockback) / rb.mass;
        float maxKnock = category == CollisionCategory.StaticImmovable ? 4f : 1.5f;
        speedChange = Mathf.Clamp(speedChange, 0f, maxKnock);

        if (speedChange > 0.05f)
            rb.AddForce(knockDir * speedChange, ForceMode.VelocityChange);
    }

    Vector3 GetHorizontalVelocity()
    {
        Vector3 v = rb.velocity;
        return new Vector3(v.x, 0f, v.z);
    }

    void ClampVerticalVelocity()
    {
        Vector3 v = rb.velocity;
        if (v.y > 3f)
            rb.velocity = new Vector3(v.x, 3f, v.z);
    }

    public float GetLastImpulse() => lastImpulse;
}
