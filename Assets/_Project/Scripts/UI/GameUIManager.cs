using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

/// <summary>
/// 游戏 UI：车辆数据、物理参数、损伤状态（设计文档 §11）。
/// </summary>
public class GameUIManager : MonoBehaviour
{
    [System.Serializable]
    public class VehicleEntry
    {
        [LabelText("显示名称")]
        public string displayName;

        [LabelText("驾驶脚本")]
        public CarController controller;
    }

    [Header("—— 车辆列表 ——")]
    public VehicleEntry[] vehicles;

    [Header("—— 信息显示 Text ——")]
    public Text currentVehicleText;
    public Text speedText;
    public Text damageText;
    public Text driveForceText;
    public Text frictionForceText;
    public Text gearText;
    public Text accelerationText;
    public Text statusText;
    public Text frictionCoeffText;

    [Header("—— 参数调节 Slider ——")]
    public Slider motorTorqueSlider;
    public Slider frictionSlider;

    [Header("—— Slider 数值范围 ——")]
    public float motorTorqueMin = 500f;
    public float motorTorqueMax = 8000f;
    public float gripMin = 0.1f;
    public float gripMax = 3f;

    [Header("—— 摄像机 ——")]
    public VehicleCameraController cameraController;

    int activeIndex;

    void Start()
    {
        if (vehicles == null || vehicles.Length == 0) return;
        SetupSliders();
        SelectVehicle(0);
    }

    void Update()
    {
        if (vehicles == null || vehicles.Length == 0) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectVehicle(0);
        if (Input.GetKeyDown(KeyCode.Alpha2) && vehicles.Length > 1) SelectVehicle(1);
        if (Input.GetKeyDown(KeyCode.Alpha3) && vehicles.Length > 2) SelectVehicle(2);

        RefreshDisplay();
    }

    void SetupSliders()
    {
        if (motorTorqueSlider != null)
        {
            motorTorqueSlider.minValue = motorTorqueMin;
            motorTorqueSlider.maxValue = motorTorqueMax;
            motorTorqueSlider.onValueChanged.AddListener(OnMotorTorqueChanged);
        }

        if (frictionSlider != null)
        {
            frictionSlider.minValue = gripMin;
            frictionSlider.maxValue = gripMax;
            frictionSlider.onValueChanged.AddListener(OnGripChanged);
        }
    }

    public void SelectVehicle(int index)
    {
        if (vehicles == null || vehicles.Length == 0) return;
        index = Mathf.Clamp(index, 0, vehicles.Length - 1);

        for (int i = 0; i < vehicles.Length; i++)
        {
            if (vehicles[i].controller != null)
                vehicles[i].controller.isPlayerControlled = i == index;
        }

        activeIndex = index;
        SyncSlidersToActiveVehicle();
        SwitchCamera(index);
        RefreshDisplay();
    }

    void SwitchCamera(int index)
    {
        if (cameraController != null)
            cameraController.SwitchToVehicle(index);
    }

    public void SelectVehicle1() => SelectVehicle(0);
    public void SelectVehicle2() => SelectVehicle(1);
    public void SelectVehicle3() => SelectVehicle(2);

    void SyncSlidersToActiveVehicle()
    {
        CarController car = GetActiveController();
        if (car == null) return;

        if (motorTorqueSlider != null)
            motorTorqueSlider.SetValueWithoutNotify(car.GetMotorTorque());

        if (frictionSlider != null)
            frictionSlider.SetValueWithoutNotify(car.GetGripCoefficient());
    }

    void RefreshDisplay()
    {
        VehicleEntry entry = GetActiveEntry();
        CarController car = entry?.controller;
        VehicleState state = car != null ? car.GetComponent<VehicleState>() : null;

        if (currentVehicleText != null)
            currentVehicleText.text = car != null
                ? $"当前控制：{entry.displayName}"
                : "当前控制：无";

        if (car == null) return;

        if (speedText != null)
            speedText.text = $"车速：{car.GetSpeed():F1} km/h";

        if (damageText != null)
        {
            string impulseText = state != null && state.LastImpulse > 0f
                ? $" | 冲量：{state.LastImpulse:F0}"
                : "";
            float pct = state != null ? state.GetDamagePercentApprox() : 0f;
            string status = state != null ? state.GetStatusText() : "完好";
            damageText.text = $"损伤：{pct:F0} % ({status}){impulseText}";
        }

        if (driveForceText != null)
            driveForceText.text = $"驱动力：{car.GetCurrentDriveForce():F0} N·m";

        if (frictionForceText != null)
            frictionForceText.text = $"地面阻力：{car.GetGroundResistance():F0} N";

        if (gearText != null)
            gearText.text = $"档位：{car.GetGear()}";

        if (accelerationText != null)
            accelerationText.text = $"加速度：{car.GetAccelerationG():F2} G";

        if (statusText != null)
            statusText.text = state != null
                ? $"车辆状态：{state.GetStatusText()}"
                : "车辆状态：完好";

        if (frictionCoeffText != null)
            frictionCoeffText.text = $"抓地系数：{car.GetGripCoefficient():F2}";
    }

    void OnMotorTorqueChanged(float value)
    {
        CarController car = GetActiveController();
        if (car != null) car.SetMotorTorque(value);
    }

    void OnGripChanged(float value)
    {
        CarController car = GetActiveController();
        if (car != null) car.SetGripCoefficient(value);
    }

    VehicleEntry GetActiveEntry()
    {
        if (vehicles == null || vehicles.Length == 0) return null;
        return vehicles[Mathf.Clamp(activeIndex, 0, vehicles.Length - 1)];
    }

    CarController GetActiveController() => GetActiveEntry()?.controller;
}
