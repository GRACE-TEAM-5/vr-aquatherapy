using UnityEngine;

public class ObjectBehaviour : Interactable
{
    public GameObject sporeEffect; // Particle effect to spawn

    protected override void OnCollected()
    {
        // Spawn a spore effect
        if (sporeEffect != null)
        {
            Instantiate(sporeEffect, transform.position, Quaternion.identity);
        }

        base.OnCollected();
    }
}