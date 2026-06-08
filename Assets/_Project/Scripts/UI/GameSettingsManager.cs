using UnityEngine;

/// <summary>
/// 运行时配置中心：从 UI 读取/写入车辆、形变、碰撞、反馈与环境参数。
/// </summary>
public class GameSettingsManager : MonoBehaviour
{
    const string PrefsKey = "VirtualVehicle.RuntimeSettings";

    public static GameSettingsManager Instance { get; private set; }

    [SerializeField] GameUIManager uiManager;
    [SerializeField] SceneResetService sceneResetService;

    RuntimeGameSettings settings = new RuntimeGameSettings();
    RuntimeGameSettings defaults = new RuntimeGameSettings();
    DeformationConfig deformationConfig;

    CameraShakeController cameraShake;
    CollisionAudioManager audioManager;
    CollisionVFXManager vfxManager;

    public RuntimeGameSettings Settings => settings;
    public event System.Action SettingsChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        if (uiManager == null)
            uiManager = FindObjectOfType<GameUIManager>();
        if (sceneResetService == null)
            sceneResetService = FindObjectOfType<SceneResetService>();

        deformationConfig = CollisionConfigProvider.GetDeformationConfig();
        cameraShake = FindObjectOfType<CameraShakeController>();
        audioManager = FindObjectOfType<CollisionAudioManager>();
        vfxManager = FindObjectOfType<CollisionVFXManager>();

