#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Task = System.Threading.Tasks.Task;

/// <summary>
/// Editor window for cheats, scene navigation, etc. Handles persisting cheat state between sessions for convenience. 
/// </summary>
[InitializeOnLoad]
public class CheatsEditorWindow : OdinEditorWindow {
    static CheatsEditorWindow() {
        // Ran whenever recompiling the project
        EditorApplication.delayCall += () => {
            Instance.LoadCheatData();
        };
    }
    
    private static CheatConfiguration _cheatConfiguration;
    private static CheatConfiguration CheatConfiguration {
        get {
            // Load the configuration if not already loaded
            if (_cheatConfiguration == null) {
                string[] guids = AssetDatabase.FindAssets($"t:CheatConfiguration");
                switch (guids.Length) {
                    case 0:
                        throw new Exception("No cheat configuration found");
                    case > 1:
                        throw new Exception("More than one cheat configuration found");
                }

                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                _cheatConfiguration = AssetDatabase.LoadAssetAtPath<CheatConfiguration>(assetPath);

                if (_cheatConfiguration == null) {
                    throw new Exception($"Failed to find cheat configuration: {assetPath}");
                }
            }
            
            return _cheatConfiguration;
        }
    }

    private static CheatsEditorWindow _instance;
    private static CheatsEditorWindow Instance {
        get {
            if (_instance == null) {
                _instance = GetWindow<CheatsEditorWindow>();
            }
            return _instance;
        }
    }

    /// <summary>
    /// The '%g' allows for the keyboard shortcut CTRL + G for opening the window. 
    /// </summary>
    [MenuItem("Tools/Scene Navigation %g")]
    private static void OpenWindow() {
        Instance.LoadCheatData();
        Instance.Show();
    }

    private static bool _loadingCheatData;
    private void LoadCheatData() {
        _loadingCheatData = true;

        _cheatsEnabled = CheatConfiguration.CheatsEnabled;
        _playerMoney = CheatConfiguration.PlayerMoney;
        _removeBuildTime = CheatConfiguration.RemoveBuildTime;
        _controlAllPlayers = CheatConfiguration.ControlAllPlayers;
        _spawnData = CheatConfiguration.SpawnData;
        SetSpawnDataListeners();
        
        RegisterConfigurationChangeListener();
    }

    private async void RegisterConfigurationChangeListener() {
        // Delay 10ms so that the caller event finishes being invoked before re-registering to that event
        await Task.Delay(10);
        CheatConfiguration.CheatConfigurationChanged -= LoadCheatData;
        CheatConfiguration.CheatConfigurationChanged += LoadCheatData;
        
        _loadingCheatData = false;
    }

    #region Scene Navigation
    
    [Title("Scenes")]
    [HorizontalGroup("Scenes", Order = -2)]
    [Button("Main Menu")]
    public void OpenMainMenuScene() {
        OpenScene("MainMenu");
    }

    [Title("")]
    [HorizontalGroup("Scenes")]
    [Button("Lobby")]
    public void OpenLobbyScene() {
        OpenScene("Room");
    }

    [Title("")]
    [HorizontalGroup("Scenes")]
    [Button("Game")]
    public void OpenGameScene() {
        OpenScene("GamePlay");
    }

    private void OpenScene(string sceneName) {
        if (EditorApplication.isPlaying) {
            throw new Exception("Can not change scenes like this while in play mode");
        }

        string assetPath = LocateSceneAsset(sceneName);
        
        // Save the current scene and project first
        EditorApplication.ExecuteMenuItem("File/Save");
        EditorApplication.ExecuteMenuItem("File/Save Project");

        EditorSceneManager.OpenScene(assetPath);
    }

