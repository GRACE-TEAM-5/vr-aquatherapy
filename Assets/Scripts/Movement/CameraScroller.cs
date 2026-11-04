using UnityEngine;

/// <summary>
/// Simple constant forward movement (legacy test helper).
/// </summary>
public class CameraScroller : MonoBehaviour
{
    [Tooltip("Speed at which the object moves forward (m/s).")]
    public float scrollSpeed = 1.5f;

    private void Update()
    {
        transform.Translate(Vector3.forward * (scrollSpeed * Time.deltaTime));
    }
}
