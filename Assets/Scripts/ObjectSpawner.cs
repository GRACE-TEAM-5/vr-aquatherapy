using UnityEngine;
using System.Collections.Generic;

public class ObjectSpawner : MonoBehaviour
{
    [Header("Prefabs to Spawn")]
    public GameObject[] prefabsToSpawn; 

    [Header("Spawn Settings")]
    public float spawnInterval = 0.5f; 
    public float spawnAheadDistance = 10f; 
    public float despawnBehindDistance = 10f; 
    public float spawnWidth = 10f; 

    [Header("Grounding Settings")]
    public float heightOffset = 0.1f;
    public LayerMask terrainLayer; // Assign your Terrain layer here

    private float nextSpawnTime = 0f;
    private List<GameObject> spawnedObjects = new List<GameObject>();  

    void Update()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        if (Time.time >= nextSpawnTime)
        {
            SpawnObject(mainCamera);
            nextSpawnTime = Time.time + spawnInterval;
        }

        CleanupOldObjects(mainCamera);
    }

    void SpawnObject(Camera camera)
    {
        if (prefabsToSpawn.Length == 0) return;

        // 1. Calculate Position
        Vector3 spawnPos = camera.transform.position + (camera.transform.forward * spawnAheadDistance);
        float randomX = Random.Range(-spawnWidth / 2f, spawnWidth / 2f);
        float randomZ = Random.Range(-2f, 2f);
        spawnPos.x += randomX;
        spawnPos.z += randomZ;

        // 2. Pick Random Prefab
        GameObject prefab = prefabsToSpawn[Random.Range(0, prefabsToSpawn.Length)];
        GameObject instance = Instantiate(prefab, spawnPos, Quaternion.identity);

        // 3. Random Scale
        Vector3 randomScale = new Vector3(
            Random.Range(0.5f, 1.5f),
            Random.Range(0.5f, 1.5f),
            Random.Range(0.5f, 1.5f)
        );
        instance.transform.localScale = randomScale;
        instance.transform.Rotate(0, Random.Range(0, 360), 0);

        // 4. Grounding (Raycast to find terrain height)
        Ray downwardRay = new Ray(instance.transform.position, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(downwardRay, out hit, 100f, terrainLayer))
        {
            // Move the object to sit on the terrain
            instance.transform.position = new Vector3(
                instance.transform.position.x,
                hit.point.y + heightOffset,
                instance.transform.position.z
            );
        }
        
        spawnedObjects.Add(instance);
    }

    void CleanupOldObjects(Camera camera)
    {
        // We don't need tags anymore! We can just destroy them by distance.
        // However, if you have thousands of objects, finding them all every frame is slow.
        // For a simple scene, this works fine.
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = spawnedObjects[i];
            float distanceBehind = Vector3.Dot(camera.transform.forward, obj.transform.position - camera.transform.position);
            if (distanceBehind < -despawnBehindDistance)
            {
                spawnedObjects.RemoveAt(i);
                Destroy(obj);
            }
        }
    }
}
