using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// Controls an animation that plays for rocks landing on an individual cell. Owned by <see cref="CatapultRocks"/>
    /// </summary>
    public class RocksLandingAnimation : MonoBehaviour {
        [SerializeField] private float _lifetime;

        private float _currentTime;
        
        public void SetFacingDirection(bool faceRight) {
            int facingDirection = faceRight ? 1 : -1;
            transform.localScale = new Vector3(transform.localScale.x * facingDirection, transform.localScale.y, transform.localScale.z);
        }

        public void Update() {
            _currentTime += Time.deltaTime;
            if (_currentTime > _lifetime) {
                Destroy(gameObject);
            }
        }
    }
}