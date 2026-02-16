using Gameplay.Entities.DeathAction;
using UnityEngine;

namespace Gameplay.Config.DeathAction {
    /// <summary>
    /// <see cref="DeathActionData"/> for awarding resources to the team of the attacker who kills this entity
    /// </summary>
    [CreateAssetMenu(menuName = "Grid Entities/DeathAction/GiveMoney", fileName = "GiveMoneyDeathAction", order = 0)]
    public class GiveResourcesDeathActionData : DeathActionData {
        public ResourceAmount ResourceToAward;
        
        public override IDeathAction CreateDeathAction() {
            return new GiveResourcesDeathAction(this);
        }
    }
}