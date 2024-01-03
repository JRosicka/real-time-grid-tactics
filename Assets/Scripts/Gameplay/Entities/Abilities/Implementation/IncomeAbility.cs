using Gameplay.Config.Abilities;
using UnityEditor.Build;
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
            PlayerResourcesController resourcesController = GameManager.Instance.GetPlayerForTeam(Performer.MyTeam).ResourcesController;
            resourcesController.Earn(Data.ResourceAmountIncome);
            return true;
        }

        protected override void PayCostImpl() {
            // Nothing to do
        }

        public override bool DoAbilityEffect() {
            // Does not do anything yet - need to wait for cooldown to complete
            Debug.Log("Starting income cycle, cool");
            return true;
        }
    
    }
}