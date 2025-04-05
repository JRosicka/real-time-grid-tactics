using System;
using System.Linq;
using Game.Network;
using Mirror;
using Sirenix.Utilities;
using Steamworks;
using UnityEngine;

/// <summary>
/// Interface to work with SteamWorks.NET to interface with Steam lobbies. This includes hosting, joining, searching,
/// and updating metadata. 
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
    /// Represents a steam user inside of a <see cref="Lobby"/>
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
        public int MemberLimit;
        public LobbyMember[] Members;
        public LobbyMetaData[] Data;

        /// <summary>
        /// Retrieves the metadata for the given key, if that metadata exists
        /// </summary>
        public string this[string key] => Data.FirstOrDefault(data => data.Key.Equals(key)).Value;
    }

    public Transport SteamTransport;
    [SerializeField] private GameNetworkManager _networkManager;

    private const string ArbitraryStaticID = "ArbitraryStaticID";
    private const string GameVersionKey = "GameVersionKey";
    private const string HostAddressKey = "HostAddress";
    public const string LobbyUIDKey = "LobbyUID";
    public const string LobbyIsOpenKey = "LobbyIsOpen";
    public const string LobbyGameActiveKey = "GameIsActive";
    public const string LobbyOwnerKey = "LobbyOwner";

    /// <summary>
    /// Metadata keys for <see cref="LobbyMember"/>s in a <see cref="Lobby"/>
    /// </summary>
    public enum LobbyMemberData {
        Faction,
        PlayerType,
        Color
    }

    public bool SteamEnabled => gameObject.activeSelf;
    
    private CallResult<LobbyCreated_t> _lobbyCreated;                      // When creating a lobby
    private CallResult<LobbyMatchList_t> _lobbyMatchList;                  // When getting a list of lobbies
    // private Callback<GameLobbyJoinRequested_t> _lobbyJoinRequested;        // When a player direct joins a lobby (not a game) // TODO when does this trigger compared to GameRichPresenceJoinRequested_t ?
    private Callback<GameRichPresenceJoinRequested_t> _gameJoinRequested;   // When a player direct joins a game (not a lobby)
    private CallResult<LobbyEnter_t> _lobbyEntered;                        // At the entrance to the lobby
    // private CallResult<LobbyChatMsg_t> _lobbyChatMessage;               // When you receive a message in the lobby
    // private CallResult<LobbyChatUpdate_t> _lobbyChatUpdate;             // When changing the list of players in the lobby
    private Callback<LobbyDataUpdate_t> _lobbyDataUpdate;                  // When you update metadata in the lobby

    private bool _isCurrentlyCreatingLobby;
    private bool _lobbyOpenToEveryone;
    private bool _isCurrentlyRequestingLobbies;
    private CSteamID _currentIDForLobbyBeingJoined;
    private bool _isConnectingToLobby;

    private Action<uint, bool, string> _onLobbiesReturned;
    public CSteamID CurrentLobbyID;

    public static SteamLobbyService Instance { get; private set; }

    // Events
    /// <summary>The lobby that we tried to host has succeeded or failed to get created</summary>
    public event Action<bool> OnLobbyCreationComplete;
    /// <summary>We have either succeeded for failed to join a lobby</summary>
    public event Action<bool, string> OnLobbyJoinComplete;
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
        _currentIDForLobbyBeingJoined = CSteamID.Nil;
        _isConnectingToLobby = false;
        _isCurrentlyRequestingLobbies = false;

        Transport.activeTransport = SteamTransport;
    }

    private void SetupCallbacks() {
        _lobbyCreated = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
        _lobbyMatchList = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchListReturned);
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
            OnLobbyCreationComplete?.Invoke(false);
            return;
        }

        // Tell Mirror to start the host
        _networkManager.StartHost();

        CurrentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);

        // Give Steam our lobby info
        SteamMatchmaking.SetLobbyData(
            CurrentLobbyID,                                 // The lobby
            HostAddressKey,                                 // Key
            SteamUser.GetSteamID().ToString());     // Our Steam ID
        
        // Assign a UID
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, LobbyUIDKey, GenerateUniqueID());
        
        // Assign lobby availability (public or private)
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, LobbyIsOpenKey, _lobbyOpenToEveryone.ToString());
        
        // Assign lobby owner (us)
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, LobbyOwnerKey, SteamFriends.GetPersonaName());

        // Assign the arbitrary ID so that we can identify lobbies we create
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, ArbitraryStaticID, ArbitraryStaticID);

        // Assign the game version
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, GameVersionKey, Application.version);

        // Assign the game-active status (false since we just started this lobby)
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, LobbyGameActiveKey, false.ToString());
        
        _isCurrentlyCreatingLobby = false;
        _lobbyOpenToEveryone = false;
        OnLobbyCreationComplete?.Invoke(true);
    }
    
    /// <summary>
    /// Create and pretty-fy a unique string ID
    /// </summary>
    private static string GenerateUniqueID() {
        // TODO: If two users create lobbies at the exact same tick, then they will have the same IDs. Probably won't happen, but this definitely isn't ideal.
        long ticks = DateTime.Now.Ticks;
        byte[] bytes = BitConverter.GetBytes(ticks);
        string uid = Convert.ToBase64String(bytes)
            .Replace('+', '0')    // Bad
            .Replace('/', '1')    // Also bad
            .Replace('-', '2')    // Yup, also bad
            .TrimEnd('=');
        uid = uid.Substring(0, 8);
        uid = uid.Substring(0, 4) + "-" + uid.Substring(4, 4);
        return uid;
    }

    #endregion
    
    #region Request Lobbies
    
    /// <summary>
    /// Request a particular lobby by filtering lobbies for one with the specified joinID in its metadata.
    /// Can fail.
    /// Takes a while to complete - listen for the passed-in action for completion.
    /// </summary>
    public void RequestLobbyByID(string joinID, Action<Lobby, bool, string> onLobbyProcessed = null) {
        if (_isCurrentlyRequestingLobbies) {
            Debug.LogError("Failed to request lobby because we are already performing a request");
            return;
        }

        _isCurrentlyRequestingLobbies = true;
        
        // Only allow the lobby with the specified ID to be retrieved
        SteamMatchmaking.AddRequestLobbyListResultCountFilter(1);
        SteamMatchmaking.AddRequestLobbyListStringFilter(LobbyUIDKey, joinID, ELobbyComparison.k_ELobbyComparisonEqual);
        
        GetAllOpenLobbies((lobbyCount, success, failureMessage) => {
            if (!success) {
                Debug.LogError("Failed to request lobby by ID!");
                onLobbyProcessed?.Invoke(new Lobby(), false, failureMessage);
                return;
            } 
            if (lobbyCount == 0) {
                Debug.Log("Failed to find a lobby");
                onLobbyProcessed?.Invoke(new Lobby(), false, "Lobby with given ID is not joinable or does not exist.");
                return;
            } 
            Debug.Log($"Retrieved {lobbyCount} lobbies when searching via direct join with ID {joinID}");
            GetLobbyData(SteamMatchmaking.GetLobbyByIndex(0), lobby => {
                Debug.Log($"Processed lobby for id {joinID}. Joining...");
                onLobbyProcessed?.Invoke(lobby, true, null);
            });
        });
    }
    
    /// <summary>
    /// Requests all public lobbies. If filters should be applied, then do so before calling this method.
    /// Can fail.
    /// Takes a while to complete - listen for the passed-in action for completion.
    /// </summary>
    public void GetAllOpenLobbies(Action<uint, bool, string> onLobbiesReturned) {
        if (!SteamEnabled) return;

        if (SteamMatchmakingServers.IsRefreshing(_currentServerListRequest)) {
            Debug.LogError("Attempted to retrieve lobby list when we are already requesting lobbies!");
            _isCurrentlyRequestingLobbies = false;
            return;
        }

        _onLobbiesReturned = onLobbiesReturned;

        // Only return lobbies with our arbitrary ID that we assign to all lobbies. This prevents lobbies from other developers to be returned (in the case of using the test Steam app).
        SteamMatchmaking.AddRequestLobbyListStringFilter(ArbitraryStaticID, ArbitraryStaticID, ELobbyComparison.k_ELobbyComparisonEqual);
        // Only return lobbies with the same game version.
        SteamMatchmaking.AddRequestLobbyListStringFilter(GameVersionKey, Application.version, ELobbyComparison.k_ELobbyComparisonEqual);
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
            _onLobbiesReturned?.Invoke((uint)0, false, "Error retrieving lobby list from Steam.");
            _onLobbiesReturned = null;
            return;
        }
        uint lobbyCount = callback.m_nLobbiesMatching;
        Debug.Log($"Found {callback.m_nLobbiesMatching} lobbies");
        
        _isCurrentlyRequestingLobbies = false;

        _onLobbiesReturned?.Invoke(lobbyCount, true, null);
        _onLobbiesReturned = null;
    }

    #endregion
    
    #region Join Lobby
    
    /// <summary>
    /// Joins a lobby, looking it up by CSteamID.
    /// Takes a while to complete - listen for <see cref="OnLobbyJoinComplete"/> for result. 
    /// </summary>
    /// <param name="lobbyID"></param>
    public void JoinLobby(CSteamID lobbyID) {
        if (_currentIDForLobbyBeingJoined != CSteamID.Nil || _isConnectingToLobby || _isCurrentlyCreatingLobby) {
            Debug.LogError("Failed to join lobby because we are already trying to join another lobby (or creating one)!");
            return;
        }

        _currentIDForLobbyBeingJoined = lobbyID;
        
        RequestLobbyData(lobbyID, lobby => {
            // Check to see if the lobby is actually joinable
            VerifyLobby(lobby, (isJoinable, failMessage) => {
                if (isJoinable) {
                    _lobbyEntered.Set(SteamMatchmaking.JoinLobby(lobbyID));
                }
                
                OnLobbyJoinComplete?.Invoke(isJoinable, failMessage);
            });
        });
    }
    
    /// <summary>
    /// When joining a lobby (not a game) directly through Steam
    /// </summary>
    private void OnLobbyJoinRequestedThroughSteamAPI(GameLobbyJoinRequested_t callback) {
        Debug.Log($"{nameof(OnLobbyJoinRequestedThroughSteamAPI)} - attempting to join lobby");
        JoinLobby(callback.m_steamIDLobby);
    }

    /// <summary>
    /// Determine whether the passed-in lobby is actually eligible to be joined.
    /// This takes a while to complete - listen for the passed in action to be triggered.
    /// </summary>
    private void VerifyLobby(Lobby lobby, Action<bool, string> onComplete) {
        // Check to see if the lobby is full
        if (lobby.Members.Length >= lobby.MemberLimit) {
            Debug.Log("Trying to join a lobby that is currently full, aborting");
            _currentIDForLobbyBeingJoined = CSteamID.Nil;
            onComplete?.Invoke(false, "Lobby is full.");
        }
        // Check to see if the lobby's game has started
        if (Convert.ToBoolean(lobby[LobbyGameActiveKey])) {
            Debug.Log("Trying to join a lobby that is currently in a game, aborting");
            _currentIDForLobbyBeingJoined = CSteamID.Nil;
            onComplete?.Invoke(false, "Lobby is in the middle of a game.");
        }
        // Look up the lobby by join ID to see if it is still retrievable - if not, it probably doesn't exist
        RequestLobbyByID(lobby[LobbyUIDKey], (newLobby, success, failureString) => {
            if (!success) {
                Debug.Log("Trying to join a lobby that is no longer valid, aborting");
                _currentIDForLobbyBeingJoined = CSteamID.Nil;
                onComplete?.Invoke(false, "Lobby no longer exists");
            } else {
                // Success!
                onComplete?.Invoke(true, "");
            }
        });
    }

    private void OnLobbyEntered(LobbyEnter_t callback, bool bIoFailure) {
        if (bIoFailure) {
            Debug.LogError("Error requesting to join lobby");
            _currentIDForLobbyBeingJoined = CSteamID.Nil;
            OnLobbyJoinComplete?.Invoke(false, "Unknown error when attempting to join.");
            return;
        }
        
        if (NetworkServer.active) {
            // We are hosting and just joined ourself - no need to do anything
            _currentIDForLobbyBeingJoined = CSteamID.Nil;
            OnLobbyJoinComplete?.Invoke(true, null);
            return;
        }
        
        CurrentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        
        // Get the host address key
        string hostAddress = SteamMatchmaking.GetLobbyData(
            CurrentLobbyID,
            HostAddressKey);
        if (hostAddress.IsNullOrWhitespace()) {
            Debug.LogError("No host address data found when trying to enter lobby, aborting");
            _currentIDForLobbyBeingJoined = CSteamID.Nil;
            OnLobbyJoinComplete?.Invoke(false, "No host address found when attempting to enter lobby.");
            return;
        }
        
        // TODO: Maybe trigger an event that the menu is subscribed to so that we can put up a "joining lobby" screen

        // Tell Mirror to connect to the host
        _networkManager.networkAddress = hostAddress;
        _networkManager.StartClient();
        
        _currentIDForLobbyBeingJoined = CSteamID.Nil;
        _isConnectingToLobby = true;
        OnLobbyJoinComplete?.Invoke(true, null);
    }
    
    #endregion

    private event Action<Lobby> _onLobbyDataReceived;
    // TODO I left off refactoring here
    /// <summary>
    /// Get info for the passed-in lobby.
    /// Takes a while to complete - listen for the passed-in action to trigger.
    /// </summary>
    /// <param name="lobbyID"></param>
    /// <param name="onLobbyDataReceived"></param>
    private void RequestLobbyData(CSteamID lobbyID, Action<Lobby> onLobbyDataReceived) {
        _onLobbyDataReceived += onLobbyDataReceived;
        SteamMatchmaking.RequestLobbyData(lobbyID);
    }
    
    #region Process Returned Lobbies
    
    public void ProcessReturnedLobbies(uint lobbyCount, bool success, string failureMessage) {
        if (!success) {
            return;
        }
        // We use the lobbyCount to iterate through the list of lobbies that just got cached in SteamMatchmaking. 
        // A maximum of 50 lobbies can be returned from this. That's probably fine. Probably. TODO I should really address that with filters or something
        for (int i = 0; i < lobbyCount; i++) {
            GetLobbyData(SteamMatchmaking.GetLobbyByIndex(i), lobby => OnLobbyEntryConstructed?.Invoke(lobby));
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

        int metaDataKeyCount = Enum.GetValues(typeof(LobbyMemberData)).Length;
        
        // Get lobby member IDs and metadata
        for (int i = 0; i < memberCount; i++) {
            // TODO is it okay that this only works when we are in the lobby?
            CSteamID lobbyMember = SteamMatchmaking.GetLobbyMemberByIndex(lobbyID, i);
            lobby.Members[i] = new LobbyMember {
                SteamID = lobbyMember,
                Data = new LobbyMetaData[metaDataKeyCount]
            };
            for (int j = 0; j < metaDataKeyCount; j++) {
                string metadataKey = ((LobbyMemberData) j).ToString();
                lobby.Members[i].Data[j] = new LobbyMetaData {
                    Key = metadataKey,
                    Value = SteamMatchmaking.GetLobbyMemberData(lobbyID, lobbyMember, metadataKey)
                };
            }
        }
        
        onLobbyProcessed?.Invoke(lobby);
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

        if (_onLobbyDataReceived != null) {
            Lobby lobby = GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), null);
            _onLobbyDataReceived?.Invoke(lobby);
            _onLobbyDataReceived = null;
        }
        
        OnCurrentLobbyMetadataChanged?.Invoke();
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
