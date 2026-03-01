using System;
using Gameplay.Entities;
using Gameplay.UI;
using JetBrains.Annotations;
using UnityEngine;

namespace Gameplay.Config {
    /// <summary>
    /// Pathfinding configuration for a given <see cref="GridEntity"/>
    /// </summary>
    [Serializable]
    public struct EntityPathfindingConfig {
        public Sprite MoveIconOverride;
        public Sprite AttackIconOverride;
        public Sprite TargetAttackIconOverride;
        
        [CanBeNull]
        public Sprite GetIconOverride(PathVisualizer.PathType pathType) {
            return pathType switch {
                PathVisualizer.PathType.Move => MoveIconOverride,
                PathVisualizer.PathType.AttackMove => AttackIconOverride,
                PathVisualizer.PathType.TargetAttack => TargetAttackIconOverride,
                _ => null
            };
        }
    }
}