using NUnit.Framework;
using UnityEngine;
using AbandonedAsylum.Player;

namespace AbandonedAsylum.Tests
{
    [TestFixture]
    public class PlayerHealthTests
    {
        private GameObject playerObj;
        private PlayerHealth playerHealth;

        [SetUp]
        public void SetUp()
        {
            playerObj = new GameObject("Test_Player");
            playerHealth = playerObj.AddComponent<PlayerHealth>();
            playerHealth.SetHealth(100f);
        }

        [TearDown]
        public void TearDown()
        {
            if (playerObj != null)
            {
                Object.DestroyImmediate(playerObj);
            }
        }

        [Test]
        public void InitialHealth_Equals_MaxHealth()
        {
            Assert.AreEqual(100f, playerHealth.MaxHealth);
            Assert.AreEqual(100f, playerHealth.CurrentHealth);
            Assert.IsFalse(playerHealth.IsDead);
        }

        [Test]
        public void TakeDamage_ReducesCurrentHealth()
        {
            float damageAmount = 25f;
            playerHealth.TakeDamage(damageAmount);

            Assert.AreEqual(75f, playerHealth.CurrentHealth);
            Assert.IsFalse(playerHealth.IsDead);
        }

        [Test]
        public void TakeDamage_DoesNotDropBelowZero()
        {
            playerHealth.TakeDamage(250f);

            Assert.AreEqual(0f, playerHealth.CurrentHealth);
            Assert.IsTrue(playerHealth.IsDead);
        }

        [Test]
        public void TakeDamage_Fires_OnHealthChanged_And_OnDamaged()
        {
            bool damagedFired = false;
            float receivedCurrent = -1f;
            float receivedMax = -1f;

            playerHealth.OnDamaged += () => damagedFired = true;
            playerHealth.OnHealthChanged += (cur, max) =>
            {
                receivedCurrent = cur;
                receivedMax = max;
            };

            playerHealth.TakeDamage(30f);

            Assert.IsTrue(damagedFired);
            Assert.AreEqual(70f, receivedCurrent);
            Assert.AreEqual(100f, receivedMax);
        }

        [Test]
        public void TakeDamage_FatalAmount_Fires_OnDeath()
        {
            bool deathFired = false;
            playerHealth.OnDeath += () => deathFired = true;

            playerHealth.TakeDamage(100f);

            Assert.IsTrue(deathFired);
            Assert.IsTrue(playerHealth.IsDead);
        }

        [Test]
        public void TakeDamage_AfterDeath_IsIgnored()
        {
            playerHealth.TakeDamage(100f);
            Assert.IsTrue(playerHealth.IsDead);

            int deathCallCount = 0;
            playerHealth.OnDeath += () => deathCallCount++;

            playerHealth.TakeDamage(50f);

            Assert.AreEqual(0f, playerHealth.CurrentHealth);
            Assert.AreEqual(0, deathCallCount);
        }

        [Test]
        public void Heal_IncreasesHealth_CappedAtMaxHealth()
        {
            playerHealth.TakeDamage(50f);
            Assert.AreEqual(50f, playerHealth.CurrentHealth);

            playerHealth.Heal(20f);
            Assert.AreEqual(70f, playerHealth.CurrentHealth);

            playerHealth.Heal(100f);
            Assert.AreEqual(100f, playerHealth.CurrentHealth);
        }
    }
}
