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

    [Header("Config")]
    public Color PlayerColor;
    public int PlayerIndex;
    public GameNetworkPlayer AssignedPlayer;

    public void Start() {
        // Configure
        PlayerLabel.text = $"Player {PlayerIndex}";
        Color.color = PlayerColor;
        
        // Show as empty
        PlayerName.gameObject.SetActive(false);
        ColorLabel.gameObject.SetActive(false);
        Color.gameObject.SetActive(false);
        EmptyLabel.gameObject.SetActive(true);
    }

    public void AssignPlayer(GameNetworkPlayer player, string playerName, bool isHost) {
        AssignedPlayer = player;
        
        string displayName = playerName;
        if (isHost) {
            displayName += " (Host)";
        }
        PlayerName.text = displayName;
        
        // Show as occupied
        PlayerName.gameObject.SetActive(true);
        ColorLabel.gameObject.SetActive(true);
        Color.gameObject.SetActive(true);
        EmptyLabel.gameObject.SetActive(false);
    }
}
