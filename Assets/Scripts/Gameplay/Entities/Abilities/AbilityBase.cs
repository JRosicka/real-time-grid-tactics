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
        public bool WaitUntilLegal { get; set; }

        protected AbilityBase(T data, IAbilityParameters abilityParameters, GridEntity performer) {
            Data = data;
            BaseParameters = abilityParameters;
            Performer = performer;
        }

        public virtual float CooldownDuration => Data.CooldownDuration;

        public bool CompleteCooldown() {
            if (!CompleteCooldownImpl()) {
                return false;
            }
            
            if (Data.RepeatWhenCooldownFinishes) {
                Performer.PerformAbility(AbilityData, BaseParameters, WaitUntilLegal, false);
            }

            return true;
        }

        public abstract void Cancel();

        protected abstract bool CompleteCooldownImpl();

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
        public void PayCost(bool justSpecificCost) {
            if (justSpecificCost) {
                PayCostImpl();
                return;
            }
            
            // If we pay up front, then that part already happened
            if (!Data.PayCostUpFront) {
                PayCostImpl();
            }
            
            Performer.CreateAbilityTimer(this);
            if (Data.AddedMovementTime > 0) {
                Performer.AddMovementTime(Data.AddedMovementTime);
            }
        }

        protected abstract void PayCostImpl();

        public bool PerformAbility() {
            // TODO need to redefine move ability to be legal as long as we can progress towards target
            if (!Data.AbilityLegal(BaseParameters, Performer)) return false;

            bool needToPayCost = DoAbilityEffect();
            if (needToPayCost) {
                PayCost(false);
            }
            return true;
        }
        
        /// <summary>
        /// Actually do the thing this ability is supposed to do
        /// </summary>
        /// <returns>True if the cost should be payed (because the ability resulted in some action that demands a cost), otherwise false</returns>
        public abstract bool DoAbilityEffect();
    }
}