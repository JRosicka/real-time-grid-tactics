using System;
using Game.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerMenu : MonoBehaviour {
    public Button HostButton;
    public GameObject LobbyTypeSelection;
    
    public Button LobbySearchButton;
    public Button JoinByIDButton;

    public Button CancelButton;
    public Button StopHostButton;

    public LobbyListMenu LobbyListMenu;
    public GameObject JoinByIDMenu;
    public TMP_InputField JoinByIDField;
    public GameObject FailedToJoinLobbyDialog;

    public TMP_Text LobbyStatusText;

    public GameNetworkManager NetworkManager;

    public Color DisabledTextColor;
    public Color EnabledTextColor;

    private void Start() {
        SteamLobbyService.Instance.OnLobbyJoinComplete += OnLobbyJoinComplete;
    }

    private void OnDestroy() {
        SteamLobbyService.Instance.OnLobbyJoinComplete -= OnLobbyJoinComplete;
    }

    private void OnLobbyJoinComplete(bool success) {
        if (!success) {
            ResetMultiplayerMenu();
            FailedToJoinLobbyDialog.SetActive(true);
        } else {
            // TODO I don't think we need to do anything here since we're about to be whisked away to the room scene. 
        }
    }

    private void ResetMultiplayerMenu() {
        ToggleButton(HostButton, true);
        ToggleButton(LobbySearchButton, true);
        ToggleButton(JoinByIDButton, true);
        CancelButton.gameObject.SetActive(false);
        LobbyTypeSelection.gameObject.SetActive(false);
        JoinByIDMenu.gameObject.SetActive(false);
        JoinByIDField.text = "";
        HideLobbyMenu();
    }

    #region Buttons
    
    public void OnStartHostClicked() {
        ResetMultiplayerMenu();
        CancelButton.gameObject.SetActive(true);
        ToggleButton(HostButton, false);

        LobbyTypeSelection.SetActive(true);
    }

    public void OnHostPrivateLobbyClicked() {
        SteamLobbyService.Instance.HostLobby(false);

        LobbyTypeSelection.SetActive(false);
        // StopHostButton.gameObject.SetActive(true);
        HostButton.gameObject.SetActive(false);
        LobbySearchButton.gameObject.SetActive(false);
        JoinByIDButton.gameObject.SetActive(false);
    }

    public void OnHostPublicLobbyClicked() {
        SteamLobbyService.Instance.HostLobby(true);

        LobbyTypeSelection.SetActive(false);
        // StopHostButton.gameObject.SetActive(true);
        HostButton.gameObject.SetActive(false);
        LobbySearchButton.gameObject.SetActive(false);
        JoinByIDButton.gameObject.SetActive(false);
        CancelButton.gameObject.SetActive(false);
        
        // TODO maybe respond to OnLobbyCreationComplete callback? Might just get whisked away to the room though. 
    }

    public void OnStopHostClicked() {
        SteamLobbyService.Instance.ExitLobby();
        
        ResetMultiplayerMenu();
        StopHostButton.gameObject.SetActive(false);
    }

    public void OnSearchForLobbiesClicked() {
        ResetMultiplayerMenu();
        CancelButton.gameObject.SetActive(true);
        ToggleButton(LobbySearchButton, false);
        
        DisplayLobbyMenu();
    }

    public void OnJoinByIDButtonClicked() {
        ResetMultiplayerMenu();
        CancelButton.gameObject.SetActive(true);
        ToggleButton(JoinByIDButton, false);
        
        JoinByIDMenu.gameObject.SetActive(true);
    }

    public void OnCancelClicked() {
        ResetMultiplayerMenu();
        CancelButton.gameObject.SetActive(false);
        
        // TODO cancel logic
        SteamLobbyService.Instance.ExitLobby();
    }

    private event Action _onClickedOkayOnFailedToJoinLobbyDialog;
    public void OnClickedOkOnFailedToJoinLobbyDialog() {
        FailedToJoinLobbyDialog.SetActive(false);
        _onClickedOkayOnFailedToJoinLobbyDialog.SafeInvoke();
        _onClickedOkayOnFailedToJoinLobbyDialog = null;
    }
    
    public void OnSubmitLobbyJoinIDClicked() {
        string id = JoinByIDField.text;
        
        SteamLobbyService.Instance.RequestLobbyByID(id, (lobby, success) => {
            if (!success) {
                Debug.Log("Failed to join lobby by ID");
                FailedToJoinLobbyDialog.SetActive(true);
                _onClickedOkayOnFailedToJoinLobbyDialog += () => JoinByIDMenu.SetActive(true);
                JoinByIDMenu.SetActive(false);
                return;
            }

            SteamLobbyService.Instance.JoinLobby(lobby.SteamID);
        });
    }
    
    #endregion
    
    private void UpdateLobbyStatus(string message) {
        LobbyStatusText.gameObject.SetActive(true);
        LobbyStatusText.text = message;
    }

    private void DisableLobbyStatus() {
        LobbyStatusText.gameObject.SetActive(false);
    }

    private void ToggleButton(Button button, bool enableButton) {
        button.interactable = enableButton;
        button.GetComponentInChildren<TMP_Text>().color = enableButton ? EnabledTextColor : DisabledTextColor;
    }

    private void DisplayLobbyMenu() {
        // Search for lobbies
        LobbyListMenu.ShowMenu();
    }

    private void HideLobbyMenu() {
        LobbyListMenu.LeaveMenu();
    }
}