    private string LocateSceneAsset(string sceneName) {
        string[] sceneGuids = AssetDatabase.FindAssets($"t:scene {sceneName}");
        foreach (string guid in sceneGuids) {
            string sceneAssetPath = AssetDatabase.GUIDToAssetPath(guid);
            string[] pathParts = sceneAssetPath.Split('/');
            if (sceneAssetPath.Contains(".unity")) {
                string assetName = pathParts.Last().Replace(".unity", "");
                if (string.Equals(assetName, sceneName, StringComparison.CurrentCultureIgnoreCase)) {
                    return sceneAssetPath;
                }
            } else {
                Debug.LogError($"Identified scene path without a .unity extension: {sceneAssetPath}");
            }
        }
        
        throw new Exception($"No scene asset found with scene name: {sceneName}");
    }
    
    #endregion
    #region Cheat Config

    [Title("Cheat Config")]
    [HorizontalGroup("Cheats Config", Order = -1)]
    [LabelText("Enabled")]
    [OnValueChanged("CheatsToggled")]
    [SerializeField]
    private bool _cheatsEnabled;
    private void CheatsToggled() {
        if (!_loadingCheatData) {
            CheatConfiguration.CheatsEnabled = _cheatsEnabled;
        }
    }
    
    [Title("")]
    [HorizontalGroup("Cheats Config")]
    [Button("Save State")]
    public void SaveCheatsState() {
        CheatConfiguration.SaveCheats();
    }

    [Title("")]
    [HorizontalGroup("Cheats Config")]
    [Button("Load State")]
    public void LoadCheatsState() {
        CheatConfiguration.LoadCheats();
    }
    
    #endregion
    #region Cheats

    [Title("Cheats")]
    [PropertyOrder(2)]
    [OnValueChanged("PlayerMoneyChanged")]
    [SerializeField]
    private int _playerMoney;
    private void PlayerMoneyChanged() {
        if (!_loadingCheatData) {
            CheatConfiguration.PlayerMoney = _playerMoney;
        }
    }
    
    [PropertyOrder(3)]
    [Button]
    public void SetMoney() {
        ThrowExceptionIfNotInGame();

        GameManager.Instance.Cheats.SetMoney(CheatConfiguration.PlayerMoney);
    }
    
    [PropertyOrder(4)]
    [OnValueChanged("RemoveBuildTimeToggled")]
    [SerializeField]
    private bool _removeBuildTime;
    private void RemoveBuildTimeToggled() {
        if (!_loadingCheatData) {
            CheatConfiguration.RemoveBuildTime = _removeBuildTime;
        }
    }
    
    [PropertyOrder(5)]
    [OnValueChanged("ControlAllPlayersToggled")]
    [SerializeField]
    private bool _controlAllPlayers;
    private void ControlAllPlayersToggled() {
        if (!_loadingCheatData) {
            CheatConfiguration.ControlAllPlayers = _controlAllPlayers;
        }
    }
    
    [PropertyOrder(6)]
    [Button]
    public void SpawnUnits() {
        ThrowExceptionIfNotInGame();
        
        GameManager.Instance.Cheats.SpawnUnits();
    }
    
    [PropertyOrder(7)]
    [OnValueChanged("SpawnDataChanged")]
    [SerializeField]
    private List<EntitySpawnData> _spawnData = new List<EntitySpawnData>();
    private void SpawnDataChanged() {
        if (!_loadingCheatData) {
            CheatConfiguration.SpawnData = _spawnData;
            SetSpawnDataListeners();
        }
    }
    private void SetSpawnDataListeners() {
        foreach (EntitySpawnData entitySpawnData in _spawnData) {
            entitySpawnData.SpawnLocationUpdated -= SpawnDataChanged;
            entitySpawnData.SpawnLocationUpdated += SpawnDataChanged;
        }
    }

    private static void ThrowExceptionIfNotInGame() {
        if (!EditorApplication.isPlaying) {
            throw new Exception("Game must be in play mode to perform cheat");
        }
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null) {
            throw new Exception("Must be in-game to perform cheat");
        }
    }
    
    #endregion
}
#endif