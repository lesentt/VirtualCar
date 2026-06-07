using UnityEngine;

/// <summary>
/// 碰撞子系统运行时引导：创建 CollisionManager、视听反馈，并配置车辆。
/// </summary>
public static class CollisionSystemBootstrap
{
    public static void Initialize()
    {
        EnsureCollisionSystemRoot();
        VehicleDeformationSetup.SetupAllVehicles();
        EnsureEnvironmentReporters();
    }

    public static void EnsureCollisionSystemRoot()
    {
        if (CollisionManager.Instance != null)
            return;

        GameObject root = new GameObject("CollisionSystem");

        CollisionManager manager = root.AddComponent<CollisionManager>();
        root.AddComponent<CollisionAudioManager>();
        root.AddComponent<CollisionVFXManager>();
        root.AddComponent<CameraShakeController>();
        root.AddComponent<SceneResetService>();

        CollisionConfigProvider.ApplyTo(manager);
    }

    static void EnsureEnvironmentReporters()
    {
        foreach (CollisionProfile profile in Object.FindObjectsOfType<CollisionProfile>())
        {
            if (CollisionTypes.IsPartOfPlayerVehicle(profile.gameObject))
                continue;

            if (profile.GetComponent<CollisionReporter>() == null)
                profile.gameObject.AddComponent<CollisionReporter>();
        }
    }
}
