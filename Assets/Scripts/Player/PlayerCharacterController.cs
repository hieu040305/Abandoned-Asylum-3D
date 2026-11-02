using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Animations.Rigging;
using AbandonedAsylum.Data;
using AbandonedAsylum.Core;
using AbandonedAsylum.Interfaces;
using AbandonedAsylum.Player.Weapons;

namespace AbandonedAsylum.Player
{
    /// <summary>
    /// Điều phối các thành phần chức năng của người chơi theo mô hình Component-Based:
    /// - PlayerLocomotion: Di chuyển, chạy, nhảy và trọng lực
    /// - PlayerAimRigController: Căn chỉnh hướng ngắm cột sống và Animation Rigging Two-Bone IK
    /// - PlayerWeaponController: Quản lý vũ khí, nạp đạn, đường đạn và hiệu ứng phát hỏa
    /// - PlayerHealth: Quản lý sinh mệnh và sát thương
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerHealth))]
    [RequireComponent(typeof(PlayerLocomotion))]
    [RequireComponent(typeof(PlayerAimRigController))]
    [RequireComponent(typeof(PlayerWeaponController))]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class PlayerCharacterController : MonoBehaviour, IDamageable, IPlayerStatsNotifier
    {
        #region Serialized Fields (Bảo toàn dữ liệu đã cấu hình trong Scene và Prefabs)
        [Header("Locomotion Parameters")]
        [SerializeField] private float walkSpeed = 2.4f;
        [SerializeField] private float runSpeed = 4.8f;
        [SerializeField] private float sprintSpeed = 7.2f;
        [SerializeField] private float gravity = -18.0f;
        [SerializeField] private float jumpHeight = 1.4f;

        [Header("Weapon Data (ScriptableObject)")]
        [SerializeField] private WeaponDataSO weaponData;

        [Header("Weapon & Shooting Defaults")]
        [SerializeField] private GameObject riflePrefab;
        [SerializeField] private Transform rightHandBone;
        [SerializeField] private Vector3 weaponPositionOffset = new Vector3(0.08f, 0.05f, 0.02f);
        [SerializeField] private Vector3 weaponRotationOffset = new Vector3(-85f, -10f, 95f);
        [SerializeField] private float fireRate = 0.11f;
        [SerializeField] private float damage = 35.0f;
        [SerializeField] private float range = 150.0f;
        [SerializeField] private float bulletSpeed = 220.0f;
        [SerializeField] private LayerMask shootableLayers = ~0;

        [Header("Ammo & Magazine")]
        [SerializeField] private int maxAmmo = 40;
        [SerializeField] private int currentAmmo = 40;
        [SerializeField] private float reloadDuration = 3.2f;

        [Header("Points & References")]
        [FormerlySerializedAs("shootpointTransform")]
        [SerializeField] private Transform bulletSpawnPoint;
        [SerializeField] private Transform aimReferencePoint;

        [Header("Aim Alignment")]
        [SerializeField] private bool enableAimAlignment = true;
        [SerializeField] private Transform spine1Bone;
        [SerializeField] private Transform spine2Bone;
        [SerializeField] private float maxPitchAngle = 35.0f;
        [SerializeField] private float maxYawAngle = 35.0f;
        [SerializeField] private float minForwardAimDistance = 0.8f;

        [Header("Animation Rigging & Hand IK")]
        [SerializeField] private Rig handIKRig;
        [SerializeField] private TwoBoneIKConstraint leftHandIKConstraint;
        #endregion

        #region Modular Sub-Components
        private IGameStateService gameStateService;
        private IGameStateService GameStateService => gameStateService ??= (ServiceLocator.TryGet(out IGameStateService svc) ? svc : GameManager.Instance);

        private CharacterController controller;
        private Animator animator;
        private PlayerCameraController camController;
        private PlayerHealth playerHealth;
        private PlayerLocomotion locomotion;
        private PlayerAimRigController aimRigController;
        private PlayerWeaponController weaponController;
        #endregion

        #region Public Properties (Tương thích ngược 100% với UIManager và hệ thống bên ngoài)
        public int CurrentAmmo => weaponController != null ? weaponController.CurrentAmmo : currentAmmo;
        public int MaxAmmo => weaponController != null ? weaponController.MaxAmmo : maxAmmo;
        public bool IsReloading => weaponController != null && weaponController.IsReloading;
        public float ReloadTimer => weaponController != null ? weaponController.ReloadTimer : 0.0f;
        public float ReloadDuration => weaponController != null ? weaponController.ReloadDuration : reloadDuration;
        public bool IsMoving => locomotion != null && locomotion.IsMoving;
        public float CurrentHealth => playerHealth != null ? playerHealth.CurrentHealth : 100.0f;
        public float MaxHealth => playerHealth != null ? playerHealth.MaxHealth : 100.0f;
        public bool IsDead => playerHealth != null && playerHealth.IsDead;
        public WeaponDataSO CurrentWeaponData => weaponController != null ? weaponController.CurrentWeaponData : weaponData;
        public ImpactSoundType ImpactType => ImpactSoundType.Flesh;
        public LayerMask EffectiveShootableLayers => weaponController != null ? weaponController.EffectiveShootableLayers : shootableLayers;

        public PlayerLocomotion Locomotion => locomotion != null ? locomotion : (locomotion = GetComponent<PlayerLocomotion>() ?? gameObject.AddComponent<PlayerLocomotion>());
        public PlayerAimRigController AimRig => aimRigController != null ? aimRigController : (aimRigController = GetComponent<PlayerAimRigController>() ?? gameObject.AddComponent<PlayerAimRigController>());
        public PlayerWeaponController Weapon => weaponController != null ? weaponController : (weaponController = GetComponent<PlayerWeaponController>() ?? gameObject.AddComponent<PlayerWeaponController>());
        public PlayerHealth Health => playerHealth != null ? playerHealth : (playerHealth = GetComponent<PlayerHealth>() ?? gameObject.AddComponent<PlayerHealth>());
        public IWeaponStrategy CurrentWeaponStrategy => Weapon != null ? Weapon.CurrentStrategy : null;
        public string CurrentWeaponName => Weapon != null ? Weapon.CurrentWeaponName : "Assault Rifle";

        #region IPlayerStatsNotifier Implementation
        public event Action<float, float> OnHealthChanged
        {
            add { Health.OnHealthChanged += value; }
            remove { Health.OnHealthChanged -= value; }
        }

        public event Action<int, int> OnAmmoChanged
        {
            add { Weapon.OnAmmoChanged += value; }
            remove { Weapon.OnAmmoChanged -= value; }
        }

        public event Action<float, float, bool> OnReloadProgressChanged
        {
            add { Weapon.OnReloadProgressChanged += value; }
            remove { Weapon.OnReloadProgressChanged -= value; }
        }

        public event Action<string> OnWeaponChanged
        {
            add { Weapon.OnWeaponChanged += value; }
            remove { Weapon.OnWeaponChanged -= value; }
        }

        public event Action OnPlayerDied
        {
            add { Health.OnDeath += value; }
            remove { Health.OnDeath -= value; }
        }
        #endregion
        #endregion

        private void OnValidate()
        {
            if (shootableLayers.value == 0)
            {
                shootableLayers = ~LayerMask.GetMask("Ignore Raycast");
            }
        }

        private void Awake()
        {
            if (shootableLayers.value == 0)
            {
                shootableLayers = ~LayerMask.GetMask("Ignore Raycast");
            }

            controller = GetComponent<CharacterController>();
            animator = GetComponent<Animator>();
            camController = FindFirstObjectByType<PlayerCameraController>();

            // 1. Khởi tạo và liên kết các sub-components chuẩn Component-Based
            playerHealth = GetComponent<PlayerHealth>() ?? gameObject.AddComponent<PlayerHealth>();
            locomotion = GetComponent<PlayerLocomotion>() ?? gameObject.AddComponent<PlayerLocomotion>();
            aimRigController = GetComponent<PlayerAimRigController>() ?? gameObject.AddComponent<PlayerAimRigController>();
            weaponController = GetComponent<PlayerWeaponController>() ?? gameObject.AddComponent<PlayerWeaponController>();

            // 2. Chuyển giao các tham số cấu hình sang các component chuyên biệt
            locomotion.Initialize(controller, animator, camController);
            locomotion.SetLocomotionParameters(walkSpeed, runSpeed, sprintSpeed, gravity, jumpHeight);

            weaponController.Initialize(animator, camController);
            weaponController.SetWeaponConfig(
                weaponData, riflePrefab, rightHandBone, weaponPositionOffset, weaponRotationOffset,
                fireRate, damage, range, bulletSpeed, shootableLayers, maxAmmo, reloadDuration,
                bulletSpawnPoint, aimReferencePoint
            );

            aimRigController.EnableAimAlignment = enableAimAlignment;
            aimRigController.SetAimLimits(maxPitchAngle, maxYawAngle, minForwardAimDistance);
            aimRigController.SetupBonesAndRigs(animator, spine1Bone, spine2Bone, handIKRig, leftHandIKConstraint);
        }

        private void OnEnable()
        {
            ServiceLocator.RegisterService<IPlayerStatsNotifier>(this);
            PlayerRegistry.Register(transform, gameObject);
        }

        private void OnDisable()
        {
            ServiceLocator.UnregisterService<IPlayerStatsNotifier>();
            PlayerRegistry.Unregister(transform);
        }

        private void Start()
        {
            if (playerHealth != null)
            {
                playerHealth.OnDeath += OnPlayerDeath;
            }

            bool isPlaying = GameStateService != null && GameStateService.CurrentState == GameState.Playing;
            Cursor.lockState = isPlaying ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isPlaying;

            // Đảm bảo nhân vật sinh ra đúng tọa độ spawn point nếu có
            GameObject spawnObj = GameObject.FindWithTag("Respawn") ?? GameObject.Find("spawn");
            if (spawnObj != null && (transform.position - spawnObj.transform.position).sqrMagnitude > 4.0f)
            {
                if (controller != null) controller.enabled = false;
                transform.position = spawnObj.transform.position;
                transform.rotation = spawnObj.transform.rotation;
                if (controller != null) controller.enabled = true;
            }

            // Thiết lập vũ khí và liên kết điểm nhắm
            if (weaponController != null)
            {
                weaponController.SetupWeapon(animator);
                if (aimRigController != null)
                {
                    aimRigController.SetWeaponAimReferences(weaponController.BulletSpawnPoint, weaponController.AimReferencePoint);
                }
            }
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnDeath -= OnPlayerDeath;
            }
        }

        private void OnPlayerDeath()
        {
            GameStateService?.OnPlayerDied();
        }

        private void Update()
        {
            bool isDead = IsDead;
            bool isPaused = GameManager.IsGamePaused || (GameStateService != null && GameStateService.CurrentState != GameState.Playing);

            // Cập nhật IK trọng số cho bàn tay
            if (aimRigController != null)
            {
                bool isReloading = weaponController != null && weaponController.IsReloading;
                aimRigController.UpdateHandIK(isReloading, isDead);
            }

            if (isDead || isPaused) return;

            // Ủy quyền xử lý di chuyển và vũ khí
            if (locomotion != null)
            {
                locomotion.TickLocomotion(isDead, isPaused);
            }

            if (weaponController != null)
            {
                weaponController.TickWeapon(isDead, isPaused);
            }
        }

        private void LateUpdate()
        {
            if (IsDead) return;

            if (aimRigController != null && weaponController != null && locomotion != null)
            {
                aimRigController.AlignWeaponToCrosshair(
                    locomotion.IsSprinting,
                    weaponController.IsReloading,
                    weaponController.EffectiveShootableLayers,
                    weaponController.Range
                );
            }
        }

        #region Public Facade Actions
        public void StartReload()
        {
            if (weaponController != null)
            {
                weaponController.StartReload();
            }
        }

        public void EquipWeapon(WeaponDataSO newWeaponData)
        {
            if (weaponController != null)
            {
                weaponController.EquipWeapon(newWeaponData);
                if (aimRigController != null)
                {
                    aimRigController.SetWeaponAimReferences(weaponController.BulletSpawnPoint, weaponController.AimReferencePoint);
                }
            }
        }

        public void ApplyWeaponData(WeaponDataSO data)
        {
            if (weaponController != null)
            {
                weaponController.ApplyWeaponData(data);
            }
        }

        public void TakeDamage(float damageAmount)
        {
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageAmount);
            }
        }

        public void TakeDamage(float damageAmount, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageAmount, hitPoint, hitNormal);
            }
        }

        public void RestartGame()
        {
            GameStateService?.RetryMission();
        }

        public void SetWeaponStrategy(IWeaponStrategy strategy)
        {
            weaponController?.SetWeaponStrategy(strategy);
        }

        public void SwitchToWeaponSlot(int slot)
        {
            weaponController?.SwitchToSlot(slot);
        }
        #endregion
    }
}
