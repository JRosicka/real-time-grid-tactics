using System;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Config.Abilities {
    [CreateAssetMenu(menuName = "Abilities/IncomeAbilityData")]
    public class IncomeAbilityDataAsset : BaseAbilityDataAsset<IncomeAbilityData, NullAbilityParameters> { }

    /// <summary>
    /// A <see cref="AbilityDataBase{T}"/> configuration for the ability to earn resources
    /// </summary>
    [Serializable]
    public class IncomeAbilityData : AbilityDataBase<NullAbilityParameters> {
        public ResourceAmount ResourceAmountIncome;
        public override bool CancelableWhileOnCooldown => false;
        public override bool CancelableWhileInProgress => false;
        public override bool Cancelable => false;

        public override void SelectAbility(GridEntity selector) {
            // Nothing to do
        }
        
        protected override AbilityLegality AbilityLegalImpl(NullAbilityParameters parameters, GridEntity entity, GameTeam team) {
            Vector2Int? entityLocation = entity.Location;
            if (entityLocation == null) return AbilityLegality.IndefinitelyIllegal;
            
            // We need an eligible resource entity on this cell in order to get income from it
            GridEntity resourceEntity = GameManager.Instance.ResourceEntityFinder.GetMatchingResourceEntity(entity, entity.EntityData);
            if (resourceEntity == null) return AbilityLegality.IndefinitelyIllegal;
            if (resourceEntity.CurrentResourcesValue.Type != ResourceAmountIncome.Type) return AbilityLegality.IndefinitelyIllegal;
            if (resourceEntity.CurrentResourcesValue.Amount <= 0) return AbilityLegality.NotCurrentlyLegal;
            return AbilityLegality.Legal;
        }

        protected override IAbility CreateAbilityImpl(NullAbilityParameters parameters, GridEntity performer, GameTeam? overrideTeam) {
            return new IncomeAbility(this, parameters, performer, overrideTeam);
        }
    }
}