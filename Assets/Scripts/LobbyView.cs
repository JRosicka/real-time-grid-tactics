using Game.Network;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class LobbyView : MonoBehaviour {
    public Button HostButton;
    public Button LobbySearchButton;
    public Button JoinByIDButton;

    public Button CancelButton;
    public Button StopHostButton;

    public GameNetworkManager NetworkManager;
    public SteamLobby SteamLobby;
    void Start() {
        ShowDefaultButtons(true);
        StopHostButton.gameObject.SetActive(false);
    }

    private void ShowDefaultButtons(bool show) {
        HostButton.gameObject.SetActive(show);
        LobbySearchButton.gameObject.SetActive(show);
        JoinByIDButton.gameObject.SetActive(show);
    }

    public void OnStartHostClicked() {
        SteamLobby.HostLobby();

        ShowDefaultButtons(false);
        StopHostButton.gameObject.SetActive(true);
    }

    public void OnStopHostClicked() {
        SteamLobby.ExitLobby();
        
        ShowDefaultButtons(true);
        StopHostButton.gameObject.SetActive(false);
    }

    public void OnSearchForLobbiesClicked() {
        ShowDefaultButtons(false);
        CancelButton.gameObject.SetActive(true);
        
        // TODO display list
    }

    public void OnJoinByIDButtonClicked() {
        ShowDefaultButtons(false);
        CancelButton.gameObject.SetActive(true);
        
        // TODO UI and logic for entering ID and password and connecting
    }

    public void OnCancelClicked() {
        ShowDefaultButtons(true);
        CancelButton.gameObject.SetActive(false);
        
        // TODO cancel logic
        SteamLobby.ExitLobby();

    }
}
