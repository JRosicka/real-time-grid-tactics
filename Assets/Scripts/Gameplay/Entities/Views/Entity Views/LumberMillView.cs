using Gameplay.Entities.Abilities;
using Gameplay.Entities.Upgrades;
using UnityEngine;

namespace Gameplay.Entities {
    public class LumberMillView : GridEntityParticularView {
        [SerializeField] private BoostRayController _boostRayController;

        public override void Initialize(GridEntity entity) {
            _boostRayController.Initialize(entity);
        }

        public override void LethalDamageReceived() {
            _boostRayController.TearDown();
        }
        public override void NonLethalDamageReceived() { }

        public override bool DoAbility(IAbility ability, AbilityTimer abilityTimer) {
            switch (ability) {
                case BoostStructureAbility boostAbility:
                    DoBoostAnimation(boostAbility.AbilityParameters.Target);
                    return false;
                default:
                    return true;
            }
        }

        public override void UpgradeApplied(IUpgrade upgrade) { }

        private void DoBoostAnimation(GridEntity target) {
            _boostRayController.Activate(target);
        }
    }
}