using UnityEngine;
using AbandonedAsylum.Interfaces;

namespace AbandonedAsylum.Core.Input
{
    /// <summary>
    /// Triển khai IInputService sử dụng Unity Legacy Input Manager làm Fallback Strategy.
    /// Hoạt động 100% độc lập, không phụ thuộc package ngoài.
    /// </summary>
    public class LegacyInputService : IInputService
    {
        private bool isEnabled = true;

        public bool IsInputEnabled => isEnabled;

        public Vector2 MoveInput
        {
            get
            {
                if (!isEnabled) return Vector2.zero;
                return new Vector2(
                    UnityEngine.Input.GetAxisRaw("Horizontal"),
                    UnityEngine.Input.GetAxisRaw("Vertical")
                ).normalized;
            }
        }

        public Vector2 LookInput
        {
            get
            {
                if (!isEnabled) return Vector2.zero;
                return new Vector2(
                    UnityEngine.Input.GetAxis("Mouse X"),
                    UnityEngine.Input.GetAxis("Mouse Y")
                );
            }
        }

        public bool IsSprintPressed => isEnabled && UnityEngine.Input.GetKey(KeyCode.LeftShift);

        public bool IsAimPressed => isEnabled && UnityEngine.Input.GetMouseButton(1);

        public bool IsFirePressed => isEnabled && UnityEngine.Input.GetMouseButton(0);

        public bool IsFireDown => isEnabled && UnityEngine.Input.GetMouseButtonDown(0);

        public bool IsReloadPressed => isEnabled && UnityEngine.Input.GetKeyDown(KeyCode.R);

        public bool IsPausePressed => UnityEngine.Input.GetKeyDown(KeyCode.Escape);

        public bool IsJumpDown => isEnabled && (UnityEngine.Input.GetButtonDown("Jump") || UnityEngine.Input.GetKeyDown(KeyCode.Space));

        public bool IsWeapon1Pressed => isEnabled && (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1) || UnityEngine.Input.GetKeyDown(KeyCode.Keypad1));

        public bool IsWeapon2Pressed => isEnabled && (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2) || UnityEngine.Input.GetKeyDown(KeyCode.Keypad2));

        public float ScrollWheelDelta => isEnabled ? UnityEngine.Input.mouseScrollDelta.y : 0f;

        public Vector2 MousePosition => (Vector2)UnityEngine.Input.mousePosition;

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
