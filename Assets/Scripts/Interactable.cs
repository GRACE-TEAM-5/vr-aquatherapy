using UnityEngine;

public class Interactable : MonoBehaviour
{
    // Called when the player looks at the object
    public virtual void OnHoverEnter()
    {
        Debug.Log("Hover Enter: " + name);
        // Example: Highlight the object
        GetComponent<Renderer>().material.color = Color.yellow;
    }

    // Called when the player stops looking at the object
    public virtual void OnHoverExit()
    {
        Debug.Log("Hover Exit: " + name);
        // Example: Reset color
        GetComponent<Renderer>().material.color = Color.white;
    }

    // Called when the player interacts (clicks, triggers, etc.)
    public virtual void OnInteract()
    {
        // Default: Play a sound or spin
        transform.Rotate(0, 180, 0);
        Debug.Log("Interacted with: " + name);
    }
}