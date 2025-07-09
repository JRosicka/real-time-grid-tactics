using System.Collections.Generic;
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
        private class Attack {
            public GridEntity Attacker;
            public GridEntity Target;
            public int BonusDamage;
        }
        private readonly List<Attack> _queuedAttacks = new();
        
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

        /// <summary>
        /// Deal damage from an attack. The damage does not actually get dealt right away - it gets recorded and applied
        /// during the next round of ability execution.
        /// </summary>
        public void DealDamage(GridEntity attacker, GridEntity target, int bonusDamage) {
            _queuedAttacks.Add(new Attack {
                Attacker = attacker,
                Target = target,
                BonusDamage = bonusDamage
            });
        }

        public void ExecuteDamageApplication() {
            foreach (Attack attack in _queuedAttacks) {
                DoDealDamage(attack.Attacker, attack.Target, attack.BonusDamage);
            }
            _queuedAttacks.Clear();
        }

        private static void DoDealDamage(GridEntity attacker, GridEntity target, int bonusDamage) {
            attacker.LastAttackedEntity.UpdateValue(new NetworkableGridEntityValue(target));
            if (target.Location == null) {
                Debug.LogWarning("Entity received attack but it is not registered or unregistered");
                return;
            }
            
            bool killed = target.HPHandler.ReceiveAttackFromEntity(attacker, bonusDamage);
            target.TryRespondToAttack(attacker);

            // TODO For splash damage, this should gather a total amount of kills in the given instant in order to account for multiple kills at once (splash damage)
            if (killed) {
                attacker.IncrementKillCount();
            }
        }
    }
}