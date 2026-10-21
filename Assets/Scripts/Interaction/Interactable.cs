using UnityEngine;

/// <summary>
/// Something the player can point at and collect (mushrooms, seashells, ...).
/// Put it on the prefab root; the collider(s) must be on the Interactable layer.
/// </summary>
public class Interactable : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [Header("Collection")]
    public AudioClip collectSound;
    [Range(0f, 1f)] public float collectVolume = 1f;

    [Header("Hover Highlight")]
    public Color highlightColor = new Color(1f, 0.9f, 0.3f, 1f);

    protected bool isCollected;
    public bool IsCollected => isCollected;

    private Renderer[] renderers;
    private MaterialPropertyBlock propertyBlock;
    private bool highlighted;

    protected virtual void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
    }

    /// <summary>Called when the player starts pointing/looking at the object.</summary>
    public virtual void OnHoverEnter()
    {
        if (!isCollected)
            SetHighlight(true);
    }

    /// <summary>Called when the player stops pointing/looking at the object.</summary>
    public virtual void OnHoverExit()
    {
        if (!isCollected)
            SetHighlight(false);
    }

    /// <summary>Called when the player collects the object (trigger, pinch, click or gaze dwell).</summary>
    public virtual void OnInteract()
    {
        if (!TryCollect())
            return;

        OnCollected();
    }

    protected bool TryCollect()
    {
        if (isCollected)
            return false;

        isCollected = true;
        CollectibleCounter.Increment();
        return true;
    }

    protected virtual void OnCollected()
    {
        if (collectSound != null)
            AudioSource.PlayClipAtPoint(collectSound, transform.position, collectVolume);

        Destroy(gameObject);
    }

    private void SetHighlight(bool on)
    {
        if (highlighted == on || renderers == null)
            return;

        highlighted = on;
        propertyBlock ??= new MaterialPropertyBlock();

        foreach (Renderer r in renderers)
        {
            if (r == null)
                continue;

            if (!on)
            {
                // Restores the colours stored in the material.
                r.SetPropertyBlock(null);
                continue;
            }

            r.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, highlightColor); // URP Lit
            propertyBlock.SetColor(ColorId, highlightColor);     // Built-in / legacy shaders
            r.SetPropertyBlock(propertyBlock);
        }
    }
}
