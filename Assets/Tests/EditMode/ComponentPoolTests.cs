using NUnit.Framework;
using UnityEngine;
using AbandonedAsylum.Core;
using AbandonedAsylum.Interfaces;

namespace AbandonedAsylum.Tests
{
    [TestFixture]
    public class ComponentPoolTests
    {
        private GameObject testPrefabObj;
        private BoxCollider testPrefab;
        private GameObject rootContainerObj;

        [SetUp]
        public void SetUp()
        {
            testPrefabObj = new GameObject("Test_Poolable_Prefab");
            testPrefab = testPrefabObj.AddComponent<BoxCollider>();
            testPrefabObj.SetActive(false);

            rootContainerObj = new GameObject("Test_Pool_Container");
        }

        [TearDown]
        public void TearDown()
        {
            if (testPrefabObj != null)
            {
                Object.DestroyImmediate(testPrefabObj);
            }
            if (rootContainerObj != null)
            {
                Object.DestroyImmediate(rootContainerObj);
            }
        }

        [Test]
        public void ComponentPool_Prewarm_InstantiatesExpectedInactiveCount()
        {
            var pool = new ComponentPool<BoxCollider>(testPrefab, 8, rootContainerObj.transform);
            pool.Prewarm(5);

            Assert.AreEqual(0, pool.CountActive);
            Assert.AreEqual(5, pool.CountInactive);
            Assert.AreEqual(5, pool.CountAll);

            pool.Clear();
        }

        [Test]
        public void ComponentPool_GetAndRelease_MaintainsCorrectStates()
        {
            var pool = new ComponentPool<BoxCollider>(testPrefab, 8, rootContainerObj.transform);
            pool.Prewarm(3);

            var item1 = pool.Get();
            Assert.IsNotNull(item1);
            Assert.IsTrue(item1.gameObject.activeSelf);
            Assert.AreEqual(1, pool.CountActive);
            Assert.AreEqual(2, pool.CountInactive);

            pool.Release(item1);
            Assert.IsFalse(item1.gameObject.activeSelf);
            Assert.AreEqual(0, pool.CountActive);
            Assert.AreEqual(3, pool.CountInactive);

            pool.Clear();
        }

        [Test]
        public void ComponentPool_DoubleRelease_IsPreventedSafely()
        {
            var pool = new ComponentPool<BoxCollider>(testPrefab, 8, rootContainerObj.transform);
            var item = pool.Get();

            pool.Release(item);
            Assert.AreEqual(1, pool.CountInactive);

            // Double release should be ignored gracefully
            pool.Release(item);
            Assert.AreEqual(1, pool.CountInactive);

            pool.Clear();
        }

        [Test]
        public void PhysicsLayerConfigurator_RunsWithoutErrors()
        {
            Assert.DoesNotThrow(() =>
            {
                PhysicsLayerConfigurator.ConfigureOptimalPhysicsLayers();
            });
        }
    }
}
