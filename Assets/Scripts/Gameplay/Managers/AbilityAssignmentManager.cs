using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Mirror;
using UnityEngine;
using Util;

namespace Gameplay.Managers {
    /// <summary>
    /// Handles performing local entity-specific ability operations like performing, queueing, and expiring abilities for a
    /// <see cref="GridEntity"/>
    /// </summary>
    public class AbilityAssignmentManager {
        private ICommandManager CommandManager => GameManager.Instance.CommandManager;

        #region Availability checks
        
        public AbilityLegality CanEntityUseAbility(GridEntity entity, IAbilityData data, bool ignoreBlockingTimers) {
            // Is this entity set up to use this ability?
            if (!entity.Abilities.Contains(data)) {
                return AbilityLegality.IndefinitelyIllegal;
            }

            // Do we own the requirements for this ability?
            List<PurchasableData> ownedPurchasables = GameManager.Instance.GetPlayerForTeam(entity.Team)
                .OwnedPurchasablesController.OwnedPurchasables;
            if (data.Requirements.Any(r => !ownedPurchasables.Contains(r))) {
                return AbilityLegality.IndefinitelyIllegal;
            }
            
            // Are there any active timers blocking this ability?
            if (!ignoreBlockingTimers && entity.ActiveTimers.Any(t => t.ChannelBlockers.Contains(data.Channel) && !t.Expired)) {
                return AbilityLegality.NotCurrentlyLegal;
            }

            return AbilityLegality.Legal;
        }

        public bool IsAbilityChannelOnCooldownForEntity(GridEntity entity, AbilityChannel channel, out AbilityCooldownTimer timer) {
            timer = entity.ActiveTimers.FirstOrDefault(t => t.Ability.AbilityData.Channel == channel);
            return timer != null;
        }
        
        #endregion
        #region Perform

        public bool StartPerformingAbility(GridEntity entity, IAbilityData abilityData, IAbilityParameters parameters, bool fromInput,
                                    bool startPerformingEvenIfOnCooldown, bool clearOtherAbilities) {
            
            if (entity.BuildQueue != null && abilityData.TryingToPerformCancelsBuilds) {
                entity.BuildQueue.CancelAllBuilds();
            }

            AbilityLegality legality = abilityData.AbilityLegal(parameters, entity, startPerformingEvenIfOnCooldown);
            if (legality == AbilityLegality.IndefinitelyIllegal) {
                entity.AbilityFailed(abilityData);
                return false;
            }
            
            if (abilityData is not AttackAbilityData && entity.LastAttackedEntityValue is not null) {
                // Clear the targeted entity since we are telling this entity to do something else
                entity.LastAttackedEntity.UpdateValue(new NetworkableGridEntityValue(null));
            }
            
            IAbility abilityInstance = abilityData.CreateAbility(parameters, entity);
            
            if (clearOtherAbilities) { 
                CancelAllAbilities(entity); 
            }

            CommandManager.StartPerformingAbility(abilityInstance, fromInput);
            return true;
        }
        
        public void PerformOnStartAbilitiesForEntity(GridEntity entity) {
            foreach (IAbilityData abilityData in entity.Abilities.Where(a => a.PerformOnStart)) {
                StartPerformingAbility(entity, abilityData, abilityData.OnStartParameters, false, true, false);
            }
        }

        public void QueueAbility(GridEntity entity, IAbilityData abilityData, IAbilityParameters parameters, IAbility abilityToDependOn) {
            AbilityLegality legality = abilityData.AbilityLegal(parameters, entity, true);
            if (legality != AbilityLegality.Legal) {    // TODO should we allow Legality.NotCurrentlyLegal?
                entity.AbilityFailed(abilityData);
                return;
            }
            
            IAbility abilityInstance = abilityData.CreateAbility(parameters, entity);
            CommandManager.QueueAbility(abilityInstance, abilityToDependOn);
        }
        
        #endregion
        #region Expire abilities
        
