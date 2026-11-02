using System;
using AbandonedAsylum.Interfaces;
using AbandonedAsylum.Core;
using AbandonedAsylum.Core.Input;
using UnityEngine;
using AbandonedAsylum.Data;
using AbandonedAsylum.VFX;
using AbandonedAsylum.Audio;
using AbandonedAsylum.Player.Weapons;

namespace AbandonedAsylum.Player
{
    /// <summary>
    /// Chuyên trách quản lý vũ khí, nạp đạn, quỹ đạo đường đạn và hiệu ứng phát hỏa của Player.
    /// Tuân thủ nguyên tắc Single Responsibility Principle (SRP).
    /// </summary>
    [RequireComponent(typeof(WeaponFX))]
    [DisallowMultipleComponent]
    public class PlayerWeaponController : MonoBehaviour
    {
        private IInputService inputService;
        private IInputService InputService => inputService ??= (ServiceLocator.TryGet(out IInputService svc) ? svc : InputManagerService.Active);

        [Header("Weapon Data (ScriptableObject)")]
        [Tooltip("Cấu hình vũ khí dạng ScriptableObject. Nếu để trống sẽ sử dụng thông số mặc định bên dưới.")]
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

        [Header("Muzzle & Aim Points")]
        [SerializeField] private Transform bulletSpawnPoint;
        [SerializeField] private Transform aimReferencePoint;

        private bool isReloading = false;
        private float reloadTimer = 0.0f;
        private bool magOutSoundPlayed = false;
        private bool magInSoundPlayed = false;
        private bool chargeSoundPlayed = false;
        private float nextFireTime = 0.0f;

        private GameObject spawnedWeapon;
        private WeaponFX weaponFX;
        private Animator animator;
        private PlayerCameraController camController;
        private static readonly int AnimHashReload = Animator.StringToHash("Reload");
        private static readonly int AnimHashShoot = Animator.StringToHash("Shoot");
        private Camera mainCam;
        private readonly RaycastHit[] shootHitBuffer = new RaycastHit[8];

        public event Action<int, int> OnAmmoChanged;
        public event Action<string> OnWeaponChanged;
        public event Action<float, float, bool> OnReloadProgressChanged;

        private IWeaponStrategy weaponStrategy = new RifleWeaponStrategy();

        #region Public Properties
        [Header("Dual Weapon Inventory")]
        [SerializeField] private WeaponDataSO secondaryWeaponData;
        private int currentWeaponSlot = 0;
        private int slot0Ammo = 40;
        private int slot1Ammo = 8;
        private WeaponDataSO primaryWeaponData;

        public int CurrentWeaponSlot => currentWeaponSlot;
        public string CurrentWeaponName
        {
            get
            {
                if (weaponData != null && !string.IsNullOrEmpty(weaponData.WeaponName))
                    return weaponData.WeaponName;
                if (weaponStrategy != null && !string.IsNullOrEmpty(weaponStrategy.StrategyName))
                    return weaponStrategy.StrategyName;
                return currentWeaponSlot == 0 ? "Assault Rifle" : "Tactical Shotgun";
            }
        }
        public IWeaponStrategy CurrentStrategy => weaponStrategy;
        public int CurrentAmmo => currentAmmo;
        public int MaxAmmo => maxAmmo;
        public bool IsReloading => isReloading;
        public float ReloadTimer => reloadTimer;
        public float ReloadDuration => reloadDuration;
        public float Range => range;
        public float Damage => damage;
        public float FireRate => fireRate;
        public WeaponDataSO CurrentWeaponData => weaponData;
        public Transform BulletSpawnPoint => bulletSpawnPoint;
        public Transform AimReferencePoint => aimReferencePoint;
        public GameObject SpawnedWeapon => spawnedWeapon;
        public LayerMask EffectiveShootableLayers => shootableLayers.value != 0 ? shootableLayers : ~LayerMask.GetMask("Ignore Raycast");
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

            weaponFX = GetComponent<WeaponFX>() ?? gameObject.AddComponent<WeaponFX>();
            animator = GetComponent<Animator>();
            camController = FindFirstObjectByType<PlayerCameraController>();
            mainCam = Camera.main;

