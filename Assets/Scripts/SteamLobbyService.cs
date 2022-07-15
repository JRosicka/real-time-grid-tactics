using System;
using Game.Network;
using Mirror;
using Steamworks;
using UnityEngine;

/// <summary>
/// Interface to work with SteamWorks.NET to host, join, and search for lobbies
/// </summary>
public class SteamLobbyService : MonoBehaviour {
    /// <summary>
    /// Represents a single piece of data about a steam user or lobby
    /// </summary>
    public struct LobbyMetaData {
        public string Key;
        public string Value;
    }

    /// <summary>
    /// Represents a steam user inside of a lobby
    /// </summary>
    public struct LobbyMember {
        public CSteamID SteamID;
        public LobbyMetaData[] Data;
    }

    /// <summary>
    /// Represents a steam lobby
    /// </summary>
    public struct Lobby {
        public CSteamID SteamID;
        public CSteamID Owner;
        public LobbyMember[] Members;
        public int MemberLimit;
        public LobbyMetaData[] Data;
    }

    [SerializeField] private GameNetworkManager _networkManager;

    private const string HostAddressKey = "HostAddress";
    private const string LobbyUIDKey = "LobbyUID";

    private CallResult<LobbyCreated_t> _lobbyCreated;                        // When creating a lobby
    private CallResult<LobbyMatchList_t> _lobbyMatchList;                  // When receiving the list of lobbies
    private Callback<GameLobbyJoinRequested_t> _gameLobbyJoinRequested;  // When a player direct joins a lobby (not a game)
    private CallResult<LobbyEnter_t> _lobbyEntered;                        // At the entrance to the lobby
    // private CallResult<LobbyChatMsg_t> _lobbyChatMessage;               // When you receive a message in the lobby
    // private CallResult<LobbyChatUpdate_t> _lobbyChatUpdate;             // When changing the list of players in the lobby
    // private CallResult<LobbyDataUpdate_t> _lobbyDataUpdate;             // When you update metadata in the lobby

    private bool _isCurrentlyCreatingLobby;
    private bool _isCurrentlyRequestingLobbies;
    private bool _isCurrentlyJoiningLobby;

    private Action<uint, bool> _onLobbiesReturned;
    private CSteamID _currentLobbyID;

    // Events
    public event Action<bool> OnLobbyCreationComplete;
    public event Action<bool> OnLobbyJoinComplete;
    public event Action<Lobby> OnLobbyEntryConstructed;
    
    private void Start() {
        if (!SteamManager.Initialized) {
            Debug.LogError("SteamManager is not yet initialized!");
            return;
        }

        _lobbyCreated = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
        _lobbyMatchList = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchListReturned);
        _gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        _lobbyEntered = CallResult<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    #region Host Lobby
    
