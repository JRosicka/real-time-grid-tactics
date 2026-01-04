using System;
using System.Collections.Generic;
using Gameplay.Entities;

// ReSharper disable InconsistentNaming     If we ever serialize this to JSON, then it will be convenient for the fields to be lowercase

namespace Gameplay.Config {
    /// <summary>
    /// Data for a replay (a recording of a game)
    /// </summary>
    [Serializable]
    public class ReplayData {
        public string replayID;
        public string mapID;
        public float duration;
        public int seed;
        public List<TimedCommand> commands;
        
        [Serializable]
        public class TimedCommand {
            public float time;
            public long entityID;
            public CommandType commandType;
            public string data;
        }

        public enum CommandType {
            Ability = 0,
            CancelAbility = 1,
            HoldPosition = 2
        }
    }
}