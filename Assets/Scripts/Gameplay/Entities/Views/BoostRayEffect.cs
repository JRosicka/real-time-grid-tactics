using Gameplay.Config;
using Gameplay.Entities.Abilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay.Entities {
    /// <summary>
    /// VFX for rays connecting the booster and boostee structures, as part of <see cref="BoostStructureAbility"/>
    /// </summary>
    public class BoostRayEffect : MonoBehaviour {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int AlphaStrength = Shader.PropertyToID("_AlphaStrength");
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        
        [Header("Endpoints")]
        [SerializeField] private Transform _source;

        [Header("Line Renderers")]
        [SerializeField] private LineRenderer _coreLine;
        [SerializeField] private LineRenderer _glowLine;

        [Header("Beam Shape")]
        [Min(2)]
        [SerializeField] private int _pointCount = 48;

        [Tooltip("Maximum sideways displacement of the beam, in world units.")]
        [SerializeField] private float _noiseAmplitude = 0.12f;

        [Tooltip("Number of broad noise variations across the beam.")]
        [SerializeField] private float _noiseFrequency = 5f;

        [Tooltip("Strength of the smaller, higher-frequency noise.")]
        [Range(0f, 1f)]
        [SerializeField] private float _detailNoiseStrength = 0.25f;

        [Tooltip("Controls how quickly noise is suppressed near the endpoints.")]
        [SerializeField] private float _endpointFalloff = 0.7f;
        
        [Range(0.01f, 0.5f)]
        [SerializeField] private float _noiseEndpointTaper = 0.15f;
        
        [Header("Beam Motion")]
        [Tooltip("How quickly the noise pattern travels from source to target.")]
        [SerializeField] private float _noiseTravelSpeed = 2.5f;

        [Header("Appearance")]
        [SerializeField] private float _coreWidth = 0.05f;
        [SerializeField] private float _glowWidth = 0.16f;

        [ColorUsage(true, true)]
        [SerializeField] private Color _coreColor = new Color(4f, 3.5f, 1f, 1f);

        [ColorUsage(true, true)]
        [SerializeField] private Color _glowColor = new Color(2f, 1.5f, 0.2f, 0.25f);
        
        [SerializeField]
        private float _coreAlphaStrength = 2f;

        [SerializeField]
        private float _coreAlphaPower = 1f;

        [Header("Texture Flow")]
        [Tooltip("Scroll speed of the line texture. Set to 0 if using an untextured material.")]
        [SerializeField] private float _textureScrollSpeed = 1.5f;

        [Tooltip("How many texture repeats are shown per world unit.")]
        [SerializeField] private float _textureTilesPerUnit = 1f;

        [Header("2D Orientation")]
        [Tooltip("For a normal XY 2D game, leave this as (0, 0, 1).")]
        [SerializeField] private Vector3 _planeNormal = Vector3.forward;

        private Vector3[] _positions;
        private Vector2 _targetPosition;

        private float _noiseSeed;
        
        private Material _coreMaterial;
        private Material _glowMaterial;

        private float _textureOffset;

        private bool _active;

        private void LateUpdate() {
            if (!_active) return;

            UpdateBeam();
            UpdateTexture();
        }

        public void SetColors(PlayerColorData colorData) {
            _coreColor.r = colorData.TeamColor.r;
            _coreColor.g = colorData.TeamColor.g;
            _coreColor.b = colorData.TeamColor.b;
            _glowColor.r = colorData.TeamColor.r;
            _glowColor.g = colorData.TeamColor.g;
            _glowColor.b = colorData.TeamColor.b;
        }

        [Button]
        public void Initialize(Vector2 targetWorldPosition) {
            _active = true;
            _coreLine.enabled = true;
            _glowLine.enabled = true;
            _targetPosition = targetWorldPosition;
            _noiseSeed = Random.Range(0f, 1f);
            
            _pointCount = Mathf.Max(_pointCount, 2);

            EnsurePositionArray();

            InitializeLine(_coreLine, _coreWidth);
            InitializeLine(_glowLine, _glowWidth);

            /*
             * Accessing .material gives this renderer its own material instance.
             * That lets us scroll each beam without changing the shared material
             * asset used by other beams.
             */
            if (_coreLine != null && _coreMaterial == null) {
                _coreMaterial = _coreLine.material;
                _coreMaterial.SetColor(BaseColor, _coreColor);
                _coreMaterial.SetFloat(AlphaStrength, _coreAlphaStrength);
            }

            if (_glowLine != null && _glowMaterial == null) {
                _glowMaterial = _glowLine.material;
                _glowMaterial.SetColor(BaseColor, _glowColor);
                _coreMaterial.SetFloat(AlphaStrength, _coreAlphaPower);
            }
        }

        public void Hide() {
            _active = false;
            _coreLine.enabled = false;
            _glowLine.enabled = false;
        }

        private void InitializeLine(LineRenderer line, float width) {
            if (line == null) return;

            line.useWorldSpace = true;
            line.positionCount = _pointCount;

            line.widthMultiplier = width;

            line.widthCurve = new AnimationCurve(new Keyframe(0f, 0f),
                new Keyframe(0.15f, 1f),
                new Keyframe(0.85f, 1f),
                new Keyframe(1f, 0f)
            );
            
            line.startColor = Color.white;
            line.endColor = Color.white;

            // One copy of the texture over the whole beam.
            line.textureMode = LineTextureMode.Tile;
            line.textureScale = new Vector2(_textureTilesPerUnit, 1f);
            
            // Usually preferable for a 2D beam facing the camera.
            line.alignment = LineAlignment.TransformZ;

            // Helps avoid obviously angular joins when the beam bends.
            line.numCornerVertices = 0;
            line.numCapVertices = 0;
        }

        private void UpdateBeam() {
            EnsurePositionArray();

            Vector3 start = _source.position;
            Vector3 end = _targetPosition;

            Vector3 delta = end - start;

            if (delta.sqrMagnitude <= Mathf.Epsilon) {
                SetAllPositions(start);
                return;
            }

            Vector3 beamDirection = delta.normalized;

            /*
             * For an XY game:
             *
             * beamDirection = right
             * planeNormal   = forward
             *
             * Cross(forward, right) gives "up/down" relative to the beam.
             */
            Vector3 perpendicular = Vector3.Cross(_planeNormal.normalized, beamDirection).normalized;

            float phase = Time.time * _noiseTravelSpeed;

            for (int i = 0; i < _pointCount; i++) {
                float t = i / (float)(_pointCount - 1);

                if (i == 0) {
                    _positions[i] = start;
                    continue;
                }

                if (i == _pointCount - 1) {
                    _positions[i] = end;
                    continue;
                }

                Vector3 position = Vector3.Lerp(start, end, t);

                float broadNoise = SampleNoise(t * _noiseFrequency - phase, _noiseSeed);
                float detailNoise = SampleNoise(t * _noiseFrequency * 2.7f - phase * 1.8f, _noiseSeed + 27.31f);
                
                float noise = broadNoise + detailNoise * _detailNoiseStrength;
                float sourceFade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / _noiseEndpointTaper));
                float targetFade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - t) / _noiseEndpointTaper));
                float noiseWeight = sourceFade * targetFade;

                position += perpendicular * (noise * _noiseAmplitude * noiseWeight);

                _positions[i] = position;
            }

            if (_coreLine != null)
                _coreLine.SetPositions(_positions);

            if (_glowLine != null)
                _glowLine.SetPositions(_positions);
        }

        private static float SampleNoise(float x, float y) {
            return Mathf.PerlinNoise(x, y) * 2f - 1f;
        }

        private void UpdateTexture() {
            _textureOffset -= _textureScrollSpeed * Time.deltaTime;
            _textureOffset = Mathf.Repeat(_textureOffset, 1f);

            Vector2 offset = new Vector2(_textureOffset, 0f);

            if (_coreMaterial != null)
                _coreMaterial.SetTextureOffset(BaseMap, offset);

            if (_glowMaterial != null)
                _glowMaterial.SetTextureOffset(BaseMap, offset);
        }

        private void EnsurePositionArray() {
            if (_positions == null || _positions.Length != _pointCount) {
                _positions = new Vector3[_pointCount];

                if (_coreLine != null)
                    _coreLine.positionCount = _pointCount;

                if (_glowLine != null)
                    _glowLine.positionCount = _pointCount;
            }
        }

        private void SetAllPositions(Vector3 position) {
            for (int i = 0; i < _positions.Length; i++)
                _positions[i] = position;

            if (_coreLine != null)
                _coreLine.SetPositions(_positions);

            if (_glowLine != null)
                _glowLine.SetPositions(_positions);
        }

        private void OnDestroy() {
            /*
             * .material creates instances, so clean them up.
             */
            if (_coreMaterial != null)
                Destroy(_coreMaterial);

            if (_glowMaterial != null)
                Destroy(_glowMaterial);
        }
    }
}