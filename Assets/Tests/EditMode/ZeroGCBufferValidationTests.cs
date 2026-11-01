using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using AbandonedAsylum.Interfaces;
using AbandonedAsylum.Core.Input;
using AbandonedAsylum.Player;
using AbandonedAsylum.Player.Weapons;

namespace AbandonedAsylum.Tests
{
    [TestFixture]
    public class ZeroGCBufferValidationTests
    {
        private class MockDamageableTarget : MonoBehaviour, IDamageable
        {
            public float CurrentHealth { get; private set; } = 100f;
            public float MaxHealth => 100f;
            public bool IsDead => CurrentHealth <= 0f;
            public ImpactSoundType ImpactType => ImpactSoundType.Flesh;

            public void TakeDamage(float damageAmount)
            {
                CurrentHealth = Mathf.Max(0f, CurrentHealth - damageAmount);
            }

            public void TakeDamage(float damageAmount, Vector3 hitPoint, Vector3 hitNormal)
            {
                TakeDamage(damageAmount);
            }
        }

        private GameObject targetObj;
        private List<GameObject> cleanupList;

        [SetUp]
        public void SetUp()
        {
            cleanupList = new List<GameObject>();
            targetObj = new GameObject("Test_Damageable_Target");
            targetObj.AddComponent<MockDamageableTarget>();
            cleanupList.Add(targetObj);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < cleanupList.Count; i++)
            {
                if (cleanupList[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(cleanupList[i]);
                }
            }
            cleanupList.Clear();
        }

        [Test]
        public void WeaponSocketDefinition_ResolvesMuzzleAndAimPoints_Accurately()
        {
            GameObject weaponObj = new GameObject("Kalashnicolt_Test");
            cleanupList.Add(weaponObj);
            GameObject muzzleChild = new GameObject("MuzzleNode");
            muzzleChild.transform.SetParent(weaponObj.transform);

            WeaponSocketDefinition socketDef = weaponObj.AddComponent<WeaponSocketDefinition>();
            socketDef.SetSockets(muzzleChild.transform, muzzleChild.transform);

            Assert.AreSame(muzzleChild.transform, socketDef.MuzzlePoint, "MuzzlePoint must match the explicitly bound transform.");
            Assert.AreSame(muzzleChild.transform, socketDef.AimReferencePoint, "AimReferencePoint must match the explicitly bound transform.");
        }

        [Test]
        public void DamageableTarget_PolymorphicInterface_BehavesCorrectly()
        {
            IDamageable damageable = targetObj.GetComponent<IDamageable>();
            Assert.IsNotNull(damageable);
            Assert.AreEqual(100f, damageable.CurrentHealth);
            Assert.IsFalse(damageable.IsDead);
            Assert.AreEqual(ImpactSoundType.Flesh, damageable.ImpactType);

            damageable.TakeDamage(45f);
            Assert.AreEqual(55f, damageable.CurrentHealth);
            Assert.IsFalse(damageable.IsDead);

            damageable.TakeDamage(100f);
            Assert.AreEqual(0f, damageable.CurrentHealth);
            Assert.IsTrue(damageable.IsDead);
        }

        [Test]
        public void ShotgunStrategy_ImplementsWeaponStrategyContract_Properly()
        {
            IWeaponStrategy shotgun = new ShotgunWeaponStrategy();
            Assert.IsNotNull(shotgun, "ShotgunWeaponStrategy must implement IWeaponStrategy interface.");
            Assert.AreEqual("Tactical Shotgun", shotgun.StrategyName);

            IWeaponStrategy rifle = new RifleWeaponStrategy();
            Assert.IsNotNull(rifle, "RifleWeaponStrategy must implement IWeaponStrategy interface.");
            Assert.AreEqual("Assault Rifle", rifle.StrategyName);
        }

        [Test]
        public void InputService_EnableDisable_TogglesInputStateSafely()
        {
            IInputService inputService = new NewInputSystemService();
            Assert.IsTrue(inputService.IsInputEnabled);

            inputService.DisableInput();
            Assert.IsFalse(inputService.IsInputEnabled);
            Assert.AreEqual(Vector2.zero, inputService.MoveInput);
            Assert.AreEqual(Vector2.zero, inputService.LookInput);
            Assert.IsFalse(inputService.IsFirePressed);
            Assert.IsFalse(inputService.IsAimPressed);
            Assert.IsFalse(inputService.IsSprintPressed);
            Assert.IsFalse(inputService.IsWeapon1Pressed);
            Assert.IsFalse(inputService.IsWeapon2Pressed);
            Assert.AreEqual(0f, inputService.ScrollWheelDelta);

            inputService.EnableInput();
            Assert.IsTrue(inputService.IsInputEnabled);
        }

