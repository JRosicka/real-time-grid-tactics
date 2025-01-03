namespace Gameplay.UI {
    /// <summary>
    /// Configured id/description info for an <see cref="AbilitySlot"/>
    /// </summary>
    public class AbilitySlotInfo {
        public string ID { get; }
        public string Description { get; }

        public AbilitySlotInfo(string id, string description) {
            ID = id;
            Description = description;
        }
    }
}