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

        [Header("Air Control Settings")]
        [Range(0f, 1f)]
        public float airSpeedMultiplier = 0.5f;
        public float airSmoothTime = 0.4f;

        [Header("Jump & Gravity")]
        public float jumpHeight = 0.6f;
        public float gravity = -15.0f;
        public float terminalVelocity = 53.0f;

        [Header("Crouch System")]
        public float crouchHeight = 1.0f;
        public float standHeight = 2.0f;
        public float crouchTransitionSpeed = 10f;
        public Vector3 crouchCenter = new Vector3(0, 0.5f, 0);
        public Vector3 standCenter = new Vector3(0, 1f, 0);

        [Header("Look & Camera Settings")]
        public float lookClampTop = 170f;
        public float lookClampBottom = 5f;
        public float cameraTrackingSpeed = 20f;

        // ==========================================================
        // 🌟 新增：上半身合理弯曲系统 (Aim Offset) 变量区
        // ==========================================================
        [Header("Aim Offset (Upper Body Bending)")]
        public Transform spine1;
        public Transform spine2;
        public Transform spine3;
        public Transform neck;

        [Range(0f, 1f)] public float spine1Weight = 0.2f;
        [Range(0f, 1f)] public float spine2Weight = 0.3f;
        [Range(0f, 1f)] public float spine3Weight = 0.3f;
        [Range(0f, 1f)] public float neckWeight = 0.2f;

        [Header("Virtual Parenting")]
        [Tooltip("将 ik_hand_root 拖进来，代码会带它跟着 spine3 一起动，且绝对不破坏动画！")]
        public Transform ikHandRoot;

        [Tooltip("因为你的视角范围是90~170，这里填入看向正前方的Pitch值（比如130）。\n如果弯曲方向反了，可以把bendAxis改成 ( -1, 0, 0 )")]
        public float forwardPitchCenter = 90f;
        public Vector3 bendAxis = Vector3.right; // 默认绕X轴前后弯曲 (取决于你的模型骨架)
        // ==========================================================

        // Runtime State
        private float _cameraPitch = 0f;
        private float _currentSpeed;
        private Vector3 _velocity;
        private float _targetHeight;
        private Vector3 _targetCenter;

        private float _speedVelocity;
        public float speedSmoothTime = 0.1f;

        private Quaternion _originalRotation;
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

            // 如果初始_cameraPitch是平视，将其设定为平视中心值
            _cameraPitch = forwardPitchCenter;
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

            if (animator != null)
            {
                animator.SetFloat(_animVerticalVelocityHash, _velocity.y);
                animator.SetBool(_animIsGroundedHash, _cc.isGrounded);
            }
        }

        // ==========================================================
        // 🌟 新增：解除注释的 LateUpdate，处理身体合理弯曲
        // ==========================================================
        private void LateUpdate()
        {
            // ==========================================================
            // 🌟 1. 弯曲前：记录虚拟锚点的绝对关系
            // ==========================================================
            Vector3 ikRootLocalPos = Vector3.zero;
            Quaternion ikRootLocalRot = Quaternion.identity;

            if (spine3 != null && ikHandRoot != null)
            {
                // 此时 Animator 刚算完动画，ik_hand_root 在脚底(0,0,0)
                // 我们算出它相对于没弯曲前的 spine3 的“虚拟局部坐标”
                ikRootLocalPos = Quaternion.Inverse(spine3.rotation) * (ikHandRoot.position - spine3.position);
                ikRootLocalRot = Quaternion.Inverse(spine3.rotation) * ikHandRoot.rotation;
            }

            // ==========================================================
            // 🌟 2. 物理执行：弯曲上半身骨骼
            // ==========================================================
            float bendAngle = _cameraPitch - forwardPitchCenter;

            if (spine1 != null) spine1.localRotation *= Quaternion.AngleAxis(bendAngle * spine1Weight, bendAxis);
            if (spine2 != null) spine2.localRotation *= Quaternion.AngleAxis(bendAngle * spine2Weight, bendAxis);
            if (spine3 != null) spine3.localRotation *= Quaternion.AngleAxis(bendAngle * spine3Weight, bendAxis);
            if (neck != null) neck.localRotation *= Quaternion.AngleAxis(bendAngle * neckWeight, bendAxis);

            // ==========================================================
            // 🌟 3. 弯曲后：执行“虚拟绑定”同步
            // ==========================================================
            if (spine3 != null && ikHandRoot != null)
            {
                // 此时 spine3 已经弯下去了！肩膀也下去了！
                // 我们把刚才存下来的坐标，赋予给已经弯曲的 spine3。
                // 枪的锚点就会像绑在肩膀上一样，完美无瑕地跟着身体起伏！
                ikHandRoot.position = spine3.position + spine3.rotation * ikRootLocalPos;
                ikHandRoot.rotation = spine3.rotation * ikRootLocalRot;
            }

            // ==========================================================
            // 🌟 4. 最后：摄像机吸附到已经弯曲好的头部（Eye_Socket）
            // ==========================================================
            if (cameraContainer != null && headBone != null)
            {
                cameraContainer.position = headBone.position;
            }
        }

        private void HandleLook(FrameInput input)
        {
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

            if (_cc.isGrounded)
            {
                if (input.Move == Vector2.zero)
                {
                    targetSpeed = 0f;
                    _airMomentumDirection = Vector3.zero;
                }

                _currentSpeed = Mathf.SmoothDamp(_currentSpeed, targetSpeed, ref _speedVelocity, speedSmoothTime);
                Vector3 direction = (transform.right * input.Move.x + transform.forward * input.Move.y).normalized;
                _airMomentumDirection = direction;
                animator.SetFloat(SPEED, _currentSpeed);

                return direction * _currentSpeed;
            }
            else
            {
                targetSpeed *= airSpeedMultiplier;

                if (input.Move == Vector2.zero)
                {
                    targetSpeed = 0.0f;
                }
                else
                {
                    _airMomentumDirection = (transform.right * input.Move.x + transform.forward * input.Move.y).normalized;
                }

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
