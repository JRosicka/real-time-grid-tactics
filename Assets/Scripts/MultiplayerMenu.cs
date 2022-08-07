using System;
using Game.Network;
using Steamworks;
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
    public TMP_Text FailedToJoinLobbyText;

    public TMP_Text LobbyStatusText;

    public GameNetworkManager NetworkManager;

    public Color DisabledTextColor;
    public Color EnabledTextColor;

    private void Start() {
        SteamLobbyService.Instance.OnLobbyJoinComplete += OnLobbyJoinComplete;
        LobbyListEntry.LobbyJoinAttemptStarted += PromptJoinIDForLobby;
    }

    private void OnDestroy() {
        SteamLobbyService.Instance.OnLobbyJoinComplete -= OnLobbyJoinComplete;
        LobbyListEntry.LobbyJoinAttemptStarted -= PromptJoinIDForLobby;
    }
    
    private void OnLobbyJoinComplete(bool success, string failureMessage) {
        if (!success) {
            ResetMultiplayerMenu();
            FailedToJoinLobbyText.text = $"Failed to join lobby: {failureMessage}";
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
    }

    private event Action _onClickedOkayOnFailedToJoinLobbyDialog;
    public void OnClickedOkOnFailedToJoinLobbyDialog() {
        FailedToJoinLobbyDialog.SetActive(false);
        _onClickedOkayOnFailedToJoinLobbyDialog.SafeInvoke();
        _onClickedOkayOnFailedToJoinLobbyDialog = null;
    }

    private string _joinCodeForLobbyWeAreAttemptingToJoin;
    public void OnSubmitLobbyJoinIDClicked() {
        string id = JoinByIDField.text;

        // If we have a join code stored here, then we are expecting the user to input the correct join ID.
        if (_joinCodeForLobbyWeAreAttemptingToJoin != null) {
            if (id != _joinCodeForLobbyWeAreAttemptingToJoin) {
                // TODO present a "ya dun goofed, try again" toast
                Debug.Log("Incorrect lobby ID");
                return;
            }
            // Otherwise, the player put in the correct join code, so just continue and join like normal
        }
        
        SteamLobbyService.Instance.RequestLobbyByID(id, (lobby, success, failureMessage) => {
            if (!success) {
                Debug.Log("Failed to join lobby by ID");
                FailedToJoinLobbyText.text = $"Failed to join lobby: {failureMessage}";
                FailedToJoinLobbyDialog.SetActive(true);
                _onClickedOkayOnFailedToJoinLobbyDialog += () => JoinByIDMenu.SetActive(true);
                JoinByIDMenu.SetActive(false);
                _joinCodeForLobbyWeAreAttemptingToJoin = null;
                return;
            }

            SteamLobbyService.Instance.JoinLobby(lobby.SteamID);
            _joinCodeForLobbyWeAreAttemptingToJoin = null;
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

    private void PromptJoinIDForLobby(CSteamID id, string joinCode) {
        _joinCodeForLobbyWeAreAttemptingToJoin = joinCode;
        
        ResetMultiplayerMenu();
        JoinByIDMenu.gameObject.SetActive(true);
    }

    public void OnEndEditLobbyIDField() {
        // Check to see if we left by hitting the enter key    // TODO switch to use rewired once that's set up
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) {
            OnSubmitLobbyJoinIDClicked();
        }
    }
}
