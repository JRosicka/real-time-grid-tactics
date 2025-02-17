/// <summary>
/// Constant string values for icons that can be used in TMP fields
/// </summary>
public static class TextIcons {
    private const string TMPFormat = "<sprite name=\"{0}\">";

    public static string Gold => string.Format(TMPFormat, "gold");
    public static string Amber => string.Format(TMPFormat, "amber");
}