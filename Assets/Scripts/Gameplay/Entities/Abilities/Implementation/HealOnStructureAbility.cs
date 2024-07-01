using Gameplay.Config.Abilities;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for healing a friendly unit on top of this structure. Periodically checks for if
    /// healing can be performed and creates <see cref="HealAbility"/>s if able. 
    /// </summary>
    public class HealOnStructureAbility : AbilityBase<HealOnStructureAbilityData, NullAbilityParameters> {
        public HealOnStructureAbility(HealOnStructureAbilityData data, NullAbilityParameters parameters, GridEntity performer) : base(data, parameters, performer) {
            
        }
        
        public override void Cancel() {
            // Nothing to do
        }

        protected override bool CompleteCooldownImpl() {
            // Nothing to do
            return true;
        }

        protected override void PayCostImpl() {
            // Nothing to do
        }
        
        public override bool DoAbilityEffect() {
            // Queue the heal ability
            GridEntity target = GameManager.Instance.GetTopEntityAtLocation(Performer.Location);
            Performer.PerformAbility(Data.HealAbility.Content, new HealAbilityParameters {
                Target = target,
                HealAmount = Data.HealAmount
            }, true);
            
            return true;
        }
    }
}