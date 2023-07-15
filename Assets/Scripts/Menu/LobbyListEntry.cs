using System;
using Steamworks;
using TMPro;
using UnityEngine;

/// <summary>
/// View for displaying info about an available Steam room and allowing the player to join the room.
/// </summary>
public class LobbyListEntry : MonoBehaviour {
    private const int PlayerDisplayNameLimit = 20;
    
    public TMP_Text LobbyNameField;
    public TMP_Text RequiresJoinCodeField;
    public TMP_Text PlayerCountField;
    public TMP_Text PingField;

    public static event Action<CSteamID, string> LobbyJoinAttemptStarted;
    
    private CSteamID _lobbyID;
    private bool _privateLobby;
    private string _joinCode;
    
    public void PopulateEntry(SteamLobbyService.Lobby lobby) {
        string hostName = ProcessName(lobby[SteamLobbyService.LobbyOwnerKey]);
        LobbyNameField.text = $"{hostName}'s lobby";
        _privateLobby = !Convert.ToBoolean(lobby[SteamLobbyService.LobbyIsOpenKey]);
        RequiresJoinCodeField.text = _privateLobby ? "Yes" : "No";
        if (_privateLobby) {
            _joinCode = lobby[SteamLobbyService.LobbyUIDKey];
        }
        PlayerCountField.text = $"{lobby.Members.Length.ToString()}/{lobby.MemberLimit}";
        // PingField.text = TODO the only way I see to do this is NetworkTime.rtt, but I believe we need to actually be connected to a server for that

        _lobbyID = lobby.SteamID;
    }

    public void JoinLobby() {
        if (_privateLobby) {
            LobbyJoinAttemptStarted?.Invoke(_lobbyID, _joinCode);
        } else {
            SteamLobbyService.Instance.JoinLobby(_lobbyID);
        }
    }

    /// <summary>
    /// Process the name if it is too long to display
    /// </summary>
    private static string ProcessName(string fullName) {
        string filteredName = fullName.Substring(0, Mathf.Min(fullName.Length, PlayerDisplayNameLimit));
        if (filteredName.Length < fullName.Length) {
            filteredName += "...";
        }

        return filteredName;
    }
}
