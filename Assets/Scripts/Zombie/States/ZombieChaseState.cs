using UnityEngine;
using AbandonedAsylum.Audio;

namespace AbandonedAsylum.Zombie
{
    /// <summary>
    /// Trạng thái Săn Đuổi (Chase State):
    /// - Quái vật lao về phía Player khắp toàn bộ map bằng NavMesh 3 tầng dự phòng
    /// - Tự động phát hiện và gỡ kẹt ngưỡng cửa (Unstuck)
    /// - Khi tiếp cận đủ cự ly tấn công và hết hồi chiêu, chuyển sang Attack State
    /// </summary>
    public class ZombieChaseState : IZombieState
    {
        private readonly ZombieController zombie;
        private float nextDestinationUpdateTime = 0.0f;
        private float nextGrowlTime = 0.0f;

        public ZombieChaseState(ZombieController zombieController)
        {
            zombie = zombieController;
        }

        public void Enter()
        {
            if (zombie.Agent != null && zombie.Agent.isOnNavMesh)
            {
                zombie.Agent.updateRotation = true;
                zombie.Agent.isStopped = false;
            }
            zombie.EnsureAgentOnNavMesh();
            zombie.UpdateChasePath();
            nextGrowlTime = Time.time + Random.Range(2.5f, 6.0f);
        }

        public void Update()
        {
            if (zombie.TargetPlayer == null)
            {
                zombie.FindPlayerTarget();
                if (zombie.TargetPlayer == null) return;
            }

            float sqrDistToPlayer = zombie.SqrDistanceToPlayer;
            float attackRange = zombie.AttackRange;

            // Kiểm tra chuyển trạng thái tấn công (Zero-Sqrt Hot Loop)
            if (sqrDistToPlayer <= (attackRange * attackRange) && zombie.CanAttack)
            {
                zombie.ChangeState(zombie.AttackState);
                return;
            }

            // Âm thanh gầm gừ định kỳ
            if (Time.time >= nextGrowlTime)
            {
                nextGrowlTime = Time.time + Random.Range(4.5f, 8.5f);
                zombie.PlayGrowlSound();
            }

            zombie.EnsureAgentOnNavMesh();

            // Cập nhật đường đi định kỳ (staggered 0.32s - 0.40s) tối ưu CPU
            if (Time.time >= nextDestinationUpdateTime)
            {
                nextDestinationUpdateTime = Time.time + Random.Range(0.32f, 0.40f);
                zombie.UpdateChasePath();
            }

            // Giải thuật chống kẹt cửa / vượt qua đứt đoạn NavMesh
            zombie.HandleUnstuckMechanic();
        }

        public void Exit()
        {
        }
    }
}
