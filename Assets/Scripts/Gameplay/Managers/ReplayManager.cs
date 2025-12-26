using System.Collections.Generic;
using Gameplay.Config;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Scenes;

namespace Gameplay.Managers {
    /// <summary>
    /// Runtime class that handles getting abilities executed during a game and serializng them for replay purposes.
    /// </summary>
    public class ReplayManager {
        public bool Recording = true;
        private ReplayData _recordingReplayData;
        
        public void StartRecording() {
            _recordingReplayData = new ReplayData {
                mapID = GameTypeTracker.Instance.MapID,
                duration = 0,
                seed = 0, // TODO
                commands = new List<ReplayData.TimedCommand>()
            };
            Recording = true;
        }
        
        public ReplayData StopRecording() {
            _recordingReplayData.duration = GameManager.Instance.CommandManager.AbilityExecutor.MatchLength;
            Recording = false;
            
            return _recordingReplayData;
        }

        public void TryRecordAbility(GridEntity entity, IAbility ability) {
            if (Recording) {
                RecordAbility(entity, ability);
            }
        }
        
        private void RecordAbility(GridEntity entity, IAbility ability) {
            ReplayData.TimedCommand command = new ReplayData.TimedCommand {
                time = GameManager.Instance.CommandManager.AbilityExecutor.MatchLength,
                entityID = entity.UID,
                abilityType = ability.AbilityData.ContentResourceID,
                abilityParameterJson = ability.BaseParameters.SerializeToJson()
            };
            _recordingReplayData.commands.Add(command);
        }
    }
}