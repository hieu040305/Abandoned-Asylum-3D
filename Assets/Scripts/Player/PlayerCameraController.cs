using AbandonedAsylum.Interfaces;
using AbandonedAsylum.Core.Input;
using UnityEngine;
using Cinemachine;
using AbandonedAsylum.Core;

namespace AbandonedAsylum.Player
{
    [RequireComponent(typeof(Camera))]
    [RequireComponent(typeof(CinemachineBrain))]
    [DefaultExecutionOrder(-100)]
    public class PlayerCameraController : MonoBehaviour
    {
        private IInputService inputService;
        private IInputService InputService => inputService ??= (ServiceLocator.TryGet(out IInputService svc) ? svc : InputManagerService.Active);

        [Header("Target & Follow Anchors")]
        public Transform target;
        [SerializeField] private Transform followTarget;
        [SerializeField] private Transform lookTarget;

        [Header("Cinemachine Cameras")]
        [SerializeField] private CinemachineBrain brain;
        [SerializeField] private CinemachineVirtualCamera vcamNormal;
        [SerializeField] private CinemachineVirtualCamera vcamAim;

        [Header("TPS Perspective Offsets")]
        [SerializeField] private Vector3 normalOffset = new Vector3(0.45f, 1.45f, -2.2f);
        [SerializeField] private Vector3 aimOffset = new Vector3(0.55f, 1.50f, -1.3f);
        [SerializeField] private float normalFOV = 60.0f;
        [SerializeField] private float aimFOV = 42.0f;
        [SerializeField] private float aimBlendDuration = 0.18f;

        [Header("Rotation & Sensitivity")]
        [SerializeField] private float mouseSensitivity = 2.5f;
        [SerializeField] private float minPitch = -35.0f;
        [SerializeField] private float maxPitch = 65.0f;

        [Header("Wall Collision")]
        [SerializeField] private LayerMask collisionLayers;
        [SerializeField] private float collisionRadius = 0.2f;

        private float currentYaw = 0.0f;
        private float currentPitch = 10.0f;
        private Camera cam;
        private bool isAiming = false;
        private CinemachineTransposer transposerNormal;
        private CinemachineTransposer transposerAim;

        public bool IsAiming => isAiming;
        public float CurrentYaw => currentYaw;
        public CinemachineBrain Brain => brain;
        public CinemachineVirtualCamera VcamNormal => vcamNormal;
        public CinemachineVirtualCamera VcamAim => vcamAim;
        public Transform FollowTarget => followTarget;
        public Transform LookTarget => lookTarget;

        public void AddRecoil(float verticalRecoil = 0.7f, float horizontalRecoil = 0.2f)
        {
            currentPitch -= verticalRecoil;
            currentYaw += Random.Range(-horizontalRecoil, horizontalRecoil);
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
            ApplyFollowTargetTransform();
        }

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;

            if (collisionLayers.value == 0)
            {
                collisionLayers = ~LayerMask.GetMask("Ignore Raycast", "Water", "Player");
            }

            SetupCinemachine();
        }

        private void Start()
        {
            bool isPlaying = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing;
            Cursor.lockState = isPlaying ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isPlaying;

            if (target != null)
            {
                currentYaw = target.eulerAngles.y;
            }

            ApplyFollowTargetTransform();
        }

        public void SetupCinemachine()
        {
            if (cam == null) cam = GetComponent<Camera>() ?? Camera.main;

            brain = cam.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                brain = cam.gameObject.AddComponent<CinemachineBrain>();
            }

