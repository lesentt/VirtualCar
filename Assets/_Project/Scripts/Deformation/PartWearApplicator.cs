using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(DeformablePart))]
public class PartWearApplicator : MonoBehaviour
{
    const int ShaderStampCapacity = 32;

    static readonly int WearStampsId = Shader.PropertyToID("_WearStamps");
    static readonly int WearStrengthsId = Shader.PropertyToID("_WearStrengths");

    readonly List<WearStamp> stamps = new List<WearStamp>(ShaderStampCapacity);
    readonly Vector4[] stampVectors = new Vector4[ShaderStampCapacity];
    readonly float[] stampStrengths = new float[ShaderStampCapacity];

    struct WearStamp
    {
        public Vector3 LocalPos;
        public float Radius;
        public float Strength;
    }

    DeformablePart deformablePart;
    MeshRenderer meshRenderer;
    Material[] originalMaterials;
    Material[] wearMaterials;
    int maxStamps = ShaderStampCapacity;
    bool initialized;

    public bool IsReady => initialized;

    public bool Initialize(GameObject vehicleRoot, VehicleWearProfile profile, int stampCapacity)
    {
        if (initialized)
            return true;

        if (!Application.isPlaying)
            return false;

        maxStamps = Mathf.Clamp(stampCapacity, 4, ShaderStampCapacity);

        deformablePart = GetComponent<DeformablePart>();
        meshRenderer = GetComponent<MeshRenderer>();
        if (deformablePart == null || meshRenderer == null || profile == null)
            return false;

        if (!deformablePart.IsReady && !deformablePart.Initialize(vehicleRoot))
            return false;

        Shader wearShader = Shader.Find("VirtualVehicle/VehicleBodyWear");
        if (wearShader == null)
        {
            Debug.LogError("[PartWearApplicator] 找不到 VirtualVehicle/VehicleBodyWear Shader。");
            return false;
        }

        stamps.Clear();
        originalMaterials = meshRenderer.sharedMaterials;
        wearMaterials = new Material[originalMaterials.Length];

        for (int i = 0; i < originalMaterials.Length; i++)
        {
            Material source = originalMaterials[i];
            if (!ShouldApplyWear(source))
            {
                wearMaterials[i] = source;
                continue;
            }

            Material wearMat = new Material(wearShader)
            {
                name = source != null ? source.name + "_Wear" : "WearMaterial"
            };
            CopyPaintProperties(source, wearMat);
            ApplyProfileTextures(wearMat, profile);
            wearMaterials[i] = wearMat;
        }

        meshRenderer.materials = wearMaterials;
        ApplyStampsToMaterials();
        initialized = true;
        return true;
    }

    public void ApplyImpact(Vector3 worldContact, float impulse, DeformationConfig config)
    {
        if (!initialized || config == null || impulse < config.wearThreshold)
            return;

        Transform partTransform = deformablePart.meshFilter != null
            ? deformablePart.meshFilter.transform
            : transform;

        Vector3 localHit = partTransform.InverseTransformPoint(worldContact);
        if (deformablePart.TryGetImpactSurface(localHit, out Vector3 surfaceHit, out _, out _, 2f))
            localHit = surfaceHit;

        config.GetPartSettings(deformablePart.PartType, out _, out float deformRadius);
        float radiusScale = deformRadius > 0f ? deformRadius / config.deformRadius : 1f;
        float strength = impulse * config.wearImpulseScale * radiusScale;
        strength = Mathf.Clamp(Mathf.Max(strength, config.wearStrengthMin), 0f, 1f);
        float radiusMeters = deformRadius * config.wearStampRadius * Mathf.Lerp(0.85f, 1.45f, strength);

        AddStamp(localHit, radiusMeters, strength);
    }

    public void ResetWear()
    {
        if (!initialized)
            return;

        ClearWear();

        if (meshRenderer != null && originalMaterials != null)
            meshRenderer.sharedMaterials = originalMaterials;

        DestroyWearMaterials();
        originalMaterials = null;
        initialized = false;
    }

    public void ClearWear()
    {
        if (!initialized)
            return;

        stamps.Clear();
        ApplyStampsToMaterials();
    }

