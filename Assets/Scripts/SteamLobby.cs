using Game.Network;
using Mirror;
using Steamworks;
using UnityEngine;

public class SteamLobby : MonoBehaviour {
    [SerializeField] private GameNetworkManager _networkManager;

    private const string HostAddressKey = "HostAddress";

    protected Callback<LobbyCreated_t> _lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> _gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> _lobbyEntered;

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
        string hostAddress = SteamMatchmaking.GetLobbyData(
            new CSteamID(callback.m_ulSteamIDLobby),
            HostAddressKey);
        _networkManager.networkAddress = hostAddress;
        _networkManager.StartClient();
    }
}
