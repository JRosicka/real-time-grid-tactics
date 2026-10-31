using System.Collections.Generic;
using Gameplay.Grid;
using Mirror;
using Scenes;
using UnityEngine;

namespace Gameplay.Entities.Abilities {
    public class CatapultAttackLogic : IAttackLogic {
        private CatapultRocks CatapultRocksPrefab => GameManager.Instance.PrefabAtlas.CatapultRocks;
        private GridController GridController => GameManager.Instance.GridController;

        public void DoAttack(GridEntity attacker, GridEntity target, int bonusDamage) {
            throw new System.NotImplementedException();
        }

        public void DoAttack(GridEntity attacker, HashSet<Vector2Int> targetLocations, int bonusDamage) {
            Vector2Int? attackLocation = attacker.Location;
            if (attackLocation == null) return;

            Vector3 attackWorldPosition = GridController.GetWorldPosition(attackLocation.Value);

            CatapultRocks rocks = Object.Instantiate(CatapultRocksPrefab, attackWorldPosition, Quaternion.identity, 
                GameManager.Instance.CommandManager.SpawnBucket);

            if (GameTypeTracker.Instance.GameIsNetworked) {
                NetworkServer.Spawn(rocks.gameObject);
            }

            rocks.Initialize(attacker, targetLocations, bonusDamage);
        }
    }
}