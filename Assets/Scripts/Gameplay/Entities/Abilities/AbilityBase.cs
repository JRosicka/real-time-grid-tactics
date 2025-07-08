using Gameplay.Config.Abilities;
using Gameplay.Managers;
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

        protected AbilityAssignmentManager AbilityAssignmentManager => GameManager.Instance.AbilityAssignmentManager;

        protected AbilityBase(T data, IAbilityParameters abilityParameters, GridEntity performer) {
            Data = data;
            BaseParameters = abilityParameters;
            Performer = performer;
        }

        public abstract AbilityExecutionType ExecutionType { get; }
        public virtual float CooldownDuration => Data.CooldownDuration;

        public bool CompleteCooldown() {
            if (!CompleteCooldownImpl()) {
                return false;
            }
            
            if (Data.RepeatWhenCooldownFinishes) {
                AbilityAssignmentManager.PerformAbility(Performer, AbilityData, BaseParameters, false, true, false);
            }

            return true;
        }

        public abstract bool ShouldShowCooldownTimer { get; }

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
        /// TODO-abilities this is a mess, mostly because of special handling for the build ability which needs the specific cost to be paid right when the ability is queued. Would be good to:
        /// - Have this special resource cost just be paid as part of a new PayUpFrontCost that triggers the instant the ability gets performed (even if the ability does not finish being performed due to the actual build not happening yet)
        /// - Rename this to CreateAbilityTimer
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
                AbilityAssignmentManager.AddMovementTime(Performer, Data.AddedMovementTime);
            }
        }

        protected abstract void PayCostImpl();

        public AbilityResult PerformAbility() {
            (bool legal, AbilityResult? result) = Data.AbilityLegal(BaseParameters, Performer, false);
            if (!legal) return result.Value;
            if (!Data.PayCostUpFront && !Data.CanPayCost(BaseParameters, Performer)) return AbilityResult.Failed;

            (bool needToPayCost, AbilityResult result2) = DoAbilityEffect();
            if (needToPayCost) {
                PayCost(false);
            }
            return result2;
        }
        
        /// <summary>
        /// Actually do the thing this ability is supposed to do
        /// </summary>
        /// <returns>
        /// - True if the cost should be payed (because the ability resulted in some action that demands a cost), otherwise false
        /// - The result of trying to perform the ability effect
        /// </returns>
        /// TODO-abilities re-assess, maybe we only need to return the AbilityResult
        protected abstract (bool, AbilityResult) DoAbilityEffect();
    }
}