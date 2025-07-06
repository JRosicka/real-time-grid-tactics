using System;
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
        public override bool CanBeCanceled => false;
        public override bool CancelableWhileActive => false;
        public override bool CancelableWhileInProgress => false;

        public override void SelectAbility(GridEntity selector) {
            // Nothing to do
        }

        public override bool CanPayCost(IAbilityParameters parameters, GridEntity entity) {
            return true;
        }

        protected override (bool, AbilityResult?) AbilityLegalImpl(NullAbilityParameters parameters, GridEntity entity) {
            Vector2Int? entityLocation = entity.Location;
            if (entityLocation == null) return (false, AbilityResult.Failed);
            
            // We need an eligible resource entity on this cell in order to get income from it
            GridEntity resourceEntity = GameManager.Instance.GetEntitiesAtLocation(entityLocation.Value)
                ?.Entities
                .Select(e => e.Entity)
                .FirstOrDefault(e => e.Tags.Contains(EntityTag.Resource));
            if (resourceEntity == null) return (false, AbilityResult.Failed);
            if (resourceEntity.CurrentResourcesValue.Type != ResourceAmountIncome.Type) return (false, AbilityResult.Failed);
            if (resourceEntity.CurrentResourcesValue.Amount <= 0) return (false, AbilityResult.CompletedWithoutEffect);
            return (true, null);
        }

        protected override IAbility CreateAbilityImpl(NullAbilityParameters parameters, GridEntity performer) {
            return new IncomeAbility(this, parameters, performer);
        }
    }
}