    void OnDestroy()
    {
        if (!initialized)
            return;

        DestroyWearMaterials();
    }

    void AddStamp(Vector3 localPos, float radius, float strength)
    {
        for (int i = 0; i < stamps.Count; i++)
        {
            WearStamp existing = stamps[i];
            if (Vector3.Distance(existing.LocalPos, localPos) > radius * 0.35f)
                continue;

            existing.LocalPos = Vector3.Lerp(existing.LocalPos, localPos, 0.45f);
            existing.Radius = Mathf.Max(existing.Radius, radius);
            existing.Strength = Mathf.Max(existing.Strength, strength);
            stamps[i] = existing;
            ApplyStampsToMaterials();
            return;
        }

        if (stamps.Count >= maxStamps)
            stamps.RemoveAt(0);

        stamps.Add(new WearStamp
        {
            LocalPos = localPos,
            Radius = radius,
            Strength = strength
        });

        ApplyStampsToMaterials();
    }

    void ApplyStampsToMaterials()
    {
        if (wearMaterials == null)
            return;

        for (int i = 0; i < ShaderStampCapacity; i++)
        {
            if (i < stamps.Count)
            {
                WearStamp stamp = stamps[i];
                stampVectors[i] = new Vector4(stamp.LocalPos.x, stamp.LocalPos.y, stamp.LocalPos.z, stamp.Radius);
                stampStrengths[i] = stamp.Strength;
            }
            else
            {
                stampVectors[i] = Vector4.zero;
                stampStrengths[i] = 0f;
            }
        }

        for (int i = 0; i < wearMaterials.Length; i++)
        {
            Material mat = wearMaterials[i];
            if (mat == null || originalMaterials != null && mat == originalMaterials[i])
                continue;

            mat.SetVectorArray(WearStampsId, stampVectors);
            mat.SetFloatArray(WearStrengthsId, stampStrengths);
        }
    }

    void DestroyWearMaterials()
    {
        if (wearMaterials == null)
            return;

        for (int i = 0; i < wearMaterials.Length; i++)
        {
            Material mat = wearMaterials[i];
            if (mat == null || originalMaterials != null && i < originalMaterials.Length && mat == originalMaterials[i])
                continue;
            Destroy(mat);
        }

        wearMaterials = null;
    }

    static bool ShouldApplyWear(Material mat)
    {
        if (mat == null)
            return false;

        string shaderName = mat.shader != null ? mat.shader.name : string.Empty;
        if (shaderName.Contains("Transparent") || shaderName.Contains("Glass") || shaderName.Contains("Water"))
            return false;

        if (mat.HasProperty("_Mode") && mat.GetFloat("_Mode") >= 2f)
            return false;

        return true;
    }

    static void CopyPaintProperties(Material source, Material target)
    {
        if (source == null)
            return;

        if (source.HasProperty("_Color") && target.HasProperty("_Color"))
            target.SetColor("_Color", source.GetColor("_Color"));

        if (source.HasProperty("_MainTex") && target.HasProperty("_MainTex"))
            target.SetTexture("_MainTex", source.GetTexture("_MainTex"));

        if (source.HasProperty("_Glossiness") && target.HasProperty("_Glossiness"))
            target.SetFloat("_Glossiness", source.GetFloat("_Glossiness"));
        else if (source.HasProperty("_Smoothness") && target.HasProperty("_Glossiness"))
            target.SetFloat("_Glossiness", source.GetFloat("_Smoothness"));

        if (source.HasProperty("_Metallic") && target.HasProperty("_Metallic"))
            target.SetFloat("_Metallic", source.GetFloat("_Metallic"));
    }

    static void ApplyProfileTextures(Material mat, VehicleWearProfile profile)
    {
        mat.SetTexture("_WearMetalTex", profile.wearMetalColor);
        mat.SetTexture("_WearMetalNormal", profile.wearMetalNormal);
        mat.SetTexture("_WearMetalRough", profile.wearMetalRoughness);
        mat.SetFloat("_WearTiling", profile.metalTiling);
        mat.SetFloat("_WearGrime", profile.grimeAmount);
        mat.SetFloat("_WearBlendPower", profile.wearBlendPower);
    }
}
