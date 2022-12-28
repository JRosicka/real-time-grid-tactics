/// <summary>
/// Utility class for generating IDs
/// </summary>
public static class IDUtil {
    private static int _lastID = 1000;

    /// <summary>
    /// Generate an ID that has not been used before during this session on this client
    /// </summary>
    /// <returns></returns>
    public static int GenerateUID() {
        _lastID++;
        return _lastID;
    }
}