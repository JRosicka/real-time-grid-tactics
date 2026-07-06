using System.Collections.Generic;
using Gameplay.Config.Abilities;
using UnityEngine;
using Util;

namespace Gameplay.Entities {
    /// <summary>
    /// Handles playing an animation for a structure being disabled due to recently taking damage
    /// </summary>
    public class DisablingDamageAnimationBehavior : MonoBehaviour {
        public ColorTintBehaviour ColorTintBehaviour;
        public List<ParticleSystem> DamagedParticles;

        private bool _damagedAnimationActive;
        private float _damageAnimationTimeRemaining;

        private GridEntity _entity;
        private Color _teamColor;
        private float _cooldownSecondsFromBeingAttacked;

        public void Initialize(GridEntity entity, Color teamColor) {
            _entity = entity;
            _teamColor = teamColor;
            _cooldownSecondsFromBeingAttacked = _entity.GetAbilityData<IncomeAbilityData>()!.CooldownSecondsFromBeingAttacked;
        }

        public void HandleDamageReceived() {
            _damageAnimationTimeRemaining = _cooldownSecondsFromBeingAttacked;
            if (!_damagedAnimationActive) {
                _damagedAnimationActive = true;
                PlayDamagedAnimation();
            }
        }
        
        private void PlayDamagedAnimation() {
            ColorTintBehaviour.ApplyTint(new List<Color>{ Color.white, _teamColor });
            DamagedParticles.ForEach(p => p.Play());
        }

        private void StopDamagedAnimation() {
            ColorTintBehaviour.Reset();
            DamagedParticles.ForEach(p => p.Stop());
        }

        private void Update() {
            if (!_damagedAnimationActive) return;

            _damageAnimationTimeRemaining -= Time.deltaTime;
            if (_damageAnimationTimeRemaining <= 0) {
                _damageAnimationTimeRemaining = 0;
                _damagedAnimationActive = false;
                StopDamagedAnimation();
            }
        }
    }
}