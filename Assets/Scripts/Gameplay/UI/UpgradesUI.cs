using System.Linq;
using Gameplay.Entities.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI {
    /// <summary>
    /// Displays the owned upgrades for each player
    /// </summary>
    public class UpgradesUI : MonoBehaviour {
        [SerializeField] private HorizontalLayoutGroup _player1Upgrades;
        [SerializeField] private HorizontalLayoutGroup _player2Upgrades;
        [SerializeField] private UpgradeIcon _upgradeIconPrefab;
        
        public void Initialize(IGamePlayer player1, IGamePlayer player2) {
            foreach (IUpgrade upgrade in player1.OwnedPurchasablesController.Upgrades.Upgrades.OrderBy(u => u.UpgradeData.OrderIndex)) {
                SpawnUpgrade(upgrade, player1, _player1Upgrades);
            }
            foreach (IUpgrade upgrade in player2.OwnedPurchasablesController.Upgrades.Upgrades.OrderBy(u => u.UpgradeData.OrderIndex)) {
                SpawnUpgrade(upgrade, player2, _player2Upgrades);
            }
        }

        private void SpawnUpgrade(IUpgrade upgrade, IGamePlayer player, HorizontalLayoutGroup row) {
            UpgradeIcon icon = Instantiate(_upgradeIconPrefab, row.transform);
            icon.Initialize(upgrade, player);
        }
    }
}