using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gameplay.Config {
    /// <summary>
    /// Editor window for setting up, saving, and loading maps. Intended to be used for serializing/deserializing grid
    /// state in the game scene.  
    /// </summary>
    [InitializeOnLoad]
    public class MapEditorWindow : OdinEditorWindow {
        private const string GameSceneName = "GamePlay";
        static MapEditorWindow() {
            // Ran whenever recompiling the project
            EditorApplication.delayCall += () => {
                Instance.SetUpWindow();
            };
            EditorSceneManager.sceneOpened += (scene, mode) => {
                Instance.SetUpWindow();
            };
        }

        private static MapEditorWindow _instance;
        private static MapEditorWindow Instance {
            get {
                if (_instance == null) {
                    _instance = GetWindow<MapEditorWindow>();
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// The '%m' allows for the keyboard shortcut CTRL + M for opening the window. 
        /// </summary>
        [MenuItem("Tools/Scene Navigation %m")]
        private static void OpenWindow() {
            Instance.SetUpWindow();
            Instance.Show();
        }

        private bool _everSetUp;
        private bool _inGameScene;
        private bool _notInGameScene;

        private void SetUpWindow() {
            bool newInGameScene = false;
            for (int sceneIdx = 0; sceneIdx < SceneManager.sceneCount; ++sceneIdx) {
                Scene scene = SceneManager.GetSceneAt(sceneIdx);
                if (scene.name == GameSceneName) {
                    newInGameScene = true;
                    break;
                }
            }

            // Check if state changed, update state
            if (_everSetUp && newInGameScene == _inGameScene) return;
            _inGameScene = newInGameScene;
            _notInGameScene = !newInGameScene;
            _everSetUp = true;
            
            Repaint();
        }
        
        #region Not In Gameplay Scene
        
        [DisplayAsString]
        [VerticalGroup("NotInGameScene", VisibleIf = "_notInGameScene")]
        [HideLabel]
        public string NotInGameSceneMessage = "Game scene not loaded";
        
        #endregion
        #region Map Loading

        private List<ValueDropdownItem> GetMapIDs() {
            return MapSerializer.GetAllMaps().Select(m => new ValueDropdownItem(m.mapID, m.mapID)).ToList();
        }
        
        [Title("Loading")]
        [HorizontalGroup("Loading", Order = -2, VisibleIf = "_inGameScene")]
        [HideLabel]
        [ValueDropdown("GetMapIDs")]
        public string DropdownMapID; 
        
        [Title("")]
        [HorizontalGroup("Loading")]
        [Button("Load")]
        public void LoadMap() {
            MapData mapData = MapSerializer.GetMap(DropdownMapID);
            
            
            
            // TODO load tiles
            
            Repaint();
        }
        
        #endregion
        #region Map Saving

        [Title("Saving")] 
        [HorizontalGroup("Saving", Order = -1, VisibleIf = "_inGameScene")]
        public string MapID;

        [Title("")]
        [HorizontalGroup("Saving")]
        [Button("Save")]
        public void SaveMap() {
            MapSerializer.SaveMap(MapID, DisplayName, Description, DisplayIndex, LowerLeftCell, UpperRightCell, 
                null, Entities, Preview ? MapType.Preview : MapType.Playable);
            // TODO read cells
        }

        #endregion
        #region Configuration

        [Title("Configuration")]
        [VerticalGroup("Configuring", Order = 0, VisibleIf = "_inGameScene")]
        public string DisplayName;
        [VerticalGroup("Configuring")]
        public string Description;
        [VerticalGroup("Configuring")]
        public int DisplayIndex;
        [VerticalGroup("Configuring")]
        public Vector2Int LowerLeftCell;
        [VerticalGroup("Configuring")]
        public Vector2Int UpperRightCell;
        
        // TODO Odd numbered wall for left and right

        [VerticalGroup("Configuring")]
        [PropertyOrder(10)]
        [Button("Toggle Boundary")]
        public void ToggleBoundary() {
            // TODO
        }
        
        [Space]
        [VerticalGroup("Configuring")]
        [PropertyOrder(20)]
        public List<StartingEntitySet> Entities;
        
        [Space]
        [VerticalGroup("Configuring")]
        [PropertyOrder(20)]
        public bool Preview;

        #endregion
    }
}