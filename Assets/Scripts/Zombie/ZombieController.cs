using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using AbandonedAsylum.Data;
using AbandonedAsylum.VFX;
using AbandonedAsylum.Audio;
using AbandonedAsylum.Core;
using AbandonedAsylum.Interfaces;

namespace AbandonedAsylum.Zombie
{
    /// <summary>
    /// Điều khiển hành vi của quái vật Zombie Parasite:
    /// Áp dụng State Pattern (ChaseState, AttackState, DeathState) để điều phối
    /// các trạng thái bám đuổi, tấn công cận chiến và tử trận.
    /// </summary>
    public class ZombieController : BaseZombieController
    {
        [Header("Claw Attack (Tấn Công Cào Bằng Tay)")]
        [SerializeField] private float attackRange = 1.85f;
        [SerializeField] private float attackDamage = 25.0f;
        [SerializeField] private float attackCooldown = 1.3f;
        [SerializeField] private float clawReachDistance = 1.35f;
        [SerializeField] private float clawHitRadius = 0.85f;
        [SerializeField] private float clawHeightOffset = 1.25f;
        [Range(0.1f, 0.9f)]
        [SerializeField] private float autoHitNormalizedTime = 0.42f;

        [Header("Claw Audio")]
        [SerializeField] private AudioClip clawAttackSound;

        public static readonly int AnimHashSpeed = Animator.StringToHash("Speed");
        public static readonly int AnimHashIsAttacking = Animator.StringToHash("IsAttacking");
        public static readonly int AnimHashIsDead = Animator.StringToHash("IsDead");
        public static readonly int AnimHashAttack = Animator.StringToHash("Attack");
        public static readonly int AnimHashDie = Animator.StringToHash("Die");
        public static readonly int AnimHashDeath = Animator.StringToHash("Death");

        // Zero-GC Non-Alloc Hit Buffer cho đòn cào của Zombie
        private static readonly Collider[] ClawHitBuffer = new Collider[16];

        // State Machine (State Pattern)
        private IZombieState currentState;
        public ZombieChaseState ChaseState { get; private set; }
        public ZombieAttackState AttackState { get; private set; }
        public ZombieDeathState DeathState { get; private set; }

        private float nextAttackTime = 0.0f;

        #region Public Properties Override
        public override bool IsDead => currentState is ZombieDeathState;
        public override bool IsAttacking => currentState is ZombieAttackState;
        public override bool CanAttack => Time.time >= nextAttackTime;
        public override float AttackRange => attackRange;
        public override float AttackDamage => attackDamage;

        public float AttackCooldown => attackCooldown;
        public float ClawReachDistance => clawReachDistance;
        public float ClawHitRadius => clawHitRadius;
        public float ClawHeightOffset => clawHeightOffset;
        public float AutoHitNormalizedTime => autoHitNormalizedTime;
        #endregion

        /// <summary>
        /// Phục hồi hoàn toàn trạng thái chiến đấu cho Zombie khi lấy ra từ Object Pool (Zero-GC).
        /// </summary>
        public void ResetForSpawn(float hp)
        {
            StopAllCoroutines();
            maxHealth = Mathf.Max(1.0f, hp);
            currentHealth = maxHealth;
            nextAttackTime = Time.time + 0.3f;
            stuckTimer = 0.0f;
            lastStuckCheckPosition = transform.position;

            if (capsuleCollider != null)
            {
                capsuleCollider.isTrigger = false;
                capsuleCollider.radius = 0.32f;
                capsuleCollider.height = 1.95f;
                capsuleCollider.center = new Vector3(0f, 0.98f, 0f);
                capsuleCollider.enabled = true;
            }

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            if (agent != null)
            {
                agent.enabled = true;
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.velocity = Vector3.zero;
                    agent.updateRotation = true;
                }
            }

            if (animator != null)
            {
                animator.Rebind();
                animator.Update(0f);
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                animator.SetBool(AnimHashIsDead, false);
                animator.SetBool(AnimHashIsAttacking, false);
                animator.SetFloat(AnimHashSpeed, 0f);
            }

            FindPlayerTarget();
            if (ChaseState == null)
            {
                ChaseState = new ZombieChaseState(this);
                AttackState = new ZombieAttackState(this);
                DeathState = new ZombieDeathState(this);
            }
            currentState = ChaseState;
            currentState.Enter();
        }

        protected override void Awake()
        {
            base.Awake();

            // Khởi tạo các State chuyên biệt cho Parasite Zombie
            ChaseState = new ZombieChaseState(this);
            AttackState = new ZombieAttackState(this);
            DeathState = new ZombieDeathState(this);
        }

        protected override void Start()
        {
            base.Start();
            ChangeState(ChaseState);
        }

