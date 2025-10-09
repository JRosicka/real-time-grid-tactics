using System;
using System.Threading.Tasks;
using Gameplay.Config.Abilities;
using Mirror;
using UnityEngine;
using Util;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// Represents a timer for blocking particular <see cref="AbilityChannel"/> cooldowns - can be checked to see if
    /// <see cref="IAbility"/>s of those same channel are allowed to be used
    ///
    /// NOTE: I wanted to avoid needing to network the state of every timer on every frame, so each client handles its own
    /// timers instead of being given updates by the server. Only the expiration and list adding/removal are networked.
    /// This may need to be changed if ability timers are too out of sync across different clients. 
    /// </summary>
    public class AbilityCooldownTimer {
        /// <summary>
        /// The amount of time to wait between attempts to complete the ability cooldown after the timer elapses
        /// </summary>
        private const int CooldownCheckMillis = 200;
        
        public readonly IAbility Ability;
        private readonly GameTeam _team;
        
        public float TimeRemaining01 {
            get {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (InitialTimeRemaining == 0f) {
                    return 0;
                }
                return TimeRemaining / InitialTimeRemaining;
            }
        }
        
        public bool Expired;
        // Bool parameter indicates whether the ability was canceled
        public event Action<bool> ExpiredEvent;
        
        public float TimeRemaining { get; private set; }
        public float InitialTimeRemaining { get; private set; }
        private bool _markedCompletedLocally;

        public AbilityCooldownTimer(IAbility ability, float overrideCooldownDuration) {
            Ability = ability;
            _team = ability.Performer.Team;
            TimeRemaining = InitialTimeRemaining = overrideCooldownDuration > 0 ? overrideCooldownDuration : ability.CooldownDuration;
        }
        
        public bool DoesBlockChannelForTeam(AbilityChannel channel, GameTeam team) {
            if (team != _team) return false;
            if (Expired) return false;
            if (!Ability.AbilityData.ChannelBlockers.Contains(channel)) return false;
            
            return true;
        }

        public void UpdateTimer(float deltaTime) {
            if (_markedCompletedLocally) return;
            
            TimeRemaining = Mathf.Max(TimeRemaining - deltaTime, 0);
            if (TimeRemaining <= 0) {
                HandleTimerCompleted();
            }
        }

        public void AddTime(float timeToAdd) {
            TimeRemaining += timeToAdd;
            if (TimeRemaining > InitialTimeRemaining) {
                InitialTimeRemaining = TimeRemaining;
            }
        }

        private void HandleTimerCompleted() {
            _markedCompletedLocally = true;
            if (!NetworkClient.active) {
                // SP
                TryCompleteAbilityCooldownAsync().FireAndForget();
            } else if (NetworkServer.active) {
                // MP and we are the server
                TryCompleteAbilityCooldownAsync().FireAndForget();
            } else {
                // MP and we are a client. Only handle client-specific stuff. 
            }
        }

        // TODO: I probably should use a different name than cooldown since there are abilities that do their specific actions after the ability time elapses. Kinda a misnomer to call that a cooldown. 
        private async Task TryCompleteAbilityCooldownAsync() {
            await AsyncUtil.WaitUntilOnCallerThread(Ability.CompleteCooldown, CooldownCheckMillis);
            GameManager.Instance.CommandManager.MarkAbilityCooldownExpired(Ability);
        }
        
        /// <summary>
        /// We just received word that the timer has expired on the server, so we should mark it as completed here regardless
        /// of whatever state it's at on our end. 
        /// </summary>
        public void Expire(bool canceled) {
            TimeRemaining = 0f;
            Expired = true;
            ExpiredEvent?.Invoke(canceled);
        }
    }
}