using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Game.Network;
using Gameplay.Config;
using Gameplay.Entities;
using Gameplay.UI;
using Mirror;
using Scenes;
using Steamworks;
using UnityEngine;

/// <summary>
/// Handles setup for a game - figuring out if it's SP or MP, waiting for all players to connect and be ready, and
/// controlling the flow of game-start logic in the case of MP games.
/// </summary>
public class GameSetupManager : MonoBehaviour {
    public MultiplayerGameSetupHandler MPSetupHandler;
    public MapLoader MapLoader;
    public CountdownTimerView CountdownTimer;
    public StartLocationIndicator StartLocationIndicator;
    public GameOverView GameOverView;
    [SerializeField] private InGamePauseMenu _pauseMenu;

    [Header("Prefabs")]
    public SPGamePlayer SPGamePlayerPrefab;
    public MPGamePlayer MPGamePlayerPrefab;
    public MPCommandManager MPCommandManagerPrefab;
    public SPCommandManager SPCommandManagerPrefab;

    [Header("Data")]
    public PlayerData Player1Data;
    public PlayerData Player2Data;
    public PlayerData SpectatorData;
    public float CountdownTimeSeconds = 3f;
    public int GameOverDelayMillis = 5 * 1000;
    private MapData MapData => MapSerializer.GetMap(MapLoader.CurrentMapID);
    
    private static GameManager GameManager => GameManager.Instance;
    
    // Only assigned on server, and only for MP
    public readonly Dictionary<int, MPGamePlayer> AllPlayers = new Dictionary<int, MPGamePlayer>();
    // Only assigned on server, and only for MP
    private readonly List<MPGamePlayer> _spectatorPlayers = new List<MPGamePlayer>();
    public int LocalPlayerIndex;
    // The total number of players in this game who have arrived in the game scene
    private int _readyPlayerCount;
    // The total number of players in this game who have had their gameobjects assigned
    private int _assignedPlayerCount;
    // Whether the game was initialized on the server
    private bool _gameInitialized;
    public bool GameInitialized {
        get {
            if (GameNetworkStateTracker.Instance.GameIsNetworked) {
                // MP
                return MPSetupHandler.GameInitialized;
            }
            return _gameInitialized;
        }
        private set {
            _gameInitialized = value;
            if (GameNetworkStateTracker.Instance.HostForNetworkedGame) {
                MPSetupHandler.GameInitialized = value;
            } else if (value) {
                TriggerGameInitializedEvent();
            }
        }
    }
    
    public bool InputAllowed => !_pauseMenu.Paused && GameInitialized && !GameOver && !GameManager.DisconnectionHandler.Disconnected;

    public event Action GameInitializedEvent;
    public void TriggerGameInitializedEvent() {
        GameInitializedEvent?.Invoke();
    }

    // Server event
    public event Action PlayerDisconnected;
    
    public bool GameOver { get; private set; }

    public void Initialize() {
        if (!GameNetworkStateTracker.Instance.GameIsNetworked) {
            SetupSPGame();
        } else {
            CountdownTimer.ShowLoadingStatus();
            if (!GameNetworkStateTracker.Instance.HostForNetworkedGame) {
                // This is a client and not the host. Listen for all player objects getting created so that we can tell the server that we are ready.
                StartCoroutine(ListenForPlayersConnected());
            }
        }
        
        GameManager.GameEndManager.GameEnded += HandleGameOver;
    }
    
    // TODO it would be good to move all of this game over logic to GameEndManager, but we currently can't do server-side logic in that class
    private void HandleGameOver(IGamePlayer winner) {
        GameNetworkManager gameNetworkManager = (GameNetworkManager)NetworkManager.singleton;
        if (gameNetworkManager != null) {
            gameNetworkManager.RoomServerDisconnectAction -= ReDetermineWhichPlayersAreConnected;
        }

        GameTeam winningTeam = winner == null ? GameTeam.Neutral : winner.Data.Team;
        if (!GameNetworkStateTracker.Instance.GameIsNetworked) {
            ReturnToLobbyAsync();
            NotifyGameOver(winningTeam);
        } else {
            MPSetupHandler.CmdGameOver(winningTeam);
        }
    }

