using System;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    [CreateAssetMenu(menuName = "Abilities/HealOnStructureAbilityData")]
    public class HealOnStructureAbilityDataAsset : BaseAbilityDataAsset<HealOnStructureAbilityData, NullAbilityParameters> { }

    /// <summary>
    /// A <see cref="AbilityDataBase{T}"/> configuration for healing a friendly unit on top of this structure
    /// </summary>
    [Serializable]
    public class HealOnStructureAbilityData : AbilityDataBase<NullAbilityParameters> {
        public int HealAmount;
        public HealAbilityDataAsset HealAbility;
        public override bool CanBeCanceled => false;
        public override bool CancelableWhileActive => false;
        public override bool CancelableWhileQueued => false;

        public override void SelectAbility(GridEntity selector) {
            // Nothing to do
        }

        public override bool CanPayCost(IAbilityParameters parameters, GridEntity entity) {
            return true;
        }

        protected override bool AbilityLegalImpl(NullAbilityParameters parameters, GridEntity entity) {
            if (!entity.Registered || entity.DeadOrDying()) return false;
            Vector2Int? entityLocation = entity.Location;
            if (entityLocation == null) return false;
            
            // Check to see if there is any eligible target at the performer's location
            GridEntity potentialTarget = GameManager.Instance.GetTopEntityAtLocation(entityLocation.Value);
            HealAbilityParameters healParameters = new HealAbilityParameters {
                Target = potentialTarget,
                HealAmount = HealAmount
            };

            return potentialTarget != entity && HealAbility.Content.AbilityLegal(healParameters, entity);
        }
        
        protected override IAbility CreateAbilityImpl(NullAbilityParameters parameters, GridEntity performer) {
            return new HealOnStructureAbility(this, parameters, performer);
        }
    }
}