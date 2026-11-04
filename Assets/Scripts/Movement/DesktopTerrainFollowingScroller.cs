using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Desktop/XR fallback scroller for a plain camera (no OVRCameraRig):
/// rotates with the headset if one is active, otherwise with the mouse,
/// moves forward and follows the terrain height.
/// For the Meta rig use <see cref="TerrainFollowingScroller"/> instead.
/// </summary>
public class DesktopTerrainFollowingScroller : MonoBehaviour
{
    public float speed = 1.5f;
    [Tooltip("Height above the terrain in metres.")]
    public float heightOffset = 2f;
    [Tooltip("Layer(s) of the ground. Use the 'Terrain' layer.")]
    public LayerMask terrainLayer;
    public bool enableMouseLook = true;
    public float mouseSensitivity = 2f;
    public float maxLookUpAngle = 80f;
    public float maxLookDownAngle = 80f;

    private Quaternion initialRotation;
    private float mouseYaw;
    private float mousePitch;

    private void Start()
    {
        initialRotation = transform.rotation;
    }

    private void Update()
    {
        ApplyViewRotation();

        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.001f)
            transform.position += forward.normalized * (speed * Time.deltaTime);

        Vector3 origin = transform.position + Vector3.up * 5f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 105f, terrainLayer, QueryTriggerInteraction.Ignore))
        {
            transform.position = new Vector3(transform.position.x, hit.point.y + heightOffset, transform.position.z);
        }
    }

    private void ApplyViewRotation()
    {
        InputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if (head.isValid && head.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion headRotation))
        {
            transform.rotation = initialRotation * headRotation;
            return;
        }

        if (!enableMouseLook)
            return;

        Vector2 delta = DesktopInput.MouseDelta;
        mouseYaw += delta.x * mouseSensitivity;
        mousePitch -= delta.y * mouseSensitivity;
        mousePitch = Mathf.Clamp(mousePitch, -maxLookDownAngle, maxLookUpAngle);
        transform.rotation = initialRotation * Quaternion.Euler(mousePitch, mouseYaw, 0f);
    }
}
