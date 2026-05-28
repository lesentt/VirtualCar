using UnityEngine;

public enum ImpactSeverity
{
    None = 0,
    Light = 1,
    Medium = 2,
    Heavy = 3,
    Critical = 4
}

public struct ImpactInfo
{
    public float normalSpeed;
    public float impulse;
    public ImpactSeverity severity;
}

/// <summary>
/// 碰撞强度统一计算：severity、冲量、创飞/掉件力度。
/// </summary>
public static class ImpactEvaluator
{
    public const float LightThreshold = 3f;
    public const float MediumThreshold = 8f;
    public const float CriticalThreshold = 12f;
    public const float KnockMinSpeed = 4f;
    public const float KnockMaxTargetMass = 500f;
    public const float KnockMinMassRatio = 4f;

    public static ImpactInfo Evaluate(Collision collision, float selfMass)
    {
        ContactPoint contact = collision.GetContact(0);
        float normalSpeed = -Vector3.Dot(collision.relativeVelocity, contact.normal);
        float impulse = selfMass * Mathf.Max(0f, normalSpeed);

        ImpactSeverity severity = ImpactSeverity.None;
        if (normalSpeed >= CriticalThreshold) severity = ImpactSeverity.Critical;
        else if (normalSpeed >= MediumThreshold) severity = ImpactSeverity.Heavy;
        else if (normalSpeed >= LightThreshold) severity = ImpactSeverity.Medium;
        else if (normalSpeed >= 1f) severity = ImpactSeverity.Light;

        return new ImpactInfo
        {
            normalSpeed = normalSpeed,
            impulse = impulse,
            severity = severity
        };
    }

    public static float KnockForce(ImpactInfo info) => info.impulse * 0.5f;

    public static bool CanKnockFly(ImpactInfo info, float carMass, float targetMass)
    {
        if (info.severity < ImpactSeverity.Heavy) return false;
        if (info.normalSpeed < KnockMinSpeed) return false;
        if (targetMass > KnockMaxTargetMass) return false;
        return carMass / Mathf.Max(targetMass, 1f) >= KnockMinMassRatio;
    }

    public static void DetachPart(GameObject part, Vector3 pushDir, float force)
    {
        if (part == null) return;

        part.transform.SetParent(null, true);

        Rigidbody rb = part.GetComponent<Rigidbody>();
        if (rb == null) rb = part.AddComponent<Rigidbody>();

        EnsureConvexColliders(part);

        if (pushDir.sqrMagnitude < 0.01f) pushDir = Vector3.forward;
        rb.AddForce(pushDir.normalized * force, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * force * 0.3f, ForceMode.Impulse);
    }

    static void EnsureConvexColliders(GameObject part)
    {
        if (part.GetComponent<Collider>() != null) return;

        MeshFilter meshFilter = part.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            MeshCollider mc = part.AddComponent<MeshCollider>();
            mc.convex = true;
            return;
        }

        foreach (MeshFilter child in part.GetComponentsInChildren<MeshFilter>())
        {
            if (child.GetComponent<Collider>() != null) continue;
            MeshCollider mc = child.gameObject.AddComponent<MeshCollider>();
            mc.convex = true;
        }
    }
}
