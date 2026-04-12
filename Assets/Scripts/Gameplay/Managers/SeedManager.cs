using System;
using System.Collections.Generic;

namespace Gameplay.Managers {
    /// <summary>
    /// Handles providing controlled randomness for the game. Initialized client-side. 
    /// </summary>
    public class SeedManager {
        public int Seed { get; private set; }
        private readonly Dictionary<int, Random> _uidSpecificRNGs = new Dictionary<int, Random>();
        
        public void InitializeWithSeed(int seed) {
            Seed = seed;
        }
        
        public void InitializeWithRandomSeed() {
            Seed = new Random().Next();
        }

        /// <summary>
        /// Get the uid-specific RNG
        /// </summary>
        public Random GetRNG(long uid) {
            int uidInt = HashCode.Combine(Seed, uid.GetHashCode());
            if (!_uidSpecificRNGs.ContainsKey(uidInt)) {
                _uidSpecificRNGs[uidInt] = new Random(uidInt);
            }
            
            return _uidSpecificRNGs[uidInt]; 
        }
    }
}