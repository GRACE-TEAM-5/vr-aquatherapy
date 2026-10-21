using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Collectible that also spawns a visual effect (e.g. spores) when it is collected.
/// </summary>
public class CollectibleWithEffect : Interactable
{
    [FormerlySerializedAs("sporeEffect")]
    [Tooltip("Prefab spawned at the object's position when it is collected.")]
    public GameObject collectEffect;

    [Tooltip("Seconds until the spawned effect is destroyed. 0 = the effect manages its own lifetime.")]
    public float effectLifetime = 5f;

    protected override void OnCollected()
    {
        if (collectEffect != null)
        {
            GameObject effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
            if (effectLifetime > 0f)
                Destroy(effect, effectLifetime);
        }

        base.OnCollected();
    }
}
