namespace AbandonedAsylum.Zombie
{
    /// <summary>
    /// Giao diện chuẩn cho các trạng thái hành vi của Quái vật Zombie (State Pattern).
    /// Tuân thủ Single Responsibility và Open/Closed Principle (OCP).
    /// </summary>
    public interface IZombieState
    {
        void Enter();
        void Update();
        void Exit();
    }
}
