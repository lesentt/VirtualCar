using UnityEngine;

/// <summary>
/// 单部件顶点形变（设计文档 §6.2）。运行时复制 Mesh，不修改资产。
/// </summary>
[DisallowMultipleComponent]
public class DeformablePart : MonoBehaviour
{
    public VehiclePartType PartType;
    public MeshFilter meshFilter;
    public BoxCollider syncCollider;

    Mesh workingMesh;
    Vector3[] originalVerts;
    Vector3[] workingVerts;
    Vector3 originalColliderSize;
    Vector3 originalColliderCenter;
    float accumulatedDepth;
    GameObject vehicleRoot;
    bool initialized;

    public bool IsReady => initialized;
    public GameObject VehicleRoot => vehicleRoot;
    public BoxCollider SyncCollider => syncCollider;
    public float AccumulatedDepth => accumulatedDepth;
    public Vector3 OriginalColliderSize => originalColliderSize;
    public Vector3 OriginalColliderCenter => originalColliderCenter;

    public void Configure(GameObject root, VehiclePartType type, MeshFilter filter, BoxCollider collider)
    {
        vehicleRoot = root;
        PartType = type;
        meshFilter = filter;
        syncCollider = collider;
    }

    public bool Initialize(GameObject root)
    {
        if (initialized)
            return true;

        if (root != null)
            vehicleRoot = root;

        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();

        if (syncCollider == null)
            syncCollider = GetComponent<BoxCollider>();

        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogWarning($"[DeformablePart] {name} 缺少 MeshFilter/Mesh，跳过形变。");
            return false;
        }

        if (!Application.isPlaying)
            return false;

        Mesh source = meshFilter.sharedMesh;
        if (!source.isReadable)
        {
            Debug.LogError($"[DeformablePart] {name} Mesh 未启用 Read/Write。请在 FBX Import Settings 勾选 Read/Write Enabled。");
            return false;
        }

        workingMesh = Instantiate(source);
        workingMesh.name = source.name + "_Deform";
        workingMesh.MarkDynamic();
        meshFilter.mesh = workingMesh;
        originalVerts = workingMesh.vertices;
        workingVerts = (Vector3[])originalVerts.Clone();

        if (syncCollider != null)
        {
            originalColliderSize = syncCollider.size;
            originalColliderCenter = syncCollider.center;
        }

