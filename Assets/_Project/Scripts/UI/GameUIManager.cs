using UnityEngine;

/// <summary>
/// 游戏 UI：车辆切换与左下角 HUD 数据刷新（设计文档 §11）。
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

    [LabelText("自动发现场景车辆")]
    [Tooltip("启动时按 Car 1 → Police 1 → Taxi 顺序收集场景中的 CarController")]
    public bool autoDiscoverVehicles = true;

    [Header("—— 摄像机 ——")]
    public VehicleCameraController cameraController;

    int activeIndex;
    GameSettingsManager settingsManager;
    VehicleHudController hudController;

    public int VehicleCount => vehicles != null ? vehicles.Length : 0;

    void Awake()
    {
        EnsureCanvasScaler();
        DestroyLegacyHudElements();

        if (GetComponent<GameSettingsManager>() == null)
            gameObject.AddComponent<GameSettingsManager>();
        if (GetComponent<SettingsUIController>() == null)
            gameObject.AddComponent<SettingsUIController>();

        hudController = GetComponent<VehicleHudController>();
        if (hudController == null)
            hudController = gameObject.AddComponent<VehicleHudController>();
    }

    void Start()
    {
        settingsManager = GetComponent<GameSettingsManager>();
        if (autoDiscoverVehicles)
            RefreshVehicleRegistry();
        BuildHud();
        if (vehicles == null || vehicles.Length == 0) return;
        SelectVehicle(0);
    }

    void Update()
    {
        if (vehicles == null || vehicles.Length == 0) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectVehicle(0);
        if (Input.GetKeyDown(KeyCode.Alpha2) && vehicles.Length > 1) SelectVehicle(1);
        if (Input.GetKeyDown(KeyCode.Alpha3) && vehicles.Length > 2) SelectVehicle(2);
        if (Input.GetKeyDown(KeyCode.Alpha4) && vehicles.Length > 3) SelectVehicle(3);

        RefreshDisplay();
    }

    public void RefreshVehicleRegistry()
    {
        if (cameraController == null)
            cameraController = FindObjectOfType<VehicleCameraController>();

        VehicleEntry[] previous = vehicles;
        CarController[] controllers = VehicleSwitchRegistry.CollectOrderedControllers();
        vehicles = VehicleSwitchRegistry.BuildVehicleEntries(controllers);
        PreserveDisplayNames(previous);

        if (cameraController != null)
            cameraController.SyncWithControllers(controllers);
    }

    void PreserveDisplayNames(VehicleEntry[] previous)
    {
        if (previous == null || vehicles == null)
            return;

        for (int i = 0; i < vehicles.Length; i++)
        {
            CarController controller = vehicles[i].controller;
            if (controller == null)
                continue;

            for (int j = 0; j < previous.Length; j++)
            {
                if (previous[j].controller != controller)
                    continue;
                if (!string.IsNullOrEmpty(previous[j].displayName))
                    vehicles[i].displayName = previous[j].displayName;
                break;
            }
        }
    }

    void EnsureCanvasScaler()
    {
        UiCanvasSetup setup = GetComponent<UiCanvasSetup>();
        if (setup == null)
            setup = gameObject.AddComponent<UiCanvasSetup>();
        setup.ApplyToAllCanvases();
    }

    void BuildHud()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[GameUIManager] 未找到 Canvas，无法构建 HUD。");
            return;
        }

        hudController.Build(canvas.transform);
    }

    void DestroyLegacyHudElements()
    {
        Transform root = transform;
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            string n = child.name;
            if (n.StartsWith("Text_") || n.StartsWith("Slider_"))
                Destroy(child.gameObject);
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
    public void SelectVehicle4() => SelectVehicle(3);

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
        if (hudController != null && hudController.HudRoot != null)
            hudController.HudRoot.SetActive(visible);
    }

    void RefreshDisplay()
    {
        if (hudController == null) return;

        VehicleEntry entry = GetActiveEntry();
        CarController car = entry?.controller;
        VehicleState state = car != null ? car.GetComponent<VehicleState>() : null;
        CollisionEventRecorder recorder = CollisionManager.Instance?.Recorder;

        hudController.SetVehicleTabs(VehicleCount, activeIndex);

        if (car == null) return;

        float damagePct = state != null ? state.GetDamagePercentApprox() : 0f;
        string damageStatus = state != null ? state.GetStatusText() : "完好";
        int collisionCount = state != null ? state.CollisionCount : 0;
        float lastImpulse = recorder != null && recorder.LastEvent.Impulse > 0f
            ? recorder.LastEvent.Impulse
            : state != null ? state.LastImpulse : 0f;

        hudController.Refresh(
            GetVehicleDisplayName(activeIndex),
            car.GetSpeed(),
            car.GetGear(),
            car.GetThrottleInput(),
            car.GetAccelerationG(),
            car.GetGroundedWheelCount(),
            damagePct,
            damageStatus,
            car.GetDriveMultiplier(),
            collisionCount,
            lastImpulse,
            car.GetCurrentDriveForce(),
            car.GetGroundResistance(),
            car.GetGripCoefficient(),
            car.GetMass());
    }

    VehicleEntry GetActiveEntry()
    {
        if (vehicles == null || vehicles.Length == 0) return null;
        return vehicles[Mathf.Clamp(activeIndex, 0, vehicles.Length - 1)];
    }
}
