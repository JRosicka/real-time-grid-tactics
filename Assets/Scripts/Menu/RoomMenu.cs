using System;
using System.Collections.Generic;
using System.Linq;
using Audio;
using Game.Network;
using JetBrains.Annotations;
using Menu;
using Mirror;
using Scenes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI for the game lobby.
/// TODO: Also handles a lot of the business logic for swapping players around and starting the game. Might be good to
/// separate that out into its own class. 
/// </summary>
public class RoomMenu : MonoBehaviour {
    public PlayerSlot PlayerSlot1;
    public PlayerSlot PlayerSlot2;
    public List<PlayerSlot> SpectatorSlots;
    public Button StartButton;
    public Button SwitchMapButton;
    public Button ToggleReadyButton;
    public TMP_Text ToggleReadyButtonText;
    public TMP_Text JoinCodeText;
    
    public CanvasWidthSetter CanvasWidthSetter;

    public Animator CopiedToClipboardAnimator;
    
    public LobbyNetworkBehaviour LobbyNetworkBehaviour;
    
    private static GameNetworkManager NetworkManager => (GameNetworkManager)Mirror.NetworkManager.singleton;
    private SteamLobbyService SteamLobbyService => SteamLobbyService.Instance;
    private string _joinCode;
    private string _mapID;

    private List<GameNetworkPlayer> PlayersInLobby => FindObjectsByType<GameNetworkPlayer>(FindObjectsSortMode.InstanceID).ToList();
    private GameNetworkPlayer _cachedGameNetworkPlayer;
    private GameNetworkPlayer LocalPlayer {
        get {
            if (_cachedGameNetworkPlayer == null) {
                _cachedGameNetworkPlayer = PlayersInLobby.First(player => player.isLocalPlayer);
            }

            return _cachedGameNetworkPlayer;
        }
    }
    
    private List<PlayerSlot> AllPlayerSlots => new List<PlayerSlot> { PlayerSlot1, PlayerSlot2 }.Concat(SpectatorSlots).ToList();
    
    private PlayerSlot GetSlotForIndex(int index) {
        return AllPlayerSlots.First(s => s.SlotIndex == index);
    }

    [CanBeNull]
    private PlayerSlot LocalPlayerSlot => AllPlayerSlots.FirstOrDefault(s => s.AssignedPlayer != null && s.AssignedPlayer.isLocalPlayer);

    private PlayerSlot SlotForPlayer(GameNetworkPlayer player) {
        return AllPlayerSlots.FirstOrDefault(s => s.AssignedPlayer == player);
    }

    void Start() {
        CanvasWidthSetter.Initialize();

        AllPlayerSlots.ForEach(s => s.Initialize(this));
        
        StartButton.gameObject.SetActive(false);
        SwitchMapButton.gameObject.SetActive(NetworkServer.active);
        NetworkManager.RoomServerPlayersReadyAction += TryShowStartButton;
        NetworkManager.RoomServerPlayersNotReadyAction += HideStartButton;
        NetworkManager.RoomServerSceneChangedAction += UpdateLobbyOpenStatus;
        NetworkManager.RoomClientSceneChangedAction += AddUnassignedPlayers;
        GameNetworkPlayer.PlayerSteamInfoDetermined += AddUnassignedPlayers;
        GameNetworkPlayer.PlayerReadyStatusChanged += UpdatePlayerReadyStatus;
        GameNetworkPlayer.PlayerExitedRoom += UpdatePlayers;
        GameNetworkPlayer.PlayerExitedRoom += ResetReadyButton;

        SteamLobbyService.Lobby lobby =
            SteamLobbyService.Instance.GetLobbyData(SteamLobbyService.Instance.CurrentLobbyID, null);
        _joinCode = lobby[SteamLobbyService.LobbyUIDKey];
        JoinCodeText.text = _joinCode;
        // steamLobbyService.OnCurrentLobbyMetadataChanged += AddUnassignedPlayers;    // TODO do we listen to this, or maybe to one of the GameNetworkPlayer methods, or maybe to the GameNetworkManager updatelobby event.

        LobbyNetworkBehaviour.SetUpCurrentMapOnLobbyJoin();
        _mapID = GameTypeTracker.Instance.MapID;
    }
    
