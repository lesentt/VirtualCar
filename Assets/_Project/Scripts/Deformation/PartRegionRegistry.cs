using System.Collections.Generic;
using UnityEngine;

public static class PartRegionRegistry
{
    public static DeformablePart FindClosestPart(GameObject vehicleRoot, Vector3 worldPoint)
    {
        if (vehicleRoot == null)
            return null;

        DeformablePart[] parts = vehicleRoot.GetComponentsInChildren<DeformablePart>();
        DeformablePart closest = null;
        float bestDist = float.MaxValue;

        foreach (DeformablePart part in parts)
        {
            if (part.PartType == VehiclePartType.Wheel || part.PartType == VehiclePartType.OtherNonDeformable)
                continue;

            float dist = Vector3.Distance(part.transform.position, worldPoint);
            if (dist < bestDist)
            {
                bestDist = dist;
                closest = part;
            }
        }

        return closest;
    }

    public static VehiclePartType GuessPartFromName(string objectName)
    {
        string lower = objectName.ToLowerInvariant();
        if (lower.Contains("hood")) return VehiclePartType.Hood;
        if (lower.Contains("trunk")) return VehiclePartType.Trunk;
        if (lower.Contains("bumper") && lower.Contains("rear")) return VehiclePartType.RearBumper;
        if (lower.Contains("bumper") || lower.Contains("base_car") || lower.Contains("police_base")) return VehiclePartType.FrontBumper;
        if (lower.Contains("pdoor_fl")) return VehiclePartType.DoorFL;
        if (lower.Contains("pdoor_fr")) return VehiclePartType.DoorFR;
        if (lower.Contains("pdoor_bl")) return VehiclePartType.DoorRL;
        if (lower.Contains("pdoor_br")) return VehiclePartType.DoorRR;
        if (lower.Contains("cdoor_fl") || lower.Contains("door_fl")) return VehiclePartType.DoorFL;
        if (lower.Contains("cdoor_fr") || lower.Contains("door_fr")) return VehiclePartType.DoorFR;
        if (lower.Contains("cdoor_bl") || lower.Contains("door_rl")) return VehiclePartType.DoorRL;
        if (lower.Contains("cdoor_br") || lower.Contains("door_rr")) return VehiclePartType.DoorRR;
        if (lower.Contains("roof")) return VehiclePartType.Roof;
        if (lower.Contains("wheel")) return VehiclePartType.Wheel;
        return VehiclePartType.OtherNonDeformable;
    }
}
