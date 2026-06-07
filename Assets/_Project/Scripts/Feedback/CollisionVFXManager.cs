using UnityEngine;

public class CollisionVFXManager : MonoBehaviour
{
    [SerializeField] CollisionVFXProfile profile;

    bool vfxEnabled = true;

    void Awake()
    {
        if (profile == null)
            profile = ScriptableObject.CreateInstance<CollisionVFXProfile>();
    }

    void Start()
    {
        if (CollisionManager.Instance?.EventChannel != null)
            CollisionManager.Instance.EventChannel.OnCollision.AddListener(OnCollision);
    }

    void OnDestroy()
    {
        if (CollisionManager.Instance?.EventChannel != null)
            CollisionManager.Instance.EventChannel.OnCollision.RemoveListener(OnCollision);
    }

    public void OnCollision(CollisionEventData evt)
    {
        if (!vfxEnabled || profile == null || evt.Impulse < profile.minImpulseForVFX)
            return;

        SpawnProceduralSpark(evt);
    }

    void SpawnProceduralSpark(CollisionEventData evt)
    {
        GameObject go = new GameObject("ImpactSpark");
        go.transform.SetPositionAndRotation(evt.ContactPoint, Quaternion.LookRotation(evt.ContactNormal));

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ConfigureSpark(ps, evt.Impulse);
        ps.Play();
        Destroy(go, 1.2f);
    }

    static void ConfigureSpark(ParticleSystem ps, float impulse)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Clear();

        var main = ps.main;
        main.duration = 0.4f;
        main.startLifetime = 0.35f;
        main.startSpeed = Mathf.Lerp(2f, 8f, impulse / 5000f);
        main.startSize = 0.1f;
        main.startColor = new Color(1f, 0.75f, 0.2f);
        main.loop = false;
        main.maxParticles = 50;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        short count = (short)Mathf.Clamp(Mathf.Lerp(8, 35, impulse / 5000f), 1, 50);
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.05f;
    }

    public bool IsEnabled() => vfxEnabled;
    public void SetEnabled(bool enabled) => vfxEnabled = enabled;
    public CollisionVFXProfile Profile => profile;
}
