using Mirror;
using UnityEngine;

namespace Scenes {
    /// <summary>
    /// Tracks whether the current game scene is networked
    /// </summary>
    public class GameNetworkStateTracker : MonoBehaviour {
        public static GameNetworkStateTracker Instance { get; private set; }
        [HideInInspector] public bool GameIsNetworked;
        public bool HostForNetworkedGame => GameIsNetworked && NetworkServer.active;
        
        public void Initialize() {
            Instance = this;
        }
    }
}