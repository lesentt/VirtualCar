using UnityEngine;

[DefaultExecutionOrder(-100)]
public class CollisionManager : MonoBehaviour
{
    public static CollisionManager Instance { get; private set; }

    [SerializeField] CollisionEventChannel eventChannel;
    [SerializeField] DeformationConfig defaultDeformationConfig;
    [SerializeField] float minImpulse = 300f;

    CollisionEventRecorder recorder;

    public CollisionEventChannel EventChannel => eventChannel;
    public CollisionEventRecorder Recorder => recorder;
    public DeformationConfig DefaultDeformationConfig => defaultDeformationConfig;

    public void SetConfig(DeformationConfig deformConfig, CollisionEventChannel channel)
    {
        if (deformConfig != null)
            defaultDeformationConfig = deformConfig;
        if (channel != null)
            eventChannel = channel;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        recorder = GetComponent<CollisionEventRecorder>();
        if (recorder == null)
            recorder = gameObject.AddComponent<CollisionEventRecorder>();

        eventChannel = CollisionConfigProvider.GetEventChannel(eventChannel);
        defaultDeformationConfig = CollisionConfigProvider.GetDeformationConfig(defaultDeformationConfig);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Report(CollisionReporter reporter, Collision collision)
    {
        if (reporter == null || collision == null)
            return;

        GameObject selfRoot = reporter.Root;
        GameObject otherRoot = collision.gameObject.transform.root.gameObject;

        if (CollisionTypes.IsFallenEnvironmentObject(collision.gameObject))
            return;

        float impulse = CollisionTypes.ComputeVehicleImpulse(collision, reporter);
        if (impulse < minImpulse)
            return;

        CollisionEventData evt = BuildEvent(reporter, collision, selfRoot, otherRoot, impulse);

        ApplyDeformation(evt, selfRoot);
        ApplyVehicleState(evt, selfRoot);

        recorder.Record(evt);
        eventChannel?.Raise(evt);
    }

    CollisionEventData BuildEvent(CollisionReporter reporter, Collision collision,
        GameObject selfRoot, GameObject otherRoot, float impulse)
    {
        ContactPoint primary = collision.GetContact(0);
        bool otherIsVehicle = otherRoot.GetComponentInChildren<CarController>() != null;
        DeformablePart hitPart = ResolveHitPart(collision, selfRoot);

        return new CollisionEventData
        {
            Timestamp = Time.time,
            ObjectA = selfRoot,
            ObjectB = otherRoot,
            Type = otherIsVehicle ? CollisionType.VehicleVehicle : CollisionType.VehicleEnvironment,
            HitPart = hitPart != null ? hitPart.PartType : VehiclePartType.OtherNonDeformable,
            HitDeformable = hitPart,
            ContactPoint = primary.point,
            ContactNormal = primary.normal,
            RelativeVelocity = collision.relativeVelocity.magnitude,
            Impulse = impulse,
            SurfaceA = ResolveSurface(reporter, selfRoot),
            SurfaceB = ResolveSurface(otherRoot)
        };
    }

    void ApplyDeformation(CollisionEventData evt, GameObject selfRoot)
    {
        VehicleDeformationController deform = selfRoot.GetComponent<VehicleDeformationController>();
        if (deform != null)
        {
            deform.ApplyDeformation(evt);
            return;
        }

        selfRoot.GetComponent<VehicleState>()?.ApplyCollisionDamage(evt.Impulse, 0f);
    }

    void ApplyVehicleState(CollisionEventData evt, GameObject selfRoot)
    {
        selfRoot.GetComponent<VehicleState>()?.OnCollision(evt);
    }

    static DeformablePart ResolveHitPart(Collision collision, GameObject vehicleRoot)
    {
        foreach (ContactPoint cp in collision.contacts)
        {
            DeformablePart part = cp.thisCollider.GetComponent<DeformablePart>();
            if (part == null)
                part = cp.thisCollider.GetComponentInParent<DeformablePart>();

            if (part != null && part.transform.root.gameObject == vehicleRoot)
                return part;
        }

        if (collision.contactCount > 0)
            return PartRegionRegistry.FindClosestPart(vehicleRoot, collision.GetContact(0).point);

        return null;
    }

    static SurfaceMaterialType ResolveSurface(CollisionReporter reporter, GameObject root)
    {
        if (reporter?.Profile != null)
            return reporter.Profile.surfaceMaterial;
        return ResolveSurface(root);
    }

    static SurfaceMaterialType ResolveSurface(GameObject root)
    {
        if (root == null)
            return SurfaceMaterialType.Unknown;

        CollisionReporter reporter = root.GetComponentInChildren<CollisionReporter>();
        if (reporter?.Profile != null)
            return reporter.Profile.surfaceMaterial;

        CollisionProfile envProfile = root.GetComponentInChildren<CollisionProfile>();
        if (envProfile != null)
            return MapCategoryToSurface(envProfile.category, root.name);

        if (root.GetComponentInChildren<CarController>() != null)
            return SurfaceMaterialType.Metal;

        return MapNameToSurface(root.name);
    }

    static SurfaceMaterialType MapCategoryToSurface(CollisionCategory category, string name)
    {
        string lower = name.ToLowerInvariant();
        if (ContainsAny(lower, "tree", "bush", "plant", "hedge"))
            return SurfaceMaterialType.Plant;
        if (ContainsAny(lower, "guard", "rail", "barrier"))
            return SurfaceMaterialType.Guardrail;
        if (ContainsAny(lower, "bridge", "building", "wall", "pillar"))
            return SurfaceMaterialType.Concrete;
        if (ContainsAny(lower, "road", "ground", "floor"))
            return SurfaceMaterialType.Ground;

        switch (category)
        {
            case CollisionCategory.DestructibleProp: return SurfaceMaterialType.Plant;
            case CollisionCategory.DynamicProp: return SurfaceMaterialType.Plant;
            default: return SurfaceMaterialType.Concrete;
        }
    }

    static SurfaceMaterialType MapNameToSurface(string name)
    {
        string lower = name.ToLowerInvariant();
        if (ContainsAny(lower, "tree", "bush", "plant"))
            return SurfaceMaterialType.Plant;
        if (ContainsAny(lower, "guard", "rail"))
            return SurfaceMaterialType.Guardrail;
        if (ContainsAny(lower, "road", "ground"))
            return SurfaceMaterialType.Ground;
        if (ContainsAny(lower, "building", "bridge", "wall"))
            return SurfaceMaterialType.Concrete;
        return SurfaceMaterialType.Unknown;
    }

    static bool ContainsAny(string text, params string[] keywords)
    {
        foreach (string keyword in keywords)
        {
            if (text.Contains(keyword))
                return true;
        }

        return false;
    }
}
