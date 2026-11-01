using NUnit.Framework;
using UnityEngine;
using AbandonedAsylum.Data;
using AbandonedAsylum.Zombie;

namespace AbandonedAsylum.Tests
{
    [TestFixture]
    public class ZombieFSMTests
    {
        private class MockZombieState : IZombieState
        {
            public bool Entered { get; private set; }
            public bool Updated { get; private set; }
            public bool Exited { get; private set; }

            public void Enter() => Entered = true;
            public void Update() => Updated = true;
            public void Exit() => Exited = true;
        }

        private ZombieDataSO zombieData;

        [SetUp]
        public void SetUp()
        {
            zombieData = ScriptableObject.CreateInstance<ZombieDataSO>();
        }

        [TearDown]
        public void TearDown()
        {
            if (zombieData != null)
            {
                Object.DestroyImmediate(zombieData);
            }
        }

        [Test]
        public void ZombieDataSO_DefaultProperties_AreValid()
        {
            Assert.Greater(zombieData.MaxHealth, 0f);
            Assert.Greater(zombieData.ChaseSpeed, 0f);
            Assert.Greater(zombieData.AttackRange, 0f);
            Assert.Greater(zombieData.AttackDamage, 0f);
            Assert.Greater(zombieData.AttackCooldown, 0f);
        }

        [Test]
        public void IZombieState_MockExecution_InvokesLifecycleProperly()
        {
            var state = new MockZombieState();

            Assert.IsFalse(state.Entered);
            state.Enter();
            Assert.IsTrue(state.Entered);

            Assert.IsFalse(state.Updated);
            state.Update();
            Assert.IsTrue(state.Updated);

            Assert.IsFalse(state.Exited);
            state.Exit();
            Assert.IsTrue(state.Exited);
        }

        [Test]
        public void ZombieDataSO_ClawParameters_AreWithinExpectedRanges()
        {
            Assert.Greater(zombieData.ClawReachDistance, 0f);
            Assert.Greater(zombieData.ClawHitRadius, 0f);
            Assert.Greater(zombieData.ClawHeightOffset, 0f);
            Assert.Greater(zombieData.AutoHitNormalizedTime, 0f);
            Assert.LessOrEqual(zombieData.AutoHitNormalizedTime, 1f);
        }

        [Test]
        public void ZombieDataSO_DeathSequence_DurationsArePositive()
        {
            Assert.Greater(zombieData.DeathAnimationDuration, 0f);
            Assert.Greater(zombieData.CorpseStayDuration, 0f);
        }

        [Test]
        public void ZombieDeathState_Lifecycle_TicksAndCleansUp()
        {
            GameObject testZombieGo = new GameObject("TestZombie");
            testZombieGo.AddComponent<CapsuleCollider>();
            testZombieGo.AddComponent<Rigidbody>();
            var controller = testZombieGo.AddComponent<ZombieController>();

            var deathState = new ZombieDeathState(controller);
            deathState.Enter();

            Assert.IsTrue(controller.IsDead, "Controller must report IsDead = true when in DeathState.");

            // Ticking update should not throw any exceptions
            Assert.DoesNotThrow(() => deathState.Update());

            Object.DestroyImmediate(testZombieGo);
        }
    }
}
