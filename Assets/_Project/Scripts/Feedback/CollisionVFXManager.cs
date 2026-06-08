using System.Collections.Generic;
using UnityEngine;

public class CollisionVFXManager : MonoBehaviour
{
    [SerializeField] CollisionVFXProfile profile;

    readonly Dictionary<VehicleState, GameObject> vehicleBigFires = new Dictionary<VehicleState, GameObject>();

    bool vfxEnabled = true;

    void Awake()
    {
        if (profile == null)
            profile = CollisionConfigProvider.VfxProfile;

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

        GameObject vehicleRoot = ResolveVehicleRoot(evt.ObjectA);
        if (vehicleRoot == null)
            return;

        SpawnImpactVfx(evt);

        VehicleState state = vehicleRoot.GetComponent<VehicleState>();
        if (state != null && state.GetDamagePercentApprox() > profile.bigFireDamagePercent)
            TryAttachBigFire(state, vehicleRoot);
    }

    public void ClearVehicleEffects(VehicleState state)
    {
        if (state == null || !vehicleBigFires.TryGetValue(state, out GameObject fire) || fire == null)
            return;

        Destroy(fire);
        vehicleBigFires.Remove(state);
    }

    void SpawnImpactVfx(CollisionEventData evt)
    {
        if (evt.Impulse >= profile.minImpulseForImpactBigFire)
        {
            SpawnImpactEffect(profile.bigFirePrefab, evt.ContactPoint, evt.ContactNormal, profile.impactBigFireLifetime);
            return;
        }

        if (evt.Impulse >= profile.minImpulseForFire)
        {
            SpawnImpactEffect(profile.firePrefab, evt.ContactPoint, evt.ContactNormal, profile.fireLifetime);
            return;
        }

        SpawnImpactEffect(profile.smokePrefab, evt.ContactPoint, evt.ContactNormal, profile.smokeLifetime);
    }

    void SpawnImpactEffect(GameObject prefab, Vector3 position, Vector3 normal, float lifetime)
    {
        if (prefab == null)
            return;

        Quaternion rotation = normal.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(normal)
            : Quaternion.identity;

        GameObject instance = Instantiate(prefab, position, rotation);
        PlayAllParticles(instance);
        Destroy(instance, Mathf.Max(0.5f, lifetime));
    }

    void TryAttachBigFire(VehicleState state, GameObject vehicleRoot)
    {
        if (profile.bigFirePrefab == null || vehicleBigFires.ContainsKey(state))
            return;

        GameObject instance = Instantiate(profile.bigFirePrefab, vehicleRoot.transform);
        instance.transform.localPosition = profile.bigFireLocalOffset;
        instance.transform.localRotation = Quaternion.identity;
        PlayAllParticles(instance);
        vehicleBigFires[state] = instance;
    }

    static GameObject ResolveVehicleRoot(GameObject root)
    {
        if (root == null)
            return null;

        if (root.GetComponent<VehicleState>() != null || root.GetComponentInChildren<CarController>() != null)
            return root;

        return null;
    }

    static void PlayAllParticles(GameObject root)
    {
        ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
            systems[i].Play(true);
    }

    public bool IsEnabled() => vfxEnabled;
    public void SetEnabled(bool enabled) => vfxEnabled = enabled;
    public CollisionVFXProfile Profile => profile;
}
