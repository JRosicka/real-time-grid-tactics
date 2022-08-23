using Game.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles displaying info for a player slot, including any assigned player
/// </summary>
public class PlayerSlot : MonoBehaviour {
    [Header("References")]
    public TMP_Text PlayerLabel;
    public TMP_Text PlayerName;
    public TMP_Text ColorLabel;
    public Image Color;
    public TMP_Text EmptyLabel;
    public Button KickButton;
    public TMP_Text ReadyText;
    public TMP_Text NotReadyText;
    
    [Header("Config")]
    public Color PlayerColor;
    public int PlayerIndex;
    public GameNetworkPlayer AssignedPlayer;

    public void Start() {
        // Configure
        PlayerLabel.text = $"Player {PlayerIndex}";
        Color.color = PlayerColor;
        
        // Show as empty
        DisplayActive(false);
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
            return;
        }
        
        ReadyText.gameObject.SetActive(AssignedPlayer.readyToBegin);
        NotReadyText.gameObject.SetActive(!AssignedPlayer.readyToBegin);
    }

    /// <summary>
    /// Either display the slot in its active or inactive state
    /// </summary>
    private void DisplayActive(bool active) {
        PlayerName.gameObject.SetActive(active);
        ColorLabel.gameObject.SetActive(active);
        Color.gameObject.SetActive(active);
        EmptyLabel.gameObject.SetActive(!active);
        KickButton.gameObject.SetActive(false);
        ReadyText.gameObject.SetActive(false);
        NotReadyText.gameObject.SetActive(active);
        if (active) {
            UpdateReadyStatus();
        }
    }
}
