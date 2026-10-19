using UnityEngine;

public class ObjectGrounding : MonoBehaviour
{
    public float heightOffset = 0.1f; // Slight offset to prevent clipping
    public LayerMask terrainLayer;

    void Update()
    {
        Ray downwardRay = new Ray(transform.position, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(downwardRay, out hit, 10f, terrainLayer))
        {
            // Align the bottom of the object to the terrain
            float objectHeight = transform.localScale.y * 0.5f; // Approximate height
            transform.position = new Vector3(
                transform.position.x,
                hit.point.y + heightOffset,
                transform.position.z
            );
        }
    }
}