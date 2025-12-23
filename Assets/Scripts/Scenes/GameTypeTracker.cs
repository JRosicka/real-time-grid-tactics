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
        /// <summary>
        /// The ID of the map to use
        /// </summary>
        public string MapID { get; private set; }
        private static string _mapIDFromEditorWindow;
        
        public void Initialize() {
            Instance = this;
#if UNITY_EDITOR
            MapID = string.IsNullOrEmpty(_mapIDFromEditorWindow) ? "origins" : _mapIDFromEditorWindow;
            SetMap(MapID);
#endif
        }

        public void SetGameType(bool networked, bool allowInput, bool runGame) {
            GameIsNetworked = networked;
            AllowInput = allowInput;
            RunGame = runGame;
        }
        
        public void SetMap(string mapID) {
            MapID = mapID;
        }
        
#if UNITY_EDITOR
        public static void SetMapIDFromEditorWindow(string mapID) {
            _mapIDFromEditorWindow = mapID;
        }
#endif

    }
}