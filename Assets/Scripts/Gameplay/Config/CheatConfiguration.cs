using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Gameplay.Config {
    /// <summary>
    /// Handles persisting cheats state. 
    /// </summary>
    [CreateAssetMenu(menuName = "Configuration/CheatConfiguration", fileName = "CheatConfiguration", order = 0)]
    public class CheatConfiguration : ScriptableObject {
        public event Action CheatConfigurationChanged;
        
        [SerializeField]
        private CheatData _currentCheats;
        [SerializeField]
        private CheatData _savedCheats;
        
        public bool CheatsEnabled {
            get => _currentCheats.CheatsEnabled;
            set {
                _currentCheats.CheatsEnabled = value;
                ApplyCheats();
            }
        }

        public int PlayerMoney {
            get => _currentCheats.PlayerMoney;
            set {
                _currentCheats.PlayerMoney = value;
                ApplyCheats();
            }
        }
        public bool RemoveBuildTime {
            get => _currentCheats.RemoveBuildTime;
            set {
                _currentCheats.RemoveBuildTime = value;
                ApplyCheats();
            }
        }

        public bool ControlAllPlayers {
            get => _currentCheats.ControlAllPlayers;
            set {
                _currentCheats.ControlAllPlayers = value;
                ApplyCheats();
            }
        }

        public List<EntitySpawnData> SpawnData {
            get => _currentCheats.SpawnData;
            set {
                _currentCheats.SpawnData = value;
                ApplyCheats();
            }
        }

        /// <summary>
        /// Take the <see cref="_currentCheats"/> state and copy its contents to <see cref="_savedCheats"/> for later retrieval
        /// </summary>
        public void SaveCheats() {
            _savedCheats.CheatsEnabled = CheatsEnabled;
            _savedCheats.PlayerMoney = PlayerMoney;
            _savedCheats.RemoveBuildTime = RemoveBuildTime;
            _savedCheats.ControlAllPlayers = ControlAllPlayers;
            
            _savedCheats.SpawnData.Clear();
            foreach (EntitySpawnData spawnData in SpawnData) {
                _savedCheats.SpawnData.Add(new EntitySpawnData(spawnData));
            }

            ApplyCheats();
        }

        /// <summary>
        /// Overwrite the <see cref="_currentCheats"/> state with the contents of <see cref="_savedCheats"/>
        /// </summary>
        public void LoadCheats() {
            _currentCheats.CheatsEnabled = _savedCheats.CheatsEnabled;
            _currentCheats.PlayerMoney = _savedCheats.PlayerMoney;
            _currentCheats.RemoveBuildTime = _savedCheats.RemoveBuildTime;
            _currentCheats.ControlAllPlayers = _savedCheats.ControlAllPlayers;
            
            _currentCheats.SpawnData.Clear();
            foreach (EntitySpawnData spawnData in _savedCheats.SpawnData) {
                _currentCheats.SpawnData.Add(new EntitySpawnData(spawnData));
            }
            ApplyCheats();
        }

        private void ApplyCheats() {
            CheatConfigurationChanged?.Invoke();

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }
    }

    /// <summary>
    /// A persist-able set of data for all the cheats
    /// </summary>
    [Serializable]
    public class CheatData {
        /// <summary>
        /// Whether any cheats get applied
        /// </summary>
        public bool CheatsEnabled;
        /// <summary>
        /// How much of each currency each player should have
        /// </summary>
        public int PlayerMoney;
        /// <summary>
        /// If true, build time is close to 0
        /// </summary>
        public bool RemoveBuildTime;
        /// <summary>
        /// Whether the local player is able to control entities for all players
        /// </summary>
        public bool ControlAllPlayers;
        /// <summary>
        /// Extra spawn configurations
        /// </summary>
        public List<EntitySpawnData> SpawnData;
    }
}