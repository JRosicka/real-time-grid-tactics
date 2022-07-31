using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Network;
using kcp2k;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomMenu : MonoBehaviour {
    public PlayerSlot PlayerSlot1;
    public PlayerSlot PlayerSlot2;
    public Button StartButton;
    public Button ToggleReadyButton;
    public TMP_Text ToggleReadyButtonText;
    
    private GameNetworkManager _gameNetworkManager;
    private SteamLobbyService steamLobbyService => SteamLobbyService.Instance;
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
        GameNetworkPlayer.PlayerSteamInfoDetermined += UpdatePlayerSlots;
        // steamLobbyService.OnCurrentLobbyMetadataChanged += UpdatePlayerSlots;    // TODO do we listen to this, or maybe to one of the GameNetworkPlayer methods, or maybe to the GameNetworkManager updatelobby event.
    }

    private void OnDestroy() {
        GameNetworkPlayer.PlayerSteamInfoDetermined -= UpdatePlayerSlots;
        if (_gameNetworkManager != null) {
            _gameNetworkManager.RoomServerPlayersReadyAction -= ShowStartButton;
            _gameNetworkManager.RoomServerPlayersNotReadyAction -= HideStartButton;
        }
    }

    public void SetRandomMetadata() {
        steamLobbyService.UpdateCurrentLobbyMetadata("dummyData", "69");
    }

    public void SetRandomPlayerMetadata() {
        steamLobbyService.UpdateCurrentLobbyPlayerMetadata("color", "very red");
    }

    private void ShowStartButton() {
        StartButton.gameObject.SetActive(true);
    }

    private void HideStartButton() {
        StartButton.gameObject.SetActive(false);
    }

    private void UpdatePlayerSlots() {
        // List<GameNetworkPlayer> players = _gameNetworkManager.roomSlots.ConvertAll(player => (GameNetworkPlayer)player);
        List<GameNetworkPlayer> players = FindObjectsOfType<GameNetworkPlayer>().ToList();

        // TODO Un-assign any players who left
        
        // Assign any unassigned players
        foreach (GameNetworkPlayer player in players) {
            if (PlayerSlot1.AssignedPlayer != player && PlayerSlot2.AssignedPlayer != player) {
                if (PlayerSlot1.AssignedPlayer == null) {
                    PlayerSlot1.AssignPlayer(player);
                } else if (PlayerSlot2.AssignedPlayer == null) {
                    PlayerSlot2.AssignPlayer(player);
                } else {
                    Log.Error("A new player joined, but we don't have any slots for them!");
                }
            }
        }

        // TODO: If players can update their info for their slots, do so here
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

    public void StartGame() {
        _gameNetworkManager.ServerChangeScene(_gameNetworkManager.GameplayScene);
    }
}
