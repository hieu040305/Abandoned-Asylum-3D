using UnityEngine;
using AbandonedAsylum.Audio;

namespace AbandonedAsylum.Zombie
{
    /// <summary>
    /// Trạng thái Tấn Công (Attack State):
    /// - Quái vật đứng vững tại chỗ (planted feet, không trượt băng)
    /// - Xoay hướng đối diện Player và vung đòn cào toàn thân
    /// - Tự động quét hitbox trúng đòn theo nhịp animation hoặc auto-timer dự phòng
    /// </summary>
    public class ZombieAttackState : IZombieState
    {
        private readonly ZombieController zombie;
        private bool hasDealtDamageInCurrentAttack = false;
        private float attackStartTime = 0.0f;
        private const float AttackDuration = 1.5f;

        public bool HasDealtDamage => hasDealtDamageInCurrentAttack;

        public ZombieAttackState(ZombieController zombieController)
        {
            zombie = zombieController;
        }

        public void Enter()
        {
            hasDealtDamageInCurrentAttack = false;
            attackStartTime = Time.time;

            if (zombie.Agent != null && zombie.Agent.isOnNavMesh)
            {
                zombie.Agent.isStopped = true;
                zombie.Agent.velocity = Vector3.zero;
                zombie.Agent.updateRotation = false; // Tự điều khiển xoay slerp mượt mà
            }

            if (zombie.Anim != null)
            {
                zombie.Anim.SetTrigger(ZombieController.AnimHashAttack);
            }

            zombie.PlayAttackSound();
        }

        public void Update()
        {
            // Trong thời gian chuẩn bị vung đòn, xoay mặt chính diện về phía Player
            if (zombie.TargetPlayer != null)
            {
                Vector3 lookDir = (zombie.TargetPlayer.position - zombie.transform.position);
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDir);
                    zombie.transform.rotation = Quaternion.Slerp(zombie.transform.rotation, targetRot, Time.deltaTime * zombie.RotateSpeed);
                }
            }

            AnimatorStateInfo stateInfo = zombie.Anim != null 
                ? zombie.Anim.GetCurrentAnimatorStateInfo(0) 
                : default;
            bool isAttackAnimPlaying = stateInfo.IsName("Attack") || stateInfo.IsTag("Attack");

            float normalizedTime = isAttackAnimPlaying 
                ? (stateInfo.normalizedTime % 1.0f) 
                : ((Time.time - attackStartTime) / AttackDuration);

            // Tự động kiểm tra trúng đòn theo khung thời gian vung tay (Auto-Hit Fallback)
            if (!hasDealtDamageInCurrentAttack && normalizedTime >= zombie.AutoHitNormalizedTime && normalizedTime <= 0.70f)
            {
                PerformHitCheck("Auto-Timer");
            }

            // Kết thúc đòn đánh khi animation hoàn tất hoặc hết thời gian
            if (Time.time - attackStartTime >= AttackDuration || (isAttackAnimPlaying && stateInfo.normalizedTime >= 0.95f))
            {
                zombie.ChangeState(zombie.ChaseState);
            }
        }

        public void PerformHitCheck(string source)
        {
            if (hasDealtDamageInCurrentAttack) return;
            hasDealtDamageInCurrentAttack = true;
            zombie.PerformClawHitCheck(source);
        }

        public void Exit()
        {
            zombie.RecordAttackFinished();

            if (zombie.Agent != null && zombie.Agent.isOnNavMesh)
            {
                zombie.Agent.updateRotation = true;
                zombie.Agent.isStopped = false;
            }
        }
    }
}