    private void OnDestroy() {
        GameNetworkManager gameNetworkManager = (GameNetworkManager)NetworkManager.singleton;
        if (gameNetworkManager != null) {
            gameNetworkManager.RoomServerDisconnectAction -= ReDetermineWhichPlayersAreConnected;
        }
    } 
    
    /// <summary>
    /// Server/SP logic for ending the game. Halts commands and returns to lobby shortly. (SP will just reload the game scene)
    /// </summary>
    public async void ReturnToLobbyAsync() {
        await Task.Delay(GameOverDelayMillis);
        GameManager?.ReturnToLobby();
    }

    /// <summary>
    /// Client/SP logic for ending the game
    /// </summary>
    /// <param name="winner"></param>
    public void NotifyGameOver(GameTeam winner) {
        if (GameOver) return;
        GameOver = true;
        
        GameManager.Instance.GameAudio.EndMusic(true);
        
        if (winner == GameTeam.Neutral) {
            GameOverView.ShowTie();
            // TODO tie sound effect?
        } else if (GameManager.LocalTeam == GameTeam.Spectator) {
            GameOverView.ShowSpectatorThatPlayerWon(GameManager.GetPlayerForTeam(winner));
            GameManager.Instance.GameAudio.GameWinSound();
        } else if (winner == GameManager.LocalTeam) {
            GameOverView.ShowVictory();
            GameManager.Instance.GameAudio.GameWinSound();
        } else {
            GameOverView.ShowDefeat();
            GameManager.Instance.GameAudio.GameLossSound();
        }
    }

    private void SpawnStartingUnits() {
        // Spawn neutrals first so that resource patches go on the bottom of the stack
        SpawnPlayerStartingUnits(GameTeam.Neutral);
        
        // Then spawn player starting units
        SpawnPlayerStartingUnits(Player1Data.Team);
        SpawnPlayerStartingUnits(Player2Data.Team);
        
        // Then spawn any units from cheats
        GameManager.Cheats.SpawnUnits(false);
    }

    private void SpawnPlayerStartingUnits(GameTeam team) {
        StartingEntitySet entitySet = MapLoader.UnitSpawns.First(s => s.Team == team);
        foreach (EntitySpawnData entity in entitySet.Entities) {
            Debug.Log($"Spawning starting unit: {entity.Data.ID} ({entity.SpawnLocation})");
            GameManager.CommandManager.SpawnEntity(entity.Data, entity.SpawnLocation.Location, team, null, entity.SpawnLocation.Location, false);
        }
    }

    /// <summary>
    /// Start the UI for counting down. Does not actually start the game when finished counting down. 
    /// </summary>
    public void StartCountdownTimerView() {
        SceneLoader.Instance.HideLoadingScreen();
        
        CountdownTimer.StartCountdown(CountdownTimeSeconds);
        GameTeam localTeam = GameManager.LocalTeam;
        if (localTeam != GameTeam.Spectator) {
            Vector2Int keepLocation = MapLoader.GetPlayerStartLocation(localTeam);
            StartLocationIndicator.PlayMovingAnimation(GameManager.GridController.GetWorldPosition(keepLocation), 
                GameManager.GetPlayerForTeam(localTeam).Data.TeamColor, 
                CountdownTimeSeconds);
        }
        
        PerformClientSidePostMapSetupInitialization();
    }

    /// <summary>
    /// Actual timer (separate from <see cref="CountdownTimerView"/>) for starting the game
    /// </summary>
    private async void PerformGameStartCountdown() {
        await Task.Delay((int)(CountdownTimeSeconds * 1000));
        
        // The game is actually starting now, so perform on-start abilities for the starting set of entities
        PerformOnStartAbilities();
        GameInitialized = true;
    }
    
    #region Singleplayer

