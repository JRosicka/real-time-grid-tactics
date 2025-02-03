using System;
using Game.Network;
using Gameplay.UI;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class MultiplayerMenu : MonoBehaviour {
    public Button HostButton;
    public GameObject LobbyTypeSelection;
    
    public Button LobbySearchButton;
    public Button JoinByIDButton;

    public Button CancelButton;
    public Button StopHostButton;
    public Button QuitButton;
    public Button SinglePlayerButton;
    
    public LobbyListMenu LobbyListMenu;
    public GameObject JoinByIDMenu;
    public TMP_InputField JoinByIDField;
    public GameObject FailureFeedbackDialog;
    public TMP_Text FailureFeedbackText;
    public GameObject SinglePlayerConfirmationDialog;
    
    public SettingsMenu SettingsMenu;
    
    public TMP_Text LobbyStatusText;

    public GameNetworkManager NetworkManager;

    public Color DisabledTextColor;
    public Color EnabledTextColor;

    private void Start() {
        SteamLobbyService.Instance.OnLobbyJoinComplete += OnLobbyJoinComplete;
        LobbyListEntry.LobbyJoinAttemptStarted += PromptJoinIDForLobby;
        
        ShowDisconnectMessageIfAppropriate();
    }

    private void OnDestroy() {
        SteamLobbyService.Instance.OnLobbyJoinComplete -= OnLobbyJoinComplete;
        LobbyListEntry.LobbyJoinAttemptStarted -= PromptJoinIDForLobby;
    }

    private void ShowDisconnectMessageIfAppropriate() {
        switch (DisconnectFeedbackService.ProcessLastDisconnect()) {
            case DisconnectFeedbackService.DisconnectReason.NotDisconnected:
                // This is the first time we reached the main menu, no disconnect. Nothing to do. 
                break;
            case DisconnectFeedbackService.DisconnectReason.Unknown:
                ShowFailureFeedback("Disconnected from game: lost connection to server.");
                break;
            case DisconnectFeedbackService.DisconnectReason.ClientDisconnect:
                // The client voluntarily left, so nothing to display here.
                break;
            case DisconnectFeedbackService.DisconnectReason.Kicked:
                ShowFailureFeedback("Disconnected from game: kicked.");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void ShowFailureFeedback(string message) {
        FailureFeedbackText.text = message;
        FailureFeedbackDialog.SetActive(true);
    }
    
    private void OnLobbyJoinComplete(bool success, string failureMessage) {
        if (!success) {
            ResetMultiplayerMenu();
            ShowFailureFeedback($"Failed to join lobby: {failureMessage}");
        } else {
            // TODO I don't think we need to do anything here since we're about to be whisked away to the room scene. 
        }
    }

    private void ResetMultiplayerMenu() {
        ToggleButton(HostButton, true);
        ToggleButton(LobbySearchButton, true);
        ToggleButton(JoinByIDButton, true);
        ToggleButton(QuitButton, true);
        ToggleButton(SinglePlayerButton, true);
        CancelButton.gameObject.SetActive(false);
        LobbyTypeSelection.gameObject.SetActive(false);
        JoinByIDMenu.gameObject.SetActive(false);
        SinglePlayerButton.gameObject.SetActive(true);
        SinglePlayerConfirmationDialog.SetActive(false);
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

        HideButtons();
    }

    public void OnHostPublicLobbyClicked() {
        SteamLobbyService.Instance.HostLobby(true);
        
        HideButtons();
        
        // TODO maybe respond to OnLobbyCreationComplete callback? Might just get whisked away to the room though. 
    }

    private void HideButtons() {
        LobbyTypeSelection.SetActive(false);
        // StopHostButton.gameObject.SetActive(true);
        HostButton.gameObject.SetActive(false);
        LobbySearchButton.gameObject.SetActive(false);
        JoinByIDButton.gameObject.SetActive(false);
        CancelButton.gameObject.SetActive(false);
        QuitButton.gameObject.SetActive(false); 
        SinglePlayerButton.gameObject.SetActive(false);
    }

    public void OnStartSinglePlayerGameClicked() {
        SceneManager.LoadScene("GamePlay");
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

    public void OnSinglePlayerButtonClicked() {
        ResetMultiplayerMenu();
        CancelButton.gameObject.SetActive(true);
        ToggleButton(SinglePlayerButton, false);

        SinglePlayerConfirmationDialog.SetActive(true);
    }

    public void OnCancelClicked() {
        ResetMultiplayerMenu();
        CancelButton.gameObject.SetActive(false);
        
        // TODO cancel logic
    }

    public void OnSettingsClicked() {
        ResetMultiplayerMenu();
        SettingsMenu.Open();
    }

    public void OnQuitClicked() {
        Application.Quit(0);
    }

    private event Action _onClickedOkayOnFailureDialog;
    public void OnClickedOkOnFailureDialog() {
        FailureFeedbackDialog.SetActive(false);
        _onClickedOkayOnFailureDialog?.Invoke();
        _onClickedOkayOnFailureDialog = null;
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
                ShowFailureFeedback($"Failed to join lobby: {failureMessage}");
                _onClickedOkayOnFailureDialog += () => JoinByIDMenu.SetActive(true);
                JoinByIDMenu.SetActive(false);
                _joinCodeForLobbyWeAreAttemptingToJoin = null;
                return;
            }

            // Tilemap map = new Tilemap();
            // TileBase tile = map.GetTile(new Vector3Int(0, 1));
            // TileData data;
            // GameObject spawnedObject = map.GetInstantiatedObject(new Vector3Int(0, 1));
            // GameplayTile gameTile = (GameplayTile)tile;
            // Grid grid;
            

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
