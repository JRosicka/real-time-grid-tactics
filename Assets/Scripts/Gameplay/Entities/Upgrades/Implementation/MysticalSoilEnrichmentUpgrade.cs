using Gameplay.Config.Upgrades;

namespace Gameplay.Entities.Upgrades {
    public class MysticalSoilEnrichmentUpgrade : AbstractUpgrade<MysticalSoilEnrichmentUpgradeData> {
        public MysticalSoilEnrichmentUpgrade(MysticalSoilEnrichmentUpgradeData data, GameTeam team) : base(data, team) {}
        
        protected override void ApplyGlobalEffect() {
            // No effect
        }
        public override void ApplyUpgrade(GridEntity friendlyEntity) {
            if (friendlyEntity.EntityData != Data.VillageEntityData) return;
            
            // This is a friendly village, so add to the resource count of the occupied resource entity
            GridEntity resourceEntity = GameManager.Instance.ResourceEntityFinder.GetMatchingResourceEntity(friendlyEntity, friendlyEntity.EntityData);
            ResourceAmount newAmount = new() {
                Type = Data.ResourceAmountToAdd.Type,
                Amount = resourceEntity.CurrentResourcesValue.Amount + Data.ResourceAmountToAdd.Amount
            };
            resourceEntity.CurrentResources.UpdateValue(newAmount);
        }

        protected override void RemoveGlobalEffect() {
            // No effect
        }
        public override void RemoveUpgrade(GridEntity friendlyEntity) {
            // No effect
        }
    }
}