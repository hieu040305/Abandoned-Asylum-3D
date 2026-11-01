using AbandonedAsylum.Core;
using UnityEngine;

namespace AbandonedAsylum.Zombie
{
    /// <summary>
    /// Trạng thái Tử Trận (Death State):
    /// - Vô hiệu hóa NavMeshAgent và giải phóng quyền kiểm soát vật lý.
    /// - Bám sát mặt sàn bằng Raycast chống lơ lửng.
    /// - Hạ trọng tâm và chuyển Collider sang Trigger dẹt sát sàn chống vướng chân người chơi khi ngã.
    /// - Kích hoạt hoạt ảnh tử trận (Death animation) ngã xuống sàn.
    /// - Trong những khoảnh khắc cuối sau khi ngã chạm sàn, xác chìm nhẹ dần và ĐƯỢC HỦY HOÀN TOÀN
    ///   (Object.Destroy) để biến mất triệt để, giải phóng bộ nhớ và dọn sạch đấu trường.
    /// </summary>
    public class ZombieDeathState : IZombieState
    {
        private readonly ZombieController zombie;
        private float deathTimer = 0.0f;
        private float animationDuration = 3.2f;
        private bool isDestroyed = false;

        public ZombieDeathState(ZombieController zombieController)
        {
            zombie = zombieController;
        }

        public void Enter()
        {
            deathTimer = 0.0f;
            isDestroyed = false;
            animationDuration = Mathf.Max(2.5f, zombie.DeathAnimationDuration);

            zombie.StopAllCoroutines();
            GameManager.Instance?.OnZombieKilled();

            // Thông báo Pool ngay khi tử trận để giảm số lượng quái đang sống, cho phép tiếp tục spawn đợt mới
            if (ZombiePool.Instance != null)
            {
                ZombiePool.Instance.NotifyZombieDestroyed(zombie);
            }

            // 1. Dừng và vô hiệu hóa NavMeshAgent
            if (zombie.Agent != null)
            {
                if (zombie.Agent.isOnNavMesh)
                {
                    zombie.Agent.isStopped = true;
                    zombie.Agent.velocity = Vector3.zero;
                }
                zombie.Agent.enabled = false;
            }

            // 2. Bám sát sàn nhà tuyệt đối (chống lơ lửng)
            if (Physics.Raycast(zombie.transform.position + Vector3.up * 1.0f, Vector3.down, out RaycastHit groundHit, 5.0f))
            {
                zombie.transform.position = groundHit.point;
            }

            // 3. Giữ Rigidbody Kinematic và chuyển Collider thành Trigger dẹt sát sàn
            if (zombie.ZombieCollider != null)
            {
                zombie.ZombieCollider.isTrigger = true;
                zombie.ZombieCollider.center = new Vector3(0f, 0.15f, 0f);
                zombie.ZombieCollider.height = 0.3f;
                zombie.ZombieCollider.radius = 0.25f;
            }

            if (zombie.Rb != null)
            {
                zombie.Rb.isKinematic = true;
                zombie.Rb.useGravity = false;
            }

            // 4. Kích hoạt hoạt ảnh chết tức khắc
            if (zombie.Anim != null)
            {
                zombie.Anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                zombie.Anim.applyRootMotion = false;
                zombie.Anim.ResetTrigger(ZombieController.AnimHashAttack);
                zombie.Anim.ResetTrigger(ZombieController.AnimHashDie);
                zombie.Anim.SetBool(ZombieController.AnimHashIsAttacking, false);
                zombie.Anim.SetBool(ZombieController.AnimHashIsDead, true);
                zombie.Anim.SetFloat(ZombieController.AnimHashSpeed, 0f);

                zombie.Anim.CrossFadeInFixedTime(ZombieController.AnimHashDeath, 0.08f, 0);
                zombie.Anim.SetTrigger(ZombieController.AnimHashDie);
            }

            zombie.PlayDeathSound();
        }

        public void Update()
        {
            if (isDestroyed) return;

            deathTimer += Time.deltaTime;

            // Trong 0.8 giây cuối sau khi quái đã ngã chạm sàn hoàn toàn: Chìm nhẹ dần xuống sàn tạo hiệu ứng biến mất mượt mà
            if (deathTimer > (animationDuration - 0.8f))
            {
                if (zombie != null && zombie.transform != null)
                {
                    zombie.transform.position += Vector3.down * (Time.deltaTime * 0.75f);
                }
            }

            // Khi hoạt ảnh tử trận hoàn tất: THU HỒI HOÀN TOÀN về Object Pool (Zero GC Heap Allocation)
            if (deathTimer >= animationDuration)
            {
                isDestroyed = true;

                if (zombie != null && zombie.gameObject != null)
                {
                    if (ZombiePool.Instance != null)
                    {
                        ZombiePool.Instance.ReleaseZombie(zombie);
                    }
                    else
                    {
                        zombie.gameObject.SetActive(false);
                    }
                }
            }
        }

        public void Exit()
        {
        }
    }
}
