using Discord.Sdk;
using UnityEngine;

namespace Game.Network.Discord {
    /// <summary>
    /// Handles using Discord social features
    /// </summary>
    public class DiscordManager {
        private const ulong ApplicationID = 1539068505301721138;

        private readonly Client _discordClient;
        private readonly Activity _richPresenceActivity;

        public static DiscordManager Instance;

        public DiscordManager() {
            Instance = this;
            
            _discordClient = new Client();
            _discordClient.AddLogCallback(OnLog, LoggingSeverity.Warning);
            _discordClient.SetStatusChangedCallback(OnStatusChanged);
            _discordClient.SetApplicationId(ApplicationID);

            _richPresenceActivity = new Activity();
            AddButton(_richPresenceActivity);

            ResetRichPresence();
        }

        public void SetRichPresence(string details, string state) {
            _richPresenceActivity.SetType(ActivityTypes.Playing);
            _richPresenceActivity.SetDetails(details);
            _richPresenceActivity.SetState(state);
            
            _discordClient.UpdateRichPresence(_richPresenceActivity, result => {
                Log($"Rich Presence result: {result}");
            });
        }

        public void ResetRichPresence() {
            SetRichPresence(null, null);
            
            _richPresenceActivity.SetType(ActivityTypes.Playing);
            _discordClient.UpdateRichPresence(_richPresenceActivity, result => {
                Log($"Rich Presence result: {result}");
            });
        }

        private void AddButton(Activity activity) {
            ActivityButton communityButton = new ActivityButton();
            communityButton.SetLabel("Join the Community!");
            communityButton.SetUrl("https://discord.gg/1327435939550724147");
            
            activity.AddButton(communityButton);
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