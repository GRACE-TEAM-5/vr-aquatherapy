using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Drives an in-scene menu canvas:
/// - wires UI buttons (found by GameObject name) to scene actions,
/// - optionally returns to the main menu with the controller Menu button / Esc,
/// - optionally keeps the menu next to the player while the rig moves.
/// Works with Unity UI <see cref="Button"/> and <see cref="Toggle"/> based buttons
/// (the Meta Interaction SDK UI set uses both).
/// </summary>
public class SceneMenu : MonoBehaviour
{
    public enum MenuAction
    {
        LoadScene,
        LoadMainMenu,
        RestartScene,
        Quit
    }

    [Serializable]
    public class ButtonBinding
    {
        [Tooltip("Name of the button GameObject (or one of its parents) in this scene.")]
        public string buttonName;
        public MenuAction action;
        [Tooltip("Only used by the LoadScene action.")]
        public string sceneName;
    }

    [Header("Buttons")]
    public ButtonBinding[] buttons = new ButtonBinding[0];

    [Header("Shortcuts")]
    [Tooltip("Pressing the controller Menu button (or Esc) loads the main menu.")]
    public bool menuButtonReturnsToMainMenu;

    [Header("Follow Player")]
    [Tooltip("Keep this menu at its initial offset from the player rig while the rig moves.")]
    public bool followPlayer;
    [Tooltip("Optional. Defaults to the OVRCameraRig in the scene.")]
    public Transform player;

    private Vector3 followOffset;
    private bool isLoading;

    private void Awake()
    {
        // Captured in Awake, before the scroller snaps the rig onto the terrain in Start.
        if (followPlayer)
        {
            ResolvePlayer();
            if (player != null)
                followOffset = transform.position - player.position;
        }
    }

    private void Start()
    {
        foreach (ButtonBinding binding in buttons)
        {
            if (binding == null || string.IsNullOrEmpty(binding.buttonName))
                continue;

            ButtonBinding captured = binding;
            Bind(captured.buttonName, () => Execute(captured));
        }
    }

    private void Update()
    {
        if (menuButtonReturnsToMainMenu && !isLoading && MenuButtonPressed())
        {
            isLoading = true;
            SceneLoader.Load(SceneLoader.MainMenuScene);
        }
    }

    private void LateUpdate()
    {
        if (followPlayer && player != null)
            transform.position = player.position + followOffset;
    }

    private void Execute(ButtonBinding binding)
    {
        if (isLoading)
            return;

        switch (binding.action)
        {
            case MenuAction.LoadScene:
                isLoading = true;
                SceneLoader.Load(binding.sceneName);
                break;
            case MenuAction.LoadMainMenu:
                isLoading = true;
                SceneLoader.Load(SceneLoader.MainMenuScene);
                break;
            case MenuAction.RestartScene:
                isLoading = true;
                SceneLoader.Restart();
                break;
            case MenuAction.Quit:
                SceneLoader.Quit();
                break;
        }
    }

    private void Bind(string objectName, UnityAction action)
    {
        GameObject target = FindInScene(objectName);
        if (target == null)
        {
            Debug.LogWarning($"SceneMenu: no GameObject named '{objectName}' found in scene '{gameObject.scene.name}'.", this);
            return;
        }

        Button button = target.GetComponentInChildren<Button>(true);
        if (button != null)
        {
            button.onClick.AddListener(action);
            return;
        }

        Toggle toggle = target.GetComponentInChildren<Toggle>(true);
        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(_ => action());
            return;
        }

        Debug.LogWarning($"SceneMenu: '{objectName}' has no Button or Toggle component.", target);
    }

    private GameObject FindInScene(string objectName)
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == objectName)
                    return t.gameObject;
            }
        }
        return null;
    }

    private void ResolvePlayer()
    {
        if (player != null)
            return;

        OVRCameraRig rig = FindAnyObjectByType<OVRCameraRig>();
        if (rig != null)
            player = rig.transform;
        else if (Camera.main != null)
            player = Camera.main.transform.root;
    }

    private static bool MenuButtonPressed()
    {
        if (OVRInput.GetDown(OVRInput.Button.Start, OVRInput.Controller.All))
            return true;

        return DesktopInput.EscapePressed();
    }
}
