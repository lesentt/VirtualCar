using UnityEngine;

public class CollisionAudioManager : MonoBehaviour
{
    [SerializeField] CollisionAudioProfile profile;
    [SerializeField] int poolSize = 8;

    AudioSource[] pool;

    void Awake()
    {
        if (profile == null)
            profile = ScriptableObject.CreateInstance<CollisionAudioProfile>();

        pool = new AudioSource[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            pool[i] = gameObject.AddComponent<AudioSource>();
            pool[i].playOnAwake = false;
            pool[i].spatialBlend = 1f;
        }
    }

    void Start()
    {
        Subscribe(true);
    }

    void OnDestroy()
    {
        Subscribe(false);
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
        if (profile == null || pool == null || pool.Length == 0)
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
        src.volume = Mathf.Lerp(0.3f, 1f, evt.Impulse / heavyThreshold) * volumeScale;
        src.pitch = Random.Range(pitchMin, pitchMax);
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
}
