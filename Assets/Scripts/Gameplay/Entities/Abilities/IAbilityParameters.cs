using Mirror;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// Arbitrary set of ability-instance-specific parameters
    /// </summary>
    public interface IAbilityParameters {
        void Serialize(NetworkWriter writer);
        string SerializeToJson();
        void Deserialize(NetworkReader reader);
    }
    
    public abstract class BaseAbilityParameters : IAbilityParameters {
        public abstract void Serialize(NetworkWriter writer);
        public abstract string SerializeToJson();
        public abstract void Deserialize(NetworkReader reader);
        public abstract void DeserializeFromJson(string json);
    }

    public class NullAbilityParameters : BaseAbilityParameters {
        public override void Serialize(NetworkWriter writer) {
            // Nothing to do
        }

        public override string SerializeToJson() {
            return "{}";
        }

        public override void Deserialize(NetworkReader reader) {
            // Nothing to do
        }

        public override void DeserializeFromJson(string json) {
            // Nothing to do
        }
    }
}