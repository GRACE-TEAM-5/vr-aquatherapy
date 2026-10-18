using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    [Header("Settings")]
    public LayerMask interactionLayer; // The layer your mushrooms are on
    public float raycastDistance = 100f; // How far the camera can reach
    public float interactionRadius = 5f; // How close you need to be to interact

    [Header("References")]
    private Camera mainCamera;
    private GameObject currentTarget; // The object currently being looked at

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("No Main Camera found! Please tag your camera as 'MainCamera'.");
        }
    }

    void Update()
    {
        if (mainCamera == null) return;

        // 1. Cast a ray from the center of the screen
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit hit;

        // 2. Draw a green line in the Scene View to help you debug
        Debug.DrawRay(mainCamera.transform.position, mainCamera.transform.forward * raycastDistance, Color.green);

        // 3. Check if the ray hit something on the correct layer
        if (Physics.Raycast(ray, out hit, raycastDistance, interactionLayer))
        {
            GameObject target = hit.collider.gameObject;

            // 4. Check if we are close enough to interact
            if (Vector3.Distance(transform.position, hit.point) < interactionRadius)
            {
                // If we are looking at a new object, update the target
                if (currentTarget != target)
                {
                    // Exit the previous object (if any)
                    if (currentTarget != null)
                    {
                        ExitInteraction(currentTarget);
                    }
                    
                    // Enter the new object
                    EnterInteraction(target);
                    currentTarget = target;
                }

                // 5. Check for Mouse Click
                if (Input.GetMouseButtonDown(0)) // 0 is the left mouse button
                {
                    Debug.Log("Mouse Clicked on: " + target.name);
                    PerformInteraction(currentTarget);
                }
            }
            else
            {
                // If we are too far, exit the current interaction
                if (currentTarget != null)
                {
                    ExitInteraction(currentTarget);
                    currentTarget = null;
                }
            }
        }
        else
        {
            // If we hit nothing, exit the current interaction
            if (currentTarget != null)
            {
                ExitInteraction(currentTarget);
                currentTarget = null;
            }
        }
    }

    // Called when we start looking at an object
    void EnterInteraction(GameObject obj)
    {
        if (obj.TryGetComponent<Interactable>(out var interactable))
        {
            interactable.OnHoverEnter();
        }
    }

    // Called when we stop looking at an object
    void ExitInteraction(GameObject obj)
    {
        if (obj.TryGetComponent<Interactable>(out var interactable))
        {
            interactable.OnHoverExit();
        }
    }

    // Called when we click on an object
    void PerformInteraction(GameObject obj)
    {
        if (obj.TryGetComponent<Interactable>(out var interactable))
        {
            interactable.OnInteract();
        }
        else
        {
            Debug.LogWarning("Object " + obj.name + " does not have an Interactable component.");
        }
    }
}
