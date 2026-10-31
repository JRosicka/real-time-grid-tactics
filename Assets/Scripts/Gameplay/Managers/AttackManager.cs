using System.Collections.Generic;
using System.Linq;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Gameplay.Entities.Upgrades;
using Gameplay.Grid;
using Gameplay.UI;
using JetBrains.Annotations;
using Sirenix.Utilities;
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
            if (!target.IsActive()) {
                Debug.LogWarning("Target entity is inactive");
                return false;
            }
            
            Vector2Int targetLocation = target.Location!.Value;
            
            IAttackAbilityData attackAbilityData = attacker.GetAbilityData<IAttackAbilityData>();
            if (attackAbilityData == null) {
                Debug.LogWarning("Tried to perform an attack with an entity that does not have an attack ability");
                return false;
            }

            IAttackLogic attackLogic = AttackAbilityLogicFactory.CreateAttackLogic(attackAbilityData.AttackType);
            attackLogic.DoAttack(attacker, target, bonusDamage);
            
            if (updateTargetLocation) {
                attacker.SetTargetLocation(targetLocation, target, PathVisualizer.PathType.TargetAttack);
            }

            return true;
        }

        public bool PerformAttack(GridEntity attacker, HashSet<Vector2Int> targetLocations, int bonusDamage, bool updateTargetLocation) {
            IAttackAbilityData attackAbilityData = attacker.GetAbilityData<IAttackAbilityData>();
            if (attackAbilityData == null) {
                Debug.LogWarning("Tried to perform an attack with an entity that does not have an attack ability");
                return false;
            }

            IAttackLogic attackLogic = AttackAbilityLogicFactory.CreateAttackLogic(attackAbilityData.AttackType);
            attackLogic.DoAttack(attacker, targetLocations, bonusDamage);

            if (updateTargetLocation) {
                // Pick one of the locations closest to the attacker
                int minDistance = targetLocations.Min(l => CellDistanceLogic.DistanceBetweenCells(attacker.Location!.Value, l));
                List<Vector2Int> eligibleTargetLocations = targetLocations.Where(l => CellDistanceLogic.DistanceBetweenCells(attacker.Location!.Value, l) == minDistance).ToList();
                Vector2Int targetLocation = eligibleTargetLocations[GameManager.Instance.SeedManager.GetRNG(attacker.UID).Next(0, eligibleTargetLocations.Count)];
                
                attacker.SetTargetLocation(targetLocation, null, PathVisualizer.PathType.TargetAttack);
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

        private static int BonusDamageFromUpgrades(GridEntity attacker) {
            int bonusDamage = 0;
            foreach (IUpgrade upgrade in GameManager.Instance.GetPlayerForTeam(attacker).OwnedPurchasablesController.Upgrades.GetOwnedUpgrades()) {
                bonusDamage += upgrade.GetAttackBonus(attacker);
            }
            return bonusDamage;
        }

        private static void DoDealDamage([NotNull] GridEntity attacker, GridEntity target, int bonusDamage) {
            if (target.Location == null) {
                Debug.LogWarning("Entity received attack but it is not registered or unregistered");
                return;
            }
            
            // Apply any bonus damage from upgrades
            bonusDamage += BonusDamageFromUpgrades(attacker);
            bool killed = target.HPHandler.ReceiveAttackFromEntity(attacker, bonusDamage);
            target.TryRespondToAttack(attacker);

            if (killed) {
                attacker.LastAttackedEntity.UpdateValue(new NetworkableGridEntityValue(null));
                attacker.IncrementKillCount();
            } else {
                attacker.LastAttackedEntity.UpdateValue(new NetworkableGridEntityValue(target));
            }
        }
    }
}