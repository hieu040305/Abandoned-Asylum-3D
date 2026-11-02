using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AbandonedAsylum.Core;

namespace AbandonedAsylum.Zombie
{
    /// <summary>
    /// Quản lý Object Pool cho Zombie bằng cách tái sử dụng ComponentPool<T> chuẩn,
    /// loại bỏ hoàn toàn việc duyệt tuyến tính O(N) và giảm thiểu GC allocation.
    /// </summary>
    [DisallowMultipleComponent]
    public class ZombiePool : MonoBehaviour
    {
        private static ZombiePool instance;
        private static bool isQuitting = false;

        public static ZombiePool Instance
        {
            get
            {
                if (isQuitting) return null;
                if (instance == null)
                {
                    instance = FindFirstObjectByType<ZombiePool>();
                    if (instance == null && Application.isPlaying)
                    {
                        GameObject go = new GameObject("[Zombie_ObjectPool]");
                        instance = go.AddComponent<ZombiePool>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        [Header("Pool Configuration")]
        [Tooltip("Số lượng Zombie khởi tạo sẵn trong bộ nhớ ngay khi bắt đầu")]
        [SerializeField] private int initialPoolSize = 12;

        [Tooltip("Số lượng Zombie tối đa được phép lưu trữ trong Pool")]
        [SerializeField] private int maxPoolSize = 24;

        [Tooltip("Prefab quái vật Zombie Parasite chuẩn")]
        [SerializeField] private GameObject defaultZombiePrefab;

        private ComponentPool<ZombieController> internalPool;
        private readonly HashSet<ZombieController> activeZombies = new HashSet<ZombieController>();
        private readonly List<ZombieController> fallenCorpses = new List<ZombieController>();
        private GameObject cachedPrefab;
        private ZombieController cachedZombieComponent;

        public int ActiveCount => activeZombies.Count;
        public int InactiveCount => internalPool != null ? internalPool.CountInactive : 0;
        public int TotalCount => internalPool != null ? internalPool.CountAll : 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            isQuitting = false;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            GameManager.OnSceneResetRequested -= ResetAllZombies;
            GameManager.OnSceneResetRequested += ResetAllZombies;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
            GameManager.OnSceneResetRequested -= ResetAllZombies;
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        public void RegisterPrefab(GameObject prefab)
        {
            if (prefab != null && (cachedPrefab != prefab || internalPool == null))
            {
                ResetAllZombies();
                if (internalPool != null)
                {
                    internalPool.Clear();
                    internalPool = null;
                }
                cachedPrefab = prefab;
                cachedZombieComponent = prefab.GetComponent<ZombieController>();
                InitializeInternalPool();
            }
        }

        private void EnsurePrefabLoaded()
        {
            if (cachedPrefab != null && cachedZombieComponent != null) return;

            if (defaultZombiePrefab != null)
            {
                cachedPrefab = defaultZombiePrefab;
                cachedZombieComponent = defaultZombiePrefab.GetComponent<ZombieController>();
                return;
            }

            cachedPrefab = Resources.Load<GameObject>("Zombie/Zombie_Parasite");
            if (cachedPrefab != null)
            {
                cachedZombieComponent = cachedPrefab.GetComponent<ZombieController>();
                return;
            }

#if UNITY_EDITOR
            cachedPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Zombie/Zombie_Parasite.prefab")
                        ?? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Zombie/Zombie_Parasite.prefab");
            if (cachedPrefab != null)
            {
                cachedZombieComponent = cachedPrefab.GetComponent<ZombieController>();
            }
#endif
        }

        private void InitializeInternalPool()
        {
            EnsurePrefabLoaded();
            if (cachedZombieComponent == null || internalPool != null) return;

            internalPool = new ComponentPool<ZombieController>(
                cachedZombieComponent,
                defaultCapacity: initialPoolSize,
                rootContainer: transform,
                onGet: z =>
                {
                    activeZombies.Add(z);
                },
                onRelease: z =>
                {
                    activeZombies.Remove(z);
                    z.StopAllCoroutines();
                    z.gameObject.SetActive(false);
                }
            );

            internalPool.Prewarm(initialPoolSize);
        }

        /// <summary>
        /// Lấy một Zombie từ ComponentPool và kích hoạt tại tọa độ yêu cầu (O(1) LIFO Stack).
        /// </summary>
        public ZombieController SpawnZombie(Vector3 position, Quaternion rotation, float health, GameObject fallbackPrefab = null)
        {
            if (internalPool == null)
            {
                if (fallbackPrefab != null) RegisterPrefab(fallbackPrefab);
                else InitializeInternalPool();
            }

            if (internalPool == null) return null;

            if (activeZombies.Count >= maxPoolSize)
            {
                // Giới hạn số lượng quái hoạt động đồng thời để bảo toàn ngân sách CPU
                return null;
            }

            ZombieController availableZombie = internalPool.Get();
            if (availableZombie == null) return null;

            Transform zTransform = availableZombie.transform;
            zTransform.position = position;
            zTransform.rotation = rotation;

            availableZombie.gameObject.SetActive(true);
            availableZombie.ResetForSpawn(health);

            if (availableZombie.Agent != null && availableZombie.Agent.isOnNavMesh)
            {
                availableZombie.Agent.Warp(position);
            }

            availableZombie.ForceChasePlayer();
            return availableZombie;
        }

        /// <summary>
        /// Thu hồi Zombie trở lại ComponentPool (Zero GC Heap Allocation).
        /// </summary>
        public void ReleaseZombie(ZombieController zombie)
        {
            if (zombie == null) return;

            activeZombies.Remove(zombie);
            fallenCorpses.Remove(zombie);

            if (internalPool != null)
            {
                internalPool.Release(zombie);
            }
            else
            {
                zombie.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Thông báo Zombie tử trận: loại khỏi danh sách quái đang hoạt động và ghi nhận xác nằm sàn.
        /// </summary>
        public void NotifyZombieDestroyed(ZombieController zombie)
        {
            if (zombie != null)
            {
                activeZombies.Remove(zombie);
                if (!fallenCorpses.Contains(zombie))
                {
                    fallenCorpses.Add(zombie);
                }
            }
        }

        /// <summary>
        /// Thu hồi toàn bộ quái vật và dọn dẹp xác khi đổi màn hoặc chơi lại mà không Destroy GameObject nào.
        /// </summary>
        public void ResetAllZombies()
        {
            for (int i = 0; i < fallenCorpses.Count; i++)
            {
                ZombieController corpse = fallenCorpses[i];
                if (corpse != null)
                {
                    if (internalPool != null)
                    {
                        internalPool.Release(corpse);
                    }
                    else
                    {
                        corpse.gameObject.SetActive(false);
                    }
                }
            }
            fallenCorpses.Clear();

            if (internalPool != null && activeZombies.Count > 0)
            {
                var toRelease = new List<ZombieController>(activeZombies);
                for (int i = 0; i < toRelease.Count; i++)
                {
                    if (toRelease[i] != null)
                    {
                        internalPool.Release(toRelease[i]);
                    }
                }
                activeZombies.Clear();
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(false);
            }
        }
    }
}