using UnityEngine;

public static class VertexDeformer
{
    public static float Deform(DeformablePart part, Vector3 worldContact, Vector3 worldNormal,
        float impulse, DeformationConfig config)
    {
        if (part == null || config == null)
            return 0f;

        if (impulse < config.deformThreshold)
            return 0f;

        GameObject root = part.VehicleRoot != null ? part.VehicleRoot : part.transform.root.gameObject;
        if (!part.IsReady && !part.Initialize(root))
            return 0f;

        config.GetPartSettings(part.PartType, out float maxDepth, out float radius);

        float range = config.heavyDamageThreshold - config.deformThreshold;
        float k = range > 0f ? maxDepth / range : maxDepth;
        float depth = Mathf.Min(maxDepth, k * impulse * config.deformDepthMultiplier);
        float totalDepth = Mathf.Min(maxDepth, part.AccumulatedDepth + depth * config.accumulateRatio);

        Transform t = part.meshFilter != null ? part.meshFilter.transform : part.transform;
        Vector3 localHit = t.InverseTransformPoint(worldContact);
        Vector3 localNormal = t.InverseTransformDirection(worldNormal).normalized;
        if (localNormal.sqrMagnitude < 0.001f)
            localNormal = Vector3.forward;

        int affected = part.DeformVertices(localHit, localNormal, depth, radius, config.falloff);
        if (affected <= 0)
            return 0f;

        part.SetAccumulatedDepth(totalDepth);

        if (part.SyncCollider != null)
        {
            ColliderSyncService.SyncBox(
                part.SyncCollider,
                localHit,
                localNormal,
                totalDepth,
                radius,
                part.OriginalColliderSize,
                part.OriginalColliderCenter);
        }

        return totalDepth;
    }
}
