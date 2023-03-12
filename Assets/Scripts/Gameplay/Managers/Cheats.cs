#if UNITY_EDITOR
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
        
        private void Awake() {
            _gameNetworkManager = FindObjectOfType<GameNetworkManager>(); // TODO better way to get this
        }
        
        [Button]
        public void ReturnToLobby() {
            // Make the return to lobby button available only for the server host
            if (!NetworkServer.active) {
                Debug.LogWarning("Can only return to the lobby on the host");
                return;
            }
            
            _gameNetworkManager.ServerChangeScene(_gameNetworkManager.RoomScene);
        }

        [Header("Spawn Unit 1")] 
        public bool Unit1_LocalTeam;
        public EntityData Unit1;
        [Button]
        public void SpawnUnit1() {
            IGamePlayer player = Unit1_LocalTeam ? GameManagerInstance.LocalPlayer : GameManagerInstance.OpponentPlayer;
            GameManagerInstance.CommandManager.SpawnEntity(Unit1, player.Data.SpawnLocation, player.Data.Team);
        }

        [Header("Spawn Unit 2")] 
        public bool Unit2_LocalTeam;
        public EntityData Unit2;
        [Button]
        public void SpawnUnit2() {
            IGamePlayer player = Unit2_LocalTeam ? GameManagerInstance.LocalPlayer : GameManagerInstance.OpponentPlayer;
            GameManagerInstance.CommandManager.SpawnEntity(Unit2, player.Data.SpawnLocation, player.Data.Team);
        }

        [Header("Spawn Unit 3")] 
        public bool Unit3_LocalTeam;
        public EntityData Unit3;
        [Button]
        public void SpawnUnit3() {
            IGamePlayer player = Unit3_LocalTeam ? GameManagerInstance.LocalPlayer : GameManagerInstance.OpponentPlayer;
            GameManagerInstance.CommandManager.SpawnEntity(Unit3, player.Data.SpawnLocation, player.Data.Team);
        }
        
        [Header("Give money")] 
        public bool GiveMoney_LocalTeam;
        public int GiveMoney_Amount = 10000;
        public ResourceType GiveMoney_ResourceType = ResourceType.Basic;
        [Button]
        public void GiveMoney() {
            IGamePlayer player = GiveMoney_LocalTeam ? GameManagerInstance.LocalPlayer : GameManagerInstance.OpponentPlayer;
            ResourceAmount amountToGive = new ResourceAmount {
                Amount = GiveMoney_Amount,
                Type = GiveMoney_ResourceType
            };
            player.ResourcesController.Earn(amountToGive);
        }
    }
}
#endif