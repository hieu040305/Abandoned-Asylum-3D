using NUnit.Framework;
using UnityEngine;
using AbandonedAsylum.Player;

namespace AbandonedAsylum.Tests
{
    [TestFixture]
    public class AimRigVisualizationTests
    {
        private GameObject playerObj;
        private PlayerAimRigController aimRig;

        [SetUp]
        public void SetUp()
        {
            playerObj = new GameObject("Test_Player_AimRig");
            aimRig = playerObj.AddComponent<PlayerAimRigController>();
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
        public void AimRig_DefaultVisualizationProperties_AreConfiguredCorrectly()
        {
            Assert.IsTrue(aimRig.EnableAimAlignment, "Aim alignment should be enabled by default.");
            Assert.IsTrue(aimRig.ShowAimGizmos, "Show aim gizmos should be enabled by default for Editor tuning.");
            Assert.AreEqual(Color.yellow, aimRig.AimGizmoColor, "Gizmo color should default to clean yellow.");
            Assert.AreEqual(40.0f, aimRig.AimRayLength, 0.01f, "Default aim ray length should be 40m.");
        }

        [Test]
        public void AimRig_SetGizmoProperties_UpdatesWithoutErrors()
        {
            Color customColor = new Color(1f, 0.92f, 0.016f, 1f);
            aimRig.AimGizmoColor = customColor;
            Assert.AreEqual(customColor, aimRig.AimGizmoColor);

            aimRig.AimRayLength = 60f;
            Assert.AreEqual(60f, aimRig.AimRayLength, 0.001f);

            aimRig.ShowAimGizmos = false;
            Assert.IsFalse(aimRig.ShowAimGizmos);
        }

        [Test]
        public void AimRig_GetEffectiveAimOrigin_DiscoversFallbackChildMuzzle()
        {
            GameObject muzzleObj = new GameObject("shootpoint");
            muzzleObj.transform.SetParent(playerObj.transform);

            Transform origin = aimRig.GetEffectiveAimOrigin();
            Assert.IsNotNull(origin, "Should discover shootpoint child when explicit references are empty.");
            Assert.AreSame(muzzleObj.transform, origin);
        }
    }
}
