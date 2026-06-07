using UnityEngine;

public class SceneResetService : MonoBehaviour
{
    public void ResetScene()
    {
        foreach (VehicleState state in FindObjectsOfType<VehicleState>())
            state.ResetState();

        foreach (DestructibleProp prop in FindObjectsOfType<DestructibleProp>())
            prop.ResetProp();

        CollisionManager.Instance?.Recorder?.Clear();
    }
}
