using Gameplay.Entities.Abilities;
using UnityEngine;

namespace Gameplay.Entities {
    public class ArcherView : GridEntityParticularView {
        public override bool DoAbility(IAbility ability, AbilityCooldownTimer cooldownTimer) {
            return true;
        }
    }
}