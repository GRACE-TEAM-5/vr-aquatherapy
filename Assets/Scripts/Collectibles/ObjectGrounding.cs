using UnityEngine;

/// <summary>
/// Places the object on the ground (raycast down onto the terrain layer).
/// </summary>
public class ObjectGrounding : MonoBehaviour
{
    [Tooltip("Slight offset above the ground to prevent clipping.")]
    public float heightOffset = 0.1f;
    [Tooltip("Layer(s) of the ground. Use the 'Terrain' layer.")]
    public LayerMask terrainLayer;
    [Tooltip("Keep re-grounding every frame (only needed if the ground or object moves).")]
    public bool continuous;
    [Tooltip("The ground probe starts this far above the object.")]
    public float probeHeight = 2f;

    private void Start()
    {
        Ground();
    }

    private void Update()
    {
        if (continuous)
            Ground();
    }

    public void Ground()
    {
        Vector3 origin = transform.position + Vector3.up * probeHeight;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, probeHeight + 10f, terrainLayer, QueryTriggerInteraction.Ignore))
        {
            Vector3 p = transform.position;
            transform.position = new Vector3(p.x, hit.point.y + heightOffset, p.z);
        }
    }
}
