using System.Collections.Generic;
using Gameplay.Config;
using Gameplay.Config.Abilities;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Newtonsoft.Json;
using Scenes;
using UnityEditor;
using UnityEngine;

namespace Gameplay.Managers {
    /// <summary>
    /// Runtime class that handles getting abilities executed during a game and serializing them for replay purposes.
    /// Also handles playing replays. 
    /// </summary>
    public class ReplayManager : MonoBehaviour {
        [HideInInspector] public bool Recording;
        private static ReplayData _recordingReplayData;

        private bool _playingReplay;
        private ReplayData _replayData;
        private int _lastCommandIndex = -1;
        public void Initialize() {
            _replayData = GameConfigurationLocator.GameConfiguration.MapsConfiguration.GetReplay(GameTypeTracker.Instance.ReplayID);
            _playingReplay = _replayData != null;
        }
        
#if UNITY_EDITOR
        public void StartRecording() {
            GameTypeTracker gameTypeTracker = FindFirstObjectByType<GameTypeTracker>();
            _recordingReplayData = new ReplayData {
                mapID = gameTypeTracker.MapID,
                duration = 0,
                seed = 0, // TODO
                commands = new List<ReplayData.TimedCommand>()
            };
            Recording = true;
        }
        
        public ReplayData StopRecording() {
            Recording = false;

            if (!EditorApplication.isPlaying) {
                _recordingReplayData = null;
                return null;
            }
            
            _recordingReplayData.duration = GameManager.Instance.CommandManager.AbilityExecutor.MatchLength;
            ReplayData ret = _recordingReplayData;
            _recordingReplayData = null;
            return ret;
        }
#endif

        public void TryRecordAbility(GridEntity entity, IAbility ability) {
            if (Recording) {
                RecordAbility(entity, ability);
            }
        }
        
        private void RecordAbility(GridEntity entity, IAbility ability) {
            ReplayData.TimedCommand command = new() {
                time = GameManager.Instance.CommandManager.AbilityExecutor.MatchLength,
                entityID = entity.UID,
                abilityType = ability.AbilityData.ContentResourceID,
                abilityParameterJson = ability.BaseParameters.SerializeToJson()
            };
            _recordingReplayData.commands.Add(command);
        }

        public void UpdateReplayPlayback(float matchTimeStamp) {
            if (!_playingReplay) return;

            int nextCommandIndex = _lastCommandIndex + 1;
            while (nextCommandIndex < _replayData.commands.Count) {
                ReplayData.TimedCommand command = _replayData.commands[nextCommandIndex];
                if (command.time <= matchTimeStamp) {
                    nextCommandIndex++;
                    PerformRecordedAbility(command);
                } else {
                    _lastCommandIndex = nextCommandIndex - 1;
                    return;
                }
            }
        }
        
        private void PerformRecordedAbility(ReplayData.TimedCommand command) {
            IAbilityData abilityData = GameManager.Instance.Configuration.GetAbility(command.abilityType).Content;
            GridEntity performer = GameManager.Instance.CommandManager.EntitiesOnGrid.GetEntityByID(command.entityID);
            IAbilityParameters parameters = abilityData.DeserializeParametersFromJson(JsonConvert.DeserializeObject<Dictionary<string, object>>(command.abilityParameterJson));
            GameManager.Instance.AbilityAssignmentManager.StartPerformingAbility(performer, abilityData, parameters, true, true,  false, null); // TODO what to do about clearOtherAbilities and overrideTeam?
        }
    }
}