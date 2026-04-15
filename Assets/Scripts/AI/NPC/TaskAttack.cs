using AI.BehaviorTree;
using UnityEngine;

namespace AI.NPC
{
    public class TaskAttack : ActionNode
    {
        private float _lastPathUpdateTime;
        public override NodeState Evaluate(BehaviorTreeRunner runner)
        {
            NPCInputProvider npc = runner.GetComponent<NPCInputProvider>();
            Transform target = (Transform)runner.GetData("target");

            if (target == null)
            {
                state = NodeState.FAILURE;
                return state;
            }

            npc.ClearInput(); // MUST CLEAR INPUT BEFORE SETTING LOOK AND MOVE

            npc.SmoothLookAt(target.position);
            float distance = Vector3.Distance(npc.transform.position, target.position);

            Vector3 dirToTarget = (target.position - npc.transform.position).normalized;
            dirToTarget.y = 0;
            float angle = Vector3.Angle(npc.transform.forward, dirToTarget);

            if (distance <= npc.attackRange)
            {
                npc.SetInputMove(Vector2.zero);

                // 将 5f 放宽到 15f 或 20f
                if (angle < 15f)
                {
                    npc.SetInputFire(true);
                }
                else
                {
                    npc.SetInputFire(false);
                }
            }
            else
            {

            // 每 0.2 秒最多更新一次路径
            if (Time.time - _lastPathUpdateTime > 0.2f && Vector3.Distance(npc.agent.destination, target.position) > 1.5f)
            {
                npc.agent.SetDestination(target.position);
                _lastPathUpdateTime = Time.time;
            }
                Vector2 mapMove = npc.WorldDirectionToInputMove(npc.agent.desiredVelocity);
                npc.SetInputMove(mapMove);
                npc.SetInputFire(false);
            }

            state = NodeState.RUNNING;
            return state;
        }
    }
}
