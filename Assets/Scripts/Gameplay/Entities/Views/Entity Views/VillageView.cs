using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;

namespace Gameplay.Entities {
    public class VillageView : GridEntityParticularView {
        public IncomeAnimationBehavior IncomeAnimationBehavior;

        private GridEntity _resourceEntity;

        public override void Initialize(GridEntity entity) {
            IncomeAnimationBehavior.Initialize(entity, ResourceType.Basic);
            _resourceEntity = GameManager.Instance.ResourceEntityFinder.GetMatchingResourceEntity(entity, entity.EntityData);
            _resourceEntity?.ToggleView(false);
        }
        
        public override void LethalDamageReceived() {
            _resourceEntity?.ToggleView(true);
        }

        public override bool DoAbility(IAbility ability, AbilityCooldownTimer cooldownTimer) {
            switch (ability.AbilityData) {
                case IncomeAbilityData data:
                    IncomeAnimationBehavior.DoIncomeAnimation(data);
                    return false;
                default:
                    return true;
            }
        }
    }
}