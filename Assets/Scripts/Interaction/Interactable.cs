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

    [Header("Hitbox")]
    [Tooltip("Adds an invisible, larger trigger sphere so the object is easier to hit by gaze, rays and hands.")]
    public bool enlargeHitbox = true;
    [Tooltip("Hitbox radius relative to the object's visible size (1 = just around the object).")]
    public float hitboxScale = 1.5f;
    [Tooltip("Minimum hitbox radius in metres.")]
    public float minHitboxRadius = 0.25f;

    protected bool isCollected;
    public bool IsCollected => isCollected;

    private Renderer[] renderers;
    private MaterialPropertyBlock propertyBlock;
    private bool highlighted;

    protected virtual void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
    }

    protected virtual void Start()
    {
        // In Start, so the random scale applied by the spawner is already included.
        if (enlargeHitbox)
            CreateHitbox();
    }

    private void CreateHitbox()
    {
        if (renderers == null || renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                bounds.Encapsulate(renderers[i].bounds);
        }

        float worldRadius = Mathf.Max(bounds.extents.magnitude * hitboxScale, minHitboxRadius);
        Vector3 s = transform.lossyScale;
        float scale = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z), 0.0001f);

        GameObject hitbox = new GameObject("Hitbox");
        hitbox.layer = gameObject.layer;
        hitbox.transform.SetParent(transform, false);
        hitbox.transform.position = bounds.center;

        SphereCollider sphere = hitbox.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = worldRadius / scale;
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
