using UnityEngine;

public class DamageEvaluator
{
    readonly DeformationConfig config;
    float accumulatedDamage;

    public float AccumulatedImpulse => accumulatedDamage;
    public float DamagePercent { get; private set; }

    public DamageEvaluator(DeformationConfig deformationConfig)
    {
        config = deformationConfig;
    }

    /// <summary>
    /// 每次碰撞按「本次冲量 + 本次形变深度」增量累加损伤。
    /// </summary>
    public DamageLevel Evaluate(float impulse, float incrementalDepthRatio, float sensitivity = 1f)
    {
        float hitDamage = 0f;
        float effectiveImpulse = ScaleImpulseForDamage(impulse);

        if (effectiveImpulse >= config.minDamageImpulse)
        {
            float impulsePercent = config.totaledThreshold > 0f
                ? (effectiveImpulse / config.totaledThreshold) * 100f
                : 0f;
            hitDamage += impulsePercent * config.damageImpulseScale * sensitivity;
        }

        if (incrementalDepthRatio > 0f)
        {
            float depthHit = incrementalDepthRatio * config.depthDamageWeight * 100f * sensitivity;
            depthHit *= GetLightImpactDepthScale(impulse);
            hitDamage += depthHit;
        }

        accumulatedDamage = Mathf.Min(100f, accumulatedDamage + hitDamage);
        DamagePercent = accumulatedDamage;
        return LevelFromPercent(DamagePercent);
    }

    /// <summary>
    /// 低速轻碰时降低有效冲量，但保留一定损伤贡献，避免「一蹭就 30%」或「完全不涨」两个极端。
    /// </summary>
    float ScaleImpulseForDamage(float impulse)
    {
        if (impulse <= config.lightDamageThreshold)
        {
            float t = Mathf.Clamp01(impulse / Mathf.Max(config.lightDamageThreshold, 1f));
            float softScale = Mathf.Lerp(0.15f, 1f, t * t * t);
            return impulse * softScale;
        }

        return impulse;
    }

    float GetLightImpactDepthScale(float impulse)
    {
        if (impulse >= config.lightDamageThreshold)
            return 1f;

        float t = Mathf.Clamp01(impulse / Mathf.Max(config.lightDamageThreshold, 1f));
        return t * t;
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
        accumulatedDamage = 0f;
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
