using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;

namespace Gameplay.Entities {
    public class AmberMineView : GridEntityParticularView {
        public IncomeAnimationBehavior IncomeAnimationBehavior;

        private GridEntity _entity;
        private GridEntity _resourceEntity;
        private bool _dying;
        
        public override void Initialize(GridEntity entity) {
            _entity = entity;
            GameManager.Instance.CommandManager.EntityCollectionChangedEvent += EntityCollectionChanged;

            IncomeAnimationBehavior.Initialize(entity, ResourceType.Advanced);
            CheckForResourceEntity();
        }

        public override void LethalDamageReceived() {
            _dying = true;
            _resourceEntity?.ToggleView(true);
            GameManager.Instance.CommandManager.EntityCollectionChangedEvent -= EntityCollectionChanged;
        }
        public override void NonLethalDamageReceived() { }

        public override bool DoAbility(IAbility ability, AbilityTimer abilityTimer) {
            switch (ability.AbilityData) {
                case IncomeAbilityData data:
                    IncomeAnimationBehavior.DoIncomeAnimation();
                    return false;
                default:
                    return true;
            }
        }
        
        private void EntityCollectionChanged() {
            if (_dying) return;
            CheckForResourceEntity();
        }

        private void CheckForResourceEntity() {
            _resourceEntity = GameManager.Instance.ResourceEntityFinder.GetMatchingResourceEntity(_entity, _entity.EntityData);
            _resourceEntity?.ToggleView(false);
        }
    }
}