            primaryWeaponData = weaponData;
            if (secondaryWeaponData == null)
            {
                secondaryWeaponData = WeaponDataSO.CreateShotgunInstance();
            }

            slot0Ammo = maxAmmo;
            slot1Ammo = secondaryWeaponData != null ? secondaryWeaponData.MaxAmmo : 8;

            if (weaponData != null)
            {
                ApplyWeaponData(weaponData);
            }
        }

        private void Start()
        {
            OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
            OnWeaponChanged?.Invoke(CurrentWeaponName);
            OnReloadProgressChanged?.Invoke(0f, reloadDuration, isReloading);
        }

        public void Initialize(Animator anim, PlayerCameraController cam)
        {
            animator = anim;
            camController = cam;
        }

        public void SetWeaponStrategy(IWeaponStrategy strategy)
        {
            if (strategy != null)
            {
                weaponStrategy = strategy;
            }
        }

        public void SetWeaponConfig(WeaponDataSO data, GameObject prefab, Transform handBone, Vector3 posOffset, Vector3 rotOffset, float fRate, float dmg, float rng, float spd, LayerMask layers, int mAmmo, float rDuration, Transform spawnPt, Transform aimPt)
        {
            if (data != null) weaponData = data;
            if (prefab != null) riflePrefab = prefab;
            if (handBone != null) rightHandBone = handBone;
            weaponPositionOffset = posOffset;
            weaponRotationOffset = rotOffset;
            fireRate = fRate;
            damage = dmg;
            range = rng;
            bulletSpeed = spd;
            shootableLayers = layers;
            maxAmmo = mAmmo;
            reloadDuration = rDuration;
            if (spawnPt != null) bulletSpawnPoint = spawnPt;
            if (aimPt != null) aimReferencePoint = aimPt;

            if (weaponData != null)
            {
                ApplyWeaponData(weaponData);
            }
        }

        public void SetupWeapon(Animator anim = null)
        {
            if (anim != null) animator = anim;

            if (rightHandBone == null && animator != null)
            {
                rightHandBone = animator.GetBoneTransform(HumanBodyBones.RightHand);
                if (rightHandBone == null)
                {
                    Transform t = transform.Find("mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:RightShoulder/mixamorig:RightArm/mixamorig:RightForeArm/mixamorig:RightHand");
                    rightHandBone = t != null ? t : transform;
                }
            }

            if (rightHandBone != null)
            {
                Transform existingWeapon = rightHandBone.Find("Kalashnicolt_Equipped") ?? rightHandBone.Find("Kalashnicolt");
                if (existingWeapon == null && rightHandBone.childCount > 0)
                {
                    existingWeapon = rightHandBone.GetChild(0);
                }

                if (existingWeapon != null)
                {
                    spawnedWeapon = existingWeapon.gameObject;
                }
                else if (riflePrefab != null)
                {
                    spawnedWeapon = Instantiate(riflePrefab, rightHandBone);
                    spawnedWeapon.name = "Kalashnicolt_Equipped";
                    spawnedWeapon.transform.localPosition = weaponPositionOffset;
                    spawnedWeapon.transform.localRotation = Quaternion.Euler(weaponRotationOffset);
                }

                if (spawnedWeapon != null)
                {
                    WeaponSocketDefinition socketDef = spawnedWeapon.GetComponent<WeaponSocketDefinition>();
                    if (socketDef != null)
                    {
                        if (bulletSpawnPoint == null) bulletSpawnPoint = socketDef.MuzzlePoint;
                        if (aimReferencePoint == null) aimReferencePoint = socketDef.AimReferencePoint;
                    }
                    else
                    {
                        if (bulletSpawnPoint == null)
                        {
                            bulletSpawnPoint = spawnedWeapon.transform.Find("shootp (1)")
                                            ?? spawnedWeapon.transform.Find("shootpoint")
                                            ?? spawnedWeapon.transform.Find("Muzzle")
                                            ?? spawnedWeapon.transform;
                        }
                        if (aimReferencePoint == null)
                        {
                            aimReferencePoint = bulletSpawnPoint;
                        }
                    }
                }
            }
        }

        public void TickWeapon(bool isDead, bool isPaused)
        {
            if (isDead || isPaused) return;

            HandleWeaponSwitching();
            HandleReloadTimer();
            HandleShooting();
        }

