using Gameplay.Config.Abilities;
using Mirror;
using UnityEngine;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for attacking a <see cref="GridEntity"/>
    /// </summary>
    public class AttackAbility : AbilityBase<AttackAbilityData, AttackAbilityParameters> {
        private AttackAbilityParameters AbilityParameters => (AttackAbilityParameters) BaseParameters;
        private int _moveCost;
        
        public AttackAbility(AttackAbilityData data, AttackAbilityParameters parameters, GridEntity performer) : base(data, parameters, performer) {
            
        }

        protected override bool CompleteCooldownImpl() {
            // Nothing to do
            return true;
            // TODO some of these are abstract and have empty overrides, and other methods are virtual with the option of overriding. Would be nice to pick one and use that consistently, otherwise it seems like it would be easy to forget some of these when implementing new abilities. 
        }

        protected override void PayCostImpl() {
            // Nothing to do
        }

        public override void DoAbilityEffect() {
            Debug.Log($"Did attack to {AbilityParameters.Target.DisplayName}, cool");
            AbilityParameters.Target.ReceiveAttackFromEntity(AbilityParameters.Attacker);

        }
    }

    public class AttackAbilityParameters : IAbilityParameters {
        public GridEntity Attacker;
        public GridEntity Target;
        public void Serialize(NetworkWriter writer) {
            writer.Write(Attacker);
            writer.Write(Target);
        }

        public void Deserialize(NetworkReader reader) {
            Attacker = reader.Read<GridEntity>();
            Target = reader.Read<GridEntity>();
        }
    }
}