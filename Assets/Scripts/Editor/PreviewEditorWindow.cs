using System.Collections.Generic;
using System.Linq;
using Gameplay.Managers;
using Scenes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Gameplay.Config {
    /// <summary>
    /// Editor window for recording and saving preview games  
    /// </summary>
    [InitializeOnLoad]
    public class PreviewEditorWindow : OdinEditorWindow {
        private const string GameSceneName = "GamePlay";
        static PreviewEditorWindow() {
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

        private static PreviewEditorWindow _instance;
        private static PreviewEditorWindow Instance {
            get {
                if (_instance == null) {
                    _instance = GetWindow<PreviewEditorWindow>();
                }
                return _instance;
            }
        }

        private MapsConfiguration MapsConfiguration => GameConfigurationLocator.GameConfiguration.MapsConfiguration;
        
        private ReplayManager _replayManager;
        private ReplayManager ReplayManager {
            get {
                _replayManager ??= GameManager.Instance?.ReplayManager;
                return _replayManager;
            }
        }
        
        private bool _everSetUp;
        private bool _everPopulated;
        private bool _inGameScene;
        private bool _notInGameScene;
        private bool _recording;
        private bool _canRecord;
        private bool _canStopRecording;
        
        private ReplayData _pendingReplayData;

        private void SetUpWindow() {
            UpdateRecordingFlags();

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
            UpdateRecordingFlags();
            
            _everSetUp = true;

            if (_inGameScene && !_everPopulated) {
                DropdownReplayID = "originsReplay1";
                PopulateFields(MapsConfiguration.GetReplay(DropdownReplayID));
                
                _everPopulated = true;
            }
            
            Repaint();
        }

        private void UpdateRecordingFlags() {
            _canRecord = !_recording && _inGameScene;
            _canStopRecording = _recording && _inGameScene; 
        }
        
        #region Not In Gameplay Scene
        
        [DisplayAsString]
        [VerticalGroup("NotInGameScene", VisibleIf = "_notInGameScene")]
        [HideLabel]
        public string NotInGameSceneMessage = "Game scene not loaded";
        
        #endregion
        #region Replay Loading

        private List<ValueDropdownItem> GetReplayIDs() {
            return MapsConfiguration.PreviewReplays.Select(m => new ValueDropdownItem(m.replayID, m.replayID)).ToList();
        }
        
        [Title("Loading")]
        [HorizontalGroup("Loading", Order = -2, VisibleIf = "_inGameScene")]
        [HideLabel]
        [ValueDropdown("GetReplayIDs")]
        public string DropdownReplayID; 
        
        [Title("")]
        [HorizontalGroup("Loading")]
        [Button("Load")]
        public void LoadReplay() {
            ReplayData replayData = MapsConfiguration.GetReplay(DropdownReplayID);
            PopulateFields(replayData);
            GameTypeTracker.SetMapIDFromEditorWindow(replayData.mapID);
            
            Repaint();
        }

        private void PopulateFields(ReplayData replayData) {
            ReplayID = replayData.replayID;
            MapID = replayData.mapID;
            Duration = replayData.duration;
            Seed = replayData.seed;
            TimedCommands = CopyCommands(replayData.commands);
        }
        
        #endregion
        #region Replay Saving
        
        [Title("Recording")] 
        [HorizontalGroup("Recording", Order = -1, VisibleIf = "_canRecord")]
        [Button("Record")]
        public void Record() {
            _recording = true;
            UpdateRecordingFlags();
            
            // Re-load the map to make sure the changes are reflected
            LoadReplay();
            
            ReplayManager.StartRecording();
        }
        
        [Title("")]
        [HorizontalGroup("Recording")]
        [Button("Save")]
        public void SaveRecording() {
            _pendingReplayData = new ReplayData {
                replayID = ReplayID,
                mapID = MapID,
                duration = Duration,
                seed = Seed,
                commands = CopyCommands(TimedCommands)
            };
            MapsConfiguration.AddReplay(_pendingReplayData); 
            LoadReplay();
        }

        [Title("Recording")]
        [VerticalGroup("Recording2", Order = -1, VisibleIf = "_canStopRecording")]
        [Button("Stop Recording")]
        public void StopRecording() {
            _recording = false;
            UpdateRecordingFlags();

            _pendingReplayData = ReplayManager.StopRecording();
            _pendingReplayData.replayID = ReplayID;
            PopulateFields(_pendingReplayData);
        }
        
        #endregion
        #region Configuration
        
        [Title("Configuration")]
        [VerticalGroup("Configuring", Order = 0, VisibleIf = "_inGameScene")]
        public string ReplayID;
        [VerticalGroup("Configuring")]
        public string MapID;
        [VerticalGroup("Configuring")]
        public float Duration;
        [VerticalGroup("Configuring")]
        public int Seed;
        [VerticalGroup("Configuring")]
        public List<ReplayData.TimedCommand> TimedCommands;
            
        #endregion
        
        private List<ReplayData.TimedCommand> CopyCommands(List<ReplayData.TimedCommand> commands) {
            return commands.Select(command => new ReplayData.TimedCommand {
                time = command.time, 
                entityID = command.entityID, 
                abilityType = command.abilityType, 
                abilityParameterJson = command.abilityParameterJson
            }).ToList();
        }
    }
}