using Mirror;
using UnityEngine;

namespace Scenes {
    /// <summary>
    /// Tracks basic info about the current game, like whether it is networked or allows for user input
    /// </summary>
    public class GameTypeTracker : MonoBehaviour {
        public static GameTypeTracker Instance { get; private set; }
        
        /// <summary>
        /// Whether the user can interact with the game scene at all
        /// </summary>
        public bool AllowInput { get; private set; }
        /// <summary>
        /// Whether any actions should actually happen in the game
        /// </summary>
        public bool RunGame { get; private set; }
        /// <summary>
        /// Whether this is a networked (MP) game
        /// </summary>
        public bool GameIsNetworked { get; private set; }
        /// <summary>
        /// Whether the local machine is the host for the MP game
        /// </summary>
        public bool HostForNetworkedGame => GameIsNetworked && NetworkServer.active;

        private string _mapID;
        private static string _mapIDFromEditorWindow;
        /// <summary>
        /// The ID of the map to use
        /// </summary>
        public string MapID {
            get {
#if UNITY_EDITOR
                if (string.IsNullOrEmpty(_mapID)) {
                    _mapID = string.IsNullOrEmpty(_mapIDFromEditorWindow) ? "origins" : _mapIDFromEditorWindow;
                }
#else
                 if (string.IsNullOrEmpty(_mapID)) {
                    _mapID = "origins";
                }
#endif
                return _mapID;
            }
            private set {
                _mapID = value; 
            }
        }
        public string ReplayID { get; private set; }
        
        public void Initialize() {
            Instance = this;
        }

        public void SetGameType(bool networked, bool allowInput, bool runGame) {
            GameIsNetworked = networked;
            AllowInput = allowInput;
            RunGame = runGame;
        }
        
        public void SetMap(string mapID, string replayID = null) {
            MapID = mapID;
            ReplayID = replayID;
        }
        
#if UNITY_EDITOR
        public static void SetMapIDFromEditorWindow(string mapID) {
            _mapIDFromEditorWindow = mapID;
        }
#endif

    }
}