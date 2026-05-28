using UnityEngine;

/// <summary>
/// 车辆损伤：按撞击强度分级，重撞分区掉件，致命撞引擎区起火。
/// </summary>
public class CarDamageSystem : MonoBehaviour
{
    [Header("—— 损伤参数 ——")]
    [LabelText("损伤百分比")]
    [Range(0f, 100f)]
    public float damagePercent;

    [LabelText("损伤倍率")]
    public float damageMultiplier = 0.15f;

    [LabelText("碰撞次数")]
    public int collisionCount;

    [Header("—— 兼容：按损伤%掉件（无分区时用）——")]
    [LabelText("破碎部件列表")]
    public BreakablePart[] breakableParts;

    private CarController carController;
    private VehicleFireSystem fireSystem;
    private Rigidbody rb;
    private bool isTotaled;
    private float lastImpactForce;

    [System.Serializable]
    public class BreakablePart
    {
        public GameObject part;
        [Range(0f, 100f)] public float breakAtDamage = 25f;
        [HideInInspector] public bool broken;
    }

    void Awake()
    {
        carController = GetComponent<CarController>();
        fireSystem = GetComponent<VehicleFireSystem>();
        rb = GetComponent<Rigidbody>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isTotaled || rb == null) return;

        ImpactInfo info = ImpactEvaluator.Evaluate(collision, rb.mass);
        if (info.severity <= ImpactSeverity.Light) return;

        collisionCount++;
        lastImpactForce = info.normalSpeed;

        if (info.severity >= ImpactSeverity.Medium)
            ApplyDamage(info.impulse * damageMultiplier);

        if (info.severity >= ImpactSeverity.Heavy)
        {
            TryBreakZone(collision, info);
            TryBreakLegacy(info);
        }

        if (info.severity >= ImpactSeverity.Critical && IsEngineHit(collision))
            fireSystem?.TryIgnite();
    }

    void ApplyDamage(float amount)
    {
        damagePercent = Mathf.Min(100f, damagePercent + amount);
        carController?.SetDriveMultiplier(1f - damagePercent / 100f);
        if (damagePercent >= 100f) TotalVehicle();
    }

    void TryBreakZone(Collision collision, ImpactInfo info)
    {
        VehicleDamageZone zone = collision.collider.GetComponent<VehicleDamageZone>()
            ?? collision.collider.GetComponentInParent<VehicleDamageZone>();
        if (zone == null || zone.parts == null) return;

        Vector3 pushDir = collision.GetContact(0).point - transform.position;
        float force = ImpactEvaluator.KnockForce(info);

        foreach (GameObject part in zone.parts)
        {
            if (part == null || !part.transform.IsChildOf(transform)) continue;
            ImpactEvaluator.DetachPart(part, pushDir, force);
        }
    }

    void TryBreakLegacy(ImpactInfo info)
    {
        if (breakableParts == null) return;

        Vector3 pushDir = transform.forward;
        float force = ImpactEvaluator.KnockForce(info);

        foreach (BreakablePart bp in breakableParts)
        {
            if (bp.broken || bp.part == null) continue;
            if (damagePercent < bp.breakAtDamage) continue;
            bp.broken = true;
            ImpactEvaluator.DetachPart(bp.part, pushDir, force);
        }
    }

    static bool IsEngineHit(Collision collision)
    {
        VehicleDamageZone zone = collision.collider.GetComponent<VehicleDamageZone>()
            ?? collision.collider.GetComponentInParent<VehicleDamageZone>();
        return zone != null && zone.zoneType == DamageZoneType.Engine;
    }

    void TotalVehicle()
    {
        isTotaled = true;
        carController?.SetDisabled(true);
    }

    public float GetDamagePercent() => damagePercent;
    public int GetCollisionCount() => collisionCount;
    public float GetLastImpactForce() => lastImpactForce;
    public bool IsTotaled() => isTotaled;

    public string GetVehicleStatus()
    {
        if (fireSystem != null && fireSystem.IsOnFire) return "起火";
        if (isTotaled || damagePercent >= 100f) return "已报废";
        if (damagePercent >= 60f) return "严重受损";
        if (damagePercent >= 25f) return "轻微受损";
        return "正常";
    }
}
