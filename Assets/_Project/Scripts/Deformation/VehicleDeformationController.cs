using UnityEngine;

[DisallowMultipleComponent]
public class VehicleDeformationController : MonoBehaviour
{
    [SerializeField] DeformationConfig config;

    DeformablePart[] parts;

    DeformationConfig Config => CollisionConfigProvider.GetDeformationConfig(config);

    public void EnsurePartsReady() => VehicleDeformationSetup.FinalizeDeformableParts(gameObject);

    public void ApplyDeformation(CollisionEventData evt)
    {
        DeformationConfig cfg = Config;
        VehicleState state = GetComponent<VehicleState>();
        float depthRatio = 0f;

        DeformablePart target = evt.HitDeformable;
        if (target == null || target.PartType == VehiclePartType.Wheel)
            target = PartRegionRegistry.FindClosestPart(gameObject, evt.ContactPoint);

        if (target != null)
        {
            if (!target.IsReady)
                target.Initialize(gameObject);

            if (evt.Impulse >= cfg.deformThreshold)
            {
                float depth = VertexDeformer.Deform(
                    target,
                    evt.ContactPoint,
                    evt.ContactNormal,
                    evt.Impulse,
                    cfg);

                if (depth > 0f)
                {
                    cfg.GetPartSettings(target.PartType, out float maxDepth, out _);
                    depthRatio = maxDepth > 0f ? depth / maxDepth : 0f;
                }
            }
        }

        state?.ApplyCollisionDamage(evt.Impulse, depthRatio);
    }

    public void ResetDeformation()
    {
        if (parts == null)
            parts = GetComponentsInChildren<DeformablePart>(true);

        foreach (DeformablePart part in parts)
            part.ResetDeformation();
    }
}
