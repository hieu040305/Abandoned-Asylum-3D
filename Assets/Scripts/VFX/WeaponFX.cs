using AbandonedAsylum.VFX;
using UnityEngine;
using AbandonedAsylum.Audio;

namespace AbandonedAsylum.VFX
{
    public class WeaponFX : MonoBehaviour
    {
        [SerializeField] private GameObject customMuzzleFlashPrefab;
        [SerializeField] private AudioClip shootSound;
        [SerializeField] private float lightRange = 3.5f;
        [SerializeField] private float lightIntensity = 4.0f;
        [SerializeField] private Color flashColor = new Color(1.0f, 0.85f, 0.4f);

        private AudioSource audioSource;
        private GameObject pooledFlashObj;
        private Light pooledFlashLight;
        private ParticleSystem pooledFlashPS;
        private float muzzleFlashTimer = 0.0f;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0.0f;
            }
        }

        private void Update()
        {
            if (muzzleFlashTimer > 0.0f)
            {
                muzzleFlashTimer -= Time.deltaTime;
                if (muzzleFlashTimer <= 0.0f && pooledFlashLight != null)
                {
                    pooledFlashLight.enabled = false;
                }
            }
        }

        public void PlayShootFX(Transform muzzlePoint)
        {
            if (muzzlePoint == null) return;

            TriggerMuzzleFlash(muzzlePoint);

            if (audioSource != null && shootSound != null)
            {
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.PlayOneShot(shootSound);
            }
            else
            {
                GameAudioFX.PlayRifleShoot(muzzlePoint.position, audioSource);
            }
        }

        private void TriggerMuzzleFlash(Transform muzzlePoint)
        {
            if (customMuzzleFlashPrefab != null)
            {
                GameObject customFlash = Instantiate(customMuzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation, muzzlePoint);
                Destroy(customFlash, 0.08f);
                return;
            }

            // Tái sử dụng đối tượng MuzzleFlash tạo sẵn (Zero Allocation)
            if (pooledFlashObj == null)
            {
                pooledFlashObj = new GameObject("PooledMuzzleFlash");
                pooledFlashObj.transform.SetParent(muzzlePoint, false);

                pooledFlashLight = pooledFlashObj.AddComponent<Light>();
                pooledFlashLight.type = LightType.Point;
                pooledFlashLight.color = flashColor;
                pooledFlashLight.range = lightRange;
                pooledFlashLight.intensity = lightIntensity;
                pooledFlashLight.shadows = LightShadows.None;
                pooledFlashLight.enabled = false;

                pooledFlashPS = pooledFlashObj.AddComponent<ParticleSystem>();
                pooledFlashPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = pooledFlashPS.main;
                main.duration = 0.05f;
                main.loop = false;
                main.startLifetime = 0.05f;
                main.startSpeed = 3.5f;
                main.startSize = 0.12f;
                main.startColor = new Color(1.0f, 0.9f, 0.45f);
                main.playOnAwake = false;

                var emission = pooledFlashPS.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 8) });

                var shape = pooledFlashPS.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.03f;

                var renderer = pooledFlashObj.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = TracerBullet.TracerMaterial;
            }

            pooledFlashObj.transform.position = muzzlePoint.position;
            pooledFlashObj.transform.rotation = muzzlePoint.rotation;

            if (pooledFlashLight != null) pooledFlashLight.enabled = true;
            if (pooledFlashPS != null)
            {
                pooledFlashPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                pooledFlashPS.Play();
            }

            muzzleFlashTimer = 0.045f;
        }

        public void SpawnTracerBullet(Vector3 origin, Vector3 hitPoint, Vector3 hitNormal, GameObject hitTarget, float damage, float bulletSpeed)
        {
            TracerBullet.SpawnPooledBullet(origin, hitPoint, hitNormal, hitTarget, damage, bulletSpeed);
        }
    }
}