    private void SetupSPGame() {
        if (GameInitialized) {
            Debug.LogError("Can not set up SP game, the game was already set up");
            return;
        }
        
        ICommandManager commandManager = Instantiate(SPCommandManagerPrefab, transform);
        GameManager.SetupCommandManager(commandManager);
        
        // Set up players
        SPGamePlayer player1 = Instantiate(SPGamePlayerPrefab, GameManager.transform);
        player1.Data = Player1Data;
        SPGamePlayer player2 = Instantiate(SPGamePlayerPrefab, GameManager.transform);
        player2.Data = Player2Data;
        
        // Attempt to set the local player name
        try {
            player1.DisplayName = SteamFriends.GetPersonaName();
        } catch (InvalidOperationException) {
            // Steamworks not initialized. Just set a dummy name. 
            player1.DisplayName = "Local Player";
        }
        player2.DisplayName = "Opponent";
        
        GameManager.SetPlayers(player1, player2, player1, 0);

        MapLoader.LoadMap(MapData);
        MapLoader.SetUpCamera(player1.Data.Team);
        SpawnStartingUnits();

        PerformClientSidePostMapSetupInitialization();

        PerformOnStartAbilities();

        GameInitialized = true;
    } 

    #endregion

    private static void PerformOnStartAbilities() {
        GameManager.CommandManager.EntitiesOnGrid.AllEntities().ForEach(e => 
            GameManager.AbilityAssignmentManager.PerformOnStartAbilitiesForEntity(e));
    }

    /// <summary>
    /// Initialization items that should be performed on each client after map setup is complete
    /// </summary>
    private void PerformClientSidePostMapSetupInitialization() {
        if (GameManager.CommandManager.EntitiesOnGrid.Entities.Count == 0) {
            GameManager.CommandManager.EntityCollectionChangedEvent += DoPerformClientSidePostMapSetupInitialization;
        } else {
            DoPerformClientSidePostMapSetupInitialization();
        }
    }

    private void DoPerformClientSidePostMapSetupInitialization() {
        GameManager.CommandManager.EntityCollectionChangedEvent -= DoPerformClientSidePostMapSetupInitialization;
        
        GameManager.AmberForgeAvailabilityNotifier.Initialize();
        GameManager.LeaderTracker.Initialize(GameManager.CommandManager);
    }
    
    #region Multiplayer
    
    /// <summary>
    /// Check to see if all players have connected for this client. If they have, notify the server that we are ready.
    /// If not, then check again later. 
    /// </summary>
    [Client]
    private IEnumerator ListenForPlayersConnected() {
        if (MPSetupHandler.PlayerCount < 0) {
            // The server does not yet know the player count. Try again in a bit
            yield return new WaitForSeconds(.1f);
            StartCoroutine(ListenForPlayersConnected());
            yield break;
        }

        List<MPGamePlayer> players = FindObjectsByType<MPGamePlayer>(FindObjectsSortMode.InstanceID).ToList();
        if (players.Count == MPSetupHandler.PlayerCount) {
            // Load the map client-side first, then notify the server that the client setup is finished
            MapLoader.LoadMap(MapData);
            MapLoader.SetUpCamera(players.First(p => p.isLocalPlayer).Data.Team);
            MPSetupHandler.CmdNotifyPlayerReady(players.First(p => p.isLocalPlayer).DisplayName);
        } else if (players.Count > MPSetupHandler.PlayerCount) {
            throw new Exception($"Detected more player objects than the recorded player count! Expected: {MPSetupHandler.PlayerCount}. Actual: {players.Count}");
        } else {
            // Try again in a bit
            yield return new WaitForSeconds(.1f);
            StartCoroutine(ListenForPlayersConnected());
        }
    }

    /// <summary>
    /// We just detected that a player has arrived in the game scene! Perform setup. 
    /// </summary>
    [Server]
    public void SetupMPPlayer(GameNetworkPlayer networkPlayer, MPGamePlayer gamePlayer, int playerCount) {
        if ((MPGamePlayer) GameManager.Player1 == gamePlayer 
                || (MPGamePlayer) GameManager.Player2 == gamePlayer
                || _spectatorPlayers.Cast<MPGamePlayer>().Contains(gamePlayer)) {
            Debug.LogError($"Game scene loaded for player {networkPlayer.DisplayName}, but we already detected the game scene loading for them.");
            return;
        }
        if (MPSetupHandler.PlayerCount > -1 && playerCount != MPSetupHandler.PlayerCount) {
            Debug.LogError($"{nameof(playerCount)} ({playerCount}) is a different value than what we previously recorded ({MPSetupHandler.PlayerCount})!");
            return;
        }

        MPSetupHandler.PlayerCount = playerCount;

        int index = networkPlayer.index;
        PlayerData data = index switch {
            0 => Player1Data,
            1 => Player2Data,
            _ => SpectatorData
        };

        gamePlayer.Index = index;
        gamePlayer.Data = data;
        gamePlayer.DisplayName = networkPlayer.DisplayName;
        gamePlayer.Connected = true;
        AllPlayers[index] = gamePlayer;
        
        Debug.Log($"Player ({gamePlayer.DisplayName}) has been detected. Index ({networkPlayer.index}).");
        if (gamePlayer.Data.Team == GameTeam.Spectator) {
            _spectatorPlayers.Add(gamePlayer);
        }

        if (networkPlayer.isLocalPlayer) {
            // This is the server's local player. Since no other clients will notify us that this player joined, we should do it here
            MapLoader.LoadMap(MapData);
            MapLoader.SetUpCamera(gamePlayer.Data.Team); 
            MarkPlayerReady(networkPlayer.DisplayName + " (host)");
        }
    }

