using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.Utilities;
using Steamworks;
using TMPro;
using UnityEngine;

public class LobbyListEntry : MonoBehaviour {
    public TMP_Text LobbyNameField;
    public TMP_Text HostField;
    public TMP_Text RequiresJoinCodeField;
    public TMP_Text PlayerCountField;
    public TMP_Text PingField;

    public static event Action<CSteamID, string> LobbyJoinAttemptStarted;
    
    private CSteamID _lobbyID;
    private bool _privateLobby;
    private string _joinCode;
    
    public void PopulateEntry(SteamLobbyService.Lobby lobby) {
        LobbyNameField.text = $"{lobby[SteamLobbyService.LobbyOwnerKey]}'s lobby";
        _privateLobby = !lobby[SteamLobbyService.LobbyIsOpenKey].IsNullOrWhitespace();
        RequiresJoinCodeField.text = _privateLobby ? "No" : "Yes";
        if (_privateLobby) {
            _joinCode = lobby[SteamLobbyService.LobbyUIDKey];
        }
        PlayerCountField.text = $"{lobby.Members.Length.ToString()}/{lobby.MemberLimit}";
        // PingField.text = TODO

        _lobbyID = lobby.SteamID;
    }

    public void JoinLobby() {
        if (_privateLobby) {
            LobbyJoinAttemptStarted.SafeInvoke(_lobbyID, _joinCode);
        } else {
            SteamLobbyService.Instance.JoinLobby(_lobbyID);
        }
    }
}
