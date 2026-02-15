using UnityEngine;
using Random = System.Random;

namespace Gameplay.Grid {
    /// <summary>
    /// Background image for particular tiles. Exists to handle things like rotating the background to be consistent.
    /// </summary>
    public class TileBackground : MonoBehaviour {
        private void Start() {
            transform.localRotation = Quaternion.Euler(0, 0, 0);
            int xScale = new Random().Next(0, 2) == 1 ? 1 : -1;
            transform.localScale = new Vector3(xScale, 1, 1);
        }
    }
}