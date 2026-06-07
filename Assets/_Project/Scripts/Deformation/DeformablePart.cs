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

        localHit = SnapToMeshSurface(localHit, radius * 1.5f);

        int affected = 0;
        float radiusSq = radius * radius;
        Vector3 pushDir = -localNormal.normalized;

        for (int i = 0; i < workingVerts.Length; i++)
        {
            float distSq = (workingVerts[i] - localHit).sqrMagnitude;
            if (distSq > radiusSq)
                continue;

            float dist = Mathf.Sqrt(distSq);
            float factor = Mathf.Pow(1f - dist / radius, falloff);
            workingVerts[i] += pushDir * depth * factor;
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
    /// 碰撞点可能在 Collider 外表面，与 Mesh 顶点有偏差；对齐到最近顶点再形变。
    /// </summary>
    Vector3 SnapToMeshSurface(Vector3 localHit, float maxDistance)
    {
        float maxDistSq = maxDistance * maxDistance;
        int bestIndex = -1;
        float bestDistSq = maxDistSq;

        for (int i = 0; i < workingVerts.Length; i++)
        {
            float distSq = (workingVerts[i] - localHit).sqrMagnitude;
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                bestIndex = i;
            }
        }

        return bestIndex >= 0 ? workingVerts[bestIndex] : localHit;
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
    }
}
