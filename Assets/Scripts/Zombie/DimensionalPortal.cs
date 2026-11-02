using AbandonedAsylum.Audio;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AbandonedAsylum.Core;
using AbandonedAsylum.Interfaces;
using AbandonedAsylum.VFX;

namespace AbandonedAsylum.Zombie
{
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(DimensionalPortalVFX))]
    public class DimensionalPortal : MonoBehaviour, IDamageable
    {
        [Header("Portal Combat & Health")]
        [Tooltip("Lượng máu tối đa của cánh cổng tròn (Mặc định: 800 HP)")]
        [SerializeField] private float maxHealth = 800.0f;

        [Tooltip("Máu hiện tại của cánh cổng tròn")]
        [SerializeField] private float currentHealth = 800.0f;

        private float lastDamageTime = -10.0f;
        private bool isDestroyed = false;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDestroyed => isDestroyed;
        public bool IsDead => isDestroyed;
        public ImpactSoundType ImpactType => ImpactSoundType.Portal;

        [Header("Zombie Spawning")]
        [Tooltip("Prefab quái vật Zombie Parasite hoàn chỉnh")]
        [SerializeField] private GameObject zombiePrefab;

        [Tooltip("Khoảng thời gian giữa các lần sinh 1 con quái mới (giây, mặc định 15s)")]
        [SerializeField] private float spawnInterval = 15.0f;

        [Tooltip("Máu của Zombie sinh ra từ cánh cổng này")]
        [SerializeField] private float zombieHealth = 100.0f;

        [Tooltip("Số lượng quái sống tối đa cùng lúc từ cánh cổng này (Tối ưu hiệu năng: 6 con/cổng)")]
        [SerializeField] private int maxZombiesAlive = 6;

        [Tooltip("Sinh ngay 1 con quái khi cổng bắt đầu hoạt động trong trận")]
        [SerializeField] private bool spawnOnStart = true;

        [Tooltip("Số lượng cổng tối đa được kích hoạt ngẫu nhiên mỗi lượt chơi (Mặc định: 3 cổng)")]
        [SerializeField] private int maxActivePortalsInScene = 3;

        [Header("Aesthetics - Dark Abyssal Circular Portal")]
        [SerializeField] private Color deepRedBrownColor = new Color(0.25f, 0.08f, 0.03f, 1.0f);
        [SerializeField] private Color brightRedBrownGlow = new Color(0.72f, 0.22f, 0.07f, 1.0f);
        [SerializeField] private Color darkRimColor = new Color(0.12f, 0.12f, 0.14f, 1.0f);
        [SerializeField] private Color hitFlashColor = new Color(0.65f, 0.15f, 0.15f, 1.0f);

        private float nextSpawnTime = 0.0f;
        private readonly List<ZombieController> activeZombies = new List<ZombieController>();
        private AudioSource audioSource;
        private CapsuleCollider portalCollider;
        private DimensionalPortalVFX portalVFX;
        private IGameStateService gameStateService;
        private IGameStateService GameStateService => gameStateService ??= (ServiceLocator.TryGet(out IGameStateService svc) ? svc : GameManager.Instance);

        private static bool hasCoordinatedRandomPortals = false;

        public static void ResetSceneStatics()
        {
            hasCoordinatedRandomPortals = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            hasCoordinatedRandomPortals = false;
            RegisteredPortals.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoadedInit()
        {
            hasCoordinatedRandomPortals = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterPortalGameEvents()
        {
            GameManager.OnSceneResetRequested -= ResetSceneStatics;
            GameManager.OnSceneResetRequested += ResetSceneStatics;
            GameManager.OnCoordinatePortalsRequested -= CoordinateRandomActivePortals;
            GameManager.OnCoordinatePortalsRequested += CoordinateRandomActivePortals;
        }

        private void Awake()
        {
            RegisterPortal(this);
            currentHealth = maxHealth;
            SetupCollider();
            SetupAudio();
            ValidateAndLoadPrefab();

            portalVFX = GetComponent<DimensionalPortalVFX>();
            if (portalVFX == null) portalVFX = gameObject.AddComponent<DimensionalPortalVFX>();
            portalVFX.BuildVisuals(deepRedBrownColor, brightRedBrownGlow, darkRimColor, hitFlashColor);
        }

        private void OnDestroy()
        {
            UnregisterPortal(this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                currentHealth = maxHealth;
            }
        }
#endif

        private void SetupCollider()
        {
            portalCollider = GetComponent<CapsuleCollider>();
            if (portalCollider == null) portalCollider = gameObject.AddComponent<CapsuleCollider>();

            portalCollider.center = new Vector3(0f, 1.15f, 0.04f);
            portalCollider.radius = 1.10f;
            portalCollider.height = 2.30f;
            portalCollider.direction = 1;
        }

        private void SetupAudio()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f;
            audioSource.minDistance = 2.0f;
            audioSource.maxDistance = 20.0f;
            audioSource.playOnAwake = false;
            audioSource.loop = true;
        }

        private void ValidateAndLoadPrefab()
        {
            if (zombiePrefab == null || zombiePrefab.GetComponent<ZombieController>() == null)
            {
                zombiePrefab = Resources.Load<GameObject>("Zombie/Zombie_Parasite");
                if (zombiePrefab != null && zombiePrefab.GetComponent<ZombieController>() != null) return;
            }
            if (zombiePrefab == null || zombiePrefab.GetComponent<ZombieController>() == null)
            {
#if UNITY_EDITOR
                GameObject loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Zombie/Zombie_Parasite.prefab")
                                 ?? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Zombie/Zombie_Parasite.prefab");
                if (loaded != null && loaded.GetComponent<ZombieController>() != null)
                {
                    zombiePrefab = loaded;
                }
#endif
            }
        }

        public void BuildVisuals()
        {
            if (portalVFX == null) portalVFX = GetComponent<DimensionalPortalVFX>() ?? gameObject.AddComponent<DimensionalPortalVFX>();
            portalVFX.BuildVisuals(deepRedBrownColor, brightRedBrownGlow, darkRimColor, hitFlashColor);
        }

        private void Start()
        {
            ValidateAndLoadPrefab();
            EnsureAllSpawngatesActive();

            if (!hasCoordinatedRandomPortals)
            {
                hasCoordinatedRandomPortals = true;
                CoordinateRandomActivePortals(maxActivePortalsInScene);
            }

            if (!gameObject.activeInHierarchy) return;

            bool isPlaying = GameStateService != null && GameStateService.CurrentState == GameState.Playing;
            if (zombiePrefab != null)
            {
                ZombiePool.Instance.RegisterPrefab(zombiePrefab);
            }

            if (spawnOnStart && isPlaying)
            {
                if (GameStateService != null && GameStateService.CurrentState == GameState.Playing)
                {
                    SpawnZombie();
                }
            }

            nextSpawnTime = Time.time + spawnInterval;
        }

        private static readonly List<DimensionalPortal> RegisteredPortals = new List<DimensionalPortal>(8);

        public static void RegisterPortal(DimensionalPortal portal)
        {
            if (portal != null && !RegisteredPortals.Contains(portal))
            {
                RegisteredPortals.Add(portal);
            }
        }

        public static void UnregisterPortal(DimensionalPortal portal)
        {
            if (portal != null)
            {
                RegisteredPortals.Remove(portal);
            }
        }

        public static void CoordinateRandomActivePortals(int targetCount = 3)
        {
            if (RegisteredPortals.Count == 0)
            {
                var found = FindObjectsOfType<DimensionalPortal>(true);
                for (int f = 0; f < found.Length; f++)
                {
                    RegisterPortal(found[f]);
                }
            }

            RegisteredPortals.RemoveAll(p => p == null);
            if (RegisteredPortals.Count == 0) return;

            // Fisher-Yates shuffle O(N)
            for (int i = 0; i < RegisteredPortals.Count; i++)
            {
                int rnd = UnityEngine.Random.Range(i, RegisteredPortals.Count);
                var temp = RegisteredPortals[i];
                RegisteredPortals[i] = RegisteredPortals[rnd];
                RegisteredPortals[rnd] = temp;
            }

            int activeCount = Mathf.Min(targetCount, RegisteredPortals.Count);
            for (int i = 0; i < activeCount; i++)
            {
                RegisteredPortals[i].gameObject.SetActive(true);
            }

            for (int i = activeCount; i < RegisteredPortals.Count; i++)
            {
                RegisteredPortals[i].gameObject.SetActive(false);
            }
        }

        public static DimensionalPortal[] EnsureAllSpawngatesActive()
        {
            if (RegisteredPortals.Count == 0)
            {
                var found = FindObjectsOfType<DimensionalPortal>(true);
                for (int f = 0; f < found.Length; f++)
                {
                    RegisterPortal(found[f]);
                }
            }
            return RegisteredPortals.ToArray();
        }

        private void Update()
        {
            if (isDestroyed) return;

            bool isHit = (Time.time - lastDamageTime) < 0.15f;
            float pulse = (Mathf.Sin(Time.time * 3.6f) + 1.0f) * 0.5f;

            if (portalVFX != null)
            {
                portalVFX.UpdateVFX(pulse, isHit, hitFlashColor);
            }

            if (GameStateService == null || GameStateService.CurrentState != GameState.Playing)
            {
                return;
            }

            if (Time.time >= nextSpawnTime)
            {
                nextSpawnTime = Time.time + spawnInterval;

                for (int i = activeZombies.Count - 1; i >= 0; i--)
                {
                    if (activeZombies[i] == null || !activeZombies[i].gameObject.activeInHierarchy || activeZombies[i].IsDead)
                    {
                        activeZombies.RemoveAt(i);
                    }
                }

                if (activeZombies.Count < maxZombiesAlive)
                {
                    SpawnZombie();
                }
            }
        }

        public void TakeDamage(float damageAmount, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (isDestroyed) return;
            GameAudioFX.PlayBulletPortalHit(hitPoint);
            TakeDamage(damageAmount);
        }

        public void TakeDamage(float damageAmount)
        {
            if (isDestroyed) return;

            currentHealth = Mathf.Max(0f, currentHealth - damageAmount);
            lastDamageTime = Time.time;

            if (portalVFX != null)
            {
                portalVFX.PlayHitSpark(transform.position, transform.forward);
            }

            if (currentHealth <= 0f)
            {
                CollapseAndDestroyPortal();
            }
        }

        private void CollapseAndDestroyPortal()
        {
            if (isDestroyed) return;
            isDestroyed = true;

            if (portalCollider != null) portalCollider.enabled = false;

            GameAudioFX.PlayPortalCollapse(transform.position);

            GameStateService?.OnPortalDestroyed();

            if (portalVFX != null)
            {
                portalVFX.PlayCollapseSequence(transform, () =>
                {
                    Destroy(gameObject);
                });
            }
            else
            {
                Destroy(gameObject, 0.1f);
            }
        }

        public void SpawnZombie()
        {
            ValidateAndLoadPrefab();
            if (zombiePrefab == null) return;

            Vector3 spawnPos = transform.position + transform.forward * 1.05f;
            spawnPos.y = Mathf.Max(spawnPos.y, transform.position.y);

            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 6.0f, NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }

            ZombieController zCtrl = ZombiePool.Instance.SpawnZombie(spawnPos, transform.rotation, zombieHealth, zombiePrefab);
            if (zCtrl != null && !activeZombies.Contains(zCtrl))
            {
                activeZombies.Add(zCtrl);
            }
        }

        public void SetPortalHealth(float hp)
        {
            maxHealth = hp;
            currentHealth = hp;
        }

        public void SetSpawnInterval(float seconds)
        {
            spawnInterval = Mathf.Max(1.0f, seconds);
        }

        public void SetZombieHealth(float hp)
        {
            zombieHealth = Mathf.Max(10.0f, hp);
        }
    }
}
