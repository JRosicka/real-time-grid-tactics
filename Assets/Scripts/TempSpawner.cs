using Game.Network;
using Mirror;
using UnityEngine;

namespace Game.Gameplay
{
    public class TempSpawner
    {
        public static void InitialSpawn()
        {
            if (!NetworkServer.active) return;

            for (int i = 0; i < 10; i++)
                SpawnReward();
        }

        public static void SpawnReward()
        {
            // if (!NetworkServer.active) return;
            //
            // Vector3 spawnPosition = new Vector3(Random.Range(-19, 20), 1, Random.Range(-19, 20));
            // NetworkServer.Spawn(Object.Instantiate(((GameNetworkManager)NetworkManager.singleton).rewardPrefab, spawnPosition, Quaternion.identity));
        }
    }
}
