using UnityEngine;
using Util;

/// <summary>
/// Handles tracking when the user updates the game and applying any necessary persistent data changes on update
/// </summary>
public static class PersistentDataUpdater {
    public static void UpdateData() {
        int previousVersion = PlayerPrefs.GetInt(PlayerPrefsKeys.LastSeenVersionKey, 0);
        
        // 0.6.0
        if (previousVersion < 00600) {
            // Reset all audio prefs since we now have a full audio pass
            PlayerPrefs.DeleteKey(PlayerPrefsKeys.MusicVolumeKey);
            PlayerPrefs.DeleteKey(PlayerPrefsKeys.SoundEffectVolumeKey);
            PlayerPrefs.DeleteKey(PlayerPrefsKeys.VoiceLineVolumeKey);
        }
        
        PlayerPrefs.SetInt(PlayerPrefsKeys.LastSeenVersionKey, VersionUtil.GetVersionNumber());
    }
}