        CaptureDefaultsFromScene();
        LoadFromPlayerPrefs();
        settings.NormalizeVfxSettings();
        ApplyAll();
    }

    public void CaptureDefaultsFromScene()
    {
        defaults = new RuntimeGameSettings();
        CarController car = GetActiveCar();
        VehicleState state = car != null ? car.GetComponent<VehicleState>() : null;

        if (car != null)
            defaults = RuntimeGameSettings.CreateFromVehicle(car, state);

        defaults.CaptureDeformation(deformationConfig);
        defaults.CaptureFeedback(cameraShake, audioManager, vfxManager);
        defaults.CaptureCollision(CollisionManager.Instance, CollisionConfigProvider.VehicleMetalProfile);

        settings = CloneSettings(defaults);
    }

    public void SyncFromActiveVehicle()
    {
        CarController car = GetActiveCar();
        if (car == null) return;

        VehicleState state = car.GetComponent<VehicleState>();
        RuntimeGameSettings vehiclePart = RuntimeGameSettings.CreateFromVehicle(car, state);

        settings.motorTorque = vehiclePart.motorTorque;
        settings.maxSteerAngle = vehiclePart.maxSteerAngle;
        settings.brakeTorque = vehiclePart.brakeTorque;
        settings.handbrakeTorque = vehiclePart.handbrakeTorque;
        settings.gripCoefficient = vehiclePart.gripCoefficient;
        settings.rollingResistanceCoeff = vehiclePart.rollingResistanceCoeff;
        settings.airDragCoefficient = vehiclePart.airDragCoefficient;
        settings.mass = vehiclePart.mass;
        settings.centerOfMassY = vehiclePart.centerOfMassY;
        settings.damageSensitivity = vehiclePart.damageSensitivity;

        NotifyChanged();
    }

    public void ApplyAll()
    {
        ApplyToActiveVehicle();
        ApplyDeformationSettings();
        ApplyCollisionSettings();
        ApplyFeedbackSettings();
        ApplyEnvironmentSettings();
        ApplySessionSettings();
        NotifyChanged();
    }

    public void ApplyToActiveVehicle()
    {
        CarController car = GetActiveCar();
        if (car == null) return;

        car.SetMotorTorque(settings.motorTorque);
        car.SetMaxSteerAngle(settings.maxSteerAngle);
        car.SetBrakeTorque(settings.brakeTorque);
        car.SetHandbrakeTorque(settings.handbrakeTorque);
        car.SetGripCoefficient(settings.gripCoefficient);
        car.SetRollingResistanceCoeff(settings.rollingResistanceCoeff);
        car.SetAirDragCoefficient(settings.airDragCoefficient);
        car.SetMass(settings.mass);
        car.SetCenterOfMassY(settings.centerOfMassY);

        VehicleState state = car.GetComponent<VehicleState>();
        state?.SetDamageSensitivity(settings.damageSensitivity);
    }

    public void ApplyDeformationSettings()
    {
        settings.ApplyDeformation(deformationConfig);
    }

    public void ApplyCollisionSettings()
    {
        if (CollisionManager.Instance != null)
            CollisionManager.Instance.SetMinImpulse(settings.minCollisionImpulse);

        CollisionEntityProfile profile = CollisionConfigProvider.VehicleMetalProfile;
        if (profile != null)
            profile.minReportSpeed = settings.minReportSpeed;
    }

    public void ApplyFeedbackSettings()
    {
        if (cameraShake != null)
        {
            cameraShake.SetEnabled(settings.cameraShakeEnabled);
            cameraShake.SetMinImpulse(settings.cameraShakeMinImpulse);
            cameraShake.SetMaxShakeAmplitude(settings.cameraShakeAmplitude);
        }

        if (audioManager != null)
        {
            audioManager.SetEnabled(settings.audioEnabled);
            audioManager.SetMasterVolume(settings.audioMasterVolume);
            if (audioManager.Profile != null)
            {
                audioManager.Profile.lightImpulseThreshold = settings.audioLightThreshold;
                audioManager.Profile.heavyImpulseThreshold = settings.audioHeavyThreshold;
            }
        }

        ApplyVehicleAudioSettings();

        if (vfxManager != null)
        {
            vfxManager.SetEnabled(settings.vfxEnabled);
            if (vfxManager.Profile != null)
                settings.ApplyVfx(vfxManager.Profile);
        }
    }

    public void ApplyEnvironmentSettings()
    {
        foreach (DestructibleProp prop in FindObjectsOfType<DestructibleProp>())
            prop.ApplyGlobalModifiers(settings.propToppleThresholdScale, settings.propToppleForceScale);
    }

    public void ApplySessionSettings()
    {
        Time.timeScale = Mathf.Clamp(settings.timeScale, 0.1f, 2f);
        if (uiManager != null)
            uiManager.SetHudVisible(settings.showHud);
    }

    public void SetFloat(System.Action<float> setter, float value)
    {
        setter(value);
        ApplyAll();
    }

    public void SetBool(System.Action<bool> setter, bool value)
    {
        setter(value);
        ApplyAll();
    }

    public void ResetToDefaults()
    {
        settings = CloneSettings(defaults);
        ApplyAll();
    }

    public void ApplyPresetEconomy()
    {
        settings.ApplyPresetEconomy();
        ApplyAll();
    }

    public void ApplyPresetSport()
    {
        settings.ApplyPresetSport();
        ApplyAll();
    }

    public void ApplyPresetPolice()
    {
        settings.ApplyPresetPolice();
        ApplyAll();
    }

    public void ResetScene()
    {
        sceneResetService?.ResetScene();
        ApplyAll();
    }

    public void ResetActiveVehicle()
    {
        sceneResetService?.ResetActiveVehicle(GetActiveCar());
        ApplyAll();
    }

    public void UnstuckActiveVehicle()
    {
        sceneResetService?.UnstuckVehicle(GetActiveCar());
    }

    public void SaveToPlayerPrefs()
    {
        string json = JsonUtility.ToJson(settings);
        PlayerPrefs.SetString(PrefsKey, json);
        PlayerPrefs.Save();
    }

    public void LoadFromPlayerPrefs()
    {
        if (!PlayerPrefs.HasKey(PrefsKey))
            return;

        string json = PlayerPrefs.GetString(PrefsKey);
        if (string.IsNullOrEmpty(json))
            return;

        JsonUtility.FromJsonOverwrite(json, settings);
    }

    public CarController GetActiveCar()
    {
        if (uiManager != null)
            return uiManager.GetActiveController();

        foreach (CarController car in FindObjectsOfType<CarController>())
        {
            if (car.isPlayerControlled)
                return car;
        }

        return FindObjectOfType<CarController>();
    }

    public CollisionEventRecorder GetRecorder() => CollisionManager.Instance?.Recorder;

    void ApplyVehicleAudioSettings()
    {
        CarController car = GetActiveCar();
        if (car == null)
            return;

        VehicleAudioController vehicleAudio = car.GetComponent<VehicleAudioController>();
        if (vehicleAudio == null)
            return;

        vehicleAudio.SetEnabled(settings.audioEnabled);
        vehicleAudio.SetMasterVolume(settings.audioMasterVolume);
    }

    void NotifyChanged() => SettingsChanged?.Invoke();

    static RuntimeGameSettings CloneSettings(RuntimeGameSettings source)
    {
        return JsonUtility.FromJson<RuntimeGameSettings>(JsonUtility.ToJson(source));
    }
}
