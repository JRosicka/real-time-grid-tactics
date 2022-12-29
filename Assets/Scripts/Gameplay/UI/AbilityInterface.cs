using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// Handles displaying a selected <see cref="GridEntity"/>'s <see cref="IAbility"/>s and allowing the player to use them
    /// </summary>
    public class AbilityInterface : MonoBehaviour {


        public void Initialize(GridEntity entity) {
            // TODO check to see if entity is friendly, if so then have the abilities be usable
        }

        public void ToggleActive(bool active) {
            
        }
    }
}