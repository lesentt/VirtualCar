using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "CollisionEventChannel", menuName = "VirtualVehicle/Collision Event Channel")]
public class CollisionEventChannel : ScriptableObject
{
    public UnityEvent<CollisionEventData> OnCollision = new UnityEvent<CollisionEventData>();

    public void Raise(CollisionEventData data) => OnCollision.Invoke(data);
}
