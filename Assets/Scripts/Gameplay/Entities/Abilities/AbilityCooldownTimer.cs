using System.Collections.Generic;
using Gameplay.Config.Abilities;
using Mirror;
using UnityEngine;

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
        public List<AbilityChannel> ChannelBlockers => Ability.AbilityData.ChannelBlockers;
        public readonly IAbility Ability;
        
        public float TimeRemaining01 => _timeRemaining / _initialTimeRemaining;
        public bool Expired;
        
        private float _timeRemaining;
        private float _initialTimeRemaining;

        public AbilityCooldownTimer(IAbility ability) {
            Ability = ability;
            _timeRemaining = _initialTimeRemaining = ability.AbilityData.CooldownDuration;
        }

        public void UpdateTimer(float deltaTime) {
            if (_timeRemaining <= 0f) return;
            
            _timeRemaining = Mathf.Max(_timeRemaining - deltaTime, 0);
            if (_timeRemaining <= 0) {
                HandleTimerCompleted();
            }
        }

        private void HandleTimerCompleted() {
            if (!NetworkClient.active) {
                // SP
                Debug.Log("Ability timer completed, DING");
                Ability.CompleteCooldown();
                GameManager.Instance.CommandManager.MarkAbilityCooldownExpired(Ability);
            } else if (NetworkServer.active) {
                // MP and we are the server
                Debug.Log("Server: Ability timer completed, DING");
                Ability.CompleteCooldown();
                GameManager.Instance.CommandManager.MarkAbilityCooldownExpired(Ability);
            } else {
                // MP and we are a client. Only handle client-specific stuff. 
                Debug.Log("Client: Ability timer completed, DING");
            }
        }
        
        /// <summary>
        /// We just received word that the timer has expired on the server, so we should mark it as completed here regardless
        /// of whatever state it's at on our end. 
        /// </summary>
        public void Expire() {
            _timeRemaining = 0f;
            Expired = true;
        }
    }
}