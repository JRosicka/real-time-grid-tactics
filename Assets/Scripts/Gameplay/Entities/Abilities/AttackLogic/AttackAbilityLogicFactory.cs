using System;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// Instantiates <see cref="IAttackLogic"/> objects
    /// </summary>
    public static class AttackAbilityLogicFactory {
        public static IAttackLogic CreateAttackLogic(AttackAbilityLogicType attackType) {
            return attackType switch {
                AttackAbilityLogicType.None => null,
                AttackAbilityLogicType.NormalMelee => new MeleeAttackLogic(),
                AttackAbilityLogicType.ArcherArrow => new ArcherArrowAttackLogic(),
                AttackAbilityLogicType.CatapultRocks => new CatapultAttackLogic(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}