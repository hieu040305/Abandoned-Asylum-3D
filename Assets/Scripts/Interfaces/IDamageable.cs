using UnityEngine;

namespace AbandonedAsylum.Interfaces
{
    /// <summary>
    /// Định danh loại bề mặt va chạm để kích hoạt âm thanh và hiệu ứng tương ứng.
    /// </summary>
    public enum ImpactSoundType
    {
        Flesh,
        Portal,
        Concrete,
        Default
    }

    /// <summary>
    /// Giao diện chuẩn cho mọi thực thể có thể nhận sát thương (Player, Zombie, Cổng không gian, v.v.).
    /// Phục vụ nguyên lý Dependency Inversion (DIP) và Open/Closed (OCP) trong kiến trúc hướng đối tượng.
    /// </summary>
    public interface IDamageable
    {
        float CurrentHealth { get; }
        float MaxHealth { get; }
        bool IsDead { get; }
        ImpactSoundType ImpactType { get; }

        void TakeDamage(float damageAmount);
        void TakeDamage(float damageAmount, Vector3 hitPoint, Vector3 hitNormal);
    }
}
