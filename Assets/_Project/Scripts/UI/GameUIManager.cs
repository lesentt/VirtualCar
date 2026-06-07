using UnityEngine;
using UnityEngine.UI;

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
    public Text massText;
    public Text collisionInfoText;
    public Text driveMultiplierText;

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

    [Header("—— HUD 根节点（可选）——")]
    public GameObject hudRoot;

    int activeIndex;
    GameSettingsManager settingsManager;

    public int VehicleCount => vehicles != null ? vehicles.Length : 0;

    void Awake()
    {
        if (GetComponent<GameSettingsManager>() == null)
            gameObject.AddComponent<GameSettingsManager>();
        if (GetComponent<SettingsUIController>() == null)
            gameObject.AddComponent<SettingsUIController>();
    }

    void Start()
    {
        settingsManager = GetComponent<GameSettingsManager>();
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
        settingsManager?.SyncFromActiveVehicle();
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

    public string GetVehicleDisplayName(int index)
    {
        if (vehicles == null || index < 0 || index >= vehicles.Length)
            return $"车辆 {index + 1}";
        return string.IsNullOrEmpty(vehicles[index].displayName)
            ? $"车辆 {index + 1}"
            : vehicles[index].displayName;
    }

    public CarController GetActiveController() => GetActiveEntry()?.controller;

    public void SetHudVisible(bool visible)
    {
        if (hudRoot != null)
        {
            hudRoot.SetActive(visible);
            return;
        }

        ToggleTextVisible(currentVehicleText, visible);
        ToggleTextVisible(speedText, visible);
        ToggleTextVisible(damageText, visible);
        ToggleTextVisible(driveForceText, visible);
        ToggleTextVisible(frictionForceText, visible);
        ToggleTextVisible(gearText, visible);
        ToggleTextVisible(accelerationText, visible);
        ToggleTextVisible(statusText, visible);
        ToggleTextVisible(frictionCoeffText, visible);
        ToggleTextVisible(massText, visible);
        ToggleTextVisible(collisionInfoText, visible);
        ToggleTextVisible(driveMultiplierText, visible);

        if (motorTorqueSlider != null) motorTorqueSlider.gameObject.SetActive(visible);
        if (frictionSlider != null) frictionSlider.gameObject.SetActive(visible);
    }

    static void ToggleTextVisible(Text text, bool visible)
    {
        if (text != null)
            text.gameObject.SetActive(visible);
    }

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
        CollisionEventRecorder recorder = CollisionManager.Instance?.Recorder;

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

        if (massText != null)
            massText.text = $"质量：{car.GetMass():F0} kg | 接地轮：{car.GetGroundedWheelCount()}/4";

        if (driveMultiplierText != null)
            driveMultiplierText.text = $"动力倍率：{car.GetDriveMultiplier():P0}";

        if (collisionInfoText != null)
        {
            int count = state != null ? state.CollisionCount : 0;
            float lastImpulse = recorder != null && recorder.LastEvent.Impulse > 0f
                ? recorder.LastEvent.Impulse
                : state != null ? state.LastImpulse : 0f;
            collisionInfoText.text = $"碰撞次数：{count} | 最近冲量：{lastImpulse:F0}";
        }
    }

    void OnMotorTorqueChanged(float value)
    {
        if (settingsManager != null)
        {
            settingsManager.Settings.motorTorque = value;
            settingsManager.ApplyToActiveVehicle();
            return;
        }

        CarController car = GetActiveController();
        if (car != null) car.SetMotorTorque(value);
    }

    void OnGripChanged(float value)
    {
        if (settingsManager != null)
        {
            settingsManager.Settings.gripCoefficient = value;
            settingsManager.ApplyToActiveVehicle();
            return;
        }

        CarController car = GetActiveController();
        if (car != null) car.SetGripCoefficient(value);
    }

    VehicleEntry GetActiveEntry()
    {
        if (vehicles == null || vehicles.Length == 0) return null;
        return vehicles[Mathf.Clamp(activeIndex, 0, vehicles.Length - 1)];
    }
}
