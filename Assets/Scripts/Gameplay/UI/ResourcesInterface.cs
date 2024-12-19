using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Entities;
using Gameplay.Managers;
using UnityEngine;

namespace Gameplay.UI {
    /// <summary>
    /// View for displaying current resources
    /// TODO display income rate
    /// </summary>
    public class ResourcesInterface : MonoBehaviour {
        [SerializeField] private PlayerResourcesView _playerResourcesViewPrefab;
        [SerializeField] private Transform _resourcesViewParent;

        private readonly Dictionary<GameTeam, PlayerResourcesView> _playerResourcesViews = new Dictionary<GameTeam, PlayerResourcesView>();

        private GameConfiguration GameConfiguration => GameManager.Instance.Configuration;
        
        public void Initialize(IPlayerResourcesObserver resourcesObserver) {
            resourcesObserver.BalanceChanged += UpdateBalancesView;

            // Create a view for each player
            foreach (IGamePlayer player in resourcesObserver.ObservedPlayers) {
                PlayerResourcesView resourcesView = Instantiate(_playerResourcesViewPrefab, _resourcesViewParent);
                resourcesView.SetPlayerDetails(player.DisplayName, player.Data.TeamColor);
                resourcesView.UpdateAmounts(GameConfiguration.StartingGoldAmount, GameConfiguration.StartingAmberAmount);
                _playerResourcesViews.Add(player.Data.Team, resourcesView);
            }
        }

        private void UpdateBalancesView(GameTeam team, List<ResourceAmount> resourceAmounts) {
            int newGoldAmount = resourceAmounts.First(r => r.Type == ResourceType.Basic).Amount;
            int newAmberAmount = resourceAmounts.First(r => r.Type == ResourceType.Advanced).Amount;
            _playerResourcesViews[team].UpdateAmounts(newGoldAmount, newAmberAmount);
        }
    }
}