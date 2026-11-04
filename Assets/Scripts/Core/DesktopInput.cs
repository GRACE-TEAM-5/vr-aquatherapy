using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Mouse / keyboard helpers for testing in the Editor or on desktop.
/// Works with either input backend (Project Settings > Player > Active Input Handling).
/// </summary>
public static class DesktopInput
{
    public static bool MousePresent
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.mousePresent;
#else
            return false;
#endif
        }
    }

    public static Vector2 MousePosition
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.mousePosition;
#else
            return Vector2.zero;
#endif
        }
    }

    public static Vector2 MouseDelta
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                return Mouse.current.delta.ReadValue() * 0.05f;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#else
            return Vector2.zero;
#endif
        }
    }

    public static bool LeftClickPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.leftButton.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    public static bool EscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
            return Keyboard.current.escapeKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
    }
}
