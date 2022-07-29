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

    private CSteamID _lobbyID;

    public void PopulateEntry(SteamLobbyService.Lobby lobby) {
        LobbyNameField.text = $"{lobby[SteamLobbyService.LobbyOwnerKey]}'s lobby";
        RequiresJoinCodeField.text =
            lobby[SteamLobbyService.LobbyIsOpenKey].IsNullOrWhitespace()
                ? "No"
                : "Yes";
        PlayerCountField.text = $"{lobby.Members.Length.ToString()}/{lobby.MemberLimit}";
        // PingField.text = TODO

        _lobbyID = lobby.SteamID;
    }

    public void JoinLobby() {
        SteamLobbyService.Instance.JoinLobby(_lobbyID);
    }
}
