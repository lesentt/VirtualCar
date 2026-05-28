using UnityEngine;

/// <summary>
/// 引擎起火：由 CarDamageSystem 在致命撞击引擎区时触发。
/// </summary>
public class VehicleFireSystem : MonoBehaviour
{
    [LabelText("火焰粒子")]
    public ParticleSystem fireEffect;

    public bool IsOnFire { get; private set; }

    public void TryIgnite()
    {
        if (IsOnFire) return;
        IsOnFire = true;
        if (fireEffect != null) fireEffect.Play();
    }
}
