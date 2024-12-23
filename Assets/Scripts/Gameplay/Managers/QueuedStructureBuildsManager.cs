using System.Collections.Generic;
using System.Linq;
using Gameplay.Config;
using Gameplay.Entities;
using Gameplay.Entities.Abilities;
using Sirenix.Utilities;
using UnityEngine;

namespace Gameplay.Managers {
    /// <summary>
    /// Handles tracking and displaying queued structures being built. When a structure gets added, we display a
    /// silhouette of the structure on the destination and prevent the local player from trying to build other stuff
    /// in that location. When the build gets started or canceled, we remove the silhouette from that location.
    /// 
    /// Entirely client-side. 
    /// </summary>
    public class QueuedStructureBuildsManager {
        private readonly GameManager _gameManager;
        private readonly List<QueuedBuildInfo> _queuedStructures = new List<QueuedBuildInfo>();
        
        public List<Vector2Int> LocationsWithQueuedStructures => _queuedStructures.Select(s => s.BuildParameters.BuildLocation).ToList();

        public QueuedStructureBuildsManager(GameManager gameManager) {
            _gameManager = gameManager;
        }

        public void RegisterQueuedStructure(BuildAbilityParameters queuedBuild, GridEntity builder) {
            // TODO account for multiple?
            // builder.KilledEvent += UnregisterQueuedBuildsForEntity;

            if (_queuedStructures.Any(s => s.BuildParameters.BuildLocation == queuedBuild.BuildLocation)) {
                Debug.LogWarning("Tried to register a queued build for a location that already has one. This should not be allowed.");
                return;
            }

            InProgressBuildingView structureSilhouette = Object.Instantiate(_gameManager.PrefabAtlas.StructureImagesView, 
                    _gameManager.GridController.GetWorldPosition(queuedBuild.BuildLocation), 
                    Quaternion.identity,
                    _gameManager.CommandManager.SpawnBucket);
            structureSilhouette.Initialize(builder.Team, (EntityData)queuedBuild.Buildable, true);
            
            _queuedStructures.Add(new QueuedBuildInfo {
                Builder = builder,
                BuildParameters = queuedBuild,
                StructureSilhouette = structureSilhouette
            });
        }

        public void UnregisterNoLongerQueuedBuildsForEntity(GridEntity builder) {
            List<QueuedBuildInfo> queuedStructuresCopy = new List<QueuedBuildInfo>(_queuedStructures);
            List<BuildAbility> currentlyQueuedBuilds = builder.QueuedAbilities
                .Where(a => a is BuildAbility)
                .Cast<BuildAbility>()
                .ToList();
            // Remove queued builds for this builder...
            queuedStructuresCopy.Where(info => info.Builder == builder)
                    // but only those whose build locations are not present in the list of build locations for the builder's current queued builds
                    .Where(info => currentlyQueuedBuilds.All(b => b.AbilityParameters.BuildLocation != info.BuildParameters.BuildLocation))
                    .ForEach(info => {
                _queuedStructures.Remove(info);
                info.StructureSilhouette.RemoveView();
            });
            
            // TODO account for multiple?
            // builder.KilledEvent -= UnregisterQueuedBuildsForEntity;
        }
        
        private class QueuedBuildInfo {
            public GridEntity Builder;
            public BuildAbilityParameters BuildParameters;
            public InProgressBuildingView StructureSilhouette;
        }
    }
}