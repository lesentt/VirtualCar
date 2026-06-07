using UnityEngine;

[DisallowMultipleComponent]
public class CollisionReporter : MonoBehaviour
{
    [SerializeField] CollisionEntityProfile profile;
    [SerializeField] float minReportSpeed = 0.5f;

    public CollisionEntityProfile Profile => profile;

    public void SetProfile(CollisionEntityProfile value) => profile = value;

    public GameObject Root => transform.root.gameObject;

    void OnCollisionEnter(Collision collision)
    {
        if (collision.relativeVelocity.magnitude < GetMinReportSpeed())
            return;

        CollisionManager.Instance?.Report(this, collision);
    }

    float GetMinReportSpeed()
    {
        return profile != null ? profile.minReportSpeed : minReportSpeed;
    }
}
