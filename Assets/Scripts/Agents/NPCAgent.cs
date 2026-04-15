using AI.NPC;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;
using Core;

namespace Agents
{
    public class NPCAgent : Agent
    {
        private Health _health;
        private NPCInputProvider _inputProvider; // 如果训练时由Agent接管输入

        public override void Initialize()
        {
            _health = GetComponent<Health>();
            _inputProvider = GetComponent<NPCInputProvider>();

            // 完美利用你写好的 UnityEvent 订阅伤害和死亡事件
            _health.onDamaged.AddListener(OnTookDamage);
            _health.onDeath.AddListener(OnDied);
        }

        // 每次开始新回合（包括出生和死亡后复活）时调用
        public override void OnEpisodeBegin()
        {
            // 1. 重置生命值（调用你 Health.cs 里的方法）
            _health.ResetHealth();

            // 2. 清理输入和运动状态
            if (_inputProvider != null) _inputProvider.ClearInput();

            // 3. 随机重置位置（防止过拟合在同一个出生点）
            transform.localPosition = new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f));
        }

        private void OnTookDamage(float amount)
        {
            // 挨打时给予小幅度惩罚，促使其学会躲避
            AddReward(-0.1f);
        }

        private void OnDied()
        {
            // 死亡时给予决定性惩罚，并结束回合
            SetReward(-1.0f);
            EndEpisode();
        }
    }
}
