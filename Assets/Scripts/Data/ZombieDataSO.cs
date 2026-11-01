using UnityEngine;

namespace AbandonedAsylum.Data
{
    [CreateAssetMenu(fileName = "NewZombieData", menuName = "Abandoned Asylum/AI/Zombie Data", order = 2)]
    public class ZombieDataSO : ScriptableObject
    {
        [Header("General Info")]
        [SerializeField] private string enemyName = "Parasite Zombie";

        [Header("Locomotion & Health")]
        [SerializeField] private float maxHealth = 100.0f;
        [SerializeField] private float chaseSpeed = 3.6f;
        [SerializeField] private float rotateTowardsPlayerSpeed = 8.0f;
        [SerializeField] private float detectionRange = 200.0f;

        [Header("Claw Attack")]
        [SerializeField] private float attackRange = 1.8f;
        [SerializeField] private float attackDamage = 25.0f;
        [SerializeField] private float attackCooldown = 1.3f;
        [SerializeField] private float clawReachDistance = 1.3f;
        [SerializeField] private float clawHitRadius = 0.85f;
        [SerializeField] private float clawHeightOffset = 1.2f;
        [Range(0.1f, 0.9f)]
        [SerializeField] private float autoHitNormalizedTime = 0.42f;

        [Header("Death Sequence")]
        [SerializeField] private float deathAnimationDuration = 3.2f;
        [SerializeField] private float corpseStayDuration = 4.0f;

        [Header("Audio Overrides")]
        [SerializeField] private AudioClip growlSound;
        [SerializeField] private AudioClip clawAttackSound;
        [SerializeField] private AudioClip hitPlayerSound;
        [SerializeField] private AudioClip deathSound;

        public string EnemyName => enemyName;
        public float MaxHealth => maxHealth;
        public float ChaseSpeed => chaseSpeed;
        public float RotateTowardsPlayerSpeed => rotateTowardsPlayerSpeed;
        public float DetectionRange => detectionRange;
        public float AttackRange => attackRange;
        public float AttackDamage => attackDamage;
        public float AttackCooldown => attackCooldown;
        public float ClawReachDistance => clawReachDistance;
        public float ClawHitRadius => clawHitRadius;
        public float ClawHeightOffset => clawHeightOffset;
        public float AutoHitNormalizedTime => autoHitNormalizedTime;
        public float DeathAnimationDuration => deathAnimationDuration;
        public float CorpseStayDuration => corpseStayDuration;
        public AudioClip GrowlSound => growlSound;
        public AudioClip ClawAttackSound => clawAttackSound;
        public AudioClip HitPlayerSound => hitPlayerSound;
        public AudioClip DeathSound => deathSound;
    }
}
