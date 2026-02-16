using Gameplay.Config.DeathAction;

namespace Gameplay.Entities.DeathAction {
    /// <summary>
    /// <see cref="IDeathAction"/> for awarding resources to the team of the attacker who kills this entity
    /// </summary>
    public class GiveResourcesDeathAction : IDeathAction {
        private readonly ResourceAmount _resourceAmount;
        public GiveResourcesDeathAction(GiveResourcesDeathActionData data) {
            _resourceAmount = data.ResourceToAward;
        }
        
        public void DoDeathAction(GridEntity dyingEntity, GridEntity attacker) {
            GameTeam teamToAward = attacker.Team;
            GameManager.Instance.GetPlayerForTeam(teamToAward).ResourcesController.Earn(_resourceAmount);
        }
    }
}