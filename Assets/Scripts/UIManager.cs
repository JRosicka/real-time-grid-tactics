using Game.Network;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {
    public Button ReturnToLobbyButton;
    
    private GameNetworkManager _gameNetworkManager;

    private void Awake() {
        // Make the return to lobby button available only for the server host
        ReturnToLobbyButton.gameObject.SetActive(NetworkServer.active);
        _gameNetworkManager = FindObjectOfType<GameNetworkManager>();    // TODO better way to get this
    }

    public void ReturnToLobby() {
        _gameNetworkManager.ServerChangeScene(_gameNetworkManager.RoomScene);
    }
}
