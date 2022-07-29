using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

public class LobbyListMenu : MonoBehaviour {
    public SteamLobbyService SteamLobbyService;
    public LobbyListEntry LobbyListEntryPrefab;
    public GameObject ScrollViewContent;
    
    private List<LobbyListEntry> _lobbyEntries = new List<LobbyListEntry>();

    public void ShowMenu() {
        gameObject.SetActive(true);
        RefreshLobbyList();
    }

    public void LeaveMenu() {
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
        SteamLobbyService.OnLobbyEntryConstructed += DisplayLobby;

        // Search for lobbies
        SteamLobbyService.GetAllOpenLobbies(SteamLobbyService.ProcessReturnedLobbies);
    }

    private void DisplayLobby(SteamLobbyService.Lobby lobby) {
        LobbyListEntry newEntry = Instantiate(LobbyListEntryPrefab, ScrollViewContent.transform);
        newEntry.PopulateEntry(lobby);
        _lobbyEntries.Add(newEntry);
    }
}
