using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 收集场景中可驾驶车辆，并生成切换列表与显示名称。
/// </summary>
public static class VehicleSwitchRegistry
{
    static readonly string[] PreferredOrder =
    {
        "Car 1",
        "Police 1",
        "Taxi"
    };

    public static CarController[] CollectOrderedControllers()
    {
        CarController[] found = Object.FindObjectsOfType<CarController>();
        if (found == null || found.Length == 0)
            return System.Array.Empty<CarController>();

        var ordered = new List<CarController>();
        var used = new HashSet<CarController>();

        foreach (string preferredName in PreferredOrder)
        {
            foreach (CarController car in found)
            {
                if (car == null || used.Contains(car))
                    continue;

                if (!car.gameObject.name.Contains(preferredName))
                    continue;

                ordered.Add(car);
                used.Add(car);
            }
        }

        System.Array.Sort(found, (a, b) => string.CompareOrdinal(a.gameObject.name, b.gameObject.name));
        foreach (CarController car in found)
        {
            if (car != null && !used.Contains(car))
                ordered.Add(car);
        }

        return ordered.ToArray();
    }

    public static GameUIManager.VehicleEntry[] BuildVehicleEntries(CarController[] controllers)
    {
        if (controllers == null || controllers.Length == 0)
            return System.Array.Empty<GameUIManager.VehicleEntry>();

        var entries = new GameUIManager.VehicleEntry[controllers.Length];
        for (int i = 0; i < controllers.Length; i++)
        {
            entries[i] = new GameUIManager.VehicleEntry
            {
                controller = controllers[i],
                displayName = GetDisplayName(controllers[i])
            };
        }

        return entries;
    }

    public static VehicleCameraController.CameraEntry[] BuildCameraEntries(CarController[] controllers)
    {
        if (controllers == null || controllers.Length == 0)
            return System.Array.Empty<VehicleCameraController.CameraEntry>();

        var entries = new VehicleCameraController.CameraEntry[controllers.Length];
        for (int i = 0; i < controllers.Length; i++)
        {
            CarController car = controllers[i];
            VehicleCameraRig rig = car != null ? VehicleCameraRig.EnsureOn(car.transform) : null;
            entries[i] = new VehicleCameraController.CameraEntry
            {
                vehicleRoot = car != null ? car.transform : null,
                cameraRig = rig,
                vehicleCamera = rig != null ? rig.VehicleCamera : null
            };
        }

        return entries;
    }

    public static string GetDisplayName(CarController controller)
    {
        if (controller == null)
            return "车辆";

        string name = controller.gameObject.name;
        if (name.Contains("Police"))
            return "police";
        if (name.Contains("Taxi"))
            return "taxi";
        if (name.Contains("Car"))
            return "car";

        return name;
    }
}
