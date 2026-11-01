using UnityEngine;
using AbandonedAsylum.Interfaces;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AbandonedAsylum.Core.Input
{
    /// <summary>
    /// Dịch vụ xử lý tín hiệu điều khiển sử dụng Unity New Input System,
    /// hỗ trợ tự động dự phòng sang Legacy Input khi cần thiết và hỗ trợ cả Gamepad.
    /// </summary>
    public class NewInputSystemService : IInputService
    {
        private bool isEnabled = true;
        public bool IsInputEnabled => isEnabled;

        private static bool isLegacyInputAvailable = true;
        private static bool isLegacyChecked = false;

        public NewInputSystemService()
        {
            CheckLegacyInputAvailability();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            isLegacyInputAvailable = true;
            isLegacyChecked = false;
        }

        private static void CheckLegacyInputAvailability()
        {
            if (isLegacyChecked) return;
            isLegacyChecked = true;
            try
            {
                _ = UnityEngine.Input.mousePosition;
                isLegacyInputAvailable = true;
            }
            catch
            {
                isLegacyInputAvailable = false;
            }
        }

        public Vector2 MoveInput
        {
            get
            {
                if (!isEnabled) return Vector2.zero;

                Vector2 move = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    float x = 0f;
                    float y = 0f;
                    if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
                    if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
                    if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;
                    if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
                    move = new Vector2(x, y);
                }

                if (move.sqrMagnitude < 0.01f)
                {
                    var gamepad = Gamepad.current;
                    if (gamepad != null)
                    {
                        move = gamepad.leftStick.ReadValue();
                    }
                }
#endif

                if (move.sqrMagnitude < 0.01f && isLegacyInputAvailable)
                {
                    move = new Vector2(
                        UnityEngine.Input.GetAxisRaw("Horizontal"),
                        UnityEngine.Input.GetAxisRaw("Vertical")
                    );
                }

                return move.sqrMagnitude > 1.0f ? move.normalized : move;
            }
        }

        public Vector2 LookInput
        {
            get
            {
                if (!isEnabled) return Vector2.zero;

                Vector2 delta = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    delta = mouse.delta.ReadValue() * 0.1f;
                }

                if (delta.sqrMagnitude < 0.0001f)
                {
                    var gamepad = Gamepad.current;
                    if (gamepad != null)
                    {
                        delta = gamepad.rightStick.ReadValue() * (Time.deltaTime * 120.0f);
                    }
                }
#endif

                if (delta.sqrMagnitude < 0.0001f && isLegacyInputAvailable)
                {
                    delta = new Vector2(
                        UnityEngine.Input.GetAxis("Mouse X"),
                        UnityEngine.Input.GetAxis("Mouse Y")
                    );
                }

                return delta;
            }
        }

        public bool IsSprintPressed
        {
            get
            {
                if (!isEnabled) return false;

#if ENABLE_INPUT_SYSTEM
                var kb = Keyboard.current;
                if (kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed)) return true;
                var gp = Gamepad.current;
                if (gp != null && gp.leftStickButton.isPressed) return true;
#endif
                if (isLegacyInputAvailable)
                {
                    return UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift);
                }

                return false;
            }
        }

        public bool IsAimPressed
        {
            get
            {
                if (!isEnabled) return false;

#if ENABLE_INPUT_SYSTEM
                var mouse = Mouse.current;
                if (mouse != null && mouse.rightButton.isPressed) return true;
                var gp = Gamepad.current;
                if (gp != null && gp.leftTrigger.isPressed) return true;
#endif
                if (isLegacyInputAvailable)
                {
                    return UnityEngine.Input.GetMouseButton(1);
                }

                return false;
            }
        }

        public bool IsFirePressed
        {
            get
            {
                if (!isEnabled) return false;

#if ENABLE_INPUT_SYSTEM
                var mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.isPressed) return true;
                var gp = Gamepad.current;
                if (gp != null && gp.rightTrigger.isPressed) return true;
#endif
                if (isLegacyInputAvailable)
                {
                    return UnityEngine.Input.GetMouseButton(0);
                }

                return false;
            }
        }

        public bool IsFireDown
        {
            get
            {
                if (!isEnabled) return false;

#if ENABLE_INPUT_SYSTEM
                var mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;
                var gp = Gamepad.current;
                if (gp != null && gp.rightTrigger.wasPressedThisFrame) return true;
#endif
                if (isLegacyInputAvailable)
                {
                    return UnityEngine.Input.GetMouseButtonDown(0);
                }

                return false;
            }
        }

        public bool IsReloadPressed
        {
            get
            {
                if (!isEnabled) return false;

#if ENABLE_INPUT_SYSTEM
                var kb = Keyboard.current;
                if (kb != null && kb.rKey.wasPressedThisFrame) return true;
                var gp = Gamepad.current;
                if (gp != null && gp.xButton.wasPressedThisFrame) return true;
#endif
                if (isLegacyInputAvailable)
                {
                    return UnityEngine.Input.GetKeyDown(KeyCode.R);
                }

                return false;
            }
        }

        public bool IsPausePressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var kb = Keyboard.current;
                if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;
                var gp = Gamepad.current;
                if (gp != null && gp.startButton.wasPressedThisFrame) return true;
