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
    private bool xrInteractionWasPressed;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
            Debug.LogError("No Main Camera found! Please tag your camera as 'MainCamera'.");
    }

    void Update()
    {
        if (mainCamera == null) return;

        bool mouseInput = Input.mousePresent &&
            (Input.GetMouseButton(0) || Input.GetAxis("Mouse X") != 0f || Input.GetAxis("Mouse Y") != 0f);
        Ray ray = mouseInput
            ? mainCamera.ScreenPointToRay(Input.mousePosition)
            : new Ray(mainCamera.transform.position, mainCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, interactionLayer))
        {
            GameObject target = hit.collider.GetComponentInParent<Interactable>()?.gameObject;
            if (target != null && Vector3.Distance(mainCamera.transform.position, hit.point) < interactionRadius)
            {
                SetCurrentTarget(target);
                if (Input.GetMouseButtonDown(0) || WasXRInteractionPressed())
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

        WasXRInteractionPressed();
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

    bool WasXRInteractionPressed()
    {
        bool pressed = false;
        var devices = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand,
            devices);

        foreach (InputDevice device in devices)
        {
            if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerPressed) && triggerPressed ||
                device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryPressed) && primaryPressed)
            {
                pressed = true;
                break;
            }
        }

        bool wasPressed = pressed && !xrInteractionWasPressed;
        xrInteractionWasPressed = pressed;
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
