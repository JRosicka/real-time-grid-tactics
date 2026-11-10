using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// Handles displaying text alerts to the local user
    /// </summary>
    public class AlertTextDisplayer : MonoBehaviour {
        [SerializeField] private AlertTextEntry _entryPrefab;
        [SerializeField] private float _secondsToWaitBeforeFade;
        [SerializeField] private float _fadeTimeSeconds;
        [SerializeField] private float _maxEntries;

        private readonly List<AlertTextEntry> _alertEntries = new List<AlertTextEntry>();
        
        /// <summary>
        /// String key: id
        /// int value: expiration time in-game (seconds)
        /// </summary>
        private readonly Dictionary<string, float> _alertsOnCooldown = new Dictionary<string, float>();

        public void DisplayAlert(string text) {
            AlertTextEntry entry = Instantiate(_entryPrefab, transform);
            entry.Initialize(text, _secondsToWaitBeforeFade, _fadeTimeSeconds, EntryDestroyed);
            _alertEntries.Add(entry);
            if (_alertEntries.Count > _maxEntries) {
                AlertTextEntry entryToDestroy = _alertEntries[0];
                entryToDestroy.DestroyEntry();
            }
        }

        public void DisplayAlertWithCooldown(string id, int cooldownSeconds, string message) {
            float matchLength = GameManager.Instance.CommandManager.AbilityExecutor.MatchLength;
            if (_alertsOnCooldown.TryGetValue(id, out var expiration)) {
                // See if cooldown has expired
                if (expiration > matchLength) {
                    return;
                }
            }
            
            _alertsOnCooldown[id] = matchLength + cooldownSeconds;
            DisplayAlert(message);
        }

        private void EntryDestroyed(AlertTextEntry entry) {
            _alertEntries.Remove(entry);
        }

        [Button]
        private void DisplayTestAlert() {
            DisplayAlert("Test alert message that is kinda sorta long");
        }
    }
}