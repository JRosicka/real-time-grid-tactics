using System;
using UnityEngine;
using Util;

namespace Gameplay.UI {
    /// <summary>
    /// Individual setting in the settings menu
    /// </summary>
    public class SettingEntry : MonoBehaviour {
        [Header("References")]
        [SerializeField] private ListenerButton _listenerButton;

        [Header("Config")]
        [SerializeField] [TextArea] private string _description;

        public Action<SettingEntry> Entered;

        public string Description => _description;
        public float HeightWorldPosition => transform.position.y;
        
        public void Initialize() {
            if (_listenerButton) {
                _listenerButton.Entered += OnButtonEntered;
            }
        }

        private void OnButtonEntered() {
            Entered?.Invoke(this);
        }
    }
}