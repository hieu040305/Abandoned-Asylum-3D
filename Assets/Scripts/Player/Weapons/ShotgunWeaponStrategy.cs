using UnityEngine;
using AbandonedAsylum.VFX;

namespace AbandonedAsylum.Player.Weapons
{
    /// <summary>
    /// Chiến lược phát hỏa Súng Săn (Combat Shotgun Strategy):
    /// - Triển khai Open/Closed Principle (OCP) độc lập cùng IWeaponStrategy.
    /// - Bắn chùm 6 tia đạn phân tán hình nón (Cone Spread) theo thời gian thực.
    /// - Áp dụng Non-Alloc Raycast và kiểm tra va chạm O(1) root transform.
    /// - Phân bổ sát thương đồng đều theo từng viên pellet và sinh hiệu ứng Tracer riêng biệt.
    /// </summary>
    public class ShotgunWeaponStrategy : IWeaponStrategy
    {
        public string StrategyName => "Combat Pump/Tactical Shotgun";

        private const int PelletCount = 6;
        private const float SpreadAngle = 3.5f;
        private static readonly RaycastHit[] FallbackHitBuffer = new RaycastHit[8];

        public void ExecuteFire(in WeaponFireContext context)
        {
            Transform spawnPoint = context.BulletSpawnPoint;

            // 1. Hiệu ứng đầu nòng
            if (context.WeaponFX != null)
            {
                context.WeaponFX.PlayShootFX(spawnPoint);
            }

            Camera cam = context.Camera;
            Vector3 viewportCenter = context.IsCursorLocked
                ? new Vector3(0.5f, 0.5f, 0f)
                : (cam != null && Screen.width > 0 && Screen.height > 0
                    ? new Vector3(context.MouseScreenPosition.x / Screen.width, context.MouseScreenPosition.y / Screen.height, 0f)
                    : new Vector3(0.5f, 0.5f, 0f));

            Ray baseRay = cam != null
                ? cam.ViewportPointToRay(viewportCenter)
                : new Ray(context.DefaultOrigin, context.ForwardDirection);

            Transform playerT = context.PlayerTransform;
            Transform playerRoot = playerT != null ? playerT.root : null;
            Vector3 bulletOrigin = spawnPoint != null ? spawnPoint.position : context.DefaultOrigin;
            float damagePerPellet = Mathf.Max(1.0f, context.Damage / PelletCount);

            RaycastHit[] hitBuffer = context.HitBuffer ?? FallbackHitBuffer;

            // 2. Bắn từng viên pellet trong chùm
            for (int p = 0; p < PelletCount; p++)
            {
                // Độ lệch ngẫu nhiên theo hình nón
                Quaternion spreadRot = Quaternion.Euler(
                    Random.Range(-SpreadAngle, SpreadAngle),
                    Random.Range(-SpreadAngle, SpreadAngle),
                    0f
                );
                Vector3 pelletDir = spreadRot * baseRay.direction;
                Ray pelletRay = new Ray(baseRay.origin, pelletDir);

                int hitCount = Physics.RaycastNonAlloc(
                    pelletRay,
                    hitBuffer,
                    context.Range,
                    context.ShootableLayers,
                    QueryTriggerInteraction.Ignore
                );

                bool hasHit = false;
                RaycastHit validHit = default;
                float closestDist = float.MaxValue;

                for (int i = 0; i < hitCount; i++)
                {
                    RaycastHit h = hitBuffer[i];
                    if (playerRoot != null && h.transform != null && h.transform.root == playerRoot)
                    {
                        continue;
                    }

                    if (h.distance < closestDist)
                    {
                        closestDist = h.distance;
                        validHit = h;
                        hasHit = true;
                    }
                }

                Vector3 hitPoint;
                Vector3 hitNormal = Vector3.up;
                GameObject hitTarget = null;

                if (hasHit)
                {
                    hitPoint = validHit.point;
                    hitNormal = validHit.normal;
                    hitTarget = validHit.collider != null ? validHit.collider.gameObject : null;
                }
                else
                {
                    hitPoint = pelletRay.origin + pelletRay.direction * context.Range;
                }

                if (context.WeaponFX != null)
                {
                    context.WeaponFX.SpawnTracerBullet(
                        bulletOrigin,
                        hitPoint,
                        hitNormal,
                        hitTarget,
                        damagePerPellet,
                        context.BulletSpeed * Random.Range(0.92f, 1.05f)
                    );
                }
            }

            // 3. Độ giật mạnh đặc thù của Shotgun
            if (context.CameraController != null)
            {
                context.CameraController.AddRecoil(context.VerticalRecoil * 1.8f, context.HorizontalRecoil * 1.5f);
            }
        }
    }
}
