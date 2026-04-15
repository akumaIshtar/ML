using System;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEditor;

namespace AI.BehaviorTree.Editor
{
    public class NodeView : UnityEditor.Experimental.GraphView.Node
    {
        public Action<NodeView> OnNodeSelected;
        public AI.BehaviorTree.Node node;
        public Port inputPort;
        public Port outputPort;

        public NodeView(AI.BehaviorTree.Node node)
        {
            this.node = node;
            this.title = node.name.Replace("Node", "");
            this.viewDataKey = node.guid;

            style.left = node.position.x;
            style.top = node.position.y;

            CreateInputPorts();
            CreateOutputPorts();
        }

        private void CreateInputPorts()
        {
            if (node is ActionNode || node is CompositeNode || node is DecoratorNode)
            {
                inputPort = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
                inputPort.portName = "";
                inputContainer.Add(inputPort);
            }
        }

        private void CreateOutputPorts()
        {
            if (node is DecoratorNode)
            {
                outputPort = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Single, typeof(bool));
                outputPort.portName = "";
                outputContainer.Add(outputPort);
            }
            else if (node is CompositeNode)
            {
                outputPort = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Multi, typeof(bool));
                outputPort.portName = "";
                outputContainer.Add(outputPort);
            }
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            Undo.RecordObject(node, "Behavior Tree (Set Position)");
            node.position.x = newPos.xMin;
            node.position.y = newPos.yMin;
            EditorUtility.SetDirty(node);
        }

        public override void OnSelected()
        {
            base.OnSelected();
            OnNodeSelected?.Invoke(this);
        }

        public void UpdateState()
        {
            RemoveFromClassList("running");
            RemoveFromClassList("failure");
            RemoveFromClassList("success");

            if (Application.isPlaying)
            {
                switch (node.state)
                {
                    case NodeState.RUNNING:
                        if (node.state == NodeState.RUNNING)
                            style.backgroundColor = new StyleColor(new Color(1f, 0.6f, 0f));
                        break;
                    case NodeState.FAILURE:
                        style.backgroundColor = new StyleColor(new Color(0.8f, 0.2f, 0.2f));
                        break;
                    case NodeState.SUCCESS:
                        style.backgroundColor = new StyleColor(new Color(0.2f, 0.8f, 0.2f));
                        break;
                }
            }
        }
    }
}
