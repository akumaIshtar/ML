using AI.BehaviorTree;
using UnityEngine;

namespace AI.NPC
{
    public class TaskSearch : ActionNode
    {
        public override NodeState Evaluate(BehaviorTreeRunner runner)
        {
            NPCInputProvider npc = runner.GetComponent<NPCInputProvider>();

            // 停下脚步，停止开火
            npc.SetInputMove(Vector2.zero);
            npc.SetInputFire(false);

            // 获取玩家最后已知位置
            object targetPosObj = runner.GetData("LastSeenPosition");
            Vector3 baseLookDir = npc.transform.forward;

            if (targetPosObj != null)
            {
                Vector3 dir = ((Vector3)targetPosObj - npc.transform.position).normalized;
                dir.y = 0;
                if (dir.sqrMagnitude > 0) baseLookDir = dir;
            }

            // 利用正弦波让NPC在最后已知方向的 ±45度 范围内摇头搜索
            float searchAngle = Mathf.Sin(Time.time * 1.5f) * 45f;
            Vector3 lookDir = Quaternion.Euler(0, searchAngle, 0) * baseLookDir;

            npc.SmoothLookAt(npc.transform.position + lookDir);

            state = NodeState.RUNNING;
            return state;
        }
    }
}
