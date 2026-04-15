namespace AI.BehaviorTree
{
    public class Sequence : CompositeNode
    {
        public override NodeState Evaluate(BehaviorTreeRunner runner)
        {
            bool anyChildIsRunning = false;

            foreach (Node node in children)
            {
                switch (node.Evaluate(runner))
                {
                    case NodeState.FAILURE:
                        state = NodeState.FAILURE;
                        return state;
                    case NodeState.SUCCESS:
                        continue;
                    case NodeState.RUNNING:
                        state = NodeState.RUNNING; // 正确逻辑：一旦有子节点在运行，当前序列必须立刻停止并上报运行状态
                        return state;
                    default:
                        state = NodeState.SUCCESS;
                        return state;
                }
            }

            state = anyChildIsRunning ? NodeState.RUNNING : NodeState.SUCCESS;
            return state;
        }
    }
}
