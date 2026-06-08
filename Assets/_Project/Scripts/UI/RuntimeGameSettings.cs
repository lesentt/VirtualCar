using System;
using UnityEngine;

/// <summary>
/// 运行时游戏配置快照，供 UI 编辑与 PlayerPrefs 持久化。
/// </summary>
[Serializable]
public class RuntimeGameSettings
{
    [Header("驾驶物理")]
    public float motorTorque = 1500f;
    public float maxSteerAngle = 18f;
    public float brakeTorque = 3000f;
    public float handbrakeTorque = 10000f;
    public float gripCoefficient = 1f;
    public float rollingResistanceCoeff = 0.02f;
    public float airDragCoefficient = 0.4f;
    public float mass = 1000f;
    public float centerOfMassY = -0.5f;

    [Header("损伤")]
    public float damageSensitivity = 0.16f;

    [Header("形变")]
    public float deformThreshold = 500f;
    public float maxDeformDepth = 0.35f;
    public float deformRadius = 0.9f;
    public float falloff = 1.4f;
    public float accumulateRatio = 0.92f;
    public float deformDepthMultiplier = 4.5f;
    public float damageImpulseScale = 0.38f;
    public float depthDamageWeight = 0.07f;
    public float totaledThreshold = 42000f;
    public int maxVerticesPerFrame = 500;

    [Header("碰撞")]
    public float minCollisionImpulse = 300f;
    public float minReportSpeed = 0.5f;

    [Header("反馈")]
    public bool cameraShakeEnabled = true;
    public float cameraShakeMinImpulse = 1500f;
    public float cameraShakeAmplitude = 0.4f;
    public bool audioEnabled = true;
    public float audioMasterVolume = 1f;
    public float audioLightThreshold = 500f;
    public float audioHeavyThreshold = 4000f;
    public bool vfxEnabled = true;
    public float vfxMinImpulse = 300f;

    [Header("环境")]
    public float propToppleThresholdScale = 1f;
    public float propToppleForceScale = 1f;

    [Header("场景")]
    public float timeScale = 1f;
    public bool showHud = true;

    public static RuntimeGameSettings CreateFromVehicle(CarController car, VehicleState state)
    {
        RuntimeGameSettings s = new RuntimeGameSettings();
        if (car == null) return s;

        s.motorTorque = car.GetMotorTorque();
        s.maxSteerAngle = car.GetMaxSteerAngle();
        s.brakeTorque = car.GetBrakeTorque();
        s.handbrakeTorque = car.GetHandbrakeTorque();
        s.gripCoefficient = car.GetGripCoefficient();
        s.rollingResistanceCoeff = car.GetRollingResistanceCoeff();
        s.airDragCoefficient = car.GetAirDragCoefficient();
        s.mass = car.GetMass();
        s.centerOfMassY = car.GetCenterOfMassY();

        if (state != null)
            s.damageSensitivity = state.GetDamageSensitivity();

        return s;
    }

    public void CaptureDeformation(DeformationConfig cfg)
    {
        if (cfg == null) return;

        deformThreshold = cfg.deformThreshold;
        maxDeformDepth = cfg.maxDeformDepth;
        deformRadius = cfg.deformRadius;
        falloff = cfg.falloff;
        accumulateRatio = cfg.accumulateRatio;
        deformDepthMultiplier = cfg.deformDepthMultiplier;
        damageImpulseScale = cfg.damageImpulseScale;
        depthDamageWeight = cfg.depthDamageWeight;
        totaledThreshold = cfg.totaledThreshold;
        maxVerticesPerFrame = cfg.maxVerticesPerFrame;
    }

    public void ApplyDeformation(DeformationConfig cfg)
    {
        if (cfg == null) return;

        cfg.deformThreshold = deformThreshold;
        cfg.maxDeformDepth = maxDeformDepth;
        cfg.deformRadius = deformRadius;
        cfg.falloff = falloff;
        cfg.accumulateRatio = accumulateRatio;
        cfg.deformDepthMultiplier = deformDepthMultiplier;
        cfg.damageImpulseScale = damageImpulseScale;
        cfg.depthDamageWeight = depthDamageWeight;
        cfg.totaledThreshold = totaledThreshold;
        cfg.maxVerticesPerFrame = maxVerticesPerFrame;
    }

    public void CaptureFeedback(
        CameraShakeController shake,
        CollisionAudioManager audio,
        CollisionVFXManager vfx)
    {
        if (shake != null)
        {
            cameraShakeEnabled = shake.IsEnabled();
            cameraShakeMinImpulse = shake.GetMinImpulse();
            cameraShakeAmplitude = shake.GetMaxShakeAmplitude();
        }

        if (audio != null)
        {
            audioEnabled = audio.IsEnabled();
            audioMasterVolume = audio.GetMasterVolume();
            if (audio.Profile != null)
            {
                audioLightThreshold = audio.Profile.lightImpulseThreshold;
                audioHeavyThreshold = audio.Profile.heavyImpulseThreshold;
            }
        }

        if (vfx != null)
        {
            vfxEnabled = vfx.IsEnabled();
            if (vfx.Profile != null)
                vfxMinImpulse = vfx.Profile.minImpulseForVFX;
        }
    }

    public void CaptureCollision(CollisionManager manager, CollisionEntityProfile entityProfile)
    {
        if (manager != null)
            minCollisionImpulse = manager.MinImpulse;

        if (entityProfile != null)
            minReportSpeed = entityProfile.minReportSpeed;
    }

    public void ApplyPresetEconomy()
    {
        motorTorque = 1200f;
        maxSteerAngle = 16f;
        brakeTorque = 2500f;
        handbrakeTorque = 6000f;
        gripCoefficient = 1.2f;
        rollingResistanceCoeff = 0.035f;
        airDragCoefficient = 0.35f;
        mass = 1100f;
        centerOfMassY = -0.45f;
        damageSensitivity = 0.16f;
    }

    public void ApplyPresetSport()
    {
        motorTorque = 3500f;
        maxSteerAngle = 22f;
        brakeTorque = 5000f;
        handbrakeTorque = 12000f;
        gripCoefficient = 2.2f;
        rollingResistanceCoeff = 0.015f;
        airDragCoefficient = 0.55f;
        mass = 950f;
        centerOfMassY = -0.55f;
        damageSensitivity = 0.24f;
    }

    public void ApplyPresetPolice()
    {
        motorTorque = 2200f;
        maxSteerAngle = 30f;
        brakeTorque = 10000f;
        handbrakeTorque = 30000f;
        gripCoefficient = 1f;
        rollingResistanceCoeff = 0.02f;
        airDragCoefficient = 0.4f;
        mass = 1400f;
        centerOfMassY = -0.5f;
        damageSensitivity = 0.42f;
    }
}
