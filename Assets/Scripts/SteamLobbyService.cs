using System;
using System.Linq;
using Game.Network;
using Mirror;
using Steamworks;
using UnityEngine;

public class SteamLobbyService : MonoBehaviour {
    [SerializeField] private GameNetworkManager _networkManager;

    private const string HostAddressKey = "HostAddress";
    private const string LobbyUIDKey = "LobbyUID";
    private const uint SteamAppID = 480; // TODO currently just Spacewar

    private Callback<LobbyCreated_t> _lobbyCreated;
    private Callback<GameLobbyJoinRequested_t> _gameLobbyJoinRequested;
    private Callback<LobbyEnter_t> _lobbyEntered;
    private Callback<LobbyMatchList_t> _lobbyMatchList;

    private CSteamID currentLobbyID;

    private void Start() {
        if (!SteamManager.Initialized) // TODO error handling
            return;

        _lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        _gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        _lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        _lobbyMatchList = Callback<LobbyMatchList_t>.Create(OnLobbyMatchListReturned);
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

    public void JoinLobby(CSteamID lobbyID) {
        // if (_isCurrentlyJoiningLobby) {
        //     Debug.LogError("Failed to join lobby because we are already trying to join another lobby!");
        //     return;
        // }    TODO currently this disrupts the matching block in DirectJoinLobby

        // _isCurrentlyJoiningLobby = true;
        SteamMatchmaking.JoinLobby(lobbyID);
    }

    /// <summary>
    /// Directly join a lobby by searching in the list of lobbies for one with the specified joinID in its metadata.
    /// </summary>
    public void DirectJoinLobby(string joinID, Action<Lobby> onLobbyProcessed = null) {
        if (_isCurrentlyJoiningLobby) {
            Debug.LogError("Failed to direct join lobby because we are already trying to join another lobby!");
            return;
        }

        _isCurrentlyJoiningLobby = true;
        
        SteamMatchmaking.AddRequestLobbyListResultCountFilter(1);
        SteamMatchmaking.AddRequestLobbyListStringFilter(LobbyUIDKey, joinID, ELobbyComparison.k_ELobbyComparisonEqual);
        
        GetAllOpenLobbies(lobbyCount => {
            Debug.Log($"Retrieved {lobbyCount} lobbies when searching via direct join with ID {joinID}");
            ProcessReturnedLobby(SteamMatchmaking.GetLobbyByIndex(0), lobby => {
                Debug.Log($"Processed lobby for id {joinID}. Joining...");
                
                onLobbyProcessed.SafeInvoke(lobby);
            });
        });
        
    }

    /// <summary>
    /// Cancel any active server query and make a new one
    /// </summary>
    public void RefreshServerList() {
        // TODO call
        SteamMatchmakingServers.ReleaseRequest(currentServerListRequest);
        GetAllOpenLobbies();
    }

    // TODO periodically call RefreshServer on the request
    private HServerListRequest currentServerListRequest;

    private Action<uint> _onLobbiesReturned;
    private bool _isCurrentlyJoiningLobby; // TODO set this to false if we fail or succeed to join a lobby
    public void GetAllOpenLobbies(Action<uint> onLobbiesReturned = null) {
        if (SteamMatchmakingServers.IsRefreshing(currentServerListRequest)) {
            // TODO error handling
            return;
        }

        // Use the default lobby processing if we do not specify a different one
        _onLobbiesReturned = onLobbiesReturned ?? ProcessReturnedLobbies;

        SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
        SteamMatchmaking.RequestLobbyList();

        // MatchMakingKeyValuePair_t[] searchFilters = new MatchMakingKeyValuePair_t[0]; //[1];
        // // searchFilters[0].m_szKey = "map";
        // // searchFilters[0].m_szValue = "cp_granary";    // Can apply search filters here
        // currentServerListRequest = SteamMatchmakingServers.RequestInternetServerList(
        //     new AppId_t(SteamAppID), 
        //     searchFilters, 
        //     (uint) searchFilters.Length,
        //     new ISteamMatchmakingServerListResponse(OnServerResponded, OnServerFailedToRespond,
        //         OnRefreshComplete));
    }

    private void OnLobbyCreated(LobbyCreated_t callback) {
        if (callback.m_eResult != EResult.k_EResultOK) {
            // TODO error handling
            Debug.Log("Lobby failed to get created! Error: " + callback.m_eResult);
            return;
        }

        // Tell Mirror to start the host
        _networkManager.StartHost();

        CSteamID lobbyID = new CSteamID(callback.m_ulSteamIDLobby);

        // Give Steam our lobby info
        SteamMatchmaking.SetLobbyData(
            lobbyID,                                    // The lobby
            HostAddressKey,                              // Key
            SteamUser.GetSteamID().ToString());   // Our Steam ID
        
        // Assign a UID
        SteamMatchmaking.SetLobbyData(lobbyID, LobbyUIDKey, GenerateUniqueID());

        // TODO can assign any other metadata like the map it's on, description, password
    }
    
    private string GenerateUniqueID() {
        long ticks = DateTime.Now.Ticks;
        byte[] bytes = BitConverter.GetBytes(ticks);
        string uid = Convert.ToBase64String(bytes)
            .Replace('+', '_')
            .Replace('/', '-')
            .TrimEnd('=');

        Debug.Log($"Generated unique ID: {uid}");
        return uid;
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

        _isCurrentlyJoiningLobby = false;
        
        // Tell Mirror to connect to the host
        currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        string hostAddress = SteamMatchmaking.GetLobbyData(
            currentLobbyID,
            HostAddressKey);
        _networkManager.networkAddress = hostAddress;
        _networkManager.StartClient();
    }


    private void OnLobbyMatchListReturned(LobbyMatchList_t callback) {
        uint lobbyCount = callback.m_nLobbiesMatching;
        Debug.Log($"Found {callback.m_nLobbiesMatching} lobbies");
        
        _onLobbiesReturned.SafeInvoke(lobbyCount);
        _onLobbiesReturned = null;
    }

    private void ProcessReturnedLobbies(uint lobbyCount) {
        // We use the lobbyCount to iterate through the list of lobbies that just got cached in SteamMatchmaking. 
        // A maximum of 50 lobbies can be returned from this. That's probably fine. Probably. TODO I should really address that with filters or something
        for (int i = 0; i < lobbyCount; i++) {
            ProcessReturnedLobby(SteamMatchmaking.GetLobbyByIndex(i), lobby => OnLobbyEntryConstructed.SafeInvoke(lobby));
        }
    }

    public struct LobbyMetaData {
        public string Key;
        public string Value;
    }

    public struct LobbyMember {
        public CSteamID SteamID;
        public LobbyMetaData[] Data;
    }

    public struct Lobby {
        public CSteamID SteamID;
        public CSteamID Owner;
        public LobbyMember[] Members;
        public int MemberLimit;
        public LobbyMetaData[] Data;
    }
    
    public event Action<Lobby> OnLobbyEntryConstructed;

    /// <summary>
    /// Constructs a new <see cref="Lobby"/> by querying Steam for relevant info using the provided lobby ID
    /// </summary>
    private void ProcessReturnedLobby(CSteamID lobbyID, Action<Lobby> onLobbyProcessed) {
        int dataCount = SteamMatchmaking.GetLobbyDataCount(lobbyID);
        Lobby lobby = new Lobby {
            SteamID = lobbyID,
            Owner = SteamMatchmaking.GetLobbyOwner(lobbyID),
            Members = new LobbyMember[SteamMatchmaking.GetNumLobbyMembers(lobbyID)],
            MemberLimit = SteamMatchmaking.GetLobbyMemberLimit(lobbyID),
            Data = new LobbyMetaData[dataCount]
        };

        for (int i = 0; i < dataCount; i++) {
            bool lobbyDataRet = SteamMatchmaking.GetLobbyDataByIndex(
                                                    lobbyID, 
                                                    i, 
                                                    out lobby.Data[i].Key,
                                                    Constants.k_nMaxLobbyKeyLength, 
                                                    out lobby.Data[i].Value, 
                                                    Constants.k_cubChatMetadataMax);
            if (!lobbyDataRet) {
                Debug.LogError($"Error retrieving lobby metadata for index {i}. Lobby ID: {lobbyID}");
                continue;
            }
        }
        
        onLobbyProcessed.SafeInvoke(lobby);
    }

    // private void OnRefreshComplete(HServerListRequest hrequest, EMatchMakingServerResponse response) {
    //     if (response == EMatchMakingServerResponse.eServerFailedToRespond) {
    //         // TODO error handling
    //         return;
    //     }
    //
    //     if (response == EMatchMakingServerResponse.eNoServersListedOnMasterServer) {
    //         // TODO "No servers found" UI
    //         Debug.Log("No servers found when querying steam servers");
    //         return;
    //     }
    //     
    //     int serverCount = SteamMatchmakingServers.GetServerCount(hrequest);
    //     for (int i = 0; i < serverCount; i++) {
    //         gameserveritem_t serverDetails = SteamMatchmakingServers.GetServerDetails(hrequest, i);
    //         Debug.Log($"Server [{i}]: Name: {serverDetails.GetServerName()}, Description: {serverDetails.GetGameDescription()}, player count: {serverDetails.m_nPlayers}, has password: {serverDetails.m_bPassword}");
    //     }
    // }
    //
    // private void OnServerFailedToRespond(HServerListRequest hrequest, int iserver) {
    //     // TODO error handling
    //     Debug.Log("Failed to get response from Steam servers when requesting server list");
    // }
    //
    // private void OnServerResponded(HServerListRequest hrequest, int iserver) {
    //     // TODO: So for some reason the list of servers we get when polling for the Spacewar app contains what appears to be ~3000 tf2 servers. Huh. Just return the first 100 of those for now. We can return all of them once I switch to the proper app ID. 
    //     int serverCount = Mathf.Min(SteamMatchmakingServers.GetServerCount(hrequest), 100);
    //     for (int i = 0; i < serverCount; i++) {
    //         gameserveritem_t serverDetails = SteamMatchmakingServers.GetServerDetails(hrequest, i);
    //         Debug.Log($"Server [{i}]: Name: {serverDetails.GetServerName()}, Description: {serverDetails.GetGameDescription()}, player count: {serverDetails.m_nPlayers}, has password: {serverDetails.m_bPassword}");
    //     }
    // }
}
