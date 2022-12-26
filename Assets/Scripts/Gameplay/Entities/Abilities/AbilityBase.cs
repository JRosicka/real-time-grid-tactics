using Gameplay.Config.Abilities;
using Mirror;
using UnityEngine;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// A base implementation of <see cref="IAbility"/>
    /// </summary>
    public abstract class AbilityBase<T, P> : IAbility where T : AbilityDataBase<P> where P : IAbilityParameters, new() {
        protected readonly T Data;
        public IAbilityData AbilityData => Data;
        public IAbilityParameters BaseParameters { get; }
        public void SerializeParameters(NetworkWriter writer) {
            BaseParameters.Serialize(writer);
        }
        
        protected AbilityBase(T data, IAbilityParameters abilityParameters) {
            Data = data;
            BaseParameters = abilityParameters;
        }
    
        public abstract void PerformAbility();
    }
}