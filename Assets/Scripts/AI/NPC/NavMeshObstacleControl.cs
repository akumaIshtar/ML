using UnityEngine;
using UnityEngine.AI;
using Core; // 引入 Health 所在的命名空间

namespace AI.NPC
{
    /// <summary>
    /// 导航障碍物控制类：适用于门、路障、可破坏薄墙等。
    /// 利用 NavMesh Obstacle 的 Carving（雕刻）特性实现低功耗、瞬时导航更新。
    /// </summary>
    [RequireComponent(typeof(NavMeshObstacle))]
    public class NavMeshObstacleControl : MonoBehaviour
    {
        private NavMeshObstacle _obstacle;
        private Health _health;

        private void Awake()
        {
            _obstacle = GetComponent<NavMeshObstacle>();
            _health = GetComponent<Health>();

            // 核心配置：开启雕刻模式
            // 这在运行时能确保障碍物移除后网格瞬间“愈合”
            _obstacle.carving = true;
            _obstacle.carveOnlyStationary = true; 
        }

        private void OnEnable()
        {
            // 自动挂钩 Health 组件的死亡事件 (如果存在)
            if (_health != null)
            {
                _health.onDeath.AddListener(OnDestroyedOrOpened);
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.onDeath.RemoveListener(OnDestroyedOrOpened);
            }
        }

        /// <summary>
        /// 当物体被摧毁、门被打开或机关触发时调用。
        /// 禁用 Obstacle 即告知寻路系统此区域已可通行。
        /// </summary>
        public void OnDestroyedOrOpened()
        {
            // 关键：禁用 Obstacle 会立即消除它在 NavMesh 上雕刻出的“洞”
            _obstacle.enabled = false;
            
            // 为了性能，通常你可以选择隐藏或者销毁物体
            // gameObject.SetActive(false); 
            
            Debug.Log($"[NavMeshObstacleControl] 障碍物 '{gameObject.name}' 已移除，导航网格已更新。");
        }

        /// <summary>
        /// 恢复障碍物（例如门重新关闭）
        /// </summary>
        public void RestoreObstacle()
        {
            _obstacle.enabled = true;
        }
    }
}
