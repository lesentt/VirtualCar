using UnityEngine;

public class SceneResetService : MonoBehaviour
{
    const float UnstuckLift = 0.65f;
    const float UnstuckBackward = 0.5f;

    public void ResetScene()
    {
        foreach (VehicleState state in FindObjectsOfType<VehicleState>())
            state.ResetState();

        foreach (DestructibleProp prop in FindObjectsOfType<DestructibleProp>())
            prop.ResetProp();

        CollisionManager.Instance?.Recorder?.Clear();
    }

    public void ResetActiveVehicle(CarController car)
    {
        if (car == null)
            return;

        VehicleState state = car.GetComponent<VehicleState>();
        state?.ResetState();
    }

    public void UnstuckVehicle(CarController car)
    {
        if (car == null)
            return;

        Rigidbody rb = car.GetComponent<Rigidbody>();
        if (rb == null)
            return;

        Vector3 nudge = car.transform.up * UnstuckLift - car.transform.forward * UnstuckBackward;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.MovePosition(rb.position + nudge);
        Physics.SyncTransforms();
    }
}
