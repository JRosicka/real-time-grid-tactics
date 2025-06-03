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
using UnityEngine.Serialization;

/// <summary>
/// Editor window for cheats, scene navigation, etc. Handles persisting cheat state between sessions for convenience. 
/// </summary>
public class CheatsEditorWindow : OdinEditorWindow {
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

    private static CheatsEditorWindow Window => GetWindow<CheatsEditorWindow>();
    
    /// <summary>
    /// The '%g' allows for the keyboard shortcut CTRL + G for opening the window. 
    /// </summary>
    [MenuItem("Tools/Scene Navigation %g")]
    private static void OpenWindow() {
        Window.Show();
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
        CheatConfiguration.CheatsEnabled = _cheatsEnabled;
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
        CheatConfiguration.PlayerMoney = _playerMoney;
    }
    
    [Button]
    public void SetMoney() {
        ThrowExceptionIfNotInGame();

        GameManager.Instance.Cheats.SetMoney(CheatConfiguration.PlayerMoney);
    }
    
    [PropertyOrder(3)]
    [OnValueChanged("RemoveBuildTimeToggled")]
    [SerializeField]
    private bool _removeBuildTime;
    private void RemoveBuildTimeToggled() {
        CheatConfiguration.RemoveBuildTime = _removeBuildTime;
    }
    
    [PropertyOrder(4)]
    [OnValueChanged("ControlAllPlayersToggled")]
    [SerializeField]
    private bool _controlAllPlayers;
    private void ControlAllPlayersToggled() {
        CheatConfiguration.ControlAllPlayers = _controlAllPlayers;
    }
    
    [PropertyOrder(5)]
    [ListDrawerSettings(ShowFoldout = false)]
    [OnValueChanged("SpawnDataChanged")]
    [SerializeField]
    private List<EntitySpawnData> _spawnData = new List<EntitySpawnData>();
    private void SpawnDataChanged() {
        CheatConfiguration.SpawnData = _spawnData;
    }
    
    [PropertyOrder(6)]
    [Button]
    public void SpawnUnits() {
        ThrowExceptionIfNotInGame();
        
        GameManager.Instance.Cheats.SpawnUnits(CheatConfiguration.SpawnData);
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