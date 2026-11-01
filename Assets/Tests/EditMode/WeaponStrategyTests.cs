using NUnit.Framework;
using UnityEngine;
using AbandonedAsylum.Data;
using AbandonedAsylum.Player.Weapons;

namespace AbandonedAsylum.Tests
{
    [TestFixture]
    public class WeaponStrategyTests
    {
        private WeaponDataSO weaponData;
        private RifleWeaponStrategy rifleStrategy;

        [SetUp]
        public void SetUp()
        {
            weaponData = ScriptableObject.CreateInstance<WeaponDataSO>();
            rifleStrategy = new RifleWeaponStrategy();
        }

        [TearDown]
        public void TearDown()
        {
            if (weaponData != null)
            {
                Object.DestroyImmediate(weaponData);
            }
        }

        [Test]
        public void RifleStrategy_ReturnsValidName()
        {
            Assert.IsNotNull(rifleStrategy.StrategyName);
            Assert.IsNotEmpty(rifleStrategy.StrategyName);
            StringAssert.Contains("Rifle", rifleStrategy.StrategyName);
        }

        [Test]
        public void WeaponDataSO_DefaultProperties_AreValid()
        {
            Assert.Greater(weaponData.Damage, 0f);
            Assert.Greater(weaponData.Range, 0f);
            Assert.Greater(weaponData.FireRate, 0f);
            Assert.Greater(weaponData.MaxAmmo, 0);
            Assert.Greater(weaponData.ReloadDuration, 0f);
        }

        [Test]
        public void WeaponFireContext_PreservesPassedValues()
        {
            var context = new WeaponFireContext
            {
                Range = 150f,
                Damage = 35f,
                BulletSpeed = 220f,
                VerticalRecoil = 0.7f,
                HorizontalRecoil = 0.2f,
                IsCursorLocked = true
            };

            Assert.AreEqual(150f, context.Range);
            Assert.AreEqual(35f, context.Damage);
            Assert.AreEqual(220f, context.BulletSpeed);
            Assert.AreEqual(0.7f, context.VerticalRecoil);
            Assert.AreEqual(0.2f, context.HorizontalRecoil);
            Assert.IsTrue(context.IsCursorLocked);
        }

        [Test]
        public void WeaponStrategy_NullHitBuffer_HandledGracefullyByFallback()
        {
            var context = new WeaponFireContext
            {
                Range = 100f,
                Damage = 20f,
                BulletSpeed = 200f,
                HitBuffer = null, // Kiểm tra fallback static buffer Zero-GC
                DefaultOrigin = Vector3.zero,
                ForwardDirection = Vector3.forward,
                ShootableLayers = ~0
            };

            Assert.DoesNotThrow(() =>
            {
                rifleStrategy.ExecuteFire(in context);
            });
        }

        [Test]
        public void WeaponFireContext_DefaultRecoilValues_AreNonNegative()
        {
            var context = new WeaponFireContext
            {
                VerticalRecoil = 0.5f,
                HorizontalRecoil = 0.15f
            };

            Assert.GreaterOrEqual(context.VerticalRecoil, 0f);
            Assert.GreaterOrEqual(context.HorizontalRecoil, 0f);
        }

        [Test]
        public void WeaponDataSO_NormalizedReloadTimings_AreInValidSequence()
        {
            Assert.GreaterOrEqual(weaponData.MagOutTimeNormalized, 0f);
            Assert.Less(weaponData.MagOutTimeNormalized, weaponData.MagInTimeNormalized);
            Assert.Less(weaponData.MagInTimeNormalized, weaponData.ChargeTimeNormalized);
            Assert.LessOrEqual(weaponData.ChargeTimeNormalized, 1.0f);
        }

        [Test]
        public void ShotgunStrategy_ImplementsInterface_AndReturnsValidName()
        {
            IWeaponStrategy shotgun = new ShotgunWeaponStrategy();
            Assert.IsNotNull(shotgun.StrategyName);
            StringAssert.Contains("Shotgun", shotgun.StrategyName);
        }

        [Test]
        public void PolymorphicWeaponStrategies_CanBeAssignedToInterface()
        {
            IWeaponStrategy strategy1 = new RifleWeaponStrategy();
            IWeaponStrategy strategy2 = new ShotgunWeaponStrategy();

            Assert.AreNotEqual(strategy1.StrategyName, strategy2.StrategyName);
            Assert.IsNotNull(strategy1.StrategyName);
            Assert.IsNotNull(strategy2.StrategyName);
        }
    }
}
