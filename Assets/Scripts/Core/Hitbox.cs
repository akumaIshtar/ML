using UnityEngine;

namespace Core
{
    public enum BodyPart
    {
        Head,
        Chest,
        Abdomen,
        Limbs
    }

    /// <summary>
    /// Attach this component to individual body part colliders (e.g., Head, Chest, Arms, Legs).
    /// </summary>
    public class Hitbox : MonoBehaviour, IDamageable
    {
        [Tooltip("Reference to the main Health module of the character.")]
        public Health healthModule;

        [Tooltip("The body part this hitbox represents.")]
        public BodyPart bodyPart;

        /// <summary>
        /// Returns the damage multiplier for the specific body part.
        /// </summary>
        public float GetDamageMultiplier()
        {
            switch (bodyPart)
            {
                case BodyPart.Head: return 2.0f;     // 2x damage for headshots
                case BodyPart.Chest: return 1.2f;    // 1.2x damage for chest
                case BodyPart.Abdomen: return 1.0f;  // Normal damage for abdomen
                case BodyPart.Limbs: return 0.5f;    // Half damage for limbs
                default: return 1.0f;
            }
        }

        public void TakeDamage(float amount)
        {
            if (healthModule != null)
            {
                // Calculate the final damage including the multiplier
                float finalDamage = amount * GetDamageMultiplier();
                healthModule.TakeDamage(finalDamage);
            }
            else
            {
                Debug.LogWarning($"Hitbox on {gameObject.name} is missing a reference to the Health module!");
            }
        }
    }
}
