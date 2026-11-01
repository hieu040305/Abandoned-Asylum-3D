using AbandonedAsylum.Interfaces;
using AbandonedAsylum.Core;
using AbandonedAsylum.Core.Input;
using UnityEngine;

namespace AbandonedAsylum.Player
{
    /// <summary>
    /// Quản lý logic di chuyển, nhảy, trọng lực và cập nhật tham số Animator
    /// cho nhân vật người chơi.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public class PlayerLocomotion : MonoBehaviour
    {
        private IInputService inputService;
        private IInputService InputService => inputService ??= (ServiceLocator.TryGet(out IInputService svc) ? svc : InputManagerService.Active);

        [Header("Locomotion Parameters")]
        [SerializeField] private float walkSpeed = 2.4f;
        [SerializeField] private float runSpeed = 4.8f;
        [SerializeField] private float sprintSpeed = 7.2f;
        [SerializeField] private float gravity = -18.0f;
        [SerializeField] private float jumpHeight = 1.4f;

        private CharacterController controller;
        private Animator animator;
        private PlayerCameraController camController;

        private Vector3 velocity;
        private bool isSprinting = false;
        private bool isAiming = false;
        private Vector3 moveDirection = Vector3.zero;
        private static readonly int AnimHashHorizontal = Animator.StringToHash("Horizontal");
        private static readonly int AnimHashVertical = Animator.StringToHash("Vertical");
        private static readonly int AnimHashIsAiming = Animator.StringToHash("IsAiming");
        private static readonly int AnimHashIsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int AnimHashJump = Animator.StringToHash("Jump");

        private LayerMask groundCheckMask;

        public bool IsMoving => controller != null && controller.velocity.sqrMagnitude > 0.05f;
        public bool IsSprinting => isSprinting;
        public bool IsAiming => isAiming;
        public float WalkSpeed => walkSpeed;
        public float RunSpeed => runSpeed;
        public float SprintSpeed => sprintSpeed;

        public void Initialize(CharacterController cc, Animator anim, PlayerCameraController cam)
        {
            controller = cc;
            animator = anim;
            camController = cam;
        }

        private void Awake()
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            if (animator == null) animator = GetComponent<Animator>();
            if (camController == null) camController = FindFirstObjectByType<PlayerCameraController>();
            groundCheckMask = ~LayerMask.GetMask("Ignore Raycast", "Player");
        }

        public void SetLocomotionParameters(float walk, float run, float sprint, float grav, float jump)
        {
            walkSpeed = walk;
            runSpeed = run;
            sprintSpeed = sprint;
            gravity = grav;
            jumpHeight = jump;
        }

        public void TickLocomotion(bool isDead, bool isPaused)
        {
            if (isDead || isPaused)
            {
                if (animator != null)
                {
                    animator.SetFloat(AnimHashHorizontal, 0f, 0.1f, Time.deltaTime);
                    animator.SetFloat(AnimHashVertical, 0f, 0.1f, Time.deltaTime);
                }
                return;
            }

            HandleMovement();
        }

        private void HandleMovement()
        {
            if (controller == null) return;

            bool isGrounded = controller.isGrounded || Physics.Raycast(
                transform.position + Vector3.up * 0.2f, 
                Vector3.down, 
                0.40f, 
                groundCheckMask
            );

            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2.0f;
            }

            Vector2 move = InputService.MoveInput;
            float horizontal = move.x;
            float vertical = move.y;

            Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

            isAiming = InputService.IsAimPressed;
            isSprinting = InputService.IsSprintPressed && vertical > 0.1f && !isAiming;
            float currentSpeed = walkSpeed;

            if (isSprinting) currentSpeed = sprintSpeed;
            else if (inputDir.magnitude > 0.1f && !isAiming) currentSpeed = runSpeed;

            if (camController != null)
            {
                float targetAngle = camController.CurrentYaw;
                transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);
            }

            moveDirection = Vector3.zero;
            if (inputDir.magnitude >= 0.1f)
            {
                Vector3 camForward = camController != null ? camController.transform.forward : transform.forward;
                camForward.y = 0f;
                camForward.Normalize();

                Vector3 camRight = camController != null ? camController.transform.right : transform.right;
                camRight.y = 0f;
                camRight.Normalize();

                moveDirection = (camForward * vertical + camRight * horizontal).normalized;
            }

            if (InputService.IsJumpDown && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
                if (animator != null) animator.SetTrigger(AnimHashJump);
            }

            velocity.y += gravity * Time.deltaTime;
            velocity.y = Mathf.Max(velocity.y, -25.0f);

            controller.Move((moveDirection * currentSpeed + velocity) * Time.deltaTime);

            if (animator != null)
            {
                float animHorizontal = horizontal;
                float animVertical = vertical;

                if (isSprinting) animVertical *= 2.0f;
                else if (inputDir.magnitude > 0.1f && !isAiming) animVertical *= 1.5f;

                animator.SetFloat(AnimHashHorizontal, animHorizontal, 0.1f, Time.deltaTime);
                animator.SetFloat(AnimHashVertical, animVertical, 0.1f, Time.deltaTime);
                animator.SetBool(AnimHashIsAiming, isAiming);
                animator.SetBool(AnimHashIsGrounded, isGrounded);
            }
        }
    }
}
