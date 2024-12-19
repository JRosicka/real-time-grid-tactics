using System;
using System.Linq;
using Gameplay.Config;
using Gameplay.Entities;
using UnityEngine;

/// <summary>
/// Detects and handles the end of the game
/// </summary>
public class GameEndManager {
    public event Action<IGamePlayer> GameEnded;
    
    private readonly GameManager _gameManager;
    private bool _gameOver;
    
    private GridEntityCollection EntityCollection => _gameManager.CommandManager.EntitiesOnGrid;

    public GameEndManager(GameManager gameManager) {
        _gameManager = gameManager;
    }
    
    /// <summary>
    /// Check to see if a player has won by the victory condition. Only runs on the server.
    /// </summary>
    public void CheckForGameEnd() {
        if (_gameOver) return;
        if (!_gameManager.GameSetupManager.GameInitialized) return;
        
        bool player1Lost = PlayerHasLost(_gameManager.Player1);
        bool player2Lost = PlayerHasLost(_gameManager.Player2);
        
        if (player1Lost && player2Lost) {
            // Tie, because both players lost at the same time
            EndGame(null);
        } else if (player1Lost) {
            EndGame(_gameManager.Player2);
        } else if (player2Lost) {
            EndGame(_gameManager.Player1);
        }
    }

    public void EndGame(IGamePlayer winner) {
        _gameOver = true;
        
        if (winner == null) {
            Debug.Log("Game over - tie!");
        } else {
            Debug.Log($"Game over - the winner is {winner.DisplayName}");
        }
        
        GameEnded?.Invoke(winner);
    }
    
    private bool PlayerHasLost(IGamePlayer player) {
        return !EntityCollection.ActiveEntitiesForTeam(player.Data.Team).Any(e => e.Tags.Contains(EntityData.EntityTag.HomeBase));
    }
}