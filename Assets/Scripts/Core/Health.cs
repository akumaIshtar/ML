using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Core
{
    public class Health : MonoBehaviour
    {
        [Header("Settings")]
        public float maxHealth = 100f;
        public float currentHealth;
        public bool isDead => currentHealth <= 0;

        [Header("Events")]
        public UnityEvent<float> onDamaged;
        public UnityEvent onDeath;

        private void Start()
        {
            ResetHealth();
        }

        public void TakeDamage(float amount)
        {
            if (isDead) return;
            currentHealth -= amount;
            onDamaged?.Invoke(amount);
            if (currentHealth <= 0) Die();
        }

        public void ResetHealth()
        {
            currentHealth = maxHealth;
        }

        private void Die()
        {
            onDeath?.Invoke();
            // 训练时这里会通知 ML-Agents 的 Agent.EndEpisode()
        }
    }

}
