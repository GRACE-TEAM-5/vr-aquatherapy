using UnityEngine;

/// <summary>
/// Finds the ground height below a point.
/// On Unity terrains the height map is sampled directly, so trees painted on the
/// terrain (whose colliders are part of the TerrainCollider) are ignored - a plain
/// downward raycast would land on top of the tree. Elsewhere a raycast is used.
/// </summary>
public static class Ground
{
    public static bool TryGetHeight(Vector3 position, LayerMask groundLayers, float probeHeight, out float height)
    {
        Terrain terrain = TerrainAt(position, groundLayers);
        if (terrain != null)
        {
            height = terrain.SampleHeight(position) + terrain.GetPosition().y;
            return true;
        }

        Vector3 origin = position + Vector3.up * probeHeight;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, probeHeight + 200f, groundLayers, QueryTriggerInteraction.Ignore)
            && !(hit.collider is TerrainCollider))
        {
            height = hit.point.y;
            return true;
        }

        height = 0f;
        return false;
    }

    /// <summary>The active terrain (on one of the given layers) whose area contains the point, or null.</summary>
    public static Terrain TerrainAt(Vector3 position, LayerMask layers)
    {
        Terrain[] terrains = Terrain.activeTerrains;
        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain t = terrains[i];
            if (t == null || t.terrainData == null || (layers.value & (1 << t.gameObject.layer)) == 0)
                continue;

            Vector3 p = t.GetPosition();
            Vector3 s = t.terrainData.size;
            if (position.x >= p.x && position.x <= p.x + s.x && position.z >= p.z && position.z <= p.z + s.z)
                return t;
        }

        return null;
    }
}
