using AI.BehaviorTree;
using UnityEngine;

namespace AI.NPC
{
    public class TaskPatrol : ActionNode
    {
        public float waitTime = 2.0f;
        private float _waitTimer = 0f;
        private bool _isWaiting = false;
        private int _currentWaypointIndex = 0;
        private Transform[] _waypoints;

        public override NodeState Evaluate(BehaviorTreeRunner runner)
        {
            NPCInputProvider npc = runner.GetComponent<NPCInputProvider>();

            if (npc.patrolPath == null || npc.patrolPath.points == null || npc.patrolPath.points.Length == 0)
            {
                npc.ClearInput();
                state = NodeState.SUCCESS;
                return state;
            }

            _waypoints = npc.patrolPath.points;
            Transform wp = _waypoints[_currentWaypointIndex];
            
            if (wp == null) // In case object is destroyed
            {
                _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Length;
                state = NodeState.RUNNING;
                return state;
            }

            if (_isWaiting)
            {
                npc.ClearInput();
                _waitTimer -= Time.deltaTime;
                if (_waitTimer <= 0)
                {
                    _isWaiting = false;
                    _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Length;
                    wp = _waypoints[_currentWaypointIndex];
                    npc.agent.SetDestination(wp.position);
                }
                
                state = NodeState.RUNNING;
                return state;
            }

            if (Vector3.Distance(npc.agent.destination, wp.position) > 0.1f)
            {
                npc.agent.SetDestination(wp.position);
            }

            float dist = Vector3.Distance(npc.transform.position, wp.position);
            if (dist < 1.5f) // Reached waypoint
            {
                _isWaiting = true;
                _waitTimer = waitTime;
                npc.ClearInput();
                state = NodeState.RUNNING;
                return state;
            }

            Vector3 desiredVelocity = npc.agent.desiredVelocity;
            Vector2 mapMove = npc.WorldDirectionToInputMove(desiredVelocity);

            npc.ClearInput();
            npc.SetInputMove(mapMove);
            
            if (desiredVelocity.sqrMagnitude > 0.1f)
            {
                npc.SmoothLookAt(npc.transform.position + desiredVelocity);
            }

            state = NodeState.RUNNING;
            return state;
        }
    }
}
