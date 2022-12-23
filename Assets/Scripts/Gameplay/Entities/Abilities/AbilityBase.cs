using Gameplay.Config.Abilities;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// A base implementation of <see cref="IAbility"/>
    /// </summary>
    public abstract class AbilityBase<T, P> : IAbility where T : AbilityDataBase<P> where P : IAbilityParameters {
        protected readonly T Data;
        public IAbilityData AbilityData => Data;
        public IAbilityParameters BaseParameters { get; }

        protected AbilityBase(T data, IAbilityParameters abilityParameters) {
            Data = data;
            BaseParameters = abilityParameters;
        }
    
        public abstract void PerformAbility();
    }
}