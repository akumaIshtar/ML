using System;
using UnityEngine;

namespace Core
{
    public class PlayerInputProvider : MonoBehaviour, IInputProvider
    {
        [Header("Setting")] public float lookSensitivity = 0.1f;
        // 新系统的鼠标Delta值通常是像素单位，可能很大，需要重新调整灵敏度
        // 建议从 0.1 开始调试
        private PlayerControls _inputActions;

        private void Awake()
        {
            _inputActions = new PlayerControls();
        }

        private void OnEnable()
        {
            _inputActions.GamePlay.Enable();
        }

        private void OnDisable()
        {
            _inputActions.GamePlay.Disable();
        }

        public FrameInput GetInput()
        {
            var i = new FrameInput();
            // 1. 读取移动 (Vector2)
            // 直接读取 Action 的值
            i.Move = _inputActions.GamePlay.Move.ReadValue<Vector2>();

            // 2. 读取视角 (Vector2)
            // 注意：新系统的鼠标Delta不受Time.deltaTime影响，通常是像素差
            // 也不受 Unity Editor 设置里的 "Input Manager" 灵敏度影响
            Vector2 rawLook = _inputActions.GamePlay.Look.ReadValue<Vector2>();
            i.Look = rawLook * lookSensitivity;

            // 3. 读取按键状态
            // IsPressed() 对应旧系统的 GetKey() (持续按住)
            // WasPressedThisFrame() 对应 GetKeyDown() (按下瞬间)

            // 跳跃：我们需要的是触发瞬间，还是持续状态？
            // 在 CharacterController 逻辑里，通常判断 "Jump" 信号即可。
            // 这里建议用 IsPressed 或者 WasPressedThisFrame 取决于你的 Controller 怎么写。
            // 之前的 Controller 是每帧检测，如果按住可能导致连跳。
            // 为了更精准的跳跃，这里推荐用 WasPressedThisFrame()，
            // *但是* 我们的 FrameInput 是个结构体，Controller在Update里跑。
            // 为了安全起见，先用 IsPressed()，并在 Controller 里加一个落地重置锁，或者就用 Trigger 方式。
            // 简单起见，这里先映射为 bool 值：
            // 必须每次重新按下空格才能跳跃
            i.Jump = _inputActions.GamePlay.Jump.WasPressedThisFrame();

            i.Crouch = _inputActions.GamePlay.Crouch.IsPressed();
            i.Sprint = _inputActions.GamePlay.Sprint.IsPressed();
            i.Fire = _inputActions.GamePlay.Fire.IsPressed();

            return i;
        }
        private void OnApplicationFocus(bool hasFocus)
        {
            Cursor.lockState = hasFocus ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !hasFocus;
        }
    }
}
