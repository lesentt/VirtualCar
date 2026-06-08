using UnityEngine;

[CreateAssetMenu(fileName = "CollisionVFXProfile", menuName = "VirtualVehicle/Collision VFX Profile")]
public class CollisionVFXProfile : ScriptableObject
{
    [Header("—— 粒子预制体 ——")]
    public GameObject smokePrefab;
    public GameObject firePrefab;
    public GameObject bigFirePrefab;

    [Header("—— 撞击特效分级（冲量）——")]
    [Tooltip("低于此冲量不播放任何撞击特效")]
    public float minImpulseForVFX = 300f;
    [Tooltip("≥ 此值且 < 大火阈值：播放 fire")]
    public float minImpulseForFire = 4000f;
    [Tooltip("≥ 此值：撞击点播放 big fire")]
    public float minImpulseForImpactBigFire = 10000f;

    [Header("—— 车身大火（损伤）——")]
    [Tooltip("破损值大于此百分比时在车身挂载 big fire")]
    public float bigFireDamagePercent = 50f;

    [Header("—— 持续时间 ——")]
    public float smokeLifetime = 4f;
    public float fireLifetime = 5f;
    public float impactBigFireLifetime = 6f;

    [Header("—— 车身大火 ——")]
    public Vector3 bigFireLocalOffset = new Vector3(0f, 1.1f, 0.3f);
}
