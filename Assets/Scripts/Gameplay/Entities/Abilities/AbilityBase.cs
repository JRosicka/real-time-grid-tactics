using System;
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
        /// </summary>
        private void CreateAbilityTimer() {
            Performer.CreateAbilityTimer(this);
            if (Data.AddedMovementTime > 0) {
                AbilityAssignmentManager.AddMovementTime(Performer, Data.AddedMovementTime);
            }
        }

        public abstract bool TryPayUpFrontCost();

        public AbilityResult PerformAbility() {
            AbilityLegality legality = Data.AbilityLegal(BaseParameters, Performer, false);
            switch (legality) {
                case AbilityLegality.Legal:
                    // Continue on to try to perform the ability
                    break;
                case AbilityLegality.NotCurrentlyLegal:
                    return AbilityResult.IncompleteWithoutEffect;
                case AbilityLegality.IndefinitelyIllegal:
                    return AbilityResult.Failed;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            (bool needToCreateAbilityTimer, AbilityResult result2) = DoAbilityEffect();
            if (needToCreateAbilityTimer) {
                CreateAbilityTimer();
            }
            return result2;
        }
        
        /// <summary>
        /// Actually do the thing this ability is supposed to do
        /// </summary>
        /// <returns>
        /// - True if the ability should go on cooldown, otherwise false
        /// - The result of trying to perform the ability effect
        /// </returns>
        protected abstract (bool, AbilityResult) DoAbilityEffect();
    }
}