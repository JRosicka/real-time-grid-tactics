using System.Collections;
using System.Collections.Generic;
using Game.Network;
using UnityEngine;
using UnityEngine.UI;

public class RoomMenu : MonoBehaviour {
    public PlayerSlot PlayerSlot1;
    public PlayerSlot PlayerSlot2;
    public Button StartButton;
    
    private GameNetworkManager _gameNetworkManager;
    private SteamLobbyService steamLobbyService => SteamLobbyService.Instance;

    void Start() {
        StartButton.gameObject.SetActive(false);
        _gameNetworkManager = FindObjectOfType<GameNetworkManager>();
        _gameNetworkManager.RoomServerPlayersReadyAction += ShowStartButton;
        _gameNetworkManager.RoomServerPlayersNotReadyAction += HideStartButton;
        // steamLobbyService.OnCurrentLobbyMetadataChanged += UpdatePlayerSlots;    // TODO do we listen to this, or maybe to one of the GameNetworkPlayer methods, or maybe to the GameNetworkManager updatelobby event.
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
        
    }
}
