using UnityEngine;

/// <summary>
/// 单车双视角锚点：第三人称追车 / 驾驶室第一人称。
/// 结构应通过编辑器烘焙进 Prefab；运行时仅挂载 Camera 到对应锚点。
/// </summary>
[DisallowMultipleComponent]
public class VehicleCameraRig : MonoBehaviour
{
    public enum ViewMode
    {
        ThirdPerson,
        FirstPerson
    }

    [Header("—— 锚点 ——")]
    [SerializeField] Transform thirdPersonAnchor;
    [SerializeField] Transform cockpitAnchor;
    [SerializeField] Camera vehicleCamera;

    [Header("—— 驾驶室默认位姿（本地）——")]
    [SerializeField] Vector3 cockpitLocalPosition = new Vector3(-0.38f, 1.08f, 0.35f);
    [SerializeField] Vector3 cockpitLocalEuler = Vector3.zero;

    [Header("—— 镜头参数 ——")]
    [SerializeField] float thirdPersonFov = 60f;
    [SerializeField] float cockpitFov = 72f;
    [SerializeField] float thirdPersonNearClip = 0.3f;
    [SerializeField] float cockpitNearClip = 0.08f;

    ViewMode currentMode = ViewMode.ThirdPerson;

    public ViewMode CurrentMode => currentMode;
    public Camera VehicleCamera => vehicleCamera;
    public Transform ThirdPersonAnchor => thirdPersonAnchor;
    public Transform CockpitAnchor => cockpitAnchor;

    void Awake()
    {
        EnsureSetup(false);
        ApplyViewMode(currentMode);
    }

    /// <param name="createCockpitIfMissing">为 false 时仅补全引用，不新建 CockpitAnchor（供编辑器检查用）。</param>
    public bool EnsureSetup(bool createCockpitIfMissing = true)
    {
        Transform root = transform;
        ResolveVehicleCamera();

        Transform rigRoot = EnsureChildTransform(root, "CameraRig");
        thirdPersonAnchor = EnsureAnchor(rigRoot, "ThirdPersonAnchor", ref thirdPersonAnchor, CreateThirdPersonAnchorPose);
        if (createCockpitIfMissing || cockpitAnchor != null)
            cockpitAnchor = EnsureAnchor(rigRoot, "CockpitAnchor", ref cockpitAnchor, ApplyDefaultCockpitPose);

        if (vehicleCamera == null)
            vehicleCamera = CreateDefaultCamera(thirdPersonAnchor);

        MigrateLegacyCameraTransform();
        return thirdPersonAnchor != null && cockpitAnchor != null && vehicleCamera != null;
    }

    public void SetViewMode(ViewMode mode)
    {
        if (!EnsureSetup())
            return;

        currentMode = mode;
        ApplyViewMode(mode);
    }

    public void ApplyViewMode(ViewMode mode)
    {
        if (thirdPersonAnchor == null || cockpitAnchor == null || vehicleCamera == null)
            return;

        currentMode = mode;
        Transform anchor = mode == ViewMode.FirstPerson ? cockpitAnchor : thirdPersonAnchor;
        Transform camTransform = vehicleCamera.transform;
        camTransform.SetParent(anchor, false);
        ResetCameraLocalPose();

        vehicleCamera.fieldOfView = mode == ViewMode.FirstPerson ? cockpitFov : thirdPersonFov;
        vehicleCamera.nearClipPlane = mode == ViewMode.FirstPerson ? cockpitNearClip : thirdPersonNearClip;
    }

    public void ResetCameraLocalPose()
    {
        if (vehicleCamera == null)
            return;

        Transform camTransform = vehicleCamera.transform;
        camTransform.localPosition = Vector3.zero;
        camTransform.localRotation = Quaternion.identity;
    }

    public void ApplyDefaultCockpitPose()
    {
        if (cockpitAnchor == null)
            return;

        if (TryEstimateCockpitPose(out Vector3 localPos, out Vector3 localEuler))
        {
            cockpitAnchor.localPosition = localPos;
            cockpitAnchor.localRotation = Quaternion.Euler(localEuler);
            return;
        }

        cockpitAnchor.localPosition = cockpitLocalPosition;
        cockpitAnchor.localRotation = Quaternion.Euler(cockpitLocalEuler);
    }

    public void AssignReferences(Transform thirdPerson, Transform cockpit, Camera camera)
    {
        thirdPersonAnchor = thirdPerson;
        cockpitAnchor = cockpit;
        vehicleCamera = camera;
    }

