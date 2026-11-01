using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using AbandonedAsylum.Zombie;

namespace AbandonedAsylum.Tests.PlayMode
{
    [TestFixture]
    public class ZombiePoolLifecycleTests
    {
        private GameObject dummyZombiePrefab;

        [SetUp]
        public void SetUp()
        {
            dummyZombiePrefab = new GameObject("Dummy_Zombie_Prefab");
            dummyZombiePrefab.AddComponent<CapsuleCollider>();
            dummyZombiePrefab.AddComponent<ZombieController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (dummyZombiePrefab != null)
            {
                Object.DestroyImmediate(dummyZombiePrefab);
            }
            ZombiePool.Instance.ResetAllZombies();
        }

        [UnityTest]
        public IEnumerator ZombiePool_SpawnMultiple_RecyclesSuccessfully()
        {
            ZombiePool pool = ZombiePool.Instance;
            Assert.IsNotNull(pool, "ZombiePool instance must exist");

            pool.RegisterPrefab(dummyZombiePrefab);

            // Spawn 5 zombies
            for (int i = 0; i < 5; i++)
            {
                var z = pool.SpawnZombie(new Vector3(i * 2f, 0f, 0f), Quaternion.identity, 100f, dummyZombiePrefab);
                Assert.IsNotNull(z, $"Zombie {i} should be spawned successfully");
                Assert.IsTrue(z.gameObject.activeSelf);
            }

            yield return null;

            // Reset toàn bộ zombie về hồ chứa
            pool.ResetAllZombies();

            yield return null;

            // Xác nhận tất cả zombie đều đã được inactive
            Transform poolTransform = pool.transform;
            for (int i = 0; i < poolTransform.childCount; i++)
            {
                Transform child = poolTransform.GetChild(i);
                Assert.IsFalse(child.gameObject.activeSelf, "Pooled zombie should be inactive after ResetAllZombies");
            }
        }
    }
}
