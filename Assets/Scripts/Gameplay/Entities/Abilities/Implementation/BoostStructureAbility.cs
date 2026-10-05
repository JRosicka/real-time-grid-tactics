using System;
using System.Collections.Generic;
using Gameplay.Commands;
using Gameplay.Config.Abilities;
using JetBrains.Annotations;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for boosting the production speed and income of an adjacent structure
    /// </summary>
    public class BoostStructureAbility : AbilityBase<BoostStructureAbilityData, BoostStructureAbilityParameters> {
        private BoostStructureAbilityParameters AbilityParameters => (BoostStructureAbilityParameters) BaseParameters;

        public BoostStructureAbility(BoostStructureAbilityData data, BoostStructureAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam) : base(data, parameters, performer, overrideTeam) { }
        private System.Random RNG => GameManager.Instance.SeedManager.GetRNG(Performer.UID);
        private AbilityEventRouter AbilityEventRouter => GameManager.Instance.AbilityEventRouter;
        
        public override AbilityExecutionType ExecutionType => AbilityExecutionType.PreInteractionGridUpdate;
        public override bool ShouldShowAbilityTimer => false;
        
        public override void Cancel() {
            // Unregister the target entity from the target (if target is set)
            if (AbilityParameters.Target != null && AbilityParameters.Target.ContainsBoost(Performer)) {
                AbilityParameters.Target.UnRegisterBoost(Performer);
            }
            
            AbilityEventRouter.UnregisterListeners(UID);
        }
        
        protected override bool CompleteCooldownImpl() {
            return true;
        }

        public override bool TryDoAbilityStartEffect() {
            if (AbilityParameters.Target == null) {
                AbilityParameters.Target = PickAdjacentStructureToBoost();
            }
            
            AbilityEventRouter.RegisterListener<Action<GameTeam>>(Performer, UID, 
                handler => GameManager.Instance.CommandManager.EntityRegisteredEvent += handler,
                handler => GameManager.Instance.CommandManager.EntityRegisteredEvent -= handler,
                EntityRegistered, nameof(EntityRegistered));

            return true;
        }
        
        protected override (bool, AbilityResult) DoAbilityEffect() {
            if (AbilityParameters.Target != null && (AbilityParameters.Target.DeadOrDying || AbilityParameters.Target.Location == null)) {
                AbilityParameters.Target = PickAdjacentStructureToBoost();
                if (AbilityParameters.Target == null) {
                    // Newly null, so indicate that an effect happened so the animation updates. Otherwise that will happen below. 
                    return (false, AbilityResult.IncompleteWithEffect);
                }
            }

            if (AbilityParameters.Target == null) {
                return (false, AbilityResult.IncompleteWithoutEffect);
            }

            // Check to see if this ability is registered for the target entity. If not, register it so its effect is applied.
            if (!AbilityParameters.Target.ContainsBoost(Performer)) {
                AbilityParameters.Target.RegisterBoost(Performer, Data.BoostAmount);
            }
            
            // Otherwise nothing has changed
            return (false, AbilityResult.IncompleteWithoutEffect);
        }
        
        /// <summary>
        /// Search all adjacent (to the performer) structures and return an arbitrary one to boost.
        /// A null return means there are no viable structures. 
        /// </summary>
        [CanBeNull]
        private GridEntity PickAdjacentStructureToBoost() {
            List<Vector2Int> viableLocations = Data.GetViableTargets(Performer);
            if (viableLocations.Count == 0) return null;
            if (viableLocations.Count == 1) return Data.GetBoostableEntity(viableLocations[0], PerformerTeam);
            
            // Pick one at random
            Vector2Int location = viableLocations[RNG.Next(0, viableLocations.Count)];
            GridEntity entity = Data.GetBoostableEntity(location, PerformerTeam);

            if (entity != null) {
                AbilityEventRouter.RegisterListener<Action>(Performer, UID, 
                    handler => entity.UnregisteredEvent += handler,
                    handler => entity.UnregisteredEvent -= handler,
                    TargetEntityUnregistered, nameof(TargetEntityUnregistered));
            }
            
            return entity;
        }

        private void TargetEntityUnregistered() {
            AbilityEventRouter.UnregisterListener(UID, nameof(TargetEntityUnregistered));
            AbilityParameters.Target = PickAdjacentStructureToBoost();
        }

        private void EntityRegistered(GameTeam team) {
            if (team != PerformerTeam) return;
            if (AbilityParameters.Target != null) return;
            
            // See if we should apply this boost ability to the new entity
            GridEntity newEntity = PickAdjacentStructureToBoost();
            if (newEntity != null) {
                AbilityParameters.Target = newEntity;
            }
        }
    }

    public class BoostStructureAbilityParameters : IAbilityParameters {
        public GridEntity Target;
        public void Serialize(NetworkWriter writer) {
            writer.Write(Target);
        }

        public string SerializeToJson() {
            return JsonConvert.SerializeObject(new Dictionary<string, object> {
                {"Target", Target?.UID ?? 0}
            });
        }

        public void Deserialize(NetworkReader reader) {
            Target = reader.Read<GridEntity>();
        }
    }
}