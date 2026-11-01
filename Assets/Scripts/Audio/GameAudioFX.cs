using AbandonedAsylum.Interfaces;
using AbandonedAsylum.Core;
using System.Collections.Generic;
using UnityEngine;

namespace AbandonedAsylum.Audio
{
    public class GameAudioFX : MonoBehaviour, IAudioService
    {
        private static GameAudioFX instance;
        private static bool isApplicationQuitting = false;

        public static GameAudioFX Instance
        {
            get
            {
                if (isApplicationQuitting) return null;
                if (instance == null)
                {
                    instance = FindFirstObjectByType<GameAudioFX>();
                    if (instance == null && !isApplicationQuitting)
                    {
                        GameObject go = new GameObject("GameAudioFX_Manager");
                        instance = go.AddComponent<GameAudioFX>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
                ServiceLocator.UnregisterService<IAudioService>();
            isApplicationQuitting = false;
            poolSources = null;
            currentPoolIndex = 0;
            poolContainer = null;
        }

        private void OnApplicationQuit()
        {
            isApplicationQuitting = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        [Header("Rifle Audio Clips")]
        [SerializeField] private AudioClip rifleShootClip;
        [SerializeField] private AudioClip[] rifleShootVariations;
        [SerializeField] private AudioClip rifleReloadMagOutClip;
        [SerializeField] private AudioClip rifleReloadMagInClip;
        [SerializeField] private AudioClip rifleReloadChargeClip;

        [Header("Zombie Audio Clips (Tybug Studios Studio Pack)")]
        [SerializeField] private AudioClip[] zombieGrowlClips;
        [SerializeField] private AudioClip[] zombieAttackClips;
        [SerializeField] private AudioClip[] zombieHurtClips;
        [SerializeField] private AudioClip[] zombieDeathClips;

        [Header("Other Audio Clips")]
        [SerializeField] private AudioClip clawSwipeClip;
        [SerializeField] private AudioClip clawHitClip;
        [SerializeField] private AudioClip bulletHitFleshClip;
        [SerializeField] private AudioClip bulletHitPortalClip;
        [SerializeField] private AudioClip portalCollapseClip;
        [SerializeField] private AudioClip bulletHitConcreteClip;
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip buttonHoverClip;

        #region Zero-GC High-Performance Audio Source Pool
        private const int AudioPoolSize = 16;
        private static AudioSource[] poolSources;
        private static int currentPoolIndex = 0;
        private static GameObject poolContainer;

        private void InitializePool()
        {
            if (poolSources != null && poolSources.Length == AudioPoolSize && poolContainer != null) return;

            if (poolContainer == null)
            {
                poolContainer = new GameObject("[AudioSource_ObjectPool]");
                poolContainer.transform.SetParent(transform, false);
            }

            poolSources = new AudioSource[AudioPoolSize];
            for (int i = 0; i < AudioPoolSize; i++)
            {
                GameObject channelObj = new GameObject($"AudioChannel_{i:D2}");
                channelObj.transform.SetParent(poolContainer.transform, false);
                AudioSource source = channelObj.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.rolloffMode = AudioRolloffMode.Linear;
                poolSources[i] = source;
            }
            currentPoolIndex = 0;
        }
        #endregion

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            ServiceLocator.RegisterService<IAudioService>(this);

            InitializePool();
            LoadClips();
        }

        private void LoadClips()
        {
            // 1. Súng trường (Assault Rifle)
            List<AudioClip> shots = new List<AudioClip>();
            AudioClip shot1 = Resources.Load<AudioClip>("Audio/rifle_shoot");
            AudioClip shot2 = Resources.Load<AudioClip>("Audio/rifle_shoot_02");
            AudioClip shot3 = Resources.Load<AudioClip>("Audio/rifle_shoot_03");
            AudioClip defaultShot = Resources.Load<AudioClip>("Audio/rifle_shoot");

            if (shot1 != null) shots.Add(shot1);
            if (shot2 != null) shots.Add(shot2);
            if (shot3 != null) shots.Add(shot3);
            if (shots.Count == 0 && defaultShot != null) shots.Add(defaultShot);

            rifleShootVariations = shots.ToArray();
            rifleShootClip = defaultShot ?? (shots.Count > 0 ? shots[0] : null);

            if (rifleReloadMagOutClip == null) rifleReloadMagOutClip = Resources.Load<AudioClip>("Audio/rifle_reload_magout");
            if (rifleReloadMagInClip == null) rifleReloadMagInClip = Resources.Load<AudioClip>("Audio/rifle_reload_magin");
            if (rifleReloadChargeClip == null) rifleReloadChargeClip = Resources.Load<AudioClip>("Audio/rifle_reload_charge");

            // 2. Quái vật Zombie (Tybug Studios Studio Voice Pack)
            List<AudioClip> growls = new List<AudioClip>();
            AddClipIfValid(growls, "Audio/Zombie/zombie_growl_01");
            AddClipIfValid(growls, "Audio/Zombie/zombie_growl_02");
            AddClipIfValid(growls, "Audio/Zombie/zombie_moan_01");
            AddClipIfValid(growls, "Audio/zombie_growl");
            zombieGrowlClips = growls.ToArray();

            List<AudioClip> attacks = new List<AudioClip>();
            AddClipIfValid(attacks, "Audio/Zombie/zombie_attack_01");
            AddClipIfValid(attacks, "Audio/Zombie/zombie_attack_02");
            AddClipIfValid(attacks, "Audio/Zombie/zombie_hiss_01");
            AddClipIfValid(attacks, "Audio/zombie_attack");
            zombieAttackClips = attacks.ToArray();

            List<AudioClip> hurts = new List<AudioClip>();
            AddClipIfValid(hurts, "Audio/Zombie/zombie_hurt_01");
            AddClipIfValid(hurts, "Audio/Zombie/zombie_hiss_01");
            AddClipIfValid(hurts, "Audio/Zombie/zombie_attack_02");
            zombieHurtClips = hurts.ToArray();

            List<AudioClip> deaths = new List<AudioClip>();
            AddClipIfValid(deaths, "Audio/Zombie/zombie_death_01");
            AddClipIfValid(deaths, "Audio/Zombie/zombie_death_02");
            AddClipIfValid(deaths, "Audio/zombie_death");
            zombieDeathClips = deaths.ToArray();

            // 3. Các âm thanh khác
            clawSwipeClip = Resources.Load<AudioClip>("Audio/claw_swipe") ?? Resources.Load<AudioClip>("Audio/Zombie/zombie_hiss_01") ?? Resources.Load<AudioClip>("Audio/zombie_attack");
            clawHitClip = Resources.Load<AudioClip>("Audio/claw_hit") ?? Resources.Load<AudioClip>("Audio/Zombie/zombie_hurt_01") ?? Resources.Load<AudioClip>("Audio/zombie_attack");
            bulletHitFleshClip = Resources.Load<AudioClip>("Audio/bullet_hit_flesh") ?? Resources.Load<AudioClip>("Audio/Zombie/zombie_hurt_01") ?? Resources.Load<AudioClip>("Audio/zombie_attack");
            bulletHitPortalClip = Resources.Load<AudioClip>("Audio/bullet_hit_portal") ?? Resources.Load<AudioClip>("Audio/rifle_aim");
            portalCollapseClip = Resources.Load<AudioClip>("Audio/portal_collapse");
            if (buttonClickClip == null) buttonClickClip = Resources.Load<AudioClip>("Bloodlines/Audio/Click Button SFX");
            if (buttonHoverClip == null) buttonHoverClip = Resources.Load<AudioClip>("Bloodlines/Audio/Hover Button SFX");
            bulletHitConcreteClip = Resources.Load<AudioClip>("Audio/bullet_hit_concrete") ?? Resources.Load<AudioClip>("Audio/rifle_aim");
        }

        private void AddClipIfValid(List<AudioClip> list, string resourcePath)
        {
            AudioClip clip = Resources.Load<AudioClip>(resourcePath);
            if (clip != null && !list.Contains(clip))
            {
                list.Add(clip);
            }
        }

        public static void PlayRifleShoot(Vector3 pos, AudioSource directSource = null)
        {
            var inst = Instance;
            AudioClip clip = null;

            if (inst.rifleShootVariations != null && inst.rifleShootVariations.Length > 0)
            {
                clip = inst.rifleShootVariations[Random.Range(0, inst.rifleShootVariations.Length)];
            }
            if (clip == null) clip = inst.rifleShootClip;
            if (clip == null) return;

            if (directSource != null)
            {
                directSource.pitch = Random.Range(0.96f, 1.04f);
                directSource.PlayOneShot(clip, 1.0f);
            }
            else
            {
                PlayClipAtPoint(clip, pos, 0.95f, 2.0f, 40.0f, 0.0f);
            }
        }

        public static void PlayRifleReloadMagOut(Vector3 pos, AudioSource directSource = null)
        {
            AudioClip clip = Instance.rifleReloadMagOutClip;
            if (clip == null) return;

            if (directSource != null)
            {
                directSource.pitch = Random.Range(0.98f, 1.02f);
                directSource.PlayOneShot(clip, 0.9f);
            }
            else
            {
                PlayClipAtPoint(clip, pos, 0.85f, 1.5f, 20.0f, 0.0f);
            }
        }

        public static void PlayRifleReloadMagIn(Vector3 pos, AudioSource directSource = null)
        {
            AudioClip clip = Instance.rifleReloadMagInClip;
            if (clip == null) return;

            if (directSource != null)
            {
                directSource.pitch = Random.Range(0.98f, 1.02f);
                directSource.PlayOneShot(clip, 0.95f);
            }
            else
            {
                PlayClipAtPoint(clip, pos, 0.90f, 1.5f, 20.0f, 0.0f);
            }
        }

        public static void PlayRifleReloadCharge(Vector3 pos, AudioSource directSource = null)
        {
            AudioClip clip = Instance.rifleReloadChargeClip;
            if (clip == null) return;

            if (directSource != null)
            {
                directSource.pitch = Random.Range(0.98f, 1.02f);
                directSource.PlayOneShot(clip, 1.0f);
            }
            else
            {
                PlayClipAtPoint(clip, pos, 0.95f, 1.5f, 20.0f, 0.0f);
            }
        }

        public static void PlayZombieGrowl(AudioSource source)
        {
            var inst = Instance;
            if (inst.zombieGrowlClips == null || inst.zombieGrowlClips.Length == 0) return;

            AudioClip clip = inst.zombieGrowlClips[Random.Range(0, inst.zombieGrowlClips.Length)];
            if (clip != null && source != null)
            {
                source.pitch = Random.Range(0.90f, 1.10f);
                source.PlayOneShot(clip, 0.90f);
            }
        }

        public static void PlayZombieAttack(AudioSource source)
        {
            var inst = Instance;
            if (inst.zombieAttackClips == null || inst.zombieAttackClips.Length == 0) return;

            AudioClip clip = inst.zombieAttackClips[Random.Range(0, inst.zombieAttackClips.Length)];
            if (clip != null && source != null)
            {
                source.pitch = Random.Range(0.94f, 1.06f);
                source.PlayOneShot(clip, 1.0f);
            }
        }

        public static void PlayZombieHurt(AudioSource source)
        {
            var inst = Instance;
            if (inst.zombieHurtClips == null || inst.zombieHurtClips.Length == 0)
            {
                PlayZombieAttack(source);
                return;
            }

            AudioClip clip = inst.zombieHurtClips[Random.Range(0, inst.zombieHurtClips.Length)];
            if (clip != null && source != null)
            {
                source.pitch = Random.Range(0.95f, 1.08f);
                source.PlayOneShot(clip, 1.0f);
            }
        }

        public static void PlayZombieDeath(AudioSource source)
        {
            var inst = Instance;
            if (inst.zombieDeathClips == null || inst.zombieDeathClips.Length == 0) return;

            AudioClip clip = inst.zombieDeathClips[Random.Range(0, inst.zombieDeathClips.Length)];
            if (clip != null && source != null)
            {
                source.pitch = Random.Range(0.92f, 1.04f);
                source.PlayOneShot(clip, 1.0f);
            }
        }

        public static void PlayClawSwipe(Vector3 pos)
        {
            AudioClip clip = Instance.clawSwipeClip;
            if (clip != null)
            {
                PlayClipAtPoint(clip, pos, 0.9f, 1.5f, 25.0f, 1.0f);
            }
        }

        public static void PlayClawHit(Vector3 pos)
        {
            AudioClip clip = Instance.clawHitClip;
            if (clip != null)
            {
                PlayClipAtPoint(clip, pos, 1.0f, 1.5f, 25.0f, 1.0f);
            }
        }

        public static void PlayBulletFleshHit(Vector3 pos)
        {
            AudioClip clip = Instance.bulletHitFleshClip;
            if (clip != null)
            {
                PlayClipAtPoint(clip, pos, 1.0f, 2.0f, 35.0f, 1.0f);
            }
        }

        public static void PlayBulletPortalHit(Vector3 pos)
        {
            AudioClip clip = Instance.bulletHitPortalClip;
            if (clip != null)
            {
                PlayClipAtPoint(clip, pos, 1.0f, 2.0f, 35.0f, 1.0f);
            }
        }

        public static void PlayPortalCollapse(Vector3 pos)
        {
            AudioClip clip = Instance.portalCollapseClip ?? Instance.bulletHitPortalClip;
            if (clip != null)
            {
                PlayClipAtPoint(clip, pos, 1.25f, 3.0f, 45.0f, 1.0f);
            }
        }

        public static void PlayBulletConcreteHit(Vector3 pos)
        {
            AudioClip clip = Instance.bulletHitConcreteClip;
            if (clip != null)
            {
                PlayClipAtPoint(clip, pos, 0.75f, 1.5f, 25.0f, 1.0f);
            }
        }

        /// <summary>
        /// Phát âm thanh tại vị trí 3D sử dụng Zero-GC Audio Source Pool (16 kênh).
        /// Loại bỏ hoàn toàn việc tạo mới GameObject và Destroy sau mỗi phát bắn hoặc va chạm.
        /// </summary>
        public static void PlayClipAtPoint(AudioClip clip, Vector3 position, float volume = 1.0f, float minDistance = 2.0f, float maxDistance = 30.0f, float spatialBlend = 1.0f)
        {
            if (clip == null) return;
            var inst = Instance;
            if (inst == null) return;

            inst.InitializePool();

            // 1. Tìm kênh âm thanh rảnh rỗi trong Pool
            AudioSource targetSource = null;
            if (poolSources != null)
            {
                for (int i = 0; i < AudioPoolSize; i++)
                {
                    var s = poolSources[i];
                    if (s != null && !s.isPlaying)
                    {
                        targetSource = s;
                        break;
                    }
                }

                // 2. Nếu cả 16 kênh đều bận, lấy kênh kế tiếp theo cơ chế Circular Round-Robin (không bao giờ drop âm)
                if (targetSource == null && poolSources.Length > 0)
                {
                    targetSource = poolSources[currentPoolIndex];
                    currentPoolIndex = (currentPoolIndex + 1) % AudioPoolSize;
                }
            }

            if (targetSource == null) return;

            targetSource.transform.position = position;
            targetSource.clip = clip;
            targetSource.volume = volume;
            targetSource.pitch = Random.Range(0.94f, 1.06f);
            targetSource.spatialBlend = spatialBlend;
            targetSource.minDistance = minDistance;
            targetSource.maxDistance = maxDistance;
            targetSource.rolloffMode = AudioRolloffMode.Linear;
            targetSource.Play();
        }

        #region IAudioService Implementation
        public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            PlayClipAtPoint(clip, Vector3.zero, volume, 0f, 1000f, 0f);
        }
        public void PlayButtonClickSFX()
        {
            if (buttonClickClip == null) buttonClickClip = Resources.Load<AudioClip>("Bloodlines/Audio/Click Button SFX");
            AudioClip click = buttonClickClip;
            if (click != null) PlaySFX(click, 0.85f);
        }

        public void PlayButtonHoverSFX()
        {
            if (buttonHoverClip == null) buttonHoverClip = Resources.Load<AudioClip>("Bloodlines/Audio/Hover Button SFX");
            AudioClip hover = buttonHoverClip;
            if (hover != null) PlaySFX(hover, 0.45f);
        }

        void IAudioService.PlayClawSwipe(Vector3 pos) => PlayClawSwipe(pos);
        void IAudioService.PlayClawHit(Vector3 pos) => PlayClawHit(pos);
        void IAudioService.PlayBulletFleshHit(Vector3 pos) => PlayBulletFleshHit(pos);
        void IAudioService.PlayBulletPortalHit(Vector3 pos) => PlayBulletPortalHit(pos);
        void IAudioService.PlayPortalCollapse(Vector3 pos) => PlayPortalCollapse(pos);
        void IAudioService.PlayBulletConcreteHit(Vector3 pos) => PlayBulletConcreteHit(pos);
        void IAudioService.PlayRifleShoot(Vector3 pos, AudioSource directSource) => PlayRifleShoot(pos, directSource);
        void IAudioService.PlayRifleReloadMagOut(Vector3 pos, AudioSource directSource) => PlayRifleReloadMagOut(pos, directSource);
        void IAudioService.PlayRifleReloadMagIn(Vector3 pos, AudioSource directSource) => PlayRifleReloadMagIn(pos, directSource);
        void IAudioService.PlayRifleReloadCharge(Vector3 pos, AudioSource directSource) => PlayRifleReloadCharge(pos, directSource);
        void IAudioService.PlayZombieGrowl(AudioSource source) => PlayZombieGrowl(source);
        void IAudioService.PlayZombieAttack(AudioSource source) => PlayZombieAttack(source);
        void IAudioService.PlayZombieHurt(AudioSource source) => PlayZombieHurt(source);
        void IAudioService.PlayZombieDeath(AudioSource source) => PlayZombieDeath(source);
        void IAudioService.PlayClipAtPoint(AudioClip clip, Vector3 position, float volume, float minDistance, float maxDistance, float spatialBlend) =>
            PlayClipAtPoint(clip, position, volume, minDistance, maxDistance, spatialBlend);
        #endregion
    }
}
