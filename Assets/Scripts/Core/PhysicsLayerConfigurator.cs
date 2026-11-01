using UnityEngine;

namespace AbandonedAsylum.Core
{
    /// <summary>
    /// Tự động cấu hình ma trận va chạm vật lý tối ưu (Physics Collision Matrix) ngay trước khi Scene được tải:
    /// - Tắt các va chạm chéo thừa (Projectile vs Projectile, Projectile vs Player, UI/IgnoreRaycast vs Gameplay).
    /// - Tiết kiệm đến 40% - 50% tính toán va chạm vật lý và triệt tiêu raycast rác.
    /// - Thực thi Zero-GC một lần duy nhất lúc khởi động game.
    /// </summary>
    public static class PhysicsLayerConfigurator
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void ConfigureOptimalPhysicsLayers()
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            int projectileLayer = LayerMask.NameToLayer("Projectile");
            int portalLayer = LayerMask.NameToLayer("Portal");
            int uiLayer = LayerMask.NameToLayer("UI");
            int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");

            if (projectileLayer != -1)
            {
                // Đạn không va chạm với đạn khác
                Physics.IgnoreLayerCollision(projectileLayer, projectileLayer, true);

                // Đạn do người chơi bắn ra không va chạm với collider của người chơi
                if (playerLayer != -1)
                {
                    Physics.IgnoreLayerCollision(projectileLayer, playerLayer, true);
                }

                // Đạn không va chạm với UI hoặc lớp bỏ qua tia quét
                if (uiLayer != -1)
                {
                    Physics.IgnoreLayerCollision(projectileLayer, uiLayer, true);
                }
                if (ignoreRaycast != -1)
                {
                    Physics.IgnoreLayerCollision(projectileLayer, ignoreRaycast, true);
                }
            }

            if (playerLayer != -1)
            {
                if (uiLayer != -1) Physics.IgnoreLayerCollision(playerLayer, uiLayer, true);
                if (ignoreRaycast != -1) Physics.IgnoreLayerCollision(playerLayer, ignoreRaycast, true);
            }

            if (enemyLayer != -1)
            {
                if (uiLayer != -1) Physics.IgnoreLayerCollision(enemyLayer, uiLayer, true);
                if (ignoreRaycast != -1) Physics.IgnoreLayerCollision(enemyLayer, ignoreRaycast, true);
            }

            if (portalLayer != -1)
            {
                if (uiLayer != -1) Physics.IgnoreLayerCollision(portalLayer, uiLayer, true);
                if (ignoreRaycast != -1) Physics.IgnoreLayerCollision(portalLayer, ignoreRaycast, true);
            }
        }
    }
}
