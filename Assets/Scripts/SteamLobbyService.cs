using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Game.Network;
using Mirror;
using Sirenix.Utilities;
using Steamworks;
using Unity.VisualScripting;
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
        // public CSteamID Owner;
        public LobbyMember[] Members;
        public int MemberLimit;
        public LobbyMetaData[] Data;

        public string this[string key] => Data.FirstOrDefault(data => data.Key.Equals(key)).Value;
    }

    public Transport SteamTransport;
    [SerializeField] private GameNetworkManager _networkManager;

    private const string ArbitraryStaticID = "ArbitraryStaticID";
    private const string HostAddressKey = "HostAddress";
    public const string LobbyUIDKey = "LobbyUID";
    public const string LobbyIsOpenKey = "LobbyIsOpen";
    public const string LobbyGameActiveKey = "LobbyIsOpen";
    public const string LobbyOwnerKey = "LobbyOwner";

    public static readonly string[] LobbyMemberDataKeys = { "faction", "playerType", "color" };

    private CallResult<LobbyCreated_t> _lobbyCreated;                      // When creating a lobby
    private CallResult<LobbyMatchList_t> _lobbyMatchList;                  // When getting a list of lobbies
    private Callback<GameLobbyJoinRequested_t> _gameLobbyJoinRequested;    // When a player direct joins a lobby (not a game)
    private CallResult<LobbyEnter_t> _lobbyEntered;                        // At the entrance to the lobby
    // private CallResult<LobbyChatMsg_t> _lobbyChatMessage;               // When you receive a message in the lobby
    // private CallResult<LobbyChatUpdate_t> _lobbyChatUpdate;             // When changing the list of players in the lobby
    private Callback<LobbyDataUpdate_t> _lobbyDataUpdate;             // When you update metadata in the lobby

    private bool _isCurrentlyCreatingLobby;
    private bool _lobbyOpenToEveryone;
    private bool _isCurrentlyRequestingLobbies;
    private bool _isCurrentlyJoiningLobby;

    private Action<uint, bool> _onLobbiesReturned;
    public CSteamID CurrentLobbyID;

    public static SteamLobbyService Instance { get; private set; }

    // Events
    /// <summary>The lobby that we tried to host has succeeded or failed to get created</summary>
    public event Action<bool> OnLobbyCreationComplete;
    /// <summary>We have either succeeded for failed to join a lobby</summary>
    public event Action<bool> OnLobbyJoinComplete;
    /// <summary>We have gotten the data for a particular lobby that we requested</summary>
    public event Action<Lobby> OnLobbyEntryConstructed;
    /// <summary>The lobby metadata just got updated</summary>
    public event Action OnCurrentLobbyMetadataChanged;

    private void Awake() {
        // Only one instance of SteamLobbyService!
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }

    public void Start() {
        if (!SteamManager.Initialized) {
            Debug.LogError("SteamManager is not yet initialized!");
            return;
        }
        
        SetupCallbacks();
    }

    public void ReInitialize(GameNetworkManager newManager) {
        if (!SteamManager.Initialized) {
            Debug.LogError("SteamManager is not yet initialized!");
            return;
        }

        _networkManager = newManager;
        CurrentLobbyID = CSteamID.Nil;
        _isCurrentlyCreatingLobby = false;
        _isCurrentlyJoiningLobby = false;
        _isCurrentlyRequestingLobbies = false;

        Transport.activeTransport = SteamTransport;
    }

    private void SetupCallbacks() {
        _lobbyCreated = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
        _lobbyMatchList = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchListReturned);
        _gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        _lobbyEntered = CallResult<LobbyEnter_t>.Create(OnLobbyEntered);
        _lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnMetadataUpdated);
    }

    #region Host Lobby
    
    /// <summary>
    /// Create a new lobby. Listen for <see cref="OnLobbyCreationComplete"/> for the result. 
    /// </summary>
    public void HostLobby(bool openToEveryone) {
        if (_isCurrentlyCreatingLobby) {
            Debug.LogError("Trying to create a lobby, but we are already creating one!");
            return;
        }

        _isCurrentlyCreatingLobby = true;
        _lobbyOpenToEveryone = openToEveryone;
        // Tell Steam to create a lobby, then wait for callbacks to trigger
        _lobbyCreated.Set(SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, _networkManager.maxConnections));
    }
    
    private void OnLobbyCreated(LobbyCreated_t callback, bool bIoFailure) {
        if (bIoFailure || callback.m_eResult != EResult.k_EResultOK) {
            Debug.LogError("Lobby failed to get created! Error: " + callback.m_eResult);
            _isCurrentlyCreatingLobby = false;
            _lobbyOpenToEveryone = false;
            OnLobbyCreationComplete.SafeInvoke(false);
            return;
        }

        // Tell Mirror to start the host
        _networkManager.StartHost();

        CurrentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);

        // Give Steam our lobby info
        SteamMatchmaking.SetLobbyData(
            CurrentLobbyID,                            // The lobby
            HostAddressKey,                     // Key
            SteamUser.GetSteamID().ToString()); // Our Steam ID
        
        // Assign a UID
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, LobbyUIDKey, GenerateUniqueID());
        
        // Assign lobby availability (public or private)
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, LobbyIsOpenKey, _lobbyOpenToEveryone.ToString());
        
        // Assign lobby owner (us)
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, LobbyOwnerKey, SteamFriends.GetPersonaName());

        // Assign the arbitrary ID so that we can identify lobbies we create
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, ArbitraryStaticID, ArbitraryStaticID);

        // Assign the game-active status (false since we just started this lobby)
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, LobbyGameActiveKey, false.ToString());
        
        // TODO can assign any other metadata like the map it's on, description, password
        
        _isCurrentlyCreatingLobby = false;
        _lobbyOpenToEveryone = false;
        OnLobbyCreationComplete.SafeInvoke(true);
    }
    
    private string GenerateUniqueID() {
        long ticks = DateTime.Now.Ticks;
        byte[] bytes = BitConverter.GetBytes(ticks);
        string uid = Convert.ToBase64String(bytes)
            .Replace('+', '_')
            .Replace('/', '-')
            .TrimEnd('=');
        uid = uid.Substring(0, 8);
        uid = uid.Substring(0, 4) + "-" + uid.Substring(4, 4);
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
            if (lobbyCount == 0) {
                Debug.Log("Failed to find a lobby");
                onLobbyProcessed.SafeInvoke(new Lobby(), false);
                return;
            } 
            Debug.Log($"Retrieved {lobbyCount} lobbies when searching via direct join with ID {joinID}");
            GetLobbyData(SteamMatchmaking.GetLobbyByIndex(0), lobby => {
                Debug.Log($"Processed lobby for id {joinID}. Joining...");
                onLobbyProcessed.SafeInvoke(lobby, true);
            });
        });
    }
    
    /// <summary>
    /// Requests all public lobbies. If filters should be applied, then do so before calling this method.
    /// Can fail.
    /// </summary>
    public void GetAllOpenLobbies(Action<uint, bool> onLobbiesReturned) {
        if (SteamMatchmakingServers.IsRefreshing(_currentServerListRequest)) {
            Debug.LogError("Attempted to retrieve lobby list when we are already requesting lobbies!");
            _isCurrentlyRequestingLobbies = false;
            return;
        }

        _onLobbiesReturned = onLobbiesReturned;

        // Only return lobbies with our arbitrary ID that we assign to all lobbies. This prevents lobbies from other developers to be returned (in the case of using the test Steam app).
        SteamMatchmaking.AddRequestLobbyListStringFilter(ArbitraryStaticID, ArbitraryStaticID, ELobbyComparison.k_ELobbyComparisonEqual);
        // Only return lobbies that are not actively playing in a game. 
        SteamMatchmaking.AddRequestLobbyListStringFilter(LobbyGameActiveKey, false.ToString(), ELobbyComparison.k_ELobbyComparisonEqual);
        // Worldwide search
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
        
        CurrentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);

        // TODO: We need to call SteamMatchmaking.RequestLobbyData(CSteamID lobbyID) and wait for the LobbyDataUpdate_t callback
        // before calling GetLobbyData, if we really care about doing this check
        // Lobby lobby = GetLobbyData(CurrentLobbyID, null);
        // if (lobby.Members.Length >= lobby.MemberLimit) {
        //     Debug.LogError("Trying to join a lobby that is currently full, aborting");
        //     _isCurrentlyJoiningLobby = false;
        //     OnLobbyJoinComplete.SafeInvoke(false);
        //     return;
        // }        
        
        // Tell Mirror to connect to the host
        string hostAddress = SteamMatchmaking.GetLobbyData(
            CurrentLobbyID,
            HostAddressKey);
        if (hostAddress.IsNullOrWhitespace()) {
            Debug.LogError("No host address data found when trying to enter lobby (perhaps the lobby was full), aborting");
            _isCurrentlyJoiningLobby = false;
            OnLobbyJoinComplete.SafeInvoke(false);
            return;
        }
        
        _isCurrentlyJoiningLobby = false;

        _networkManager.networkAddress = hostAddress;
        _networkManager.StartClient();
        
        OnLobbyJoinComplete.SafeInvoke(true);
    }

    #endregion

    #region Process Returned Lobbies
    
    public void ProcessReturnedLobbies(uint lobbyCount, bool success) {
        if (!success) {
            return;
        }
        // We use the lobbyCount to iterate through the list of lobbies that just got cached in SteamMatchmaking. 
        // A maximum of 50 lobbies can be returned from this. That's probably fine. Probably. TODO I should really address that with filters or something
        for (int i = 0; i < lobbyCount; i++) {
            GetLobbyData(SteamMatchmaking.GetLobbyByIndex(i), lobby => OnLobbyEntryConstructed.SafeInvoke(lobby));
        }
    }

    /// <summary>
    /// Constructs a new <see cref="Lobby"/> by querying Steam for relevant info using the provided lobby ID
    /// Always succeeds.
    /// </summary>
    public Lobby GetLobbyData(CSteamID lobbyID, Action<Lobby> onLobbyProcessed) {
        int dataCount = SteamMatchmaking.GetLobbyDataCount(lobbyID);
        int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyID);
        Lobby lobby = new Lobby {
            SteamID = lobbyID,
            // Owner = SteamMatchmaking.GetLobbyOwner(lobbyID),
            Members = new LobbyMember[memberCount],
            MemberLimit = SteamMatchmaking.GetLobbyMemberLimit(lobbyID),
            Data = new LobbyMetaData[dataCount]
        };

        // Get lobby metadata
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
        
        // Get lobby member IDs and metadata
        for (int i = 0; i < memberCount; i++) {
            // TODO is it okay that this only works when we are in the lobby?
            CSteamID lobbyMember = SteamMatchmaking.GetLobbyMemberByIndex(lobbyID, i);
            lobby.Members[i] = new LobbyMember {
                SteamID = lobbyMember,
                Data = new LobbyMetaData[LobbyMemberDataKeys.Length]
            };
            for (int j = 0; j < LobbyMemberDataKeys.Length; j++) {
                lobby.Members[i].Data[j] = new LobbyMetaData {
                    Key = LobbyMemberDataKeys[j],
                    Value = SteamMatchmaking.GetLobbyMemberData(lobbyID, lobbyMember, LobbyMemberDataKeys[j])
                };
            }
        }
        
        onLobbyProcessed.SafeInvoke(lobby);
        return lobby;
    }
    
    #endregion
    
    #region Update Lobby Metadata

    public void UpdateCurrentLobbyMetadata(string key, string value) {
        // TODO error handling for not being in a lobby
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, key, value);
    }

    /// <summary>
    /// Sets member data for the local user in the current lobby. TODO does this broadcast to all players like the above one does?
    /// </summary>
    public void UpdateCurrentLobbyPlayerMetadata(string key, string value) {
        // TODO error handling for not being in a lobby
        SteamMatchmaking.SetLobbyMemberData(CurrentLobbyID, key, value);
    }

    private void OnMetadataUpdated(LobbyDataUpdate_t callback) {
        // TODO error handling
        if (Convert.ToBoolean(callback.m_bSuccess) == false) {
            
        }
        
        OnCurrentLobbyMetadataChanged.SafeInvoke();
    }
    
    #endregion
    
    // TODO callback for this? Looks like not necessary for the client, and we can notify other players when they listen to LobbyChatUpdate_t
    public void ExitLobby() {
        if (CurrentLobbyID == CSteamID.Nil) {
            Debug.Log("Tried to leave a lobby, but we are not in a lobby");
            return;
        }

        SteamMatchmaking.LeaveLobby(CurrentLobbyID);
        CurrentLobbyID = CSteamID.Nil;
    }

    // TODO periodically call RefreshServer on the request
    private HServerListRequest _currentServerListRequest;

    /// <summary>
    /// Cancel any active server query and make a new one
    /// </summary>
    public void RefreshServerList() {
        // TODO call
        SteamMatchmakingServers.ReleaseRequest(_currentServerListRequest);
        GetAllOpenLobbies(ProcessReturnedLobbies);
    }
}
