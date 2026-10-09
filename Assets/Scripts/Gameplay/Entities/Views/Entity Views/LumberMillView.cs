using Gameplay.Entities.Abilities;
using Gameplay.Entities.Upgrades;

namespace Gameplay.Entities {
    public class LumberMillView : GridEntityParticularView {
        public override void Initialize(GridEntity entity) { }
        public override void LethalDamageReceived() { }
        public override void NonLethalDamageReceived() { }

        public override bool DoAbility(IAbility ability, AbilityTimer abilityTimer) {
            switch (ability) {
                case BoostStructureAbility boostAbility:
                    DoBoostAnimation(boostAbility.Performer, boostAbility.AbilityParameters.Target);
                    return false;
                default:
                    return true;
            }
        }

        public override void UpgradeApplied(IUpgrade upgrade) { }

        private void DoBoostAnimation(GridEntity performer, GridEntity target) {
            
        }
    }
}