using UnityEngine;

public class DamageEvaluator
{
    readonly DeformationConfig config;
    float accumulatedImpulse;
    float maxDepthRatio;

    public float AccumulatedImpulse => accumulatedImpulse;
    public float DamagePercent { get; private set; }

    public DamageEvaluator(DeformationConfig deformationConfig)
    {
        config = deformationConfig;
    }

    /// <summary>
    /// 根据冲量与形变深度更新损伤百分比，返回损伤等级。
    /// </summary>
    public DamageLevel Evaluate(float impulse, float partDepthRatio, float sensitivity = 1f)
    {
        float effectiveImpulse = ScaleImpulseForDamage(impulse);

        if (effectiveImpulse >= config.minDamageImpulse)
        {
            float scaled = effectiveImpulse * config.damageImpulseScale * sensitivity;
            accumulatedImpulse += scaled;
        }

        maxDepthRatio = Mathf.Max(maxDepthRatio, partDepthRatio);

        float impulsePercent = config.totaledThreshold > 0f
            ? (accumulatedImpulse / config.totaledThreshold) * 100f
            : 0f;

        float depthPercent = maxDepthRatio * 100f * config.depthDamageWeight;
        DamagePercent = Mathf.Clamp(Mathf.Max(impulsePercent, depthPercent), 0f, 100f);

        return LevelFromPercent(DamagePercent);
    }

    /// <summary>
    /// 低速轻碰时降低有效冲量，避免轻轻一蹭就满损。
    /// </summary>
    float ScaleImpulseForDamage(float impulse)
    {
        if (impulse <= config.lightDamageThreshold)
        {
            float t = impulse / Mathf.Max(config.lightDamageThreshold, 1f);
            return impulse * t * t;
        }

        return impulse;
    }

    static DamageLevel LevelFromPercent(float percent)
    {
        if (percent >= 85f) return DamageLevel.Totaled;
        if (percent >= 55f) return DamageLevel.Heavy;
        if (percent >= 25f) return DamageLevel.Light;
        return DamageLevel.Intact;
    }

    public void Reset()
    {
        accumulatedImpulse = 0f;
        maxDepthRatio = 0f;
        DamagePercent = 0f;
    }

    public static float GetDriveMultiplier(DamageLevel level)
    {
        switch (level)
        {
            case DamageLevel.Light: return 0.9f;
            case DamageLevel.Heavy: return 0.7f;
            case DamageLevel.Totaled: return 0.2f;
            default: return 1f;
        }
    }

    public static float GetSteerMultiplier(DamageLevel level)
    {
        switch (level)
        {
            case DamageLevel.Light: return 0.95f;
            case DamageLevel.Heavy: return 0.6f;
            case DamageLevel.Totaled: return 0.4f;
            default: return 1f;
        }
    }
}
