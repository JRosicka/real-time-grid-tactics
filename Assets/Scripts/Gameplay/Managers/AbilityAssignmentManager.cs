using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Managers {
    /// <summary>
    /// Handles performing entity-specific ability operations like performing, queueing, and expiring abilities for a
    /// <see cref="GridEntity"/>
    /// </summary>
    public class AbilityAssignmentManager {
        private ICommandManager CommandManager => GameManager.Instance.CommandManager;

        #region Availability checks
        
        public bool CanEntityUseAbility(GridEntity entity, IAbilityData data, bool ignoreBlockingTimers) {
            // Is this entity set up to use this ability?
            if (!entity.Abilities.Contains(data)) {
                return false;
            }

            // Do we own the requirements for this ability?
            List<PurchasableData> ownedPurchasables = GameManager.Instance.GetPlayerForTeam(entity.Team)
                .OwnedPurchasablesController.OwnedPurchasables;
            if (data.Requirements.Any(r => !ownedPurchasables.Contains(r))) {
                return false;
            }
            
            // Are there any active timers blocking this ability?
            if (!ignoreBlockingTimers && entity.ActiveTimers.Any(t => t.ChannelBlockers.Contains(data.Channel) && !t.Expired)) {
                return false;
            }

            return true;
        }

        public bool IsAbilityChannelOnCooldownForEntity(GridEntity entity, AbilityChannel channel, out AbilityCooldownTimer timer) {
            timer = entity.ActiveTimers.FirstOrDefault(t => t.Ability.AbilityData.Channel == channel);
            return timer != null;
        }
        
        #endregion
        #region Perform/Queue

        public bool PerformAbility(GridEntity entity, IAbilityData abilityData, IAbilityParameters parameters, bool fromInput,
                                    bool queueIfNotLegal, bool clearQueueFirst = true) {
            if (entity.BuildQueue != null && abilityData.TryingToPerformCancelsBuilds) {
                entity.BuildQueue.CancelAllBuilds();
            }
            
            if (!abilityData.AbilityLegal(parameters, entity)) {
                if (queueIfNotLegal) {
                    // We specified to perform the ability now, but we can't legally do that. So queue it if we can. 
                    
                    // Don't try to queue it if we can't actually pay for it
                    if (abilityData.PayCostUpFront && !abilityData.CanPayCost(parameters, entity)) {
                        entity.AbilityFailed(abilityData);
                        return false;
                    }
                    QueueAbility(entity, abilityData, parameters, true, clearQueueFirst, false, fromInput);
                    if (abilityData is not AttackAbilityData && entity.LastAttackedEntityValue is not null) {
                        // Clear the targeted entity since we are telling this entity to do something else
                        entity.LastAttackedEntity.UpdateValue(new NetworkableGridEntityValue(null));
                    }
                    return true;
                }

                entity.AbilityFailed(abilityData);
                return false;
            }
            
            if (abilityData is not AttackAbilityData && entity.LastAttackedEntityValue is not null) {
                // Clear the targeted entity since we are telling this entity to do something else
                entity.LastAttackedEntity.UpdateValue(new NetworkableGridEntityValue(null));
            }
            
            IAbility abilityInstance = abilityData.CreateAbility(parameters, entity);
            abilityInstance.WaitUntilLegal = queueIfNotLegal;
            CommandManager.PerformAbility(abilityInstance, clearQueueFirst, true, fromInput);
            return true;
        }
        
        public void QueueAbility(GridEntity entity, IAbilityData abilityData, IAbilityParameters parameters, 
                                bool waitUntilLegal, bool clearQueueFirst, bool insertAtFront, bool fromInput) {
            IAbility abilityInstance = abilityData.CreateAbility(parameters, entity);
            abilityInstance.WaitUntilLegal = waitUntilLegal;
            CommandManager.QueueAbility(abilityInstance, clearQueueFirst, insertAtFront, fromInput);
        }

        public void PerformOnStartAbilitiesForEntity(GridEntity entity) {
            foreach (IAbilityData abilityData in entity.Abilities.Where(a => a.PerformOnStart)) {
                PerformAbility(entity, abilityData, new NullAbilityParameters(), false, abilityData.RepeatForeverAfterStartEvenWhenFailed);
            }
        }
        
        #endregion
        #region Expire abilities
        
        /// <summary>
        /// Server method. Clear the ability timer locally if the ability is active, otherwise remove it from the
        /// queue if queued.
        /// </summary>
        public void ExpireAbility(GridEntity entity, IAbility ability, bool canceled) {
            ability.Cancel();
            if (ExpireTimerForAbility(entity, ability, canceled)) return;
            
            // The ability is queued, so remove it from the queue
            IAbility localAbility = entity.QueuedAbilities.FirstOrDefault(a => a.UID == ability.UID);
            if (localAbility != null) {
                CommandManager.RemoveAbilityFromQueue(entity, localAbility);
            }
        }
        
        /// <summary>
        /// Client method. Returns true if the timer was active and successfully expired, otherwise false.
        /// </summary>
        public bool ExpireTimerForAbility(GridEntity entity, IAbility ability, bool canceled) {
            // Find the timer with the indicated ability. The timers themselves are not synchronized, but
            // since their abilities are we can use those. 
            AbilityCooldownTimer cooldownTimer = entity.ActiveTimers.FirstOrDefault(t => t.Ability.UID == ability.UID);
            if (cooldownTimer == null) return false;
            
            // The ability is indeed active, so cancel it
            cooldownTimer.Expire();
            entity.ActiveTimers.Remove(cooldownTimer);
            entity.TriggerAbilityCooldownExpired(ability, cooldownTimer, canceled);

            return true;
        }

        #endregion
        
        /// <summary>
        /// Add time to the movement cooldown timer due to another ability being performed.
        /// If there is an active cooldown timer, then this amount is added to that timer.
        /// Otherwise, a new cooldown timer is added with this amount.
        /// </summary>
        public void AddMovementTime(GridEntity entity, float timeToAdd) {
            MoveAbilityData moveAbilityData = entity.GetAbilityData<MoveAbilityData>();
            if (moveAbilityData == null) return;
            Vector2Int? location = entity.Location;
            if (location == null) return;
            
            List<AbilityCooldownTimer> activeTimersCopy = new List<AbilityCooldownTimer>(entity.ActiveTimers);
            AbilityCooldownTimer movementTimer = activeTimersCopy.FirstOrDefault(t => t.Ability is MoveAbility);
            if (movementTimer != null) {
                if (movementTimer.Expired) {
                    Debug.LogWarning("Tried to add movement cooldown timer time from another ability, but that " +
                                     "timer is expired. Adding new movement cooldown timer instead. This might not behave correctly.");
                } else {
                    // Add this time to the current movement cooldown timer
                    entity.AddTimeToAbilityTimer(movementTimer.Ability, timeToAdd);
                    return;
                }
            }
            
            // Add a new movement cooldown timer
            entity.CreateAbilityTimer(new MoveAbility(moveAbilityData, new MoveAbilityParameters {
                Destination = location.Value,
                NextMoveCell = location.Value,
                SelectorTeam = entity.Team,
                BlockedByOccupation = false
            }, entity), timeToAdd);
        }
    }
}