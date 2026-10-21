using UnityEngine;

/// <summary>
/// Finds the <see cref="Interactable"/> the player is pointing at and collects it.
///
/// Targeting (first hit wins):
///   1. Touch controller rays (left/right controller anchors of the OVRCameraRig)
///   2. Mouse cursor (Editor / desktop without a headset)
///   3. Head gaze (centre of view)
///
/// Collecting:
///   - controller index trigger or A/X button,
///   - hand-tracking index pinch (OVRInput maps it to A/X),
///   - left mouse click,
///   - looking at the object for <see cref="dwellTime"/> seconds (gaze dwell).
/// </summary>
public class InteractionManager : MonoBehaviour
{
    [Header("Raycast")]
    [Tooltip("Layers that can be targeted. Use the 'Interactable' layer.")]
    public LayerMask interactionLayer;
    public float raycastDistance = 100f;
    [Tooltip("Maximum distance between the pointer origin and the target.")]
    public float interactionRadius = 5f;

    [Header("Controllers & Hands")]
    public bool useControllerRays = true;
    [Tooltip("Optional. Defaults to the OVRCameraRig left controller anchor.")]
    public Transform leftPointer;
    [Tooltip("Optional. Defaults to the OVRCameraRig right controller anchor.")]
    public Transform rightPointer;

    [Header("Gaze")]
    [Tooltip("Collect an object by looking at it for 'Dwell Time' seconds.")]
    public bool collectByGazeDwell = true;
    [Min(0.1f)] public float dwellTime = 1.5f;

    [Header("Editor / Desktop")]
    [Tooltip("Aim with the mouse cursor when no headset is active.")]
    public bool allowMouse = true;

    private Camera mainCamera;
    private Interactable currentTarget;
    private Interactable dwellTarget;
    private float dwellTimer;

    private const OVRInput.RawButton RightCollectButtons = OVRInput.RawButton.RIndexTrigger | OVRInput.RawButton.A;
    private const OVRInput.RawButton LeftCollectButtons = OVRInput.RawButton.LIndexTrigger | OVRInput.RawButton.X;

    private void Start()
    {
        ResolveReferences();
        if (mainCamera == null)
            Debug.LogError("InteractionManager: no camera tagged 'MainCamera' found.", this);
        if (interactionLayer.value == 0)
            Debug.LogWarning("InteractionManager: Interaction Layer is empty, nothing can be targeted.", this);
    }

    private void OnDisable()
    {
        SetCurrentTarget(null);
    }

    private void ResolveReferences()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (!useControllerRays || (leftPointer != null && rightPointer != null))
            return;

        OVRCameraRig rig = FindAnyObjectByType<OVRCameraRig>();
        if (rig == null)
            return;

        if (leftPointer == null)
            leftPointer = rig.leftControllerAnchor != null ? rig.leftControllerAnchor : rig.leftHandAnchor;
        if (rightPointer == null)
            rightPointer = rig.rightControllerAnchor != null ? rig.rightControllerAnchor : rig.rightHandAnchor;
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            ResolveReferences();
            if (mainCamera == null)
                return;
        }

        bool rightPressed = OVRInput.GetDown(RightCollectButtons, OVRInput.Controller.All);
        bool leftPressed = OVRInput.GetDown(LeftCollectButtons, OVRInput.Controller.All);
        bool mouseClicked = DesktopInput.LeftClickPressed();
        bool anyPressed = rightPressed || leftPressed || mouseClicked;

        Interactable target = null;
        bool collect = false;
        bool isGazeTarget = false;

        // 1. Controller rays.
        if (useControllerRays)
        {
            Interactable right = OVRInput.IsControllerConnected(OVRInput.Controller.RTouch)
                ? RaycastFrom(rightPointer) : null;
            Interactable left = OVRInput.IsControllerConnected(OVRInput.Controller.LTouch)
                ? RaycastFrom(leftPointer) : null;

            if (left != null && (leftPressed || right == null))
            {
                target = left;
                collect = leftPressed;
            }
            else if (right != null)
            {
                target = right;
                collect = rightPressed;
            }
        }

        // 2. Mouse cursor (only without an active headset).
        if (target == null && allowMouse && !XRActive() && DesktopInput.MousePresent)
        {
            target = Raycast(mainCamera.ScreenPointToRay(DesktopInput.MousePosition));
            collect = mouseClicked;
        }

        // 3. Head gaze. Any collect input counts while looking at an object.
        if (target == null)
        {
            Transform head = mainCamera.transform;
            target = Raycast(new Ray(head.position, head.forward));
            collect = anyPressed;
            isGazeTarget = target != null;
        }

        SetCurrentTarget(target);
        UpdateDwell(isGazeTarget ? target : null, ref collect);

        if (collect && currentTarget != null)
            Collect(currentTarget);
    }

    private void UpdateDwell(Interactable gazeTarget, ref bool collect)
    {
        if (!collectByGazeDwell || gazeTarget == null)
        {
            dwellTarget = null;
            dwellTimer = 0f;
            return;
        }

        if (gazeTarget != dwellTarget)
        {
            dwellTarget = gazeTarget;
            dwellTimer = 0f;
        }

        dwellTimer += Time.deltaTime;
        if (dwellTimer >= dwellTime)
        {
            collect = true;
            dwellTimer = 0f;
        }
    }

    private Interactable RaycastFrom(Transform pointer)
    {
        if (pointer == null || !pointer.gameObject.activeInHierarchy)
            return null;

        return Raycast(new Ray(pointer.position, pointer.forward));
    }

    private Interactable Raycast(Ray ray)
    {
        if (!Physics.Raycast(ray, out RaycastHit hit, raycastDistance, interactionLayer, QueryTriggerInteraction.Collide))
            return null;

        if (Vector3.Distance(ray.origin, hit.point) > interactionRadius)
            return null;

        Interactable interactable = hit.collider.GetComponentInParent<Interactable>();
        if (interactable == null || interactable.IsCollected)
            return null;

        return interactable;
    }

    private void SetCurrentTarget(Interactable target)
    {
        if (currentTarget == target)
            return;

        if (currentTarget != null)
            currentTarget.OnHoverExit();

        currentTarget = target;

        if (currentTarget != null)
            currentTarget.OnHoverEnter();
    }

    private void Collect(Interactable target)
    {
        // Clear the hover first, the object destroys itself when collected.
        if (currentTarget == target)
            currentTarget = null;

        target.OnInteract();
        dwellTarget = null;
        dwellTimer = 0f;
    }

    private static bool XRActive()
    {
        return UnityEngine.XR.XRSettings.isDeviceActive;
    }
}
