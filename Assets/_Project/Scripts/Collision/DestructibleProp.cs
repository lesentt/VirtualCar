using UnityEngine;

/// <summary>
/// 可倾倒环境道具（路灯、树、红绿灯等）：受足够冲量后解除 Kinematic，靠 PhysX 倾倒，不会瞬间消失。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class DestructibleProp : MonoBehaviour
{
    public enum PropKind
    {
        Generic,
        Tree,
        Lamp,
        TrafficSignal,
        Stone
    }

    [LabelText("道具类型")]
    public PropKind kind = PropKind.Generic;

    [LabelText("质量(kg)")]
    public float mass = 80f;

    [LabelText("倾倒冲量阈值")]
    [Tooltip("碰撞冲量超过此值时解除固定并开始倾倒")]
    public float toppleImpulseThreshold = 1800f;

    [LabelText("倾倒力倍率")]
    [Tooltip("在碰撞点施加的额外扭矩冲量，越大越容易倒")]
    [Range(0.05f, 0.5f)]
    public float toppleForceScale = 0.12f;

    Rigidbody rb;
    bool hasFallen;

    public bool HasFallen => hasFallen;
    public float GetEffectiveMass() => mass;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.mass = mass;
            rb.isKinematic = true;
            AdjustCenterOfMass();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasFallen) return;
        if (!IsVehicleCollision(collision)) return;

        float impulse = CollisionTypes.ComputeImpulse(collision);
        float impactSpeed = collision.relativeVelocity.magnitude;

        if (impulse < toppleImpulseThreshold && impactSpeed < 4f)
            return;

        Topple(collision, impulse);
    }

    static bool IsVehicleCollision(Collision collision)
    {
        if (collision.rigidbody == null) return false;
        return collision.rigidbody.GetComponent<CollisionReporter>() != null
            || collision.rigidbody.GetComponent<CarController>() != null;
    }

    public void ResetProp()
    {
        hasFallen = false;
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        gameObject.layer = CollisionTypes.GetLayerIndex(CollisionTypes.LayerDestructible);
    }

    void Topple(Collision collision, float impulse)
    {
        hasFallen = true;

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.mass = mass;
        rb.drag = 1f;
        rb.angularDrag = 2f;
        rb.maxDepenetrationVelocity = 1.5f;
        rb.maxAngularVelocity = 2f;
        AdjustCenterOfMass();
        ApplyLowBounceMaterial();
        gameObject.layer = CollisionTypes.GetLayerIndex(CollisionTypes.LayerDebris);

        if (collision.contactCount == 0)
            return;

        ContactPoint contact = collision.GetContact(0);
        Vector3 pushDir = -contact.normal;
        pushDir.y = 0f;
        if (pushDir.sqrMagnitude < 0.01f)
            pushDir = transform.forward;

        pushDir.Normalize();
        float force = Mathf.Clamp(impulse * toppleForceScale, 50f, 500f);
        rb.AddForceAtPosition(pushDir * force, contact.point, ForceMode.Impulse);

        Vector3 torqueAxis = Vector3.Cross(Vector3.up, pushDir);
        if (torqueAxis.sqrMagnitude > 0.01f)
            rb.AddTorque(torqueAxis.normalized * force * 0.1f, ForceMode.Impulse);
    }

    static PhysicMaterial fallenMaterial;

    void ApplyLowBounceMaterial()
    {
        if (fallenMaterial == null)
        {
            fallenMaterial = new PhysicMaterial("FallenProp")
            {
                bounciness = 0.02f,
                bounceCombine = PhysicMaterialCombine.Minimum,
                dynamicFriction = 0.8f,
                staticFriction = 0.8f
            };
        }

        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            if (col is WheelCollider)
                continue;

            col.material = fallenMaterial;
        }
    }

    void AdjustCenterOfMass()
    {
        if (rb == null) return;

        switch (kind)
        {
            case PropKind.Tree:
            case PropKind.Lamp:
            case PropKind.TrafficSignal:
                rb.centerOfMass = new Vector3(0f, GetApproxHeight() * 0.35f, 0f);
                break;
            default:
                rb.centerOfMass = new Vector3(0f, GetApproxHeight() * 0.25f, 0f);
                break;
        }
    }

    float GetApproxHeight()
    {
        Collider col = GetComponentInChildren<Collider>();
        if (col != null)
            return col.bounds.size.y;

        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
            return renderer.bounds.size.y;

        return 2f;
    }

    public static float GetDefaultToppleThreshold(PropKind kind)
    {
        switch (kind)
        {
            case PropKind.Tree: return 3500f;
            case PropKind.Lamp: return 2200f;
            case PropKind.TrafficSignal: return 2800f;
            case PropKind.Stone: return 5000f;
            default: return 2500f;
        }
    }
}
