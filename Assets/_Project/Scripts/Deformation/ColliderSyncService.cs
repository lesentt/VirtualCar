using UnityEngine;

public static class ColliderSyncService
{
    public static void SyncBox(BoxCollider box, Vector3 localHit, Vector3 localNormal,
        float depth, float radius, Vector3 originalSize, Vector3 originalCenter)
    {
        if (box == null || depth <= 0f)
            return;

        Vector3 absNormal = new Vector3(
            Mathf.Abs(localNormal.x),
            Mathf.Abs(localNormal.y),
            Mathf.Abs(localNormal.z));

        Vector3 size = box.size;
        Vector3 center = box.center;

        float compression = Mathf.Clamp(depth / GetPrimaryAxisSize(originalSize, absNormal), 0f, 0.4f);
        ApplyAxisCompression(ref size, ref center, localNormal, compression, depth, originalSize, originalCenter);

        box.size = size;
        box.center = center;
    }

    static float GetPrimaryAxisSize(Vector3 size, Vector3 absNormal)
    {
        if (absNormal.x >= absNormal.y && absNormal.x >= absNormal.z) return size.x;
        if (absNormal.y >= absNormal.x && absNormal.y >= absNormal.z) return size.y;
        return size.z;
    }

    static void ApplyAxisCompression(ref Vector3 size, ref Vector3 center, Vector3 localNormal,
        float compression, float depth, Vector3 originalSize, Vector3 originalCenter)
    {
        Vector3 absNormal = new Vector3(
            Mathf.Abs(localNormal.x),
            Mathf.Abs(localNormal.y),
            Mathf.Abs(localNormal.z));

        if (absNormal.z >= absNormal.x && absNormal.z >= absNormal.y)
        {
            size.z = originalSize.z * (1f - compression);
            center.z = originalCenter.z + localNormal.z * depth * 0.5f;
        }
        else if (absNormal.x >= absNormal.y)
        {
            size.x = originalSize.x * (1f - compression);
            center.x = originalCenter.x + localNormal.x * depth * 0.5f;
        }
        else
        {
            size.y = originalSize.y * (1f - compression);
            center.y = originalCenter.y + localNormal.y * depth * 0.5f;
        }
    }
}
