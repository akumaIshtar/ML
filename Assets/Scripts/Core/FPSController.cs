using UnityEngine;

namespace Core
{
    [RequireComponent(typeof(CharacterController))]
    public class FPSController : MonoBehaviour
    {
        [Header("References")]
        public Transform cameraContainer; // 摄像机父节点，用于控制垂直旋转
        private CharacterController _cc;
        private IInputProvider _inputProvider;

        [Header("Movement Settings")]
        public float walkSpeed = 5f;
        public float runSpeed = 9f;
        public float crouchSpeed = 2.5f;
        public float speedChangeRate = 10f; // 速度切换的平滑度

        [Header("Jump & Gravity")]
        public float jumpHeight = 1.2f;
        public float gravity = -15.0f;
        public float terminalVelocity = 53.0f; // 最大下落速度

        [Header("Crouch System")]
        public float crouchHeight = 1.0f;       // 蹲下时的碰撞盒高度
        public float standHeight = 2.0f;        // 站立时的碰撞盒高度
        public float crouchTransitionSpeed = 10f;
        public Vector3 crouchCenter = new Vector3(0, 0.5f, 0); // 蹲下时碰撞盒中心
        public Vector3 standCenter = new Vector3(0, 1f, 0);    // 站立时碰撞盒中心

        // --- 新增内容 ---
        [Header("Camera Height Settings")]
        public float cameraStandHeight = 1.6f;  // 站立时眼睛高度
        public float cameraCrouchHeight = 0.8f; // 蹲下时眼睛高度 (通常比碰撞盒略低或持平)
        // ----------------

        [Header("Look Settings")]
        public float lookClampTop = 85f;
        public float lookClampBottom = -85f;

        // Runtime State
        private float _cameraPitch = 0f; // 垂直角度记录
        private float _currentSpeed;
        private Vector3 _velocity; // 垂直方向速度(重力/跳跃)
        private float _targetHeight;
        private Vector3 _targetCenter;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            // 获取同一物体上的输入提供者（无论是Player还是AI）
            _inputProvider = GetComponent<IInputProvider>();

            _targetHeight = standHeight;
            _targetCenter = standCenter;
        }

        private void Update()
        {
            if (_inputProvider == null) return;

            // 1. 获取输入 (解耦核心)
            FrameInput input = _inputProvider.GetInput();

            // 2. 执行逻辑
            HandleLook(input);
            HandleMovement(input);
            HandleGravityAndJump(input);
            HandleCrouch(input);
        }

        private void HandleLook(FrameInput input)
        {
            // 水平旋转 (转身体)
            transform.Rotate(Vector3.up * input.Look.x);

            // 垂直旋转 (转摄像机容器)
            _cameraPitch -= input.Look.y;
            _cameraPitch = Mathf.Clamp(_cameraPitch, lookClampBottom, lookClampTop);
            cameraContainer.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
        }

        private void HandleMovement(FrameInput input)
        {
            // 确定目标速度
            float targetSpeed = walkSpeed;
            if (input.Sprint && !input.Crouch) targetSpeed = runSpeed;
            if (input.Crouch) targetSpeed = crouchSpeed;

            // 如果没有输入，速度归零
            if (input.Move == Vector2.zero) targetSpeed = 0.0f;

            // 平滑速度变化 (防止瞬移感)
            float currentHorizontalSpeed = new Vector3(_cc.velocity.x, 0.0f, _cc.velocity.z).magnitude;
            float speedOffset = 0.1f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _currentSpeed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * input.Move.magnitude, Time.deltaTime * speedChangeRate);
            }
            else
            {
                _currentSpeed = targetSpeed;
            }

            // 计算移动方向 (基于当前朝向)
            Vector3 direction = new Vector3(input.Move.x, 0.0f, input.Move.y).normalized;
            if (input.Move != Vector2.zero)
            {
                direction = transform.right * input.Move.x + transform.forward * input.Move.y;
            }

            // 应用移动
            _cc.Move(direction.normalized * (_currentSpeed * Time.deltaTime));
        }

        private void HandleGravityAndJump(FrameInput input)
        {
            // 地面检测
            if (_cc.isGrounded)
            {
                // 即使在地面，也给一个微小的向下的力，确保isGrounded判定稳定
                if (_velocity.y < 0.0f) _velocity.y = -2f;

                // 跳跃逻辑
                if (input.Jump)
                {
                    // 公式: v = sqrt(h * -2 * g)
                    _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }

            // 应用重力
            if (_velocity.y > -terminalVelocity)
            {
                _velocity.y += gravity * Time.deltaTime;
            }

            // 应用垂直移动
            _cc.Move(_velocity * Time.deltaTime);
        }

        private void HandleCrouch(FrameInput input)
        {
            // 1. 判定目标状态
            bool isTryingToCrouch = input.Crouch;

            // 站起时的头顶检测 (防止卡在障碍物里)
            // 如果想站起来，但头顶有东西，被迫保持蹲下
            if (!isTryingToCrouch && Physics.Raycast(cameraContainer.position, Vector3.up, 1.0f))
            {
                isTryingToCrouch = true;
            }

            // 2. 设定物理(碰撞盒)的目标值
            if (isTryingToCrouch)
            {
                _targetHeight = crouchHeight;
                _targetCenter = crouchCenter;
            }
            else
            {
                _targetHeight = standHeight;
                _targetCenter = standCenter;
            }

            // 3. 应用物理插值 (CharacterController)
            _cc.height = Mathf.Lerp(_cc.height, _targetHeight, Time.deltaTime * crouchTransitionSpeed);
            _cc.center = Vector3.Lerp(_cc.center, _targetCenter, Time.deltaTime * crouchTransitionSpeed);

            // 4. --- 新增核心逻辑：处理摄像机高度 ---
            // 根据当前是蹲还是站，决定摄像机的目标Y轴高度
            float targetCamHeight = isTryingToCrouch ? cameraCrouchHeight : cameraStandHeight;

            // 获取当前摄像机容器的本地坐标
            Vector3 currentCamPos = cameraContainer.localPosition;

            // 对 Y 轴进行独立的平滑插值
            float newCamHeight = Mathf.Lerp(currentCamPos.y, targetCamHeight, Time.deltaTime * crouchTransitionSpeed);

            // 应用新的高度
            cameraContainer.localPosition = new Vector3(currentCamPos.x, newCamHeight, currentCamPos.z);
        }
    }
}
