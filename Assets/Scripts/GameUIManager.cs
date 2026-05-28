using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

/// <summary>
/// 游戏 UI 总管理：显示车辆数据、调节物理参数、切换车辆与摄像机。
/// </summary>
public class GameUIManager : MonoBehaviour
{
    [System.Serializable]
    public class VehicleEntry
    {
        [LabelText("显示名称")]
        [Tooltip("UI 显示名称，如「车辆 1」")]
        public string displayName;

        [LabelText("驾驶脚本")]
        [Tooltip("该车的 CarController 组件")]
        public CarController controller;

        [LabelText("损伤脚本")]
        [Tooltip("该车的 CarDamageSystem 组件")]
        public CarDamageSystem damage;
    }

    [Header("—— 车辆列表 ——")]
    [LabelText("车辆列表")]
    public VehicleEntry[] vehicles;

    [Header("—— 信息显示 Text ——")]
    [LabelText("当前车辆 Text")]
    public Text currentVehicleText;

    [LabelText("车速 Text")]
    public Text speedText;

    [LabelText("损伤 Text")]
    public Text damageText;

    [LabelText("驱动力 Text")]
    public Text driveForceText;

    [LabelText("地面阻力 Text")]
    [Tooltip("显示滚动阻力 + 空气阻力之和（N）")]
    public Text frictionForceText;

    [LabelText("档位 Text")]
    public Text gearText;

    [LabelText("加速度 Text")]
    public Text accelerationText;

    [LabelText("状态 Text")]
    public Text statusText;

    [LabelText("抓地系数 Text")]
    [Tooltip("显示轮胎抓地系数 μ")]
    public Text frictionCoeffText;

    [Header("—— 参数调节 Slider ——")]
    [LabelText("驱动力 Slider")]
    public Slider motorTorqueSlider;

    [LabelText("抓地系数 Slider")]
    [Tooltip("调节轮胎抓地系数，影响打滑程度")]
    public Slider frictionSlider;

    [Header("—— Slider 数值范围 ——")]
    [LabelText("驱动力最小值")]
    public float motorTorqueMin = 500f;

    [LabelText("驱动力最大值")]
    public float motorTorqueMax = 8000f;

    [LabelText("抓地系数最小值")]
    [FormerlySerializedAs("frictionMin")]
    public float gripMin = 0.1f;

    [LabelText("抓地系数最大值")]
    [FormerlySerializedAs("frictionMax")]
    public float gripMax = 3f;

    [Header("—— 摄像机 ——")]
    [LabelText("摄像机控制器")]
    public VehicleCameraController cameraController;

    private int activeIndex;

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
        CarDamageSystem damage = entry?.damage;

        if (currentVehicleText != null)
            currentVehicleText.text = car != null
                ? $"当前控制：{entry.displayName}"
                : "当前控制：无";

        if (car == null) return;

        if (speedText != null)
            speedText.text = $"车速：{car.GetSpeed():F1} km/h";

        if (damageText != null)
            damageText.text = damage != null
                ? $"损伤：{damage.GetDamagePercent():F1} %"
                : "损伤：0.0 %";

        if (driveForceText != null)
            driveForceText.text = $"驱动力：{car.GetCurrentDriveForce():F0} N·m";

        if (frictionForceText != null)
            frictionForceText.text = $"地面阻力：{car.GetGroundResistance():F0} N";

        if (gearText != null)
            gearText.text = $"档位：{car.GetGear()}";

        if (accelerationText != null)
            accelerationText.text = $"加速度：{car.GetAccelerationG():F2} G";

        if (statusText != null)
            statusText.text = damage != null
                ? $"车辆状态：{damage.GetVehicleStatus()}"
                : "车辆状态：正常";

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
