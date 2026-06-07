using System.Collections.Generic;
using UnityEngine;

public class CollisionEventRecorder : MonoBehaviour
{
    readonly List<CollisionEventData> history = new List<CollisionEventData>();
    CollisionEventData lastEvent;

    public CollisionEventData LastEvent => lastEvent;
    public IReadOnlyList<CollisionEventData> History => history;

    public void Record(CollisionEventData data)
    {
        lastEvent = data;
        history.Add(data);
        if (history.Count > 200)
            history.RemoveAt(0);
    }

    public void Clear()
    {
        history.Clear();
        lastEvent = default;
    }
}
