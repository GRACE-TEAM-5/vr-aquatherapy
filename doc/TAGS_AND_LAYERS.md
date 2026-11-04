# Required Tags and Layers

Use the exact names below. Unity tags and layers are case-sensitive, and the
names must exist in **Edit > Project Settings > Tags and Layers**.

## Required assignments

| Object | Tag | Layer | Why |
| --- | --- | --- | --- |
| Main camera | `MainCamera` | Any appropriate camera layer | `Camera.main` is used by the spawner and interaction scripts. |
| Terrain or environment ground collider | `Terrain` | `Terrain` | `TerrainFollowingScroller`, `CollectibleSpawner`, and `ObjectGrounding` use a `LayerMask` to raycast against this layer. |
| Interactable prefab root and its collider | Usually no special tag | `Interactable` | `InteractionManager` only raycasts against the configured interaction layer. The collider must be on this layer so the object can be targeted. |

The project's `Terrain` layer is layer 3 and its `Interactable` layer is layer
6. Prefer selecting the names in the Unity Inspector rather than entering layer
numbers manually.

### Main camera

Select the camera used for gameplay and set **Tag** to `MainCamera`. There must
be one active gameplay camera with this tag. With the Meta `OVRCameraRig` this is
the `CenterEyeAnchor` only (not the rig root or the eye anchors).

### Environment and terrain

Set the GameObject that owns the terrain or ground collider to:

- **Tag:** `Terrain`
- **Layer:** `Terrain`

The tag identifies the object for humans and editor searches; the layer is what
the runtime raycasts actually use. If a ground collider is on a child object,
set the child collider’s layer to `Terrain` as well.

In each scene, set the `terrainLayer` field on `TerrainFollowingScroller`,
`CollectibleSpawner`, and `ObjectGrounding` (when present) to the `Terrain` layer.
Do not use *Everything*: the probe would hit the player or the collectibles.

Because the ground is on the custom `Terrain` layer, the Meta locomotor's character
controller (`Interaction Rig > Locomotor > PlayerController`) must include `Terrain`
in its layer mask, otherwise the player falls through the ground. This is set as a
prefab override in the Forest and Beach scenes.

### Interactable prefabs

For a prefab that can be collected or otherwise interacted with:

1. Add an `Interactable` or `CollectibleWithEffect` component to the prefab root.
2. Set the root to the `Interactable` layer.
3. Set every child GameObject containing a collider to the `Interactable`
   layer. `InteractionManager` raycasts the collider first and then searches
   its parent for the interactable component.

Set the `interactionLayer` field on `InteractionManager` to the `Interactable`
   layer. A prefab does not need a special tag for interaction.

## Available project tags

These tags are defined in `ProjectSettings/TagManager.asset`:

- `Terrain` - classification tag for environment ground.
- `Mushroom` - classification for mushroom collectibles.
- `Seashell` - classification for seashell collectibles.
- `Spawnable` - optional classification for assets intended to be spawned.
- `MainCamera` - Unity’s built-in camera tag and required on the gameplay camera.

`CollectibleSpawner` receives its prefab list through `prefabsToSpawn`; it does not
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
