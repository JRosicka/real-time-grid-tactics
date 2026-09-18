using System.Collections.Generic;
using Gameplay.Entities;
using Gameplay.Grid;
using JetBrains.Annotations;
using Scenes;
using Sirenix.Utilities;
using UnityEngine;

namespace Gameplay.Managers {
    /// <summary>
    /// Central logic for Fog of War. Handles state management, events, and communicating to entities when FoW state changes. 
    /// Entirely client-side.
    ///
    /// Sets up one or more <see cref="TeamFogOfWarTracker"/>s to track state on a per-team basis. The behavior for that:
    /// - If this is a client that is a player, we just need to track its own team's FoW
    /// - If this is a client that is a spectator, needs to track both players' FoW for path visualization purposes
    /// - If this is the server, then we need to track both players' FoW regardless of spectator or not 
    /// </summary>
    public class FogOfWarManager {
        private readonly Dictionary<GameTeam, TeamFogOfWarTracker> _teamFogOfWarTrackers = new();
        private readonly GameTeam _localTeam;
        
        public FogOfWarSetting LocalFowSetting { get; }
        
        public FogOfWarManager(GridController gridController, ICommandManager commandManager, FogOfWarSetting fowSetting, bool realGame, GameTeam localTeam) {
            _localTeam = localTeam;
            FogOfWarSetting fowForMatch = DetermineFoWSettingForMatch(fowSetting, realGame);
            LocalFowSetting = localTeam == GameTeam.Spectator ? FogOfWarSetting.None : fowForMatch;
            
            if (GameTypeTracker.Instance.HostForNetworkedGame || !GameTypeTracker.Instance.GameIsNetworked) {
                // MP server or SP. Need to track FoW for both teams
                RegisterFoWForTeam(GameTeam.Player1, fowForMatch, gridController, commandManager);
                RegisterFoWForTeam(GameTeam.Player2, fowForMatch, gridController, commandManager);
            } else if (localTeam == GameTeam.Spectator) {
                // Spectators are able to observe paths for either player, so need to track FoW for both teams
                RegisterFoWForTeam(GameTeam.Player1, fowForMatch, gridController, commandManager);
                RegisterFoWForTeam(GameTeam.Player2, fowForMatch, gridController, commandManager);
            } else {
                // This is a client on a team. Only need to track the local team. 
                RegisterFoWForTeam(localTeam, fowForMatch, gridController, commandManager);
            }
        }

        [CanBeNull]
        public TeamFogOfWarTracker GetTracker(GameTeam team) {
            if (team is GameTeam.Spectator or GameTeam.Neutral) return null;
            
            if (!_teamFogOfWarTrackers.TryGetValue(team, out TeamFogOfWarTracker tracker)) {
                Debug.LogWarning($"No fog of war tracker for team {team}. Registered trackers: {string.Join(", ", _teamFogOfWarTrackers.Keys)}");
                return null;
            }

            return tracker;
        }

        [CanBeNull]
        public TeamFogOfWarTracker GetLocalTeamTracker() {
            return GetTracker(_localTeam);
        }
        
        public void UnregisterListeners() {
            _teamFogOfWarTrackers.ForEach(t => t.Value.UnregisterListeners());
        }

        private void RegisterFoWForTeam(GameTeam team, FogOfWarSetting fowSetting, GridController gridController, ICommandManager commandManager) {
            // We only want to directly update the entity visuals from this tracker if it is for the local team, since 
            // displaying of units only happens through the local team FoW.
            bool updateEntityVisuals = team == _localTeam;
            _teamFogOfWarTrackers[team] = new TeamFogOfWarTracker(team, fowSetting, updateEntityVisuals, gridController, commandManager);
        }
        
        private FogOfWarSetting DetermineFoWSettingForMatch(FogOfWarSetting fowSetting, bool realGame) {
            if (!realGame) return FogOfWarSetting.None;
            return fowSetting;
        }
    }
}