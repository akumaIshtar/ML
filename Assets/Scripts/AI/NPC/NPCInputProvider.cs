using System;
using Core;
using UnityEngine;
using AI.BehaviorTree;
using UnityEngine.AI;

namespace AI.NPC
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class NPCInputProvider : BehaviorTreeRunner, IInputProvider
    {
        [Header("NPC Settings")]
        public float fov = 120f;
        public float viewDistance = 30f;
        public float attackRange = 15f;
        public float turnSpeed = 5f;
        public LayerMask hitMask = ~0;
        public WaypointPath patrolPath;

        [HideInInspector] public NavMeshAgent agent;

        private FrameInput _currentInput;

        private Health _health;
        private CharacterController _cc;

        protected override void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.updatePosition = false;
            agent.updateRotation = false;

            _cc = GetComponent<CharacterController>();
            _health = GetComponent<Health>();
            if (_health != null)
            {
                _health.onDeath.AddListener(HandleDeath);
            }

            base.Start(); // Starts BehaviorTreeRunner
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.onDeath.RemoveListener(HandleDeath);
            }
        }

        protected override void Update()
        {
            if (agent != null)
            {
                // Force NavMeshAgent to stick with the actual physics CharacterController position
                agent.nextPosition = transform.position;
            }

            base.Update();
        }

        public void ClearInput() { _currentInput = default(FrameInput); }
        public void SetInputMove(Vector2 move) { _currentInput.Move = move; }
        public void SetInputLook(Vector2 look) { _currentInput.Look = look; }
        public void SetInputFire(bool fire) { _currentInput.Fire = fire; }
        public void SetInputCrouch(bool crouch) { _currentInput.Crouch = crouch; }

        public void SetInputReload(bool reload) { _currentInput.Reload = reload;}

        public FrameInput GetInput() { return _currentInput; }

        public Vector2 WorldDirectionToInputMove(Vector3 worldDir)
        {
            worldDir.y = 0;
            if (worldDir.sqrMagnitude < 0.01f) return Vector2.zero;
            Vector3 localDir = transform.InverseTransformDirection(worldDir);
            return new Vector2(localDir.x, localDir.z).normalized;
        }

        public void SmoothLookAt(Vector3 targetPosition)
        {
            Vector3 dirToTarget = (targetPosition - transform.position).normalized;
            dirToTarget.y = 0;

            if (dirToTarget.sqrMagnitude > 0.01f)
            {
                float angleDiff = Vector3.SignedAngle(transform.forward, dirToTarget, Vector3.up);
                float maxStep = turnSpeed * 100f * Time.deltaTime;
                float step = Mathf.Clamp(angleDiff, -maxStep, maxStep);
                _currentInput.Look = new Vector2(step, 0);
            }
            else
            {
                _currentInput.Look = Vector2.zero;
            }
        }

        private void HandleDeath()
        {
            // 1. 关闭自身组件（停止 Update，也就停止了 BehaviorTreeRunner 运行）
            this.enabled = false;

            // 2. 强行清空最后一帧的输入，立刻松开开火键和方向键
            ClearInput();

            // 3. 关闭寻路，防止死后模型滑行或阻挡其他 NPC
            if (agent != null)
            {
                agent.isStopped = true;
                agent.enabled = false;
            }

            // 4. (可选) 关闭角色控制器，让尸体不再具有物理体积
            if (_cc != null)
            {
                _cc.enabled = false;
            }

            Debug.Log($"[NPC] {gameObject.name} 行为树及底层输入已终止。");
        }
        // ==========================================
        // ML-Agents 训练专用的复活重置接口
        // ==========================================
        public void Respawn()
        {
            // 当 ML-Agents 回合重启时，把脑子和物理组件重新打开
            this.enabled = true;
            if (agent != null) agent.enabled = true;
            if (_cc != null) _cc.enabled = true;
            ClearInput();
        }
    }
}
