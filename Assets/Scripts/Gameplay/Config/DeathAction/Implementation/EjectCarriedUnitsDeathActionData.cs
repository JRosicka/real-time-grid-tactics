using Gameplay.Entities.DeathAction;
using UnityEngine;

namespace Gameplay.Config.DeathAction {
    /// <summary>
    /// <see cref="DeathActionData"/> for putting carried units back on the grid when the carrier dies
    /// </summary>
    [CreateAssetMenu(menuName = "Grid Entities/DeathAction/EjectCarriedUnits", fileName = "EjectCarriedUnitsDeathAction", order = 0)]
    public class EjectCarriedUnitsDeathActionData : DeathActionData {
        public override IDeathAction CreateDeathAction() {
            return new EjectCarriedUnitsDeathAction();
        }
    }
}