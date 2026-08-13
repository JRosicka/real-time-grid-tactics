using Gameplay.Config;
using Gameplay.Config.Abilities;
using UnityEngine;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for selling a structure for a refund and a peasant.
    /// </summary>
    public class SellStructureAbility : AbilityBase<SellStructureAbilityData, NullAbilityParameters> {
        public SellStructureAbility(SellStructureAbilityData data, NullAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam) : base(data, parameters, performer, overrideTeam) { }

        public override AbilityExecutionType ExecutionType => AbilityExecutionType.PreInteractionGridUpdate;

        public override float CooldownDuration => Performer.EntityData.StructureSellTime;

        public override bool ShouldShowAbilityTimer => true;

        public override void Cancel() {
            // Nothing to do
        }

        protected override bool CompleteCooldownImpl() {
            AwardSellEffect();
            return true;
        }

        private void AwardSellEffect() {
            if (Performer == null || Performer.DeadOrDying || Performer.Location == null) return;
            
            // Give resources equal to half of the structure cost, rounded down
            foreach (ResourceAmount resourceAmount in Performer.EntityData.Cost) {
                if (resourceAmount.Amount > 0) {
                    GameManager.Instance.GetPlayerForTeam(Performer.Team).ResourcesController.Earn(new ResourceAmount {
                        Amount = resourceAmount.Amount / 2,
                        Type = resourceAmount.Type
                    });
                }
            }
            
            // Unlock the structure so we can spawn a peasant
            Performer.SetLockStatus(false);
            
            // Spawn a peasant at the structure location
            SpawnEntity(Data.Peasant, Performer.Location.Value);
            
            // Unregister and destroy the structure, along with death effect. Select the spawned peasant if the structure was selected. TODO
            GameManager.Instance.CommandManager.AbilityExecutor.MarkForUnRegistration(Performer, true, false);
        }

        // Server method
        private void SpawnEntity(EntityData entityData, Vector2Int location) {
            if (GameManager.Instance == null) return;
            GameManager.Instance.CommandManager.SpawnEntity(entityData, location, Performer.Team, Performer, location, false, true);
        }
        
        public override bool TryDoAbilityStartEffect() {
            // If there are any other units at the structure, then fail.
            if (Data.OccupiedByOtherEntities(Performer)) return false;
            
            // Lock the structure so that we can't do anything with it or move anything onto it. TODO actually make it so that no commands can be given while the structure is locked
            Performer.SetLockStatus(true);
            
            return true;
        }
        
        protected override (bool, AbilityResult) DoAbilityEffect() {
            // Real sell happens at the end of cooldown
            return (true, AbilityResult.CompletedWithEffect);
        }
    }
}