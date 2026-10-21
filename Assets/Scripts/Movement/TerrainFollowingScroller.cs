using UnityEngine;

/// <summary>
/// Moves the player rig slowly forward in the direction the player is looking
/// and keeps the eyes at a fixed height above the terrain.
/// Put it on the OVRCameraRig root ("Camera Rig").
/// </summary>
public class TerrainFollowingScroller : MonoBehaviour
{
    [Tooltip("Forward speed in metres per second.")]
    public float speed = 1.5f;
    [Tooltip("Eye height above the terrain in metres.")]
    public float heightOffset = 2f;
    [Tooltip("Layer(s) of the ground. Use the 'Terrain' layer.")]
    public LayerMask terrainLayer;
    [Tooltip("Assign the Meta XR CenterEyeAnchor or its Main Camera. Defaults to Camera.main.")]
    public Transform viewTransform;

    [Header("Ground Probe")]
    [Tooltip("The ground probe starts this far above the eyes.")]
    public float probeHeight = 5f;
    [Tooltip("Stop moving forward when there is no terrain ahead (end of the map).")]
    public bool stopAtTerrainEdge = true;
    [Tooltip("How far ahead the terrain edge is checked.")]
    public float edgeCheckDistance = 2f;
    [Tooltip("How quickly the height follows the terrain. 0 = snap instantly.")]
    public float heightSmoothing = 8f;

    private void OnEnable()
    {
        PlayAreaWrapper.PlayerRelocated += HandleRelocated;
    }

    private void OnDisable()
    {
        PlayAreaWrapper.PlayerRelocated -= HandleRelocated;
    }

    private void HandleRelocated(Vector3 offset)
    {
        // Snap to the new ground height while the screen is faded out.
        FollowTerrain(true);
    }

    private void Start()
    {
        if (viewTransform == null && Camera.main != null)
            viewTransform = Camera.main.transform;

        if (terrainLayer.value == 0)
            Debug.LogWarning("TerrainFollowingScroller: Terrain Layer is empty.", this);

        FollowTerrain(true);
    }

    private void Update()
    {
        Transform view = viewTransform != null ? viewTransform : transform;

        Vector3 forward = view.forward;
        forward.y = 0f;
        if (speed > 0f && forward.sqrMagnitude > 0.001f)
        {
            forward.Normalize();
            Vector3 probe = view.position + forward * edgeCheckDistance;
            if (!stopAtTerrainEdge || TryGetGroundHeight(probe, out _))
                transform.position += forward * (speed * Time.deltaTime);
        }

        FollowTerrain(false);
    }

    private void FollowTerrain(bool instant)
    {
        Vector3 eye = viewTransform != null ? viewTransform.position : transform.position;
        if (!TryGetGroundHeight(eye, out float groundY))
            return;

        // Height of the eyes above the rig origin (tracked head height).
        float eyeHeightInRig = viewTransform != null ? viewTransform.position.y - transform.position.y : 0f;
        float targetY = groundY + heightOffset - eyeHeightInRig;

        Vector3 p = transform.position;
        float y = instant || heightSmoothing <= 0f
            ? targetY
            : Mathf.Lerp(p.y, targetY, 1f - Mathf.Exp(-heightSmoothing * Time.deltaTime));
        transform.position = new Vector3(p.x, y, p.z);
    }

    private bool TryGetGroundHeight(Vector3 position, out float height)
    {
        Vector3 origin = position + Vector3.up * probeHeight;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, probeHeight + 100f, terrainLayer, QueryTriggerInteraction.Ignore))
        {
            height = hit.point.y;
            return true;
        }

        height = 0f;
        return false;
    }
}
