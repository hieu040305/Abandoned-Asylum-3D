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
    /// Lớp cơ sở trừu tượng cho hệ thống Zombie AI:
    /// Quản lý tìm đường NavMesh, cơ chế chống kẹt vị trí, xử lý lượng máu/sát thương (IDamageable)
    /// và âm thanh; cung cấp các phương thức ảo để các loài Zombie chuyên biệt mở rộng.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public abstract class BaseZombieController : MonoBehaviour, IDamageable
    {
        [Header("Zombie Data (ScriptableObject)")]
        [Tooltip("Cấu hình quái vật dạng ScriptableObject. Nếu để trống sẽ sử dụng thông số mặc định bên dưới.")]
        [SerializeField] protected ZombieDataSO zombieData;

        [Header("Target & Detection")]
        [Tooltip("Mục tiêu người chơi cần đuổi theo (Tự động tìm tag Player nếu để trống)")]
        [SerializeField] protected Transform targetPlayer;
        [SerializeField] protected float detectionRange = 200.0f;
        [SerializeField] protected float chaseSpeed = 3.6f;
        [SerializeField] protected float rotateTowardsPlayerSpeed = 8.0f;

        [Header("Health & Death")]
        [SerializeField] protected float maxHealth = 100.0f;
        [SerializeField] protected float currentHealth = 100.0f;
        [SerializeField] protected float deathAnimationDuration = 3.2f;
        [SerializeField] protected float corpseStayDuration = 4.0f;

        [Header("Audio FX")]
        [SerializeField] protected AudioClip growlSound;
        [SerializeField] protected AudioClip hitPlayerSound;
        [SerializeField] protected AudioClip deathSound;

        // Components cơ bản
        protected NavMeshAgent agent;
        protected Animator animator;
        protected CapsuleCollider capsuleCollider;
        protected AudioSource audioSource;
        protected Rigidbody rb;
        protected IGameStateService gameStateService;
        protected IGameStateService GameStateService => gameStateService ??= (ServiceLocator.TryGet(out IGameStateService svc) ? svc : GameManager.Instance);

        // Navigation & Unstuck Engine
        protected float nextNavMeshCheckTime = 0.0f;
        protected float nextPlayerSearchTime = 0.0f;
        protected float stuckTimer = 0.0f;
        protected Vector3 lastStuckCheckPosition;
        protected float currentAnimSpeed = 0.0f;

        #region Public Properties
        public abstract bool IsDead { get; }
        public abstract bool IsAttacking { get; }
        public abstract bool CanAttack { get; }
        public abstract float AttackRange { get; }
        public abstract float AttackDamage { get; }

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public ImpactSoundType ImpactType => ImpactSoundType.Flesh;
        public ZombieDataSO CurrentZombieData => zombieData;

        public NavMeshAgent Agent => agent;
        public Animator Anim => animator;
        public CapsuleCollider ZombieCollider => capsuleCollider;
        public AudioSource ZombieAudioSource => audioSource;
        public Rigidbody Rb => rb;
        public Transform TargetPlayer => targetPlayer;

        public float DetectionRange => detectionRange;
        public float ChaseSpeed => chaseSpeed;
        public float RotateSpeed => rotateTowardsPlayerSpeed;
        public float DeathAnimationDuration => deathAnimationDuration;
        public float CorpseStayDuration => corpseStayDuration;

        public float DistanceToPlayer => targetPlayer != null 
            ? Mathf.Sqrt(SqrDistanceToPlayer) 
            : float.MaxValue;

        public float SqrDistanceToPlayer => targetPlayer != null 
            ? (transform.position - targetPlayer.position).sqrMagnitude 
            : float.MaxValue;
        #endregion

        public virtual void SetHealth(float newMaxHealth)
        {
            maxHealth = Mathf.Max(1.0f, newMaxHealth);
            currentHealth = maxHealth;
        }

        protected virtual void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            capsuleCollider = GetComponent<CapsuleCollider>();
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f;
            audioSource.maxDistance = 25.0f;

            rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            transform.localScale = Vector3.one * 1.05f;

            if (capsuleCollider != null)
            {
                capsuleCollider.radius = 0.32f;
                capsuleCollider.height = 1.95f;
                capsuleCollider.center = new Vector3(0f, 0.98f, 0f);
            }

            if (zombieData != null)
            {
                ApplyZombieData(zombieData);
            }

            currentHealth = maxHealth;
            ConfigureNavMeshAgent();
        }

        protected virtual void Start()
        {
            FindPlayerTarget();
            if (GameStateService != null && GameStateService.CurrentState == GameState.Playing)
            {
                ForceChasePlayer();
            }
        }

        protected virtual void ConfigureNavMeshAgent()
        {
            if (agent != null)
            {
                agent.speed = chaseSpeed;
                agent.stoppingDistance = 1.3f;
                agent.acceleration = 18.0f;
                agent.angularSpeed = 400.0f;
                agent.autoBraking = false;
                agent.radius = 0.20f;
                agent.height = 1.85f;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
                agent.autoTraverseOffMeshLink = true;
            }
        }

        public virtual void ApplyZombieData(ZombieDataSO data)
        {
            if (data == null) return;
            zombieData = data;
            maxHealth = data.MaxHealth;
            currentHealth = maxHealth;
            chaseSpeed = data.ChaseSpeed;
            rotateTowardsPlayerSpeed = data.RotateTowardsPlayerSpeed;
            detectionRange = data.DetectionRange;
            deathAnimationDuration = data.DeathAnimationDuration;
            corpseStayDuration = data.CorpseStayDuration;

            if (data.GrowlSound != null) growlSound = data.GrowlSound;
            if (data.HitPlayerSound != null) hitPlayerSound = data.HitPlayerSound;
            if (data.DeathSound != null) deathSound = data.DeathSound;

            if (agent != null)
            {
                agent.speed = chaseSpeed;
            }
        }

        public virtual void FindPlayerTarget()
        {
            if (targetPlayer != null) return;

            // Truy xuất trực tiếp từ PlayerRegistry (0 allocation, O(1))
            if (PlayerRegistry.HasPlayer)
            {
                targetPlayer = PlayerRegistry.PlayerTransform;
                return;
            }

            if (Time.time < nextPlayerSearchTime) return;
            nextPlayerSearchTime = Time.time + 1.0f;

            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                targetPlayer = playerObj.transform;
                PlayerRegistry.Register(targetPlayer, playerObj);
            }
        }

        public virtual void EnsureAgentOnNavMesh()
        {
            if (agent == null || agent.isOnNavMesh) return;
            if (Time.time < nextNavMeshCheckTime) return;
            nextNavMeshCheckTime = Time.time + 0.5f;

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 8.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }

        public virtual void UpdateChasePath()
        {
            if (agent == null || !agent.isOnNavMesh || targetPlayer == null) return;

            agent.isStopped = false;

            // 1. Thử SetDestination trực tiếp đến Player
            bool pathOk = agent.SetDestination(targetPlayer.position);

            // 2. Dự phòng: Đến điểm NavMesh gần Player nhất trong bán kính 12m
            if (!pathOk || agent.pathStatus == NavMeshPathStatus.PathInvalid || !agent.hasPath)
            {
                if (NavMesh.SamplePosition(targetPlayer.position, out NavMeshHit playerHit, 12.0f, NavMesh.AllAreas))
                {
                    agent.SetDestination(playerHit.position);
                }

                // 3. Dự phòng: Tìm điểm xa nhất theo hướng nhìn về phía Player
                if (agent.pathStatus == NavMeshPathStatus.PathInvalid || !agent.hasPath)
                {
                    Vector3 dirToPlayer = (targetPlayer.position - transform.position).normalized;
                    Vector3 probePos = transform.position + dirToPlayer * 6.0f;
                    if (NavMesh.SamplePosition(probePos, out NavMeshHit stepHit, 6.0f, NavMesh.AllAreas))
                    {
                        agent.SetDestination(stepHit.position);
                    }
                }
            }
        }

        public virtual void HandleUnstuckMechanic()
        {
            if (IsAttacking || IsDead || targetPlayer == null) return;

            float sqrDistMoved = (transform.position - lastStuckCheckPosition).sqrMagnitude;
            if (sqrDistMoved < 0.0144f) // 0.12m * 0.12m (Tránh Mathf.Sqrt tốn CPU)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer >= 0.5f)
                {
                    stuckTimer = 0.0f;

                    Vector3 dirToPlayer = (targetPlayer.position - transform.position);
                    dirToPlayer.y = 0;
                    if (dirToPlayer.sqrMagnitude > 0.001f)
                    {
                        dirToPlayer.Normalize();

                        if (agent != null && agent.isOnNavMesh)
                        {
                            agent.Move(dirToPlayer * (chaseSpeed * 0.45f));
                        }
                        else
                        {
                            transform.position += dirToPlayer * (chaseSpeed * Time.deltaTime * 3.0f);
                            EnsureAgentOnNavMesh();
                        }

                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dirToPlayer), Time.deltaTime * 12.0f);
                    }
                }
            }
            else
            {
                stuckTimer = 0.0f;
                lastStuckCheckPosition = transform.position;
            }
        }

        public abstract void ForceChasePlayer();

        #region IDamageable Implementation
        public virtual void TakeDamage(float damageAmount)
        {
            TakeDamage(damageAmount, transform.position + Vector3.up * 1.2f, -transform.forward);
        }

        public virtual void TakeDamage(float damageAmount, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (IsDead) return;

            currentHealth -= damageAmount;
            ZombieBloodFX.SpawnBloodSplatter(hitPoint, hitNormal, (hitPoint - transform.position).normalized);

            if (audioSource != null)
            {
                GameAudioFX.PlayZombieHurt(audioSource);
            }

            OnDamageTaken(damageAmount, hitPoint, hitNormal);
            ForceChasePlayer();

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        protected virtual void OnDamageTaken(float damageAmount, Vector3 hitPoint, Vector3 hitNormal) { }
        protected abstract void Die();
        #endregion

        #region Audio Helpers
        public virtual void PlayGrowlSound()
        {
            if (growlSound != null) PlaySound(growlSound);
            else GameAudioFX.PlayZombieGrowl(audioSource);
        }

        public virtual void PlayDeathSound()
        {
            if (deathSound != null) PlaySound(deathSound);
            else GameAudioFX.PlayZombieDeath(audioSource);
        }

        protected void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
        #endregion
    }
}
