using UnityEngine;
using UnityEngine.XR;

public class TerrainFollowingScroller : MonoBehaviour
{
    public float speed = 1.5f; // How fast the camera moves forward
    public float heightOffset = 2f; // Distance above the terrain
    public LayerMask terrainLayer; // Assign your Terrain layer here
    public bool enableMouseLook = true;
    public float mouseSensitivity = 2f;
    public float maxLookUpAngle = 80f;
    public float maxLookDownAngle = 80f;

    private Quaternion initialRotation;
    private float mouseYaw;
    private float mousePitch;

    void Start()
    {
        initialRotation = transform.rotation;
    }

    void Update()
    {
        ApplyViewRotation();

        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.001f)
            transform.position += forward.normalized * (speed * Time.deltaTime);

        Ray downwardRay = new Ray(transform.position, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(downwardRay, out hit, 100f, terrainLayer))
        {
            transform.position = new Vector3(
                transform.position.x,
                hit.point.y + heightOffset, 
                transform.position.z
            );
        }
    }

    void ApplyViewRotation()
    {
        InputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if (head.isValid && head.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion headRotation))
        {
            transform.rotation = initialRotation * headRotation;
            return;
        }

        if (!enableMouseLook) return;

        mouseYaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        mousePitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        mousePitch = Mathf.Clamp(mousePitch, -maxLookDownAngle, maxLookUpAngle);
        transform.rotation = initialRotation * Quaternion.Euler(mousePitch, mouseYaw, 0f);
    }
}