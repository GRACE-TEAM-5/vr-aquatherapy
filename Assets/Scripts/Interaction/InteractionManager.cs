using UnityEngine;

/// <summary>
/// Finds the <see cref="Interactable"/> the player is pointing at and collects it.
///
/// Targeting (first hit wins):
///   1. Touch controller rays (controller anchors of the OVRCameraRig)
///   2. Hand-tracking rays (Meta system pointer pose: shoulder -> hand, like the Quest menu)
///   3. Mouse cursor (Editor / desktop without a headset)
///   4. Head gaze (centre of view)
///
/// Collecting:
///   - controller index trigger or A/X button,
///   - hand tracking: pinch while pointing at an object, keep pointing at it for
///     'Hand Dwell Time' seconds, or touch it with the hand,
///   - left mouse click,
///   - looking at the object for 'Dwell Time' seconds (gaze dwell).
/// </summary>
public class InteractionManager : MonoBehaviour
{
    [Header("Raycast")]
    [Tooltip("Layers that can be targeted. Use the 'Interactable' layer.")]
    public LayerMask interactionLayer;
    public float raycastDistance = 100f;
    [Tooltip("Maximum distance between the pointer origin and the target.")]
    public float interactionRadius = 5f;

    [Header("Controllers")]
    public bool useControllerRays = true;
    [Tooltip("Optional. Defaults to the OVRCameraRig left controller anchor.")]
    public Transform leftPointer;
    [Tooltip("Optional. Defaults to the OVRCameraRig right controller anchor.")]
    public Transform rightPointer;

    [Header("Hand Tracking")]
    [Tooltip("Point at objects with the tracked hands (outstretched arm).")]
    public bool useHandRays = true;
    [Tooltip("Collect by keeping the hand pointed at an object for 'Hand Dwell Time' seconds.")]
    public bool collectByHandDwell = true;
    [Min(0.1f)] public float handDwellTime = 1f;
    [Tooltip("Collect by pinching (thumb + index) while pointing at an object.")]
    public bool collectByPinch = true;
    [Tooltip("Collect by touching an object with a hand or controller.")]
    public bool collectByTouch = true;
    [Tooltip("Radius around the hand used for touch collection (metres).")]
    public float touchRadius = 0.12f;

    [Header("Gaze")]
    [Tooltip("Collect an object by looking at it for 'Dwell Time' seconds.")]
    public bool collectByGazeDwell = true;
    [Min(0.1f)] public float dwellTime = 1.5f;

    [Header("Editor / Desktop")]
    [Tooltip("Aim with the mouse cursor when no headset is active.")]
    public bool allowMouse = true;

    private const OVRInput.RawButton RightCollectButtons = OVRInput.RawButton.RIndexTrigger | OVRInput.RawButton.A;
    private const OVRInput.RawButton LeftCollectButtons = OVRInput.RawButton.LIndexTrigger | OVRInput.RawButton.X;

    private Camera mainCamera;
    private OVRCameraRig rig;
    private Interactable currentTarget;

    private DwellTimer gazeDwell;
    private DwellTimer handDwell;

    private TrackedHand leftHand = new TrackedHand(OVRPlugin.Hand.HandLeft);
    private TrackedHand rightHand = new TrackedHand(OVRPlugin.Hand.HandRight);
    private readonly Collider[] touchHits = new Collider[8];

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

        if (rig == null)
            rig = FindAnyObjectByType<OVRCameraRig>();

        if (rig == null)
            return;

