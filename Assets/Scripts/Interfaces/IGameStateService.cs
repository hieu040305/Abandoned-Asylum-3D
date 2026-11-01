using System;

namespace AbandonedAsylum.Interfaces
{
    public enum GameState
    {
        MainMenu,
        Playing,
        GameOver,
        Victory
    }

    /// <summary>
    /// Contract cho Service điều phối trạng thái màn chơi và tiến độ nhiệm vụ.
    /// Giúp tách rời GameManager khỏi các module bên ngoài.
    /// </summary>
    public interface IGameStateService
    {
        GameState CurrentState { get; }
        int PortalsDestroyed { get; }
        int TargetPortalsToDestroy { get; }
        int ZombiesKilled { get; }
        float ElapsedSurvivalTime { get; }

        event Action<GameState> OnGameStateChanged;
        event Action<int, int> OnPortalProgressChanged;

        void StartMission();
        void RetryMission();
        void ReturnToMainMenu();
        void OnPortalDestroyed();
        void OnPlayerDied();
        void OnZombieKilled();
        void TriggerVictory();
    }
}
