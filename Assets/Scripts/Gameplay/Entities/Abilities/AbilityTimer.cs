using System;
using System.Collections.Generic;
using Gameplay.Config.Abilities;
using UnityEngine;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// Represents a timer for blocking particular <see cref="AbilityChannel"/> cooldowns - can be checked to see if
    /// <see cref="IAbility"/>s of those same channel are allowed to be used
    /// </summary>
    public class AbilityTimer {
        public event Action<AbilityTimer> CompletedEvent;
        public List<AbilityChannel> ChannelBlockers => Ability.AbilityData.ChannelBlockers;
        public readonly IAbility Ability;
        
        public float TimeRemaining01 => _timeRemaining / _initialTimeRemaining;
        public bool Expired => _timeRemaining <= 0f;
        
        private float _timeRemaining;
        private float _initialTimeRemaining;

        public AbilityTimer(IAbility ability) {
            Ability = ability;
            _timeRemaining = _initialTimeRemaining = ability.AbilityData.CooldownDuration;
        }

        public void UpdateTimer(float deltaTime) {
            if (Expired) return;
            
            _timeRemaining = Mathf.Max(_timeRemaining - deltaTime, 0);
            if (_timeRemaining <= 0) {
                Debug.Log("Ability timer completed, DING");
                CompletedEvent?.Invoke(this);
            }
        }

        public void Expire() {
            _timeRemaining = 0f;
        }
    }
}