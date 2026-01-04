using System;
using System.Collections.Generic;
using System.Linq;
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

        public bool PlayingReplay { get; private set; }
        private ReplayData _replayData;
        private int _lastCommandIndex = -1;
        public void Initialize() {
            _replayData = GameConfigurationLocator.GameConfiguration.MapsConfiguration.GetReplay(GameTypeTracker.Instance.ReplayID);
            PlayingReplay = _replayData != null;
        }
        
        #region Recording
        
#if UNITY_EDITOR
        public void StartRecording() {
            GameTypeTracker gameTypeTracker = FindFirstObjectByType<GameTypeTracker>();
            _recordingReplayData = new ReplayData {
                mapID = gameTypeTracker.MapID,
                duration = 0,
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
            _recordingReplayData.seed = GameManager.Instance.SeedManager.Seed;
            ReplayData ret = _recordingReplayData;
            _recordingReplayData = null;
            return ret;
        }
#endif

        public void TryRecordAbility(GridEntity entity, IAbility ability, bool fromInput, bool performEvenIfOnCooldown, bool clearOtherAbilities, GameTeam? overrideTeam) {
            if (Recording) {
                DoRecordAbility(entity, ability, fromInput, performEvenIfOnCooldown, clearOtherAbilities, overrideTeam);
            }
        }
        
        private void DoRecordAbility(GridEntity entity, IAbility ability, bool fromInput, bool performEvenIfOnCooldown, bool clearOtherAbilities, GameTeam? overrideTeam) {
            ReplayData.TimedCommand command = new() {
                time = GameManager.Instance.CommandManager.AbilityExecutor.MatchLength,
                entityID = entity.UID,
                commandType = ReplayData.CommandType.Ability,
                data = new TimedAbilityCommandData {
                    abilityType = ability.AbilityData.ContentResourceID,
                    abilityParameterJson = ability.BaseParameters.SerializeToJson(),
                    fromInput = fromInput,
                    performEvenIfOnCooldown = performEvenIfOnCooldown,
                    clearOtherAbilities = clearOtherAbilities,
                    overrideTeam = overrideTeam == null ? -2L : Convert.ToInt64(overrideTeam)
                }.SerializeToJson()
            };
            _recordingReplayData.commands.Add(command);
        }
        
        public void TryRecordHoldPosition(GridEntity entity, bool holdPosition) {
            if (Recording) {
                DoRecordHoldPosition(entity, holdPosition);
            }
        }

        private void DoRecordHoldPosition(GridEntity entity, bool holdPosition) {
            ReplayData.TimedCommand command = new() {
                time = GameManager.Instance.CommandManager.AbilityExecutor.MatchLength,
                entityID = entity.UID,
                commandType = ReplayData.CommandType.HoldPosition,
                data = new TimedToggleHoldPositionCommandData {
                    holdPosition = holdPosition
                }.SerializeToJson()
            };
            _recordingReplayData.commands.Add(command);
        }

        public void TryRecordCancelAbility(GridEntity entity, IAbility ability) {
            if (Recording) {
                DoRecordCancelAbility(entity, ability);
            }
        }
        
        private void DoRecordCancelAbility(GridEntity entity, IAbility ability) {
            ReplayData.TimedCommand command = new() {
                time = GameManager.Instance.CommandManager.AbilityExecutor.MatchLength,
                entityID = entity.UID,
                commandType = ReplayData.CommandType.CancelAbility,
                data = new TimedCancelAbilityCommandData {
                    abilityInstance = Convert.ToInt64(ability.UID.Split('_')[^1]),
                    abilityType = ability.AbilityData.ContentResourceID
                }.SerializeToJson()
            };
            _recordingReplayData.commands.Add(command);
        }
        
        #endregion
        #region Playback

        public void UpdateReplayPlayback(float matchTimeStamp) {
            if (!PlayingReplay) return;

            int nextCommandIndex = _lastCommandIndex + 1;
            while (nextCommandIndex < _replayData.commands.Count) {
                ReplayData.TimedCommand command = _replayData.commands[nextCommandIndex];
                if (command.time <= matchTimeStamp) {
                    nextCommandIndex++;
                    PerformRecordedCommand(command);
                } else {
                    _lastCommandIndex = nextCommandIndex - 1;
                    return;
                }
            }
            _lastCommandIndex = nextCommandIndex - 1;
        }
        
        private void PerformRecordedCommand(ReplayData.TimedCommand command) {
            switch (command.commandType) {
                case ReplayData.CommandType.Ability:
                    PerformRecordedAbility(command);
                    break;
                case ReplayData.CommandType.HoldPosition:
                    PerformRecordedHoldPosition(command);
                    break;
                case ReplayData.CommandType.CancelAbility:
                    PerformRecordedCancelAbility(command);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(command.commandType.ToString(), "Unknown command type!");
            }
        }
        
        private void PerformRecordedAbility(ReplayData.TimedCommand command) {
            TimedAbilityCommandData data = TimedAbilityCommandData.DeserializeFromJson(command.data);
            IAbilityData abilityData = GameManager.Instance.Configuration.GetAbility(data.abilityType).Content;
            GridEntity performer = GameManager.Instance.CommandManager.EntitiesOnGrid.GetEntityByID(command.entityID);
            IAbilityParameters parameters = abilityData.DeserializeParametersFromJson(JsonConvert.DeserializeObject<Dictionary<string, object>>(data.abilityParameterJson));
            GameManager.Instance.AbilityAssignmentManager.StartPerformingAbility(performer, abilityData, parameters, 
                data.fromInput, data.performEvenIfOnCooldown,  data.clearOtherAbilities, false, 
                data.overrideTeam == -2 ? null : (GameTeam)Convert.ToInt32(data.overrideTeam));
        }

        private void PerformRecordedHoldPosition(ReplayData.TimedCommand command) {
            TimedToggleHoldPositionCommandData data = TimedToggleHoldPositionCommandData.DeserializeFromJson(command.data);
            GridEntity performer = GameManager.Instance.CommandManager.EntitiesOnGrid.GetEntityByID(command.entityID);
            performer.ToggleHoldPosition(data.holdPosition, false);
        }

        private void PerformRecordedCancelAbility(ReplayData.TimedCommand command) {
            TimedCancelAbilityCommandData data = TimedCancelAbilityCommandData.DeserializeFromJson(command.data);
            GridEntity performer = GameManager.Instance.CommandManager.EntitiesOnGrid.GetEntityByID(command.entityID);
            
            // Construct the ability UID that we expect to find
            string abilityUID = $"{performer.UID}_{data.abilityType}_{data.abilityInstance}";

            // Check the in-progress abilities
            IAbility ability = performer.InProgressAbilities.FirstOrDefault(t => t.UID == abilityUID); 
            if (ability == null) {
                // Check the active timers
                ability = performer.ActiveTimers.FirstOrDefault(t => t.Ability.UID == abilityUID)?.Ability;
                if (ability == null) {
                    Debug.LogError($"Tried to perform a recorded ability cancellation for an ability that doesn't exist! ID: {abilityUID}. Performer: {performer.EntityData.ID}");
                    return;
                    // So I have a couple options on how to fix this issue:
                    // 1. Change the ability UIDs to be of the format {performer UID}_{ability type}_{index by order of ability creation per type per performer}, 
                    // e.g. 1001_AttackMelee_3. That way the only requirement would be that the indexing of each ability is consistent. 
                    // 2. Resolve the timing issue in AbilityExecutor. Some non-input abilities are not happening exactly when expected. 
                    // I thiiiiiink this is happening due to some timing inconsistency with when ability timers expire and thus 
                    // trigger new abilities. The example I'm seeing is with village income. Maybe the deltatime is different 
                    // between the recorded session and the replay session, and that's delaying the ability to the following frame.
                    // 
                    // I have gone with option 1, looks like that is sufficient. 
                }
            }
            
            GameManager.Instance.CommandManager.CancelAbility(ability, false);
        }

        #endregion
    }
}