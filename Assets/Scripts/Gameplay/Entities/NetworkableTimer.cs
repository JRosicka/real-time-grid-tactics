using System;
using System.Threading.Tasks;
using Mirror;
using Scenes;
using UnityEngine;
using Util;

namespace Gameplay.Entities {
    /// <summary>
    /// A psuedo-networkable timer for synchronizing the tracking of something across all clients.
    ///
    /// Most of the functionality here is not actually networked in order to avoid networking timer state every frame.
    /// Each client handles its own timers instead of being given updates by the server. Only the expiration and list
    /// adding/removal are networked.
    /// </summary>
    public abstract class NetworkableTimer {
        public readonly GameTeam Team;

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
        // Bool parameter indicates whether the associated action (ability, upgrade, etc) was canceled
        public event Action<bool> ExpiredEvent;
        
        public float TimeRemaining { get; protected set; }
        public float InitialTimeRemaining { get; protected set; }
        private bool _markedCompletedLocally;
        
        protected abstract Task TryCompleteTimerAsync();

        protected NetworkableTimer(GameTeam team, float timeRemaining) {
            Team = team;
            TimeRemaining = InitialTimeRemaining = timeRemaining;
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
            if (!GameTypeTracker.Instance.GameIsNetworked) {
                // SP
                TryCompleteTimerAsync().FireAndForget();
            } else if (GameTypeTracker.Instance.HostForNetworkedGame) {
                // MP and we are the server
                TryCompleteTimerAsync().FireAndForget();
            } // else MP and we are a client. Only handle client-specific stuff. 
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