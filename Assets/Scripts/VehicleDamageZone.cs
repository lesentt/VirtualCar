using UnityEngine;

public enum DamageZoneType
{
    Front,
    Side,
    Rear,
    Engine
}

/// <summary>
/// 车辆损伤分区：挂在子 Collider 上，标记该区域可脱落部件。
/// </summary>
public class VehicleDamageZone : MonoBehaviour
{
    [LabelText("区域类型")]
    public DamageZoneType zoneType;

    [LabelText("该区域部件")]
    [Tooltip("此区域被重撞时可脱落的部件")]
    public GameObject[] parts;
}
