using UnityEngine;

/// <summary>
/// 车辆摄像机切换脚本。
/// 功能：按数字键切换车辆时，同步启用对应车载摄像机。
/// </summary>
public class VehicleCameraController : MonoBehaviour
{
    [System.Serializable]
    public class CameraEntry
    {
        [LabelText("车辆根物体")]
        [Tooltip("车辆根物体（Car），用于自动查找子物体中的 Camera")]
        public Transform vehicleRoot;

        [LabelText("车载摄像机")]
        [Tooltip("该车的摄像机。留空则自动在 vehicleRoot 子物体中查找 Camera_car")]
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

    private int activeIndex = -1;

    public void SyncWithControllers(CarController[] controllers)
    {
        cameras = VehicleSwitchRegistry.BuildCameraEntries(controllers);
        activeIndex = -1;
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

    Camera ResolveCamera(int index)
    {
        CameraEntry entry = cameras[index];
        if (entry.vehicleCamera != null) return entry.vehicleCamera;

        if (entry.vehicleRoot != null)
        {
            entry.vehicleCamera = entry.vehicleRoot.GetComponentInChildren<Camera>(true);
            return entry.vehicleCamera;
        }

        return null;
    }
}
