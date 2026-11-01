using UnityEngine;

namespace AbandonedAsylum.Interfaces
{
    /// <summary>
    /// Contract trừu tượng cho hệ thống điều khiển Input (Input Abstraction Layer).
    /// Đảm bảo logic gameplay hoàn toàn tách biệt khỏi Unity Input API cụ thể,
    /// cho phép hoán đổi mượt mà giữa Unity New Input System, Legacy Input, hoặc Mock Input.
    /// </summary>
    public interface IInputService
    {
        /// <summary>Vector di chuyển 2D (X: Ngang [-1, 1], Y: Dọc [-1, 1])</summary>
        Vector2 MoveInput { get; }

        /// <summary>Vector xoay camera / nhìn (X: Delta X, Y: Delta Y)</summary>
        Vector2 LookInput { get; }

        /// <summary>Trạng thái giữ phím chạy nhanh (Sprint / Shift)</summary>
        bool IsSprintPressed { get; }

        /// <summary>Trạng thái giữ nút ngắm bắn (Aim / Right Mouse Button)</summary>
        bool IsAimPressed { get; }

        /// <summary>Trạng thái giữ nút bắn liên thanh (Fire / Left Mouse Button)</summary>
        bool IsFirePressed { get; }

        /// <summary>Sự kiện vừa nhấn nút bắn trong frame hiện tại (Fire Down)</summary>
        bool IsFireDown { get; }

        /// <summary>Sự kiện vừa nhấn nút thay đạn (Reload / R)</summary>
        bool IsReloadPressed { get; }

        /// <summary>Sự kiện vừa nhấn nút tạm dừng menu (Pause / ESC)</summary>
        bool IsPausePressed { get; }

        /// <summary>Sự kiện vừa nhấn nút nhảy trong frame hiện tại (Jump / Space)</summary>
        bool IsJumpDown { get; }

        /// <summary>Sự kiện chọn vũ khí 1 (Phím số 1 / Rifle)</summary>
        bool IsWeapon1Pressed { get; }

        /// <summary>Sự kiện chọn vũ khí 2 (Phím số 2 / Shotgun)</summary>
        bool IsWeapon2Pressed { get; }

        /// <summary>Độ cuộn chuột chuyển đổi vũ khí (Scroll Wheel Delta Y)</summary>
        float ScrollWheelDelta { get; }

        /// <summary>Vị trí con trỏ chuột trên màn hình theo pixel (Screen Space)</summary>
        Vector2 MousePosition { get; }

        /// <summary>Trạng thái đang kích hoạt nhận input</summary>
        bool IsInputEnabled { get; }

        /// <summary>Bật nhận tín hiệu điều khiển</summary>
        void EnableInput();

        /// <summary>Tắt nhận tín hiệu điều khiển</summary>
        void DisableInput();
    }
}
