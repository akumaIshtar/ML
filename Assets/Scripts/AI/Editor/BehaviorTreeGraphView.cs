using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System.Linq;
using System;

namespace AI.BehaviorTree.Editor
{
    public class BehaviorTreeGraphView : GraphView
    {
        public Action<NodeView> OnNodeSelected;
        private BehaviorTreeAsset _tree;

        public BehaviorTreeGraphView()
        {
            Insert(0, new GridBackground());

            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(endPort =>
                endPort.direction != startPort.direction &&
                endPort.node != startPort.node).ToList();
        }

        public void PopulateView(BehaviorTreeAsset tree)
        {
            _tree = tree;

            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(graphElements);
            graphViewChanged += OnGraphViewChanged;

            if (_tree.rootNode == null)
            {
                _tree.rootNode = _tree.CreateNode(typeof(AI.BehaviorTree.Selector)) as AI.BehaviorTree.Selector;
                EditorUtility.SetDirty(_tree);
                AssetDatabase.SaveAssets();
            }

            // Sync with AssetDatabase: recover missing references dynamically
            var path = AssetDatabase.GetAssetPath(_tree);
            if (!string.IsNullOrEmpty(path))
            {
                var objs = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var obj in objs)
                {
                    if (obj is AI.BehaviorTree.Node node && !_tree.nodes.Contains(node))
                    {
                        _tree.nodes.Add(node);
                    }
                }
            }

            // ==========================================
            // 【核心修复】：在生成视图前，清理掉列表中的空节点
            // 这能有效防止外部误删子资产或脚本丢失导致的奔溃
            // ==========================================
            _tree.nodes.RemoveAll(n => n == null);

            // Save recovered structure
            EditorUtility.SetDirty(_tree);

            // Create Node Views
            foreach (var n in _tree.nodes)
            {
                // 再次保险判断
                if (n != null)
                {
                    CreateNodeView(n);
                }
            }

            // Create Edges
            foreach (var n in _tree.nodes)
            {
                if (n == null) continue; // 安全校验

                var children = GetChildren(n);
                foreach (var child in children)
                {
                    if (child == null) continue; // 安全校验

                    NodeView parentView = GetNodeByGuid(n.guid) as NodeView;
                    NodeView childView = GetNodeByGuid(child.guid) as NodeView;

                    if (parentView != null && childView != null)
                    {
                        Edge edge = parentView.outputPort.ConnectTo(childView.inputPort);
                        AddElement(edge);
                    }
                }
            }
        }

        public void UpdateNodeStates()
        {
            nodes.ForEach(n => {
                NodeView view = n as NodeView;
                view.UpdateState();
            });
        }

        private List<AI.BehaviorTree.Node> GetChildren(AI.BehaviorTree.Node node)
        {
            if (node is DecoratorNode decorator && decorator.child != null)
                return new List<AI.BehaviorTree.Node> { decorator.child };
            if (node is CompositeNode composite)
                return composite.children;
            return new List<AI.BehaviorTree.Node>();
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (change.elementsToRemove != null)
            {
                foreach (var elem in change.elementsToRemove)
                {
                    if (elem is NodeView nodeView)
                    {
                        _tree.DeleteNode(nodeView.node);
                    }
                    if (elem is Edge edge)
                    {
                        NodeView parentView = edge.output.node as NodeView;
                        NodeView childView = edge.input.node as NodeView;
                        _tree.RemoveChild(parentView.node, childView.node);
                    }
                }
            }

            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate)
                {
                    NodeView parentView = edge.output.node as NodeView;
                    NodeView childView = edge.input.node as NodeView;
                    _tree.AddChild(parentView.node, childView.node);
                }
            }
            return change;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            var types = TypeCache.GetTypesDerivedFrom<AI.BehaviorTree.ActionNode>();

            evt.menu.AppendAction("Sequences/Sequence", (a) => CreateNode(typeof(AI.BehaviorTree.Sequence)));
            evt.menu.AppendAction("Sequences/Selector", (a) => CreateNode(typeof(AI.BehaviorTree.Selector)));

            foreach (var type in types)
            {
                evt.menu.AppendAction($"Actions/{type.Name}", (a) => CreateNode(type));
            }
        }

        private void CreateNode(System.Type type)
        {
            AI.BehaviorTree.Node node = _tree.CreateNode(type);
            CreateNodeView(node);
        }

        private void CreateNodeView(AI.BehaviorTree.Node node)
        {
            NodeView nodeView = new NodeView(node);
            nodeView.OnNodeSelected = OnNodeSelected;
            AddElement(nodeView);
        }
    }
}