    private void OnDestroy() {
        GameNetworkPlayer.PlayerSteamInfoDetermined -= AddUnassignedPlayers;
        GameNetworkPlayer.PlayerReadyStatusChanged -= UpdatePlayerReadyStatus;
        GameNetworkPlayer.PlayerExitedRoom -= UpdatePlayers;
        GameNetworkPlayer.PlayerExitedRoom -= ResetReadyButton;
        if (NetworkManager != null) {
            NetworkManager.RoomServerPlayersReadyAction -= TryShowStartButton;
            NetworkManager.RoomServerPlayersNotReadyAction -= HideStartButton;
            NetworkManager.RoomServerSceneChangedAction -= UpdateLobbyOpenStatus;
            NetworkManager.RoomClientSceneChangedAction -= AddUnassignedPlayers;
        }

        foreach (GameNetworkPlayer player in AllPlayerSlots.Select(s => s.AssignedPlayer)
                     .Where(p => p != null)) {
            player.PlayerSwappedToSlot -= HandlePlayerSwappedToSlot;
        }
    }

    public void SetRandomMetadata() {
        SteamLobbyService.UpdateCurrentLobbyMetadata("dummyData", "69");
    }

    public void SetRandomPlayerMetadata() {
        SteamLobbyService.UpdateCurrentLobbyPlayerMetadata("color", "very red");
    }

    public void ExitRoom() {
        DisconnectFeedbackService.SetDisconnectReason(DisconnectFeedbackService.DisconnectReason.ClientDisconnect);
        if (NetworkServer.active) {
            // We're the host, so stop the whole server
            NetworkManager.StopHost();
            SceneLoader.Instance.LoadMainMenu();
        } else {
            // We're just a little baby client, so just stop the client. This automatically takes us back to the main menu.
            NetworkManager.StopClient();
        }
    }

    /// <summary>
    /// Temporary logic to cycle between maps. Server method.
    /// </summary>
    public void SwitchMap() {
        _mapID = _mapID switch {
            "origins" => "mountainPass",
            "mountainPass" => "oakcrest",
            "oakcrest" => "origins",
            _ => throw new ArgumentOutOfRangeException(_mapID, $"Unexpected map ID: {_mapID}")
        };

        LobbyNetworkBehaviour.SwitchMap(_mapID);
    }

    public void CopyJoinCode() {
        GUIUtility.systemCopyBuffer = _joinCode;
        CopiedToClipboardAnimator.Play("Copy Join Code");
    }

    private void TryShowStartButton() {
        // Can only show the start button for the host
        if (!NetworkServer.active) return;
        
        if (PlayerSlot1.AssignedPlayer == null && PlayerSlot2.AssignedPlayer == null) {
            // Need at least one filled team slot to start a game
            StartButton.gameObject.SetActive(false);
        } else if (PlayerSlot1.AssignedPlayer != null && !PlayerSlot1.AssignedPlayer.readyToBegin) {
            // Player 1 not ready
            StartButton.gameObject.SetActive(false);
        } else if (PlayerSlot2.AssignedPlayer != null && !PlayerSlot2.AssignedPlayer.readyToBegin) {
            // Player 2 not ready
            StartButton.gameObject.SetActive(false);
        } else {
            StartButton.gameObject.SetActive(true);
        }
    }

    private void HideStartButton() {
        StartButton.gameObject.SetActive(false);
    }

    private void AddUnassignedPlayers() {
        List<GameNetworkPlayer> players = PlayersInLobby.OrderBy(p => p.index).ToList();
        
        // Assign any unassigned players
        foreach (GameNetworkPlayer player in players.Where(player => !AllPlayerSlots.Select(s => s.AssignedPlayer).Contains(player))) {
            PlayerSlot slotToAssign = GetSlotForIndex(player.index);
            if (slotToAssign == null) {
                Debug.LogError("A new player joined, but we don't have any slots for them!");
            } else {
                slotToAssign.AssignPlayer(player, PlayerIsKickable(player));
                player.PlayerSwappedToSlot += HandlePlayerSwappedToSlot;
            }
        }

        UpdatePlayerReadyStatus();
        ResetReadyButton();
        // TODO: If players can update their info for their slots, do so here
    }
    
    private void UpdatePlayerReadyStatus() {
        AllPlayerSlots.ForEach(s => s.UpdateReadyStatus());
    }

