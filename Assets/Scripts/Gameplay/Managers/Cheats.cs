using System.Collections.Generic;
using Gameplay.Config;
using Gameplay.Entities;
using UnityEngine;

namespace Gameplay.Managers {
    /// <summary>
    /// Game runtime object for applying cheats. 
    /// </summary>
    public class Cheats : MonoBehaviour {
        [SerializeField] private CheatConfiguration _cheatConfiguration;
        [SerializeField] private GameObject _canvas;
        
        public bool RemoveBuildTime => _cheatConfiguration.CheatsEnabled && _cheatConfiguration.RemoveBuildTime;
        public int? PlayerMoneyFromCheats => _cheatConfiguration.CheatsEnabled && _cheatConfiguration.PlayerMoney > 0 ? _cheatConfiguration.PlayerMoney : null;
        public bool ControlAllPlayers => _cheatConfiguration.CheatsEnabled && _cheatConfiguration.ControlAllPlayers;
        private List<StartingEntitySet> SpawnData => _cheatConfiguration.CheatsEnabled ? _cheatConfiguration.SpawnData : new List<StartingEntitySet>();

        void Start() {
            _cheatConfiguration.ControlAllPlayersToggled += ToggleControlAllPlayers;
        }

        void OnDestroy() {
            _cheatConfiguration.ControlAllPlayersToggled -= ToggleControlAllPlayers;
        }
        
        public void SetMoney(int amount) {
            SetMoneyForTeam(GameTeam.Player1, amount);
            SetMoneyForTeam(GameTeam.Player2, amount);
        }

        private static void SetMoneyForTeam(GameTeam team, int amount) {
            IGamePlayer player = GameManager.Instance.GetPlayerForTeam(team);
            PlayerResourcesController resourcesController = player.ResourcesController;
            foreach (ResourceType resourceType in new[] { ResourceType.Basic, ResourceType.Advanced }) {
                int currentAmount = resourcesController.GetBalance(resourceType).Amount;
                if (currentAmount < amount) {
                    ResourceAmount amountToGive = new ResourceAmount {
                        Amount = amount - currentAmount,
                        Type = resourceType
                    };
                    resourcesController.Earn(amountToGive);
                } else if (currentAmount > amount) {
                    ResourceAmount amountToLose = new ResourceAmount {
                        Amount = currentAmount - amount,
                        Type = resourceType
                    };
                    resourcesController.Spend(new List<ResourceAmount> {amountToLose});
                }
            }
        }

        public void SpawnUnits(bool built) {
            foreach (StartingEntitySet entitySet in SpawnData) {
                foreach (EntitySpawnData entitySpawnData in entitySet.Entities) {
                    GameManager.Instance.CommandManager.SpawnEntity(entitySpawnData.Data, entitySpawnData.SpawnLocation.Location,
                        entitySet.Team, null, entitySpawnData.SpawnLocation.Location, built);
                }
            }
        }

        private bool _uiActive = true;
        public void ToggleUI(bool disableEntityUIFrames) {
            _uiActive = !_uiActive;
            
            _canvas.SetActive(_uiActive);
            
            // Disable King parade animation
            foreach (KingView kingView in FindObjectsByType<KingView>(FindObjectsSortMode.None)) {
                kingView.ToggleParadeAnimation(_uiActive);
            }
            
            // Disable income animation
            foreach (IncomeAnimationBehavior incomeAnimationBehavior in FindObjectsByType<IncomeAnimationBehavior>(FindObjectsSortMode.None)) {
                incomeAnimationBehavior.ToggleIncomeAnimation(_uiActive);
            }
            
            if (disableEntityUIFrames) {
                foreach(GridEntityView entityView in FindObjectsByType<GridEntityView>(FindObjectsSortMode.None)) {
                    entityView.ToggleUI(_uiActive);
                }
            }
        }

        /// <summary>
        /// Update each registered entity with the corresponding IInteractBehavior
        /// </summary>
        private void ToggleControlAllPlayers(bool controlEverything) {
            foreach (GridEntity entity in GameManager.Instance.CommandManager.EntitiesOnGrid.AllEntities()) {
                entity.ToggleControlFromCheat(controlEverything);
            }
        }
    }
}