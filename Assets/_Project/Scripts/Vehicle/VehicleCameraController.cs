using UnityEngine;

/// <summary>
/// 车辆摄像机切换脚本。
/// 功能：按数字键切换车辆时同步启用对应车载摄像机；V 键切换第三人称 / 驾驶室视角。
/// </summary>
public class VehicleCameraController : MonoBehaviour
{
    [System.Serializable]
    public class CameraEntry
    {
        [LabelText("车辆根物体")]
        [Tooltip("车辆根物体（Car），用于自动查找子物体中的 Camera")]
        public Transform vehicleRoot;

        [LabelText("相机 Rig")]
        [Tooltip("双视角 Rig。留空则自动在 vehicleRoot 上查找或创建 VehicleCameraRig")]
        public VehicleCameraRig cameraRig;

        [LabelText("车载摄像机")]
        [Tooltip("该车的摄像机。留空则自动从 Rig 解析")]
        public Camera vehicleCamera;
    }

    [Header("—— 车辆摄像机列表 ——")]
    [LabelText("摄像机列表")]
    [Tooltip("顺序必须与 GameUIManager 的车辆列表一致")]
    public CameraEntry[] cameras;

    [Header("—— 可选设置 ——")]
    [LabelText("场景主摄像机")]
    [Tooltip("场景中独立的 Main Camera。使用车载摄像机时建议拖入并会自动禁用")]
    public Camera sceneMainCamera;

    [LabelText("视角切换键")]
    public KeyCode viewToggleKey = KeyCode.V;

    VehicleCameraRig.ViewMode viewMode = VehicleCameraRig.ViewMode.ThirdPerson;
    int activeIndex = -1;

    public VehicleCameraRig.ViewMode CurrentViewMode => viewMode;

    public void SyncWithControllers(CarController[] controllers)
    {
        cameras = VehicleSwitchRegistry.BuildCameraEntries(controllers);
        activeIndex = -1;
    }

    void Update()
    {
        if (Input.GetKeyDown(viewToggleKey))
            ToggleViewMode();
    }

    public void ToggleViewMode()
    {
        viewMode = viewMode == VehicleCameraRig.ViewMode.ThirdPerson
            ? VehicleCameraRig.ViewMode.FirstPerson
            : VehicleCameraRig.ViewMode.ThirdPerson;

        ApplyViewModeToActive();
    }

    public void SetViewMode(VehicleCameraRig.ViewMode mode)
    {
        viewMode = mode;
        ApplyViewModeToActive();
    }

    public void SwitchToVehicle(int index)
    {
        if (cameras == null || cameras.Length == 0) return;
        index = Mathf.Clamp(index, 0, cameras.Length - 1);
        if (index == activeIndex) return;

        for (int i = 0; i < cameras.Length; i++)
            SetCameraActive(i, i == index);

        if (sceneMainCamera != null)
            sceneMainCamera.enabled = false;

        activeIndex = index;
        ApplyViewModeToActive();
    }

    public Camera GetActiveCamera()
    {
        if (activeIndex < 0 || cameras == null || activeIndex >= cameras.Length)
            return null;

        return ResolveCamera(activeIndex);
    }

    public Transform GetActiveCameraTransform()
    {
        Camera cam = GetActiveCamera();
        return cam != null ? cam.transform : null;
    }

    void ApplyViewModeToActive()
    {
        CameraShakeController.Instance?.StopAndReset();

        VehicleCameraRig rig = GetActiveRig();
        rig?.SetViewMode(viewMode);
    }

    VehicleCameraRig GetActiveRig()
    {
        if (activeIndex < 0 || cameras == null || activeIndex >= cameras.Length)
            return null;

        return ResolveRig(activeIndex);
    }

    void SetCameraActive(int index, bool active)
    {
        Camera cam = ResolveCamera(index);
        if (cam == null) return;

        cam.enabled = active;

        AudioListener listener = cam.GetComponent<AudioListener>();
        if (listener != null)
            listener.enabled = active;
    }

    VehicleCameraRig ResolveRig(int index)
    {
        CameraEntry entry = cameras[index];
        if (entry.cameraRig != null)
            return entry.cameraRig;

        if (entry.vehicleRoot != null)
        {
            entry.cameraRig = VehicleCameraRig.EnsureOn(entry.vehicleRoot);
            return entry.cameraRig;
        }

        return null;
    }

    Camera ResolveCamera(int index)
    {
        CameraEntry entry = cameras[index];
        if (entry.vehicleCamera != null)
            return entry.vehicleCamera;

        VehicleCameraRig rig = ResolveRig(index);
        if (rig != null)
        {
            entry.vehicleCamera = rig.VehicleCamera;
            return entry.vehicleCamera;
        }

        if (entry.vehicleRoot != null)
        {
            entry.vehicleCamera = entry.vehicleRoot.GetComponentInChildren<Camera>(true);
            return entry.vehicleCamera;
        }

        return null;
    }
}
