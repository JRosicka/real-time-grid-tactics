using UnityEngine;

namespace Gameplay.Managers {
    /// <summary>
    /// Handles providing controlled randomness for the game. 
    /// </summary>
    public class SeedManager {
        public int Seed { get; private set; }
        
        public void InitializeWithSeed(int seed) {
            Seed = seed;
            Random.InitState(seed);
        }
        
        public void InitializeWithRandomSeed() {
            Seed = new System.Random().Next();
            Random.InitState(Seed);
        }
    }
}