    private void UpdatePlayers() {
        List<GameNetworkPlayer> players = NetworkManager.roomSlots.ConvertAll(player => (GameNetworkPlayer)player);

        // Unassign any players who have disconnected
        foreach (PlayerSlot playerSlot in AllPlayerSlots) {
            if (playerSlot.AssignedPlayer != null && !players.Contains(playerSlot.AssignedPlayer)) {
                playerSlot.AssignedPlayer.PlayerSwappedToSlot -= HandlePlayerSwappedToSlot;
                playerSlot.UnassignPlayer();
            }
        }
    }

    private bool PlayerIsKickable(GameNetworkPlayer player) {
        return NetworkManager.IsHosting() && !player.isLocalPlayer;
    }

    public void SwapLocalPlayerToSlot(PlayerSlot playerSlot) {
        LocalPlayer.CmdSwapToSlot(playerSlot.SlotIndex);
    }

    // Client event
    private void HandlePlayerSwappedToSlot(ulong steamID, int slotIndex) {
        PlayerSlot slotToSwapTo = GetSlotForIndex(slotIndex);
        if (slotToSwapTo.AssignedPlayer != null) {
            // This is probably because the player slot already updated locally
            ResetReadyButton();
            TryShowStartButton();
            return;
        }

        GameNetworkPlayer playerToAssign = PlayersInLobby.FirstOrDefault(p => p.SteamID.m_SteamID == steamID);
        if (playerToAssign == null) {
            Debug.LogWarning($"Tried to swap a player that is not in the lobby! Steam ID: {steamID}. Slot: {slotIndex}");
            return;
        }
        
        PlayerSlot slotToSwapFrom = SlotForPlayer(playerToAssign);
        if (slotToSwapFrom == null) {
            Debug.LogWarning($"Tried to swap a player away from a slot, but they are not currently assigned to " +
                             $"a slot! Steam ID: {steamID}. Slot we attempted to swap to: {slotIndex}");
            return;
        }
        
        // Unassign the player from whatever slot they were in
        slotToSwapFrom.UnassignPlayer();
        
        // Assign the player to their new slot
        slotToSwapTo.AssignPlayer(playerToAssign, PlayerIsKickable(playerToAssign));
        
        // If the local player just swapped, then we might want to toggle the availability of the ready/cancel button
        ResetReadyButton(playerToAssign == LocalPlayer);
        
        // Swapping players out of/into a player slot could affect whether we can start the game
        TryShowStartButton();
    }

    public void ToggleReady() {
        if (LocalPlayer.readyToBegin) {
            ToggleReadyButtonText.text = "Ready";
            LocalPlayer.CmdChangeReadyState(false);
        } else {
            ToggleReadyButtonText.text = "Cancel";
            LocalPlayer.CmdChangeReadyState(true);
        }
    }

    /// <summary>
    /// If the ready state changes from some way other than clicking the button, such as when another player leaves the
    /// room, then call this to reset the text on the ready button to reflect the actual ready state
    /// </summary>
    private void ResetReadyButton() {
        ResetReadyButton(false);
    }

    private void ResetReadyButton(bool forceNotReady) {
        PlayerSlot localSlot = LocalPlayerSlot;
        if (localSlot == null || localSlot.SpectatorSlot) { 
            ToggleReadyButton.gameObject.SetActive(false);
        } else {
            ToggleReadyButton.gameObject.SetActive(true);
            ToggleReadyButtonText.text = !forceNotReady && LocalPlayer.readyToBegin ? "Cancel" : "Ready";
        }
    }
    
    /// <summary>
    /// Check to see if we should open/close the lobby. If we just came back to the room scene, then we should open it.
    /// This should be done only on the server. 
    /// </summary>
    private void UpdateLobbyOpenStatus() {
        bool isInGameScene = Mirror.NetworkManager.IsSceneActive(NetworkManager.GameplayScene);
        SteamLobbyService.UpdateCurrentLobbyMetadata(SteamLobbyService.LobbyGameActiveKey, isInGameScene.ToString());
    }

    public void StartGame() {
        SteamLobbyService.UpdateCurrentLobbyMetadata(SteamLobbyService.LobbyGameActiveKey, true.ToString());
        LobbyNetworkBehaviour.LockMapLoading();
        NetworkManager.ServerChangeScene(NetworkManager.GameplayScene);
    }
}
