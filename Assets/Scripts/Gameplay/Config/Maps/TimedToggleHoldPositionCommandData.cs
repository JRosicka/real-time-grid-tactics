using System;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming     If we ever serialize this to JSON, then it will be convenient for the fields to be lowercase

namespace Gameplay.Config {
    /// <summary>
    /// <see cref="ReplayData.TimedCommand"/> data for toggling hold position. Some actions disrupt the hold position
    /// status without performing an ability. 
    /// </summary>
    [Serializable]
    public class TimedToggleHoldPositionCommandData : ITimedCommandData {
        public bool holdPosition;
        
        public string SerializeToJson() {
            return JsonConvert.SerializeObject(this);
        }
        
        public static TimedToggleHoldPositionCommandData DeserializeFromJson(string json) {
            return JsonConvert.DeserializeObject<TimedToggleHoldPositionCommandData>(json);
        }
    }
}