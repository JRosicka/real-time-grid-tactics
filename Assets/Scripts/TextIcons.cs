/// <summary>
/// Constant string values for icons that can be used in TMP fields
/// </summary>
public static class TextIcons {
    private const string TMPFormat = "<sprite name=\"{0}\">";

    public static string Food => string.Format(TMPFormat, "food");
    public static string Amber => string.Format(TMPFormat, "amber");
}