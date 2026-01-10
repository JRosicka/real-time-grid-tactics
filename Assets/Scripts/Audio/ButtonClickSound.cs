using UnityEngine;
using Util;

namespace Audio {
    /// <summary>
    /// Plays a button click sound when the attacked button is clicked
    /// </summary>
    [RequireComponent(typeof(ListenerButton))]
    public class ButtonClickSound : MonoBehaviour {
        private void Awake() {
            ListenerButton button = GetComponent<ListenerButton>();
            button.Pressed += ButtonClickDown;
            button.NoLongerPressed += ButtonClickUp;
        }

        private void ButtonClickDown() {
            GameAudio.Instance.ButtonClickDownSound();
        }
        
        private void ButtonClickUp() {
            GameAudio.Instance.ButtonClickUpSound();
        }
    }
}