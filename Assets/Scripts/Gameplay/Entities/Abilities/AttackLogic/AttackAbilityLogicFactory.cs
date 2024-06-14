using System;
using Gameplay.Config.Abilities;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// Instantiates <see cref="IAttackLogic"/> objects
    /// </summary>
    public static class AttackAbilityLogicFactory {
        public static IAttackLogic CreateAttackLogic(AttackAbility attackAbility) {
            return ((AttackAbilityData)attackAbility.AbilityData).AttackType switch {
                AttackAbilityLogicType.None => null,
                AttackAbilityLogicType.NormalMelee => new MeleeAttackLogic(),
                AttackAbilityLogicType.ArcherArrow => new ArcherArrowAttackLogic(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}