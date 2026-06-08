using System.Collections;
using UnityEngine;

public class CameraShakeController : MonoBehaviour
{
    [SerializeField] float minImpulse = 1500f;
    [SerializeField] float maxShakeAmplitude = 0.4f;
    [SerializeField] float firstPersonRotShakeScale = 10f;
    [SerializeField] VehicleCameraController cameraController;

    bool shakeEnabled = true;
    Coroutine shakeRoutine;
    Transform activeShakeTransform;

    public static CameraShakeController Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        if (cameraController == null)
            cameraController = FindObjectOfType<VehicleCameraController>();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (CollisionManager.Instance?.EventChannel != null)
            CollisionManager.Instance.EventChannel.OnCollision.RemoveListener(OnCollision);

        StopAndReset();
    }

    void Start()
    {
        if (CollisionManager.Instance?.EventChannel != null)
            CollisionManager.Instance.EventChannel.OnCollision.AddListener(OnCollision);
    }

    public void OnCollision(CollisionEventData evt)
    {
        if (!shakeEnabled || evt.Impulse < minImpulse)
            return;

        CarController activeCar = FindActivePlayerCar();
        if (activeCar == null || evt.ObjectA != activeCar.gameObject)
            return;

        float strength = Mathf.InverseLerp(minImpulse, 8000f, evt.Impulse);
        BeginShake(strength * maxShakeAmplitude, 0.2f);
    }

    public void StopAndReset()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        ResetShakenTransform();
        activeShakeTransform = null;
    }

    void BeginShake(float amplitude, float duration)
    {
        StopAndReset();
        shakeRoutine = StartCoroutine(ShakeRoutine(amplitude, duration));
    }

    IEnumerator ShakeRoutine(float amplitude, float duration)
    {
        activeShakeTransform = cameraController != null
            ? cameraController.GetActiveCameraTransform()
            : Camera.main?.transform;

        if (activeShakeTransform == null)
        {
            shakeRoutine = null;
            yield break;
        }

        bool firstPerson = cameraController != null
            && cameraController.CurrentViewMode == VehicleCameraRig.ViewMode.FirstPerson;

        ResetShakenTransform();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float decay = 1f - elapsed / duration;

            if (firstPerson)
            {
                activeShakeTransform.localPosition = Vector3.zero;
                float rot = amplitude * decay * firstPersonRotShakeScale;
                activeShakeTransform.localRotation = Quaternion.Euler(
                    Random.Range(-rot, rot),
                    Random.Range(-rot, rot),
                    Random.Range(-rot, rot) * 0.35f);
            }
            else
            {
                activeShakeTransform.localRotation = Quaternion.identity;
                activeShakeTransform.localPosition = Random.insideUnitSphere * amplitude * decay;
            }

            yield return null;
        }

        ResetShakenTransform();
        activeShakeTransform = null;
        shakeRoutine = null;
    }

    void ResetShakenTransform()
    {
        if (activeShakeTransform == null)
            return;

        activeShakeTransform.localPosition = Vector3.zero;
        activeShakeTransform.localRotation = Quaternion.identity;
    }

    CarController FindActivePlayerCar()
    {
        foreach (CarController car in FindObjectsOfType<CarController>())
        {
            if (car.isPlayerControlled)
                return car;
        }

        return null;
    }

    public bool IsEnabled() => shakeEnabled;
    public void SetEnabled(bool enabled) => shakeEnabled = enabled;
    public float GetMinImpulse() => minImpulse;
    public void SetMinImpulse(float impulse) => minImpulse = Mathf.Max(0f, impulse);
    public float GetMaxShakeAmplitude() => maxShakeAmplitude;
    public void SetMaxShakeAmplitude(float amplitude) => maxShakeAmplitude = Mathf.Clamp(amplitude, 0f, 1.5f);
}
