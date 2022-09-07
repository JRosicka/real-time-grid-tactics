using Game.Network;
using Mirror;
using UnityEngine;

public class GameManager : MonoBehaviour {
    public static GameManager Instance;

    public SPGamePlayer SPGamePlayerPrefab;
    
    [Header("References")] 
    public GridController GridController;
    public EntityManager EntityManager;

    private IGamePlayer _localPlayer;
    private bool _gameInitialized;

    public void SetupMPGame(GameNetworkPlayer localNetworkPlayer, MPGamePlayer localGamePlayer) {
        if (_gameInitialized) {
            Debug.LogError("Can not set up MP game, the game was already set up");
        }
        _localPlayer = localGamePlayer;
        localGamePlayer.SetIndex(localNetworkPlayer.index);
        // TODO set color
        
        // TODO detect opponent player, store in field
        
        EntityManager.SetupCommandController(true);
        
        _gameInitialized = true;
    }

    private void SetupSPGame() {
        if (_gameInitialized) {
            Debug.LogError("Can not set up SP game, the game was already set up");
        }
        _localPlayer = Instantiate(SPGamePlayerPrefab);
        _localPlayer.SetIndex(0);
        // TODO set color

        EntityManager.SetupCommandController(false);
                
        _gameInitialized = true;
    }
    
    private void Awake() {
        if (Instance != null) {
            Debug.LogError("GameManager instance is not null!!");
        }
        
        Instance = this;
    }

    private void Start() {
        // If we are not connected in a multiplayer session, then we must be playing singleplayer. Set up the game now. 
        // Otherwise, wait for the network manager to set up the multiplayer game. 
        if (!NetworkServer.localClientActive) {
            SetupSPGame();
        }
    }
}
