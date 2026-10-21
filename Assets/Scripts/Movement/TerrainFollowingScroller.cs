using UnityEngine;

/// <summary>
/// Moves the player rig slowly forward in the direction the player is looking,
/// keeps the eyes at a fixed height above the ground and steers around obstacles
/// such as trees. Put it on the OVRCameraRig root ("Camera Rig").
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
    [Tooltip("The ground probe starts this far above the eyes (only used for non-terrain ground).")]
    public float probeHeight = 5f;
    [Tooltip("Stop moving forward when there is no terrain ahead (end of the map).")]
    public bool stopAtTerrainEdge = true;
    [Tooltip("How far ahead the terrain edge is checked.")]
    public float edgeCheckDistance = 2f;
    [Tooltip("How quickly the height follows the terrain. 0 = snap instantly.")]
    public float heightSmoothing = 8f;

    [Header("Obstacle Avoidance")]
    [Tooltip("Walk around trees and other obstacles instead of through them.")]
    public bool avoidObstacles = true;
    [Tooltip("Layers that block the way. Terrain trees are part of the Terrain layer; placed trees are on Default.")]
    public LayerMask obstacleLayers = (1 << 0) | (1 << 3);
    [Tooltip("Half the width of the player's body (metres).")]
    public float bodyRadius = 0.5f;
    [Tooltip("How far ahead obstacles are detected (metres).")]
    public float lookAheadDistance = 3f;
    [Tooltip("Height above the ground at which obstacles are checked (chest height).")]
    public float obstacleCheckHeight = 1.1f;
    [Tooltip("Largest detour angle from the view direction. If every direction within it is blocked the player stops.")]
    [Range(10f, 90f)] public float maxAvoidanceAngle = 75f;
    [Tooltip("How fast the walking direction turns (degrees per second).")]
    public float steeringSpeed = 90f;

    private const float AngleStep = 15f;

    private Vector3 moveDirection;
    private float preferredSide = 1f;
    private readonly RaycastHit[] obstacleHits = new RaycastHit[16];

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
        moveDirection = Vector3.zero;
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

        Vector3 desired = view.forward;
        desired.y = 0f;
        if (speed > 0f && desired.sqrMagnitude > 0.001f)
        {
            desired.Normalize();
            Vector3 target = avoidObstacles ? FindFreeDirection(view.position, desired) : desired;

            if (target == Vector3.zero)
            {
                // Completely blocked: stand still until the player looks elsewhere.
                moveDirection = Vector3.zero;
            }
            else
            {
                moveDirection = moveDirection == Vector3.zero
                    ? target
                    : Vector3.RotateTowards(moveDirection, target, steeringSpeed * Mathf.Deg2Rad * Time.deltaTime, 0f);

                Vector3 probe = view.position + moveDirection * edgeCheckDistance;
                if (!stopAtTerrainEdge || Ground.TryGetHeight(probe, terrainLayer, probeHeight, out _))
                    transform.position += moveDirection * (speed * Time.deltaTime);
            }
        }

        FollowTerrain(false);
    }

    /// <summary>
    /// Returns the walking direction closest to <paramref name="desired"/> that is not blocked,
    /// or Vector3.zero if every direction within the allowed angle is blocked.
    /// </summary>
    private Vector3 FindFreeDirection(Vector3 eye, Vector3 desired)
    {
        if (!IsBlocked(eye, desired))
            return desired;

        for (float angle = AngleStep; angle <= maxAvoidanceAngle + 0.01f; angle += AngleStep)
        {
            Vector3 first = Quaternion.Euler(0f, angle * preferredSide, 0f) * desired;
            if (!IsBlocked(eye, first))
                return first;

            Vector3 second = Quaternion.Euler(0f, -angle * preferredSide, 0f) * desired;
            if (!IsBlocked(eye, second))
            {
                // Keep going round the same side next time, so the path does not flip back and forth.
                preferredSide = -preferredSide;
                return second;
            }
        }

        return Vector3.zero;
    }

    private bool IsBlocked(Vector3 eye, Vector3 direction)
    {
        if (!Ground.TryGetHeight(eye, terrainLayer, probeHeight, out float groundY))
            groundY = eye.y - heightOffset;

        // Start a little behind the body so obstacles right next to the player are found too.
        Vector3 origin = new Vector3(eye.x, groundY + obstacleCheckHeight, eye.z) - direction * bodyRadius;
        int count = Physics.SphereCastNonAlloc(origin, bodyRadius, direction, obstacleHits,
            lookAheadDistance + bodyRadius, obstacleLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = obstacleHits[i];

            // Overlapping at the start (e.g. the player's own colliders) or part of the rig.
            if (hit.distance <= 0f || hit.collider.transform.IsChildOf(transform))
                continue;

            // Menus that follow the player and collectibles are not obstacles.
            if (hit.collider.GetComponentInParent<Canvas>() != null
                || hit.collider.GetComponentInParent<SceneMenu>() != null
                || hit.collider.GetComponentInParent<Interactable>() != null)
                continue;

            // Ground and slopes are walkable; trees and walls have steep normals.
            if (hit.normal.y > 0.6f)
                continue;

            return true;
        }

        return false;
    }

    private void FollowTerrain(bool instant)
    {
        Vector3 eye = viewTransform != null ? viewTransform.position : transform.position;
        if (!Ground.TryGetHeight(eye, terrainLayer, probeHeight, out float groundY))
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
}
