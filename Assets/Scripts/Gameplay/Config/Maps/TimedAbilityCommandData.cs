using System;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming     If we ever serialize this to JSON, then it will be convenient for the fields to be lowercase

namespace Gameplay.Config {
    /// <summary>
    /// <see cref="ReplayData.TimedCommand"/> data for an ability being performed
    /// </summary>
    [Serializable]
    public class TimedAbilityCommandData : ITimedCommandData {
        public string abilityType;
        public string abilityParameterJson;
        public bool fromInput;
        public bool performEvenIfOnCooldown;
        public bool clearOtherAbilities;
        public long overrideTeam;
        
        public string SerializeToJson() {
            return JsonConvert.SerializeObject(this);
        }
        
        public static TimedAbilityCommandData DeserializeFromJson(string json) {
            return JsonConvert.DeserializeObject<TimedAbilityCommandData>(json);
        }
    }
}