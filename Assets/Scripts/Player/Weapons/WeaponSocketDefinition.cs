using UnityEngine;

namespace AbandonedAsylum.Player.Weapons
{
    /// <summary>
    /// Định nghĩa các socket gắn kết của vũ khí (Muzzle, Aim Reference, Left Hand Grip IK).
    /// Loại bỏ hoàn toàn việc tìm kiếm node phân cấp bằng chuỗi đệ quy (FindDeepChild).
    /// </summary>
    [DisallowMultipleComponent]
    public class WeaponSocketDefinition : MonoBehaviour
    {
        [Header("Muzzle & Projectile Trajectory")]
        [Tooltip("Điểm nòng súng phát hỏa và sinh đạn vạch đường Tracer")]
        [SerializeField] private Transform muzzlePoint;

        [Tooltip("Điểm tham chiếu hướng ngắm của nòng súng")]
        [SerializeField] private Transform aimReferencePoint;

        [Header("Inverse Kinematics (IK) Targets")]
        [Tooltip("Điểm kẹp tay trái vào ốp lót tay của súng (Animation Rigging Two-Bone IK)")]
        [SerializeField] private Transform leftHandGripIK;

        public Transform MuzzlePoint => muzzlePoint != null ? muzzlePoint : transform;
        public Transform AimReferencePoint => aimReferencePoint != null ? aimReferencePoint : MuzzlePoint;
        public Transform LeftHandGripIK => leftHandGripIK;

        public void SetSockets(Transform muzzle, Transform aim, Transform grip = null)
        {
            muzzlePoint = muzzle;
            aimReferencePoint = aim;
            leftHandGripIK = grip;
        }

        private void Reset()
        {
            // Tự động gán fallback ngay khi AddComponent trong Editor
            if (muzzlePoint == null)
            {
                Transform child = transform.Find("shootp (1)") ?? transform.Find("shootpoint") ?? transform.Find("Muzzle");
                muzzlePoint = child != null ? child : transform;
            }
            if (aimReferencePoint == null)
            {
                aimReferencePoint = muzzlePoint;
            }
        }
    }
}
