using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor tool: reduces the hills of all terrains in the open scene (less up/down
/// movement = less motion sickness). Heights are pulled towards one common reference
/// height, so neighbouring terrain tiles still line up. Trees, grass and objects placed
/// on the ground stay on the ground. Supports Undo (Ctrl+Z).
/// Menu: Aquatherapy > Flatten Terrains...
/// </summary>
public class TerrainFlattenTool : EditorWindow
{
    private enum ReferenceHeight
    {
        Average,
        Lowest
    }

    [SerializeField] private float keepRelief = 0.3f;
    [SerializeField] private ReferenceHeight reference = ReferenceHeight.Average;
    [SerializeField] private bool moveObjectsWithGround = true;

    [MenuItem("Aquatherapy/Flatten Terrains...")]
    private static void Open()
    {
        TerrainFlattenTool window = GetWindow<TerrainFlattenTool>(true, "Flatten Terrains");
        window.minSize = new Vector2(380f, 210f);
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Flattens every terrain in the open scene. Open the Forest scene, apply, save; " +
            "then do the same for the Beach scene. Ctrl+Z undoes it.",
            MessageType.Info);

        keepRelief = EditorGUILayout.Slider(
            new GUIContent("Keep hills (%)", "0 % = completely flat, 100 % = unchanged."),
            keepRelief * 100f, 0f, 100f) / 100f;
        reference = (ReferenceHeight)EditorGUILayout.EnumPopup(
            new GUIContent("Flatten towards", "Average: hills go down, valleys come up. Lowest: everything sinks to the lowest point."),
            reference);
        moveObjectsWithGround = EditorGUILayout.Toggle(
            new GUIContent("Keep objects on ground", "Moves placed objects (trees, props) up/down with the ground."),
            moveObjectsWithGround);

        Terrain[] terrains = Terrain.activeTerrains;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Terrains in open scene(s): {terrains.Length}");

        using (new EditorGUI.DisabledScope(terrains.Length == 0 || EditorApplication.isPlaying))
        {
            if (GUILayout.Button("Flatten", GUILayout.Height(30f)))
                Flatten(terrains, keepRelief, reference, moveObjectsWithGround);
        }
    }

    private static void Flatten(Terrain[] terrains, float keep, ReferenceHeight reference, bool moveObjects)
    {
        // 1. Common reference height (world space) over all terrains.
        double sum = 0;
        long count = 0;
        float lowest = float.MaxValue;
        foreach (Terrain terrain in terrains)
        {
            TerrainData data = terrain.terrainData;
            int res = data.heightmapResolution;
            float[,] heights = data.GetHeights(0, 0, res, res);
            float baseY = terrain.GetPosition().y;
            float sizeY = data.size.y;
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float world = baseY + heights[y, x] * sizeY;
                    sum += world;
                    count++;
                    if (world < lowest)
                        lowest = world;
                }
            }
        }

        if (count == 0)
            return;

        float referenceY = reference == ReferenceHeight.Average ? (float)(sum / count) : lowest;

        // 2. Remember how high objects stand above the ground.
        var grounded = new List<(Transform transform, float heightAboveGround)>();
        if (moveObjects)
        {
            foreach (Transform t in CollectGroundObjects())
            {
                if (TryGroundHeight(t.position, out float ground) && Mathf.Abs(t.position.y - ground) < 3f)
                    grounded.Add((t, t.position.y - ground));
            }
        }

        // 3. Flatten.
        var undoObjects = new List<Object>();
        foreach (Terrain terrain in terrains)
            undoObjects.Add(terrain.terrainData);
        foreach (var g in grounded)
            undoObjects.Add(g.transform);
        Undo.RegisterCompleteObjectUndo(undoObjects.ToArray(), "Flatten Terrains");

        foreach (Terrain terrain in terrains)
        {
            TerrainData data = terrain.terrainData;
            int res = data.heightmapResolution;
            float[,] heights = data.GetHeights(0, 0, res, res);
            float baseY = terrain.GetPosition().y;
            float sizeY = Mathf.Max(data.size.y, 0.0001f);

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float world = baseY + heights[y, x] * sizeY;
                    float flattened = referenceY + (world - referenceY) * keep;
                    heights[y, x] = Mathf.Clamp01((flattened - baseY) / sizeY);
                }
            }

            data.SetHeights(0, 0, heights);
            data.SyncHeightmap();

            // Put the painted trees back onto the new surface.
            data.SetTreeInstances(data.treeInstances, true);
            EditorUtility.SetDirty(data);
        }

        // 4. Move objects with the ground.
        foreach (var g in grounded)
        {
            if (TryGroundHeight(g.transform.position, out float ground))
            {
                Vector3 p = g.transform.position;
                g.transform.position = new Vector3(p.x, ground + g.heightAboveGround, p.z);
            }
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
            EditorSceneManager.MarkSceneDirty(SceneManager.GetSceneAt(i));
        AssetDatabase.SaveAssets();

        Debug.Log($"[Aquatherapy] Flattened {terrains.Length} terrain(s) to {keep * 100f:0}% relief " +
                  $"(reference height {referenceY:0.00} m), moved {grounded.Count} object(s). Save the scene to keep it.");
    }

    /// <summary>Root objects of the open scenes that stand on the ground (not terrains, player rig or UI).</summary>
    private static IEnumerable<Transform> CollectGroundObjects()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
                continue;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                // Only visible objects (props, trees); managers and empty helpers stay where they are.
                if (root.GetComponentInChildren<Renderer>(true) == null
                    || root.GetComponent<Terrain>() != null
                    || root.GetComponentInChildren<Camera>(true) != null
                    || root.GetComponentInChildren<Canvas>(true) != null
                    || root.GetComponent<Light>() != null)
                    continue;

                yield return root.transform;
            }
        }
    }

    private static bool TryGroundHeight(Vector3 position, out float height)
    {
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            Vector3 p = terrain.GetPosition();
            Vector3 s = terrain.terrainData.size;
            if (position.x >= p.x && position.x <= p.x + s.x && position.z >= p.z && position.z <= p.z + s.z)
            {
                height = terrain.SampleHeight(position) + p.y;
                return true;
            }
        }

        height = 0f;
        return false;
    }
}
