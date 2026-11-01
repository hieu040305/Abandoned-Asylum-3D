using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AbandonedAsylum.VFX
{
    public class DimensionalPortalVFX : MonoBehaviour
    {
        private GameObject discVoidObj;
        private Material voidMaterial;
        private Light portalLight;

        private Transform outerSpiralHub;
        private Transform mainSpiralHub;
        private Transform innerMagmaHub;
        private Transform coreRingHub;

        private readonly List<Material> vortexEmissiveMaterials = new List<Material>();
        private readonly List<Color> baseEmissionColors = new List<Color>();
        private readonly List<Material> createdRuntimeMaterials = new List<Material>();

        private static Texture2D cachedSwirlDiscTex;

        public void BuildVisuals(Color deepRedBrownColor, Color brightRedBrownGlow, Color darkRimColor, Color hitFlashColor)
        {
            Transform oldVisual = transform.Find("RiftVisuals");
            if (oldVisual != null)
            {
                DestroyImmediate(oldVisual.gameObject);
            }

            CleanupMaterials();

            GameObject rootVisual = new GameObject("RiftVisuals");
            rootVisual.transform.SetParent(transform, false);

            Shader solidShader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
            Shader unlitShader = Shader.Find("Mobile/Particles/Alpha Blended")
                              ?? Shader.Find("Particles/Standard Unlit")
                              ?? Shader.Find("Sprites/Default");

            if (cachedSwirlDiscTex == null)
            {
                cachedSwirlDiscTex = GenerateSpiralDiscTexture(256);
            }

            // 1. KHỐI NỀN HỐ ĐEN DÀY VƯỢT QUA BẢNG TRẮNG / TƯỜNG (Từ z = -0.14m đến z = +0.22m)
            GameObject backplate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            backplate.name = "AbyssVoidBacking";
            backplate.transform.SetParent(rootVisual.transform, false);
            backplate.transform.localPosition = new Vector3(0f, 1.15f, 0.04f);
            backplate.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            backplate.transform.localScale = new Vector3(2.22f, 0.16f, 2.22f);
            Destroy(backplate.GetComponent<Collider>());

            Material abyssBlackMat = CreateEmissiveMaterial(
                solidShader,
                "AbyssBlack_Mat",
                new Color(0.02f, 0.005f, 0.005f),
                new Color(0.05f, 0.008f, 0.005f),
                null
            );
            backplate.GetComponent<MeshRenderer>().sharedMaterial = abyssBlackMat;

            // 2. ĐĨA XOÁY NỀN ĐỎ THẪM (CIRCULAR VOID DISC - Nhô ra tới z = +0.23m và -0.15m)
            discVoidObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            discVoidObj.name = "CircularVoidDisc";
            discVoidObj.transform.SetParent(rootVisual.transform, false);
            discVoidObj.transform.localPosition = new Vector3(0f, 1.15f, 0.04f);
            discVoidObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            discVoidObj.transform.localScale = new Vector3(2.08f, 0.175f, 2.08f);
            Destroy(discVoidObj.GetComponent<Collider>());

            voidMaterial = CreateEmissiveMaterial(
                solidShader,
                "CircularVoid_Mat",
                new Color(0.55f, 0.08f, 0.02f),
                new Color(1.15f, 0.16f, 0.03f),
                cachedSwirlDiscTex
            );
            discVoidObj.GetComponent<MeshRenderer>().sharedMaterial = voidMaterial;
            RegisterVortexMaterial(voidMaterial, new Color(1.15f, 0.16f, 0.03f));

            // 3. CỤM CÁNH TAY LỐC XOÁY NGOÀI (OUTER CRIMSON SPIRAL ARMS)
            GameObject outerHubObj = new GameObject("OuterSpiralVortexHub");
            outerHubObj.transform.SetParent(rootVisual.transform, false);
            outerHubObj.transform.localPosition = new Vector3(0f, 1.15f, 0.04f);
            outerSpiralHub = outerHubObj.transform;

            Build3DSpiralArms(
                outerSpiralHub,
                solidShader,
                armCount: 8,
                segmentsPerArm: 5,
                innerRadius: 0.42f,
                outerRadius: 1.02f,
                twistAngleDeg: 110f,
                zThickness: 0.37f,
                innerColor: new Color(0.95f, 0.22f, 0.04f),
                outerColor: new Color(0.38f, 0.03f, 0.01f),
                innerEmission: new Color(1.85f, 0.35f, 0.05f),
                outerEmission: new Color(0.65f, 0.05f, 0.01f),
                includeDarkChannels: true
            );

            // 4. CỤM LỐC XOÁY LỬA CHÍNH (MAIN BLAZING VORTEX HUB)
            GameObject mainHubObj = new GameObject("MainSpiralVortexHub");
            mainHubObj.transform.SetParent(rootVisual.transform, false);
            mainHubObj.transform.localPosition = new Vector3(0f, 1.15f, 0.04f);
            mainSpiralHub = mainHubObj.transform;

            Build3DSpiralArms(
                mainSpiralHub,
                solidShader,
                armCount: 6,
                segmentsPerArm: 5,
                innerRadius: 0.26f,
                outerRadius: 0.82f,
                twistAngleDeg: 135f,
                zThickness: 0.39f,
                innerColor: new Color(1.0f, 0.48f, 0.08f),
                outerColor: new Color(0.82f, 0.10f, 0.02f),
                innerEmission: new Color(2.40f, 0.78f, 0.12f),
                outerEmission: new Color(1.35f, 0.16f, 0.03f),
                includeDarkChannels: false
            );

            // 5. CỤM MẠCH LỬA MAGMA LÕI TRONG (INNER MOLTEN MAGMA SPIRAL HUB)
            GameObject innerHubObj = new GameObject("InnerMagmaSpiralHub");
            innerHubObj.transform.SetParent(rootVisual.transform, false);
            innerHubObj.transform.localPosition = new Vector3(0f, 1.15f, 0.04f);
            innerMagmaHub = innerHubObj.transform;

            Build3DSpiralArms(
                innerMagmaHub,
                solidShader,
                armCount: 5,
                segmentsPerArm: 4,
                innerRadius: 0.18f,
                outerRadius: 0.54f,
                twistAngleDeg: 145f,
                zThickness: 0.41f,
                innerColor: new Color(1.0f, 0.72f, 0.20f),
                outerColor: new Color(0.95f, 0.25f, 0.04f),
                innerEmission: new Color(2.85f, 1.35f, 0.28f),
                outerEmission: new Color(1.75f, 0.38f, 0.06f),
                includeDarkChannels: true
            );

            // 6. HỐ ĐEN TRUNG TÂM & VÀNH LỬA HỘI TỤ (EVENT HORIZON CORE)
            GameObject coreHubObj = new GameObject("SingularityCoreHub");
            coreHubObj.transform.SetParent(rootVisual.transform, false);
            coreHubObj.transform.localPosition = new Vector3(0f, 1.15f, 0.04f);
            coreRingHub = coreHubObj.transform;

            GameObject accretionRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            accretionRing.name = "AccretionFireRing";
            accretionRing.transform.SetParent(coreRingHub, false);
            accretionRing.transform.localPosition = Vector3.zero;
            accretionRing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            accretionRing.transform.localScale = new Vector3(0.46f, 0.215f, 0.46f);
            Destroy(accretionRing.GetComponent<Collider>());

            Material accretionMat = CreateEmissiveMaterial(
                solidShader,
                "AccretionRing_Mat",
                new Color(1.0f, 0.55f, 0.12f),
                new Color(2.6f, 0.95f, 0.18f),
                null
            );
            accretionRing.GetComponent<MeshRenderer>().sharedMaterial = accretionMat;
            RegisterVortexMaterial(accretionMat, new Color(2.6f, 0.95f, 0.18f));

            GameObject blackHoleCenter = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            blackHoleCenter.name = "BlackHoleSingularity";
            blackHoleCenter.transform.SetParent(coreRingHub, false);
            blackHoleCenter.transform.localPosition = Vector3.zero;
            blackHoleCenter.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            blackHoleCenter.transform.localScale = new Vector3(0.34f, 0.225f, 0.34f);
            Destroy(blackHoleCenter.GetComponent<Collider>());
            blackHoleCenter.GetComponent<MeshRenderer>().sharedMaterial = abyssBlackMat;

            // 7. ĐÈN ĐIỂM ĐỎ THẪM CHIẾU RỌI VỪA PHẢI
            GameObject lightObj = new GameObject("PortalPointLight");
            lightObj.transform.SetParent(rootVisual.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 1.15f, 0.42f);
            portalLight = lightObj.AddComponent<Light>();
            portalLight.type = LightType.Point;
            portalLight.color = new Color(0.90f, 0.18f, 0.04f);
            portalLight.range = 5.0f;
            portalLight.intensity = 2.0f;
            portalLight.shadows = LightShadows.None;

            // 8. HẠT KHÓI CUỘN TRONG LÒNG CỔNG
            CreateDarkVoidParticles(rootVisual.transform, unlitShader);
        }

        private void Build3DSpiralArms(
            Transform hubParent,
            Shader solidShader,
            int armCount,
            int segmentsPerArm,
            float innerRadius,
            float outerRadius,
            float twistAngleDeg,
            float zThickness,
            Color innerColor,
            Color outerColor,
            Color innerEmission,
            Color outerEmission,
            bool includeDarkChannels)
        {
            Material darkChannelMat = includeDarkChannels
                ? CreateEmissiveMaterial(
                    solidShader,
                    "DarkVortexChannel_Mat",
                    new Color(0.02f, 0.003f, 0.003f),
                    new Color(0.04f, 0.005f, 0.005f),
                    null)
                : null;

            for (int s = 0; s < segmentsPerArm; s++)
            {
                float t = segmentsPerArm > 1 ? (float)s / (segmentsPerArm - 1) : 0f;
                float radius = Mathf.Lerp(innerRadius, outerRadius, t);
                float spiralOffsetDeg = t * twistAngleDeg;

                Color segColor = Color.Lerp(innerColor, outerColor, t);
                Color segEmission = Color.Lerp(innerEmission, outerEmission, t);

                Material segMat = CreateEmissiveMaterial(
                    solidShader,
                    $"VortexSpiralSeg_{s}_Mat",
                    segColor,
                    segEmission,
                    null
                );
                RegisterVortexMaterial(segMat, segEmission);

                float segWidth = Mathf.Lerp(0.07f, 0.14f, t);
                float segLength = Mathf.Lerp(0.16f, 0.30f, t);

                for (int a = 0; a < armCount; a++)
                {
                    float baseAngleDeg = a * (360f / armCount);
                    float totalAngleDeg = baseAngleDeg + spiralOffsetDeg;
                    float rad = totalAngleDeg * Mathf.Deg2Rad;

                    GameObject flameSeg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    flameSeg.name = $"FlameSpiral_{a}_{s}";
                    flameSeg.transform.SetParent(hubParent, false);
                    flameSeg.transform.localPosition = new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0f);
                    flameSeg.transform.localRotation = Quaternion.Euler(0f, 0f, totalAngleDeg - 55f);
                    flameSeg.transform.localScale = new Vector3(segWidth, segLength, zThickness);
                    Destroy(flameSeg.GetComponent<Collider>());
                    flameSeg.GetComponent<MeshRenderer>().sharedMaterial = segMat;

                    if (includeDarkChannels && darkChannelMat != null)
                    {
                        float darkAngleDeg = totalAngleDeg + (180f / armCount);
                        float darkRad = darkAngleDeg * Mathf.Deg2Rad;

                        GameObject darkSeg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                        darkSeg.name = $"AbyssChannel_{a}_{s}";
                        darkSeg.transform.SetParent(hubParent, false);
                        darkSeg.transform.localPosition = new Vector3(Mathf.Cos(darkRad) * radius, Mathf.Sin(darkRad) * radius, 0f);
                        darkSeg.transform.localRotation = Quaternion.Euler(0f, 0f, darkAngleDeg - 55f);
                        darkSeg.transform.localScale = new Vector3(segWidth * 0.85f, segLength * 0.95f, zThickness + 0.008f);
                        Destroy(darkSeg.GetComponent<Collider>());
                        darkSeg.GetComponent<MeshRenderer>().sharedMaterial = darkChannelMat;
                    }
                }
            }
        }

        private void RegisterVortexMaterial(Material mat, Color baseEmission)
        {
            if (mat == null) return;
            vortexEmissiveMaterials.Add(mat);
            baseEmissionColors.Add(baseEmission);
        }

        private Material CreateEmissiveMaterial(Shader solidShader, string name, Color albedo, Color emission, Texture2D tex)
        {
            Material mat = new Material(solidShader)
            {
                name = name,
                color = albedo,
                mainTexture = tex
            };
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.05f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.0f);
            mat.EnableKeyword("_EMISSION");
            if (tex != null && mat.HasProperty("_EmissionMap"))
            {
                mat.SetTexture("_EmissionMap", tex);
            }
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", emission);
            }
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            createdRuntimeMaterials.Add(mat);
            return mat;
        }

        private static Texture2D GenerateSpiralDiscTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            Color32[] pixels = new Color32[size * size];
            float half = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);

                    float spiral = Mathf.Sin(angle * 6.0f + r * 14.0f) * 0.5f + 0.5f;
                    float rings = Mathf.Sin(r * 18.0f) * 0.5f + 0.5f;
                    float v = Mathf.SmoothStep(0.15f, 0.85f, spiral * 0.7f + rings * 0.3f);

                    Color c = Color.Lerp(new Color(0.08f, 0.01f, 0.01f, 1f), new Color(1.0f, 0.32f, 0.06f, 1f), v);
                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        private void CreateDarkVoidParticles(Transform parent, Shader shader)
        {
            GameObject psObj = new GameObject("DarkVoidParticles");
            psObj.transform.SetParent(parent, false);
            psObj.transform.localPosition = new Vector3(0f, 1.15f, 0.25f);

            ParticleSystem ps = psObj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 2.0f;
            main.loop = true;
            main.startLifetime = 1.1f;
            main.startSpeed = 0.04f;
            main.startSize = 0.16f;
            Color darkAsh1 = new Color(0.55f, 0.14f, 0.04f, 0.60f);
            Color darkAsh2 = new Color(0.18f, 0.04f, 0.02f, 0.45f);
            main.startColor = new ParticleSystem.MinMaxGradient(darkAsh1, darkAsh2);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = ps.emission;
            emission.rateOverTime = 12f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.55f;
            shape.radiusThickness = 1.0f;
            shape.position = Vector3.zero;
            shape.rotation = Vector3.zero;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);
            vel.y = new ParticleSystem.MinMaxCurve(0.01f, 0.05f);
            vel.z = new ParticleSystem.MinMaxCurve(0.01f, 0.04f);

            var renderer = psObj.GetComponent<ParticleSystemRenderer>();
            Material mistMat = new Material(shader) { color = new Color(0.15f, 0.04f, 0.02f, 0.55f) };
            createdRuntimeMaterials.Add(mistMat);
            renderer.sharedMaterial = mistMat;

            ps.Play();
        }

        public void UpdateVFX(float pulse, bool isHit, Color hitFlashColor)
        {
            float dt = Time.deltaTime;

            if (outerSpiralHub != null)
            {
                outerSpiralHub.Rotate(0f, 0f, -65f * dt, Space.Self);
            }
            if (mainSpiralHub != null)
            {
                mainSpiralHub.Rotate(0f, 0f, -115f * dt, Space.Self);
            }
            if (innerMagmaHub != null)
            {
                innerMagmaHub.Rotate(0f, 0f, -175f * dt, Space.Self);
                float scalePulse = 1.0f + Mathf.Sin(Time.time * 4.5f) * 0.06f;
                innerMagmaHub.localScale = new Vector3(scalePulse, scalePulse, 1.0f);
            }
            if (coreRingHub != null)
            {
                float coreScale = 1.0f + Mathf.Cos(Time.time * 3.2f) * 0.08f;
                coreRingHub.localScale = new Vector3(coreScale, coreScale, 1.0f);
            }

            float glowMultiplier = 0.85f + pulse * 0.55f;

            for (int i = 0; i < vortexEmissiveMaterials.Count; i++)
            {
                Material mat = vortexEmissiveMaterials[i];
                if (mat == null) continue;

                if (isHit)
                {
                    mat.color = hitFlashColor;
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.SetColor("_EmissionColor", hitFlashColor * 3.0f);
                    }
                }
                else
                {
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.SetColor("_EmissionColor", baseEmissionColors[i] * glowMultiplier);
                    }
                }
            }

            if (portalLight != null)
            {
                portalLight.intensity = Mathf.Lerp(1.4f, 2.8f, pulse);
            }
        }

        public void PlayHitSpark(Vector3 position, Vector3 forward)
        {
            // Zero-GC: Tái sử dụng Object Pool 16 tia lửa hiệu năng cao từ TracerBullet thay vì cấp phát mới GameObject
            Vector3 sparkPos = position + forward * 0.25f + Vector3.up * 1.15f + UnityEngine.Random.insideUnitSphere * 0.15f;
            TracerBullet.SpawnImpactSparks(sparkPos, forward);
        }

        public void PlayCollapseSequence(Transform targetTransform, Action onComplete)
        {
            StartCoroutine(CollapseSequenceCoroutine(targetTransform, onComplete));
        }

        private IEnumerator CollapseSequenceCoroutine(Transform targetTransform, Action onComplete)
        {
            if (targetTransform == null) yield break;

            Vector3 initialScale = targetTransform.localScale;
            float duration = 0.6f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (targetTransform == null) yield break;
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = Mathf.Lerp(1f, 0f, t * t);
                targetTransform.localScale = initialScale * scale;
                yield return null;
            }

            if (targetTransform != null)
            {
                SpawnExplosionShockwave(targetTransform.position, targetTransform.forward);
            }

            onComplete?.Invoke();
        }

        public void SpawnExplosionShockwave(Vector3 position, Vector3 forward)
        {
            GameObject shockObj = new GameObject("VoidExplosionShockwave");
            shockObj.transform.position = position + forward * 0.2f + Vector3.up * 1.15f;

            ParticleSystem ps = shockObj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.25f;
            main.loop = false;
            main.startLifetime = 0.25f;
            main.startSpeed = 2.5f;
            main.startSize = 0.16f;
            main.startColor = new Color(0.20f, 0.05f, 0.05f, 0.8f);
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20) });

            var renderer = shockObj.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = TracerBullet.TracerMaterial;

            ps.Play();
            Destroy(shockObj, 0.4f);
        }

        private void OnDestroy()
        {
            CleanupMaterials();
        }

        public void CleanupMaterials()
        {
            for (int i = 0; i < createdRuntimeMaterials.Count; i++)
            {
                if (createdRuntimeMaterials[i] != null)
                {
                    Destroy(createdRuntimeMaterials[i]);
                }
            }
            createdRuntimeMaterials.Clear();
            vortexEmissiveMaterials.Clear();
            baseEmissionColors.Clear();
        }
    }
}
