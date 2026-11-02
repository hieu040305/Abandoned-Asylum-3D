using UnityEngine;
using AbandonedAsylum.VFX;

namespace AbandonedAsylum.Player.Weapons
{
    /// <summary>
    /// Chiến lược phát hỏa dành riêng cho Súng Trường Tự Động (Automatic Rifle / Kalashnicolt hiện tại):
    /// - Tính toán tia bắn Raycast Non-Alloc chính xác từ tâm màn hình Viewport (hoặc con trỏ chuột).
    /// - Lọc bỏ va chạm với bản thân nhân vật Player để tránh tự bắn trúng mình.
    /// - Sinh đường đạn bay siêu thanh (Tracer Bullet VFX) và gây sát thương vật lý/máu IDamageable.
    /// - Truyền lực giật dọc/ngang (Procedural Recoil) mượt mà lên PlayerCameraController.
    /// </summary>
    public class RifleWeaponStrategy : IWeaponStrategy
    {
        public string StrategyName => "Assault Rifle";
        private static readonly RaycastHit[] FallbackHitBuffer = new RaycastHit[8];

        public void ExecuteFire(in WeaponFireContext context)
        {
            Transform spawnPoint = context.BulletSpawnPoint;

            // 1. Kích hoạt hiệu ứng phát hỏa tại đầu nòng (Muzzle Flash & Eject Shell)
            if (context.WeaponFX != null)
            {
                context.WeaponFX.PlayShootFX(spawnPoint);
            }

            // 2. Xác định tâm ngắm trên Viewport màn hình
            Camera cam = context.Camera;
            Vector3 viewportCenter;

            if (context.IsCursorLocked)
            {
                viewportCenter = new Vector3(0.5f, 0.5f, 0f);
            }
            else if (cam != null && Screen.width > 0 && Screen.height > 0)
            {
                viewportCenter = new Vector3(
                    context.MouseScreenPosition.x / Screen.width,
                    context.MouseScreenPosition.y / Screen.height,
                    0f
                );
            }
            else
            {
                viewportCenter = new Vector3(0.5f, 0.5f, 0f);
            }

            // 3. Khởi tạo tia bắn và tính toán va chạm vật lý RaycastNonAlloc
            Ray ray = cam != null
                ? cam.ViewportPointToRay(viewportCenter)
                : new Ray(context.DefaultOrigin, context.ForwardDirection);

            Vector3 hitPoint;
            Vector3 hitNormal = Vector3.up;
            GameObject hitTarget = null;

            RaycastHit[] hitBuffer = context.HitBuffer ?? FallbackHitBuffer;
            int hitCount = Physics.RaycastNonAlloc(
                ray,
                hitBuffer,
                context.Range,
                context.ShootableLayers,
                QueryTriggerInteraction.Ignore
            );

            bool hasHit = false;
            RaycastHit validHit = default;
            float closestDist = float.MaxValue;
            Transform playerT = context.PlayerTransform;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit h = hitBuffer[i];
                if (playerT != null && (h.transform == playerT || h.transform.root == playerT.root))
                {
                    continue; // Bỏ qua bản thân người chơi (O(1) root comparison)
                }

                if (h.distance < closestDist)
                {
                    closestDist = h.distance;
                    validHit = h;
                    hasHit = true;
                }
            }

            if (hasHit)
            {
                hitPoint = validHit.point;
                hitNormal = validHit.normal;
                hitTarget = validHit.collider != null ? validHit.collider.gameObject : null;
            }
            else
            {
                hitPoint = ray.origin + ray.direction * context.Range;
            }

            // 4. Sinh viên đạn Tracer bay và gây sát thương IDamageable
            Vector3 bulletOrigin = spawnPoint != null ? spawnPoint.position : context.DefaultOrigin;
            if (context.WeaponFX != null)
            {
                context.WeaponFX.SpawnTracerBullet(
                    bulletOrigin,
                    hitPoint,
                    hitNormal,
                    hitTarget,
                    context.Damage,
                    context.BulletSpeed
                );
            }

            // 5. Áp lực giật lên Camera
            if (context.CameraController != null)
            {
                context.CameraController.AddRecoil(context.VerticalRecoil, context.HorizontalRecoil);
            }
        }
    }
}
