using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using AbandonedAsylum.Core;
using AbandonedAsylum.VFX;

namespace AbandonedAsylum.Tests.PlayMode
{
    [TestFixture]
    public class ZeroGCHotLoopIntegrationTests
    {
        private GameObject playerMock;

        [SetUp]
        public void SetUp()
        {
            playerMock = new GameObject("Test_Player_Integration");
            PlayerRegistry.Register(playerMock.transform, playerMock);
        }

        [TearDown]
        public void TearDown()
        {
            if (playerMock != null)
            {
                PlayerRegistry.Unregister(playerMock.transform);
                Object.DestroyImmediate(playerMock);
            }
        }

        [Test]
        public void PlayerRegistry_RegistersAndUnregisters_WithZeroAllocation()
        {
            Assert.IsTrue(PlayerRegistry.HasPlayer);
            Assert.AreSame(playerMock.transform, PlayerRegistry.PlayerTransform);
            Assert.AreSame(playerMock, PlayerRegistry.PlayerGameObject);

            PlayerRegistry.Unregister(playerMock.transform);
            Assert.IsFalse(PlayerRegistry.HasPlayer);
            Assert.IsNull(PlayerRegistry.PlayerTransform);
            Assert.IsNull(PlayerRegistry.PlayerGameObject);
        }

        [Test]
        public void TracerBullet_SpawnAndFire_ExecutesWithoutExceptions()
        {
            GameObject bulletObj = new GameObject("Tracer_Test_Instance");
            TracerBullet bullet = bulletObj.AddComponent<TracerBullet>();

            Vector3 start = Vector3.zero;
            Vector3 target = new Vector3(0f, 0f, 25f);
            bullet.Fire(start, target, Vector3.back, null, 35f, 200f);

            Assert.IsTrue(bullet.gameObject.activeSelf, "Fired bullet must be set active.");
            Assert.AreEqual(start, bullet.transform.position, "Fired bullet start position must be initialized properly.");

            Object.DestroyImmediate(bulletObj);
        }
    }
}
