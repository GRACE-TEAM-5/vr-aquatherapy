using UnityEngine;
using UnityEngine.XR;

public class InteractionManager : MonoBehaviour
{
    [Header("Settings")]
    public LayerMask interactionLayer;
    public float raycastDistance = 100f;
    public float interactionRadius = 5f;

    private Camera mainCamera;
    private GameObject currentTarget;
    private bool handPinchWasPressed;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
            Debug.LogError("No Main Camera found! Please tag your camera as 'MainCamera'.");
    }

    void Update()
    {
        if (mainCamera == null) return;

        bool handInput = TryGetHandInteraction(out Ray handRay, out bool handPinchPressed);
        bool mouseInput = Input.mousePresent &&
            (Input.GetMouseButton(0) || Input.GetAxis("Mouse X") != 0f || Input.GetAxis("Mouse Y") != 0f);
        Ray ray = handInput
            ? handRay
            : mouseInput
            ? mainCamera.ScreenPointToRay(Input.mousePosition)
            : new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        bool collectPressed = WasHandPinchPressed(handPinchPressed);

        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, interactionLayer))
        {
            GameObject target = hit.collider.GetComponentInParent<Interactable>()?.gameObject;
            if (target != null && Vector3.Distance(ray.origin, hit.point) < interactionRadius)
            {
                SetCurrentTarget(target);
                if (Input.GetMouseButtonDown(0) || collectPressed)
                    PerformInteraction(currentTarget);
            }
            else
            {
                ClearCurrentTarget();
            }
        }
        else
        {
            ClearCurrentTarget();
        }

    }

    void SetCurrentTarget(GameObject target)
    {
        if (currentTarget == target) return;

        if (currentTarget != null)
            ExitInteraction(currentTarget);

        EnterInteraction(target);
        currentTarget = target;
    }

    void ClearCurrentTarget()
    {
        if (currentTarget == null) return;

        ExitInteraction(currentTarget);
        currentTarget = null;
    }

    bool TryGetHandInteraction(out Ray handRay, out bool pinchPressed)
    {
        handRay = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        pinchPressed = false;
        bool handFound = false;
        var hands = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.HandTracking | InputDeviceCharacteristics.HeldInHand,
            hands);

        foreach (InputDevice hand in hands)
        {
            if (!hand.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position) ||
                !hand.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
                continue;

            Ray candidateRay = new Ray(position, rotation * Vector3.forward);
            if (!handFound ||
                (!Physics.Raycast(handRay, raycastDistance, interactionLayer) &&
                 Physics.Raycast(candidateRay, raycastDistance, interactionLayer)))
            {
                handRay = candidateRay;
                handFound = true;
            }

            pinchPressed |= IsHandPinching(hand);
        }

        return handFound;
    }

    bool IsHandPinching(InputDevice hand)
    {
        return (hand.TryGetFeatureValue(CommonUsages.triggerButton, out bool trigger) && trigger) ||
            (hand.TryGetFeatureValue(CommonUsages.primaryButton, out bool primary) && primary) ||
            (hand.TryGetFeatureValue(CommonUsages.gripButton, out bool grip) && grip);
    }

    bool WasHandPinchPressed(bool pinchPressed)
    {
        bool wasPressed = pinchPressed && !handPinchWasPressed;
        handPinchWasPressed = pinchPressed;
        return wasPressed;
    }

    void EnterInteraction(GameObject obj)
    {
        if (obj.TryGetComponent<Interactable>(out var interactable))
            interactable.OnHoverEnter();
    }

    void ExitInteraction(GameObject obj)
    {
        if (obj.TryGetComponent<Interactable>(out var interactable))
            interactable.OnHoverExit();
    }

    void PerformInteraction(GameObject obj)
    {
        if (obj.TryGetComponent<Interactable>(out var interactable))
            interactable.OnInteract();
        else
            Debug.LogWarning("Object " + obj.name + " does not have an Interactable component.");
    }
}
