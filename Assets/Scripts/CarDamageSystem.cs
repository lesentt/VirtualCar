using UnityEngine;

/// <summary>
/// 车辆损伤与破碎系统。
/// 功能：碰撞累积损伤、部件脱落、100% 时报废无法移动。
/// </summary>
public class CarDamageSystem : MonoBehaviour
{
    [Header("—— 损伤参数 ——")]
    [LabelText("损伤百分比")]
    [Tooltip("当前累计损伤百分比（运行时自动更新）。100% 时车辆报废")]
    [Range(0f, 100f)]
    public float damagePercent;

    [LabelText("损伤倍率")]
    [Tooltip("碰撞力度转损伤的系数。越大，同样碰撞下损伤增加越快")]
    public float damageMultiplier = 2f;

    [LabelText("最低碰撞速度")]
    [Tooltip("最低有效碰撞速度。低于此值的轻碰不计损伤")]
    public float minImpactForce = 3f;

    [LabelText("碰撞次数")]
    [Tooltip("累计碰撞次数（运行时自动更新）")]
    public int collisionCount;

    [Header("—— 可破碎部件 ——")]
    [LabelText("破碎部件列表")]
    [Tooltip("配置会在损伤达标时脱落的部件，如引擎盖、车门、后备箱")]
    public BreakablePart[] breakableParts;

    private CarController carController;
    private bool isTotaled;
    private float lastImpactForce;

    [System.Serializable]
    public class BreakablePart
    {
        [LabelText("部件物体")]
        [Tooltip("要破碎的部件物体，从 Hierarchy 拖入（如 Car_hood）")]
        public GameObject part;

        [LabelText("破碎阈值(%)")]
        [Tooltip("损伤达到该百分比时，此部件脱落并飞出")]
        [Range(0f, 100f)]
        public float breakAtDamage = 25f;

        [HideInInspector]
        public bool broken;
    }

    void Awake()
    {
        carController = GetComponent<CarController>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isTotaled) return;

        float impactForce = collision.relativeVelocity.magnitude;
        if (impactForce < minImpactForce) return;

        collisionCount++;
        lastImpactForce = impactForce;
        float damageAmount = impactForce * damageMultiplier + collisionCount * 1.5f;
        ApplyDamage(damageAmount);
    }

    void ApplyDamage(float amount)
    {
        damagePercent = Mathf.Min(100f, damagePercent + amount);
        UpdateDrivePenalty();
        TryBreakParts();

        if (damagePercent >= 100f)
            TotalVehicle();
    }

    void UpdateDrivePenalty()
    {
        if (carController == null) return;
        float driveFactor = 1f - damagePercent / 100f;
        carController.SetDriveMultiplier(driveFactor);
    }

    void TryBreakParts()
    {
        foreach (BreakablePart part in breakableParts)
        {
            if (part.broken || part.part == null) continue;
            if (damagePercent < part.breakAtDamage) continue;
            BreakPart(part);
        }
    }

    void BreakPart(BreakablePart breakable)
    {
        breakable.broken = true;
        GameObject part = breakable.part;

        part.transform.SetParent(null, true);

        Rigidbody partRb = part.GetComponent<Rigidbody>();
        if (partRb == null) partRb = part.AddComponent<Rigidbody>();

        EnsureConvexColliders(part);

        Vector3 pushDir = (part.transform.position - transform.position).normalized;
        if (pushDir.sqrMagnitude < 0.01f) pushDir = transform.forward;
        partRb.AddForce(pushDir * 150f, ForceMode.Impulse);
        partRb.AddTorque(Random.insideUnitSphere * 50f, ForceMode.Impulse);
    }

    static void EnsureConvexColliders(GameObject part)
    {
        if (part.GetComponent<Collider>() != null) return;

        MeshFilter meshFilter = part.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            MeshCollider meshCollider = part.AddComponent<MeshCollider>();
            meshCollider.convex = true;
            return;
        }

        foreach (MeshFilter childMesh in part.GetComponentsInChildren<MeshFilter>())
        {
            if (childMesh.GetComponent<Collider>() != null) continue;
            MeshCollider childCollider = childMesh.gameObject.AddComponent<MeshCollider>();
            childCollider.convex = true;
        }
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
        if (isTotaled || damagePercent >= 100f) return "已报废";
        if (damagePercent >= 60f) return "严重受损";
        if (damagePercent >= 25f) return "轻微受损";
        return "正常";
    }
}
