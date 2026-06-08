using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(DeformablePart))]
public class PartWearApplicator : MonoBehaviour
{
    static readonly int WearMaskId = Shader.PropertyToID("_WearMask");
    static Material stampMaterial;

    DeformablePart deformablePart;
    MeshRenderer meshRenderer;
    RenderTexture wearMask;
    Material[] originalMaterials;
    Material[] wearMaterials;
    bool initialized;

    public bool IsReady => initialized;
    public RenderTexture WearMask => wearMask;

    public bool Initialize(GameObject vehicleRoot, VehicleWearProfile profile, int maskResolution)
    {
        if (initialized)
            return true;

        if (!Application.isPlaying)
            return false;

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

        EnsureStampMaterial();

        wearMask = new RenderTexture(maskResolution, maskResolution, 0, RenderTextureFormat.R8)
        {
            name = $"{name}_WearMask",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            useMipMap = false,
            autoGenerateMips = false
        };
        wearMask.Create();
        ClearWearMask();

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
            wearMat.SetTexture(WearMaskId, wearMask);
            wearMaterials[i] = wearMat;
        }

        meshRenderer.materials = wearMaterials;
        initialized = true;
        return true;
    }

    public void ApplyImpact(Vector3 worldContact, float impulse, DeformationConfig config)
    {
        if (!initialized || wearMask == null || config == null || impulse < config.wearThreshold)
            return;

        Transform partTransform = deformablePart.meshFilter != null
            ? deformablePart.meshFilter.transform
            : transform;

        Vector3 localHit = partTransform.InverseTransformPoint(worldContact);
        if (!deformablePart.TryGetNearestUv(localHit, out Vector2 uv))
            return;

        config.GetPartSettings(deformablePart.PartType, out _, out float deformRadius);
        float radiusScale = deformRadius > 0f ? deformRadius / config.deformRadius : 1f;
        float strength = Mathf.Clamp01(impulse * config.wearImpulseScale * radiusScale);
        float stampRadius = config.wearStampRadius * Mathf.Lerp(0.75f, 1.35f, strength);

        StampWearMask(uv, stampRadius, strength);
    }

    public void ResetWear()
    {
        if (!initialized)
            return;

        ClearWear();

        if (meshRenderer != null && originalMaterials != null)
            meshRenderer.sharedMaterials = originalMaterials;

        DestroyWearMaterials();

        if (wearMask != null)
        {
            wearMask.Release();
            Destroy(wearMask);
            wearMask = null;
        }

        originalMaterials = null;
        initialized = false;
    }

    public void ClearWear()
    {
        if (!initialized || wearMask == null)
            return;

        ClearWearMask();
    }

    void OnDestroy()
    {
        if (!initialized)
            return;

        DestroyWearMaterials();

        if (wearMask != null)
        {
            wearMask.Release();
            Destroy(wearMask);
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

    void StampWearMask(Vector2 uv, float radius, float strength)
    {
        EnsureStampMaterial();
        stampMaterial.SetVector("_StampUv", new Vector4(uv.x, uv.y, 0f, 0f));
        stampMaterial.SetFloat("_StampRadius", radius);
        stampMaterial.SetFloat("_Strength", strength);

        RenderTexture temp = RenderTexture.GetTemporary(
            wearMask.width, wearMask.height, 0, wearMask.format);
        Graphics.Blit(wearMask, temp, stampMaterial);
        Graphics.Blit(temp, wearMask);
        RenderTexture.ReleaseTemporary(temp);
    }

    void ClearWearMask()
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = wearMask;
        GL.Clear(false, true, Color.black);
        RenderTexture.active = previous;
    }

    static void EnsureStampMaterial()
    {
        if (stampMaterial != null)
            return;

        Shader stampShader = Shader.Find("Hidden/VirtualVehicle/WearMaskStamp");
        if (stampShader == null)
        {
            Debug.LogError("[PartWearApplicator] 找不到 WearMaskStamp Shader。");
            return;
        }

        stampMaterial = new Material(stampShader);
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
        mat.SetTexture("_MetalLightTex", profile.metalLightColor);
        mat.SetTexture("_MetalHeavyTex", profile.metalHeavyColor);
        mat.SetTexture("_MetalLightNormal", profile.metalLightNormal);
        mat.SetTexture("_MetalHeavyNormal", profile.metalHeavyNormal);
        mat.SetTexture("_MetalLightRough", profile.metalLightRoughness);
        mat.SetTexture("_MetalHeavyRough", profile.metalHeavyRoughness);
        mat.SetFloat("_WearTiling", profile.metalTiling);
        mat.SetFloat("_WearGrime", profile.grimeAmount);
    }
}
