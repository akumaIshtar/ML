using AI.BehaviorTree;
using UnityEngine;

namespace AI.NPC
{
    public class CheckTargetTimeout : ActionNode
    {
        public float memoryDuration = 30f;

        public override NodeState Evaluate(BehaviorTreeRunner runner)
        {
            object lastSeenObj = runner.GetData("LastSeenTime");
            if (lastSeenObj == null)
            {
                state = NodeState.FAILURE;
                return state;
            }

            float lastSeenTime = (float)lastSeenObj;
            if (Time.time - lastSeenTime <= memoryDuration)
            {
                state = NodeState.SUCCESS;
                return state;
            }

            state = NodeState.FAILURE;
            return state;
        }
    }
}