    /// <summary>
    /// Create a new lobby. Listen for <see cref="OnLobbyCreationComplete"/> for the result. 
    /// </summary>
    public void HostLobby() {
        if (_isCurrentlyCreatingLobby) {
            Debug.LogError("Trying to create a lobby, but we are already creating one!");
            return;
        }

        _isCurrentlyCreatingLobby = true;
        // Tell Steam to create a lobby, then wait for callbacks to trigger
        _lobbyCreated.Set(SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, _networkManager.maxConnections));
    }
    
    private void OnLobbyCreated(LobbyCreated_t callback, bool bIoFailure) {
        if (bIoFailure || callback.m_eResult != EResult.k_EResultOK) {
            Debug.LogError("Lobby failed to get created! Error: " + callback.m_eResult);
            _isCurrentlyCreatingLobby = false;
            OnLobbyCreationComplete.SafeInvoke(false);
            return;
        }

        // Tell Mirror to start the host
        _networkManager.StartHost();

        CSteamID lobbyID = new CSteamID(callback.m_ulSteamIDLobby);

        // Give Steam our lobby info
        SteamMatchmaking.SetLobbyData(
            lobbyID,                            // The lobby
            HostAddressKey,                     // Key
            SteamUser.GetSteamID().ToString()); // Our Steam ID
        
        // Assign a UID
        SteamMatchmaking.SetLobbyData(lobbyID, LobbyUIDKey, GenerateUniqueID());

        // TODO can assign any other metadata like the map it's on, description, password

        _isCurrentlyCreatingLobby = false;
        OnLobbyCreationComplete.SafeInvoke(true);
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

    #endregion
    
    #region Request Lobbies
    
    /// <summary>
    /// Request a particular lobby by filtering lobbies for one with the specified joinID in its metadata.
    /// Can fail.
    /// </summary>
    public void RequestLobbyByID(string joinID, Action<Lobby, bool> onLobbyProcessed = null) {
        if (_isCurrentlyRequestingLobbies) {
            Debug.LogError("Failed to request lobby because we are already performing a request");
            return;
        }

        _isCurrentlyRequestingLobbies = true;
        
        SteamMatchmaking.AddRequestLobbyListResultCountFilter(1);
        SteamMatchmaking.AddRequestLobbyListStringFilter(LobbyUIDKey, joinID, ELobbyComparison.k_ELobbyComparisonEqual);
        
        GetAllOpenLobbies((lobbyCount, success) => {
            if (!success) {
                Debug.LogError("Failed to request lobby by ID!");
                onLobbyProcessed.SafeInvoke(new Lobby(), false);
                return;
            }
            Debug.Log($"Retrieved {lobbyCount} lobbies when searching via direct join with ID {joinID}");
            ProcessReturnedLobby(SteamMatchmaking.GetLobbyByIndex(0), lobby => {
                Debug.Log($"Processed lobby for id {joinID}. Joining...");
                onLobbyProcessed.SafeInvoke(lobby, true);
            });
        });
    }
    
    /// <summary>
    /// Requests all public lobbies. If filters should be applied, then do so before calling this method.
    /// Can fail.
    /// </summary>
    public void GetAllOpenLobbies(Action<uint, bool> onLobbiesReturned = null) {
        if (SteamMatchmakingServers.IsRefreshing(_currentServerListRequest)) {
            Debug.LogError("Attempted to retrieve lobby list when we are already requesting lobbies!");
            _isCurrentlyRequestingLobbies = false;
            return;
        }

        // Use the default lobby processing if we do not specify a different one
        _onLobbiesReturned = onLobbiesReturned ?? ProcessReturnedLobbies;

        SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
        _lobbyMatchList.Set(SteamMatchmaking.RequestLobbyList());
    }
    
    private void OnLobbyMatchListReturned(LobbyMatchList_t callback, bool bIoFailure) {
        if (bIoFailure) {
            Debug.LogError("Error getting lobby list");
            _isCurrentlyRequestingLobbies = false;
            _onLobbiesReturned.SafeInvoke((uint)0, false);
            _onLobbiesReturned = null;
            return;
        }
        uint lobbyCount = callback.m_nLobbiesMatching;
        Debug.Log($"Found {callback.m_nLobbiesMatching} lobbies");
        
        _isCurrentlyRequestingLobbies = false;

        _onLobbiesReturned.SafeInvoke(lobbyCount, true);
        _onLobbiesReturned = null;
    }

    #endregion
    
    #region Join Lobby
    
    /// <summary>
    /// Joins a lobby, looking it up by CSteamID. Listen for <see cref="OnLobbyJoinComplete"/> for result. 
    /// </summary>
    /// <param name="lobbyID"></param>
    public void JoinLobby(CSteamID lobbyID) {
        if (_isCurrentlyJoiningLobby) {
            Debug.LogError("Failed to join lobby because we are already trying to join another lobby!");
            return;
        }

        _isCurrentlyJoiningLobby = true;
        _lobbyEntered.Set(SteamMatchmaking.JoinLobby(lobbyID));
    }
    
    /// <summary>
    /// When joining a lobby (not a game) directly through Steam
    /// </summary>
    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback) {
        // if (bIoFailure) {
        //     Debug.LogError("Error requesting to join lobby");
        //     OnLobbyJoinComplete.SafeInvoke(false);
        //     return;
        // }    // TODO I don't think I can really do any error handling with this one

        _lobbyEntered.Set(SteamMatchmaking.JoinLobby(callback.m_steamIDLobby));
    }

    private void OnLobbyEntered(LobbyEnter_t callback, bool bIoFailure) {
        if (bIoFailure) {
            Debug.LogError("Error requesting to join lobby");
            _isCurrentlyJoiningLobby = false;
            OnLobbyJoinComplete.SafeInvoke(false);
            return;
        }

        if (NetworkServer.active) {
            // We are hosting and just joined ourself - no need to do anything
            _isCurrentlyJoiningLobby = false;
            OnLobbyJoinComplete.SafeInvoke(true);
            return;
        }

        _isCurrentlyJoiningLobby = false;
        
        // Tell Mirror to connect to the host
        _currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        string hostAddress = SteamMatchmaking.GetLobbyData(
            _currentLobbyID,
            HostAddressKey);
        _networkManager.networkAddress = hostAddress;
        _networkManager.StartClient();
        
        OnLobbyJoinComplete.SafeInvoke(true);
    }

    #endregion

    #region Process Returned Lobbies
    
    private void ProcessReturnedLobbies(uint lobbyCount, bool success) {
        if (!success) {
            return;
        }
        // We use the lobbyCount to iterate through the list of lobbies that just got cached in SteamMatchmaking. 
        // A maximum of 50 lobbies can be returned from this. That's probably fine. Probably. TODO I should really address that with filters or something
        for (int i = 0; i < lobbyCount; i++) {
            ProcessReturnedLobby(SteamMatchmaking.GetLobbyByIndex(i), lobby => OnLobbyEntryConstructed.SafeInvoke(lobby));
        }
    }

    /// <summary>
    /// Constructs a new <see cref="Lobby"/> by querying Steam for relevant info using the provided lobby ID
    /// Always succeeds.
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
    
    #endregion
    
    // TODO callback for this?
    public void ExitLobby() {
        if (_currentLobbyID == CSteamID.Nil) {
            Debug.Log("Tried to leave a lobby, but we are not in a lobby");
            return;
        }

        SteamMatchmaking.LeaveLobby(_currentLobbyID);
        _currentLobbyID = CSteamID.Nil;
    }

    // TODO periodically call RefreshServer on the request
    private HServerListRequest _currentServerListRequest;

    /// <summary>
    /// Cancel any active server query and make a new one
    /// </summary>
    public void RefreshServerList() {
        // TODO call
        SteamMatchmakingServers.ReleaseRequest(_currentServerListRequest);
        GetAllOpenLobbies();
    }
}
