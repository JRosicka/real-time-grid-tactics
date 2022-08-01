using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

public class LobbyListMenu : MonoBehaviour {
    public LobbyListEntry LobbyListEntryPrefab;
    public GameObject ScrollViewContent;
    
    private List<LobbyListEntry> _lobbyEntries = new List<LobbyListEntry>();

    public void ShowMenu() {
        gameObject.SetActive(true);
        RefreshLobbyList();
    }

    public void LeaveMenu() {
        SteamLobbyService.Instance.OnLobbyEntryConstructed -= DisplayLobby;
        // TODO Cancel any lobby join attempts

        // Clear out the old list if there are entries in it
        _lobbyEntries.ForEach(e => Destroy(e.gameObject));
        _lobbyEntries.Clear();
        gameObject.SetActive(false);
    }

    public void RefreshLobbyList() {
        // Clear out the old list if there are entries in it
        _lobbyEntries.ForEach(e => Destroy(e.gameObject));
        _lobbyEntries.Clear();
        
        // Get ready to display lobbies when we find them
        SteamLobbyService.Instance.OnLobbyEntryConstructed += DisplayLobby;

        // Search for lobbies
        SteamLobbyService.Instance.GetAllOpenLobbies(SteamLobbyService.Instance.ProcessReturnedLobbies);
    }

    private void DisplayLobby(SteamLobbyService.Lobby lobby) {
        LobbyListEntry newEntry = Instantiate(LobbyListEntryPrefab, ScrollViewContent.transform);
        newEntry.PopulateEntry(lobby);
        _lobbyEntries.Add(newEntry);
    }

    private void OnDestroy() {
        SteamLobbyService.Instance.OnLobbyEntryConstructed -= DisplayLobby;
    }
}
