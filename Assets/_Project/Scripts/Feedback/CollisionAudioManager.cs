using UnityEngine;

public class CollisionAudioManager : MonoBehaviour
{
    public static CollisionAudioManager Instance { get; private set; }

    [SerializeField] CollisionAudioProfile profile;
    [SerializeField] int poolSize = 8;

    bool audioEnabled = true;
    float masterVolume = 1f;
    AudioSource[] pool;
    AudioSource scrapeSource;
    int scrapeContactCount;
    float scrapeIntensity;

    const float ScrapeMinTangentSpeed = 1.5f;
    const float ScrapeFadeSpeed = 6f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (profile == null)
            profile = CollisionConfigProvider.AudioProfile;

        pool = new AudioSource[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            pool[i] = gameObject.AddComponent<AudioSource>();
            pool[i].playOnAwake = false;
            pool[i].spatialBlend = 1f;
            pool[i].minDistance = 2f;
            pool[i].maxDistance = 35f;
            pool[i].rolloffMode = AudioRolloffMode.Linear;
        }

        scrapeSource = gameObject.AddComponent<AudioSource>();
        scrapeSource.playOnAwake = false;
        scrapeSource.loop = true;
        scrapeSource.spatialBlend = 1f;
        scrapeSource.minDistance = 2f;
        scrapeSource.maxDistance = 30f;
        scrapeSource.rolloffMode = AudioRolloffMode.Linear;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        Subscribe(false);
    }

    void Start()
    {
        Subscribe(true);
    }

    void Update()
    {
        UpdateScrapeFade();
    }

    void Subscribe(bool subscribe)
    {
        if (CollisionManager.Instance?.EventChannel == null)
            return;

        if (subscribe)
            CollisionManager.Instance.EventChannel.OnCollision.AddListener(OnCollision);
        else
            CollisionManager.Instance.EventChannel.OnCollision.RemoveListener(OnCollision);
    }

    public void OnCollision(CollisionEventData evt)
    {
        if (!audioEnabled || profile == null || pool == null || pool.Length == 0)
            return;

        CollisionAudioProfile.ClipSet set = ResolveClipSet(evt.SurfaceA, evt.SurfaceB);
        AudioClip clip = SelectByImpulse(set, evt.Impulse);
        if (clip == null)
            return;

        AudioSource src = GetFreeSource();
        if (src == null)
            return;

        float volumeScale = set != null ? set.volumeScale : 1f;
        float pitchMin = set != null ? set.pitchMin : 0.9f;
        float pitchMax = set != null ? set.pitchMax : 1.1f;
        float heavyThreshold = Mathf.Max(profile.heavyImpulseThreshold, 1f);

        src.transform.position = evt.ContactPoint;
        src.clip = clip;
        src.volume = Mathf.Lerp(0.3f, 1f, evt.Impulse / heavyThreshold) * volumeScale * masterVolume;
        src.pitch = Random.Range(pitchMin, pitchMax);
        src.Play();
    }

    public void ReportScrape(Vector3 contactPoint, float tangentSpeed, float normalForce)
    {
        if (!audioEnabled || profile == null || profile.scrapeLoop == null)
            return;

        if (tangentSpeed < ScrapeMinTangentSpeed || normalForce < 80f)
            return;

        scrapeContactCount++;
        float intensity = Mathf.Clamp01(tangentSpeed / 8f) * Mathf.Clamp01(normalForce / 2500f);
        scrapeIntensity = Mathf.Max(scrapeIntensity, intensity);
        scrapeSource.transform.position = contactPoint;

        if (!scrapeSource.isPlaying)
        {
            scrapeSource.clip = profile.scrapeLoop;
            scrapeSource.Play();
        }

        scrapeSource.volume = scrapeIntensity * masterVolume * 0.7f;
        scrapeSource.pitch = Mathf.Lerp(0.85f, 1.1f, scrapeIntensity);
    }

    public void EndScrapeFrame()
    {
        scrapeContactCount = 0;
    }

    void UpdateScrapeFade()
    {
        if (scrapeSource == null)
            return;

        if (scrapeContactCount > 0)
            return;

        scrapeIntensity = Mathf.MoveTowards(scrapeIntensity, 0f, ScrapeFadeSpeed * Time.deltaTime);
        if (scrapeIntensity <= 0.01f)
        {
            scrapeIntensity = 0f;
            if (scrapeSource.isPlaying)
                scrapeSource.Stop();
            return;
        }

        scrapeSource.volume = scrapeIntensity * masterVolume * 0.7f;
    }

    public void PlayOneShotAt(AudioClip clip, Vector3 position, float volumeScale = 1f, float pitch = 1f)
    {
        if (!audioEnabled || clip == null)
            return;

        AudioSource src = GetFreeSource();
        if (src == null)
            return;

        src.transform.position = position;
        src.clip = clip;
        src.volume = Mathf.Clamp01(volumeScale) * masterVolume;
        src.pitch = pitch;
        src.Play();
    }

    CollisionAudioProfile.ClipSet ResolveClipSet(SurfaceMaterialType a, SurfaceMaterialType b)
    {
        SurfaceMaterialType other = a == SurfaceMaterialType.Metal ? b : a;

        switch (other)
        {
            case SurfaceMaterialType.Concrete:
            case SurfaceMaterialType.Ground:
                return profile.metalConcrete;
            case SurfaceMaterialType.Plant:
                return profile.metalPlant;
            case SurfaceMaterialType.Guardrail:
                return profile.metalGuardrail;
            case SurfaceMaterialType.Metal:
                return profile.metalMetal;
            default:
                return profile.metalConcrete;
        }
    }

    AudioClip SelectByImpulse(CollisionAudioProfile.ClipSet set, float impulse)
    {
        if (set == null)
            return GetProceduralClip(impulse);

        if (impulse >= profile.heavyImpulseThreshold)
            return set.heavy ?? GetProceduralClip(impulse, 2);
        if (impulse >= profile.lightImpulseThreshold)
            return set.medium ?? GetProceduralClip(impulse, 1);
        return set.light ?? GetProceduralClip(impulse, 0);
    }

    static AudioClip GetProceduralClip(float impulse, int tier = -1)
    {
        if (tier < 0)
        {
            if (impulse >= 4000f) tier = 2;
            else if (impulse >= 500f) tier = 1;
            else tier = 0;
        }

        tier = Mathf.Clamp(tier, 0, 3);
        return CreateFallbackImpactClip(tier);
    }

    static AudioClip CreateFallbackImpactClip(int tier)
    {
        int sampleRate = 44100;
        float duration = 0.08f + tier * 0.05f;
        float amplitude = 0.35f + tier * 0.2f;
        int count = Mathf.CeilToInt(sampleRate * duration);
        float[] data = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)count;
            float env = Mathf.Exp(-t * 12f) * (1f - t);
            float thump = Mathf.Sin(t * Mathf.PI) * 0.5f;
            float noise = (Random.value * 2f - 1f) * 0.3f;
            data[i] = (thump + noise) * env * amplitude;
        }

        AudioClip clip = AudioClip.Create($"FallbackImpact_{tier}", count, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    AudioSource GetFreeSource()
    {
        foreach (AudioSource src in pool)
        {
            if (!src.isPlaying)
                return src;
        }

        return pool[0];
    }

    public bool IsEnabled() => audioEnabled;
    public void SetEnabled(bool enabled) => audioEnabled = enabled;
    public float GetMasterVolume() => masterVolume;
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        SyncVehicleAudio();
    }

    public CollisionAudioProfile Profile => profile;

    void SyncVehicleAudio()
    {
        CarController car = GameSettingsManager.Instance != null
            ? GameSettingsManager.Instance.GetActiveCar()
            : null;

        if (car == null)
        {
            foreach (CarController candidate in FindObjectsOfType<CarController>())
            {
                if (candidate.isPlayerControlled)
                {
                    car = candidate;
                    break;
                }
            }
        }

        if (car == null)
            return;

        VehicleAudioController vehicleAudio = car.GetComponent<VehicleAudioController>();
        if (vehicleAudio != null)
        {
            vehicleAudio.SetMasterVolume(masterVolume);
            vehicleAudio.SetEnabled(audioEnabled);
        }
    }
}