        /// <summary>
        /// Server method. Clear the ability timer locally if the ability is active, otherwise remove it from the
        /// set of in-progress abilities.
        /// </summary>
        public void CancelAbility(GridEntity entity, IAbility ability) {
            ability.Cancel();
            if (ExpireTimerForAbility(entity, ability, true)) return;
            
            // The ability is in-progress, so remove it from the set
            IAbility localAbility = entity.InProgressAbilities.FirstOrDefault(a => a.UID == ability.UID);
            if (localAbility != null) {
                if (RemoveAbility(entity, localAbility)) {
                    CommandManager.AbilityExecutor.MarkInProgressAbilitiesDirty(entity);
                }
            }
        }

        public void CancelAllAbilities(GridEntity entity) {
            List<IAbility> abilities = new List<IAbility>(entity.InProgressAbilities);
            abilities.ForEach(a => GameManager.Instance.CommandManager.CancelAbility(a));
        }
        
        /// <summary>
        /// Remove the ability from the in-progress abilities set (here on the server)
        /// </summary>
        /// <returns>True if the ability was actually removed, otherwise false</returns>
        private bool RemoveAbility(GridEntity entity, IAbility ability) {
            IAbility abilityInstance = entity.InProgressAbilities.FirstOrDefault(t => t.UID == ability.UID);
            if (abilityInstance == null) {
                // This can happen if the whole set of in-progress abilities was cleared between sending the remove command and now
                return false;
            }

            entity.InProgressAbilities.Remove(abilityInstance);
            return true;
        }
        
        /// <summary>
        /// Client method. Returns true if the timer was active and successfully expired, otherwise false.
        /// </summary>
        public bool ExpireTimerForAbility(GridEntity entity, IAbility ability, bool canceled) {
            // Find the timer with the indicated ability. The timers themselves are not synchronized, but
            // since their abilities are we can use those. 
            AbilityCooldownTimer cooldownTimer = entity.ActiveTimers.FirstOrDefault(t => t.Ability.UID == ability.UID);
            if (cooldownTimer == null) return false;

            if (canceled && !ability.AbilityData.CancelableWhileOnCooldown) {
                // Can't cancel this ability's cooldown timer
                return false;
            }
            
            // The ability is indeed active, so cancel it
            cooldownTimer.Expire(canceled);
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
            
            MoveAbility newAbility = new MoveAbility(moveAbilityData, new MoveAbilityParameters {
                Destination = location.Value,
                NextMoveCell = location.Value,
                SelectorTeam = entity.Team,
                BlockedByOccupation = false
            }, entity);
            
            AddToCooldownTimer(entity, newAbility, timeToAdd);
        }
        
        /// <summary>
        /// Add time to the attack cooldown timer due to another ability being performed.
        /// If there is an active cooldown timer, then this amount is added to that timer.
        /// Otherwise, a new cooldown timer is added with this amount.
        /// </summary>
        public void AddAttackTime(GridEntity entity, float timeToAdd) {
            AttackAbilityData attackAbilityData = entity.GetAbilityData<AttackAbilityData>();
            if (attackAbilityData == null) return;

            AttackAbility newAbility = new AttackAbility(attackAbilityData, new AttackAbilityParameters(), entity);
            AddToCooldownTimer(entity, newAbility, timeToAdd);
        }
        
        private void AddToCooldownTimer(GridEntity entity, IAbility ability, float timeToAdd) {
            List<AbilityCooldownTimer> activeTimersCopy = new List<AbilityCooldownTimer>(entity.ActiveTimers);
            AbilityCooldownTimer timer = activeTimersCopy.FirstOrDefault(t => t.Ability.GetType() == ability.GetType());
            if (timer != null) {
                if (timer.Expired) {
                    Debug.LogWarning($"Tried to add {ability.GetType()} cooldown timer time from another ability, but that " +
                                     "timer is expired. Adding new cooldown timer instead. This might not behave correctly.");
                } else {
                    // Add this time to the current cooldown timer
                    entity.AddTimeToAbilityTimer(timer.Ability, timeToAdd);
                    return;
                }
            }
            
            // Since we won't actually be performing this ability, we need to generate a UID for it now
            int uid = IDUtil.GenerateUID();
            if (NetworkClient.active && !NetworkServer.active) {
                // MP client. Hack - use a different set of UIDs than what the server creates
                uid *= -1;
            }
            ability.UID = uid;
            
            // Add a new cooldown timer
            entity.CreateAbilityTimer(ability, timeToAdd);
        }
    }
}