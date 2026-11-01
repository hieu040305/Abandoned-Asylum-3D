using System.Collections.Generic;
using UnityEngine;

namespace AbandonedAsylum.VFX
{
    public class ZombieBloodFX : MonoBehaviour
    {
        private static Material sharedDropMaterial;
        private static Material sharedSplatMaterial;
        private static Material sharedChunkMaterial;
        private static Material sharedMistMaterial;

        private static Shader ParticleShader
        {
            get
            {
                return Shader.Find("Mobile/Particles/Alpha Blended")
                    ?? Shader.Find("Particles/Standard Unlit")
                    ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended")
                    ?? Shader.Find("Sprites/Default");
            }
        }

        public static Material DropMaterial
        {
            get
            {
                if (sharedDropMaterial == null)
                {
                    sharedDropMaterial = new Material(ParticleShader) { name = "Blood_Drop_Mat" };
                    Texture2D tex = Resources.Load<Texture2D>("Textures/Blood/blood_drop") ?? CreateProceduralCircleTex();
                    sharedDropMaterial.mainTexture = tex;
                }
                return sharedDropMaterial;
            }
        }

        public static Material SplatMaterial
        {
            get
            {
                if (sharedSplatMaterial == null)
                {
                    sharedSplatMaterial = new Material(ParticleShader) { name = "Blood_Splat_Mat" };
                    Texture2D tex = Resources.Load<Texture2D>("Textures/Blood/blood_splat") ?? CreateProceduralCircleTex();
                    sharedSplatMaterial.mainTexture = tex;
                }
                return sharedSplatMaterial;
            }
        }

        public static Material ChunkMaterial
        {
            get
            {
                if (sharedChunkMaterial == null)
                {
                    sharedChunkMaterial = new Material(ParticleShader) { name = "Blood_Chunk_Mat" };
                    Texture2D tex = Resources.Load<Texture2D>("Textures/Blood/blood_chunk") ?? CreateProceduralCircleTex();
                    sharedChunkMaterial.mainTexture = tex;
                }
                return sharedChunkMaterial;
            }
        }

        public static Material MistMaterial
        {
            get
            {
                if (sharedMistMaterial == null)
                {
                    sharedMistMaterial = new Material(ParticleShader) { name = "Blood_Mist_Mat" };
                    Texture2D tex = Resources.Load<Texture2D>("Textures/Blood/blood_mist") ?? CreateProceduralCircleTex();
                    sharedMistMaterial.mainTexture = tex;
                }
                return sharedMistMaterial;
            }
        }

        #region High-Performance Zero-Allocation Object Pool
        private const int PoolSize = 10;
        private static BloodPoolInstance[] pool;
        private static int currentPoolIndex = 0;
        private static GameObject poolContainer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            pool = null;
            currentPoolIndex = 0;
            poolContainer = null;
            sharedDropMaterial = null;
            sharedSplatMaterial = null;
            sharedChunkMaterial = null;
            sharedMistMaterial = null;
        }

        private class BloodPoolInstance
        {
            public GameObject root;
            public ParticleSystem spurtsPS;
            public ParticleSystem splatPS;
            public ParticleSystem chunksPS;
            public ParticleSystem mistPS;

            public void PlayAt(Vector3 position, Quaternion rotation)
            {
                if (root == null) return;
                root.transform.position = position;
                root.transform.rotation = rotation;
                root.SetActive(true);

                if (spurtsPS != null)
                {
                    spurtsPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    spurtsPS.Play();
                }
                if (splatPS != null)
                {
                    splatPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    splatPS.Play();
                }
                if (chunksPS != null)
                {
                    chunksPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    chunksPS.Play();
                }
                if (mistPS != null)
                {
                    mistPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    mistPS.Play();
                }
            }
        }

