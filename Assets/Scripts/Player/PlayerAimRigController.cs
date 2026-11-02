using UnityEngine;
using UnityEngine.Animations.Rigging;
using AbandonedAsylum.Interfaces;
using AbandonedAsylum.Core;
using AbandonedAsylum.Core.Input;

namespace AbandonedAsylum.Player
{
    /// <summary>
    /// Điều khiển hướng ngắm của cột sống (Procedural Spine Alignment)
    /// và điều chỉnh trọng số Animation Rigging (Two-Bone IK) cho tay phụ trợ.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerAimRigController : MonoBehaviour
    {
        private IInputService inputService;
        private IInputService InputService => inputService ??= (ServiceLocator.TryGet(out IInputService svc) ? svc : InputManagerService.Active);

        [Header("Aim Alignment Settings")]
        [SerializeField] private bool enableAimAlignment = true;
        [SerializeField] private Transform spine1Bone;
        [SerializeField] private Transform spine2Bone;
        [SerializeField] private float maxPitchAngle = 35.0f;
        [SerializeField] private float maxYawAngle = 35.0f;
        [SerializeField] private float minForwardAimDistance = 0.8f;

        [Header("Aim Gizmo (Editor Only)")]
        [Tooltip("Hiển thị đường ngắm trong Scene View khi căn chỉnh thông số nòng súng.")]
        [SerializeField] private bool showAimGizmos = true;

        [Tooltip("Chiều dài hiển thị tối đa của tia căn chỉnh (mét).")]
        [SerializeField] private float aimRayLength = 40.0f;

        [Tooltip("Màu sắc của tia căn chỉnh trong Scene View.")]
        [SerializeField] private Color aimGizmoColor = Color.yellow;

        [Header("Animation Rigging & Hand IK")]
        [SerializeField] private Rig handIKRig;
        [SerializeField] private TwoBoneIKConstraint leftHandIKConstraint;

        private float currentHandIKWeight = 1.0f;
        private float currentAimWeight = 0.0f;
        private readonly RaycastHit[] aimHitBuffer = new RaycastHit[8];

        private Camera cachedMainCam;
        private Transform bulletSpawnPoint;
        private Transform aimReferencePoint;
        private Vector3 currentAimTargetPoint;
        private bool hasValidAimTarget = false;

        public bool EnableAimAlignment
        {
            get => enableAimAlignment;
            set => enableAimAlignment = value;
        }

        public bool ShowAimGizmos
        {
            get => showAimGizmos;
            set => showAimGizmos = value;
        }

        public float AimRayLength
        {
            get => aimRayLength;
            set => aimRayLength = Mathf.Max(1.0f, value);
        }

        public Color AimGizmoColor
        {
            get => aimGizmoColor;
            set => aimGizmoColor = value;
        }

        public Transform Spine1Bone => spine1Bone;
        public Transform Spine2Bone => spine2Bone;
        public Rig HandIKRig => handIKRig;
        public TwoBoneIKConstraint LeftHandIKConstraint => leftHandIKConstraint;
        public Vector3 CurrentAimTarget => currentAimTargetPoint;
        public bool HasValidAimTarget => hasValidAimTarget;

        public void SetAimLimits(float pitch, float yaw, float minDistance)
        {
            maxPitchAngle = pitch;
            maxYawAngle = yaw;
            minForwardAimDistance = minDistance;
        }

        public void SetupBonesAndRigs(Animator animator, Transform customSpine1 = null, Transform customSpine2 = null, Rig customRig = null, TwoBoneIKConstraint customIK = null)
        {
            if (customSpine1 != null) spine1Bone = customSpine1;
            if (customSpine2 != null) spine2Bone = customSpine2;
            if (customRig != null) handIKRig = customRig;
            if (customIK != null) leftHandIKConstraint = customIK;

            SetupSpineBones(animator);
            SetupHandIK();
        }

        public void SetWeaponAimReferences(Transform spawnPoint, Transform aimPoint)
        {
            bulletSpawnPoint = spawnPoint;
            aimReferencePoint = aimPoint;
        }

        public void SetupSpineBones(Animator animator)
        {
            if (animator == null) return;

            if (spine2Bone == null)
            {
                spine2Bone = animator.GetBoneTransform(HumanBodyBones.Chest)
                          ?? animator.GetBoneTransform(HumanBodyBones.UpperChest);
                if (spine2Bone == null)
                {
                    spine2Bone = transform.Find("mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2");
                }
            }

            if (spine1Bone == null)
            {
                spine1Bone = animator.GetBoneTransform(HumanBodyBones.Spine);
                if (spine1Bone == null)
                {
                    spine1Bone = transform.Find("mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1");
                }
            }
        }

        public void SetupHandIK()
        {
            if (handIKRig == null)
            {
                handIKRig = GetComponentInChildren<Rig>();
            }

            if (leftHandIKConstraint == null)
            {
                leftHandIKConstraint = GetComponentInChildren<TwoBoneIKConstraint>();
            }

            if (handIKRig != null)
            {
                currentHandIKWeight = handIKRig.weight;
            }
            else if (leftHandIKConstraint != null)
            {
                currentHandIKWeight = leftHandIKConstraint.weight;
            }
        }

        public void UpdateHandIK(bool isReloading, bool isDead)
        {
            float targetWeight = (isReloading || isDead) ? 0.0f : 1.0f;
            float blendSpeed = isReloading ? 12.0f : 6.0f;

            // Sử dụng Time.unscaledDeltaTime để bảo toàn góc tay kể cả khi Pause Game (timeScale = 0)
            float dt = Time.unscaledDeltaTime;
            currentHandIKWeight = Mathf.MoveTowards(currentHandIKWeight, targetWeight, dt * blendSpeed);

            if (leftHandIKConstraint != null)
            {
                leftHandIKConstraint.weight = currentHandIKWeight;
            }

            if (handIKRig != null)
            {
                handIKRig.weight = currentHandIKWeight;
            }
        }

        public void AlignWeaponToCrosshair(bool isSprinting, bool isReloading, LayerMask shootableLayers, float range)
        {
            Transform refPoint = GetEffectiveAimOrigin();
            if (!enableAimAlignment || refPoint == null || (spine1Bone == null && spine2Bone == null))
            {
                return;
            }

            float targetWeight = (isSprinting || isReloading) ? 0.0f : 1.0f;

            // Sử dụng Time.unscaledDeltaTime: Giúp tính toán súng vẫn mượt mà khi Pause game (timeScale = 0)
            float dt = Time.unscaledDeltaTime;
            currentAimWeight = Mathf.MoveTowards(currentAimWeight, targetWeight, dt * 6.0f);

            Camera cam = cachedMainCam != null ? cachedMainCam : (cachedMainCam = Camera.main);
            if (cam == null)
            {
                return;
            }

            // 1. Xác định vị trí điểm ngắm trên màn hình
            Vector2 mouseScreen = InputService != null ? InputService.MousePosition : (Vector2)Input.mousePosition;
            Vector3 viewportCenter;

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                viewportCenter = new Vector3(0.5f, 0.5f, 0f);
            }
            else
            {
                if (Screen.width > 0 && Screen.height > 0)
                {
                    viewportCenter = new Vector3(
                        Mathf.Clamp01(mouseScreen.x / Screen.width),
                        Mathf.Clamp01(mouseScreen.y / Screen.height),
                        0f
                    );
                }
                else
                {
                    viewportCenter = new Vector3(0.5f, 0.5f, 0f);
                }
            }

            Ray ray = cam.ViewportPointToRay(viewportCenter);
            Vector3 aimTarget;

            int hitCount = Physics.RaycastNonAlloc(ray, aimHitBuffer, range, shootableLayers, QueryTriggerInteraction.Ignore);
            bool hasHit = false;
            RaycastHit validHit = default;
            float closestDist = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit h = aimHitBuffer[i];
                if (h.transform != transform && !h.transform.IsChildOf(transform))
                {
                    if (h.distance < closestDist)
                    {
                        closestDist = h.distance;
                        validHit = h;
                        hasHit = true;
                    }
                }
            }

