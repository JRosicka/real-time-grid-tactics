namespace Util {
    /// <summary>
    /// Utility class for generating IDs
    /// </summary>
    public class IDGenerator {
        private int _lastID = 1000;

        /// <summary>
        /// Generate an ID that has not been used before during this session on this client
        /// </summary>
        public int GenerateUID() {
            _lastID++;
            return _lastID;
        }
    }
}