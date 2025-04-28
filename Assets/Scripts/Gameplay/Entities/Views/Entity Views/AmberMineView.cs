using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Entities {
    public class AmberMineView : GridEntityParticularView {
        public IncomeAnimationBehavior IncomeAnimationBehavior;
        public EntityData AmberMineResourceEntity;

        private GridEntity _resourceEntity;
        
        public override void Initialize(GridEntity entity) {
            IncomeAnimationBehavior.Initialize(entity, ResourceType.Advanced);
            RecordMatchingResourceEntity(entity);
            _resourceEntity.ToggleView(false);
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

        private void RecordMatchingResourceEntity(GridEntity entity) {
            if (entity.Location == null) {
                Debug.LogWarning($"{entity.EntityData.ID} location is null!");
                return;
            }

            var entities = GameManager.Instance.GetEntitiesAtLocation(entity.Location.Value);
            if (entities == null) {
                Debug.LogWarning($"No entities found at location ({entity.Location.Value})!");
                return;
            }

            _resourceEntity = entities.Entities.Select(o => o.Entity).FirstOrDefault(e => e.EntityData.ID == AmberMineResourceEntity.ID);
            if (_resourceEntity == null) {
                Debug.LogWarning("Matching amber resource entity not found!");
            }
        }
    }
}