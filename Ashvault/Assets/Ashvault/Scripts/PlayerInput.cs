using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Ashvault
{
    // Platform input is isolated here; no UI library dependency is required.
    public static class PlayerInput
    {
        public static Vector2 Movement()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Application.isMobilePlatform)
            {
                foreach (var touch in Touchscreen.current.touches)
                {
                    if (!touch.press.isPressed) continue;
                    Vector2 start = touch.startPosition.ReadValue();
                    if (start.x > Screen.width * 0.42f) continue;
                    Vector2 direction = (touch.position.ReadValue() - start) / 85f;
                    return Vector2.ClampMagnitude(direction, 1f);
                }
                return Vector2.zero;
            }
            var keyboard = Keyboard.current;
            if (keyboard == null) return Vector2.zero;
            float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0) -
                      (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
            float y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0) -
                      (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
#else
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
#endif
        }

        public static Vector2 Look()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Application.isMobilePlatform)
            {
                foreach (var touch in Touchscreen.current.touches)
                {
                    if (!touch.press.isPressed) continue;
                    Vector2 origin = touch.startPosition.ReadValue();
                    if (origin.x < Screen.width * 0.42f || origin.y < Screen.height * 0.45f) continue;
                    return touch.delta.ReadValue() * 0.11f;
                }
                return Vector2.zero;
            }
            return Mouse.current != null ? Mouse.current.delta.ReadValue() * 0.12f : Vector2.zero;
#else
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 2f;
#endif
        }

        public static bool Fire()
        {
#if ENABLE_INPUT_SYSTEM
            return !Application.isMobilePlatform && Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
            return !Application.isMobilePlatform && Input.GetMouseButton(0);
#endif
        }

        public static bool Interact()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.E);
#endif
        }

        public static bool SwapWeapon()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null &&
                (Keyboard.current.qKey.wasPressedThisFrame || Keyboard.current.digit1Key.wasPressedThisFrame ||
                Keyboard.current.digit2Key.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha2);
#endif
        }

        public static bool Pause()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }
    }
}
