using Gameplay.Entities.Abilities;

namespace Gameplay.Config.Abilities {
    /// <summary>
    /// Data for an attack ability (i.e. for damaging enemy entities)
    /// </summary>
    public interface IAttackAbilityData : IAbilityData {
        AttackAbilityLogicType AttackType { get; }
    }
}