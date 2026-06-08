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
        float incrementalDepthRatio = 0f;

        DeformablePart target = evt.HitDeformable;
        if (target == null || target.PartType == VehiclePartType.Wheel)
            target = PartRegionRegistry.FindClosestPart(gameObject, evt.ContactPoint);

        if (target != null)
        {
            if (!target.IsReady)
                target.Initialize(gameObject);

            float prevDepth = target.AccumulatedDepth;

            if (evt.Impulse >= cfg.deformThreshold)
            {
                float totalDepth = VertexDeformer.Deform(
                    target,
                    evt.ContactPoint,
                    evt.ContactNormal,
                    evt.Impulse,
                    cfg);

                if (totalDepth > prevDepth)
                {
                    cfg.GetPartSettings(target.PartType, out float maxDepth, out _);
                    incrementalDepthRatio = maxDepth > 0f ? (totalDepth - prevDepth) / maxDepth : 0f;
                }
            }

            if (cfg.enableWear)
                ApplyWear(target, evt, cfg);
        }

        state?.ApplyCollisionDamage(evt.Impulse, incrementalDepthRatio);
    }

    public void ResetDeformation()
    {
        if (parts == null)
            parts = GetComponentsInChildren<DeformablePart>(true);

        foreach (DeformablePart part in parts)
            part.ResetDeformation();
    }

    static void ApplyWear(DeformablePart target, CollisionEventData evt, DeformationConfig cfg)
    {
        if (target == null || evt.Impulse < cfg.wearThreshold)
            return;

        PartWearApplicator wear = target.GetComponent<PartWearApplicator>();
        if (wear == null)
            return;

        GameObject root = target.VehicleRoot != null ? target.VehicleRoot : target.transform.root.gameObject;
        VehicleWearProfile profile = CollisionConfigProvider.WearProfile;
        if (profile == null)
            return;

        if (!wear.IsReady && !wear.Initialize(root, profile, cfg.maxWearStamps))
            return;

        wear.ApplyImpact(evt.ContactPoint, evt.Impulse, cfg);
    }
}
