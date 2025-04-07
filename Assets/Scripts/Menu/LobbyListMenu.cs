using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// View for retrieving and displaying available Steam rooms
/// </summary>
public class LobbyListMenu : MonoBehaviour {
    public LobbyListEntry LobbyListEntryPrefab;
    public GameObject ScrollViewContent;
    public GameObject LoadingIcon;
    public GameObject NoLobbiesFoundMessage;
    
    private List<LobbyListEntry> _lobbyEntries = new List<LobbyListEntry>();
    private bool _retrievingLobbies;

    public void ShowMenu() {
        gameObject.SetActive(true);
        
        // Get ready to display lobbies when we find them
        SteamLobbyService.Instance.OnLobbyEntryConstructed += DisplayLobby;

        RefreshLobbyList();
    }
    
    private void OnDestroy() {
        if (SteamLobbyService.Instance == null || !SteamLobbyService.Instance.SteamEnabled) return;

        SteamLobbyService.Instance.OnLobbyEntryConstructed -= DisplayLobby;
    }

    public void LeaveMenu() {
        if (SteamLobbyService.Instance == null || !SteamLobbyService.Instance.SteamEnabled) return;

        SteamLobbyService.Instance.OnLobbyEntryConstructed -= DisplayLobby;
        _retrievingLobbies = false;
        NoLobbiesFoundMessage.SetActive(false);

        // Clear out the old list if there are entries in it
        _lobbyEntries.ForEach(e => Destroy(e.gameObject));
        _lobbyEntries.Clear();
        gameObject.SetActive(false);
    }

    public void RefreshLobbyList() {
        if (_retrievingLobbies) {
            return;
        }

        _retrievingLobbies = true;
        NoLobbiesFoundMessage.SetActive(false);

        // Clear out the old list if there are entries in it
        _lobbyEntries.ForEach(e => Destroy(e.gameObject));
        _lobbyEntries.Clear();
        
        // Show loading animation
        LoadingIcon.SetActive(true);
        
        // Search for lobbies
        SteamLobbyService.Instance.GetAllOpenLobbies((lobbyCount, success, failureMessage) => {
            _retrievingLobbies = false;

            // Hide the loading animation if it is being displayed
            LoadingIcon.SetActive(false);
            // If no lobbies were found, display that to the user
            NoLobbiesFoundMessage.SetActive(lobbyCount == 0);
            
            SteamLobbyService.Instance.ProcessReturnedLobbies(lobbyCount, success, failureMessage);
        });
    }

    /// <summary>
    /// Construct a new lobby entry view in the list and populate it with the data from the retrieved lobby
    /// </summary>
    private void DisplayLobby(SteamLobbyService.Lobby lobby) {
        // Hide the loading animation if it is being displayed
        LoadingIcon.SetActive(false);
        
        LobbyListEntry newEntry = Instantiate(LobbyListEntryPrefab, ScrollViewContent.transform);
        newEntry.PopulateEntry(lobby);
        _lobbyEntries.Add(newEntry);
    }
}
