using System.Collections.Generic;
using Gameplay.Config;
using Gameplay.Entities;
using UnityEngine;

namespace Gameplay.Managers {
    /// <summary>
    /// Game runtime object for applying cheats. 
    /// </summary>
    public class Cheats : MonoBehaviour {
        [SerializeField]
        private CheatConfiguration _cheatConfiguration;
        
        public bool RemoveBuildTime => _cheatConfiguration.CheatsEnabled && _cheatConfiguration.RemoveBuildTime;
        public int? PlayerMoneyFromCheats => _cheatConfiguration.CheatsEnabled ? _cheatConfiguration.PlayerMoney : null;
        public bool ControlAllPlayers => _cheatConfiguration.CheatsEnabled && _cheatConfiguration.ControlAllPlayers;
        private List<EntitySpawnData> SpawnData => _cheatConfiguration.CheatsEnabled ? _cheatConfiguration.SpawnData : new List<EntitySpawnData>();

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

        public void SpawnUnits() {
            foreach (EntitySpawnData entitySpawnData in SpawnData) {
                GameManager.Instance.CommandManager.SpawnEntity(entitySpawnData.Data, entitySpawnData.SpawnLocation, entitySpawnData.Team, null, false);
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