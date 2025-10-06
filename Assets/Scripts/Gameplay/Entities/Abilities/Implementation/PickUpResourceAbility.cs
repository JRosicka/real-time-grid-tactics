using Gameplay.Config.Abilities;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for a resource pickup getting collected
    /// </summary>
    public class PickUpResourceAbility : AbilityBase<PickUpResourceAbilityData, NullAbilityParameters> {
        public PickUpResourceAbility(PickUpResourceAbilityData data, NullAbilityParameters parameters, GridEntity performer) : base(data, parameters, performer) { }

        public override AbilityExecutionType ExecutionType => AbilityExecutionType.PreInteractionGridUpdate;
        public override bool ShouldShowCooldownTimer => false;

        public override void Cancel() {
            // Nothing to do
        }

        protected override bool CompleteCooldownImpl() {
            return true;
        }

        public override bool TryDoAbilityStartEffect() {
            // Nothing to do
            return true;
        }

        protected override (bool, AbilityResult) DoAbilityEffect() {
            if (!Performer || Performer.Location == null) return (false, AbilityResult.Failed);
            
            GridEntity topEntity = GameManager.Instance.CommandManager.EntitiesOnGrid.EntitiesAtLocation(Performer.Location.Value)?.GetTopEntity()?.Entity;
            if (!topEntity || topEntity == Performer || topEntity.Team == GameTeam.Neutral) return (false, AbilityResult.IncompleteWithoutEffect);
            
            // There is an entity here belonging to a player, so award them the resources
            ResourceAmount resourceAmount = new ResourceAmount(Performer.EntityData.StartingResourceSet);
            PlayerResourcesController resourcesController = GameManager.Instance.GetPlayerForTeam(topEntity.Team).ResourcesController;
            resourcesController.Earn(resourceAmount);
            
            // Now destroy the resource pickup
            GameManager.Instance.CommandManager.AbilityExecutor.MarkForUnRegistration(Performer, false);
            return (true, AbilityResult.CompletedWithEffect);
        }
    }
}