        private void HandleWeaponSwitching()
        {
            if (isReloading) return;

            if (InputService.IsWeapon1Pressed && currentWeaponSlot != 0)
            {
                SwitchToSlot(0);
            }
            else if (InputService.IsWeapon2Pressed && currentWeaponSlot != 1)
            {
                SwitchToSlot(1);
            }
            else if (Mathf.Abs(InputService.ScrollWheelDelta) > 0.1f)
            {
                int targetSlot = currentWeaponSlot == 0 ? 1 : 0;
                SwitchToSlot(targetSlot);
            }
        }

        public void SwitchToSlot(int slot)
        {
            if (slot == currentWeaponSlot) return;

            if (currentWeaponSlot == 0) slot0Ammo = currentAmmo;
            else slot1Ammo = currentAmmo;

            currentWeaponSlot = slot;

            if (currentWeaponSlot == 0)
            {
                weaponStrategy = new RifleWeaponStrategy();
                weaponData = primaryWeaponData;
                if (primaryWeaponData != null)
                {
                    ApplyWeaponData(primaryWeaponData);
                }
                else
                {
                    fireRate = 0.11f;
                    damage = 35.0f;
                    range = 150.0f;
                    bulletSpeed = 220.0f;
                    maxAmmo = 40;
                    reloadDuration = 3.2f;
                }
                currentAmmo = slot0Ammo;
            }
            else
            {
                weaponStrategy = new ShotgunWeaponStrategy();
                weaponData = secondaryWeaponData;
                if (secondaryWeaponData != null)
                {
                    ApplyWeaponData(secondaryWeaponData);
                }
                else
                {
                    fireRate = 0.65f;
                    damage = 72.0f;
                    range = 50.0f;
                    bulletSpeed = 180.0f;
                    maxAmmo = 8;
                    reloadDuration = 2.8f;
                }
                currentAmmo = slot1Ammo;
            }

            GameAudioFX.PlayRifleReloadCharge(transform.position);
            OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
            OnWeaponChanged?.Invoke(CurrentWeaponName);
            OnReloadProgressChanged?.Invoke(0f, reloadDuration, false);
        }


        public void StartReload()
        {
            if (isReloading || currentAmmo >= maxAmmo) return;

            isReloading = true;
            reloadTimer = reloadDuration;
            magOutSoundPlayed = false;
            magInSoundPlayed = false;
            chargeSoundPlayed = false;

            if (animator != null)
            {
                animator.SetTrigger(AnimHashReload);
            }
        }

        private void HandleReloadTimer()
        {
            if (isReloading)
            {
                reloadTimer -= Time.deltaTime;
                float elapsed = reloadDuration - reloadTimer;
                OnReloadProgressChanged?.Invoke(elapsed, reloadDuration, true);

                float magOutThreshold = (weaponData != null ? weaponData.MagOutTimeNormalized : 0.11f) * reloadDuration;
                float magInThreshold = (weaponData != null ? weaponData.MagInTimeNormalized : 0.48f) * reloadDuration;
                float chargeThreshold = (weaponData != null ? weaponData.ChargeTimeNormalized : 0.77f) * reloadDuration;

                if (elapsed >= magOutThreshold && !magOutSoundPlayed)
                {
                    magOutSoundPlayed = true;
                    if (weaponData != null && weaponData.ReloadMagOutSound != null)
                    {
                        GameAudioFX.PlayClipAtPoint(weaponData.ReloadMagOutSound, transform.position);
                    }
                    else
                    {
                        GameAudioFX.PlayRifleReloadMagOut(transform.position);
                    }
                }
                else if (elapsed >= magInThreshold && !magInSoundPlayed)
                {
                    magInSoundPlayed = true;
                    if (weaponData != null && weaponData.ReloadMagInSound != null)
                    {
                        GameAudioFX.PlayClipAtPoint(weaponData.ReloadMagInSound, transform.position);
                    }
                    else
                    {
                        GameAudioFX.PlayRifleReloadMagIn(transform.position);
                    }
                }
                else if (elapsed >= chargeThreshold && !chargeSoundPlayed)
                {
                    chargeSoundPlayed = true;
                    if (weaponData != null && weaponData.ReloadChargeSound != null)
                    {
                        GameAudioFX.PlayClipAtPoint(weaponData.ReloadChargeSound, transform.position);
                    }
                    else
                    {
                        GameAudioFX.PlayRifleReloadCharge(transform.position);
                    }
                }

                if (reloadTimer <= 0.0f)
                {
                    currentAmmo = maxAmmo;
                    isReloading = false;
                    if (currentWeaponSlot == 0) slot0Ammo = maxAmmo;
                    else slot1Ammo = maxAmmo;
                    OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
                    OnReloadProgressChanged?.Invoke(reloadDuration, reloadDuration, false);
                }
            }

            if (InputService.IsReloadPressed && currentAmmo < maxAmmo && !isReloading)
            {
                StartReload();
            }
        }

