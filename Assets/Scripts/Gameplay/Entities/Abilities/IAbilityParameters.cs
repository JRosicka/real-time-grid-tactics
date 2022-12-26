using Mirror;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// Arbitrary set of ability-instance-specific parameters
    /// </summary>
    public interface IAbilityParameters {
        void Serialize(NetworkWriter writer);
        void Deserialize(NetworkReader reader);
    }
    
    public abstract class BaseAbilityParameters : IAbilityParameters {
        public abstract void Serialize(NetworkWriter writer);
        public abstract void Deserialize(NetworkReader reader);
    }

    public class NullAbilityParameters : BaseAbilityParameters {
        public override void Serialize(NetworkWriter writer) {
            // Nothing to do
        }

        public override void Deserialize(NetworkReader reader) {
            // Nothing to do
        }
    }
}