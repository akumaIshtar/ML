using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AI.BehaviorTree
{
    [CreateAssetMenu(menuName = "AI/BehaviorTreeAsset")]
    public class BehaviorTreeAsset : ScriptableObject
    {
        public Node rootNode;
        public NodeState treeState = NodeState.RUNNING;
        public List<Node> nodes = new List<Node>();

        public NodeState UpdateTree(BehaviorTreeRunner runner)
        {
            if (rootNode != null)
            {
                treeState = rootNode.Evaluate(runner);
            }
            return treeState;
        }

        public BehaviorTreeAsset Clone()
        {
            BehaviorTreeAsset tree = Instantiate(this);
            if (rootNode != null)
            {
                tree.rootNode = rootNode.Clone();
            }
            // Optional: re-populate tree.nodes list post-clone if needed for runtime debugging
            return tree;
        }

#if UNITY_EDITOR
        public Node CreateNode(System.Type type)
        {
            Node node = ScriptableObject.CreateInstance(type) as Node;
            node.name = type.Name;
            node.guid = GUID.Generate().ToString();
            
            Undo.RecordObject(this, "Behavior Tree (CreateNode)");
            nodes.Add(node);
            
            if(!Application.isPlaying) 
            {
                AssetDatabase.AddObjectToAsset(node, this);
            }
            
            Undo.RegisterCreatedObjectUndo(node, "Behavior Tree (CreateNode)");
            AssetDatabase.SaveAssets();
            return node;
        }

        public void DeleteNode(Node node)
        {
            Undo.RecordObject(this, "Behavior Tree (DeleteNode)");
            nodes.Remove(node);
            
            // Remove connections
            if (node is DecoratorNode decorator) decorator.child = null;
            if (node is CompositeNode composite) composite.children.Clear();
            
            // Also clean up references from other nodes to this node
            foreach(var n in nodes) {
                if (n is DecoratorNode d && d.child == node) d.child = null;
                if (n is CompositeNode c && c.children.Contains(node)) c.children.Remove(node);
            }

            Undo.DestroyObjectImmediate(node);
            AssetDatabase.SaveAssets();
        }

        public void AddChild(Node parent, Node child)
        {
            Undo.RecordObject(parent, "Behavior Tree (AddChild)");
            if (parent is DecoratorNode decorator)
            {
                decorator.child = child;
            }
            else if (parent is CompositeNode composite)
            {
                composite.children.Add(child);
            }
            EditorUtility.SetDirty(parent);
        }

        public void RemoveChild(Node parent, Node child)
        {
            Undo.RecordObject(parent, "Behavior Tree (RemoveChild)");
            if (parent is DecoratorNode decorator)
            {
                decorator.child = null;
            }
            else if (parent is CompositeNode composite)
            {
                composite.children.Remove(child);
            }
            EditorUtility.SetDirty(parent);
        }
#endif
    }
}
