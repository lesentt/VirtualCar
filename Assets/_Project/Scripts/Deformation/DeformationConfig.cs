using UnityEngine;

[CreateAssetMenu(fileName = "DeformationConfig", menuName = "VirtualVehicle/Deformation Config")]
public class DeformationConfig : ScriptableObject
{
    [Header("形变")]
    public float deformThreshold = 500f;
    public float maxDeformDepth = 0.35f;
    public float deformRadius = 0.9f;
    public float falloff = 1.4f;
    public float accumulateRatio = 0.92f;
    public float deformDepthMultiplier = 4.5f;
    public int maxVerticesPerFrame = 500;

    [Header("损伤（独立于形变阈值）")]
    public float minDamageImpulse = 500f;
    public float damageImpulseScale = 0.28f;
    public float depthDamageWeight = 0.3f;
    public float lightDamageThreshold = 6000f;
    public float heavyDamageThreshold = 20000f;
    public float totaledThreshold = 50000f;

    public PartDeformOverride[] partOverrides;

    public void GetPartSettings(VehiclePartType part, out float maxDepth, out float radius)
    {
        maxDepth = maxDeformDepth;
        radius = deformRadius;

        if (partOverrides == null)
            return;

        foreach (PartDeformOverride o in partOverrides)
        {
            if (o.part != part)
                continue;
            maxDepth = o.maxDepth;
            radius = o.radius;
            return;
        }
    }
}

[System.Serializable]
public struct PartDeformOverride
{
    public VehiclePartType part;
    public float maxDepth;
    public float radius;
}
