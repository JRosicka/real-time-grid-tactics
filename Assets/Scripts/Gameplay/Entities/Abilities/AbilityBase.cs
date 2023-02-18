using System;
using Gameplay.Config.Abilities;
using Mirror;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// A base implementation of <see cref="IAbility"/>
    /// </summary>
    public abstract class AbilityBase<T, P> : IAbility where T : AbilityDataBase<P> where P : IAbilityParameters, new() {
        protected readonly T Data;
        public IAbilityData AbilityData => Data;
        public int UID { get; set; }
        public IAbilityParameters BaseParameters { get; }
        public GridEntity Performer { get; }
        protected AbilityBase(T data, IAbilityParameters abilityParameters, GridEntity performer) {
            Data = data;
            BaseParameters = abilityParameters;
            Performer = performer;
        }

        public void CompleteCooldown() {
            CompleteCooldownImpl();
            if (Data.RepeatWhenCooldownFinishes) {
                Performer.DoAbility(AbilityData, BaseParameters);
            }
        }

        protected abstract void CompleteCooldownImpl();

        public virtual void SerializeParameters(NetworkWriter writer) {
            writer.Write(Performer);
            BaseParameters.Serialize(writer);
        }

        public virtual void DeserializeImpl(NetworkReader reader) {
            // Nothing else to do by default
        }

        /// <summary>
        /// Pay any costs required for the ability. By default, this just creates a new timer for the performing <see cref="GridEntity"/>,
        /// but the impl method can be overridden to do other things too. 
        /// </summary>
        private void PayCost() {
            PayCostImpl();
            Performer.CreateAbilityTimer(this);
        }

        protected abstract void PayCostImpl();

        public bool PerformAbility() {
            if (!Data.AbilityLegal(BaseParameters, Performer)) return false;
            
            PayCost();
            DoAbilityEffect();
            return true;
        }
        
        public abstract void DoAbilityEffect();
    }
}