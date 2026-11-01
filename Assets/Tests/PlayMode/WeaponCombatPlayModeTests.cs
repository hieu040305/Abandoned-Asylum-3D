using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using AbandonedAsylum.VFX;
using AbandonedAsylum.Player.Weapons;
using AbandonedAsylum.Interfaces;

namespace AbandonedAsylum.Tests.PlayMode
{
    [TestFixture]
    public class WeaponCombatPlayModeTests
    {
        private GameObject testContainer;
        private WeaponFX weaponFX;

        [SetUp]
        public void SetUp()
        {
            testContainer = new GameObject("[Test_WeaponCombat]");
            weaponFX = testContainer.AddComponent<WeaponFX>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testContainer != null)
            {
                Object.DestroyImmediate(testContainer);
            }
        }

        [UnityTest]
        public IEnumerator RifleStrategy_ExecuteFire_TriggersTracerBulletWithoutExceptions()
        {
            var strategy = new RifleWeaponStrategy();
            var muzzle = new GameObject("Test_Muzzle");
            muzzle.transform.SetParent(testContainer.transform);

            var context = new WeaponFireContext
            {
                BulletSpawnPoint = muzzle.transform,
                DefaultOrigin = muzzle.transform.position,
                ForwardDirection = Vector3.forward,
                Range = 100f,
                Damage = 35f,
                BulletSpeed = 250f,
                HitBuffer = new RaycastHit[8],
                WeaponFX = weaponFX,
                ShootableLayers = ~0,
                PlayerTransform = testContainer.transform
            };

            // Bắn 10 phát liên tiếp
            for (int i = 0; i < 10; i++)
            {
                Assert.DoesNotThrow(() => strategy.ExecuteFire(in context));
                yield return null;
            }

            Assert.IsNotNull(strategy.StrategyName);
            StringAssert.Contains("Rifle", strategy.StrategyName);
        }

        [UnityTest]
        public IEnumerator TracerBullet_SpawnAndRecycle_OperatesWithinPoolCapacity()
        {
            Vector3 origin = Vector3.zero;
            Vector3 target = Vector3.forward * 50f;

            // Bắn 40 viên đạn thử nghiệm
            for (int i = 0; i < 40; i++)
            {
                TracerBullet.SpawnPooledBullet(origin, target, Vector3.back, null, 30f, 300f);
            }

            yield return null;

            // Kiểm tra container pool tồn tại và quản lý các instance
            GameObject poolObj = GameObject.Find("[TracerBullet_ObjectPool]");
            Assert.IsNotNull(poolObj, "Tracer bullet pool container must exist in scene");
            Assert.LessOrEqual(poolObj.transform.childCount, 40, "Pool size should not exceed 40 instances");
        }
    }
}
