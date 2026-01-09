using System;
using System.Collections.Generic;
using System.Linq;
using Audio;
using Game.Network;
using Gameplay.UI;
using Scenes;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MultiplayerMenu : MonoBehaviour {
    public GameObject LobbyTypeSelection;

    public Button StopHostButton;
    public GameObject MenuDialog;
    
    public LobbyListMenu LobbyListMenu;
    public GameObject JoinByIDMenu;
    public TMP_InputField JoinByIDField;
    public GameObject FailureFeedbackDialog;
    public TMP_Text FailureFeedbackText;
    public GameObject SinglePlayerConfirmationDialog;
    
    public TipsMenu TipsMenu;
    public SettingsMenu SettingsMenu;
    
    public TMP_Text LobbyStatusText;

    public GameNetworkManager NetworkManager;

    public Color DisabledTextColor;
    public Color EnabledTextColor;
    
    public CanvasWidthSetter CanvasWidthSetter;
    
    private void Start() {
        CanvasWidthSetter.Initialize();

        if (SteamLobbyService.Instance == null || !SteamLobbyService.Instance.SteamEnabled) return;
        
        SteamLobbyService.Instance.OnLobbyJoinComplete += OnLobbyJoinComplete;
        LobbyListEntry.LobbyJoinAttemptStarted += PromptJoinIDForLobby;
        
        ShowDisconnectMessageIfAppropriate();
    }

    private void OnDestroy() {
        if (SteamLobbyService.Instance == null || !SteamLobbyService.Instance.SteamEnabled) return;

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
        MenuDialog.SetActive(true);

        SinglePlayerConfirmationDialog.SetActive(false);
        JoinByIDMenu.SetActive(false);
        LobbyTypeSelection.SetActive(false);
        FailureFeedbackDialog.SetActive(false);
        LobbyStatusText.gameObject.SetActive(false);
        JoinByIDField.text = "";
        HideLobbyMenu();
    }

    #region Buttons
    
    public void OnStartHostClicked() {
        ResetMultiplayerMenu();

        LobbyTypeSelection.SetActive(true);
        
        GameAudio.Instance.ButtonClickSound();
    }

    public void OnHostPrivateLobbyClicked() {
        SteamLobbyService.Instance.HostLobby(false);

        HideButtons();
        
        GameAudio.Instance.ButtonClickSound();
    }

    public async void OnHostPublicLobbyClicked() {
        GameAudio.Instance.ButtonClickSound();
        SceneLoader.Instance.LockMapLoading();
        
        await SceneLoader.Instance.LoadLobby();
        SteamLobbyService.Instance.HostLobby(true);
    }

    private void HideButtons() {
        MenuDialog.SetActive(false);
    }

    public void OnStartSinglePlayerGameClicked() {
        GameAudio.Instance.ButtonClickSound();
        SceneLoader.Instance.LoadIntoSinglePlayerGame();
    }

    public void OnStopHostClicked() {
        SteamLobbyService.Instance.ExitLobby();
        
        ResetMultiplayerMenu();
        StopHostButton.gameObject.SetActive(false);
        
        GameAudio.Instance.ButtonClickSound();
    }

    public void OnSearchForLobbiesClicked() {
        ResetMultiplayerMenu();
        
        DisplayLobbyMenu();
        
        GameAudio.Instance.ButtonClickSound();
    }

    public void OnJoinByIDButtonClicked() {
        ResetMultiplayerMenu();
        
        JoinByIDMenu.gameObject.SetActive(true);
        
        GameAudio.Instance.ButtonClickSound();
    }

    public void OnSinglePlayerButtonClicked() {
        ResetMultiplayerMenu();

        SinglePlayerConfirmationDialog.SetActive(true);
        
        GameAudio.Instance.ButtonClickSound();
    }
    
    public void OnTipsClicked() {
        ResetMultiplayerMenu();
        HideButtons();
        TipsMenu.Open(ResetMultiplayerMenu);
        
        GameAudio.Instance.ButtonClickSound();
    }

    public void OnSettingsClicked() {
        ResetMultiplayerMenu();
        HideButtons();
        SettingsMenu.Open(ResetMultiplayerMenu);
        
        GameAudio.Instance.ButtonClickSound();
    }
    
    public void OnQuitClicked() {
        Application.Quit(0);
        
        GameAudio.Instance.ButtonClickSound();
    }

    private event Action _onClickedOkayOnFailureDialog;
    public void OnClickedOkOnFailureDialog() {
        FailureFeedbackDialog.SetActive(false);
        _onClickedOkayOnFailureDialog?.Invoke();
        _onClickedOkayOnFailureDialog = null;
        
        GameAudio.Instance.ButtonClickSound();
    }

    private string _joinCodeForLobbyWeAreAttemptingToJoin;
    public void OnSubmitLobbyJoinIDClicked() {
        GameAudio.Instance.ButtonClickSound();
        
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
