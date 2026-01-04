using System;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming     If we ever serialize this to JSON, then it will be convenient for the fields to be lowercase

namespace Gameplay.Config {
    /// <summary>
    /// <see cref="ReplayData.TimedCommand"/> data for an ability being canceled
    /// </summary>
    [Serializable]
    public class TimedCancelAbilityCommandData : ITimedCommandData {
        public long abilityInstance;
        public string abilityType;
        
        public string SerializeToJson() {
            return JsonConvert.SerializeObject(this);
        }
        
        public static TimedCancelAbilityCommandData DeserializeFromJson(string json) {
            return JsonConvert.DeserializeObject<TimedCancelAbilityCommandData>(json);
        }
    }
}