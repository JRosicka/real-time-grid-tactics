namespace Gameplay.Config {
    /// <summary>
    /// Type-specific data for a <see cref="ReplayData.TimedCommand"/> with serializer/deserializer
    /// </summary>
    public interface ITimedCommandData {
        string SerializeToJson();
    }
}