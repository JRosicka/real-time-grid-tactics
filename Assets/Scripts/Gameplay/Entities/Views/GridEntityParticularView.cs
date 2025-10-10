using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Entities {
    public abstract class GridEntityParticularView : MonoBehaviour {
        public abstract void Initialize(GridEntity entity);
        public abstract void LethalDamageReceived();
        /// <summary>
        /// Perform any entity-specific animations for the given ability.
        /// </summary>
        /// <returns>True if generic animation work should be performed, otherwise false</returns>
        public abstract bool DoAbility(IAbility ability, AbilityTimer abilityTimer);
    }
}