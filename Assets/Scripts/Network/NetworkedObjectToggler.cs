using Mirror;
using Scenes;
using UnityEngine;

namespace Game.Network {
    /// <summary>
    /// Handles disabling <see cref="NetworkBehaviour"/> objects active in the game scene when we are not actually in
    /// a MP match
    /// </summary>
    public class NetworkedObjectToggler : MonoBehaviour {
        private void Awake() {
            if (!GameTypeTracker.Instance.GameIsNetworked) {
                Destroy(gameObject);
            }
        }
    }
}