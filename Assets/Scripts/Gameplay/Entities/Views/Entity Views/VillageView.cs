using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using Gameplay.Entities.Upgrades;
using UnityEngine;

namespace Gameplay.Entities {
    public class VillageView : GridEntityParticularView {
        public IncomeAnimationBehavior IncomeAnimationBehavior;
        public DisablingDamageAnimationBehavior DamageAnimationBehavior;

        private GridEntity _entity;
        private GridEntity _resourceEntity;
        private bool _dying;

        public override void Initialize(GridEntity entity) {
            _entity = entity;
            Color teamColor = GameManager.Instance.GetPlayerForTeam(entity).ColorData.TeamColor;
            
            GameManager.Instance.CommandManager.EntityCollectionChangedEvent += EntityCollectionChanged;
            
            IncomeAnimationBehavior.Initialize(entity, ResourceType.Basic);
            DamageAnimationBehavior.Initialize(entity, teamColor);
            CheckForResourceEntity();
        }
        
        public override void LethalDamageReceived() {
            _dying = true;
            _resourceEntity?.ToggleView(true);
            GameManager.Instance.CommandManager.EntityCollectionChangedEvent -= EntityCollectionChanged;
        }

        public override void NonLethalDamageReceived() {
            DamageAnimationBehavior.HandleDamageReceived();
        }

        public override bool DoAbility(IAbility ability, AbilityTimer abilityTimer) {
            switch (ability.AbilityData) {
                case IncomeAbilityData data:
                    IncomeAnimationBehavior.DoIncomeAnimation();
                    return false;
                default:
                    return true;
            }
        }

        public override void UpgradeApplied(IUpgrade upgrade) { }

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