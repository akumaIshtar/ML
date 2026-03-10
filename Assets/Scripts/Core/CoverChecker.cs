using UnityEngine;

namespace Core
{
    [RequireComponent(typeof(CharacterController))]
    public class CoverChecker : MonoBehaviour
    {
        public Transform playerTransform; // 玩家（敌人）的位置
        public LayerMask coverLayers;    // 掩体所在的物理层

        private CharacterController _controller;

        private void Awake()
        {
            // 自动获取角色控制器
            _controller = GetComponent<CharacterController>();
        }

        public bool IsFullyInCover()
        {
            if (playerTransform == null || _controller == null) return false;

            // 1. 动态获取角色的尺寸
            float height = _controller.height;
            float radius = _controller.radius;
            Vector3 center = transform.position + _controller.center;

            // 2. 根据包围盒动态计算 5 个关键采样点
            // 我们假设 transform.position 在角色脚底（Unity 常用做法）
            Vector3[] checkPoints = new Vector3[]
            {
                transform.position + Vector3.up * height,           // 头顶 (Top)
                center,                                             // 躯干中心 (Center)
                transform.position + Vector3.up * 0.1f,              // 脚部 (Bottom)
                center + transform.right * radius,                  // 身体右边缘 (Right)
                center - transform.right * radius                   // 身体左边缘 (Left)
            };

            // 3. 判定：只要有一个点被“看见”，就不算完全躲避
            foreach (Vector3 point in checkPoints)
            {
                if (!IsPointHidden(point))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsPointHidden(Vector3 targetPoint)
        {
            Vector3 direction = playerTransform.position - targetPoint;
            float distance = direction.magnitude;

            // 如果射线撞到了掩体，说明该点被遮挡
            return Physics.Raycast(targetPoint, direction, distance, coverLayers);
        }

        // 调试辅助线：在场景窗口中直接看到采样点，非常有助于调试 AI 行为
        private void OnDrawGizmosSelected()
        {
            if (_controller == null) _controller = GetComponent<CharacterController>();
            if (_controller == null) return;

            float height = _controller.height;
            float radius = _controller.radius;
            Vector3 center = transform.position + _controller.center;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * height, 0.05f); // 头
            Gizmos.DrawWireSphere(center, 0.05f);                                  // 躯干
            Gizmos.DrawWireSphere(center + transform.right * radius, 0.05f);        // 右
            Gizmos.DrawWireSphere(center - transform.right * radius, 0.05f);        // 左
        }
    }
}