        [Test]
        public void LegacyInputService_EnableDisable_FunctionsProperly()
        {
            IInputService legacyService = new LegacyInputService();
            Assert.IsTrue(legacyService.IsInputEnabled);

            legacyService.DisableInput();
            Assert.IsFalse(legacyService.IsInputEnabled);
            Assert.AreEqual(Vector2.zero, legacyService.MoveInput);
            Assert.AreEqual(Vector2.zero, legacyService.LookInput);
            Assert.IsFalse(legacyService.IsWeapon1Pressed);
            Assert.IsFalse(legacyService.IsWeapon2Pressed);
            Assert.AreEqual(0f, legacyService.ScrollWheelDelta);

            legacyService.EnableInput();
            Assert.IsTrue(legacyService.IsInputEnabled);
        }

        [Test]
        public void WeaponStrategy_RaycastExecution_PreAllocatedBufferProducesZeroGC()
        {
            IWeaponStrategy rifle = new RifleWeaponStrategy();
            IWeaponStrategy shotgun = new ShotgunWeaponStrategy();
            RaycastHit[] hitBuffer = new RaycastHit[16];

            WeaponFireContext context = new WeaponFireContext
            {
                DefaultOrigin = Vector3.zero,
                ForwardDirection = Vector3.forward,
                Range = 100f,
                Damage = 35f,
                BulletSpeed = 220f,
                HitBuffer = hitBuffer,
                
                ShootableLayers = ~0
            };

            // Pre-warm JIT and static caches
            for (int i = 0; i < 20; i++)
            {
                rifle.ExecuteFire(in context);
                shotgun.ExecuteFire(in context);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long memoryBefore = GC.GetTotalMemory(false);

            // Execute 200 combat iterations with shared non-allocating buffer
            for (int i = 0; i < 200; i++)
            {
                rifle.ExecuteFire(in context);
                shotgun.ExecuteFire(in context);
            }

            long memoryAfter = GC.GetTotalMemory(false);
            long allocatedBytes = memoryAfter - memoryBefore;

            // In non-profiler EditMode test runner, memory growth during pure struct operations should be 0 or negligible (< 100 bytes from framework)
            Assert.LessOrEqual(allocatedBytes, 0, $"Weapon raycast execution must allocate 0 GC heap bytes, but allocated {allocatedBytes} bytes.");
        }

        [Test]
        public void PlayerWeaponController_DualWeaponSwitching_UpdatesAmmoAndInvokesEvents()
        {
            GameObject playerGo = new GameObject("Test_Player_Weapon");
            cleanupList.Add(playerGo);
            PlayerWeaponController weaponController = playerGo.AddComponent<PlayerWeaponController>();

            string lastWeaponName = null;
            int lastCurrentAmmo = -1;
            int lastMaxAmmo = -1;

            weaponController.OnWeaponChanged += name => lastWeaponName = name;
            weaponController.OnAmmoChanged += (curr, max) =>
            {
                lastCurrentAmmo = curr;
                lastMaxAmmo = max;
            };

            // Initial slot is 0: Assault Rifle
            Assert.AreEqual("Assault Rifle", weaponController.CurrentWeaponName);
            Assert.AreEqual(0, weaponController.CurrentWeaponSlot);

            // Switch to Shotgun (Slot 1)
            weaponController.SwitchToSlot(1);
            Assert.AreEqual(1, weaponController.CurrentWeaponSlot);
            Assert.AreEqual("Tactical Shotgun", weaponController.CurrentWeaponName);
            Assert.AreEqual(8, weaponController.MaxAmmo);
            Assert.AreEqual("Tactical Shotgun", lastWeaponName);
            Assert.AreEqual(8, lastMaxAmmo);

            // Switch back to Rifle (Slot 0)
            weaponController.SwitchToSlot(0);
            Assert.AreEqual(0, weaponController.CurrentWeaponSlot);
            Assert.AreEqual("Assault Rifle", weaponController.CurrentWeaponName);
            Assert.AreEqual(40, weaponController.MaxAmmo);
            Assert.AreEqual("Assault Rifle", lastWeaponName);
            Assert.AreEqual(40, lastMaxAmmo);
        }

        [Test]
        public void PlayerCharacterController_NotifierInterface_ExposesDualWeaponProperties()
        {
            GameObject playerGo = new GameObject("Test_PlayerCharacter");
            cleanupList.Add(playerGo);
            PlayerCharacterController character = playerGo.AddComponent<PlayerCharacterController>();
            IPlayerStatsNotifier notifier = character;

            Assert.IsNotNull(notifier.CurrentWeaponName);
            Assert.AreEqual("Assault Rifle", notifier.CurrentWeaponName);

            string receivedWeaponEvent = null;
            notifier.OnWeaponChanged += name => receivedWeaponEvent = name;

            character.Weapon.SwitchToSlot(1);
            Assert.AreEqual("Tactical Shotgun", notifier.CurrentWeaponName);
            Assert.AreEqual("Tactical Shotgun", receivedWeaponEvent);
        }
    }
}
