using Game.Network;
using Mirror;
using Steamworks;
using UnityEngine;

public class SteamLobby : MonoBehaviour {
    [SerializeField] private GameNetworkManager _networkManager;

    private const string HostAddressKey = "HostAddress";

    private Callback<LobbyCreated_t> _lobbyCreated;
    private Callback<GameLobbyJoinRequested_t> _gameLobbyJoinRequested;
    private Callback<LobbyEnter_t> _lobbyEntered;

    private CSteamID currentLobbyID;

    private void Start() {
        if (!SteamManager.Initialized)    // TODO error handling
            return;
        
        _lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyConnected);
        _gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        _lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
    }
    
    public void HostLobby() {
        // Tell Steam to create a lobby, then wait for callbacks to trigger
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, _networkManager.maxConnections);
    }

    public void ExitLobby() {
        if (currentLobbyID == CSteamID.Nil) {
            Debug.Log("Tried to leave a lobby, but we are not in a lobby");
            return;
        }
        SteamMatchmaking.LeaveLobby(currentLobbyID);
        currentLobbyID = CSteamID.Nil;
    }

    public void JoinLobby(ulong steamIDLobby) {
        SteamMatchmaking.JoinLobby(new CSteamID(steamIDLobby));
    }

    public void GetAllOpenLobbies() {
        // SteamMatchmakingServers.RequestInternetServerList()    // TODO probably something like this
    }

    private void OnLobbyConnected(LobbyCreated_t callback) {
        if (callback.m_eResult != EResult.k_EResultOK) {
            // TODO error handling
            Debug.Log("Lobby failed to get created! Error: "+ callback.m_eResult);
            return;
        }
        
        // Tell Mirror to start the host
        _networkManager.StartHost();
        
        // Give Steam our lobby info
        SteamMatchmaking.SetLobbyData(
            new CSteamID(callback.m_ulSteamIDLobby),         // The lobby
            HostAddressKey,                                 // Key
            SteamUser.GetSteamID().ToString());    // Our Steam ID
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback) {
        // TODO error handling

        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback) {
        // TODO error handling

        if (NetworkServer.active) {
            // We are hosting and just joined ourself - no need to do anything
            return;
        }

        // Tell Mirror to connect to the host
        currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        string hostAddress = SteamMatchmaking.GetLobbyData(
            currentLobbyID,
            HostAddressKey);
        _networkManager.networkAddress = hostAddress;
        _networkManager.StartClient();
    }
}
