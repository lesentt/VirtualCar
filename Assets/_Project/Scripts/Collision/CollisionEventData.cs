using UnityEngine;

public enum CollisionType { VehicleVehicle, VehicleEnvironment }

public enum VehiclePartType
{
    FrontBumper, Hood, RearBumper, Trunk,
    DoorFL, DoorFR, DoorRL, DoorRR,
    Roof, Wheel, OtherNonDeformable
}

public enum DamageLevel { Intact = 0, Light = 1, Heavy = 2, Totaled = 3 }

public enum SurfaceMaterialType
{
    Metal, Concrete, Plant, Guardrail, Ground, Unknown
}

[System.Serializable]
public struct CollisionEventData
{
    public float Timestamp;
    public GameObject ObjectA;
    public GameObject ObjectB;
    public CollisionType Type;
    public VehiclePartType HitPart;
    public DeformablePart HitDeformable;
    public Vector3 ContactPoint;
    public Vector3 ContactNormal;
    public float RelativeVelocity;
    public float Impulse;
    public SurfaceMaterialType SurfaceA;
    public SurfaceMaterialType SurfaceB;
}
