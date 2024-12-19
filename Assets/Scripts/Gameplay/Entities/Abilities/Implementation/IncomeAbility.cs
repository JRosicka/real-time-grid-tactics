using System.Linq;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using UnityEngine;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAbility"/> for collecting resource income
    /// </summary>
    public class IncomeAbility : AbilityBase<IncomeAbilityData, NullAbilityParameters> {
        public IncomeAbility(IncomeAbilityData data, NullAbilityParameters parameters, GridEntity performer) : base(data, parameters, performer) { }

        public override void Cancel() {
            // Nothing to do
        }

        protected override bool CompleteCooldownImpl() {
            Vector2Int? performerLocation = Performer.Location;
            if (performerLocation == null) return false;
            
            // Get the resource entity on this cell
            GridEntity resourceEntity = GameManager.Instance.GetEntitiesAtLocation(performerLocation.Value)
                .Entities
                .Select(e => e.Entity)
                .FirstOrDefault(e => e.Tags.Contains(EntityData.EntityTag.Resource));
            if (resourceEntity == null) return false;
            if (resourceEntity.CurrentResources.Value.Type != Data.ResourceAmountIncome.Type) return false;
            if (resourceEntity.CurrentResources.Value.Amount <= 0) return false;

            // Subtract the income amount from the resource entity
            ResourceAmount resourceAmount = new ResourceAmount(resourceEntity.CurrentResources.Value);
            int amountToGain = Mathf.Min(Data.ResourceAmountIncome.Amount, resourceAmount.Amount);
            resourceAmount.Amount -= amountToGain;
            resourceEntity.CurrentResources.UpdateValue(resourceAmount);

            // Gain the income
            ResourceAmount resourceIncome = new ResourceAmount {
                Type = Data.ResourceAmountIncome.Type,
                Amount = amountToGain
            };
            PlayerResourcesController resourcesController = GameManager.Instance.GetPlayerForTeam(Performer.Team).ResourcesController;
            resourcesController.Earn(resourceIncome);
            return true;
        }

        protected override void PayCostImpl() {
            // Nothing to do
        }

        public override bool DoAbilityEffect() {
            // Does not do anything yet - need to wait for cooldown to complete
            return true;
        }
    
    }
}