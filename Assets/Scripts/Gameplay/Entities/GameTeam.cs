using System;

namespace Gameplay.Entities {
    /// <summary>
    /// Represents the team in a game
    /// </summary>
    public enum GameTeam {
        Neutral = -1,
        Player1 = 1,
        Player2 = 2
    }
    
    public static class TeamUtil {
        /// <summary>
        /// Get the opposing team for the given team
        /// </summary>
        public static GameTeam OpponentTeam(this GameTeam myTeam) {
            return myTeam switch {
                GameTeam.Neutral => GameTeam.Neutral,
                GameTeam.Player1 => GameTeam.Player2,
                GameTeam.Player2 => GameTeam.Player1,
                _ => throw new ArgumentOutOfRangeException(nameof(myTeam), myTeam, null)
            };
        }
    }
}