            bool hitTooClose = false;
            if (hasHit)
            {
                float forwardDistFromPlayer = Vector3.Dot(validHit.point - transform.position, cam.transform.forward);
                if (forwardDistFromPlayer < minForwardAimDistance)
                {
                    hitTooClose = true;
                }
            }

            if (hasHit && !hitTooClose)
            {
                aimTarget = validHit.point;
            }
            else
            {
                aimTarget = ray.origin + ray.direction * range;
            }

            currentAimTargetPoint = aimTarget;
            hasValidAimTarget = true;

            if (currentAimWeight <= 0.001f) return;

            // 2. Tính toán lượng giác xoay cột sống (Spine1 35% / Spine2 65%)
            Vector3 currentMuzzleDir = refPoint.forward;
            Vector3 desiredMuzzleDir = (aimTarget - refPoint.position).normalized;

            Vector3 localDesired = transform.InverseTransformDirection(desiredMuzzleDir);
            Vector3 localCurrent = transform.InverseTransformDirection(currentMuzzleDir);

            float targetPitch = -Mathf.Asin(Mathf.Clamp(localDesired.y, -1f, 1f)) * Mathf.Rad2Deg;
            float currentPitch = -Mathf.Asin(Mathf.Clamp(localCurrent.y, -1f, 1f)) * Mathf.Rad2Deg;
            float deltaPitch = Mathf.DeltaAngle(currentPitch, targetPitch);
            deltaPitch = Mathf.Clamp(deltaPitch, -maxPitchAngle, maxPitchAngle);

