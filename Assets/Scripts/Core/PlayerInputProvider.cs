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
        private FrameInput _currentInput;

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

        private void Update()
        {
            // 每帧统一收集一次输入，缓存起来
            var i = new FrameInput();
            i.Move = _inputActions.GamePlay.Move.ReadValue<Vector2>();
            i.Look = _inputActions.GamePlay.Look.ReadValue<Vector2>() * lookSensitivity;

            // 注意：如果在 FixedUpdate 中使用 Agent 训练，WasPressedThisFrame 可能会有同步问题，
            // 但 PlayerInput 本身是给人玩的，Agent 会提供另一个 AgentInputProvider
            i.Jump = _inputActions.GamePlay.Jump.WasPressedThisFrame();
            i.Crouch = _inputActions.GamePlay.Crouch.IsPressed();
            i.Sprint = _inputActions.GamePlay.Sprint.IsPressed();
            i.Fire = _inputActions.GamePlay.Fire.IsPressed();
            i.Reload = _inputActions.GamePlay.Reload.WasPressedThisFrame();
            i.ThrowGrenade = _inputActions.GamePlay.ThrowGrenade.WasPressedThisFrame();
            i.Aim = _inputActions.GamePlay.Aim.IsPressed();
            i.ChangeWeaponScroll = _inputActions.GamePlay.ChangeWeapon.ReadValue<float>();
            i.ChangeFireMode = _inputActions.GamePlay.ChangeFireMode.WasPressedThisFrame();

            _currentInput = i;
        }

        public FrameInput GetInput()
        {
            return _currentInput;
        }
        private void OnApplicationFocus(bool hasFocus)
        {
            Cursor.lockState = hasFocus ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !hasFocus;
        }
    }
}
