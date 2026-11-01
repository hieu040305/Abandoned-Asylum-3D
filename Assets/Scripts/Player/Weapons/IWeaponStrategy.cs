using UnityEngine;
using AbandonedAsylum.VFX;

namespace AbandonedAsylum.Player.Weapons
{
    /// <summary>
    /// Đóng gói toàn bộ bối cảnh dữ liệu (Execution Context) cần thiết cho một phát bắn của vũ khí.
    /// Giúp thuật toán phát hỏa hoàn toàn tách biệt (decoupled) khỏi MonoBehaviour PlayerWeaponController.
    /// </summary>
    public struct WeaponFireContext
    {
        public Transform BulletSpawnPoint;
        public Vector3 DefaultOrigin;
        public Vector3 ForwardDirection;
        public Camera Camera;
        public Vector2 MouseScreenPosition;
        public bool IsCursorLocked;
        public LayerMask ShootableLayers;
        public float Range;
        public float Damage;
        public float BulletSpeed;
        public RaycastHit[] HitBuffer;
        public WeaponFX WeaponFX;
        public PlayerCameraController CameraController;
        public float VerticalRecoil;
        public float HorizontalRecoil;
        public Transform PlayerTransform;
    }

    /// <summary>
    /// Strategy Pattern Contract cho hệ thống Vũ khí (Open/Closed Principle - OCP):
    /// Cho phép mở rộng thêm bất kỳ loại vũ khí nào trong tương lai
    /// mà không cần can thiệp hay sửa đổi mã nguồn của PlayerWeaponController.
    /// </summary>
    public interface IWeaponStrategy
    {
        /// <summary>Tên định danh của chiến lược phát hỏa</summary>
        string StrategyName { get; }

        /// <summary>Thực thi thuật toán phát hỏa của vũ khí</summary>
        void ExecuteFire(in WeaponFireContext context);
    }
}
