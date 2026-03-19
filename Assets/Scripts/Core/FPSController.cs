using UnityEngine;

namespace Core
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(CharacterController))]
    public class FPSController : MonoBehaviour
    {
        [Header("References")]
        public Transform cameraContainer;
        public Transform headBone;

        private CharacterController _cc;
        private IInputProvider _inputProvider;

        [Header("Movement Settings")]
        public float walkSpeed = 5f;
        public float runSpeed = 9f;
        public float crouchSpeed = 2.5f;
        public float speedChangeRate = 10f;

        // 🌟 新增：空中控制系统 (Air Control)
        [Header("Air Control Settings")]
        [Range(0f, 1f)]
        public float airSpeedMultiplier = 0.5f; // 空中横向速度衰减 (50% 移动力)
        public float airSmoothTime = 0.4f;      // 空中惯性阻尼 (越大在空中越难改变方向，松手后滑行越远)

        [Header("Jump & Gravity")]
        // 🌟 修改：1.2m 是超人，0.6m 是标准战术成年男性的真实垂直起跳高度
        public float jumpHeight = 0.6f;
        public float gravity = -15.0f; // 保持 -15，这比地球重力(-9.8)大，能带来射击游戏需要的“干脆落地感”
        public float terminalVelocity = 53.0f;

        [Header("Crouch System")]
        public float crouchHeight = 1.0f;
        public float standHeight = 2.0f;
        public float crouchTransitionSpeed = 10f;
        public Vector3 crouchCenter = new Vector3(0, 0.5f, 0);
        public Vector3 standCenter = new Vector3(0, 1f, 0);

        [Header("Look & Camera Settings")]
        public float lookClampTop = 170f;
        public float lookClampBottom = 90f;
        public float cameraTrackingSpeed = 20f;

        // Runtime State
        private float _cameraPitch = 0f;
        private float _currentSpeed;
        private Vector3 _velocity;
        private float _targetHeight;
        private Vector3 _targetCenter;

        private float _speedVelocity;
        public float speedSmoothTime = 0.1f;

        private Quaternion _originalRotation;

        // 🌟 新增：记录空中松手瞬间的惯性方向
        private Vector3 _airMomentumDirection = Vector3.zero;

        [Header("Animation")]
        public Animator animator;
        private readonly int SPEED = Animator.StringToHash("Speed");
        private readonly int _animVerticalVelocityHash = Animator.StringToHash("VerticalVelocity");
        private readonly int _animIsGroundedHash = Animator.StringToHash("IsGrounded");


        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _inputProvider = GetComponent<IInputProvider>();
            _targetHeight = standHeight;
            _targetCenter = standCenter;
            _originalRotation = cameraContainer.localRotation;
            animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            if (_inputProvider == null) return;
            FrameInput input = _inputProvider.GetInput();

            HandleLook(input);
            HandleCrouch(input);

            Vector3 horizontalMove = CalculateMovement(input);
            Vector3 verticalMove = CalculateGravityAndJump(input);

            _cc.Move((horizontalMove + verticalMove) * Time.deltaTime);
            // ==========================================================
            // 向 Animator 传递底层平滑速度，驱动下半身 Locomotion
            // ==========================================================
            if (animator != null)
            {
                // 🌟 新增：把垂直速度和地面状态实时传给动画机
                animator.SetFloat(_animVerticalVelocityHash, _velocity.y);
                animator.SetBool(_animIsGroundedHash, _cc.isGrounded);
            }
        }

        // private void LateUpdate()
        // {
        //     if (cameraContainer != null && headBone != null)
        //     {
        //         cameraContainer.position = Vector3.Lerp(cameraContainer.position, headBone.position, Time.deltaTime * cameraTrackingSpeed);
        //     }
        // }

        private void HandleLook(FrameInput input)
        {
            // 🌟 无论在地面还是空中，视角旋转始终生效，带动整个身体转动
            transform.Rotate(Vector3.up * input.Look.x);
            _cameraPitch -= input.Look.y;
            _cameraPitch = Mathf.Clamp(_cameraPitch, lookClampBottom, lookClampTop);
            cameraContainer.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
        }

        private Vector3 CalculateMovement(FrameInput input)
        {
            float targetSpeed = walkSpeed;
            if (input.Sprint && !input.Crouch) targetSpeed = runSpeed;
            if (input.Crouch) targetSpeed = crouchSpeed;

            // 🌟 核心分轨：判断是在地面还是空中
            if (_cc.isGrounded)
            {
                // ========== 地面逻辑 (绝对防穿模) ==========
                if (input.Move == Vector2.zero)
                {
                    targetSpeed = 0f;
                    _airMomentumDirection = Vector3.zero; // 落地瞬间清空空中惯性
                    //return Vector3.zero; // 地面松手瞬间死死停住，完美配合 IK
                }

                _currentSpeed = Mathf.SmoothDamp(_currentSpeed, targetSpeed, ref _speedVelocity, speedSmoothTime);
                Vector3 direction = (transform.right * input.Move.x + transform.forward * input.Move.y).normalized;
                _airMomentumDirection = direction; // 随时记录起跳前最后一刻的方向
                animator.SetFloat(SPEED,_currentSpeed);

                return direction * _currentSpeed;
            }
            else
            {
                // ========== 空中逻辑 (真实物理惯性) ==========
                targetSpeed *= airSpeedMultiplier; // 空中最大横向速度减半

                if (input.Move == Vector2.zero)
                {
                    targetSpeed = 0.0f;
                    // 在空中松开键盘，速度不会瞬间变0，而是像降落伞一样逐渐衰减
                }
                else
                {
                    // 在空中按住 WASD，方向会实时跟随你的鼠标视角转动
                    _airMomentumDirection = (transform.right * input.Move.x + transform.forward * input.Move.y).normalized;
                }

                // 使用专门的 airSmoothTime 带来更粘稠的空中滞空感
                _currentSpeed = Mathf.SmoothDamp(_currentSpeed, targetSpeed, ref _speedVelocity, airSmoothTime);

                return _airMomentumDirection * _currentSpeed;
            }
        }

        private Vector3 CalculateGravityAndJump(FrameInput input)
        {
            if ((_cc.collisionFlags & CollisionFlags.Above) != 0 && _velocity.y > 0)
            {
                _velocity.y = 0;
            }

            if (_cc.isGrounded)
            {
                if (_velocity.y < 0.0f) _velocity.y = -2f;

                if (input.Jump)
                {
                    _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }

            if (_velocity.y > -terminalVelocity)
            {
                _velocity.y += gravity * Time.deltaTime;
            }

            return _velocity;
        }

        private void HandleCrouch(FrameInput input)
        {
            bool isTryingToCrouch = input.Crouch;

            float castDistance = standHeight - crouchHeight;
            if (!isTryingToCrouch && Physics.SphereCast(transform.position + crouchCenter, _cc.radius, Vector3.up, out _, castDistance))
            {
                isTryingToCrouch = true;
            }

            _targetHeight = isTryingToCrouch ? crouchHeight : standHeight;
            _targetCenter = isTryingToCrouch ? crouchCenter : standCenter;

            _cc.height = Mathf.Lerp(_cc.height, _targetHeight, Time.deltaTime * crouchTransitionSpeed);
            _cc.center = Vector3.Lerp(_cc.center, _targetCenter, Time.deltaTime * crouchTransitionSpeed);
        }
    }
}
