using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

public class LobbyListMenu : MonoBehaviour {
    public LobbyListEntry LobbyListEntryPrefab;
    public GameObject ScrollViewContent;
    public GameObject LoadingIcon;
    
    private List<LobbyListEntry> _lobbyEntries = new List<LobbyListEntry>();

    public void ShowMenu() {
        gameObject.SetActive(true);
        
        // Get ready to display lobbies when we find them
        SteamLobbyService.Instance.OnLobbyEntryConstructed += DisplayLobby;

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
        
        // Show loading animation
        LoadingIcon.SetActive(true);
        
        // Search for lobbies
        SteamLobbyService.Instance.GetAllOpenLobbies(SteamLobbyService.Instance.ProcessReturnedLobbies);
    }

    private void DisplayLobby(SteamLobbyService.Lobby lobby) {
        // Hide the loading animation if it is being displayed
        LoadingIcon.SetActive(false);
        
        LobbyListEntry newEntry = Instantiate(LobbyListEntryPrefab, ScrollViewContent.transform);
        newEntry.PopulateEntry(lobby);
        _lobbyEntries.Add(newEntry);
    }

    private void OnDestroy() {
        SteamLobbyService.Instance.OnLobbyEntryConstructed -= DisplayLobby;
    }
}