        private static void InitializePool()
        {
            if (pool != null && pool.Length == PoolSize && poolContainer != null) return;

            if (poolContainer == null)
            {
                poolContainer = new GameObject("[ZombieBloodFX_ObjectPool]");
                DontDestroyOnLoad(poolContainer);
            }
            pool = new BloodPoolInstance[PoolSize];

            for (int i = 0; i < PoolSize; i++)
            {
                GameObject instanceObj = new GameObject($"BloodFX_PoolItem_{i}");
                instanceObj.transform.SetParent(poolContainer.transform);

                BloodPoolInstance item = new BloodPoolInstance
                {
                    root = instanceObj,
                    spurtsPS = CreateDirectionalSpurts(instanceObj.transform),
                    splatPS = CreateSplatterBurst(instanceObj.transform),
                    chunksPS = CreateGoreChunks(instanceObj.transform),
                    mistPS = CreateDarkBloodMist(instanceObj.transform)
                };

                instanceObj.SetActive(false);
                pool[i] = item;
            }
        }

        public static void SpawnBloodSplatter(Vector3 hitPoint, Vector3 hitNormal, Vector3 incomingDirection)
        {
            InitializePool();

            Vector3 outwardDir = hitNormal != Vector3.zero ? hitNormal : -incomingDirection;
            Quaternion rot = outwardDir != Vector3.zero ? Quaternion.LookRotation(outwardDir) : Quaternion.identity;

            BloodPoolInstance item = pool[currentPoolIndex];
            currentPoolIndex = (currentPoolIndex + 1) % PoolSize;

            item.PlayAt(hitPoint, rot);
        }
        #endregion

        #region Particle System Configurations (Coagulated Dark Green Blood)
        private static readonly Color DeepVenousGreen = new Color(0.01f, 0.16f, 0.05f, 0.98f);
        private static readonly Color CoagulatedDarkGreen = new Color(0.06f, 0.30f, 0.10f, 0.95f);
        private static readonly Color VisceralGreen = new Color(0.04f, 0.22f, 0.08f, 0.90f);

        private static ParticleSystem CreateDirectionalSpurts(Transform parent)
        {
            GameObject go = new GameObject("Spurts");
            go.transform.SetParent(parent, false);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 0.30f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5.0f, 10.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(DeepVenousGreen, CoagulatedDarkGreen);
            main.gravityModifier = 1.8f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 16) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 14.0f;
            shape.radius = 0.03f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.06f;
            renderer.lengthScale = 2.0f;
            renderer.sharedMaterial = DropMaterial;

            return ps;
        }

        private static ParticleSystem CreateSplatterBurst(Transform parent)
        {
            GameObject go = new GameObject("Splatter");
            go.transform.SetParent(parent, false);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 0.40f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.20f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 6.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.24f);
            main.startColor = new ParticleSystem.MinMaxGradient(CoagulatedDarkGreen, DeepVenousGreen);
            main.startRotation = new ParticleSystem.MinMaxCurve(0.0f, 360.0f * Mathf.Deg2Rad);
            main.gravityModifier = 2.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 20) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.06f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = SplatMaterial;

            return ps;
        }

        private static ParticleSystem CreateGoreChunks(Transform parent)
        {
            GameObject go = new GameObject("GoreChunks");
            go.transform.SetParent(parent, false);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 0.50f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startColor = DeepVenousGreen;
            main.startRotation = new ParticleSystem.MinMaxCurve(0.0f, 360.0f * Mathf.Deg2Rad);
            main.gravityModifier = 3.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 10) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = ChunkMaterial;

            return ps;
        }

        private static ParticleSystem CreateDarkBloodMist(Transform parent)
        {
            GameObject go = new GameObject("BloodMist");
            go.transform.SetParent(parent, false);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 0.40f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.30f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.50f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(VisceralGreen.r, VisceralGreen.g, VisceralGreen.b, 0.70f),
                new Color(DeepVenousGreen.r, DeepVenousGreen.g, DeepVenousGreen.b, 0.20f)
            );
            main.gravityModifier = 0.3f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 6) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.10f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = MistMaterial;

            return ps;
        }

        private static Texture2D CreateProceduralCircleTex()
        {
            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] colors = new Color[size * size];
            float center = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    float alpha = Mathf.Clamp01(1.0f - dist * dist);
                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            return tex;
        }
        #endregion
    }
}
