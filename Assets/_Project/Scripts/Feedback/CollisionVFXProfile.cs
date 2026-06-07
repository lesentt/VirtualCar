using UnityEngine;

[CreateAssetMenu(fileName = "CollisionVFXProfile", menuName = "VirtualVehicle/Collision VFX Profile")]
public class CollisionVFXProfile : ScriptableObject
{
    public GameObject sparkMetalPrefab;
    public GameObject dustConcretePrefab;
    public GameObject debrisMetalPrefab;
    public GameObject leafPlantPrefab;

    public float minImpulseForVFX = 300f;
    public float minImpulseForHeavyVFX = 2500f;
    public int poolSizePerType = 6;
}
