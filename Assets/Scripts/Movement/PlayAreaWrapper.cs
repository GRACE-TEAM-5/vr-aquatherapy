using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Keeps the player inside the map without having to turn around.
/// When the player gets close to the map border while moving outwards, the view fades
/// out, the rig is moved to the opposite side of the map (keeping the direction the
/// player is facing, so the whole map lies ahead again) and the view fades back in.
///
/// Unity terrains cannot be rotated, so instead of rotating the world around the player
/// the player is relocated - this is also far more comfortable than artificial rotation.
/// Put it on the player rig (OVRCameraRig root).
/// </summary>
public class PlayAreaWrapper : MonoBehaviour
{
    /// <summary>Raised after the player has been relocated (argument: movement offset).</summary>
    public static event Action<Vector3> PlayerRelocated;

    [Tooltip("Player rig root. Defaults to this transform.")]
    public Transform rig;
    [Tooltip("The head (CenterEyeAnchor). Defaults to Camera.main.")]
    public Transform viewTransform;

    [Header("Play Area")]
    [Tooltip("Relocate when the head is closer than this to the map border (metres).")]
    public float borderMargin = 15f;
    [Tooltip("Optional manual play area (world space). Leave the size at 0 to use the bounds of all terrains in the scene.")]
    public Bounds customArea;

    [Header("Fade")]
    public float fadeOutTime = 0.4f;
    public float holdTime = 0.25f;
    public float fadeInTime = 0.6f;
    public Color fadeColor = Color.black;

    private Rect innerArea;
    private bool hasArea;
    private bool relocating;

    private void Start()
    {
        if (rig == null)
            rig = transform;
        if (viewTransform == null && Camera.main != null)
            viewTransform = Camera.main.transform;

        ComputeArea();
    }

    private void ComputeArea()
    {
        Rect area;
        if (customArea.size.x > 0f && customArea.size.z > 0f)
        {
            area = Rect.MinMaxRect(customArea.min.x, customArea.min.z, customArea.max.x, customArea.max.z);
        }
        else
        {
            Terrain[] terrains = Terrain.activeTerrains;
            if (terrains == null || terrains.Length == 0)
            {
                Debug.LogWarning("PlayAreaWrapper: no terrain found and no custom area set.", this);
                return;
            }

            area = default;
            bool first = true;
            foreach (Terrain terrain in terrains)
            {
                if (terrain == null || terrain.terrainData == null)
                    continue;

                Vector3 p = terrain.GetPosition();
                Vector3 s = terrain.terrainData.size;
                Rect r = Rect.MinMaxRect(p.x, p.z, p.x + s.x, p.z + s.z);
                area = first ? r : Rect.MinMaxRect(
                    Mathf.Min(area.xMin, r.xMin), Mathf.Min(area.yMin, r.yMin),
                    Mathf.Max(area.xMax, r.xMax), Mathf.Max(area.yMax, r.yMax));
                first = false;
            }

            if (first)
                return;
        }

        float margin = Mathf.Min(borderMargin, Mathf.Min(area.width, area.height) * 0.25f);
        innerArea = Rect.MinMaxRect(area.xMin + margin, area.yMin + margin, area.xMax - margin, area.yMax - margin);
        hasArea = innerArea.width > 1f && innerArea.height > 1f;
    }

    private void Update()
    {
        if (!hasArea || relocating || viewTransform == null)
            return;

        Vector2 head = new Vector2(viewTransform.position.x, viewTransform.position.z);
        if (innerArea.Contains(head))
            return;

        Vector2 forward = FlatForward();

        // Only when heading outwards; a player turning back inside is left alone.
        if (Vector2.Dot(forward, innerArea.center - head) > 0f)
            return;

        StartCoroutine(Relocate(forward));
    }

    private IEnumerator Relocate(Vector2 forward)
    {
        relocating = true;

        yield return ScreenFader.FadeTo(1f, fadeOutTime, fadeColor);

        // Re-read the direction at the darkest moment and place the head on the
        // opposite side of the area, so the whole map lies in front of the player.
        forward = FlatForward();
        Vector2 center = innerArea.center;
        Vector2 back = -forward;
        float distanceToEdge = DistanceToEdge(center, back, innerArea) * 0.95f;
        Vector2 target = center + back * distanceToEdge;

        Vector3 head = viewTransform.position;
        Vector3 offset = new Vector3(target.x - head.x, 0f, target.y - head.z);
        rig.position += offset;
        Physics.SyncTransforms();
        PlayerRelocated?.Invoke(offset);

        yield return new WaitForSecondsRealtime(holdTime);
        yield return ScreenFader.FadeTo(0f, fadeInTime, fadeColor);

        relocating = false;
    }

    private Vector2 FlatForward()
    {
        Vector3 f = viewTransform.forward;
        Vector2 flat = new Vector2(f.x, f.z);
        return flat.sqrMagnitude < 0.0001f ? Vector2.up : flat.normalized;
    }

    /// <summary>Distance from a point inside the rect to its border along a direction.</summary>
    private static float DistanceToEdge(Vector2 origin, Vector2 dir, Rect rect)
    {
        float tx = dir.x > 0f ? (rect.xMax - origin.x) / dir.x : dir.x < 0f ? (rect.xMin - origin.x) / dir.x : float.PositiveInfinity;
        float ty = dir.y > 0f ? (rect.yMax - origin.y) / dir.y : dir.y < 0f ? (rect.yMin - origin.y) / dir.y : float.PositiveInfinity;
        return Mathf.Max(0f, Mathf.Min(tx, ty));
    }

    private void OnDrawGizmosSelected()
    {
        if (!hasArea)
            return;

        Gizmos.color = Color.cyan;
        Vector3 c = new Vector3(innerArea.center.x, transform.position.y, innerArea.center.y);
        Gizmos.DrawWireCube(c, new Vector3(innerArea.width, 0.1f, innerArea.height));
    }
}
