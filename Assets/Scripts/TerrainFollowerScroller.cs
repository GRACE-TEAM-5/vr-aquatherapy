using UnityEngine;

public class TerrainFollowingScroller : MonoBehaviour
{
    public float speed = 1.5f; // How fast the camera moves forward
    public float heightOffset = 2f; // Distance above the terrain
    public LayerMask terrainLayer; // Assign your Terrain layer here

    void Update()
    {
        // 1. Move Forward
        // We move along the local Z-axis. If your camera looks down -Z, use Vector3.back
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        // 2. Follow the Terrain Height
        // Cast a ray straight down from the camera
        Ray downwardRay = new Ray(transform.position, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(downwardRay, out hit, 100f, terrainLayer))
        {
            // Move the camera to be 'heightOffset' units above the terrain surface
            transform.position = new Vector3(
                transform.position.x, 
                hit.point.y + heightOffset, 
                transform.position.z
            );
        }
    }
}