            float targetYaw = Mathf.Atan2(localDesired.x, localDesired.z) * Mathf.Rad2Deg;
            float currentYaw = Mathf.Atan2(localCurrent.x, localCurrent.z) * Mathf.Rad2Deg;
            float deltaYaw = Mathf.DeltaAngle(currentYaw, targetYaw);
            deltaYaw = Mathf.Clamp(deltaYaw, -maxYawAngle, maxYawAngle);

            float weight = currentAimWeight;
            float pitchForSpine2 = deltaPitch * 0.65f * weight;
            float pitchForSpine1 = deltaPitch * 0.35f * weight;
            float yawForSpine2 = deltaYaw * 0.65f * weight;
            float yawForSpine1 = deltaYaw * 0.35f * weight;

            if (spine1Bone != null)
            {
                Quaternion spine1Offset = Quaternion.Euler(pitchForSpine1, yawForSpine1, 0f);
                spine1Bone.rotation = spine1Bone.rotation * spine1Offset;
            }

            if (spine2Bone != null)
            {
                Quaternion spine2Offset = Quaternion.Euler(pitchForSpine2, yawForSpine2, 0f);
                spine2Bone.rotation = spine2Bone.rotation * spine2Offset;
            }
        }

        #region Effective Aim Origin Discovery (Self-Healing)
        public Transform GetEffectiveAimOrigin()
        {
            if (aimReferencePoint != null) return aimReferencePoint;
            if (bulletSpawnPoint != null) return bulletSpawnPoint;

            var weaponCtrl = GetComponent<PlayerWeaponController>();
            if (weaponCtrl != null)
            {
                if (weaponCtrl.AimReferencePoint != null) return weaponCtrl.AimReferencePoint;
                if (weaponCtrl.BulletSpawnPoint != null) return weaponCtrl.BulletSpawnPoint;
            }

            // Fallback Discovery: Tu dong tim kiem trong cay phan cap con (shootpoint, muzzle, firepoint, bulletspawn)
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] == transform) continue;
                string lower = children[i].name.ToLowerInvariant();
                if (lower.Contains("shootpoint") || lower.Contains("muzzle") || lower.Contains("bulletspawn") || lower.Contains("firepoint"))
                {
                    return children[i];
                }
            }

            return transform;
        }
        #endregion

        #region Editor Aim Gizmo
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            DrawAimGizmoInternal();
        }

        private void OnDrawGizmosSelected()
        {
            DrawAimGizmoInternal();
        }

        private void DrawAimGizmoInternal()
        {
            if (!showAimGizmos || !enableAimAlignment) return;

            Transform refPoint = GetEffectiveAimOrigin();
            if (refPoint == null) return;

            Vector3 rayOrigin = refPoint.position;
            Vector3 rayDir = refPoint.forward;
            Vector3 rayEnd = rayOrigin + rayDir * aimRayLength;

            if (Physics.Raycast(rayOrigin, rayDir, out RaycastHit hit, aimRayLength, ~LayerMask.GetMask("Ignore Raycast", "Player")))
            {
                rayEnd = hit.point;
            }

            Gizmos.color = aimGizmoColor;
            Gizmos.DrawLine(rayOrigin, rayEnd);
        }
#endif
        #endregion
    }
}
