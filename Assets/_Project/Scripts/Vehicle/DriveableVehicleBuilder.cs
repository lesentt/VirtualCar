using UnityEngine;

/// <summary>
/// 将仅有模型的车辆预制体装配为可驾驶车辆（刚体、车轮、摄像机、驾驶脚本）。
/// 在 Editor 与运行时均可调用，重复调用安全。
/// </summary>
public static class DriveableVehicleBuilder
{
    static readonly Vector3 WheelsRootOffset = new Vector3(0f, 0.355384f, 0.117608f);

    public struct DriveProfile
    {
        public float mass;
        public float motorTorque;
        public float maxSteerAngle;
        public float brakeTorque;
        public float handbrakeTorque;
        public float gripCoefficient;
        public float rollingResistanceCoeff;
        public float airDragCoefficient;
        public float damageSensitivity;
        public float centerOfMassY;
        public string wheelMeshFl;
        public string wheelMeshFr;
        public string wheelMeshRl;
        public string wheelMeshRr;
        public Vector3 wheelFl;
        public Vector3 wheelFr;
        public Vector3 wheelRl;
        public Vector3 wheelRr;
    }

    public static readonly DriveProfile TaxiProfile = new DriveProfile
    {
        mass = 1350f,
        motorTorque = 1500f,
        maxSteerAngle = 15f,
        brakeTorque = 3000f,
        handbrakeTorque = 5000f,
        gripCoefficient = 2.8f,
        rollingResistanceCoeff = 0.07f,
        airDragCoefficient = 0.85f,
        damageSensitivity = 0.16f,
        centerOfMassY = -10f,
        wheelMeshFl = "TS_WheelFL",
        wheelMeshFr = "TS_WheelFR",
        wheelMeshRl = "",
        wheelMeshRr = "",
        wheelFl = new Vector3(-0.689f, 0.15f, 1.47f),
        wheelFr = new Vector3(0.689f, 0.14f, 1.47f),
        wheelRl = new Vector3(-0.689f, 0.156f, -1.35f),
        wheelRr = new Vector3(0.689f, 0.156f, -1.35f),
    };

    public static bool EnsureDriveable(GameObject root, DriveProfile profile)
    {
        if (root == null)
            return false;

        Transform wheelsRoot = EnsureWheelsRoot(root.transform);
        WheelCollider wheelFl = EnsureWheelCollider(wheelsRoot, "FL", profile.wheelFl);
        WheelCollider wheelFr = EnsureWheelCollider(wheelsRoot, "FR", profile.wheelFr);
        WheelCollider wheelRl = EnsureWheelCollider(wheelsRoot, "RL", profile.wheelRl);
        WheelCollider wheelRr = EnsureWheelCollider(wheelsRoot, "RR", profile.wheelRr);

        Rigidbody rb = root.GetComponent<Rigidbody>();
        if (rb == null)
            rb = root.AddComponent<Rigidbody>();
        rb.mass = profile.mass;
        rb.drag = 0f;
        rb.angularDrag = 0.1f;
        rb.centerOfMass = new Vector3(0f, profile.centerOfMassY, 0f);
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.maxDepenetrationVelocity = 2f;

        CarController car = root.GetComponent<CarController>();
        if (car == null)
            car = root.AddComponent<CarController>();

        car.wheelFL = wheelFl;
        car.wheelFR = wheelFr;
        car.wheelRL = wheelRl;
        car.wheelRR = wheelRr;
        car.meshFL = AlignWheelVisual(root.transform, wheelsRoot, profile.wheelMeshFl);
        car.meshFR = AlignWheelVisual(root.transform, wheelsRoot, profile.wheelMeshFr);
        car.meshRL = AlignWheelVisual(root.transform, wheelsRoot, profile.wheelMeshRl);
        car.meshRR = AlignWheelVisual(root.transform, wheelsRoot, profile.wheelMeshRr);
        car.motorTorque = profile.motorTorque;
        car.maxSteerAngle = profile.maxSteerAngle;
        car.brakeTorque = profile.brakeTorque;
        car.handbrakeTorque = profile.handbrakeTorque;
        car.gripCoefficient = profile.gripCoefficient;
        car.rollingResistanceCoeff = profile.rollingResistanceCoeff;
        car.airDragCoefficient = profile.airDragCoefficient;
        car.isPlayerControlled = false;

        EnsureVehicleCamera(root.transform);

        VehicleState state = root.GetComponent<VehicleState>();
        if (state == null)
            state = root.AddComponent<VehicleState>();
        state.damageSensitivity = profile.damageSensitivity;

        VehicleDeformationSetup.SetupVehicle(root);
        return true;
    }

