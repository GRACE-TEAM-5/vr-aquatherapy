using UnityEngine;

public class Interactable : MonoBehaviour
{
    [Header("Collection")]
    public AudioClip collectSound;

    protected bool isCollected;

    // Called when the player looks at the object
    public virtual void OnHoverEnter()
    {
        if (isCollected)
            return;

        Debug.Log("Hover Enter: " + name);
        // Example: Highlight the object
        if (TryGetComponent<Renderer>(out var renderer))
            renderer.material.color = Color.yellow;
    }

    // Called when the player stops looking at the object
    public virtual void OnHoverExit()
    {
        if (isCollected)
            return;

        Debug.Log("Hover Exit: " + name);
        // Example: Reset color
        if (TryGetComponent<Renderer>(out var renderer))
            renderer.material.color = Color.white;
    }

    // Called when the player interacts (clicks, triggers, etc.)
    public virtual void OnInteract()
    {
        if (!TryCollect())
            return;

        OnCollected();
        Debug.Log("Collected: " + name);
    }

    protected bool TryCollect()
    {
        if (isCollected)
            return false;

        isCollected = true;
        CollectedPrefabCounter.Increment();
        return true;
    }

    protected virtual void OnCollected()
    {
        if (collectSound != null)
            AudioSource.PlayClipAtPoint(collectSound, transform.position);

        Destroy(gameObject);
    }
}