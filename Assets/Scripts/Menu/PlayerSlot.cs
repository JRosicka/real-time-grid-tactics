using Game.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles displaying info for a player slot, including any assigned player
/// </summary>
public class PlayerSlot : MonoBehaviour {
    [Header("References")]
    public TMP_Text PlayerName;
    public TMP_Text ColorLabel;
    public Image Color;
    public TMP_Text EmptyLabel;
    public Button KickButton;
    public TMP_Text ReadyText;
    public TMP_Text NotReadyText;
    
    [Header("Config")]
    public Color PlayerColor;
    public int SlotIndex;
    public GameNetworkPlayer AssignedPlayer;
    public bool SpectatorSlot;

    private RoomMenu _roomMenu;

    public void Start() {
        // Configure
        Color.color = PlayerColor;
        
        // Show as empty
        DisplayActive(false);
    }

    public void Initialize(RoomMenu roomMenu) {
        _roomMenu = roomMenu;
    }

    public void AssignPlayer(GameNetworkPlayer player, bool kickable) {
        AssignedPlayer = player;
        
        string displayName = player.DisplayName;
        if (player.IsHostPlayer) {
            displayName += " (Host)";
        }
        PlayerName.text = displayName;
        
        // Show as occupied
        DisplayActive(true);

        KickButton.gameObject.SetActive(kickable);
    }

    public void UnassignPlayer() {
        AssignedPlayer = null;
        PlayerName.text = "Player Name";
        
        // Show as empty
        DisplayActive(false);
    }

    public void SwapToSlot() {
        if (AssignedPlayer != null) return;
        _roomMenu.SwapLocalPlayerToSlot(this);
    }

    public void KickPlayer() {
        if (AssignedPlayer == null) {
            Debug.LogError("Attempted to kick a player from a slot with no assigned player! Doing nothing.");
            return;
        }

        AssignedPlayer.Kick();
        // TODO the kicked player probably should not be let back in. We could add a blocklist of steam IDs as metadata to the lobby and filter blocked IDs out when searching for lobbies, and then also deny entry if they try to join directly through steam. 
    }

    public void UpdateReadyStatus() {
        if (AssignedPlayer == null) {
            ReadyText.gameObject.SetActive(false);
            NotReadyText.gameObject.SetActive(false);
            return;
        }
        
        ReadyText.gameObject.SetActive(AssignedPlayer.readyToBegin && !SpectatorSlot);
        NotReadyText.gameObject.SetActive(!AssignedPlayer.readyToBegin && !SpectatorSlot);
    }

    /// <summary>
    /// Either display the slot in its active or inactive state
    /// </summary>
    private void DisplayActive(bool active) {
        PlayerName.gameObject.SetActive(active);
        ColorLabel.gameObject.SetActive(active && !SpectatorSlot);
        Color.gameObject.SetActive(active && !SpectatorSlot);
        EmptyLabel.gameObject.SetActive(!active);
        KickButton.gameObject.SetActive(false);
        ReadyText.gameObject.SetActive(false);
        NotReadyText.gameObject.SetActive(active && !SpectatorSlot);
        if (active) {
            UpdateReadyStatus();
        }
    }
}
