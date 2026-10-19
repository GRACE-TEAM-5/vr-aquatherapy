# Required Tags and Layers

Use the exact names below. Unity tags and layers are case-sensitive, and the
names must exist in **Edit > Project Settings > Tags and Layers**.

## Required assignments

| Object | Tag | Layer | Why |
| --- | --- | --- | --- |
| Main camera | `MainCamera` | Any appropriate camera layer | `Camera.main` is used by the spawner and interaction scripts. |
| Terrain or environment ground collider | `Terrain` | `Terrain` | `TerrainFollowingScroller`, `ObjectSpawner`, and `ObjectGrounding` use a `LayerMask` to raycast against this layer. |
| Interactable prefab root and its collider | Usually no special tag | `Interactable` | `InteractionManager` only raycasts against the configured interaction layer. The collider must be on this layer so the object can be targeted. |

The project's `Terrain` layer is layer 3 and its `Interactable` layer is layer
6. Prefer selecting the names in the Unity Inspector rather than entering layer
numbers manually.

### Main camera

Select the camera used for gameplay and set **Tag** to `MainCamera`. There must
be one active gameplay camera with this tag.

### Environment and terrain

Set the GameObject that owns the terrain or ground collider to:

- **Tag:** `Terrain`
- **Layer:** `Terrain`

The tag identifies the object for humans and editor searches; the layer is what
the runtime raycasts actually use. If a ground collider is on a child object,
set the child collider’s layer to `Terrain` as well.

In each scene, set the `terrainLayer` field on `TerrainFollowingScroller`,
`ObjectSpawner`, and `ObjectGrounding` (when present) to the `Terrain` layer.

### Interactable prefabs

For a prefab that can be collected or otherwise interacted with:

1. Add an `Interactable` or `ObjectBehaviour` component to the prefab root.
2. Set the root to the `Interactable` layer.
3. Set every child GameObject containing a collider to the `Interactable`
   layer. `InteractionManager` raycasts the collider first and then searches
   its parent for the interactable component.

Set the `interactionLayer` field on `InteractionManager` to the `Interactable`
   layer. A prefab does not need a special tag for interaction.

## Available project tags

These tags are defined in `ProjectSettings/TagManager.asset`:

- `Terrain` - classification tag for environment ground.
- `Mushroom` - optional classification for mushroom assets.
- `Seashell` - optional classification for seashell assets.
- `Spawnable` - optional classification for assets intended to be spawned.
- `MainCamera` - Unity’s built-in camera tag and required on the gameplay camera.

`ObjectSpawner` receives its prefab list through `prefabsToSpawn`; it does not
search by the `Spawnable` tag. Therefore, adding `Spawnable` alone does not make
a prefab spawn. Add the prefab to the `prefabsToSpawn` array and make sure its
colliders use the `Interactable` layer if it should be interactable.

## Available project layers

The project defines these relevant layers:

- `Terrain` - ground and terrain colliders used for height raycasts.
- `Interactable` - colliders that should receive interaction raycasts.
- `Water` - available for water-specific objects; no current script requires
  this layer.
- `UI` - available for interface elements.

Keep unrelated environment meshes on their normal layer unless they need to be
hit by one of the configured raycasts. After creating or importing a prefab or
environment, verify both the root object and any collider-bearing children in
the Inspector.
