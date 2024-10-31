using System;
using System.Collections.Generic;
using System.Linq;
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
        public override bool CancelWhenNewCommandGivenToPerformer => false;
        public override bool CancelableWhileActive => false;
        public override bool CancelableWhileQueued => false;

        public override void SelectAbility(GridEntity selector) {
            // Nothing to do
        }

        public override bool CanPayCost(IAbilityParameters parameters, GridEntity entity) {
            return true;
        }

        protected override bool AbilityLegalImpl(NullAbilityParameters parameters, GridEntity entity) {
            Vector2Int? entityLocation = entity.Location;
            if (entityLocation == null) return false;
            
            // We need an eligible resource entity on this cell in order to get income from it
            GridEntity resourceEntity = GameManager.Instance.GetEntitiesAtLocation(entityLocation.Value)
                .Entities
                .Select(e => e.Entity)
                .FirstOrDefault(e => e.Tags.Contains(EntityData.EntityTag.Resource));
            if (resourceEntity == null) return false;
            if (resourceEntity.CurrentResources.Type != ResourceAmountIncome.Type) return false;
            if (resourceEntity.CurrentResources.Amount <= 0) return false;
            return true;
        }

        protected override IAbility CreateAbilityImpl(NullAbilityParameters parameters, GridEntity performer) {
            return new IncomeAbility(this, parameters, performer);
        }
    }
}