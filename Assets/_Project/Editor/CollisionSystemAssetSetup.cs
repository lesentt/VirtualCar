using UnityEditor;
using UnityEngine;

/// <summary>
/// 碰撞系统 ScriptableObject 资产创建（由 Setup Current Scene 自动调用）。
/// </summary>
public static class CollisionSystemAssetSetup
{
    const string SoRoot = "Assets/_Project/Resources/CollisionConfig";

    public static void CreateConfigAssetsSilent() => CreateConfigAssetsInternal();

    static void CreateConfigAssetsInternal()
    {
        EnsureFolder("Assets/_Project");
        EnsureFolder("Assets/_Project/Resources");
        EnsureFolder(SoRoot);

        CreateDeformationConfig();
        CreateCollisionEventChannel();
        CreateVehicleEntityProfile();
        CreateAudioProfile();
        CreateVfxProfile();
        CreateVehicleWearProfile();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static int EnableVehicleMeshReadWriteSilent() => EnableVehicleMeshReadWriteInternal();

    static int EnableVehicleMeshReadWriteInternal()
    {
        int count = 0;
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string lower = path.ToLowerInvariant();
            if (!lower.Contains("car") && !lower.Contains("police") && !lower.Contains("taxi"))
                continue;

            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null || importer.isReadable)
                continue;

            importer.isReadable = true;
            importer.SaveAndReimport();
            count++;
        }

        return count;
    }

    static void CreateDeformationConfig()
    {
        string path = SoRoot + "/DefaultDeformationConfig.asset";
        DeformationConfig config = LoadOrCreate<DeformationConfig>(path);
        config.deformThreshold = 500f;
        config.maxDeformDepth = 0.35f;
        config.deformRadius = 0.9f;
        config.falloff = 1.4f;
        config.accumulateRatio = 0.92f;
        config.deformDepthMultiplier = 4.5f;
        config.maxVerticesPerFrame = 500;
        config.minDamageImpulse = 450f;
        config.damageImpulseScale = 0.38f;
        config.depthDamageWeight = 0.07f;
        config.lightDamageThreshold = 6000f;
        config.heavyDamageThreshold = 20000f;
        config.totaledThreshold = 42000f;
        config.enableWear = true;
        config.wearThreshold = 300f;
        config.wearImpulseScale = 0.00032f;
        config.wearStrengthMin = 0.28f;
        config.wearMaskResolution = 512;
        config.wearStampRadius = 0.18f;
        config.partOverrides = new[]
        {
            new PartDeformOverride { part = VehiclePartType.FrontBumper, maxDepth = 0.45f, radius = 1.1f },
            new PartDeformOverride { part = VehiclePartType.Hood, maxDepth = 0.38f, radius = 1f },
            new PartDeformOverride { part = VehiclePartType.DoorFL, maxDepth = 0.32f, radius = 0.85f },
            new PartDeformOverride { part = VehiclePartType.DoorFR, maxDepth = 0.32f, radius = 0.85f },
            new PartDeformOverride { part = VehiclePartType.DoorRL, maxDepth = 0.32f, radius = 0.85f },
            new PartDeformOverride { part = VehiclePartType.DoorRR, maxDepth = 0.32f, radius = 0.85f },
            new PartDeformOverride { part = VehiclePartType.RearBumper, maxDepth = 0.38f, radius = 0.95f },
            new PartDeformOverride { part = VehiclePartType.Trunk, maxDepth = 0.35f, radius = 0.9f },
        };
        EditorUtility.SetDirty(config);
    }

    static void CreateCollisionEventChannel()
    {
        LoadOrCreate<CollisionEventChannel>(SoRoot + "/CollisionEventChannel.asset");
    }

    static void CreateVehicleEntityProfile()
    {
        string path = SoRoot + "/VehicleMetalProfile.asset";
        CollisionEntityProfile profile = LoadOrCreate<CollisionEntityProfile>(path);
        profile.surfaceMaterial = SurfaceMaterialType.Metal;
        profile.minReportSpeed = 0.5f;
        profile.restitution = 0.2f;
        EditorUtility.SetDirty(profile);
    }

    static void CreateAudioProfile()
    {
        LoadOrCreate<CollisionAudioProfile>(SoRoot + "/DefaultCollisionAudioProfile.asset");
        LoadOrCreate<ProjectAudioLibrary>(SoRoot + "/ProjectAudioLibrary.asset");
        ProjectAudioSetup.WireProjectAudioClips();
    }

    static void CreateVfxProfile()
    {
        LoadOrCreate<CollisionVFXProfile>(SoRoot + "/DefaultCollisionVFXProfile.asset");
    }

    static void CreateVehicleWearProfile()
    {
        string path = SoRoot + "/VehicleWearProfile.asset";
        VehicleWearProfile profile = LoadOrCreate<VehicleWearProfile>(path);
        profile.wearMetalColor = LoadTexture("Assets/_Project/Materials/Metal059C_1K-JPG/Metal059C_1K-JPG_Color.jpg");
        profile.wearMetalNormal = LoadTexture("Assets/_Project/Materials/Metal059C_1K-JPG/Metal059C_1K-JPG_NormalGL.jpg");
        profile.wearMetalRoughness = LoadTexture("Assets/_Project/Materials/Metal059C_1K-JPG/Metal059C_1K-JPG_Roughness.jpg");
        profile.metalTiling = 5f;
        profile.grimeAmount = 0.45f;
        profile.wearBlendPower = 2.2f;
        EditorUtility.SetDirty(profile);
    }

    static Texture2D LoadTexture(string assetPath) =>
        AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
            return asset;

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string name = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
