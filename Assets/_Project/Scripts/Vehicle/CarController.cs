using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 车辆驾驶控制。
/// 力体系：驱动/刹车/抓地 → WheelCollider；滚动+空气阻力 → AddForce；碰撞 → PhysX。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("—— 控制开关 ——")]
    [LabelText("玩家控制")]
    [Tooltip("是否由玩家控制。切换车辆时由 GameUIManager 自动设置")]
    public bool isPlayerControlled;

    [Header("—— 车轮物理（Wheel Collider）——")]
    [LabelText("左前轮 Collider")]
    [Tooltip("左前轮 Wheel Collider")]
    public WheelCollider wheelFL;

    [LabelText("右前轮 Collider")]
    [Tooltip("右前轮 Wheel Collider")]
    public WheelCollider wheelFR;

    [LabelText("左后轮 Collider")]
    [Tooltip("左后轮 Wheel Collider")]
    public WheelCollider wheelRL;

    [LabelText("右后轮 Collider")]
    [Tooltip("右后轮 Wheel Collider")]
    public WheelCollider wheelRR;

    [Header("—— 车轮视觉模型 ——")]
    [LabelText("左前轮模型")]
    public Transform meshFL;

    [LabelText("右前轮模型")]
    public Transform meshFR;

    [LabelText("左后轮模型")]
    public Transform meshRL;

    [LabelText("右后轮模型")]
    public Transform meshRR;

    [Header("—— 驾驶参数 ——")]
    [LabelText("驱动力矩")]
    [Tooltip("发动机驱动力矩（N·m），越大加速越快")]
    public float motorTorque = 1500f;

    [LabelText("最大转向角")]
    [Tooltip("前轮最大转向角（度）。高速时会自动减小舵角以保持稳定")]
    public float maxSteerAngle = 18f;

    [LabelText("刹车力矩")]
    [Tooltip("按住 Space 时的刹车力矩")]
    public float brakeTorque = 3000f;

    [LabelText("急停力矩")]
    [Tooltip("按住 Left Shift 时的急停力矩")]
    public float handbrakeTorque = 10000f;

    [Header("—— 抓地与阻力（分离）——")]
    [LabelText("抓地系数 μ")]
    [Tooltip("轮胎抓地系数，写入 WheelCollider，影响加速/转向/刹车是否打滑")]
    [FormerlySerializedAs("frictionStiffness")]
    [Range(0.1f, 3f)]
    public float gripCoefficient = 1f;

    [LabelText("滚动阻力系数")]
    [Tooltip("滚动阻力 F = c × N，与抓地系数无关。越大松油门减速越快")]
    [Range(0.005f, 0.1f)]
    public float rollingResistanceCoeff = 0.02f;

    [LabelText("空气阻力系数")]
    [Tooltip("空气阻力 F = k × v²，高速时减速更明显")]
    [Range(0.1f, 2f)]
    public float airDragCoefficient = 0.4f;

    [Header("—— 可选 ——")]
    [LabelText("物理材质")]
    [Tooltip("碰撞反弹由 PhysX 处理。可指定 Bounciness≈0.2 的 PhysicMaterial")]
    public PhysicMaterial bodyPhysicMaterial;

    private Rigidbody rb;
    private float speedKmh;
    private float driveMultiplier = 1f;
    private float currentDriveTorque;
    private float groundResistance;
    private float normalForce;
    private float accelerationG;
    private float throttleInput;
    private float steerInput;
    private bool isDisabled;
    private bool isBraking;
    private bool isHandbraking;
    private int groundedWheelCount;
    private Vector3 lastVelocity;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
        rb.maxAngularVelocity = 2.5f;
        lastVelocity = rb.velocity;
        ApplyGripToWheels();

        if (bodyPhysicMaterial != null)
        {
            Collider bodyCollider = GetComponent<Collider>();
            if (bodyCollider != null)
                bodyCollider.material = bodyPhysicMaterial;
        }
    }

    void Update()
    {
        speedKmh = GetHorizontalSpeedKmh();

        if (!isPlayerControlled) return;

        if (isDisabled)
        {
            throttleInput = 0f;
            steerInput = 0f;
            isBraking = false;
            isHandbraking = true;
            return;
        }

        throttleInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxis("Horizontal");
        isBraking = Input.GetKey(KeyCode.Space);
        isHandbraking = Input.GetKey(KeyCode.LeftShift);
    }

    void FixedUpdate()
    {
        if (!isPlayerControlled)
        {
            SetMotorAll(0f);
            SetBrakeAll(0f);
            wheelFL.steerAngle = wheelFR.steerAngle = 0f;
        }
        else if (isDisabled)
        {
            currentDriveTorque = 0f;
            SetMotorAll(0f);
            wheelFL.steerAngle = wheelFR.steerAngle = 0f;
            SetBrakeAll(handbrakeTorque);
        }
        else
        {
            ApplyWheelDrive();
        }

        UpdateWheelMesh(wheelFL, meshFL);
        UpdateWheelMesh(wheelFR, meshFR);
        UpdateWheelMesh(wheelRL, meshRL);
        UpdateWheelMesh(wheelRR, meshRR);

        UpdateGroundContact();
        ApplyResistanceForces();

        Vector3 acceleration = (rb.velocity - lastVelocity) / Time.fixedDeltaTime;
        accelerationG = acceleration.magnitude / 9.81f;
        lastVelocity = rb.velocity;
    }

    void ApplyWheelDrive()
    {
        float speed = GetHorizontalSpeedKmh();

        float steerScale = 1f;
        if (speed > 50f)
            steerScale = Mathf.Lerp(1f, 0.45f, Mathf.Clamp01((speed - 50f) / 50f));

        float steerAngle = maxSteerAngle * steerInput * steerScale;
        wheelFL.steerAngle = steerAngle;
        wheelFR.steerAngle = steerAngle;

        currentDriveTorque = motorTorque * driveMultiplier * throttleInput;

        // 前轮驱动：拉着走弯，比后轮推+转向稳得多
        if (isHandbraking)
        {
            SetBrakeAll(handbrakeTorque);
            SetMotorAll(0f);
        }
        else if (isBraking)
        {
            SetBrakeAll(brakeTorque);
            SetMotorAll(currentDriveTorque * 0.3f);
        }
        else
        {
            SetBrakeAll(0f);
            SetMotorAll(currentDriveTorque);
        }
    }

    void SetMotorAll(float torque)
    {
        wheelFL.motorTorque = wheelFR.motorTorque = torque;
        wheelRL.motorTorque = wheelRR.motorTorque = 0f;
    }

    void UpdateGroundContact()
    {
        normalForce = 0f;
        groundedWheelCount = 0;

        AccumulateWheelNormal(wheelFL);
        AccumulateWheelNormal(wheelFR);
        AccumulateWheelNormal(wheelRL);
        AccumulateWheelNormal(wheelRR);

        if (groundedWheelCount == 0)
            normalForce = rb.mass * Mathf.Abs(Physics.gravity.y);
    }

    void AccumulateWheelNormal(WheelCollider wheel)
    {
        if (wheel == null) return;
        if (!wheel.GetGroundHit(out WheelHit hit)) return;
        normalForce += hit.force;
        groundedWheelCount++;
    }

    void ApplyResistanceForces()
    {
        if (groundedWheelCount == 0 || isDisabled) return;

        Vector3 horizontalVel = GetHorizontalVelocity();
        float speed = horizontalVel.magnitude;
        if (speed < 0.05f)
        {
            groundResistance = 0f;
            return;
        }

        float rollingForce = rollingResistanceCoeff * normalForce;
        float airDragForce = airDragCoefficient * speed * speed;
        groundResistance = rollingForce + airDragForce;

        rb.AddForce(-horizontalVel.normalized * groundResistance, ForceMode.Force);
    }

    Vector3 GetHorizontalVelocity()
    {
        Vector3 vel = rb.velocity;
        return new Vector3(vel.x, 0f, vel.z);
    }

    float GetHorizontalSpeedKmh() => GetHorizontalVelocity().magnitude * 3.6f;

    void SetBrakeAll(float torque)
    {
        wheelFL.brakeTorque = wheelFR.brakeTorque = torque;
        wheelRL.brakeTorque = wheelRR.brakeTorque = torque;
    }

    void UpdateWheelMesh(WheelCollider col, Transform mesh)
    {
        if (col == null || mesh == null) return;
        col.GetWorldPose(out Vector3 pos, out Quaternion rot);
        mesh.position = pos;
        mesh.rotation = rot;
    }

    void ApplyGripToWheels()
    {
        SetWheelGrip(wheelFL);
        SetWheelGrip(wheelFR);
        SetWheelGrip(wheelRL);
        SetWheelGrip(wheelRR);
    }

    void SetWheelGrip(WheelCollider wheel)
    {
        if (wheel == null) return;

        WheelFrictionCurve forward = wheel.forwardFriction;
        forward.stiffness = gripCoefficient;
        wheel.forwardFriction = forward;

        WheelFrictionCurve sideways = wheel.sidewaysFriction;
        sideways.stiffness = gripCoefficient;
        wheel.sidewaysFriction = sideways;
    }

    public float GetSpeed() => speedKmh;
    public float GetMotorTorque() => motorTorque;
    public float GetCurrentDriveForce() => Mathf.Abs(currentDriveTorque);
    public float GetGroundResistance() => groundResistance;
    public float GetNormalForce() => normalForce;
    public float GetGripCoefficient() => gripCoefficient;
    public float GetAccelerationG() => accelerationG;

    public string GetGear()
    {
        if (isDisabled) return "P";
        if (Mathf.Abs(speedKmh) < 1f && Mathf.Abs(throttleInput) < 0.05f) return "N";
        if (throttleInput < -0.05f || Vector3.Dot(GetHorizontalVelocity(), transform.forward) < -0.5f) return "R";
        return "D";
    }

    public void SetDriveMultiplier(float multiplier) => driveMultiplier = Mathf.Clamp01(multiplier);
    public void SetDisabled(bool disabled) => isDisabled = disabled;
    public bool IsDisabled() => isDisabled;
    public void SetMotorTorque(float torque) => motorTorque = Mathf.Max(0f, torque);

    public void SetGripCoefficient(float grip)
    {
        gripCoefficient = Mathf.Clamp(grip, 0.1f, 3f);
        ApplyGripToWheels();
    }

    public float GetMaxSteerAngle() => maxSteerAngle;
    public void SetMaxSteerAngle(float angle) => maxSteerAngle = Mathf.Clamp(angle, 5f, 45f);

    public float GetBrakeTorque() => brakeTorque;
    public void SetBrakeTorque(float torque) => brakeTorque = Mathf.Max(0f, torque);

    public float GetHandbrakeTorque() => handbrakeTorque;
    public void SetHandbrakeTorque(float torque) => handbrakeTorque = Mathf.Max(0f, torque);

    public float GetRollingResistanceCoeff() => rollingResistanceCoeff;
    public void SetRollingResistanceCoeff(float coeff) =>
        rollingResistanceCoeff = Mathf.Clamp(coeff, 0.005f, 0.1f);

    public float GetAirDragCoefficient() => airDragCoefficient;
    public void SetAirDragCoefficient(float coeff) => airDragCoefficient = Mathf.Clamp(coeff, 0.1f, 2f);

    public float GetMass() => rb != null ? rb.mass : 1000f;

    public void SetMass(float mass)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb != null) rb.mass = Mathf.Clamp(mass, 500f, 5000f);
    }

    public float GetCenterOfMassY()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        return rb != null ? rb.centerOfMass.y : -0.5f;
    }

    public void SetCenterOfMassY(float y)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb == null) return;
        Vector3 com = rb.centerOfMass;
        com.y = Mathf.Clamp(y, -2f, 0.5f);
        rb.centerOfMass = com;
    }

    public float GetDriveMultiplier() => driveMultiplier;
    public int GetGroundedWheelCount() => groundedWheelCount;
    public float GetThrottleInput() => throttleInput;
    public bool IsBraking() => isBraking;
    public bool IsHandbraking() => isHandbraking;
}
