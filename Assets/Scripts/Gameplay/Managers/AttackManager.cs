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


        public void PerformAttack(GridEntity attacker, GridEntity target, int bonusDamage, bool updateTargetLocation) {
            if (target == null || target.DeadOrDying) {
                Debug.LogWarning("Target object is dead");
                return;
            }

            if (target.Location == null) {
                Debug.LogWarning("Target location is null");
                return;
            }
            
            Vector2Int targetLocation = target.Location.Value;
            
            AttackAbilityData attackAbilityData = attacker.GetAbilityData<AttackAbilityData>();
            if (attackAbilityData == null) {
                Debug.LogWarning("Tried to perform an attack with an entity that does not have an attack ability");
                return;
            }

            IAttackLogic attackLogic = AttackAbilityLogicFactory.CreateAttackLogic(attackAbilityData.AttackType);
            attackLogic.DoAttack(attacker, target, bonusDamage);
            
            if (updateTargetLocation) {
                attacker.SetTargetLocation(targetLocation, target, true);
            }
        }
    }
}