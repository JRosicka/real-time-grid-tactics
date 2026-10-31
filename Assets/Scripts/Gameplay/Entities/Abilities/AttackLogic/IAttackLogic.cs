using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// Defines behavior for actually executing the attack. Runs on the server. 
    /// </summary>
    public interface IAttackLogic {
        void DoAttack(GridEntity attacker, GridEntity target, int bonusDamage);
        void DoAttack(GridEntity attacker, HashSet<Vector2Int> targetLocations, int bonusDamage);
    }
}