using UnityEngine;

namespace Core
{
    public struct FrameInput
    {
        public Vector2 Move;      // X: 左右, Y: 前后
        public Vector2 Look;      // X: 鼠标水平, Y: 鼠标垂直
        public bool Jump;         // 跳跃
        public bool Crouch;       // 蹲下
        public bool Sprint;       // 冲刺
        public bool Fire;         // 开火 (预留给下一阶段)
    }

    public interface IInputProvider
    {
        FrameInput GetInput();
    }
}
