using Game.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyView : MonoBehaviour {
    public Button HostButton;
    public Button LobbySearchButton;
    public Button JoinByIDButton;

    public Button CancelButton;
    public Button StopHostButton;

    public TMP_Text LobbyStatusText;

    public GameNetworkManager NetworkManager;
    public SteamLobbyService SteamLobbyService;
    void Start() {
        ShowDefaultButtons(true);
        StopHostButton.gameObject.SetActive(false);
        CancelButton.gameObject.SetActive(false);
    }

    private void ShowDefaultButtons(bool show) {
        HostButton.gameObject.SetActive(show);
        LobbySearchButton.gameObject.SetActive(show);
        JoinByIDButton.gameObject.SetActive(show);
    }

    #region Buttons
    
    public void OnStartHostClicked() {
        SteamLobbyService.HostLobby();

        ShowDefaultButtons(false);
        StopHostButton.gameObject.SetActive(true);
    }

    public void OnStopHostClicked() {
        SteamLobbyService.ExitLobby();
        
        ShowDefaultButtons(true);
        StopHostButton.gameObject.SetActive(false);
    }

    public void OnSearchForLobbiesClicked() {
        ShowDefaultButtons(false);
        CancelButton.gameObject.SetActive(true);
        
        SteamLobbyService.GetAllOpenLobbies();
        // TODO display list
    }

    public void OnJoinByIDButtonClicked() {
        ShowDefaultButtons(false);
        CancelButton.gameObject.SetActive(true);
        
        SteamLobbyService.DirectJoinLobby("", lobby => {
            // TODO UI and logic for entering ID and password and connecting

            SteamLobbyService.JoinLobby(lobby.SteamID);
        });
    }

    public void OnCancelClicked() {
        ShowDefaultButtons(true);
        CancelButton.gameObject.SetActive(false);
        
        // TODO cancel logic
        SteamLobbyService.ExitLobby();

    }
    
    #endregion

    private void UpdateLobbyStatus(string message) {
        LobbyStatusText.gameObject.SetActive(true);
        LobbyStatusText.text = message;
    }

    private void DisableLobbyStatus() {
        LobbyStatusText.gameObject.SetActive(false);
    }
}
