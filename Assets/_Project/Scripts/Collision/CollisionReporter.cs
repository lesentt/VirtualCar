using UnityEngine;

[DisallowMultipleComponent]
public class CollisionReporter : MonoBehaviour
{
    [SerializeField] CollisionEntityProfile profile;
    [SerializeField] float minReportSpeed = 0.5f;

    CarController vehicle;
    bool isVehicleReporter;

    public CollisionEntityProfile Profile => profile;

    public void SetProfile(CollisionEntityProfile value) => profile = value;

    public GameObject Root => transform.root.gameObject;

    void Awake()
    {
        vehicle = GetComponentInParent<CarController>();
        isVehicleReporter = vehicle != null;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.relativeVelocity.magnitude < GetMinReportSpeed())
            return;

        CollisionManager.Instance?.Report(this, collision);
    }

    void OnCollisionStay(Collision collision)
    {
        if (!isVehicleReporter || CollisionAudioManager.Instance == null)
            return;

        if (collision.contactCount == 0)
            return;

        ContactPoint contact = collision.GetContact(0);
        Vector3 relativeVelocity = collision.relativeVelocity;
        float normalSpeed = Mathf.Abs(Vector3.Dot(relativeVelocity, contact.normal));
        Vector3 tangentVelocity = relativeVelocity - contact.normal * normalSpeed;
        float tangentSpeed = tangentVelocity.magnitude;
        float pressStrength = normalSpeed * 80f + collision.impulse.magnitude * 0.05f;

        CollisionAudioManager.Instance.ReportScrape(contact.point, tangentSpeed, pressStrength);
    }

    void LateUpdate()
    {
        if (isVehicleReporter)
            CollisionAudioManager.Instance?.EndScrapeFrame();
    }

    float GetMinReportSpeed()
    {
        return profile != null ? profile.minReportSpeed : minReportSpeed;
    }
}
