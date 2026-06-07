using UnityEngine;

/// <summary>
/// 车辆碰撞/形变组件的唯一配置入口（运行时与 Editor 共用）。
/// </summary>
public static class VehicleDeformationSetup
{
    struct PartDef
    {
        public string objectName;
        public VehiclePartType partType;
    }

    static readonly PartDef[] Parts =
    {
        new PartDef { objectName = "Base_Car", partType = VehiclePartType.FrontBumper },
        new PartDef { objectName = "Car_hood", partType = VehiclePartType.Hood },
        new PartDef { objectName = "Car_trunk", partType = VehiclePartType.Trunk },
        new PartDef { objectName = "CDoor_FL", partType = VehiclePartType.DoorFL },
        new PartDef { objectName = "CDoor_FR", partType = VehiclePartType.DoorFR },
        new PartDef { objectName = "CDoor_BL", partType = VehiclePartType.DoorRL },
        new PartDef { objectName = "CDoor_BR", partType = VehiclePartType.DoorRR },
        new PartDef { objectName = "Police_base", partType = VehiclePartType.FrontBumper },
        new PartDef { objectName = "Police_hood", partType = VehiclePartType.Hood },
        new PartDef { objectName = "Police_trunk", partType = VehiclePartType.Trunk },
        new PartDef { objectName = "PDoor_FL", partType = VehiclePartType.DoorFL },
        new PartDef { objectName = "PDoor_FR", partType = VehiclePartType.DoorFR },
        new PartDef { objectName = "PDoor_BL", partType = VehiclePartType.DoorRL },
        new PartDef { objectName = "PDoor_BR", partType = VehiclePartType.DoorRR },
        new PartDef { objectName = "Taxi_S", partType = VehiclePartType.FrontBumper },
    };

    public static void SetupAllVehicles()
    {
        foreach (CarController car in Object.FindObjectsOfType<CarController>())
            SetupVehicle(car.gameObject);
    }

    public static void SetupVehicle(GameObject vehicleRoot)
    {
        if (vehicleRoot == null)
            return;

        EnsureCoreComponents(vehicleRoot);
        SetupDeformableParts(vehicleRoot);
        ConfigureRigidbody(vehicleRoot);
        ConfigureDamageSensitivity(vehicleRoot);
        FinalizeDeformableParts(vehicleRoot);
    }

    public static void FinalizeDeformableParts(GameObject vehicleRoot)
    {
        if (vehicleRoot == null || !Application.isPlaying)
            return;

        foreach (DeformablePart part in vehicleRoot.GetComponentsInChildren<DeformablePart>(true))
        {
            if (part.meshFilter == null)
            {
                part.Configure(
                    vehicleRoot,
                    part.PartType,
                    part.GetComponent<MeshFilter>(),
                    part.GetComponent<BoxCollider>());
            }

            part.Initialize(vehicleRoot);
        }
    }

    static void EnsureCoreComponents(GameObject root)
    {
        if (root.GetComponent<CollisionReporter>() == null)
            root.AddComponent<CollisionReporter>();

        CollisionReporter reporter = root.GetComponent<CollisionReporter>();
        CollisionEntityProfile metalProfile = CollisionConfigProvider.VehicleMetalProfile;
        if (metalProfile != null)
            reporter.SetProfile(metalProfile);

        if (root.GetComponent<VehicleDeformationController>() == null)
            root.AddComponent<VehicleDeformationController>();

        if (root.GetComponent<VehicleState>() == null)
            root.AddComponent<VehicleState>();

        if (root.GetComponent<VehicleAudioController>() == null)
            root.AddComponent<VehicleAudioController>();

        root.layer = CollisionTypes.GetLayerIndex(CollisionTypes.LayerVehicle);
    }

    static void ConfigureDamageSensitivity(GameObject root)
    {
        VehicleState state = root.GetComponent<VehicleState>();
        if (state == null)
            return;

        string lower = root.name.ToLowerInvariant();
        if (lower.Contains("police"))
            state.damageSensitivity = 0.65f;
        else if (lower.Contains("taxi"))
            state.damageSensitivity = 0.32f;
        else if (lower.Contains("car"))
            state.damageSensitivity = 0.28f;
    }

    static void ConfigureRigidbody(GameObject root)
    {
        Rigidbody rb = root.GetComponent<Rigidbody>();
        if (rb == null)
            return;

        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.maxDepenetrationVelocity = 2f;
    }

    static void SetupDeformableParts(GameObject root)
    {
        foreach (PartDef def in Parts)
        {
            Transform t = FindChildRecursive(root.transform, def.objectName);
            if (t == null)
                continue;

            GameObject partGo = t.gameObject;
            MeshFilter mf = partGo.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
                continue;

            BoxCollider box = partGo.GetComponent<BoxCollider>();
            if (box == null)
            {
                box = partGo.AddComponent<BoxCollider>();
                Bounds b = mf.sharedMesh.bounds;
                box.center = b.center;
                box.size = b.size;
            }

            DeformablePart part = partGo.GetComponent<DeformablePart>();
            if (part == null)
                part = partGo.AddComponent<DeformablePart>();

            part.Configure(root, def.partType, mf, box);
            partGo.layer = root.layer;
        }
    }

    static Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            return parent;

        foreach (Transform child in parent)
        {
            Transform found = FindChildRecursive(child, name);
            if (found != null)
                return found;
        }

        return null;
    }
}