            brain.m_UpdateMethod = CinemachineBrain.UpdateMethod.LateUpdate;
            brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, aimBlendDuration);

            if (target == null)
            {
                if (PlayerRegistry.HasPlayer)
                {
                    target = PlayerRegistry.PlayerTransform;
                }
                else
                {
                    var playerObj = GameObject.FindWithTag("Player");
                    if (playerObj != null) target = playerObj.transform;
                }
            }

            // Create or bind follow pivot
            if (followTarget == null && target != null)
            {
                Transform existingPivot = target.Find("CM_FollowTarget");
                if (existingPivot != null)
                {
                    followTarget = existingPivot;
                }
                else
                {
                    var pivotObj = new GameObject("CM_FollowTarget");
                    pivotObj.transform.SetParent(target, false);
                    pivotObj.transform.localPosition = Vector3.zero;
                    followTarget = pivotObj.transform;
                }
            }

            // Create or bind look target (head height)
            if (lookTarget == null && target != null)
            {
                Transform existingLook = target.Find("CM_LookTarget");
                if (existingLook != null)
                {
                    lookTarget = existingLook;
                }
                else
                {
                    var lookObj = new GameObject("CM_LookTarget");
                    lookObj.transform.SetParent(target, false);
                    lookObj.transform.localPosition = new Vector3(0f, 1.5f, 0f);
                    lookTarget = lookObj.transform;
                }
            }

            // Setup Normal Virtual Camera
            if (vcamNormal == null)
            {
                GameObject existingVcam = GameObject.Find("CM_vcam_Normal");
                if (existingVcam != null)
                {
                    vcamNormal = existingVcam.GetComponent<CinemachineVirtualCamera>();
                }
                else
                {
                    GameObject vcamObj = new GameObject("CM_vcam_Normal");
                    vcamNormal = vcamObj.AddComponent<CinemachineVirtualCamera>();
                }
            }

            if (vcamNormal != null)
            {
                vcamNormal.m_Priority = 10;
                vcamNormal.m_Lens.FieldOfView = normalFOV;
                if (followTarget != null) vcamNormal.Follow = followTarget;
                if (lookTarget != null) vcamNormal.LookAt = lookTarget;

                // Body: Transposer in LockToTarget mode matches the classic TPS perspective
                transposerNormal = vcamNormal.GetCinemachineComponent<CinemachineTransposer>();
                if (transposerNormal == null)
                {
                    transposerNormal = vcamNormal.AddCinemachineComponent<CinemachineTransposer>();
                }
                transposerNormal.m_BindingMode = CinemachineTransposer.BindingMode.LockToTarget;
                transposerNormal.m_FollowOffset = normalOffset;
                transposerNormal.m_XDamping = 0f;
                transposerNormal.m_YDamping = 0f;
                transposerNormal.m_ZDamping = 0f;

                // Aim: SameAsFollowTarget keeps rotation locked to player mouse look
                var aimComp = vcamNormal.GetCinemachineComponent<CinemachineSameAsFollowTarget>();
                if (aimComp == null)
                {
                    aimComp = vcamNormal.AddCinemachineComponent<CinemachineSameAsFollowTarget>();
                }
                aimComp.m_Damping = 0f;

                // Collision: CinemachineCollider prevents clipping through walls
                var colliderComp = vcamNormal.GetComponent<CinemachineCollider>();
                if (colliderComp == null)
                {
                    colliderComp = vcamNormal.gameObject.AddComponent<CinemachineCollider>();
                }
                colliderComp.m_CollideAgainst = collisionLayers;
                colliderComp.m_CameraRadius = collisionRadius;
                colliderComp.m_Strategy = CinemachineCollider.ResolutionStrategy.PullCameraForward;
                colliderComp.m_IgnoreTag = "Player";
                colliderComp.m_DampingWhenOccluded = 0f;
                colliderComp.m_Damping = 0.3f;
                colliderComp.m_MinimumDistanceFromTarget = 0.3f;
            }

            // Setup Aim Virtual Camera
            if (vcamAim == null)
            {
                GameObject existingAim = GameObject.Find("CM_vcam_Aim");
                if (existingAim != null)
                {
                    vcamAim = existingAim.GetComponent<CinemachineVirtualCamera>();
                }
                else
                {
                    GameObject aimObj = new GameObject("CM_vcam_Aim");
                    vcamAim = aimObj.AddComponent<CinemachineVirtualCamera>();
                }
            }

            if (vcamAim != null)
            {
                vcamAim.m_Priority = 5;
                vcamAim.m_Lens.FieldOfView = aimFOV;
                if (followTarget != null) vcamAim.Follow = followTarget;
                if (lookTarget != null) vcamAim.LookAt = lookTarget;

                transposerAim = vcamAim.GetCinemachineComponent<CinemachineTransposer>();
                if (transposerAim == null)
                {
                    transposerAim = vcamAim.AddCinemachineComponent<CinemachineTransposer>();
                }
                transposerAim.m_BindingMode = CinemachineTransposer.BindingMode.LockToTarget;
                transposerAim.m_FollowOffset = aimOffset;
                transposerAim.m_XDamping = 0f;
                transposerAim.m_YDamping = 0f;
                transposerAim.m_ZDamping = 0f;

                var aimCompAim = vcamAim.GetCinemachineComponent<CinemachineSameAsFollowTarget>();
                if (aimCompAim == null)
                {
                    aimCompAim = vcamAim.AddCinemachineComponent<CinemachineSameAsFollowTarget>();
                }
                aimCompAim.m_Damping = 0f;

                var colliderCompAim = vcamAim.GetComponent<CinemachineCollider>();
                if (colliderCompAim == null)
                {
                    colliderCompAim = vcamAim.gameObject.AddComponent<CinemachineCollider>();
                }
                colliderCompAim.m_CollideAgainst = collisionLayers;
                colliderCompAim.m_CameraRadius = collisionRadius;
                colliderCompAim.m_Strategy = CinemachineCollider.ResolutionStrategy.PullCameraForward;
                colliderCompAim.m_IgnoreTag = "Player";
                colliderCompAim.m_DampingWhenOccluded = 0f;
                colliderCompAim.m_Damping = 0.3f;
                colliderCompAim.m_MinimumDistanceFromTarget = 0.3f;
            }
        }

        private void Update()
        {
            bool isPaused = GameManager.IsGamePaused;
            bool isMenuOrEnded = isPaused || GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing;
            if (isMenuOrEnded)
            {
                isAiming = false;
                if (vcamAim != null) vcamAim.m_Priority = 5;
                if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                return;
            }

            if (InputService.IsPausePressed)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if ((InputService.IsFireDown || InputService.IsAimPressed) && Cursor.lockState != CursorLockMode.Locked)
            {
                if (!isPaused && GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 look = InputService.LookInput;
                currentYaw += look.x * mouseSensitivity;
                currentPitch -= look.y * mouseSensitivity;
                currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
            }

            isAiming = InputService.IsAimPressed;
            if (vcamAim != null)
            {
                vcamAim.m_Priority = isAiming ? 20 : 5;
            }

            ApplyFollowTargetTransform();
        }

        private void LateUpdate()
        {
            ApplyFollowTargetTransform();
        }

        private void ApplyFollowTargetTransform()
        {
            if (followTarget != null)
            {
                if (target != null)
                {
                    followTarget.position = target.position;
                }
                followTarget.rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            }

            if (lookTarget != null && target != null)
            {
                lookTarget.position = target.position + Vector3.up * 1.5f;
            }
        }
    }
}
