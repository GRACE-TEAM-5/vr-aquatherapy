using UnityEngine;

public class ObjectBehaviour : Interactable
{
    public GameObject sporeEffect; // Particle effect to spawn

    public override void OnInteract()
    {
        // Spin the object
        transform.Rotate(0, 360, 0);

        // Spawn a spore effect
        if (sporeEffect != null)
        {
            Instantiate(sporeEffect, transform.position, Quaternion.identity);
        }

        // Optional: Make it disappear after a delay
        Destroy(gameObject, 2f);
    }
}