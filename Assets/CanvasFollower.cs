using UnityEngine;

public class CanvasFollower : MonoBehaviour
{
    [Header("Target")]
    public Transform headset; // usually the Main Camera

    [Header("Settings")]
    public float distanceFromHeadset = 1.5f;
    public float followSpeed = 5f;
    public bool scaleWithDistance = true;
    public float baseScale = 0.001f;

    private void Start()
    {
        if (headset == null)
        {
            headset = Camera.main?.transform;
            if (headset == null)
                Debug.LogError("Headset (Main Camera) not assigned and not found in scene.");
        }
    }

    void LateUpdate()
    {
        if (headset == null) return;

        // Desired position in front of headset
        Vector3 targetPosition = headset.position + headset.forward * distanceFromHeadset;

        // Smooth follow
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);

        // Face the headset
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(transform.position - headset.position), Time.deltaTime * followSpeed);

        // Optional: Scale canvas to stay readable at any distance
        if (scaleWithDistance)
        {
            float distance = Vector3.Distance(headset.position, transform.position);
            transform.localScale = Vector3.one * baseScale * distance;
        }
    }
}
