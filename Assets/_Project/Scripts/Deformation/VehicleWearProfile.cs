using UnityEngine;

[CreateAssetMenu(fileName = "VehicleWearProfile", menuName = "VirtualVehicle/Vehicle Wear Profile")]
public class VehicleWearProfile : ScriptableObject
{
    [Header("Metal054B — 轻刮擦 / 露底漆")]
    public Texture2D metalLightColor;
    public Texture2D metalLightNormal;
    public Texture2D metalLightRoughness;

    [Header("Metal059C — 重损 / 脏污金属")]
    public Texture2D metalHeavyColor;
    public Texture2D metalHeavyNormal;
    public Texture2D metalHeavyRoughness;

    [Header("Shader")]
    public float metalTiling = 4f;
    [Range(0f, 1f)] public float grimeAmount = 0.38f;
}
