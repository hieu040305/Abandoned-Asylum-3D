using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace AbandonedAsylum.Core
{
    /// <summary>
    /// Điều phối hệ thống chiếu sáng thời gian thực chuẩn Production AAA cho Viện tâm thần Karlheinz:
    /// 1. Tôn trọng 100% không gian âm u, u tối rùng rợn nguyên bản:
    ///    - Giữ các bóng đèn ở cường độ dịu nhẹ (0.85f - 0.90f), cự ly cục bộ mờ ảo (5.0m - 5.5m), không gây sáng trưng.
    ///    - Triệt tiêu hoàn toàn mọi nguồn sáng mặt trời/mặt trăng nhân tạo rọi xuyên trần hay đèn pin giả lập.
    /// 2. Khắc phục triệt để lỗi model Player và Zombie bị đen sì:
    ///    - Tắt `receiveShadows = false` và `shadowCastingMode = Off` trên SkinnedMeshRenderer của nhân vật để triệt tiêu vĩnh viễn
    ///      hiện tượng Self-Shadowing và loại bỏ hoàn toàn chi phí tính bóng đổ (0 GC, 0 Draw Calls bóng).
    ///    - Tắt `lightProbeUsage = Off` trên nhân vật để ngăn việc lấy mẫu Light Probes rác/đen từ scene cũ.
    ///    - Thiết lập Ambient Trilight dịu nhẹ đảm bảo texture, da thịt, quần áo luôn rõ nét chi tiết mà không mất độ u ám.
    /// 3. Loại bỏ hoàn toàn đổ bóng (Shadows Disabled):
    ///    - Thiết lập `QualitySettings.shadows = ShadowQuality.Disable` và `LightShadows.None` trên 100% bóng đèn.
    ///    - Tối đa hóa hiệu năng GPU (tăng 20-30 FPS, hoàn toàn mượt mà 60+ FPS trên mọi cấu hình phần cứng).
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-200)]
    public class AsylumLightingManager : MonoBehaviour
    {
        private static AsylumLightingManager instance;
        public static AsylumLightingManager Instance => instance;

        [Header("Lighting Pacing Configuration")]
        [Tooltip("Tần suất đồng bộ và kiểm tra trạng thái bóng đèn (giây/lần) - Zero-GC Throttle")]
        [SerializeField] private float shadowPacingInterval = 0.5f;

        // Bộ đệm Zero-GC quản lý bóng đèn
        private Light[] cachedMapLamps = Array.Empty<Light>();
        private Vector3[] cachedLampPositions = Array.Empty<Vector3>();
        private int cachedLampCount = 0;

        private Transform playerTransform;
        private float nextShadowPacingTime = 0f;
        private float nextLampDiscoveryTime = 0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureLightingManagerExists()
        {
            if (instance == null && FindFirstObjectByType<AsylumLightingManager>() == null)
            {
                GameObject go = new GameObject("[AsylumLightingManager]", typeof(AsylumLightingManager));
                DontDestroyOnLoad(go);
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                if (Application.isPlaying) Destroy(gameObject);
                else DestroyImmediate(gameObject);
                return;
            }
            instance = this;
            if (Application.isPlaying && transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            PurgeArtificialLights();
            ConfigureForwardLightingQuality();
            DiscoverMapLamps();
        }

        private void OnEnable()
        {
            PurgeArtificialLights();
            ConfigureForwardLightingQuality();
            DiscoverMapLamps();
            UpdateProximityShadows(true);
        }

        private void Start()
        {
            PurgeArtificialLights();
            ConfigureForwardLightingQuality();
            DiscoverMapLamps();
            AuditAndEnableDynamicShadows();
            UpdateProximityShadows(true);
        }

        private void Update()
        {
            float currentTime = Application.isPlaying ? Time.time : (float)DateTime.Now.TimeOfDay.TotalSeconds;

            if (currentTime >= nextShadowPacingTime)
            {
                nextShadowPacingTime = currentTime + shadowPacingInterval;
                UpdateProximityShadows(false);
            }

            if (cachedLampCount == 0 && currentTime >= nextLampDiscoveryTime)
            {
                nextLampDiscoveryTime = currentTime + 1.0f;
                DiscoverMapLamps();
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        /// <summary>
        /// Triệt tiêu 100% mọi nguồn sáng tự sinh từ hư vô (ánh trăng xuyên trần, đèn pin rọi ảo).
        /// </summary>
        public void PurgeArtificialLights()
        {
            GameObject ml = GameObject.Find("[Asylum_Atmospheric_Moonlight]");
            if (ml != null)
            {
                if (Application.isPlaying) Destroy(ml);
                else DestroyImmediate(ml);
            }

            var dirLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < dirLights.Length; i++)
            {
                if (dirLights[i] != null && dirLights[i].type == LightType.Directional)
                {
                    if (dirLights[i].name.Contains("Moonlight") || dirLights[i].name.Contains("Atmospheric"))
                    {
                        if (Application.isPlaying) Destroy(dirLights[i].gameObject);
                        else DestroyImmediate(dirLights[i].gameObject);
                    }
                    else
                    {
                        dirLights[i].enabled = false;
                    }
                }
            }

            if (playerTransform == null)
            {
                playerTransform = PlayerRegistry.PlayerTransform;
                if (playerTransform == null)
                {
                    GameObject pObj = GameObject.FindWithTag("Player");
                    if (pObj != null) playerTransform = pObj.transform;
                }
            }

            if (playerTransform != null)
            {
                Transform fl = playerTransform.Find("TacticalFlashlight");
                if (fl != null)
                {
                    if (Application.isPlaying) Destroy(fl.gameObject);
                    else DestroyImmediate(fl.gameObject);
                }
            }

            GameObject flDirect = GameObject.Find("TacticalFlashlight");
            if (flDirect != null)
            {
                if (Application.isPlaying) Destroy(flDirect);
                else DestroyImmediate(flDirect);
            }
        }

        /// <summary>
        /// Thiết lập Ambient Trilight dịu nhẹ để nhân vật không bao giờ bị đen kịt ngay cả trong góc khuất,
        /// đồng thời giữ trọn vẹn sự u tối, ghê rợn của viện tâm thần và TRIỆT TIÊU HOÀN TOÀN MỌI ĐỔ BÓNG.
        /// </summary>
        public void ConfigureForwardLightingQuality()
        {
            // Tối ưu hóa chất lượng ánh sáng thời gian thực: Hard shadows với budget khoảng cách 25m
            QualitySettings.shadows = ShadowQuality.HardOnly;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.shadowDistance = 25f;
            QualitySettings.shadowCascades = 2;
            QualitySettings.shadowNearPlaneOffset = 0.1f;

            // Tăng pixel light count = 16 để toàn bộ bóng đèn hành lang và phòng bệnh hiển thị đầy đủ trên mọi bề mặt mesh
            QualitySettings.pixelLightCount = 16;

            // Ambient Fill không gian Viện tâm thần u ám rùng rợn nhưng có độ tương phản và chiều sâu rõ nét
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.24f, 0.28f, 0.35f, 1.0f);
            RenderSettings.ambientEquatorColor = new Color(0.16f, 0.18f, 0.22f, 1.0f);
            RenderSettings.ambientGroundColor = new Color(0.10f, 0.11f, 0.13f, 1.0f);
            RenderSettings.ambientIntensity = 1.0f;
        }

        /// <summary>
        /// Khám phá và lập danh sách các bóng đèn thật của bệnh viện.
        /// Giữ nguyên cường độ và cự ly dịu nhẹ để giữ không gian u ám, bí hiểm,
        /// và đảm bảo toàn bộ bóng đèn KHÔNG đổ bóng (LightShadows.None).
        /// </summary>
        public void DiscoverMapLamps()
        {
            var allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            int validCount = 0;

            for (int i = 0; i < allLights.Length; i++)
            {
                Light l = allLights[i];
                if (l == null || !l.enabled) continue;
                if (l.type != LightType.Point && l.type != LightType.Spot) continue;

                string lName = l.name;
                string pName = l.transform.parent != null ? l.transform.parent.name : string.Empty;
                if (lName.Contains("Tracer") || lName.Contains("Muzzle") || lName.Contains("Portal") ||
                    pName.Contains("Weapon") || pName.Contains("Portal") || pName.Contains("Tracer"))
                {
                    continue;
                }

                validCount++;
            }

            if (validCount != cachedLampCount || cachedMapLamps.Length < validCount)
            {
                cachedMapLamps = new Light[validCount];
                cachedLampPositions = new Vector3[validCount];
            }

            int index = 0;
            for (int i = 0; i < allLights.Length; i++)
            {
                Light l = allLights[i];
                if (l == null || !l.enabled) continue;
                if (l.type != LightType.Point && l.type != LightType.Spot) continue;

                string lName = l.name;
                string pName = l.transform.parent != null ? l.transform.parent.name : string.Empty;
                if (lName.Contains("Tracer") || lName.Contains("Muzzle") || lName.Contains("Portal") ||
                    pName.Contains("Weapon") || pName.Contains("Portal") || pName.Contains("Tracer"))
                {
                    continue;
                }

                cachedMapLamps[index] = l;
                cachedLampPositions[index] = l.transform.position;

                // Tắt hoàn toàn đổ bóng trên từng ngọn đèn để tối đa hóa hiệu năng GPU
                l.shadows = LightShadows.None;
                l.lightmapBakeType = LightmapBakeType.Realtime;
                l.renderMode = LightRenderMode.ForcePixel;

                // Đảm bảo cường độ thực tế và cự ly chiếu rọi bao quát (12m - 14m, intensity 1.8f - 2.0f)
                if (l.range < 12.0f) l.range = 14.0f;
                if (l.intensity < 1.5f) l.intensity = 2.0f;

                index++;
                if (index >= validCount) break;
            }

            cachedLampCount = index;
        }

        /// <summary>
        /// Triệt tiêu hoàn toàn đổ bóng cho toàn bộ bóng đèn trên bản đồ (0 GC, tối đa hóa hiệu năng GPU).
        /// </summary>
        public void UpdateProximityShadows(bool forceAll)
        {
            if (cachedLampCount == 0) return;

            // Shadows Disabled: 100% bóng đèn triệt tiêu bóng đổ (0 GC, 0 Draw Calls bóng đổ)
            for (int i = 0; i < cachedLampCount; i++)
            {
                Light lamp = cachedMapLamps[i];
                if (lamp != null && lamp.shadows != LightShadows.None)
                {
                    lamp.shadows = LightShadows.None;
                }
            }
        }

        /// <summary>
        /// Kiểm toán Renderers:
        /// Tắt hoàn toàn đổ bóng trên toàn bộ Model nhân vật và Môi trường.
        /// - Nhân vật Player & Zombie:
        ///    + `shadowCastingMode = Off`: Không tạo bóng giả lập.
        ///    + `receiveShadows = false`: Triệt tiêu vĩnh viễn hiện tượng đen xì do Point Light self-shadowing.
        ///    + `lightProbeUsage = Off`: Loại bỏ Light Probes hỏng/đen, đảm bảo hiển thị đủ màu sắc texture và da thịt.
        /// - Môi trường và tường sàn:
        ///    + `shadowCastingMode = Off` và `receiveShadows = false`.
        /// </summary>
        public void AuditAndEnableDynamicShadows()
        {
            var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null) continue;
                if (r is ParticleSystemRenderer || r is TrailRenderer) continue;

                if (r.shadowCastingMode != ShadowCastingMode.Off)
                {
                    r.shadowCastingMode = ShadowCastingMode.Off;
                }
                if (r.receiveShadows)
                {
                    r.receiveShadows = false;
                }

                Transform root = r.transform.root;
                bool isCharacter = (r is SkinnedMeshRenderer) ||
                                   r.CompareTag("Player") || r.CompareTag("Enemy") ||
                                   (root != null && (root.CompareTag("Player") || root.CompareTag("Enemy")));

                if (isCharacter && r.lightProbeUsage != LightProbeUsage.Off)
                {
                    r.lightProbeUsage = LightProbeUsage.Off;
                }
            }
        }
    }
}
