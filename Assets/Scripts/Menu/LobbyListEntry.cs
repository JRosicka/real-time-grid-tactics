using System;
using Steamworks;
using TMPro;
using UnityEngine;

/// <summary>
/// View for displaying info about an available Steam room and allowing the player to join the room.
/// </summary>
public class LobbyListEntry : MonoBehaviour {
    private const int PlayerDisplayNameLimit = 20;
    
    public TMP_Text LobbyNameText;
    public TMP_Text PlayerCountText;
    public TMP_Text PrivateStatusText;

    public string LobbyNameFormat = "{0}'s lobby";
    public string PlayerCountFormat = "Player count: {0}/{1}";
    public string PrivateStatus = "Private";
    public string PublicStatus = "Public";

    public static event Action<CSteamID, string> LobbyJoinAttemptStarted;
    
    private CSteamID _lobbyID;
    private bool _privateLobby;
    private string _joinCode;
    
    public void PopulateEntry(SteamLobbyService.Lobby lobby) {
        string members = "";
        foreach (SteamLobbyService.LobbyMember member in lobby.Members) {
            members += $"({member.SteamID}, {member.Data.Length}), ";
        }
        string data = "";
        foreach (SteamLobbyService.LobbyMetaData metadata in lobby.Data) {
            data += $"({metadata.Key}, {metadata.Value}), ";
        }
        Debug.Log($"Populating lobby: ID: {lobby.SteamID}. Member limit: {lobby.MemberLimit}. Members: {members}. Data: {data}.");
        
        string hostName = ProcessName(lobby[SteamLobbyService.LobbyOwnerKey]);
        LobbyNameText.text = string.Format(LobbyNameFormat, hostName);
        _privateLobby = !Convert.ToBoolean(lobby[SteamLobbyService.LobbyIsOpenKey]);
        PrivateStatusText.text = _privateLobby ? PrivateStatus : PublicStatus;
        if (_privateLobby) {
            _joinCode = lobby[SteamLobbyService.LobbyUIDKey];
        }
        PlayerCountText.text = string.Format(PlayerCountFormat, lobby.Members.Length.ToString(), lobby.MemberLimit);

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