        initialized = true;
        return true;
    }

    void Start()
    {
        Initialize(transform.root.gameObject);
    }

    public void SetAccumulatedDepth(float depth) => accumulatedDepth = depth;

    /// <returns>本次实际修改的顶点数</returns>
    public int DeformVertices(Vector3 localHit, Vector3 localNormal, float depth,
        float radius, float falloff)
    {
        if (!initialized || workingVerts == null || depth <= 0f || radius <= 0f)
            return 0;

        if (!TryGetImpactSurface(localHit, out localHit, out Vector3 inward, out _, radius * 1.5f))
            inward = ResolveInwardNormal(localHit, localNormal);

        inward = inward.normalized;
        Vector3 outward = -inward;

        int affected = 0;
        float radiusSq = radius * radius;

        for (int i = 0; i < workingVerts.Length; i++)
        {
            Vector3 offset = workingVerts[i] - localHit;
            float distSq = offset.sqrMagnitude;
            if (distSq > radiusSq)
                continue;

            // 只形变撞击侧外表面，避免薄板背面跟着外凸
            if (Vector3.Dot(offset, outward) < -radius * 0.02f)
                continue;

            float dist = Mathf.Sqrt(distSq);
            float factor = Mathf.Pow(1f - dist / radius, falloff);
            workingVerts[i] += inward * depth * factor;
            affected++;
        }

        if (affected <= 0)
            return 0;

        workingMesh.vertices = workingVerts;
        workingMesh.RecalculateNormals();
        workingMesh.RecalculateBounds();
        return affected;
    }

    /// <summary>
    /// 将碰撞点投影到 Mesh 表面，并返回指向部件内部的法线。
    /// </summary>
    public bool TryGetImpactSurface(Vector3 localPoint, out Vector3 surfacePoint,
        out Vector3 inwardNormal, out Vector2 uv, float maxDistance = 1.5f)
    {
        surfacePoint = localPoint;
        inwardNormal = Vector3.forward;
        uv = Vector2.zero;

        Mesh mesh = workingMesh != null ? workingMesh : meshFilter != null ? meshFilter.sharedMesh : null;
        if (mesh == null)
            return false;

        Vector3[] verts = workingVerts ?? mesh.vertices;
        Vector2[] uvs = mesh.uv;
        int[] tris = mesh.triangles;
        if (verts == null || verts.Length == 0 || tris == null || tris.Length < 3)
            return false;

        float maxDistSq = maxDistance * maxDistance;
        float bestDistSq = maxDistSq;
        bool found = false;
        Vector3 bestNormal = Vector3.up;
        int bestI0 = 0;
        int bestI1 = 0;
        int bestI2 = 0;
        Vector3 bestClosest = localPoint;

        for (int i = 0; i < tris.Length; i += 3)
        {
            int i0 = tris[i];
            int i1 = tris[i + 1];
            int i2 = tris[i + 2];
            Vector3 a = verts[i0];
            Vector3 b = verts[i1];
            Vector3 c = verts[i2];
            Vector3 closest = ClosestPointOnTriangle(localPoint, a, b, c);
            float distSq = (closest - localPoint).sqrMagnitude;
            if (distSq >= bestDistSq)
                continue;

            bestDistSq = distSq;
            surfacePoint = closest;
            bestClosest = closest;
            bestNormal = Vector3.Cross(b - a, c - a).normalized;
            bestI0 = i0;
            bestI1 = i1;
            bestI2 = i2;
            found = true;
        }

        if (!found)
            return false;

        inwardNormal = ResolveInwardNormal(surfacePoint, bestNormal);

        if (uvs != null && uvs.Length == verts.Length)
        {
            ComputeBarycentric(bestClosest, verts[bestI0], verts[bestI1], verts[bestI2],
                out float wa, out float wb, out float wc);
            uv = uvs[bestI0] * wa + uvs[bestI1] * wb + uvs[bestI2] * wc;
        }
        else
        {
            uv = PlanarFallbackUv(surfacePoint, inwardNormal, mesh.bounds);
        }

        return true;
    }

    static Vector2 PlanarFallbackUv(Vector3 localPoint, Vector3 inwardNormal, Bounds bounds)
    {
        Vector3 abs = new Vector3(Mathf.Abs(inwardNormal.x), Mathf.Abs(inwardNormal.y), Mathf.Abs(inwardNormal.z));
        if (abs.y >= abs.x && abs.y >= abs.z)
            return new Vector2(
                Mathf.InverseLerp(bounds.min.x, bounds.max.x, localPoint.x),
                Mathf.InverseLerp(bounds.min.z, bounds.max.z, localPoint.z));
        if (abs.x >= abs.z)
            return new Vector2(
                Mathf.InverseLerp(bounds.min.z, bounds.max.z, localPoint.z),
                Mathf.InverseLerp(bounds.min.y, bounds.max.y, localPoint.y));
        return new Vector2(
            Mathf.InverseLerp(bounds.min.x, bounds.max.x, localPoint.x),
            Mathf.InverseLerp(bounds.min.y, bounds.max.y, localPoint.y));
    }

    /// <summary>
    /// Unity 接触法线指向「其它碰撞体 → 当前碰撞体」，即通常指向部件内部。
    /// </summary>
    public Vector3 ResolveInwardNormal(Vector3 localPoint, Vector3 localNormal)
    {
        Vector3 n = localNormal.sqrMagnitude > 0.0001f ? localNormal.normalized : Vector3.forward;
        Vector3 toCenter = workingMesh != null ? workingMesh.bounds.center - localPoint : Vector3.zero;
        if (toCenter.sqrMagnitude > 0.0001f && Vector3.Dot(n, toCenter) < 0f)
            n = -n;
        return n;
    }

    public void ResetDeformation()
    {
        if (!initialized || workingVerts == null)
            return;

        workingVerts = (Vector3[])originalVerts.Clone();
        workingMesh.vertices = workingVerts;
        workingMesh.RecalculateNormals();
        workingMesh.RecalculateBounds();
        accumulatedDepth = 0f;

        if (syncCollider != null)
        {
            syncCollider.size = originalColliderSize;
            syncCollider.center = originalColliderCenter;
        }

        GetComponent<PartWearApplicator>()?.ClearWear();
    }

    public bool TryGetNearestUv(Vector3 localPoint, out Vector2 uv, float maxDistance = 0.6f)
    {
        uv = Vector2.zero;
        Mesh mesh = workingMesh != null ? workingMesh : meshFilter != null ? meshFilter.sharedMesh : null;
        if (mesh == null)
            return false;

        Vector3[] verts = workingVerts ?? mesh.vertices;
        Vector2[] uvs = mesh.uv;
        int[] tris = mesh.triangles;
        if (verts == null || verts.Length == 0)
            return false;

        if (uvs == null || uvs.Length != verts.Length || tris == null || tris.Length < 3)
        {
            Bounds bounds = mesh.bounds;
            if (bounds.size.sqrMagnitude <= 0.0001f)
                return false;

            uv = new Vector2(
                Mathf.InverseLerp(bounds.min.x, bounds.max.x, localPoint.x),
                Mathf.InverseLerp(bounds.min.z, bounds.max.z, localPoint.z));
            return true;
        }

        float maxDistSq = maxDistance * maxDistance;
        float bestDistSq = maxDistSq;
        bool found = false;

        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 a = verts[tris[i]];
            Vector3 b = verts[tris[i + 1]];
            Vector3 c = verts[tris[i + 2]];
            Vector3 closest = ClosestPointOnTriangle(localPoint, a, b, c);
            float distSq = (closest - localPoint).sqrMagnitude;
            if (distSq >= bestDistSq)
                continue;

            bestDistSq = distSq;
            ComputeBarycentric(closest, a, b, c, out float wa, out float wb, out float wc);
            uv = uvs[tris[i]] * wa + uvs[tris[i + 1]] * wb + uvs[tris[i + 2]] * wc;
            found = true;
        }

        return found;
    }

    static Vector3 ClosestPointOnTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 ab = b - a;
        Vector3 ac = c - a;
        Vector3 ap = p - a;
        float d1 = Vector3.Dot(ab, ap);
        float d2 = Vector3.Dot(ac, ap);
        if (d1 <= 0f && d2 <= 0f)
            return a;

        Vector3 bp = p - b;
        float d3 = Vector3.Dot(ab, bp);
        float d4 = Vector3.Dot(ac, bp);
        if (d3 >= 0f && d4 <= d3)
            return b;

        float vc = d1 * d4 - d3 * d2;
        if (vc <= 0f && d1 >= 0f && d3 <= 0f)
        {
            float v = d1 / (d1 - d3);
            return a + ab * v;
        }

        Vector3 cp = p - c;
        float d5 = Vector3.Dot(ab, cp);
        float d6 = Vector3.Dot(ac, cp);
        if (d6 >= 0f && d5 <= d6)
            return c;

        float vb = d5 * d2 - d1 * d6;
        if (vb <= 0f && d2 >= 0f && d6 <= 0f)
        {
            float w = d2 / (d2 - d6);
            return a + ac * w;
        }

        float va = d3 * d6 - d5 * d4;
        if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
        {
            float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
            return b + (c - b) * w;
        }

        float denom = 1f / (va + vb + vc);
        float v2 = vb * denom;
        float w2 = vc * denom;
        return a + ab * v2 + ac * w2;
    }

    static void ComputeBarycentric(Vector3 p, Vector3 a, Vector3 b, Vector3 c,
        out float wa, out float wb, out float wc)
    {
        Vector3 v0 = b - a;
        Vector3 v1 = c - a;
        Vector3 v2 = p - a;
        float d00 = Vector3.Dot(v0, v0);
        float d01 = Vector3.Dot(v0, v1);
        float d11 = Vector3.Dot(v1, v1);
        float d20 = Vector3.Dot(v2, v0);
        float d21 = Vector3.Dot(v2, v1);
        float denom = d00 * d11 - d01 * d01;
        if (Mathf.Abs(denom) <= 0.000001f)
        {
            wa = 1f;
            wb = 0f;
            wc = 0f;
            return;
        }

        float inv = 1f / denom;
        wb = (d11 * d20 - d01 * d21) * inv;
        wc = (d00 * d21 - d01 * d20) * inv;
        wa = 1f - wb - wc;
    }
}