#endif
                if (isLegacyInputAvailable)
                {
                    return UnityEngine.Input.GetKeyDown(KeyCode.Escape);
                }

                return false;
            }
        }

        public bool IsJumpDown
        {
            get
            {
                if (!isEnabled) return false;

#if ENABLE_INPUT_SYSTEM
                var kb = Keyboard.current;
                if (kb != null && kb.spaceKey.wasPressedThisFrame) return true;
                var gp = Gamepad.current;
                if (gp != null && gp.buttonSouth.wasPressedThisFrame) return true;
#endif
                if (isLegacyInputAvailable)
                {
                    return UnityEngine.Input.GetButtonDown("Jump") || UnityEngine.Input.GetKeyDown(KeyCode.Space);
                }

                return false;
            }
        }

        public bool IsWeapon1Pressed
        {
            get
            {
                if (!isEnabled) return false;
#if ENABLE_INPUT_SYSTEM
                var kb = Keyboard.current;
                if (kb != null && (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)) return true;
#endif
                if (isLegacyInputAvailable)
                {
                    return UnityEngine.Input.GetKeyDown(KeyCode.Alpha1) || UnityEngine.Input.GetKeyDown(KeyCode.Keypad1);
                }
                return false;
            }
        }

        public bool IsWeapon2Pressed
        {
            get
            {
                if (!isEnabled) return false;
#if ENABLE_INPUT_SYSTEM
                var kb = Keyboard.current;
                if (kb != null && (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)) return true;
#endif
                if (isLegacyInputAvailable)
                {
                    return UnityEngine.Input.GetKeyDown(KeyCode.Alpha2) || UnityEngine.Input.GetKeyDown(KeyCode.Keypad2);
                }
                return false;
            }
        }

        public float ScrollWheelDelta
        {
            get
            {
                if (!isEnabled) return 0f;
#if ENABLE_INPUT_SYSTEM
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    float y = mouse.scroll.ReadValue().y;
                    if (Mathf.Abs(y) > 0.01f) return y;
                }
#endif
                if (isLegacyInputAvailable)
                {
                    return UnityEngine.Input.mouseScrollDelta.y;
                }
                return 0f;
            }
        }

        public Vector2 MousePosition
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var mouse = Mouse.current;
                if (mouse != null) return mouse.position.ReadValue();
#endif
                if (isLegacyInputAvailable)
                {
                    return (Vector2)UnityEngine.Input.mousePosition;
                }

                return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            }
        }

        public void EnableInput()
        {
            isEnabled = true;
        }

        public void DisableInput()
        {
            isEnabled = false;
        }
    }
}
