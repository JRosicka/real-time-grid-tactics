namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// Arbitrary set of ability-instance-specific parameters
    /// </summary>
    public interface IAbilityParameters {}    // TODO I don't think this is going to be networked properly... required during read/write of IAbility
    
    public class NullAbilityParameters : IAbilityParameters {}
}