        if (leftPointer == null)
            leftPointer = rig.leftControllerAnchor != null ? rig.leftControllerAnchor : rig.leftHandAnchor;
        if (rightPointer == null)
            rightPointer = rig.rightControllerAnchor != null ? rig.rightControllerAnchor : rig.rightHandAnchor;
    }

    private void Update()
    {
        if (mainCamera == null || rig == null)
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
        bool isHandTarget = false;

        // 1. Controller rays.
        if (useControllerRays)
        {
            Interactable right = OVRInput.IsControllerConnected(OVRInput.Controller.RTouch) ? RaycastFrom(rightPointer) : null;
            Interactable left = OVRInput.IsControllerConnected(OVRInput.Controller.LTouch) ? RaycastFrom(leftPointer) : null;

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

        // 2. Hand-tracking rays.
        if (target == null && useHandRays && rig != null)
        {
            leftHand.Update(rig.trackingSpace);
            rightHand.Update(rig.trackingSpace);

            Interactable right = rightHand.HasRay ? Raycast(rightHand.Ray) : null;
            Interactable left = leftHand.HasRay ? Raycast(leftHand.Ray) : null;

            if (left != null && (leftHand.PinchStarted || right == null))
            {
                target = left;
                collect = collectByPinch && leftHand.PinchStarted;
            }
            else if (right != null)
            {
                target = right;
                collect = collectByPinch && rightHand.PinchStarted;
            }

            isHandTarget = target != null;
        }

        // 3. Mouse cursor (only without an active headset).
        if (target == null && allowMouse && !XRActive() && DesktopInput.MousePresent)
        {
            target = Raycast(mainCamera.ScreenPointToRay(DesktopInput.MousePosition));
            collect = mouseClicked;
        }

        // 4. Head gaze. Any collect input (trigger, pinch, click) counts while looking at an object.
        if (target == null)
        {
            Transform head = mainCamera.transform;
            target = Raycast(new Ray(head.position, head.forward));
            collect = anyPressed;
            isGazeTarget = target != null;
        }

        SetCurrentTarget(target);

        if (gazeDwell.Tick(collectByGazeDwell && isGazeTarget ? target : null, dwellTime))
            collect = true;
        if (handDwell.Tick(collectByHandDwell && isHandTarget ? target : null, handDwellTime))
            collect = true;

        if (collect && currentTarget != null)
            Collect(currentTarget);

        // Touch: reach out and touch an object with a hand or controller.
        if (collectByTouch && rig != null)
        {
            TouchCollect(rig.leftHandAnchor);
            TouchCollect(rig.rightHandAnchor);
        }
    }

    private void TouchCollect(Transform hand)
    {
        if (hand == null || !hand.gameObject.activeInHierarchy)
            return;

        int count = Physics.OverlapSphereNonAlloc(hand.position, touchRadius, touchHits, interactionLayer, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            Interactable interactable = touchHits[i].GetComponentInParent<Interactable>();
            if (interactable != null && !interactable.IsCollected)
                Collect(interactable);
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
        gazeDwell.Reset();
        handDwell.Reset();
    }

    private static bool XRActive()
    {
        return UnityEngine.XR.XRSettings.isDeviceActive;
    }

    /// <summary>Counts how long the same target has been aimed at.</summary>
    private struct DwellTimer
    {
        private Interactable target;
        private float time;

        public bool Tick(Interactable aimed, float duration)
        {
            if (aimed == null)
            {
                Reset();
                return false;
            }

            if (aimed != target)
            {
                target = aimed;
                time = 0f;
            }

            time += Time.deltaTime;
            if (time < duration)
                return false;

            time = 0f;
            return true;
        }

        public void Reset()
        {
            target = null;
            time = 0f;
        }
    }

    /// <summary>Hand-tracking state of one hand (pointer ray + index pinch).</summary>
    private class TrackedHand
    {
        private readonly OVRPlugin.Hand hand;
        private OVRPlugin.HandState state;
        private bool wasPinching;

        public bool HasRay { get; private set; }
        public Ray Ray { get; private set; }
        public bool PinchStarted { get; private set; }

        public TrackedHand(OVRPlugin.Hand hand)
        {
            this.hand = hand;
        }

        public void Update(Transform trackingSpace)
        {
            HasRay = false;
            PinchStarted = false;

            if (trackingSpace == null || !OVRPlugin.GetHandState(OVRPlugin.Step.Render, hand, ref state))
            {
                wasPinching = false;
                return;
            }

            bool inputValid = (state.Status & OVRPlugin.HandStatus.InputStateValid) != 0;
            bool systemGesture = (state.Status & OVRPlugin.HandStatus.SystemGestureInProgress) != 0;
            if (!inputValid || systemGesture)
            {
                wasPinching = false;
                return;
            }

            // The pointer pose is reported in tracking space (same conversion as OVRHand.PointerPose).
            OVRPose pose = state.PointerPose.ToOVRPose();
            Vector3 origin = trackingSpace.TransformPoint(pose.position);
            Vector3 direction = trackingSpace.rotation * (pose.orientation * Vector3.forward);
            Ray = new Ray(origin, direction);
            HasRay = true;

            bool pinching = (state.Pinches & OVRPlugin.HandFingerPinch.Index) != 0;
            PinchStarted = pinching && !wasPinching;
            wasPinching = pinching;
        }
    }
}
