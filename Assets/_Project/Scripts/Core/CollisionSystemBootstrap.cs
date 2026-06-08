using UnityEngine;

/// <summary>
/// 碰撞子系统运行时引导：创建 CollisionManager、视听反馈，并配置车辆。
/// </summary>
public static class CollisionSystemBootstrap
{
    public static void Initialize()
    {
        EnsureCollisionSystemRoot();
        EnsureDriveableVehicleModels();
        VehicleDeformationSetup.SetupAllVehicles();
        EnsureEnvironmentReporters();
    }

    static void EnsureDriveableVehicleModels()
    {
        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            EnsureDriveableVehicleRecursive(root);
    }

    static bool IsTaxiVehicleRoot(GameObject go)
    {
        if (go == null)
            return false;

        if (go.GetComponentInParent<CarController>() != null && go.GetComponent<CarController>() == null)
            return false;

        string lower = go.name.ToLowerInvariant();
        if (lower.Contains("stylized"))
            return false;

        CarController car = go.GetComponent<CarController>();
        return car != null && lower.Contains("taxi");
    }

    static void EnsureDriveableVehicleRecursive(GameObject go)
    {
        if (go == null)
            return;

        if (IsTaxiVehicleRoot(go))
            DriveableVehicleBuilder.EnsureTaxi(go);

        foreach (Transform child in go.transform)
            EnsureDriveableVehicleRecursive(child.gameObject);
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