    void ResolveVehicleCamera()
    {
        if (vehicleCamera != null)
            return;

        foreach (Camera cam in GetComponentsInChildren<Camera>(true))
        {
            vehicleCamera = cam;
            return;
        }
    }

    void MigrateLegacyCameraTransform()
    {
        if (vehicleCamera == null || thirdPersonAnchor == null)
            return;

        Transform camTransform = vehicleCamera.transform;
        if (camTransform.parent == thirdPersonAnchor || camTransform.parent == cockpitAnchor)
            return;

        thirdPersonAnchor.localPosition = camTransform.localPosition;
        thirdPersonAnchor.localRotation = camTransform.localRotation;
        camTransform.SetParent(thirdPersonAnchor, false);
        camTransform.localPosition = Vector3.zero;
        camTransform.localRotation = Quaternion.identity;
    }

    void CreateThirdPersonAnchorPose()
    {
        if (thirdPersonAnchor == null)
            return;

        if (vehicleCamera != null && vehicleCamera.transform.parent != thirdPersonAnchor)
        {
            thirdPersonAnchor.localPosition = vehicleCamera.transform.localPosition;
            thirdPersonAnchor.localRotation = vehicleCamera.transform.localRotation;
            return;
        }

        thirdPersonAnchor.localPosition = new Vector3(0.12f, 2.79f, -4.25f);
        thirdPersonAnchor.localRotation = Quaternion.Euler(20.412f, 0f, 0f);
    }

    Camera CreateDefaultCamera(Transform parent)
    {
        GameObject cameraGo = new GameObject("Camera");
        cameraGo.transform.SetParent(parent, false);
        Camera cam = cameraGo.AddComponent<Camera>();
        cam.fieldOfView = thirdPersonFov;
        cam.nearClipPlane = thirdPersonNearClip;
        cam.farClipPlane = 1000f;
        if (cameraGo.GetComponent<AudioListener>() == null)
            cameraGo.AddComponent<AudioListener>();
        return cam;
    }

    static Transform EnsureChildTransform(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
            return child;

        GameObject go = new GameObject(name);
        child = go.transform;
        child.SetParent(parent, false);
        child.localPosition = Vector3.zero;
        child.localRotation = Quaternion.identity;
        child.localScale = Vector3.one;
        return child;
    }

    delegate void AnchorPoseSetup();

    static Transform EnsureAnchor(Transform rigRoot, string name, ref Transform cached, AnchorPoseSetup poseSetup)
    {
        if (cached != null)
            return cached;

        Transform existing = rigRoot.Find(name);
        if (existing != null)
        {
            cached = existing;
            return cached;
        }

        GameObject anchorGo = new GameObject(name);
        cached = anchorGo.transform;
        cached.SetParent(rigRoot, false);
        poseSetup?.Invoke();
        return cached;
    }

    bool TryEstimateCockpitPose(out Vector3 localPos, out Vector3 localEuler)
    {
        localPos = cockpitLocalPosition;
        localEuler = cockpitLocalEuler;

        if (!TryGetVehicleBounds(out Bounds bounds))
            return false;

        Vector3 center = transform.InverseTransformPoint(bounds.center);
        float halfHeight = bounds.extents.y;
        float halfLength = bounds.extents.z;
        float halfWidth = bounds.extents.x;

        localPos = new Vector3(
            center.x - halfWidth * 0.35f,
            center.y - halfHeight * 0.15f + halfHeight * 0.55f,
            center.z + halfLength * 0.15f);
        localEuler = Vector3.zero;
        return true;
    }

    bool TryGetVehicleBounds(out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is ParticleSystemRenderer)
                continue;
            if (renderer.GetComponentInParent<Camera>() != null)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    public static VehicleCameraRig EnsureOn(Transform vehicleRoot)
    {
        if (vehicleRoot == null)
            return null;

        VehicleCameraRig rig = vehicleRoot.GetComponent<VehicleCameraRig>();
        if (rig == null)
            rig = vehicleRoot.gameObject.AddComponent<VehicleCameraRig>();

        rig.EnsureSetup();
        return rig;
    }

    void OnDrawGizmosSelected()
    {
        if (cockpitAnchor == null)
            return;

        Gizmos.color = new Color(1f, 0.55f, 0.1f, 0.95f);
        Gizmos.matrix = cockpitAnchor.localToWorldMatrix;
        Gizmos.DrawWireSphere(Vector3.zero, 0.08f);
        Gizmos.DrawLine(Vector3.zero, Vector3.forward * 0.45f);
    }
}
