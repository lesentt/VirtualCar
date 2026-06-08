using UnityEngine;

/// <summary>
/// 碰撞系统 ScriptableObject 统一加载入口。
/// </summary>
public static class CollisionConfigProvider
{
    const string DeformPath = "CollisionConfig/DefaultDeformationConfig";
    const string ChannelPath = "CollisionConfig/CollisionEventChannel";
    const string MetalProfilePath = "CollisionConfig/VehicleMetalProfile";
    const string AudioProfilePath = "CollisionConfig/DefaultCollisionAudioProfile";
    const string AudioLibraryPath = "CollisionConfig/ProjectAudioLibrary";
    const string VfxProfilePath = "CollisionConfig/DefaultCollisionVFXProfile";
    const string WearProfilePath = "CollisionConfig/VehicleWearProfile";

    static DeformationConfig deformationConfig;
    static CollisionEventChannel eventChannel;
    static CollisionEntityProfile vehicleMetalProfile;
    static CollisionAudioProfile audioProfile;
    static ProjectAudioLibrary projectAudioLibrary;
    static CollisionVFXProfile vfxProfile;
    static VehicleWearProfile wearProfile;

    public static DeformationConfig DeformationConfig =>
        deformationConfig ?? (deformationConfig = Resources.Load<DeformationConfig>(DeformPath));

    public static CollisionEventChannel EventChannel =>
        eventChannel ?? (eventChannel = Resources.Load<CollisionEventChannel>(ChannelPath));

    public static CollisionEntityProfile VehicleMetalProfile =>
        vehicleMetalProfile ?? (vehicleMetalProfile = Resources.Load<CollisionEntityProfile>(MetalProfilePath));

    public static CollisionAudioProfile AudioProfile =>
        audioProfile ?? (audioProfile = Resources.Load<CollisionAudioProfile>(AudioProfilePath));

    public static ProjectAudioLibrary ProjectAudioLibrary =>
        projectAudioLibrary ?? (projectAudioLibrary = Resources.Load<ProjectAudioLibrary>(AudioLibraryPath));

    public static CollisionVFXProfile VfxProfile =>
        vfxProfile ?? (vfxProfile = Resources.Load<CollisionVFXProfile>(VfxProfilePath));

    public static VehicleWearProfile WearProfile =>
        wearProfile ?? (wearProfile = Resources.Load<VehicleWearProfile>(WearProfilePath));

    public static DeformationConfig GetDeformationConfig(DeformationConfig overrideConfig = null)
    {
        if (overrideConfig != null)
            return overrideConfig;

        if (DeformationConfig != null)
            return DeformationConfig;

        if (CollisionManager.Instance != null && CollisionManager.Instance.DefaultDeformationConfig != null)
            return CollisionManager.Instance.DefaultDeformationConfig;

        return ScriptableObject.CreateInstance<DeformationConfig>();
    }

    public static CollisionEventChannel GetEventChannel(CollisionEventChannel overrideChannel = null)
    {
        if (overrideChannel != null)
            return overrideChannel;

        return EventChannel ?? ScriptableObject.CreateInstance<CollisionEventChannel>();
    }

    public static void ApplyTo(CollisionManager manager)
    {
        if (manager == null || DeformationConfig == null)
            return;

        manager.SetConfig(DeformationConfig, EventChannel);
    }
}
