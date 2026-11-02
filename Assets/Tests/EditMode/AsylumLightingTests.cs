using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using AbandonedAsylum.Core;

namespace AbandonedAsylum.Tests
{
    [TestFixture]
    public class AsylumLightingTests
    {
        private GameObject managerGo;
        private AsylumLightingManager manager;
        private List<GameObject> testObjects;

        [SetUp]
        public void SetUp()
        {
            testObjects = new List<GameObject>();
            managerGo = new GameObject("Test_LightingManager");
            manager = managerGo.AddComponent<AsylumLightingManager>();
            testObjects.Add(managerGo);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerRegistry.Unregister(PlayerRegistry.PlayerTransform);
            for (int i = 0; i < testObjects.Count; i++)
            {
                if (testObjects[i] != null)
                {
                    Object.DestroyImmediate(testObjects[i]);
                }
            }
            testObjects.Clear();
        }

        [Test]
        public void PurgeArtificialLights_DestroysArtificialMoonlightAndFlashlight()
        {
            // Tạo nguồn sáng nhân tạo (Directional Moonlight giả lập)
            GameObject fakeMoonlight = new GameObject("[Asylum_Atmospheric_Moonlight]");
            var mlLight = fakeMoonlight.AddComponent<Light>();
            mlLight.type = LightType.Directional;
            testObjects.Add(fakeMoonlight);

            // Tạo nhân vật có đèn pin nhân tạo
            GameObject playerGo = new GameObject("Player");
            playerGo.tag = "Player";
            GameObject fakeFlashlight = new GameObject("TacticalFlashlight");
            fakeFlashlight.transform.SetParent(playerGo.transform);
            fakeFlashlight.AddComponent<Light>();
            testObjects.Add(playerGo);

            PlayerRegistry.Register(playerGo.transform, playerGo);

            // Thực hiện dọn dẹp
            manager.PurgeArtificialLights();

            // Kiểm chứng: Toàn bộ nguồn sáng giả lập từ hư vô bị tiêu diệt
            Assert.IsTrue(fakeMoonlight == null || !fakeMoonlight.activeSelf || !fakeMoonlight.GetComponent<Light>().enabled,
                "Artificial moonlight must be destroyed or disabled.");
            Assert.IsTrue(fakeFlashlight == null, "Tactical flashlight must be destroyed.");
        }

        [Test]
        public void ShadowsDisabled_AllLampsSetToNoShadows()
        {
            // Thiết lập vị trí người chơi tại gốc tọa độ (0, 0, 0)
            GameObject playerGo = new GameObject("Player");
            playerGo.tag = "Player";
            playerGo.transform.position = Vector3.zero;
            testObjects.Add(playerGo);
            PlayerRegistry.Register(playerGo.transform, playerGo);

            // Tạo 3 bóng đèn ở khoảng cách khác nhau
            GameObject nearLamp1 = new GameObject("NeonLamp_01");
            nearLamp1.transform.position = new Vector3(3, 2, 0);
            Light light1 = nearLamp1.AddComponent<Light>();
            light1.type = LightType.Point;
            light1.range = 5.5f;
            light1.intensity = 0.90f;
            testObjects.Add(nearLamp1);

            GameObject nearLamp2 = new GameObject("LampWall_01");
            nearLamp2.transform.position = new Vector3(0, 2, 6);
            Light light2 = nearLamp2.AddComponent<Light>();
            light2.type = LightType.Point;
            light2.range = 4.5f;
            light2.intensity = 0.85f;
            testObjects.Add(nearLamp2);

            GameObject farLamp = new GameObject("NeonLamp_Far");
            farLamp.transform.position = new Vector3(40, 2, 0);
            Light light3 = farLamp.AddComponent<Light>();
            light3.type = LightType.Point;
            testObjects.Add(farLamp);

            manager.DiscoverMapLamps();
            manager.UpdateProximityShadows(true);

            // Triệt tiêu 100% bóng đổ trên toàn bộ bóng đèn
            Assert.AreEqual(LightShadows.None, light1.shadows, "Lamp 1 must not cast shadows.");
            Assert.AreEqual(LightShadows.None, light2.shadows, "Lamp 2 must not cast shadows.");
            Assert.AreEqual(LightShadows.None, light3.shadows, "Lamp 3 must not cast shadows.");
        }

        [Test]
        public void AuditAndEnableDynamicShadows_DisablesShadowsAcrossAllRenderers()
        {
            // 1. Kiểm thử nhân vật / quái vật
            GameObject enemyGo = new GameObject("Enemy");
            enemyGo.tag = "Enemy";
            var skinnedMeshRenderer = enemyGo.AddComponent<SkinnedMeshRenderer>();
            skinnedMeshRenderer.shadowCastingMode = ShadowCastingMode.On;
            skinnedMeshRenderer.receiveShadows = true;
            skinnedMeshRenderer.lightProbeUsage = LightProbeUsage.BlendProbes;
            testObjects.Add(enemyGo);

            // 2. Kiểm thử mặt sàn / môi trường
            GameObject floorGo = new GameObject("Floor_Concrete");
            var meshRenderer = floorGo.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = ShadowCastingMode.On;
            meshRenderer.receiveShadows = true;
            testObjects.Add(floorGo);

            manager.AuditAndEnableDynamicShadows();

            // Toàn bộ Renderers tắt bóng đổ để tối đa hóa hiệu năng và triệt tiêu lỗi model đen sì
            Assert.AreEqual(ShadowCastingMode.Off, skinnedMeshRenderer.shadowCastingMode, "Character must have shadows turned off.");
            Assert.IsFalse(skinnedMeshRenderer.receiveShadows, "Character must not receive shadows.");
            Assert.AreEqual(LightProbeUsage.Off, skinnedMeshRenderer.lightProbeUsage, "Character must not use corrupt light probes.");

            Assert.AreEqual(ShadowCastingMode.Off, meshRenderer.shadowCastingMode, "Environment mesh must have shadows turned off.");
            Assert.IsFalse(meshRenderer.receiveShadows, "Environment mesh must not receive shadows.");
        }
    }
}
