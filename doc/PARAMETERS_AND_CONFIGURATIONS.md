# Parameters and configurations

This is a list of the parameters and configuration options available in the project.

## ObjectSpawner settings

Add `ObjectSpawner` to the scene object responsible for spawning collectibles.
The component exposes the following Inspector settings:

| Setting | Default | Description |
| --- | ---: | --- |
| `Prefabs To Spawn` | Empty | Prefabs selected randomly for each spawn. Add every prefab that may appear; the `Spawnable` tag is not read by the script. |
| `Spawn Interval` | `0.5` seconds | Time between spawn attempts. Lower values create objects more frequently. |
| `Spawn Ahead Distance` | `10` | Distance in front of the main camera where the spawn position starts. |
| `Despawn Behind Distance` | `10` | How far behind the camera an object can travel before it is destroyed. |
| `Spawn Width` | `10` | Width of the random spawn area across the camera's horizontal direction. |
| `Height Offset` | `0.1` | Vertical offset above the terrain hit point, useful for preventing clipping. |
| `Terrain Layer` | Empty | Layer mask used by the downward grounding raycast. Set this to `Terrain`. |

At runtime, each spawned object receives a random uniform-independent scale
between `0.5` and `1.5` on each axis and a random Y rotation. The object is
grounded only when the raycast hits the configured terrain layer. Make sure
every prefab in `Prefabs To Spawn` has the required collider, interaction
component, and `Interactable` layer if it should be collectable.

## InteractionManager settings

Add `InteractionManager` to a scene manager object. It requires an active
camera tagged `MainCamera` and exposes:

| Setting | Default | Description |
| --- | ---: | --- |
| `Interaction Layer` | Empty | Layer mask for the interaction raycast. Set this to `Interactable`; colliders on other layers cannot be targeted. |
| `Raycast Distance` | `100` | Maximum distance of the raycast from the camera. |
| `Interaction Radius` | `5` | Maximum distance from the camera to the hit point for a target to be accepted. |

When mouse movement or a mouse button is detected, the manager raycasts from
the mouse position. Otherwise it raycasts forward from the camera, which
supports headset/controller use. A left mouse click, controller trigger, or
controller primary button performs the interaction. Hover enter and exit are
called as the target changes.

The raycast hits a collider first and then looks for `Interactable` on that
object or one of its parents. This is why collider-bearing children must use
the `Interactable` layer even when the component is on the prefab root.

## Interactable settings

Add `Interactable` to the root of a prefab that can be targeted and collected.
Its public Inspector setting is:

| Setting | Default | Description |
| --- | --- | --- |
| `Collect Sound` | None | Optional `AudioClip` played at the object's position when it is collected. |

`Interactable` highlights the root object's `Renderer` yellow on hover and
restores it to white when the hover ends. If the renderer is on a child,
implement a custom interactable class or place the renderer on the same object
as the component. Collection increments the global collected-prefab counter and
then destroys the object. An already-collected object cannot be collected
again.

### ObjectBehaviour settings

Use `ObjectBehaviour` instead of the base `Interactable` when collecting the
object should also create a visual effect. It inherits `Collect Sound` and
adds:

| Setting | Default | Description |
| --- | --- | --- |
| `Spore Effect` | None | Optional prefab instantiated at the collected object's position before the object is destroyed. |

The effect prefab is spawned without inheriting the collected object's rotation
or scale. Ensure the effect prefab manages its own lifetime if it should
disappear after playback.
