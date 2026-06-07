using UnityEngine;

[CreateAssetMenu(fileName = "CollisionVFXProfile", menuName = "VirtualVehicle/Collision VFX Profile")]
public class CollisionVFXProfile : ScriptableObject
{
    [Header("—— 粒子预制体 ——")]
    public GameObject smokePrefab;
    public GameObject firePrefab;
    public GameObject bigFirePrefab;

    [Header("—— 触发阈值 ——")]
    public float minImpulseForVFX = 300f;
    public float minImpulseForHeavyVFX = 15000f;
    public float bigFireDamagePercent = 60f;

    [Header("—— 持续时间 ——")]
    public float smokeLifetime = 4f;
    public float fireLifetime = 5f;

    [Header("—— 车身大火 ——")]
    public Vector3 bigFireLocalOffset = new Vector3(0f, 1.1f, 0.3f);
}
