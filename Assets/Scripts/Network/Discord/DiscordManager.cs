using Discord.Sdk;
using UnityEngine;

namespace Game.Network.Discord {
    /// <summary>
    /// Handles using Discord social features
    /// </summary>
    public class DiscordManager {
        private const ulong ApplicationID = 1539068505301721138;

        private readonly Client _discordClient;
        private ulong _lastStartTimestamp = (ulong)System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        public static DiscordManager Instance;

        public DiscordManager() {
            Instance = this;
            
            _discordClient = new Client();
            _discordClient.AddLogCallback(OnLog, LoggingSeverity.Warning);
            _discordClient.SetStatusChangedCallback(OnStatusChanged);
            _discordClient.SetApplicationId(ApplicationID);
        }

        public void SetRichPresence(string details, string state, bool resetTimer) {
            // Details
            Activity activity = new();
            activity.SetType(ActivityTypes.Playing);
            activity.SetDetails(details);
            activity.SetState(state);
            
            // Button
            ActivityButton communityButton = new();
            communityButton.SetLabel("Join the Community!");
            communityButton.SetUrl("https://discord.gg/1327435939550724147");
            activity.AddButton(communityButton);
            
            // Timer
            if (resetTimer) {
                _lastStartTimestamp = (ulong)System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
            ActivityTimestamps timestamp = new ActivityTimestamps();
            timestamp.SetStart(_lastStartTimestamp);
            activity.SetTimestamps(timestamp);

            _discordClient.UpdateRichPresence(activity, result => {
                Log($"Rich Presence result: {result}");
            });
        }
        
        private static void OnLog(string message, LoggingSeverity severity) {
            Log($"Log: {severity} - {message}");
        }

        private static void OnStatusChanged(Client.Status status, Client.Error error, int errorCode) {
            Log($"Status changed: {status}");
            if (error != Client.Error.None) {
                Log($"Error: {error}, code: {errorCode}", true);
            }
        }

        private static void Log(string message, bool error = false) {
            if (error) {
                Debug.LogError(message);
            } else {
                Debug.Log(message);
            }
        }
    }
}