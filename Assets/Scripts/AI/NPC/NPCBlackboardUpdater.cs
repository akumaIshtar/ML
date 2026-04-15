
using UnityEngine;
using AI.BehaviorTree;
using KINEMATION.FPSAnimationPack.Scripts.Player; // 引入武器命名空间

namespace AI.NPC
{
    /// <summary>
    /// AI感知层：负责读取底层组件的状态，并统一写入行为树黑板
    /// </summary>
    [RequireComponent(typeof(BehaviorTreeRunner))]
    public class NPCBlackboardUpdater : MonoBehaviour
    {
        private BehaviorTreeRunner _runner;

        [Tooltip("获取NPC当前持有的武器管理器 (例如挂载了FPSPlayer的组件)")]
        public FPSPlayer weaponManager;

        private void Awake()
        {
            _runner = GetComponent<BehaviorTreeRunner>();
        }

        private void Update()
        {
            UpdateWeaponState();
            // 未来这里还可以添加 UpdateHealthState(), UpdateCoverState() 等
        }

        private void UpdateWeaponState()
        {
            if (weaponManager == null) return;

            var activeWeapon = weaponManager.GetActiveWeapon();
            if (activeWeapon != null)
            {
                // 实时查询子弹数量
                bool needsReload = activeWeapon.GetActiveAmmo() == 0;

                // 写入当前 NPC 专属的黑板
                _runner.SetData("NeedsReload", needsReload);
            }
        }
    }
}
