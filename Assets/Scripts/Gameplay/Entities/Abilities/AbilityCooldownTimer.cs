using System;
using System.Collections.Generic;
using Gameplay.Config.Abilities;
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
        public event Action<AbilityCooldownTimer> CompletedEvent;
        public List<AbilityChannel> ChannelBlockers => Ability.AbilityData.ChannelBlockers;
        public readonly IAbility Ability;
        
        public float TimeRemaining01 => _timeRemaining / _initialTimeRemaining;
        public bool Expired => _timeRemaining <= 0f;
        
        private float _timeRemaining;
        private float _initialTimeRemaining;

        public AbilityCooldownTimer(IAbility ability) {
            Ability = ability;
            _timeRemaining = _initialTimeRemaining = ability.AbilityData.CooldownDuration;
        }

        public void UpdateTimer(float deltaTime) {
            if (Expired) return;
            
            _timeRemaining = Mathf.Max(_timeRemaining - deltaTime, 0);
            if (_timeRemaining <= 0) {
                Debug.Log("Ability timer completed, DING");
                Ability.CompleteCooldown();
                CompletedEvent?.Invoke(this);
            }
        }

        public void Expire() {
            _timeRemaining = 0f;
        }
    }
}