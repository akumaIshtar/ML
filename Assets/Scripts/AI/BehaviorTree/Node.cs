using UnityEngine;
using System.Collections.Generic;

namespace AI.BehaviorTree
{
    public enum NodeState
    {
        RUNNING,
        SUCCESS,
        FAILURE
    }

    public abstract class Node : ScriptableObject
    {
        [HideInInspector] public NodeState state = NodeState.RUNNING;
        [HideInInspector] public string guid;
        [HideInInspector] public Vector2 position; // Used by GraphView Editor
        [TextArea] public string description;

        // Clone method to instantiate independent runtime nodes
        public virtual Node Clone()
        {
            return Instantiate(this);
        }

        public abstract NodeState Evaluate(BehaviorTreeRunner runner);
    }
    
    public abstract class ActionNode : Node { }

    public abstract class DecoratorNode : Node
    {
        [HideInInspector] public Node child;

        public override Node Clone()
        {
            DecoratorNode node = Instantiate(this) as DecoratorNode;
            if (child != null) node.child = child.Clone();
            return node;
        }
    }

    public abstract class CompositeNode : Node
    {
        [HideInInspector] public List<Node> children = new List<Node>();

        public override Node Clone()
        {
            CompositeNode node = Instantiate(this) as CompositeNode;
            node.children = new List<Node>();
            foreach (var c in children)
            {
                if (c != null)
                    node.children.Add(c.Clone());
            }
            return node;
        }
    }
}