        private void HandleShooting()
        {
            if (InputService.IsFirePressed && Time.time >= nextFireTime)
            {
                if (isReloading) return;

                if (currentAmmo <= 0)
                {
                    StartReload();
                    return;
                }

                nextFireTime = Time.time + fireRate;
                currentAmmo--;
                if (currentWeaponSlot == 0) slot0Ammo = currentAmmo;
                else slot1Ammo = currentAmmo;
                OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
            OnWeaponChanged?.Invoke(CurrentWeaponName);

                FireBullet();

                if (animator != null) animator.SetTrigger(AnimHashShoot);

                if (currentAmmo <= 0)
                {
                    StartReload();
                }
            }
        }

        public void FireBullet()
        {
            Transform spawnPoint = bulletSpawnPoint != null ? bulletSpawnPoint : (spawnedWeapon != null ? spawnedWeapon.transform : transform);
            Vector2 mouseScreen = InputService != null ? InputService.MousePosition : (Vector2)Input.mousePosition;

            float vRecoil = weaponData != null ? weaponData.VerticalRecoil : 0.7f;
            float hRecoil = weaponData != null ? weaponData.HorizontalRecoil : 0.2f;

            WeaponFireContext context = new WeaponFireContext
            {
                BulletSpawnPoint = spawnPoint,
                DefaultOrigin = transform.position + Vector3.up * 1.5f,
                ForwardDirection = transform.forward,
                Camera = mainCam != null ? mainCam : (mainCam = Camera.main),
                MouseScreenPosition = mouseScreen,
                IsCursorLocked = Cursor.lockState == CursorLockMode.Locked,
                ShootableLayers = EffectiveShootableLayers,
                Range = range,
                Damage = damage,
                BulletSpeed = bulletSpeed,
                HitBuffer = shootHitBuffer,
                WeaponFX = weaponFX,
                CameraController = camController,
                VerticalRecoil = vRecoil,
                HorizontalRecoil = hRecoil,
                PlayerTransform = transform
            };

            // Ủy quyền thực thi phát hỏa cho Weapon Strategy Pattern (Open/Closed Principle)
            if (weaponStrategy == null)
            {
                weaponStrategy = new RifleWeaponStrategy();
            }
            weaponStrategy.ExecuteFire(in context);
        }

        public void ApplyWeaponData(WeaponDataSO data)
        {
            if (data == null) return;
            weaponData = data;
            if (data.WeaponPrefab != null) riflePrefab = data.WeaponPrefab;
            weaponPositionOffset = data.PositionOffset;
            weaponRotationOffset = data.RotationOffset;
            fireRate = data.FireRate;
            damage = data.Damage;
            range = data.Range;
            bulletSpeed = data.BulletSpeed;
            maxAmmo = data.MaxAmmo;
            currentAmmo = Mathf.Min(currentAmmo, maxAmmo);
            reloadDuration = data.ReloadDuration;

            if (data.Type == WeaponType.Shotgun)
            {
                weaponStrategy = new ShotgunWeaponStrategy();
            }
            else
            {
                weaponStrategy = new RifleWeaponStrategy();
            }
        }

        public void EquipWeapon(WeaponDataSO newWeaponData)
        {
            if (newWeaponData == null) return;
            ApplyWeaponData(newWeaponData);

            if (spawnedWeapon != null)
            {
                Destroy(spawnedWeapon);
                spawnedWeapon = null;
                bulletSpawnPoint = null;
            }

            SetupWeapon();
        }


    }
}
