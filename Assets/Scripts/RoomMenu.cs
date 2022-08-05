using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Network;
using kcp2k;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomMenu : MonoBehaviour {
    public PlayerSlot PlayerSlot1;
    public PlayerSlot PlayerSlot2;
    public Button StartButton;
    public Button ToggleReadyButton;
    public TMP_Text ToggleReadyButtonText;
    public TMP_Text JoinCodeText;

    public Animator CopiedToClipboardAnimator;
    
    private GameNetworkManager _gameNetworkManager;
    private SteamLobbyService steamLobbyService => SteamLobbyService.Instance;
    private string _joinCode;
    private GameNetworkPlayer _cachedGameNetworkPlayer;
    private GameNetworkPlayer _localPlayer {
        get {
            if (_cachedGameNetworkPlayer == null) {
                _cachedGameNetworkPlayer = FindObjectsOfType<GameNetworkPlayer>().First(player => player.isLocalPlayer);
            }

            return _cachedGameNetworkPlayer;
        }
    }
    
    // Do not cache because other players can leave/join later
    private GameNetworkPlayer _opponentPlayer {
        get {
            return FindObjectsOfType<GameNetworkPlayer>().First(player => !player.isLocalPlayer);
        }
    }

    private PlayerSlot localPlayerSlot => PlayerSlot1.AssignedPlayer.isLocalPlayer ? PlayerSlot1 : PlayerSlot2;
    private PlayerSlot opponentPlayerSlot => PlayerSlot1.AssignedPlayer.isLocalPlayer ? PlayerSlot2 : PlayerSlot1;

    private PlayerSlot GetSlotForPlayer(GameNetworkPlayer player) {
        if (PlayerSlot1.AssignedPlayer == player) {
            return PlayerSlot1;
        }

        if (PlayerSlot2.AssignedPlayer == player) {
            return PlayerSlot2;
        }

        return null;
    }

    void Start() {
        StartButton.gameObject.SetActive(false);
        _gameNetworkManager = FindObjectOfType<GameNetworkManager>();
        _gameNetworkManager.RoomServerPlayersReadyAction += ShowStartButton;
        _gameNetworkManager.RoomServerPlayersNotReadyAction += HideStartButton;
        _gameNetworkManager.RoomServerSceneChangedAction += UpdateLobbyOpenStatus;
        _gameNetworkManager.RoomClientSceneChangedAction += AddUnassignedPlayers;
        GameNetworkPlayer.PlayerSteamInfoDetermined += AddUnassignedPlayers;
        GameNetworkPlayer.PlayerExitedRoom += UnassignPlayer;
        GameNetworkPlayer.PlayerExitedRoom += ResetReadyButton;

        SteamLobbyService.Lobby lobby =
            SteamLobbyService.Instance.GetLobbyData(SteamLobbyService.Instance.CurrentLobbyID, null);
        _joinCode = lobby[SteamLobbyService.LobbyUIDKey];
        JoinCodeText.text = _joinCode;
        // steamLobbyService.OnCurrentLobbyMetadataChanged += AddUnassignedPlayers;    // TODO do we listen to this, or maybe to one of the GameNetworkPlayer methods, or maybe to the GameNetworkManager updatelobby event.
    }
    
    private void OnDestroy() {
        GameNetworkPlayer.PlayerSteamInfoDetermined -= AddUnassignedPlayers;
        GameNetworkPlayer.PlayerExitedRoom -= UnassignPlayer;
        GameNetworkPlayer.PlayerExitedRoom -= ResetReadyButton;
        if (_gameNetworkManager != null) {
            _gameNetworkManager.RoomServerPlayersReadyAction -= ShowStartButton;
            _gameNetworkManager.RoomServerPlayersNotReadyAction -= HideStartButton;
            _gameNetworkManager.RoomServerSceneChangedAction -= UpdateLobbyOpenStatus;
            _gameNetworkManager.RoomClientSceneChangedAction -= AddUnassignedPlayers;
        }
    }

    public void SetRandomMetadata() {
        steamLobbyService.UpdateCurrentLobbyMetadata("dummyData", "69");
    }

    public void SetRandomPlayerMetadata() {
        steamLobbyService.UpdateCurrentLobbyPlayerMetadata("color", "very red");
    }

    public void ExitRoom() {
        if (NetworkServer.active) {
            // We're the host, so stop the whole server
            _gameNetworkManager.StopHost();
        } else {
            // We're just a little baby client, so just stop the client
            _gameNetworkManager.StopClient();
        }
    }

    public void CopyJoinCode() {
        GUIUtility.systemCopyBuffer = _joinCode;
        CopiedToClipboardAnimator.Play("Copy Join Code");
    }

    private void ShowStartButton() {
        StartButton.gameObject.SetActive(true);
    }

    private void HideStartButton() {
        StartButton.gameObject.SetActive(false);
    }

    private void AddUnassignedPlayers() {
        // List<GameNetworkPlayer> players = _gameNetworkManager.roomSlots.ConvertAll(player => (GameNetworkPlayer)player);
        List<GameNetworkPlayer> players = FindObjectsOfType<GameNetworkPlayer>().ToList();
        
        // Assign any unassigned players
        bool isHosting = _gameNetworkManager.IsHosting();
        foreach (GameNetworkPlayer player in players) {
            if (PlayerSlot1.AssignedPlayer != player && PlayerSlot2.AssignedPlayer != player) {
                if (PlayerSlot1.AssignedPlayer == null) {
                    bool kickable = !player.isLocalPlayer && isHosting;
                    PlayerSlot1.AssignPlayer(player, kickable);
                } else if (PlayerSlot2.AssignedPlayer == null) {
                    bool kickable = !player.isLocalPlayer && isHosting;
                    PlayerSlot2.AssignPlayer(player, kickable);
                } else {
                    Log.Error("A new player joined, but we don't have any slots for them!");
                }
            }
        }

        // TODO: If players can update their info for their slots, do so here
    }

    private void UnassignPlayer() {
        List<GameNetworkPlayer> players = _gameNetworkManager.roomSlots.ConvertAll(player => (GameNetworkPlayer)player);

        // Unassign any players who have disconnected
        if (PlayerSlot1.AssignedPlayer != null && !players.Contains(PlayerSlot1.AssignedPlayer)) {
            PlayerSlot1.UnassignPlayer();
        }
        if (PlayerSlot2.AssignedPlayer != null && !players.Contains(PlayerSlot2.AssignedPlayer)) {
            PlayerSlot2.UnassignPlayer();
        }
    }

    public void ToggleReady() {
        if (_localPlayer.readyToBegin) {
            ToggleReadyButtonText.text = "Ready";
            _localPlayer.CmdChangeReadyState(false);
        } else {
            ToggleReadyButtonText.text = "Cancel";
            _localPlayer.CmdChangeReadyState(true);
        }
    }

    /// <summary>
    /// If the ready state changes from some way other than clicking the button, such as when another player leaves the
    /// room, then call this to reset the text on the ready button to reflect the actual ready state
    /// </summary>
    private void ResetReadyButton() {
        if (_localPlayer.readyToBegin) {
            ToggleReadyButtonText.text = "Cancel";
        } else {
            ToggleReadyButtonText.text = "Ready";
        }
    }
    
    /// <summary>
    /// Check to see if we should open/close the lobby. If we just came back to the room scene, then we should open it.
    /// If we just came to the GamePlay scene, then we should close it.
    /// This should be done only on the server. 
    /// </summary>
    private void UpdateLobbyOpenStatus() {
        bool isInGameScene = NetworkManager.IsSceneActive(_gameNetworkManager.GameplayScene);
        steamLobbyService.UpdateCurrentLobbyMetadata(SteamLobbyService.LobbyGameActiveKey, isInGameScene.ToString());
    }

    public void StartGame() {
        _gameNetworkManager.ServerChangeScene(_gameNetworkManager.GameplayScene);
    }
}
