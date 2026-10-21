using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns collectibles on the terrain in front of the player and removes them
/// once they are far behind. Used for mushrooms (forest) and seashells (beach).
/// </summary>
public class CollectibleSpawner : MonoBehaviour
{
    [Header("Prefabs to Spawn")]
    [Tooltip("Picked at random. Each prefab needs an Interactable component and a collider on the Interactable layer.")]
    public GameObject[] prefabsToSpawn;

    [Header("Spawn Settings")]
    public float spawnInterval = 0.5f;
    [Tooltip("Distance in front of the player where objects appear.")]
    public float spawnAheadDistance = 10f;
    [Tooltip("Objects further than this behind the player are removed.")]
    public float despawnBehindDistance = 10f;
    [Tooltip("Width of the spawn area across the view direction.")]
    public float spawnWidth = 10f;
    [Tooltip("Random spread along the view direction (+/-).")]
    public float spawnDepth = 2f;
    [Tooltip("Maximum number of spawned objects alive at once. 0 = unlimited.")]
    public int maxActiveObjects = 30;

    [Tooltip("Objects spawned at once after the player was moved to the other side of the map.")]
    public int spawnBurstAfterRelocation = 6;

    [Header("Random Variation")]
    [Tooltip("Uniform scale multiplier applied to the prefab's own scale.")]
    public Vector2 scaleRange = new Vector2(0.75f, 1.25f);
    public bool randomYRotation = true;

    [Header("Grounding Settings")]
    [Tooltip("Vertical offset above the terrain hit point.")]
    public float heightOffset = 0.1f;
    [Tooltip("Layer(s) of the ground. Use the 'Terrain' layer.")]
    public LayerMask terrainLayer;
    [Tooltip("The ground probe starts this far above the player's head (only used for non-terrain ground).")]
    public float groundProbeHeight = 20f;
    [Tooltip("Spawns are skipped when an obstacle (e.g. a tree) is closer than this to the spot.")]
    public float obstacleClearance = 0.5f;
    [Tooltip("Layers counted as obstacles for the clearance check. Terrain trees are part of the Terrain layer.")]
    public LayerMask obstacleLayers = (1 << 0) | (1 << 3);

    private float nextSpawnTime;
    private readonly List<GameObject> spawnedObjects = new List<GameObject>();

    private void OnEnable()
    {
        PlayAreaWrapper.PlayerRelocated += HandleRelocated;
    }

    private void OnDisable()
    {
        PlayAreaWrapper.PlayerRelocated -= HandleRelocated;
    }

    /// <summary>Removes every object spawned by this spawner.</summary>
    public void ClearSpawned()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        spawnedObjects.Clear();
    }

    private void HandleRelocated(Vector3 offset)
    {
        // The old objects are now on the other side of the map.
        ClearSpawned();

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        // Spread the burst between close range and the normal spawn distance.
        for (int i = 0; i < spawnBurstAfterRelocation; i++)
        {
            float t = spawnBurstAfterRelocation > 1 ? i / (float)(spawnBurstAfterRelocation - 1) : 1f;
            TrySpawn(mainCamera.transform, Mathf.Lerp(4f, spawnAheadDistance, t));
        }
        nextSpawnTime = Time.time + spawnInterval;
    }

    private void Start()
    {
        if (terrainLayer.value == 0)
            Debug.LogWarning("CollectibleSpawner: Terrain Layer is empty, objects cannot be placed on the ground.", this);
    }

    private void Update()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        CleanupOldObjects(mainCamera.transform);

        if (Time.time < nextSpawnTime)
            return;

        nextSpawnTime = Time.time + spawnInterval;
        if (maxActiveObjects <= 0 || spawnedObjects.Count < maxActiveObjects)
            TrySpawn(mainCamera.transform);
    }

    private void TrySpawn(Transform view)
    {
        TrySpawn(view, spawnAheadDistance);
    }

    private void TrySpawn(Transform view, float aheadDistance)
    {
        if (prefabsToSpawn == null || prefabsToSpawn.Length == 0)
            return;

        GameObject prefab = prefabsToSpawn[Random.Range(0, prefabsToSpawn.Length)];
        if (prefab == null)
            return;

        Vector3 forward = Flatten(view.forward, Vector3.forward);
        Vector3 right = Flatten(view.right, Vector3.right);

        Vector3 position = view.position
            + forward * (aheadDistance + Random.Range(-spawnDepth, spawnDepth))
            + right * Random.Range(-spawnWidth * 0.5f, spawnWidth * 0.5f);

        // Ground height (ignores trees painted on the terrain); skip the spawn if there is no ground.
        Vector3 probe = new Vector3(position.x, view.position.y, position.z);
        if (!Ground.TryGetHeight(probe, terrainLayer, groundProbeHeight, out float groundY))
            return;

        position = new Vector3(position.x, groundY + heightOffset, position.z);

        // Do not spawn inside a tree or other obstacle.
        if (Physics.CheckSphere(position + Vector3.up * 1f, obstacleClearance, obstacleLayers, QueryTriggerInteraction.Ignore))
            return;
        Quaternion rotation = prefab.transform.rotation;
        if (randomYRotation)
            rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * rotation;

        GameObject instance = Instantiate(prefab, position, rotation);
        float scale = Random.Range(Mathf.Min(scaleRange.x, scaleRange.y), Mathf.Max(scaleRange.x, scaleRange.y));
        instance.transform.localScale = prefab.transform.localScale * scale;

        spawnedObjects.Add(instance);
    }

    private void CleanupOldObjects(Transform view)
    {
        Vector3 forward = Flatten(view.forward, Vector3.forward);

        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = spawnedObjects[i];
            if (obj == null)
            {
                // Collected (destroyed) objects.
                spawnedObjects.RemoveAt(i);
                continue;
            }

            float distanceAlongView = Vector3.Dot(forward, obj.transform.position - view.position);
            if (distanceAlongView < -despawnBehindDistance)
            {
                spawnedObjects.RemoveAt(i);
                Destroy(obj);
            }
        }
    }

    private static Vector3 Flatten(Vector3 v, Vector3 fallback)
    {
        v.y = 0f;
        return v.sqrMagnitude < 0.001f ? fallback : v.normalized;
    }
}
