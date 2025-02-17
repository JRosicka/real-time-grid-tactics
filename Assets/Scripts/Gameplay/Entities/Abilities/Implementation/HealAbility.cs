using Gameplay.Config.Abilities;
using Mirror;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for healing a friendly unit.
    /// </summary>
    public class HealAbility : AbilityBase<HealAbilityData, HealAbilityParameters> {
        public HealAbilityParameters AbilityParameters => (HealAbilityParameters) BaseParameters;

        public HealAbility(HealAbilityData data, HealAbilityParameters parameters, GridEntity performer) : base(data, parameters, performer) {
            
        }

        public override bool ShouldShowCooldownTimer => true;

        public override void Cancel() {
            if (AbilityParameters.Target != null) {
                AbilityParameters.Target.KilledEvent -= TargetEntityKilled;
                AbilityParameters.Target.EntityMovedEvent -= TargetEntityNoLongerValid;
            }
        }

        protected override bool CompleteCooldownImpl() {
            if (Data.CanHeal(AbilityParameters, Performer)) {
                // Actually perform the heal
                AbilityParameters.Target.HPHandler.Heal(AbilityParameters.HealAmount);
            }

            if (AbilityParameters.Target != null) {
                AbilityParameters.Target.KilledEvent -= TargetEntityKilled;
                AbilityParameters.Target.EntityMovedEvent -= TargetEntityNoLongerValid;
            } 

            return true;
        }
        
        protected override void PayCostImpl() {
            // Nothing to do
        }
        
        public override bool DoAbilityEffect() {
            AbilityParameters.Target.KilledEvent += TargetEntityKilled;
            AbilityParameters.Target.EntityMovedEvent += TargetEntityNoLongerValid;

            // Otherwise nothing to do - need to wait until cooldown completes
            return true;
        }

        private void TargetEntityKilled() {
            TargetEntityNoLongerValid();
        }
        
        private void TargetEntityNoLongerValid() {
            AbilityParameters.Target.KilledEvent -= TargetEntityKilled;
            AbilityParameters.Target.EntityMovedEvent -= TargetEntityNoLongerValid;

            // Cancel the ability since the target is no longer heal-able
            GameManager.Instance.CommandManager.CancelAbility(this);
        }
    }

    public class HealAbilityParameters : IAbilityParameters {
        public GridEntity Target;
        public int HealAmount;
        public void Serialize(NetworkWriter writer) {
            writer.Write(Target);
            writer.WriteInt(HealAmount);
        }

        public void Deserialize(NetworkReader reader) {
            Target = reader.Read<GridEntity>();
            HealAmount = reader.ReadInt();
        }
    }
}