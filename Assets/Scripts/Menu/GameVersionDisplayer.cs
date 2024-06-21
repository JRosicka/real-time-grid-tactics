using TMPro;
using UnityEngine;

namespace Menu {
    /// <summary>
    /// Handles displaying the game version
    /// </summary>
    public class GameVersionDisplayer : MonoBehaviour {
        [SerializeField] private string _versionFormat = "AmberForge alpha v{0}";
        [SerializeField] private TMP_Text _versionText;
        private void Start() {
            _versionText.text = string.Format(_versionFormat, Application.version);
        }
    }
}