    /// <summary>
    /// A player has finished loading into the game scene
    /// </summary>
    [Server]
    public void MarkPlayerReady(string displayName) {
        Debug.Log($"Player ({displayName}) connected");
        _readyPlayerCount++;

        // Set up the game once all players have arrived
        if (MPSetupHandler.PlayerCount == _readyPlayerCount) {
            SetupMPGame();
        }
    }
    
    /// <summary>
    /// To be called when all players are ready. Perform command controller setup and send that info to the players.
    /// </summary>
    [Server]
    private void SetupMPGame() {
        if (GameInitialized) {
            Debug.LogError("Can not set up MP game, the game was already set up");
            return;
        }
        
        // If we are missing any players (due to no one picking those slots), set them up as dummy players now 
        List<MPGamePlayer> players = FindObjectsByType<MPGamePlayer>(FindObjectsSortMode.InstanceID).ToList();
        MPGamePlayer player1 = players.FirstOrDefault(p => p.Data.Team == GameTeam.Player1);
        MPGamePlayer player2 = players.FirstOrDefault(p => p.Data.Team == GameTeam.Player2);
        if (!player1) {
            player1 = Instantiate(MPGamePlayerPrefab, GameManager.transform);
            player1.Data = Player1Data;
            player1.DisplayName = "Player 1";
            player1.Connected = true;
            player1.Index = 0;
            NetworkServer.Spawn(player1.gameObject);
        }
        if (!player2) {
            player2 = Instantiate(MPGamePlayerPrefab, GameManager.transform);
            player2.Data = Player2Data;
            player2.DisplayName = "Player 2";
            player2.Connected = true;
            player2.Index = 1;
            NetworkServer.Spawn(player2.gameObject);
        }
        
        // Listen for disconnects
        GameNetworkManager gameNetworkManager = (GameNetworkManager)NetworkManager.singleton;
        gameNetworkManager.RoomServerDisconnectAction += ReDetermineWhichPlayersAreConnected;
        
        // Set up the command controller - only done on the server
        MPCommandManager newManager = Instantiate(MPCommandManagerPrefab, transform);
        NetworkServer.Spawn(newManager.gameObject);
        GameManager.SetupCommandManager(newManager);

        // Tell clients to look for the command controller and player GameObjects
        MPSetupHandler.RpcAssignPlayers();
    }

    private void ReDetermineWhichPlayersAreConnected(NetworkConnectionToClient connection) {
        IGamePlayer gamePlayer = connection.identity.gameObject.GetComponent<IGamePlayer>();
        int index = gamePlayer.Index;
        
        AllPlayers[index].Connected = false;
        PlayerDisconnected?.Invoke();
    }

    /// <summary>
    /// A player has finished the assign step
    /// </summary>
    [Server]
    public void MarkPlayerAssigned() {
        _assignedPlayerCount++;

        // Proceed with game setup once all players have been assigned
        if (MPSetupHandler.PlayerCount == _assignedPlayerCount) {
            PerformFinalGameSetupSteps();
        }
    }

    [Server]
    private void PerformFinalGameSetupSteps() {
        // Now that both players are set up, spawn units for both sides
        SpawnStartingUnits();

        PerformGameStartCountdown();
        MPSetupHandler.RpcBeginCountdownView();
    }

    #endregion
    
}