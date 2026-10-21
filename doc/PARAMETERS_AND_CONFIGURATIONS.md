# Parameters and configurations

This is a list of the parameters and configuration options available in the project.

## Scenes

| Scene | Path | Purpose |
| --- | --- | --- |
| `MainMenu` | `Assets/Scenes/MainMenu.unity` | First scene (build index 0). Buttons: **Forest**, **Beach**, **Quit**. |
| `Forest` | `Assets/Scenes/Forest.unity` | Forest walk, collect mushrooms. |
| `Beach` | `Assets/Scenes/Beach.unity` | Beach walk, collect seashells. |

Old prototype scenes live in `Assets/Scenes/Legacy` and are not part of the build.
Scene names used in code are constants in `SceneLoader` (`MainMenuScene`, `ForestScene`, `BeachScene`).

## Script folders

| Folder | Scripts |
| --- | --- |
| `Scripts/Core` | `SceneLoader`, `DesktopInput` |
| `Scripts/UI` | `SceneMenu`, `CollectibleCounterUI` |
| `Scripts/Interaction` | `InteractionManager`, `Interactable`, `CollectibleWithEffect` |
| `Scripts/Collectibles` | `CollectibleSpawner`, `ObjectGrounding`, `CollectibleCounter` |
| `Scripts/Movement` | `TerrainFollowingScroller`, `DesktopTerrainFollowingScroller`, `CameraScroller` |
| `Scripts/Editor` | `AquatherapyProjectMigration` (one-time file renames, menu **Aquatherapy > Run Project Migration**) |

## SceneMenu settings

Add `SceneMenu` to the object that holds a menu canvas (`UI Manager` in every scene).

| Setting | Description |
| --- | --- |
| `Buttons` | List of `Button Name` → `Action` (`LoadScene`, `LoadMainMenu`, `RestartScene`, `Quit`) and `Scene Name` (for `LoadScene`). The button is found by GameObject name; Unity UI `Button` and `Toggle` based buttons are supported. |
| `Menu Button Returns To Main Menu` | The controller **Menu** button (left controller, or the hand-tracking menu gesture) or **Esc** loads the main menu. On in Forest and Beach. |
| `Follow Player` | Keeps the menu at its start offset from the player rig while the rig moves. On in Forest and Beach. |
| `Player` | Optional, defaults to the `OVRCameraRig`. |

## TerrainFollowingScroller settings

On the `Camera Rig` (OVRCameraRig root). Moves the player forward in the look direction.

| Setting | Default | Description |
| --- | ---: | --- |
| `Speed` | `1.5` | Forward speed in m/s. |
| `Height Offset` | `1.6` (scene) | Eye height above the terrain. |
| `Terrain Layer` | `Terrain` | Layer mask for the ground raycast. |
| `View Transform` | `CenterEyeAnchor` | The head; defaults to `Camera.main`. |
| `Stop At Terrain Edge` | on | Stops moving forward when there is no terrain ahead. |
| `Height Smoothing` | `8` | How fast the height follows the terrain (0 = snap). |

The Meta `Interaction Rig` locomotor has gravity disabled (`_gravityFactor = 0`) in all scenes
because the scroller controls the height; its character collider also collides with the `Terrain` layer.

## CollectibleSpawner settings

On the `Mushroom Spawner` / `Seashell Spawner` objects.

| Setting | Forest | Beach | Description |
| --- | ---: | ---: | --- |
| `Prefabs To Spawn` | 4 mushrooms | `Seashell` | Picked at random. Each needs `Interactable` + a collider on the `Interactable` layer. |
| `Spawn Interval` | `0.75` | `1` | Seconds between spawns. |
| `Spawn Ahead Distance` | `15` | `12` | Distance in front of the player. |
| `Despawn Behind Distance` | `10` | `10` | Objects this far behind are removed. |
| `Spawn Width` / `Spawn Depth` | `10` / `2` | `10` / `3` | Size of the random spawn area. |
| `Max Active Objects` | `30` | `25` | Cap on spawned objects alive at once (0 = unlimited). |
| `Scale Range` | `0.5–1.5` | `0.8–1.3` | Uniform multiplier of the prefab's own scale. |
| `Height Offset` | `0.1` | `0.05` | Offset above the terrain hit point. |
| `Terrain Layer` | `Terrain` | `Terrain` | The ground probe only hits this layer. Spawns without ground below are skipped. |

## InteractionManager settings

| Setting | Default | Description |
| --- | ---: | --- |
| `Interaction Layer` | `Interactable` | Only colliders on this layer can be targeted. |
| `Raycast Distance` | `100` | Maximum raycast length. |
| `Interaction Radius` | `15` (scenes) | Maximum distance between pointer origin and target. |
| `Use Controller Rays` | on | Aim with the Touch controllers (anchors found on the `OVRCameraRig`). |
| `Collect By Gaze Dwell` | on | Collect by looking at an object for `Dwell Time` seconds. |
| `Dwell Time` | `1.5` | Seconds of gaze needed. |
| `Allow Mouse` | on | Aim with the mouse cursor when no headset is active (Editor). |

Targeting order: controller ray → mouse cursor (no headset) → head gaze.
Collect input: index trigger or A/X on the controllers, index **pinch** with hand tracking,
left mouse click, or gaze dwell.

## Interactable settings

Add `Interactable` to the root of a prefab that can be collected.

| Setting | Default | Description |
| --- | --- | --- |
| `Collect Sound` | `Item Sparkle` | Played at the object's position when collected (mushrooms and seashells use the same clip). |
| `Collect Volume` | `1` | Volume of the collect sound. |
| `Highlight Color` | yellow | Tint of all child renderers while targeted (restored afterwards). |

Collection increments `CollectibleCounter` (reset on every scene load) and destroys the object.
The count is shown by `CollectibleCounterUI`, a small head-locked world-space label that appears
automatically in scenes with an `InteractionManager`.

### CollectibleWithEffect settings

Use instead of `Interactable` when collecting should also spawn an effect.

| Setting | Default | Description |
| --- | --- | --- |
| `Collect Effect` | None | Prefab instantiated at the object's position (was `sporeEffect`). |
| `Effect Lifetime` | `5` | Seconds until the effect is destroyed (0 = effect manages itself). |

## Collectible prefabs

| Prefab | Path |
| --- | --- |
| Mushrooms (4) | `Assets/AdventureForge/PreFab/Mushroom/...` |
| Seashell | `Assets/Prefabs/Collectibles/Seashell.prefab` (copy of the URP seaShell asset, scaled to ~30 cm, sphere collider for easy targeting) |
