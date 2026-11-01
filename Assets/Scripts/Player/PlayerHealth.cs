using System;
using UnityEngine;
using AbandonedAsylum.Core;
using AbandonedAsylum.Interfaces;

namespace AbandonedAsylum.Player
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 100.0f;
        [SerializeField] private float currentHealth = 100.0f;

        private bool isDead = false;
        private Animator animator;
        private static readonly int AnimHashPlayerDie = Animator.StringToHash("PlayerDie");

        public event Action<float, float> OnHealthChanged;
        public event Action OnDamaged;
        public event Action OnDeath;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => isDead;
        public ImpactSoundType ImpactType => ImpactSoundType.Flesh;

        private void Awake()
        {
            currentHealth = maxHealth;
            animator = GetComponent<Animator>();
        }

        public void TakeDamage(float damageAmount)
        {
            if (isDead) return;

            currentHealth = Mathf.Max(0.0f, currentHealth - damageAmount);

            OnDamaged?.Invoke();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0.0f)
            {
                Die();
            }
        }

        public void TakeDamage(float damageAmount, Vector3 hitPoint, Vector3 hitNormal)
        {
            TakeDamage(damageAmount);
        }

        public void Heal(float healAmount)
        {
            if (isDead) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            if (animator != null)
            {
                animator.SetTrigger(AnimHashPlayerDie);
            }

            OnDeath?.Invoke();
        }

        public void SetHealth(float newMaxHealth)
        {
            maxHealth = Mathf.Max(1.0f, newMaxHealth);
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }
}
