using System.Collections;
using UnityEngine;
using AbandonedAsylum.Interfaces;
using AbandonedAsylum.Audio;

namespace AbandonedAsylum.VFX
{
    /// <summary>
    /// Component quản lý tia lửa va chạm được tái sử dụng trong Object Pool.
    /// Tự chủ hoàn toàn vòng đời, không phụ thuộc vào đối tượng gọi.
    /// </summary>
    public class PooledImpactSpark : MonoBehaviour
    {
        private ParticleSystem ps;
        private Light impactLight;
        private float sparkLifeTimer = 0f;
        private const float LightDuration = 0.045f;
        private const float TotalLifeDuration = 0.16f;

        public void InitSpark(ParticleSystem particleSys, ParticleSystemRenderer psRenderer, Light ptLight, Material mat)
        {
            ps = particleSys;
            impactLight = ptLight;

            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main;
                main.duration = 0.15f;
                main.loop = false;
                main.startLifetime = 0.14f;
                main.startSpeed = 8.5f;
                main.startSize = 0.05f;
                main.startColor = new Color(1.0f, 0.88f, 0.35f);
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.playOnAwake = false;

                var emission = ps.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 15) });

                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 40f;
                shape.radius = 0.02f;
            }

            if (psRenderer != null && mat != null)
            {
                psRenderer.sharedMaterial = mat;
                psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            }

            if (impactLight != null)
            {
                impactLight.type = LightType.Point;
                impactLight.color = new Color(1.0f, 0.85f, 0.40f);
                impactLight.range = 3.2f;
                impactLight.intensity = 3.6f;
                impactLight.shadows = LightShadows.None;
                impactLight.enabled = false;
            }
        }

        public void PlayAt(Vector3 pos, Vector3 normal)
        {
            transform.position = pos;
            if (normal != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(normal);
            }

            gameObject.SetActive(true);

            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play();
            }

            if (impactLight != null) impactLight.enabled = true;
            sparkLifeTimer = TotalLifeDuration;
        }

        private void Update()
        {
            if (sparkLifeTimer > 0f)
            {
                sparkLifeTimer -= Time.deltaTime;
                if (impactLight != null && impactLight.enabled && (TotalLifeDuration - sparkLifeTimer) >= LightDuration)
                {
                    impactLight.enabled = false;
                }

                if (sparkLifeTimer <= 0f)
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// Đường đạn phát sáng tốc độ cao với kiến trúc Zero-GC Circular Object Pool chuẩn Game AAA.
    /// Tái sử dụng 100% TrailRenderer và Impact Sparks, triệt tiêu hoàn toàn GC Allocations và GC Spikes trong suốt màn chơi.
    /// </summary>
    [DisallowMultipleComponent]
    public class TracerBullet : MonoBehaviour
    {
        private Vector3 startPosition;
        private Vector3 targetPosition;
        private Vector3 surfaceNormal;
        private float speed = 220.0f;
        private float damage = 35.0f;
        private GameObject hitObject;
        private float totalDistance;
        private float traveledDistance = 0.0f;
        private bool hasHit = false;
        private float postHitTimer = 0.0f;

        private TrailRenderer trail;
        private static Material sharedTracerMaterial;

        public static Material TracerMaterial
        {
            get
            {
                if (sharedTracerMaterial == null)
                {
                    Shader shader = Shader.Find("Mobile/Particles/Additive")
                                 ?? Shader.Find("Legacy Shaders/Particles/Additive")
                                 ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                                 ?? Shader.Find("Particles/Standard Unlit")
                                 ?? Shader.Find("Sprites/Default")
                                 ?? Shader.Find("Unlit/Color");

                    if (shader == null)
                    {
                        shader = Shader.Find("Standard");
                    }

                    sharedTracerMaterial = shader != null ? new Material(shader) : null;
                    if (sharedTracerMaterial != null)
                    {
                        sharedTracerMaterial.name = "TracerBullet_GlowMaterial";
                        Color glowColor = new Color(1.0f, 0.88f, 0.40f, 1.0f);
                        if (sharedTracerMaterial.HasProperty("_Color"))
                        {
                            sharedTracerMaterial.color = glowColor;
                        }
                        else if (sharedTracerMaterial.HasProperty("_TintColor"))
                        {
                            sharedTracerMaterial.SetColor("_TintColor", glowColor);
                        }
                        sharedTracerMaterial.hideFlags = HideFlags.DontSave;
                    }
                }
                return sharedTracerMaterial;
            }
        }

        #region Zero-GC Bullet Object Pool (40 Instances)
        private const int BulletPoolSize = 40;
        private static TracerBullet[] bulletPool;
        private static int currentBulletIndex = 0;
        private static GameObject bulletPoolContainer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            bulletPool = null;
            currentBulletIndex = 0;
            bulletPoolContainer = null;
            sparkPool = null;
            currentSparkIndex = 0;
            sparkPoolContainer = null;
        }

        public static void InitializeBulletPool()
        {
            if (bulletPool != null && bulletPool.Length == BulletPoolSize && bulletPoolContainer != null) return;

            if (bulletPoolContainer == null)
            {
                bulletPoolContainer = new GameObject("[TracerBullet_ObjectPool]");
                DontDestroyOnLoad(bulletPoolContainer);
            }

            bulletPool = new TracerBullet[BulletPoolSize];
            for (int i = 0; i < BulletPoolSize; i++)
            {
                GameObject bulletObj = new GameObject($"TracerBullet_Pooled_{i:D2}");
                bulletObj.transform.SetParent(bulletPoolContainer.transform, false);

                // Khởi tạo trực tiếp TrailRenderer và truyền thẳng vào TracerBullet (loại bỏ hoàn toàn lỗi MissingComponent)
                TrailRenderer tr = bulletObj.AddComponent<TrailRenderer>();
                TracerBullet tracer = bulletObj.AddComponent<TracerBullet>();
                tracer.InitTrailRenderer(tr);

                bulletObj.SetActive(false);
                bulletPool[i] = tracer;
            }
            currentBulletIndex = 0;
        }

        /// <summary>
        /// Sinh viên đạn từ Object Pool có sẵn (Zero Allocation).
        /// </summary>
        public static void SpawnPooledBullet(Vector3 origin, Vector3 hitPoint, Vector3 hitNormal, GameObject hitTarget, float damage, float speed)
        {
            InitializeBulletPool();

            // 1. Ưu tiên tìm một viên đạn đang rảnh rỗi trong pool
            TracerBullet chosenBullet = null;
            if (bulletPool != null)
            {
                for (int i = 0; i < BulletPoolSize; i++)
                {
                    int idx = (currentBulletIndex + i) % BulletPoolSize;
                    var b = bulletPool[idx];
                    if (b != null && !b.gameObject.activeInHierarchy)
                    {
                        chosenBullet = b;
                        currentBulletIndex = (idx + 1) % BulletPoolSize;
                        break;
                    }
                }

                // 2. Nếu cả 40 viên đều đang trên đường bay (xả đạn liên tục), lấy theo Circular Round-Robin
                if (chosenBullet == null && bulletPool.Length > 0)
                {
                    chosenBullet = bulletPool[currentBulletIndex];
                    currentBulletIndex = (currentBulletIndex + 1) % BulletPoolSize;
                }
            }

            if (chosenBullet != null)
            {
                chosenBullet.Fire(origin, hitPoint, hitNormal, hitTarget, damage, speed);
            }
        }
        #endregion

        #region Zero-GC Impact Sparks Pool (16 Instances)
        private const int SparkPoolSize = 16;
        private static PooledImpactSpark[] sparkPool;
        private static int currentSparkIndex = 0;
        private static GameObject sparkPoolContainer;

        private static void InitializeSparkPool()
        {
            if (sparkPool != null && sparkPool.Length == SparkPoolSize && sparkPoolContainer != null) return;

            if (sparkPoolContainer == null)
            {
                sparkPoolContainer = new GameObject("[TracerImpactSparks_ObjectPool]");
                DontDestroyOnLoad(sparkPoolContainer);
            }

            sparkPool = new PooledImpactSpark[SparkPoolSize];
            for (int i = 0; i < SparkPoolSize; i++)
            {
                GameObject sparkObj = new GameObject($"ImpactSpark_Pooled_{i:D2}");
                sparkObj.transform.SetParent(sparkPoolContainer.transform, false);

                // Khởi tạo trực tiếp ParticleSystem và Light (loại bỏ hoàn toàn lỗi MissingComponent)
                ParticleSystem ps = sparkObj.AddComponent<ParticleSystem>();
                ParticleSystemRenderer psRenderer = sparkObj.GetComponent<ParticleSystemRenderer>();
                Light ptLight = sparkObj.AddComponent<Light>();

                PooledImpactSpark spark = sparkObj.AddComponent<PooledImpactSpark>();
                spark.InitSpark(ps, psRenderer, ptLight, TracerMaterial);

                sparkObj.SetActive(false);
                sparkPool[i] = spark;
            }
            currentSparkIndex = 0;
        }

        public static void SpawnImpactSparks(Vector3 pos, Vector3 normal)
        {
            InitializeSparkPool();

            if (sparkPool == null || sparkPool.Length == 0) return;

            PooledImpactSpark chosenSpark = null;
            for (int i = 0; i < SparkPoolSize; i++)
            {
                int idx = (currentSparkIndex + i) % SparkPoolSize;
                var s = sparkPool[idx];
                if (s != null && !s.gameObject.activeInHierarchy)
                {
                    chosenSpark = s;
                    currentSparkIndex = (idx + 1) % SparkPoolSize;
                    break;
                }
            }

            if (chosenSpark == null)
            {
                chosenSpark = sparkPool[currentSparkIndex];
                currentSparkIndex = (currentSparkIndex + 1) % SparkPoolSize;
            }

            if (chosenSpark != null)
            {
                chosenSpark.PlayAt(pos, normal);
            }
        }
        #endregion

        public void InitTrailRenderer(TrailRenderer tr)
        {
            trail = tr;
            if (trail == null)
            {
                trail = GetComponent<TrailRenderer>();
                if (trail == null)
                {
                    trail = gameObject.AddComponent<TrailRenderer>();
                }
            }

            transform.localScale = Vector3.one;

            trail.sharedMaterial = TracerMaterial;
            trail.time = 0.12f;
            trail.minVertexDistance = 0.05f;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.autodestruct = false;
            trail.emitting = false;

            AnimationCurve widthCurve = new AnimationCurve();
            widthCurve.AddKey(0.0f, 0.08f);
            widthCurve.AddKey(1.0f, 0.01f);
            trail.widthCurve = widthCurve;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1.0f, 1.0f, 0.95f), 0.0f),
                    new GradientColorKey(new Color(1.0f, 0.75f, 0.25f), 0.35f),
                    new GradientColorKey(new Color(1.0f, 0.30f, 0.05f), 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(0.95f, 0.45f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            trail.colorGradient = gradient;
        }

        public void Fire(Vector3 start, Vector3 target, Vector3 normal, GameObject hitTarget, float bulletDamage, float bulletSpeed = 220.0f)
        {
            startPosition = start;
            targetPosition = target;
            surfaceNormal = normal;
            hitObject = hitTarget;
            damage = bulletDamage;
            speed = bulletSpeed;

            transform.position = start;
            Vector3 delta = target - start;
            float sqrDist = delta.sqrMagnitude;
            if (sqrDist > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(delta);
                totalDistance = Mathf.Sqrt(sqrDist);
            }
            else
            {
                totalDistance = 0.0f;
            }

            traveledDistance = 0.0f;
            hasHit = false;
            postHitTimer = 0.0f;

            gameObject.SetActive(true);

            if (trail != null)
            {
                trail.Clear();
                trail.emitting = true;
            }

            // Nếu mục tiêu siêu gần (< 0.1m), tính va chạm ngay
            if (totalDistance <= 0.1f)
            {
                transform.position = target;
                OnBulletImpact();
            }
        }

        // Tương thích ngược với lệnh Initialize cũ
        public void Initialize(Vector3 start, Vector3 target, Vector3 normal, GameObject hitTarget, float bulletDamage, float bulletSpeed = 220.0f)
        {
            Fire(start, target, normal, hitTarget, bulletDamage, bulletSpeed);
        }

        private void Update()
        {
            // Khi đạn chạm đích: giữ nguyên vệt đuôi trong 0.10s để phai mờ dần tự nhiên rồi mới trả về Pool
            if (hasHit)
            {
                postHitTimer += Time.deltaTime;
                if (postHitTimer >= 0.10f)
                {
                    DeactivateToPool();
                }
                return;
            }

            float step = speed * Time.deltaTime;
            traveledDistance += step;

            if (traveledDistance >= totalDistance)
            {
                transform.position = targetPosition;
                OnBulletImpact();
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);
            }
        }

        private void OnBulletImpact()
        {
            if (hasHit) return;
            hasHit = true;
            postHitTimer = 0.0f;

            if (trail != null)
            {
                trail.emitting = false;
            }

            if (hitObject != null)
            {
                IDamageable damageable = hitObject.GetComponent<IDamageable>()
                                      ?? hitObject.GetComponentInParent<IDamageable>()
                                      ?? hitObject.GetComponentInChildren<IDamageable>();
                if (damageable != null && !damageable.IsDead)
                {
                    PlayImpactAudio(damageable.ImpactType, targetPosition);
                    damageable.TakeDamage(damage, targetPosition, surfaceNormal);

                    if (damageable.ImpactType != ImpactSoundType.Flesh)
                    {
                        SpawnImpactSparks(targetPosition, surfaceNormal);
                    }
                }
                else
                {
                    GameAudioFX.PlayBulletConcreteHit(targetPosition);
                    SpawnImpactSparks(targetPosition, surfaceNormal);
                }
            }
            else
            {
                GameAudioFX.PlayBulletConcreteHit(targetPosition);
                SpawnImpactSparks(targetPosition, surfaceNormal);
            }
        }

        private void DeactivateToPool()
        {
            if (trail != null)
            {
                trail.emitting = false;
                trail.Clear();
            }
            gameObject.SetActive(false);
        }

        private void PlayImpactAudio(ImpactSoundType soundType, Vector3 pos)
        {
            switch (soundType)
            {
                case ImpactSoundType.Flesh:
                    GameAudioFX.PlayBulletFleshHit(pos);
                    break;
                case ImpactSoundType.Portal:
                    GameAudioFX.PlayBulletPortalHit(pos);
                    break;
                default:
                    GameAudioFX.PlayBulletConcreteHit(pos);
                    break;
            }
        }
    }
}
