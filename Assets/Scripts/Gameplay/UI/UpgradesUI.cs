using System.Collections.Generic;
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
        
        private readonly List<UpgradeIcon> _player1UpgradeIcons = new List<UpgradeIcon>();
        private readonly List<UpgradeIcon> _player2UpgradeIcons = new List<UpgradeIcon>();
        
        public void Initialize(IGamePlayer player1, IGamePlayer player2) {
            foreach (IUpgrade upgrade in player1.OwnedPurchasablesController.Upgrades.Upgrades.OrderBy(u => u.UpgradeData.OrderIndex)) {
                SpawnUpgrade(upgrade, player1, _player1Upgrades, _player1UpgradeIcons);
            }
            foreach (IUpgrade upgrade in player2.OwnedPurchasablesController.Upgrades.Upgrades.OrderBy(u => u.UpgradeData.OrderIndex)) {
                SpawnUpgrade(upgrade, player2, _player2Upgrades, _player2UpgradeIcons);
            }
        }

        private void SpawnUpgrade(IUpgrade upgrade, IGamePlayer player, HorizontalLayoutGroup row, List<UpgradeIcon> upgradeIcons) {
            UpgradeIcon icon = Instantiate(_upgradeIconPrefab, row.transform);
            icon.Initialize(upgrade, player);
            icon.UpgradeStatusChanged += HideInactiveRows;
            upgradeIcons.Add(icon);
        }

        private void HideInactiveRows() {
            _player1Upgrades.gameObject.SetActive(_player1UpgradeIcons.Any(u => u.UpgradeActive));
            _player2Upgrades.gameObject.SetActive(_player2UpgradeIcons.Any(u => u.UpgradeActive));
        }
    }
}