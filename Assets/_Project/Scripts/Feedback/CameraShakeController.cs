using System.Collections;
using UnityEngine;

public class CameraShakeController : MonoBehaviour
{
    [SerializeField] float minImpulse = 1500f;
    [SerializeField] float maxShakeAmplitude = 0.4f;
    [SerializeField] VehicleCameraController cameraController;

    Coroutine shakeRoutine;

    void Awake()
    {
        if (cameraController == null)
            cameraController = FindObjectOfType<VehicleCameraController>();
    }

    void Start()
    {
        if (CollisionManager.Instance?.EventChannel != null)
            CollisionManager.Instance.EventChannel.OnCollision.AddListener(OnCollision);
    }

    void OnDestroy()
    {
        if (CollisionManager.Instance?.EventChannel != null)
            CollisionManager.Instance.EventChannel.OnCollision.RemoveListener(OnCollision);
    }

    public void OnCollision(CollisionEventData evt)
    {
        if (evt.Impulse < minImpulse)
            return;

        CarController activeCar = FindActivePlayerCar();
        if (activeCar == null || evt.ObjectA != activeCar.gameObject)
            return;

        float strength = Mathf.InverseLerp(minImpulse, 8000f, evt.Impulse);
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(Shake(strength * maxShakeAmplitude, 0.2f));
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

    IEnumerator Shake(float amplitude, float duration)
    {
        Transform camTransform = cameraController != null
            ? cameraController.transform
            : Camera.main?.transform;

        if (camTransform == null)
            yield break;

        Vector3 originalLocalPos = camTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float decay = 1f - elapsed / duration;
            camTransform.localPosition = originalLocalPos + Random.insideUnitSphere * amplitude * decay;
            yield return null;
        }

        camTransform.localPosition = originalLocalPos;
        shakeRoutine = null;
    }
}
