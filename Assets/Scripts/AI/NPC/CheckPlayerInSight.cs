using AI.BehaviorTree;
using UnityEngine;

namespace AI.NPC
{
    public class CheckPlayerInSight : ActionNode
    {
        private Transform _playerTransform;

        private float _lastSearchTime = 0f;
        public override NodeState Evaluate(BehaviorTreeRunner runner)
        {
            NPCInputProvider npc = runner.GetComponent<NPCInputProvider>();

            if (_playerTransform == null)
            {
                if (Time.time - _lastSearchTime < 1.0f) {
                    state = NodeState.FAILURE; return state; // 每秒最多查找一次
                }
                _lastSearchTime = Time.time;
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    _playerTransform = playerObj.transform;
                }
                else
                {
                    state = NodeState.FAILURE;
                    return state;
                }
            }

            Vector3 npcHeadPos = npc.transform.position + Vector3.up * 1.6f;
            Vector3 targetPos = _playerTransform.position + Vector3.up * 1.5f;
            Vector3 directionToTarget = targetPos - npcHeadPos;
            float distance = directionToTarget.magnitude;

            if (distance > npc.viewDistance)
            {
                state = NodeState.FAILURE;
                return state;
            }

            float angle = Vector3.Angle(npc.transform.forward, directionToTarget);
            if (angle > npc.fov / 2f)
            {
                state = NodeState.FAILURE;
                return state;
            }

            if (Physics.Raycast(npcHeadPos, directionToTarget.normalized, out RaycastHit hit, npc.viewDistance, npc.hitMask))
            {
                if (hit.transform.CompareTag("Player") || hit.transform.IsChildOf(_playerTransform))
                {
                    runner.SetData("target", _playerTransform);
                    runner.SetData("LastSeenTime", Time.time); // 记录看到的时间戳
                    runner.SetData("LastSeenPosition", _playerTransform.position); // 记录玩家最后位置
                    state = NodeState.SUCCESS;
                    return state;
                }
            }

            state = NodeState.FAILURE;
            return state;
        }
    }
}
