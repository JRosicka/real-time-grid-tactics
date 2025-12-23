using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// Controls binary display for the main game UI overlay
    /// </summary>
    public class GameUI : MonoBehaviour {
        [SerializeField] private GameObject _gameUI;
        
        public void Initialize(bool display) {
            _gameUI.SetActive(display);
        }
    }
}