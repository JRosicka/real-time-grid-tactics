using Gameplay.Entities.Abilities;
using Gameplay.UI;
using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// Visualizes an <see cref="AbilityCooldownTimer"/>
    /// </summary>
    public class AbilityTimerCooldownView : MonoBehaviour {
        public AbilityTimerFill AbilityTimerFill;
        public Canvas Canvas;
        
        private AbilityCooldownTimer _cooldownTimer;
        private bool _destroyWhenExpired;
        private bool _waitForServerEvent;

        // TODO: Hmm, I think I would always want waitForServerEvent to be true? As long as it doesn't look janky locally with timers appearing/disappearing on clients. 
        public void Initialize(AbilityCooldownTimer cooldownTimer, bool destroyWhenExpired, bool waitForServerEvent, AbilityTimerFill fillOverride = null) {
            if (Canvas != null) {
                Canvas.overrideSorting = true;
                Canvas.sortingOrder = CanvasSortingOrderMap.TimerView;
            }
            gameObject.SetActive(true);
            _cooldownTimer = cooldownTimer;
            _destroyWhenExpired = destroyWhenExpired;
            _waitForServerEvent = waitForServerEvent;
            if (fillOverride != null) {
                AbilityTimerFill = fillOverride;
            }

            if (_waitForServerEvent) {
                _cooldownTimer.ExpiredEvent += OnTimerExpired;
            }
            UpdateFillAmount();
        }

        public void UnsubscribeFromTimers() {
            if (_cooldownTimer != null) {
                _cooldownTimer.ExpiredEvent -= OnTimerExpired;
            }
        }

        private void Update() {
            UpdateFillAmount();
        }

        private void UpdateFillAmount() {
            if (_cooldownTimer == null) return;
            
            float remaining = _cooldownTimer.TimeRemaining01;
            AbilityTimerFill.UpdateFillAmount01(remaining);

            if (remaining <= 0 && !_waitForServerEvent) {
                // The timer elapsed locally and we don't care about the server timer, so complete now
                HandleTimerCompleted();
            }
        }

        private void HandleTimerCompleted() {
            if (_destroyWhenExpired) {
                Destroy(gameObject);
            } else {
                gameObject.SetActive(false);
                _cooldownTimer = null;
            }
        }

        /// <summary>
        /// The timer has been expired on the server, so handle completion locally
        /// </summary>
        private void OnTimerExpired(bool canceled) {
            if (_cooldownTimer != null) {
                _cooldownTimer.ExpiredEvent -= OnTimerExpired;
            }
            HandleTimerCompleted();
        }

        private void OnDestroy() {
            UnsubscribeFromTimers();
        }
    }
}