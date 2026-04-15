namespace AI.BehaviorTree
{
    public class Selector : CompositeNode
    {
        public override NodeState Evaluate(BehaviorTreeRunner runner)
        {
            foreach (Node node in children)
            {
                switch (node.Evaluate(runner))
                {
                    case NodeState.FAILURE:
                        continue;
                    case NodeState.SUCCESS:
                        state = NodeState.SUCCESS;
                        return state;
                    case NodeState.RUNNING:
                        state = NodeState.RUNNING;
                        return state;
                    default:
                        continue;
                }
            }

            state = NodeState.FAILURE;
            return state;
        }
    }
}
