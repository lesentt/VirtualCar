using UnityEngine;

[DisallowMultipleComponent]
public class VehicleState : MonoBehaviour
{
    [LabelText("损伤敏感度")]
    [Tooltip("越小越耐撞。轿车建议 0.25~0.35，警车建议 0.55~0.7")]
    [Range(0.1f, 2f)]
    public float damageSensitivity = 0.8f;

    DamageLevel damageLevel = DamageLevel.Intact;
    float damagePercent;
    float lastImpulse;
    int collisionCount;

    CarController carController;
    VehicleDeformationController deformationController;
    DamageEvaluator evaluator;

    public DamageLevel CurrentDamageLevel => damageLevel;
    public float DamagePercent => damagePercent;
    public float LastImpulse => lastImpulse;
    public int CollisionCount => collisionCount;
    public DamageEvaluator Evaluator => evaluator ?? (evaluator = new DamageEvaluator(CollisionConfigProvider.GetDeformationConfig()));

    void Awake()
    {
        carController = GetComponent<CarController>();
        deformationController = GetComponent<VehicleDeformationController>();
        evaluator = new DamageEvaluator(CollisionConfigProvider.GetDeformationConfig());
    }

    public void OnCollision(CollisionEventData evt)
    {
        lastImpulse = evt.Impulse;
        collisionCount++;
    }

    public void ApplyCollisionDamage(float impulse, float partDepthRatio)
    {
        DamageLevel level = Evaluator.Evaluate(impulse, partDepthRatio, damageSensitivity);
        damagePercent = Evaluator.DamagePercent;
        damageLevel = level;
        ApplyDrivePerformance();
    }

    void ApplyDrivePerformance()
    {
        if (carController == null)
            return;

        carController.SetDriveMultiplier(DamageEvaluator.GetDriveMultiplier(damageLevel));

        if (damageLevel == DamageLevel.Totaled)
            carController.SetDisabled(true);
    }

    public string GetStatusText()
    {
        switch (damageLevel)
        {
            case DamageLevel.Totaled: return "报废";
            case DamageLevel.Heavy: return "重损";
            case DamageLevel.Light: return "轻损";
            default: return "完好";
        }
    }

    public float GetDamagePercentApprox() => damagePercent;
    public float GetDamageSensitivity() => damageSensitivity;

    public void SetDamageSensitivity(float sensitivity)
    {
        damageSensitivity = Mathf.Clamp(sensitivity, 0.1f, 2f);
    }

    public void ResetState()
    {
        damageLevel = DamageLevel.Intact;
        damagePercent = 0f;
        lastImpulse = 0f;
        collisionCount = 0;
        Evaluator.Reset();
        carController?.SetDisabled(false);
        carController?.SetDriveMultiplier(1f);
        deformationController?.ResetDeformation();
    }
}
