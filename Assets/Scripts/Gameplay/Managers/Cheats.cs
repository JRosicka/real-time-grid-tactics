using System;
using System.Collections.Generic;
using Gameplay.Config;
using Mirror;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Managers {
    /// <summary>
    /// Collection of convenient cheats for testing
    /// </summary>
    public class Cheats : MonoBehaviour {
        private GameManager GameManagerInstance => GameManager.Instance;

        [Button]
        public void ReturnToLobby() {
            // Make the return to lobby button available only for the server host
            if (!NetworkServer.active) {
                Debug.LogWarning("Can only return to the lobby on the host");
                return;
            }

            GameManagerInstance.ReturnToLobby();
        }

        [Header("Spawn Unit 1")] 
        public bool Unit1_LocalTeam;
        public EntityData Unit1;
        [Button]
        public void SpawnUnit1() {
            IGamePlayer player = Unit1_LocalTeam ? GameManagerInstance.LocalPlayer : GameManagerInstance.OpponentPlayer;
            GameManagerInstance.CommandManager.SpawnEntity(Unit1, player.Data.SpawnLocation, player.Data.Team, null);
        }

        [Header("Spawn Unit 2")] 
        public bool Unit2_LocalTeam;
        public EntityData Unit2;
        [Button]
        public void SpawnUnit2() {
            IGamePlayer player = Unit2_LocalTeam ? GameManagerInstance.LocalPlayer : GameManagerInstance.OpponentPlayer;
            GameManagerInstance.CommandManager.SpawnEntity(Unit2, player.Data.SpawnLocation, player.Data.Team, null);
        }

        [Header("Spawn Unit 3")] 
        public bool Unit3_LocalTeam;
        public EntityData Unit3;
        [Button]
        public void SpawnUnit3() {
            IGamePlayer player = Unit3_LocalTeam ? GameManagerInstance.LocalPlayer : GameManagerInstance.OpponentPlayer;
            GameManagerInstance.CommandManager.SpawnEntity(Unit3, player.Data.SpawnLocation, player.Data.Team, null);
        }
        
        [Header("Set money")] 
        public bool SetMoney_LocalTeam;
        public int SetMoney_Amount = 10000;
        [Button]
        public void SetMoney() {
            PlayerResourcesController resourcesController = SetMoney_LocalTeam ? GameManagerInstance.LocalPlayer.ResourcesController : GameManagerInstance.OpponentPlayer.ResourcesController;
            foreach (ResourceType resourceType in new[] { ResourceType.Basic, ResourceType.Advanced }) {
                int currentAmount = resourcesController.GetBalance(resourceType).Amount;
                if (currentAmount < SetMoney_Amount) {
                    ResourceAmount amountToGive = new ResourceAmount {
                        Amount = SetMoney_Amount - currentAmount,
                        Type = resourceType
                    };
                    resourcesController.Earn(amountToGive);
                } else if (currentAmount > SetMoney_Amount) {
                    ResourceAmount amountToLose = new ResourceAmount {
                        Amount = currentAmount - SetMoney_Amount,
                        Type = resourceType
                    };
                    resourcesController.Spend(new List<ResourceAmount> {amountToLose});
                }
            }
        }
        
        public bool RemoveBuildTime { get; private set; }
        [Button]
        public void ToggleRemoveBuildTime() {
            RemoveBuildTime = !RemoveBuildTime;
        }

        [Button]
        public void SwapTeamTarget() {
            Unit1_LocalTeam = !Unit1_LocalTeam;
            Unit2_LocalTeam = !Unit2_LocalTeam;
            Unit3_LocalTeam = !Unit3_LocalTeam;
            SetMoney_LocalTeam = !SetMoney_LocalTeam;
        }

        public static bool NeedsToDisconnect { get; private set; }
        [Button]
        public void ThrowNetworkedException() {
            NeedsToDisconnect = true;
        }
    }
}