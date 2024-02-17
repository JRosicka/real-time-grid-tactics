using Sirenix.OdinInspector;
using UnityEngine;
using Random = System.Random;

namespace Util {
    /// <summary>
    /// Applies a shake (translation and rotation) to the local position of the attached GameObject using perlin noise
    /// </summary>
    public class PerlinShakeBehaviour : MonoBehaviour {
        [Range(0, 1)]
        [Tooltip("The initial power of the shake")][SerializeField] private float _traumaStartAmount = 1;
        [Tooltip("The power of the shake")][SerializeField] private float _traumaMultiplier = 16;
        [Tooltip("The range of movement")][SerializeField] private float _traumaMag = 2f;
        [Tooltip("The rotational power")][SerializeField] private float _traumaRotMag = 17f;
        [Tooltip("How quickly the shake falls off")][SerializeField] private float _traumaDecay = 1.3f;
        [Tooltip("How quickly the lerp back to the original position takes")][SerializeField] private float _returnTime = 1f;

        private bool _shakeActive;
        private float _trauma;
        private float _timeCounter;

        private bool _startedReturn;
        private float _returnTimeCounter;
        private Vector3 _returnStartPosition;
        private Quaternion _returnStartRotation;

        /// <summary>
        /// Get a perlin float between -1 & 1, based off the time counter.
        /// </summary>
        private float GetPerlinFloat(float seed) => (Mathf.PerlinNoise(seed, _timeCounter) - 0.5f) * 2f;
        private Vector2 GetPerlinVector() => new Vector2(GetPerlinFloat(1), GetPerlinFloat(10));

        [Button]
        public void Shake() {
            Reset();
            _shakeActive = true;
        }

        private void Start() {
            _timeCounter = new Random().Next(0, 1000);
        }

        private void Update () {
            if (!_shakeActive) return;
            
            if (_trauma > 0) {
                // Increase the time counter (how fast the position changes) based off the trauma multiplier and some root of the trauma
                _timeCounter += Time.deltaTime * Mathf.Pow(_trauma,0.3f) * _traumaMultiplier;
                
                // Bind the movement to the desired range
                Vector2 newPos = GetPerlinVector() * _traumaMag * _trauma;
                transform.localPosition = newPos;
                
                // Rotation modifier applied here
                transform.localRotation = Quaternion.Euler(0, 0, GetPerlinFloat(0) * _traumaRotMag);
                
                // Decay the trauma
                _trauma -= Mathf.Clamp01(Time.deltaTime * _traumaDecay * (_trauma + 0.3f));
            } else {
                if (!_startedReturn) {
                    _startedReturn = true;
                    _returnStartPosition = transform.localPosition;
                    _returnStartRotation = transform.localRotation;
                }
                _returnTimeCounter += Time.deltaTime;
                float returnTime01 = Mathf.Clamp01(_returnTimeCounter / _returnTime);
                
                // Lerp back towards default position and rotation once shake is done
                Vector3 newPos = Vector3.Lerp(_returnStartPosition, Vector3.zero, returnTime01);
                transform.localPosition = newPos;
                transform.localRotation = Quaternion.Lerp(_returnStartRotation, Quaternion.identity, returnTime01);

                if (_returnTimeCounter >= _returnTime) {
                    Reset();
                }
            }
        }

        private void Reset() {
            _shakeActive = false;
            _startedReturn = false;
            _trauma = _traumaStartAmount;
            _returnTimeCounter = 0;
            _returnStartPosition = Vector3.zero;
            _returnStartRotation = Quaternion.identity;
        }
    }
}