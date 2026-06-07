using UnityEngine;

[CreateAssetMenu(fileName = "CollisionEntityProfile", menuName = "VirtualVehicle/Collision Entity Profile")]
public class CollisionEntityProfile : ScriptableObject
{
    public SurfaceMaterialType surfaceMaterial = SurfaceMaterialType.Metal;
    public float minReportSpeed = 0.5f;
    public float restitution = 0.2f;
}
