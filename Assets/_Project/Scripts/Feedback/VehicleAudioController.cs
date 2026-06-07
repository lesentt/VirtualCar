using UnityEngine;

/// <summary>
/// 车辆驾驶音效：发动机 RPM 分层、刹车尖叫、喇叭。
/// </summary>
[DisallowMultipleComponent]
public class VehicleAudioController : MonoBehaviour
{
    [SerializeField] CarController car;
    [SerializeField] ProjectAudioLibrary library;
    [SerializeField] float maxSpeedKmh = 140f;
    [SerializeField] float engineVolume = 0.28f;
    [SerializeField] float startupVolume = 0.35f;
    [SerializeField] float brakeVolume = 0.45f;
    [SerializeField] float stopSpeedKmh = 0.8f;
    [SerializeField] KeyCode hornKey = KeyCode.H;

    AudioSource engineSource;
    AudioSource brakeSource;
    AudioSource fxSource;
    AudioClip[] rpmClips;
    int activeClipIndex = -1;
    bool wasStopped = true;
    float engineResumeTime;
    float masterVolume = 1f;
    bool audioEnabled = true;

    void Awake()
    {
        if (car == null)
            car = GetComponent<CarController>();

        if (library == null)
            library = CollisionConfigProvider.ProjectAudioLibrary;

        engineSource = CreateChildSource("Engine", loop: true);
        brakeSource = CreateChildSource("Brake", loop: true);
        fxSource = CreateChildSource("VehicleFx", loop: false);

        rpmClips = new[]
        {
            library?.engineIdle,
            library?.engineLow,
            library?.engineMed,
            library?.engineHigh,
            library?.engineMaxRpm
        };
    }

    void OnEnable()
    {
        wasStopped = true;
        activeClipIndex = -1;
        engineResumeTime = 0f;
    }

    void OnDisable()
    {
        if (engineSource != null)
            engineSource.Stop();
        if (brakeSource != null)
            brakeSource.Stop();
    }

    void Update()
    {
        if (car == null || library == null)
            return;

        if (!audioEnabled || !car.isPlayerControlled || car.IsDisabled())
        {
            StopDrivingAudio();
            wasStopped = true;
            return;
        }

        UpdateEngine();
        UpdateBrakeAudio();

        if (Input.GetKeyDown(hornKey) && library.horn != null)
            fxSource.PlayOneShot(library.horn, masterVolume);
    }

    void UpdateEngine()
    {
        float speed = car.GetSpeed();
        if (speed < stopSpeedKmh)
        {
            if (!wasStopped && engineSource.isPlaying)
                engineSource.Stop();

            activeClipIndex = -1;
            wasStopped = true;
            engineResumeTime = 0f;
            return;
        }

        if (wasStopped)
        {
            wasStopped = false;
            PlayStartup();
        }

        if (Time.time < engineResumeTime)
        {
            if (engineSource.isPlaying)
                engineSource.Stop();
            activeClipIndex = -1;
            return;
        }

        float speedNorm = Mathf.Clamp01(speed / Mathf.Max(maxSpeedKmh, 1f));
        float throttle = Mathf.Abs(car.GetThrottleInput());
        float load = Mathf.Clamp01(speedNorm * 0.75f + throttle * 0.25f);
        int clipIndex = Mathf.Clamp(Mathf.FloorToInt(load * (rpmClips.Length - 1)), 0, rpmClips.Length - 1);
        AudioClip clip = rpmClips[clipIndex];

        if (clip == null)
        {
            engineSource.Stop();
            return;
        }

        if (clipIndex != activeClipIndex || engineSource.clip != clip)
        {
            engineSource.clip = clip;
            engineSource.Play();
            activeClipIndex = clipIndex;
        }

        engineSource.volume = engineVolume * masterVolume;
        engineSource.pitch = Mathf.Lerp(0.88f, 1.18f, load);
    }

    void PlayStartup()
    {
        if (library.engineStartup == null)
        {
            engineResumeTime = 0f;
            return;
        }

        fxSource.PlayOneShot(library.engineStartup, startupVolume * masterVolume);
        engineResumeTime = Time.time + library.engineStartup.length * 0.9f;
    }

    void UpdateBrakeAudio()
    {
        if (library.brakeScreech == null)
            return;

        bool shouldBrake = (car.IsBraking() || car.IsHandbraking()) && car.GetSpeed() > 12f;
        if (!shouldBrake)
        {
            if (brakeSource.isPlaying)
                brakeSource.Stop();
            return;
        }

        if (!brakeSource.isPlaying)
        {
            brakeSource.clip = library.brakeScreech;
            brakeSource.loop = true;
            brakeSource.Play();
        }

        float speedNorm = Mathf.Clamp01(car.GetSpeed() / Mathf.Max(maxSpeedKmh, 1f));
        brakeSource.volume = brakeVolume * masterVolume * Mathf.Lerp(0.35f, 1f, speedNorm);
        brakeSource.pitch = Mathf.Lerp(0.9f, 1.15f, speedNorm);
    }

    void StopDrivingAudio()
    {
        if (engineSource != null && engineSource.isPlaying)
            engineSource.Stop();
        if (brakeSource != null && brakeSource.isPlaying)
            brakeSource.Stop();
        activeClipIndex = -1;
    }

    AudioSource CreateChildSource(string name, bool loop)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        AudioSource src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = 1f;
        src.minDistance = 3f;
        src.maxDistance = 45f;
        src.rolloffMode = AudioRolloffMode.Linear;
        return src;
    }

    public void SetMasterVolume(float volume) => masterVolume = Mathf.Clamp01(volume);
    public void SetEnabled(bool enabled)
    {
        audioEnabled = enabled;
        if (!audioEnabled)
        {
            OnDisable();
            wasStopped = true;
        }
    }
}
