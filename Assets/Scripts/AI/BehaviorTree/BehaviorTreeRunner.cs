using UnityEngine;
using System.Collections.Generic;

namespace AI.BehaviorTree
{
    public class BehaviorTreeRunner : MonoBehaviour
    {
        public BehaviorTreeAsset treeAsset;
        
        // Runtime copied instance so each NPC has its own state
        private BehaviorTreeAsset _runtimeTree;
        
        // Blackboard data
        private Dictionary<string, object> _blackboard = new Dictionary<string, object>();

        protected virtual void Start()
        {
            if (treeAsset != null)
            {
                _runtimeTree = treeAsset.Clone();
            }
        }

        protected virtual void Update()
        {
            if (_runtimeTree != null)
            {
                _runtimeTree.UpdateTree(this); // Pass self as context
            }
        }

        // Blackboard Accessors
        public void SetData(string key, object value)
        {
            _blackboard[key] = value;
        }

        public object GetData(string key)
        {
            if (_blackboard.TryGetValue(key, out object value))
            {
                return value;
            }
            return null;
        }

        public bool ClearData(string key)
        {
            return _blackboard.Remove(key);
        }

        // Context Provider for AI Nodes to get access to the NPC
        // If your runner sits on the NPC, nodes can fetch the NPC components using `runner.GetComponent<NPCInputProvider>()`
    }
}
