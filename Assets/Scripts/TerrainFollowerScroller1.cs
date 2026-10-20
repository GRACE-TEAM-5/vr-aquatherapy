using UnityEngine;

public class TerrainFollowingScroller : MonoBehaviour
{
    public float speed = 1.5f; // How fast the camera moves forward
    public float heightOffset = 2f; // Distance above the terrain
    public LayerMask terrainLayer; // Assign your Terrain layer here
    [Tooltip("Assign the Meta XR CenterEyeAnchor or its Main Camera.")]
    public Transform viewTransform;

    void Start()
    {
        if (viewTransform == null && Camera.main != null)
            viewTransform = Camera.main.transform;
    }

    void Update()
    {
        Vector3 forward = viewTransform != null ? viewTransform.forward : transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.001f)
            transform.position += forward.normalized * (speed * Time.deltaTime);

        Ray downwardRay = new Ray(transform.position, Vector3.down);
        RaycastHit hit;

        if (!Physics.Raycast(downwardRay, out hit, 100f, terrainLayer)) return;
        float viewHeight = viewTransform
            ? viewTransform.position.y - transform.position.y
            : 0f;

        transform.position = new Vector3(
            transform.position.x,
            hit.point.y + heightOffset - viewHeight,
            transform.position.z
        );
    }
}