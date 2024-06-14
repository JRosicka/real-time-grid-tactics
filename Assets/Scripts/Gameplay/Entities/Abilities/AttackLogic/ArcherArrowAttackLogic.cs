using System;
using System.Threading.Tasks;
using Gameplay.Grid;
using Mirror;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gameplay.Entities.Abilities {
    /// <summary>
    /// <see cref="IAttackLogic"/> for an archer ranged attack
    /// </summary>
    public class ArcherArrowAttackLogic : IAttackLogic {
        private ArcherArrow ArrowPrefab => GameManager.Instance.PrefabAtlas.ArcherArrow;
        private GridController GridController => GameManager.Instance.GridController;

        private GridEntity _attacker;
        private GridEntity _target;
        
        public void DoAttack(GridEntity attacker, GridEntity target) {
            _attacker = attacker;
            _target = target;
            
            Vector2Int attackLocation = attacker.Location;
            Vector2Int targetLocation = target.Location;
            Vector3 attackWorldPosition = GridController.GetWorldPosition(attackLocation);
            Vector3 targetWorldPosition = GridController.GetWorldPosition(targetLocation);
            
            ArcherArrow arrow = Object.Instantiate(ArrowPrefab, attackWorldPosition, Quaternion.identity, 
                GameManager.Instance.CommandManager.SpawnBucket);

            if (NetworkClient.active) {
                NetworkServer.Spawn(arrow.gameObject);
            }

            MoveArrow(arrow, targetWorldPosition);
        }
        
        private async void MoveArrow(ArcherArrow arrow, Vector3 targetPosition) {
            await Task.Delay(TimeSpan.FromSeconds(1));
            arrow.transform.position = targetPosition;
            _target.ReceiveAttackFromEntity(_attacker);
        }
    }
}