    public static bool EnsureTaxi(GameObject root) =>
        EnsureDriveable(root, TaxiProfile);

    static Transform EnsureWheelsRoot(Transform parent)
    {
        Transform child = parent.Find("Drive_Wheels");
        if (child == null)
        {
            GameObject go = new GameObject("Drive_Wheels");
            child = go.transform;
            child.SetParent(parent, false);
        }

        child.localPosition = WheelsRootOffset;
        child.localRotation = Quaternion.identity;
        child.localScale = Vector3.one;
        return child;
    }

    static WheelCollider EnsureWheelCollider(Transform parent, string name, Vector3 localPosition)
    {
        Transform wheelTransform = parent.Find(name);
        if (wheelTransform == null)
        {
            GameObject go = new GameObject(name);
            wheelTransform = go.transform;
            wheelTransform.SetParent(parent, false);
        }

        wheelTransform.localPosition = localPosition;
        wheelTransform.localRotation = Quaternion.identity;

        WheelCollider wheel = wheelTransform.GetComponent<WheelCollider>();
        if (wheel == null)
            wheel = wheelTransform.gameObject.AddComponent<WheelCollider>();

        wheel.radius = 0.4f;
        wheel.suspensionDistance = 0.3f;
        wheel.mass = 20f;
        wheel.wheelDampingRate = 0.25f;
        wheel.forceAppPointDistance = 0f;
        wheel.suspensionSpring = new JointSpring
        {
            spring = 100000f,
            damper = 20000f,
            targetPosition = 0.5f
        };

        WheelFrictionCurve forward = wheel.forwardFriction;
        forward.extremumSlip = 0.4f;
        forward.extremumValue = 1f;
        forward.asymptoteSlip = 0.8f;
        forward.asymptoteValue = 0.5f;
        forward.stiffness = 1f;
        wheel.forwardFriction = forward;

        WheelFrictionCurve sideways = wheel.sidewaysFriction;
        sideways.extremumSlip = 0.2f;
        sideways.extremumValue = 1f;
        sideways.asymptoteSlip = 0.5f;
        sideways.asymptoteValue = 0.75f;
        sideways.stiffness = 1f;
        wheel.sidewaysFriction = sideways;

        return wheel;
    }

    static Transform AlignWheelVisual(Transform root, Transform wheelsRoot, string meshName)
    {
        if (string.IsNullOrEmpty(meshName))
            return null;

        Transform mesh = FindChildRecursive(root, meshName);
        if (mesh == null)
            return null;

        Vector3 worldPos = mesh.position;
        Quaternion worldRot = mesh.rotation;
        mesh.SetParent(wheelsRoot, true);
        mesh.position = worldPos;
        mesh.rotation = worldRot;
        return mesh;
    }

    static void EnsureVehicleCamera(Transform root)
    {
        Camera existing = root.GetComponentInChildren<Camera>(true);
        if (existing != null)
            return;

        GameObject cameraGo = new GameObject("Camera");
        cameraGo.transform.SetParent(root, false);
        cameraGo.transform.localPosition = new Vector3(0.12f, 2.79f, -4.25f);
        cameraGo.transform.localRotation = Quaternion.Euler(20.412f, 0f, 0f);

        Camera cam = cameraGo.AddComponent<Camera>();
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1000f;
        cameraGo.AddComponent<AudioListener>();
    }

    static Transform FindChildRecursive(Transform parent, string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                return child;
        }

        return null;
    }
}
