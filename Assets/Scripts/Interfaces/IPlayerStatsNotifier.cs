using System;

namespace AbandonedAsylum.Interfaces
{
    /// <summary>
    /// Contract giao tiếp dữ liệu trạng thái và sự kiện người chơi (Player Reactive Events).
    /// Đảm bảo phân tầng UI hoàn toàn tách rời (decoupled) khỏi implementation chi tiết của Player.
    /// Tuân thủ nguyên lý Dependency Inversion Principle (DIP).
    /// </summary>
    public interface IPlayerStatsNotifier
    {
        float CurrentHealth { get; }
        float MaxHealth { get; }
        int CurrentAmmo { get; }
        int MaxAmmo { get; }
        bool IsReloading { get; }
        string CurrentWeaponName { get; }

        event Action<float, float> OnHealthChanged;
        event Action<int, int> OnAmmoChanged;
        event Action<string> OnWeaponChanged;
        event Action<float, float, bool> OnReloadProgressChanged;
        event Action OnPlayerDied;
    }
}