        public override void ApplyZombieData(ZombieDataSO data)
        {
            base.ApplyZombieData(data);
            if (data == null) return;

            attackRange = data.AttackRange;
            attackDamage = data.AttackDamage;
            attackCooldown = data.AttackCooldown;
            clawReachDistance = data.ClawReachDistance;
            clawHitRadius = data.ClawHitRadius;
            clawHeightOffset = data.ClawHeightOffset;
            autoHitNormalizedTime = data.AutoHitNormalizedTime;

            if (data.ClawAttackSound != null) clawAttackSound = data.ClawAttackSound;
        }

        public void ChangeState(IZombieState newState)
        {
            if (newState == null || currentState == newState || (currentState is ZombieDeathState)) return;

            currentState?.Exit();
            currentState = newState;
            currentState?.Enter();
        }

        public override void ForceChasePlayer()
        {
            if (IsDead) return;

            FindPlayerTarget();
            ChangeState(ChaseState);
            EnsureAgentOnNavMesh();

            if (agent != null && agent.isOnNavMesh && targetPlayer != null)
            {
                agent.isStopped = false;
                UpdateChasePath();
            }
        }

        private void Update()
        {
            if (IsDead)
            {
                currentState?.Update();
                return;
            }

            if (GameStateService == null || GameStateService.CurrentState != GameState.Playing)
            {
                if (agent != null && agent.isOnNavMesh && !agent.isStopped)
                {
                    agent.isStopped = true;
                }
                if (animator != null)
                {
                    animator.SetFloat(AnimHashSpeed, 0f);
                }
                return;
            }

            currentState?.Update();
            UpdateAnimator();
        }

        public void RecordAttackFinished()
        {
            nextAttackTime = Time.time + attackCooldown;
        }

        public void OnZombieClawAttack()
        {
            if (currentState is ZombieAttackState attackState)
            {
                attackState.PerformHitCheck("AnimationEvent");
            }
            else
            {
                PerformClawHitCheck("AnimationEvent");
            }
        }

        public void Hit()
        {
            OnZombieClawAttack();
        }

        public void PerformClawHitCheck(string triggerSource)
        {
            Vector3 hitCenter = transform.position + transform.forward * clawReachDistance + Vector3.up * clawHeightOffset;
            int hitCount = Physics.OverlapSphereNonAlloc(hitCenter, clawHitRadius, ClawHitBuffer, ~0, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider col = ClawHitBuffer[i];
                if (col != null)
                {
                    if (col.CompareTag("Player") || col.name == "Player" || (targetPlayer != null && col.transform.IsChildOf(targetPlayer)))
                    {
                        var damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
                        if (damageable != null)
                        {
                            damageable.TakeDamage(attackDamage, hitCenter, -transform.forward);
                        }

                        if (hitPlayerSound != null)
                        {
                            PlaySound(hitPlayerSound);
                        }
                        else
                        {
                            GameAudioFX.PlayClawHit(hitCenter);
                        }

#if UNITY_EDITOR
                        Debug.Log($"<color=red>[Zombie]</color> 🩸 CÀO TRÚNG PLAYER qua [{triggerSource}]! Gây {attackDamage} sát thương.");
#endif
                        break;
                    }
                }
            }

            // Zero-Leak: Xóa sạch tham chiếu trong static buffer để tránh rò rỉ bộ nhớ
            for (int i = 0; i < hitCount; i++)
            {
                ClawHitBuffer[i] = null;
            }
        }

        protected virtual void UpdateAnimator()
        {
            if (animator == null) return;

            float targetSpeed = 0.0f;
            if (currentState is ZombieChaseState && !IsAttacking)
            {
                float actualMag = (agent != null && agent.isOnNavMesh) ? agent.velocity.magnitude : 0.0f;
                targetSpeed = Mathf.Max(actualMag, chaseSpeed * 0.85f);
            }
            else
            {
                targetSpeed = 0.0f;
            }

            currentAnimSpeed = Mathf.MoveTowards(currentAnimSpeed, targetSpeed, Time.deltaTime * 12.0f);

            animator.SetFloat(AnimHashSpeed, currentAnimSpeed);
            animator.SetBool(AnimHashIsAttacking, IsAttacking);
            animator.SetBool(AnimHashIsDead, IsDead);
        }

        public void PlayAttackSound()
        {
            if (clawAttackSound != null)
            {
                PlaySound(clawAttackSound);
            }
            else
            {
                GameAudioFX.PlayClawSwipe(transform.position + transform.forward + Vector3.up * clawHeightOffset);
                GameAudioFX.PlayZombieAttack(audioSource);
            }
        }

        protected override void Die()
        {
            ChangeState(DeathState);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Vector3 hitCenter = transform.position + transform.forward * clawReachDistance + Vector3.up * clawHeightOffset;
            Gizmos.DrawWireSphere(hitCenter, clawHitRadius);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
