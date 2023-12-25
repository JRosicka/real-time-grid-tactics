using System;
using System.Collections.Generic;
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
        
        public List<AbilityChannel> ChannelBlockers => Ability.AbilityData.ChannelBlockers;
        public readonly IAbility Ability;
        
        public float TimeRemaining01 {
            get {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (_initialTimeRemaining == 0f) {
                    return 0;
                }
                return _timeRemaining / _initialTimeRemaining;
            }
        }
        
        public bool Expired;
        public event Action ExpiredEvent;
        
        private float _timeRemaining;
        private float _initialTimeRemaining;
        private bool _markedCompletedLocally;

        public AbilityCooldownTimer(IAbility ability, float overrideCooldownDuration) {
            Ability = ability;
            _timeRemaining = _initialTimeRemaining = overrideCooldownDuration > 0 ? overrideCooldownDuration : ability.CooldownDuration;
        }

        public void UpdateTimer(float deltaTime) {
            if (_markedCompletedLocally) return;
            
            _timeRemaining = Mathf.Max(_timeRemaining - deltaTime, 0);
            if (_timeRemaining <= 0) {
                HandleTimerCompleted();
            }
        }

        public void AddTime(float timeToAdd) {
            _timeRemaining += timeToAdd;
            if (_timeRemaining > _initialTimeRemaining) {
                _initialTimeRemaining = _timeRemaining;
            }
        }

        private void HandleTimerCompleted() {
            _markedCompletedLocally = true;
            if (!NetworkClient.active) {
                // SP
                Debug.Log("Ability timer completed, DING");
                TryCompleteAbilityCooldownAsync().FireAndForget();
            } else if (NetworkServer.active) {
                // MP and we are the server
                Debug.Log("Server: Ability timer completed, DING");
                TryCompleteAbilityCooldownAsync().FireAndForget();
            } else {
                // MP and we are a client. Only handle client-specific stuff. 
                Debug.Log("Client: Ability timer completed, DING");
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
        public void Expire() {
            _timeRemaining = 0f;
            Expired = true;
            ExpiredEvent?.Invoke();
        }
    }
}