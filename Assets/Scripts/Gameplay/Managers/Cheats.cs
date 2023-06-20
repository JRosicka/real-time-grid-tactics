using System.Collections.Generic;
using Game.Network;
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
        private GameNetworkManager _gameNetworkManager;

        [Button]
        public void ReturnToLobby() {
            Debug.Log("Cheat: return to lobby");

            // Make the return to lobby button available only for the server host
            if (!NetworkServer.active) {
                Debug.LogWarning("Can only return to the lobby on the host");
                return;
            }

            if (_gameNetworkManager == null) {
                _gameNetworkManager = FindObjectOfType<GameNetworkManager>(); // TODO better way to get this
            }
            _gameNetworkManager.ServerChangeScene(_gameNetworkManager.RoomScene);
        }

        [Header("Spawn Unit 1")] 
        public bool Unit1_LocalTeam;
        public EntityData Unit1;
        [Button]
        public void SpawnUnit1() {
            Debug.Log("Cheat 1: start");
            IGamePlayer player = Unit1_LocalTeam ? GameManagerInstance.LocalPlayer : GameManagerInstance.OpponentPlayer;
            GameManagerInstance.CommandManager.SpawnEntity(Unit1, player.Data.SpawnLocation, player.Data.Team, null);
        }

        [Header("Spawn Unit 2")] 
        public bool Unit2_LocalTeam;
        public EntityData Unit2;
        [Button]
        public void SpawnUnit2() {
            Debug.Log("Cheat 2: start");
            IGamePlayer player = Unit2_LocalTeam ? GameManagerInstance.LocalPlayer : GameManagerInstance.OpponentPlayer;
            GameManagerInstance.CommandManager.SpawnEntity(Unit2, player.Data.SpawnLocation, player.Data.Team, null);
        }

        [Header("Spawn Unit 3")] 
        public bool Unit3_LocalTeam;
        public EntityData Unit3;
        [Button]
        public void SpawnUnit3() {
            Debug.Log("Cheat 3: start");
            IGamePlayer player = Unit3_LocalTeam ? GameManagerInstance.LocalPlayer : GameManagerInstance.OpponentPlayer;
            GameManagerInstance.CommandManager.SpawnEntity(Unit3, player.Data.SpawnLocation, player.Data.Team, null);
        }
        
        [Header("Set money")] 
        public bool SetMoney_LocalTeam;
        public int SetMoney_Amount = 10000;
        public ResourceType SetMoney_ResourceType = ResourceType.Basic;
        [Button]
        public void SetMoney() {
            Debug.Log("Cheat 4: start");
            PlayerResourcesController resourcesController = SetMoney_LocalTeam ? GameManagerInstance.LocalPlayer.ResourcesController : GameManagerInstance.OpponentPlayer.ResourcesController;
            int currentAmount = resourcesController.GetBalance(SetMoney_ResourceType).Amount;
            if (currentAmount < SetMoney_Amount) {
                ResourceAmount amountToGive = new ResourceAmount {
                    Amount = SetMoney_Amount - currentAmount,
                    Type = SetMoney_ResourceType
                };
                resourcesController.Earn(amountToGive);
            } else if (currentAmount > SetMoney_Amount) {
                ResourceAmount amountToLose = new ResourceAmount {
                    Amount = currentAmount - SetMoney_Amount,
                    Type = SetMoney_ResourceType
                };
                resourcesController.Spend(new List<ResourceAmount> {amountToLose});
            }
        }

        public void SwapTeamTarget() {
            Debug.Log("Cheat 5: start");
            Unit1_LocalTeam = !Unit1_LocalTeam;
            Unit2_LocalTeam = !Unit2_LocalTeam;
            Unit3_LocalTeam = !Unit3_LocalTeam;
            SetMoney_LocalTeam = !SetMoney_LocalTeam;
        }
    }
}