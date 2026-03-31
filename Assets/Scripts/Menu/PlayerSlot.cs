using System.Collections.Generic;
using System.Linq;
using Audio;
using Game.Network;
using Gameplay.Config;
using HeathenEngineering.SteamworksIntegration.UI;
using Menu;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Util;

/// <summary>
/// Handles displaying info for a player slot, including any assigned player
/// </summary>
public class PlayerSlot : MonoBehaviour {
    [Header("References")]
    [SerializeField] private TMP_Text _playerName;
    [SerializeField] private GameObject _hostText;
    [SerializeField] private ColorPicker _colorPicker;
    [SerializeField] private Button _playerKickButton;
    [SerializeField] private Button _spectatorKickButton;
    [SerializeField] private TMP_Text _readyText;
    [SerializeField] private string _readyTextValue = "Ready";
    [SerializeField] private string _notReadyTextValue = "Not Ready";
    [SerializeField] private List<Image> _coloredImages;
    [SerializeField] private List<Image> _darkColoredImages;
    [SerializeField] private List<GameObject> _occupiedObjects;
    [SerializeField] private List<GameObject> _unOccupiedObjects;
    [SerializeField] private SetUserAvatar _userAvatarSetter;
    [SerializeField] private ButtonDim _buttonDim;
    
    [Header("Config")]
    public int SlotIndex;
    public GameNetworkPlayer AssignedPlayer;
    public bool SpectatorSlot;
    public PlayerColorData DefaultColor;

    private RoomMenu _roomMenu;
    private Button _activeKickButton;
    private List<PlayerColorData> _availableColors;
    private PlayerColorData _neutralColor;

    public void Start() {
        // Show as empty
        DisplayOccupied(false);
    }

    public void Initialize(RoomMenu roomMenu, List<PlayerColorData> availableColors, Transform colorMenuParent) {
        _roomMenu = roomMenu;
        _availableColors = availableColors;
        _neutralColor = GameConfigurationLocator.GameConfiguration.NeutralColors;
        
        _playerKickButton.gameObject.SetActive(false);
        _spectatorKickButton.gameObject.SetActive(false);
        _colorPicker.Initialize(availableColors.Where(c => c.Pickable).ToList(), this, colorMenuParent);
        
        if (SpectatorSlot) {
            InitializeForSpectator();
        } else {
            InitializeForPlayer();
        }
    }

    private void InitializeForPlayer() {
        _activeKickButton = _playerKickButton;
        _colorPicker.gameObject.SetActive(true);
        UpdateColor(AssignedPlayer?.GetColorID ?? _neutralColor.ID);
    }
    
    private void InitializeForSpectator() {
        _activeKickButton = _spectatorKickButton;
        _colorPicker.gameObject.SetActive(false);
        UpdateColor(_neutralColor.ID);
    }

    public void AssignPlayer(GameNetworkPlayer player, bool kickable) {
        AssignedPlayer = player;
        
        _playerName.text = player.DisplayName;
        _hostText.SetActive(player.IsHostPlayer);
        
        // Show as occupied
        DisplayOccupied(true);

        _activeKickButton.gameObject.SetActive(kickable);
        
        // Update color for the new player
        UpdateColor(SpectatorSlot ? _neutralColor.ID : player.GetColorID);
        
        // Load the avatar
        _userAvatarSetter.LoadAvatar(player.SteamID);
        
        _buttonDim.Interactable = false;
    }

    public void UnassignPlayer() {
        AssignedPlayer = null;
        _playerName.text = "Player Name";
        
        // Show as empty
        DisplayOccupied(false);
        UpdateColor(_neutralColor.ID);
        
        _buttonDim.Interactable = true;
    }

    public void SwapToSlot() {
        if (AssignedPlayer) return;
        GameAudio.Instance.MenuThumpSound();
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
        if (!AssignedPlayer) {
            _readyText.gameObject.SetActive(false);
            return;
        }
        
        _readyText.gameObject.SetActive(!SpectatorSlot);
        _readyText.text = AssignedPlayer.readyToBegin ? _readyTextValue : _notReadyTextValue;
    }

    public void UpdateColor(string colorID) {
        PlayerColorData colorData = _availableColors.First(c => c.ID == colorID);
        _coloredImages.ForEach(i => i.color = colorData.TeamColor);
        _darkColoredImages.ForEach(i => i.color = colorData.DarkTeamColor);
        
        // Color picker
        _colorPicker.SetColor(colorData);
        _colorPicker.SetCanChangeColor(AssignedPlayer && AssignedPlayer.isLocalPlayer);
    }
    
    /// <summary>
    /// Either display the slot in its occupied or unoccupied state
    /// </summary>
    private void DisplayOccupied(bool occupied) {
        _occupiedObjects.ForEach(i => i.SetActive(occupied));
        _unOccupiedObjects.ForEach(i => i.SetActive(!occupied));
        
        if (occupied) {
            UpdateReadyStatus();
            UpdateColor(SpectatorSlot ? _neutralColor.ID : AssignedPlayer.GetColorID);
        }
    }
}
