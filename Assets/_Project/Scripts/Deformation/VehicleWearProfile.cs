using UnityEngine;

[CreateAssetMenu(fileName = "VehicleWearProfile", menuName = "VirtualVehicle/Vehicle Wear Profile")]
public class VehicleWearProfile : ScriptableObject
{
    [Header("Metal059C 做旧贴图")]
    public Texture2D wearMetalColor;
    public Texture2D wearMetalNormal;
    public Texture2D wearMetalRoughness;

    [Header("Shader")]
    public float metalTiling = 5f;
    [Range(0f, 1f)] public float grimeAmount = 0.45f;
    public float wearBlendPower = 2.2f;
}
