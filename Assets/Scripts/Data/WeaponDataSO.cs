using UnityEngine;

namespace AbandonedAsylum.Data
{
    public enum WeaponType
    {
        AssaultRifle = 0,
        Shotgun = 1
    }

    [CreateAssetMenu(fileName = "NewWeaponData", menuName = "Abandoned Asylum/Combat/Weapon Data", order = 1)]
    public class WeaponDataSO : ScriptableObject
    {
        [Header("Weapon Classification")]
        [SerializeField] private WeaponType weaponType = WeaponType.AssaultRifle;

        [Header("General Info")]
        [SerializeField] private string weaponName = "Assault Rifle";
        [SerializeField] private GameObject weaponPrefab;

        [Header("Grip & Transform Offsets")]
        [SerializeField] private Vector3 positionOffset = new Vector3(0.08f, 0.05f, 0.02f);
        [SerializeField] private Vector3 rotationOffset = new Vector3(-85f, -10f, 95f);

        [Header("Ballistics & Damage")]
        [SerializeField] private float fireRate = 0.11f;
        [SerializeField] private float damage = 35.0f;
        [SerializeField] private float range = 150.0f;
        [SerializeField] private float bulletSpeed = 220.0f;

        [Header("Ammo & Magazine")]
        [SerializeField] private int maxAmmo = 40;
        [SerializeField] private float reloadDuration = 3.2f;

        [Header("Recoil")]
        [SerializeField] private float verticalRecoil = 0.7f;
        [SerializeField] private float horizontalRecoil = 0.2f;

        [Header("Reload Timing (Normalized 0..1)")]
        [SerializeField] private float magOutTimeNormalized = 0.11f;
        [SerializeField] private float magInTimeNormalized = 0.48f;
        [SerializeField] private float chargeTimeNormalized = 0.77f;

        [Header("Audio (Optional Overrides)")]
        [SerializeField] private AudioClip shootSound;
        [SerializeField] private AudioClip reloadMagOutSound;
        [SerializeField] private AudioClip reloadMagInSound;
        [SerializeField] private AudioClip reloadChargeSound;

        public WeaponType Type => weaponType;
        public string WeaponName => weaponName;
        public GameObject WeaponPrefab => weaponPrefab;
        public Vector3 PositionOffset => positionOffset;
        public Vector3 RotationOffset => rotationOffset;
        public float FireRate => fireRate;
        public float Damage => damage;
        public float Range => range;
        public float BulletSpeed => bulletSpeed;
        public int MaxAmmo => maxAmmo;
        public float ReloadDuration => reloadDuration;
        public float VerticalRecoil => verticalRecoil;
        public float HorizontalRecoil => horizontalRecoil;
        public AudioClip ShootSound => shootSound;
        public AudioClip ReloadMagOutSound => reloadMagOutSound;
        public AudioClip ReloadMagInSound => reloadMagInSound;
        public AudioClip ReloadChargeSound => reloadChargeSound;
        public float MagOutTimeNormalized => magOutTimeNormalized;
        public float MagInTimeNormalized => magInTimeNormalized;
        public float ChargeTimeNormalized => chargeTimeNormalized;

        public static WeaponDataSO CreateShotgunInstance()
        {
            var so = ScriptableObject.CreateInstance<WeaponDataSO>();
            so.weaponType = WeaponType.Shotgun;
            so.weaponName = "Tactical Shotgun";
            so.fireRate = 0.65f;
            so.damage = 72.0f;
            so.range = 50.0f;
            so.bulletSpeed = 180.0f;
            so.maxAmmo = 8;
            so.reloadDuration = 2.8f;
            so.verticalRecoil = 1.4f;
            so.horizontalRecoil = 0.45f;
            return so;
        }
    }
}
