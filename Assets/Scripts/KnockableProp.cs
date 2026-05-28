using UnityEngine;

/// <summary>
/// 可被车辆创飞的场景物体。条件见 ImpactEvaluator.CanKnockFly。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class KnockableProp : MonoBehaviour
{
    private Rigidbody rb;

    void Awake() => rb = GetComponent<Rigidbody>();

    void OnCollisionEnter(Collision collision)
    {
        Rigidbody carRb = collision.rigidbody;
        if (carRb == null || carRb.GetComponent<CarController>() == null) return;

        ImpactInfo info = ImpactEvaluator.Evaluate(collision, carRb.mass);
        if (!ImpactEvaluator.CanKnockFly(info, carRb.mass, rb.mass)) return;

        Vector3 pushDir = -collision.GetContact(0).normal;
        rb.AddForce(pushDir * ImpactEvaluator.KnockForce(info), ForceMode.Impulse);
    }
}
