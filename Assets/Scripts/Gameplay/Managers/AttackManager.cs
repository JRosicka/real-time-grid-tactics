using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Managers {
    /// <summary>
    /// Handles executing attacks on entities. Should only be called on the server.
    ///
    /// Exists so we don't need to create and perform a new attack ability whenever we want to attack an entity. 
    /// </summary>
    public class AttackManager {
        /// <summary>
        /// Called on server to actually do an attack between two entities. All attacks should be handled through here. 
        /// </summary>
        /// <returns>True if an attack was successfully performed, otherwise false</returns>
        public bool PerformAttack(GridEntity attacker, GridEntity target, int bonusDamage, bool updateTargetLocation) {
            if (target == null || target.DeadOrDying) {
                Debug.LogWarning("Target object is dead");
                return false;
            }

            if (target.Location == null) {
                Debug.LogWarning("Target location is null");
                return false;
            }
            
            Vector2Int targetLocation = target.Location.Value;
            
            AttackAbilityData attackAbilityData = attacker.GetAbilityData<AttackAbilityData>();
            if (attackAbilityData == null) {
                Debug.LogWarning("Tried to perform an attack with an entity that does not have an attack ability");
                return false;
            }

            IAttackLogic attackLogic = AttackAbilityLogicFactory.CreateAttackLogic(attackAbilityData.AttackType);
            attackLogic.DoAttack(attacker, target, bonusDamage);
            
            if (updateTargetLocation) {
                attacker.SetTargetLocation(targetLocation, target, true);
            }

            return true;
        }
    }
}