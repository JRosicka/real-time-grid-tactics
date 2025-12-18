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
            EditorApplication.delayCall += () => Instance.SetUpWindow();
            
            // Ran whenever exiting play mode
            EditorApplication.playModeStateChanged += state => {
                if (state == PlayModeStateChange.ExitingPlayMode) {
                    EditorApplication.delayCall += () => Instance.SetUpWindow();
                }
            };
            
            // Ran whenever a scene loads
            EditorSceneManager.sceneOpened += (_, _) => Instance.SetUpWindow();
            SceneManager.sceneLoaded += (_, _) => Instance.SetUpWindow();
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
        
        private MapLoader _mapLoader;

        private MapLoader MapLoader {
            get {
                if (!_mapLoader) {
                    _mapLoader = FindFirstObjectByType<MapLoader>();
                }
                return _mapLoader;
            }
        }
        
        /// <summary>
        /// The '%m' allows for the keyboard shortcut CTRL + M for opening the window. 
        /// </summary>
        [MenuItem("Tools/Scene Navigation %m")]
        private static void OpenMapsWindow() {
            Instance.SetUpWindow();
            Instance.Show();
        }

        private bool _everSetUp;
        private bool _everPopulated;
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

            if (_inGameScene && !_everPopulated) {
                DropdownMapID = MapLoader.CurrentMapID;
                PopulateFields(MapSerializer.GetMap(DropdownMapID));
                
                _everPopulated = true;
            }
            
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
            return MapSerializer.GetAllMaps().OrderBy(m => m.index).Select(m => new ValueDropdownItem(m.mapID, m.mapID)).ToList();
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
            MapLoader.LoadMap(mapData);
            PopulateFields(mapData);
            
            Repaint();
        }

        private void PopulateFields(MapData mapData) {
            MapID = mapData.mapID;
            MapType = mapData.mapType;
            DisplayName = mapData.displayName;
            Description = mapData.description;
            DisplayIndex = mapData.index;
            PostProcessingID = mapData.postProcessingID;
            LowerLeftCell = new AssignableLocation(mapData.lowerLeftCell);
            UpperRightCell = new AssignableLocation(mapData.upperRightCell);
            WideLeftSide = mapData.wideLeftSide;
            WideRightSide = mapData.wideRightSide;

            Entities = CopyEntities(mapData.entities);
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
            List<MapData.Cell> cells = MapLoader.GridController.GetAllCells();
            MapSerializer.SaveMap(MapID, MapType, DisplayName, Description, DisplayIndex, PostProcessingID, 
                LowerLeftCell.Location, UpperRightCell.Location, WideLeftSide, WideRightSide, cells, 
                CopyEntities(Entities));
            
            // Re-load the map to make sure the changes are reflected
            LoadMap();
        }

        #endregion
        #region Configuration

        [Title("Configuration")]
        [VerticalGroup("Configuring", Order = 0, VisibleIf = "_inGameScene")]
        public MapType MapType;
        [VerticalGroup("Configuring")]
        public string DisplayName;
        [VerticalGroup("Configuring")]
        [TextArea(3, 10)]
        public string Description;
        [VerticalGroup("Configuring")]
        public int DisplayIndex;
        [VerticalGroup("Configuring")]
        public string PostProcessingID;
        [Space]
        [VerticalGroup("Configuring")]
        [InlineProperty]
        public AssignableLocation LowerLeftCell;
        [VerticalGroup("Configuring")]
        [InlineProperty]
        public AssignableLocation UpperRightCell;
        
        [Header("Whether either side should be 'wide' meaning an extra column beyond the corner cell's column")]
        [VerticalGroup("Configuring")]
        public bool WideLeftSide;
        [VerticalGroup("Configuring")]
        public bool WideRightSide;

        [VerticalGroup("Configuring")]
        [PropertyOrder(10)]
        [Button("Update Boundaries")]
        public void UpdateBoundaries() {
            MapLoader.UpdateBoundaries(LowerLeftCell.Location, UpperRightCell.Location, WideLeftSide, WideRightSide);
        }
        
        [Space]
        [VerticalGroup("Configuring")]
        [PropertyOrder(20)]
        public List<StartingEntitySet> Entities;
        
        #endregion
        
        private List<StartingEntitySet> CopyEntities(List<StartingEntitySet> entities) {
            List<StartingEntitySet> newEntities = new List<StartingEntitySet>();
            foreach (StartingEntitySet entitySet in entities) {
                StartingEntitySet newEntitySet = new StartingEntitySet {
                    Team = entitySet.Team,
                    Entities = new List<EntitySpawnData>()
                };
                foreach (EntitySpawnData spawnData in entitySet.Entities) {
                    newEntitySet.Entities.Add(new EntitySpawnData(spawnData));
                }
                newEntities.Add(newEntitySet);
            }

            return newEntities;
        }
    }
}