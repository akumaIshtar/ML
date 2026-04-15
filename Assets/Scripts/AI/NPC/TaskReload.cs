using AI.BehaviorTree;
using UnityEngine;

namespace AI.NPC
{
    public class TaskReload : ActionNode
    {
        private float _reloadStartTime;
        private bool _isReloading;
        
        public override NodeState Evaluate(BehaviorTreeRunner runner)
        {
            object needsReloadData = runner.GetData("NeedsReload");
            bool needsReload = (needsReloadData != null) && (bool)needsReloadData;
            
            // 如果不需要换弹了（底层武器的子弹恢复 > 0 会自动重置这个标记）
            if (!needsReload)
            {
                if (_isReloading)
                {
                    // 代表刚刚结束所有的换弹等待，子弹满了
                    _isReloading = false;
                    state = NodeState.SUCCESS;
                    return state;
                }
                state = NodeState.FAILURE;
                return state;
            }
            
            NPCInputProvider npc = runner.GetComponent<NPCInputProvider>();
            
            if (!_isReloading)
            {
                npc.ClearInput(); // 停止移动等操作
                npc.SetInputReload(true); // 按下换弹键
                npc.SetInputFire(false);  // 松开开火键
                
                _isReloading = true;
                _reloadStartTime = Time.time;
                
                state = NodeState.RUNNING;
                return state;
            }
            
            // 在换弹期间保持安全输入（不要重复按换弹键，绝不允许开火）
            npc.SetInputReload(false); 
            npc.SetInputFire(false);
            npc.SetInputMove(Vector2.zero);
            
            // 我们不再需要手动2.5秒倒计时了。
            // 因为现在底层的 HitscanShooter.Update() 会每帧获取真实子弹数。
            // 只要武器换弹动画没播完、子弹还没加上去，NeedsReload 就永远是 true！这个节点就会一直挂着拦截其它操作。

            state = NodeState.RUNNING;
            return state;
        }
    }
}
